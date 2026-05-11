# GPS Location Tracking Flow — Chi Tiết Toàn Bộ Luồng

## 📍 Tổng Quan Luồng

Hệ thống GPS của VinhKhanh theo dõi vị trí du khách liên tục từ khi mở app, xử lý dữ liệu theo batch, và gửi lên server để phục vụ analytics, geofencing, và session tracking.

**Luồng chính:**
```
StartTrackingAsync() 
  → GpsForegroundService (Android) / iOS Loop
    → LocationUpdatedMessage (WeakReferenceMessenger)
      → MainViewModel Buffer (25 điểm)
        → MovementController.BatchLog (POST /api/movement/batch)
          → Database (Movement table)
```

---

## 🔄 Chi Tiết Từng Giai Đoạn

### **Giai Đoạn 1: Khởi Động GPS Service**

#### 1.1 Khi nào được gọi?
- **Thời điểm:** Khi du khách mở app lần đầu hoặc quay lại app
- **Hàm:** `GpsService.StartTrackingAsync()`
- **Nơi gọi:** `MainViewModel.OnAppearing()` hoặc `App.xaml.cs` lifecycle

#### 1.2 Quy trình khởi động

**Bước 1: Kiểm tra chế độ Mock**
```csharp
if (IsMockMode)
{
    await RunRouteSimulationAsync();  // Chạy lộ trình giả lập
    return;
}
```
- Nếu bật Mock Mode (dùng để test/demo), hệ thống sẽ chạy một lộ trình mô phỏng thay vì lấy GPS thật
- Mock Mode phát hành tọa độ mỗi **1.5 giây** (nhanh hơn GPS thật để test nhanh)

**Bước 2: Xin quyền truy cập vị trí (Android)**
```csharp
var status = await Permissions.CheckStatusAsync<Permissions.LocationAlways>();
if (status != PermissionStatus.Granted)
{
    status = await Permissions.RequestAsync<Permissions.LocationAlways>();
}
```
- Yêu cầu quyền `LocationAlways` (cho phép theo dõi ngay cả khi app ở background)
- Nếu bị từ chối, fallback sang `LocationWhenInUse` (chỉ khi app đang mở)

**Bước 3: Khởi động Foreground Service (Android)**
```csharp
if (status == PermissionStatus.Granted)
{
    await GpsForegroundService.StartForegroundService();
}
```
- Trên Android, GPS phải chạy trong Foreground Service để tránh bị hệ thống kill
- Hiển thị notification "Đang theo dõi vị trí" cho người dùng biết

**Bước 4: Khởi động iOS Loop**
```csharp
// iOS không cần Foreground Service, chỉ cần loop lấy vị trí
await StartIosLoopAsync();
```
- iOS có cơ chế khác, không cần Foreground Service
- Chạy loop lấy vị trí mỗi **1.5 giây**

---

### **Giai Đoạn 2: Lấy Tọa Độ GPS Liên Tục**

#### 2.1 Tần suất lấy dữ liệu

| Platform | Tần suất | Nguồn |
|----------|---------|-------|
| Mock Mode | 1.5 giây | Lộ trình giả lập (5 điểm dừng) |
| Android (Foreground Service) | 3 giây | GPS thật qua `GpsForegroundService` |
| iOS | 1.5 giây | `Geolocation.GetLocationAsync()` |

#### 2.2 Cơ chế phát hành thông báo

**Sử dụng WeakReferenceMessenger (MVVM Toolkit)**

```csharp
// Trong GpsService hoặc GpsForegroundService
var location = new Location(latitude, longitude);
WeakReferenceMessenger.Default.Send(
    new LocationUpdatedMessage(location)
);
```

**Tại sao dùng WeakReferenceMessenger?**
- ✅ Tách biệt hoàn toàn GPS logic (service) và UI logic (ViewModel)
- ✅ Tránh memory leak (weak reference tự động cleanup)
- ✅ Cho phép nhiều subscriber lắng nghe cùng một message
- ✅ Không cần dependency injection phức tạp

#### 2.3 Ai lắng nghe LocationUpdatedMessage?

**Subscriber 1: MainViewModel**
```csharp
WeakReferenceMessenger.Default.Register<LocationUpdatedMessage>(this, 
    (recipient, message) =>
    {
        BufferLocation(message.Location);  // Lưu vào buffer
        CheckGeofence(message.Location);   // Kiểm tra geofence
    });
```
- Lưu tọa độ vào buffer (tối đa 25 điểm)
- Kiểm tra xem có vào vùng geofence nào không

**Subscriber 2: SessionTrackingService**
```csharp
WeakReferenceMessenger.Default.Register<LocationUpdatedMessage>(this,
    (recipient, message) =>
    {
        CalculateDistance(message.Location);  // Tính quãng đường
    });
```
- Tính quãng đường đi được bằng công thức Haversine
- Bỏ qua GPS jitter < 2m và nhảy bất thường > 500m

**Subscriber 3: UserLocationLayer (Map)**
```csharp
WeakReferenceMessenger.Default.Register<LocationUpdatedMessage>(this,
    (recipient, message) =>
    {
        UpdateUserMarker(message.Location);  // Cập nhật marker trên bản đồ
    });
```
- Cập nhật vị trí người dùng trên bản đồ
- Không kích hoạt re-render toàn bộ bản đồ (chỉ update marker)

---

### **Giai Đoạn 3: Lưu Vào Buffer & Kiểm Tra Geofence**

#### 3.1 Buffer Location

**Trong MainViewModel:**
```csharp
private List<Location> _locationBuffer = new();
private DateTime _lastBatchTime = DateTime.UtcNow;

private void BufferLocation(Location location)
{
    _locationBuffer.Add(location);
    
    // Điều kiện flush buffer:
    // 1. Đủ 25 điểm
    // 2. Hoặc qua 30 giây
    if (_locationBuffer.Count >= 25 || 
        (DateTime.UtcNow - _lastBatchTime).TotalSeconds >= 30)
    {
        FlushBuffer();
    }
}
```

**Lợi ích của batch processing:**
- ✅ Giảm số lần gọi API (25 điểm/batch thay vì 1 điểm/lần)
- ✅ Tiết kiệm bandwidth và pin
- ✅ Giảm tải server
- ✅ Tối ưu throughput (2000 điểm/batch tối đa)

#### 3.2 Kiểm Tra Geofence

**Khi nào kiểm tra?**
- Mỗi khi nhận LocationUpdatedMessage
- Gọi `GeofenceService.CheckTriggeredAsync(location, [listPoiAroundUser])`

**Cơ chế kiểm tra:**
```csharp
public async Task<List<Poi>> CheckTriggeredAsync(Location location, List<Poi> poiList)
{
    var triggered = new List<Poi>();
    
    foreach (var poi in poiList)
    {
        // Tính khoảng cách Haversine
        double distance = Haversine(
            location.Latitude, location.Longitude,
            poi.Latitude, poi.Longitude
        );
        
        // Kiểm tra có vào vùng geofence không
        if (distance <= poi.GeofenceRadius)  // Mặc định 15m
        {
            triggered.Add(poi);
        }
    }
    
    return triggered;
}
```

**Công thức Haversine:**
```
a = sin²(Δφ/2) + cos(φ1) × cos(φ2) × sin²(Δλ/2)
c = 2 × atan2(√a, √(1−a))
d = R × c  (R = 6371 km)
```
- Tính khoảng cách chính xác giữa 2 điểm trên Trái Đất
- Tính toán nhanh, độ chính xác cao

**Cơ chế Hysteresis (chống nhiễu):**
- Vùng kích hoạt: 15m (bán kính geofence)
- Vùng thoát: 15m × 1.5 = 22.5m
- Lợi ích: Tránh "flicker" khi GPS jitter gần biên

**Double-hit (xác nhận 2 lần):**
- Yêu cầu phải vào geofence 2 lần liên tiếp mới kích hoạt
- Tránh false positive từ GPS noise

---

### **Giai Đoạn 4: Enqueue Narration & Phát Thuyết Minh**

#### 4.1 Khi geofence được trigger

```csharp
// Trong MainViewModel
var triggeredPois = await _geofenceService.CheckTriggeredAsync(location, nearbyPois);

foreach (var poi in triggeredPois)
{
    // Enqueue vào NarrationService
    await _narrationService.EnqueueAsync(poi);
}
```

#### 4.2 NarrationService xử lý queue

```csharp
public class NarrationService
{
    private Queue<Poi> _narrationQueue = new();
    
    public async Task EnqueueAsync(Poi poi)
    {
        _narrationQueue.Enqueue(poi);
        
        if (!_isPlaying)
        {
            await PlayNextAsync();
        }
    }
    
    private async Task PlayNextAsync()
    {
        if (_narrationQueue.Count == 0) return;
        
        _isPlaying = true;
        var poi = _narrationQueue.Dequeue();
        
        // Lấy audio từ cache hoặc download
        var audioPath = await GetAudioAsync(poi);
        
        // Phát audio
        await _mediaManager.PlayAsync(audioPath);
        
        // Ghi log lượt nghe
        await LogNarrationAsync(poi);
        
        _isPlaying = false;
        await PlayNextAsync();  // Phát tiếp
    }
}
```

---

### **Giai Đoạn 5: Gửi Batch Lên Server**

#### 5.1 Khi nào flush buffer?

```csharp
private async Task FlushBuffer()
{
    if (_locationBuffer.Count == 0) return;
    
    var batch = new MovementBatchDto
    {
        SessionId = _sessionService.SessionId,
        Movements = _locationBuffer.Select(loc => new MovementDto
        {
            Latitude = loc.Latitude,
            Longitude = loc.Longitude,
            Timestamp = DateTime.UtcNow,
            Accuracy = loc.Accuracy
        }).ToList()
    };
    
    try
    {
        await _apiClient.PostAsync("/api/movement/batch", batch);
        _locationBuffer.Clear();
        _lastBatchTime = DateTime.UtcNow;
    }
    catch (Exception ex)
    {
        // Retry logic với exponential backoff
        await RetryWithBackoffAsync(() => 
            _apiClient.PostAsync("/api/movement/batch", batch)
        );
    }
}
```

#### 5.2 API Endpoint: POST /api/movement/batch

**Request:**
```json
{
  "sessionId": "550e8400-e29b-41d4-a716-446655440000",
  "movements": [
    {
      "latitude": 10.7769,
      "longitude": 106.7009,
      "timestamp": "2025-05-10T10:30:00Z",
      "accuracy": 5.2
    },
    {
      "latitude": 10.7770,
      "longitude": 106.7010,
      "timestamp": "2025-05-10T10:30:03Z",
      "accuracy": 4.8
    }
    // ... tối đa 2000 điểm
  ]
}
```

**Server xử lý:**
```csharp
[HttpPost("batch")]
[AllowAnonymous]  // Public endpoint
public async Task<IActionResult> BatchLog([FromBody] MovementBatchDto dto)
{
    // Validate
    if (dto.Movements.Count > 2000)
        return BadRequest("Tối đa 2000 điểm/batch");
    
    // Lưu vào database
    var movements = dto.Movements.Select(m => new Movement
    {
        SessionId = dto.SessionId,
        Latitude = m.Latitude,
        Longitude = m.Longitude,
        Timestamp = m.Timestamp,
        Accuracy = m.Accuracy
    }).ToList();
    
    _db.Movements.AddRange(movements);
    await _db.SaveChangesAsync();  // 1 transaction duy nhất
    
    return Ok();
}
```

**Lợi ích:**
- ✅ 1 transaction duy nhất cho 25-2000 điểm
- ✅ Giảm I/O overhead
- ✅ Tối ưu throughput
- ✅ Dễ rollback nếu lỗi

---

### **Giai Đoạn 6: Tính Quãng Đường (Distance Calculation)**

#### 6.1 SessionTrackingService lắng nghe LocationUpdatedMessage

```csharp
public class SessionTrackingService
{
    private Location _lastLocation;
    private double _totalDistance = 0;
    
    public SessionTrackingService()
    {
        WeakReferenceMessenger.Default.Register<LocationUpdatedMessage>(this,
            (recipient, message) =>
            {
                CalculateDistance(message.Location);
            });
    }
    
    private void CalculateDistance(Location currentLocation)
    {
        if (_lastLocation == null)
        {
            _lastLocation = currentLocation;
            return;
        }
        
        // Tính Haversine
        double distance = Haversine(
            _lastLocation.Latitude, _lastLocation.Longitude,
            currentLocation.Latitude, currentLocation.Longitude
        );
        
        // Bỏ qua GPS jitter < 2m
        if (distance < 2)
        {
            _lastLocation = currentLocation;
            return;
        }
        
        // Bỏ qua nhảy bất thường > 500m
        if (distance > 500)
        {
            _lastLocation = currentLocation;
            return;
        }
        
        // Cộng vào tổng quãng đường
        _totalDistance += distance;
        _lastLocation = currentLocation;
    }
    
    public double GetTotalDistance() => _totalDistance;
}
```

#### 6.2 Gửi heartbeat mỗi 60 giây

```csharp
private async Task SendHeartbeatAsync()
{
    var heartbeat = new SessionHeartbeatDto
    {
        SessionId = _sessionService.SessionId,
        PoisVisited = _mainViewModel.PoisVisited,
        DistanceTraveled = _sessionTrackingService.GetTotalDistance(),
        Timestamp = DateTime.UtcNow
    };
    
    await _apiClient.PostAsync("/api/session/heartbeat", heartbeat);
}
```

**Server xử lý:**
```csharp
[HttpPost("heartbeat")]
[AllowAnonymous]
public async Task<IActionResult> Heartbeat([FromBody] SessionHeartbeatDto dto)
{
    var session = await _db.DeviceSessions
        .FirstOrDefaultAsync(s => s.SessionId == dto.SessionId);
    
    if (session != null)
    {
        // Lấy giá trị lớn hơn (Max) để tránh giảm số liệu
        session.PoisVisited = Math.Max(session.PoisVisited, dto.PoisVisited);
        session.DistanceTraveled = Math.Max(session.DistanceTraveled, dto.DistanceTraveled);
        session.LastHeartbeat = dto.Timestamp;
        
        await _db.SaveChangesAsync();
    }
    
    return Ok();
}
```

---

### **Giai Đoạn 7: Kết Thúc Phiên**

#### 7.1 Khi nào kết thúc?
- Du khách thoát app
- App bị kill
- Người dùng tắt tính năng theo dõi

#### 7.2 Gửi EndSession

```csharp
public async Task EndSessionAsync()
{
    // Flush buffer cuối cùng
    await FlushBuffer();
    
    // Gửi end session
    var endDto = new SessionEndDto
    {
        SessionId = _sessionService.SessionId,
        EndedAt = DateTime.UtcNow,
        FinalPoisVisited = _mainViewModel.PoisVisited,
        FinalDistanceTraveled = _sessionTrackingService.GetTotalDistance()
    };
    
    await _apiClient.PostAsync("/api/session/end", endDto);
    
    // Dừng GPS service
    await _gpsService.StopTrackingAsync();
}
```

**Server xử lý:**
```csharp
[HttpPost("end")]
[AllowAnonymous]
public async Task<IActionResult> EndSession([FromBody] SessionEndDto dto)
{
    var session = await _db.DeviceSessions
        .FirstOrDefaultAsync(s => s.SessionId == dto.SessionId);
    
    if (session != null)
    {
        session.EndedAt = dto.EndedAt;
        session.PoisVisited = dto.FinalPoisVisited;
        session.DistanceTraveled = dto.FinalDistanceTraveled;
        session.DurationMinutes = (int)(session.EndedAt - session.StartedAt).Value.TotalMinutes;
        
        await _db.SaveChangesAsync();
    }
    
    return Ok();
}
```

---

## 📊 Sơ Đồ Luồng Hoàn Chỉnh

```
┌─────────────────────────────────────────────────────────────────┐
│ 1. MỞ APP                                                       │
│    MainViewModel.OnAppearing()                                  │
│    → GpsService.StartTrackingAsync()                            │
└────────────────────┬────────────────────────────────────────────┘
                     │
        ┌────────────┴────────────┐
        │                         │
    ┌───▼────────┐        ┌──────▼──────┐
    │ Mock Mode  │        │ Real GPS    │
    │ 1.5s/point │        │ 3s/point    │
    └───┬────────┘        └──────┬──────┘
        │                        │
        └────────────┬───────────┘
                     │
        ┌────────────▼────────────┐
        │ 2. PUBLISH LOCATION     │
        │ WeakReferenceMessenger  │
        │ LocationUpdatedMessage  │
        └────────────┬────────────┘
                     │
        ┌────────────┴────────────────────────┐
        │                                     │
    ┌───▼──────────────┐    ┌────────────────▼────────┐
    │ MainViewModel    │    │ SessionTrackingService  │
    │ - Buffer (25pt)  │    │ - Calculate Distance    │
    │ - Check Geofence │    │ - Haversine Formula     │
    │ - Enqueue Audio  │    │ - Ignore jitter < 2m    │
    └───┬──────────────┘    └────────────────┬────────┘
        │                                    │
        │ Flush when:                        │ Every 60s:
        │ - 25 points OR                     │ - Send Heartbeat
        │ - 30 seconds                       │ - Update POIs/Distance
        │                                    │
        └────────────┬────────────────────────┘
                     │
        ┌────────────▼────────────────────┐
        │ 3. SEND BATCH TO SERVER         │
        │ POST /api/movement/batch        │
        │ (max 2000 points/batch)         │
        └────────────┬────────────────────┘
                     │
        ┌────────────▼────────────────────┐
        │ 4. SERVER SAVES TO DATABASE     │
        │ Movement table                  │
        │ (1 transaction)                 │
        └────────────┬────────────────────┘
                     │
        ┌────────────▼────────────────────┐
        │ 5. ANALYTICS & HEATMAP          │
        │ - Track visitor behavior        │
        │ - Generate heatmap              │
        │ - Session statistics            │
        └────────────┬────────────────────┘
                     │
        ┌────────────▼────────────────────┐
        │ 6. CLOSE APP                    │
        │ EndSessionAsync()               │
        │ - Flush final buffer            │
        │ - Send /api/session/end         │
        │ - Stop GPS service              │
        └────────────────────────────────┘
```

---

## 🔧 Retry Logic & Error Handling

### Exponential Backoff Strategy

```csharp
private async Task RetryWithBackoffAsync(Func<Task> operation)
{
    int[] backoffSeconds = { 10, 30, 60, 300, 900 };  // 10s, 30s, 1m, 5m, 15m
    
    for (int attempt = 0; attempt < backoffSeconds.Length; attempt++)
    {
        try
        {
            await operation();
            return;  // Success
        }
        catch (Exception ex)
        {
            if (attempt < backoffSeconds.Length - 1)
            {
                await Task.Delay(backoffSeconds[attempt] * 1000);
            }
            else
            {
                // Final attempt failed, log and give up
                Debug.WriteLine($"Retry failed after {attempt + 1} attempts: {ex.Message}");
            }
        }
    }
}
```

### Offline-First Pattern

```csharp
// Nếu không có kết nối, lưu vào OutboxService
if (!_connectivity.IsConnected)
{
    await _outboxService.SaveAsync(new OutboxMessage
    {
        Type = "MovementBatch",
        Payload = JsonSerializer.Serialize(batch),
        CreatedAt = DateTime.UtcNow
    });
}
else
{
    // Gửi ngay
    await _apiClient.PostAsync("/api/movement/batch", batch);
}
```

---

## 📈 Performance Metrics

| Metric | Value | Lợi ích |
|--------|-------|---------|
| GPS Update Frequency | 3s (Android) / 1.5s (iOS) | Cân bằng accuracy vs battery |
| Buffer Size | 25 points | Giảm API calls 25x |
| Batch Timeout | 30 seconds | Đảm bảo data không bị delay quá lâu |
| Max Batch Size | 2000 points | Tối ưu throughput |
| Heartbeat Interval | 60 seconds | Theo dõi session mà không quá tải |
| Geofence Radius | 15m (default) | Cân bằng accuracy vs false positive |
| Hysteresis Ratio | 1.5× | Chống flicker |
| Distance Jitter Threshold | 2m | Bỏ qua GPS noise |
| Distance Jump Threshold | 500m | Bỏ qua teleport |

---

## 🎯 Tóm Tắt Luồng

1. **Khởi động:** App mở → GpsService.StartTrackingAsync() → Xin quyền → Khởi động Foreground Service
2. **Lấy dữ liệu:** GPS phát tọa độ mỗi 3s (Android) → WeakReferenceMessenger → LocationUpdatedMessage
3. **Xử lý:** MainViewModel buffer 25 điểm → Kiểm tra geofence → Enqueue narration
4. **Gửi:** Khi đủ 25 điểm hoặc 30s → POST /api/movement/batch → Server lưu database
5. **Theo dõi:** SessionTrackingService tính quãng đường → Gửi heartbeat mỗi 60s
6. **Kết thúc:** App đóng → Flush buffer cuối → POST /api/session/end → Dừng GPS

---

## 🔗 Liên Kết Tài Liệu

- **PRD Section 12.1:** GPS Service State Diagram
- **PRD Section 12.2:** POI Geofence State Diagram
- **PRD Section 02b:** Anonymous Visitor Flow
- **PRD Section 05:** MVP Features (Session Tracking)
- **PRD Section 13:** Class Diagrams (GpsService, SessionTrackingService)
- **PRD Section 14:** System Architecture
