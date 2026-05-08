# BÁO CÁO TOÀN DIỆN — PRODUCTION READINESS
Sau khi rà soát từng file, từng dòng code, đây là đánh giá đầy đủ theo 5 mục bạn yêu cầu: 

## TỔNG QUAN TRẠNG THÁI
| Mục | Trạng thái | Mức độ |
|---|---|---|
| 1. QR tải app + anonymous flow | Có nhưng thiếu sót | Cần fix |
| 2. Monitoring | Có cơ bản, thiếu nhiều | Cần bổ sung |
| 3. Xử lý trùng (overlap) | Có nhưng có bug | Cần fix |
| 4. Xử lý hàng đợi | Có nhưng có race condition | Cần fix |
| 5. Performance | Nhiều điểm yếu nghiêm trọng | Cần fix gấp |

## VẤN ĐỀ 1: QR TẢI APP + ANONYMOUS FLOW
**Những gì đã có:**
* AppDownloadController có endpoint /api/app/download và /api/app/qr
* DownloadPage.tsx có QR image tĩnh + link Google Drive
* Anonymous session flow hoạt động đúng (GUID không cần account)

**Vấn đề phát hiện:**
* QR image trong DownloadPage là file tĩnh (qr-download.png) — nếu URL thay đổi, QR sẽ sai. Cần dùng QR động từ /api/app/qr
* appsettings.json có DynamicApkUrl rỗng — fallback tìm file APK theo đường dẫn hardcode sẽ fail trên production
* iOS chỉ có "Sắp có" — không có TestFlight link thực
* Program.cs có app.UseDeveloperExceptionPage() luôn bật — kể cả production, lộ stack trace
* JWT key hardcode trong appsettings.json: "VinhKhanh_Super_Secret_Key_256bit_2026!" — phải dùng environment variable 

## VẤN ĐỀ 2: MONITORING
**Những gì đã có:**
* Dashboard: totalPois, totalTours, totalDevices, totalVisits, activeUsers (SignalR count)
* SessionsPage: platform breakdown, top devices, peak hours, session list
* AnalyticsPage: top POI chart, visitor heatmap stats
* HeatmapPage: Leaflet heatmap với live mode

**Vấn đề phát hiện:**
* activeUsers đếm SignalR connections (web admin), KHÔNG phải mobile app users — khách dùng app không kết nối SignalR, nên con số này luôn = 0 hoặc chỉ đếm admin đang mở web
* Không có real-time active mobile users — không có cách nào biết bao nhiêu khách đang cầm điện thoại đi trong phố
* admin_summary_stats cache 5 phút nhưng totalDevices tính sai: lấy max của AppHistoryLogs và PoiVisitLogs — khách mới chưa nghe gì sẽ không được đếm
* HistoryPage chỉ load 100 records, không có filter/search/export
* SessionsPage không có real-time refresh — phải reload tay
* Không có thống kê theo ngày/tuần/tháng (time-series chart)
* Không có alert/notification khi có bất thường (quá nhiều lỗi, server down...)
* GetPoiHeatmapStats tính toán trong memory — khi có 10,000+ movement logs sẽ rất chậm 

## VẤN ĐỀ 3: XỬ LÝ TRÙNG (OVERLAP)
**Những gì đã có:**
* GeofenceService.CheckTriggeredAsync(): priority-based + distance tiebreaker
* GlobalCooldownSeconds = 10 giữa các trigger
* Hysteresis (exit radius = 1.2x entry radius)
* Double-hit confirmation (2 consecutive GPS readings)

**Test cases và vấn đề phát hiện:**
* TC-01: Khách đứng đúng trung tâm 2 POI cùng bán kính Xử lý được: chọn POI priority cao hơn Nếu priority bằng nhau: chọn POI gần hơn
* TC-02: Khách đứng trong vùng giao nhau của 3+ POI Xử lý được: vẫn chọn 1 POI tốt nhất
* TC-03: Khách đi qua biên của 2 POI liên tiếp (jitter) Hysteresis xử lý được
* TC-04: Khách đứng yên trong 1 POI, POI mới có priority cao hơn xuất hiện (admin thêm POI) BUG: _insidePoiIds không được reset khi POI list thay đổi. Nếu admin thêm POI mới có priority cao hơn ngay cạnh khách, khách sẽ không nhận được trigger vì GlobalCooldown đang chạy
* TC-05: Khách đứng trong POI, app bị kill và mở lại BUG: _insidePoiIds là in-memory, không persist. Khi app restart, _insidePoiIds rỗng → khách sẽ bị trigger lại ngay lập tức dù vừa nghe xong. GeofenceCooldownStore persist cooldown nhưng _insidePoiIds thì không
* TC-06: GPS accuracy kém (accuracy > 50m), khách thực ra không trong POI THIẾU: Không có filter theo GPS accuracy. Nếu GPS báo accuracy = 100m, vẫn trigger POI
* TC-07: Khách đứng ở biên của POI A và POI B, GPS jitter liên tục vào/ra Double-hit + Hysteresis xử lý được
* TC-08: 2 POI cùng tọa độ (admin nhập sai) BUG: Cả 2 đều pass distance check, chọn theo priority. Nếu priority bằng nhau, dist < bestDistance sẽ không phân biệt được (dist ≈ 0 cho cả 2) → chọn POI đầu tiên trong list (không deterministic)
* TC-09: POI bị deactivate trong khi khách đang nghe Xử lý được: HasQueryFilter(p => p.IsActive) lọc ra
* TC-10: Khách đứng trong POI, cooldown chưa hết, POI khác priority cao hơn trigger BUG: GlobalCooldownSeconds = 10 block tất cả POI. Nếu POI A vừa trigger (priority 5), POI B priority 10 xuất hiện trong 10 giây → bị block. Đây là behavior sai — POI priority cao hơn nên được phép interrupt 

## VẤN ĐỀ 4: XỬ LÝ HÀNG ĐỢI
**Những gì đã có:**
* NarrationService: priority queue, duplicate window 25s, interrupt on higher priority
* ProcessQueueAsync(): sequential processing, 3s gap between items
* SemaphoreSlim _gate để tránh concurrent playback

**Test cases và vấn đề phát hiện:**
* TC-01: 5 khách cùng đứng trước 1 quán, cùng nghe 1 audio Mỗi device độc lập, không ảnh hưởng nhau (client-side queue)
* TC-02: Khách đi nhanh qua 3 quán liên tiếp, queue tích lũy Queue sort theo priority, phát lần lượt
* TC-03: Khách enqueue cùng 1 POI 2 lần trong 25 giây _queuedKeys + _recentlyPlayed chặn duplicate
* TC-04: Audio file không tồn tại, fallback TTS Fallback chain hoạt động
* TC-05: Khách cancel audio giữa chừng (bấm nút stop) StopCurrentAsync() → StopAsync() → _playCts.Cancel()
* TC-06: Race condition — 2 GPS updates đến cùng lúc, cả 2 trigger cùng 1 POI BUG: EnqueueAsync không có lock. _queuedKeys.Contains(key) check và _queuedKeys.Add(key) không atomic. Trên mobile, GPS callback có thể fire từ background thread → race condition
* TC-07: Queue rất dài (10+ items), khách muốn nghe POI mới nhất THIẾU: Không có max queue size. Nếu khách đi qua 20 quán nhanh, queue tích lũy 20 items, phải chờ rất lâu
* TC-08: App bị suspend (iOS background), audio đang phát THIẾU: Không có background audio session setup cho iOS. Audio sẽ bị dừng khi app vào background
* TC-09: Nhiều khách gửi analytics log cùng lúc (100 requests/giây) THIẾU: Không có rate limiting trên API. POST /api/analytics/log không có throttle → có thể bị DDoS hoặc spam
* TC-10: OutboxService flush khi offline → online Exponential backoff, max 5 retries, 7-day stale threshold 

## VẤN ĐỀ 5: PERFORMANCE
**Vấn đề phát hiện:**
* ApiClientService tạo new HttpClient() mỗi lần — CreateClient() set BaseAddress trên shared _http instance nhưng using var http = CreateClient() trong mỗi method tạo ra vấn đề: _http là field nhưng bị using dispose → socket exhaustion
* GetPoiHeatmapStats load toàn bộ movement logs vào memory rồi tính toán bằng LINQ — với 100,000 records sẽ timeout
* admin_summary_stats cache 5 phút nhưng activeUsers lấy fresh mỗi lần → inconsistent
* MovementController.GetHeatmap không có cache — mỗi request query toàn bộ DB
* AnalyticsController.GetHeatmap cache 30s nhưng không invalidate khi có data mới
* NarrationService._httpClient là static — tốt, nhưng không có retry policy
* AudioCacheService không có cache eviction — cache sẽ phình to mãi, chiếm storage
* GeofenceService không có index — pois.Where(...) scan toàn bộ list mỗi GPS update (1.5s interval)
* Program.cs dùng EnsureCreated() cho SQLite/SQL Server thay vì migrations → không thể upgrade schema
* Không có connection pooling config cho PostgreSQL/SQL Server
* appsettings.Production.json có CHANGE_ME password — chưa được cấu hình thực
* app.UseDeveloperExceptionPage() không có điều kiện — luôn bật kể cả production