# Hướng Dẫn Kiểm Thử Bằng Máy Ảo (Emulator) và Máy Thật (Physical Device)

Dưới đây là hướng dẫn chi tiết cách thiết lập, cấu hình và khắc phục lỗi khi chạy ứng dụng .NET MAUI của dự án Vinh Khanh trên máy ảo và máy điện thoại thật.

---

## 1. Kiểm Thử Bằng Máy Ảo (Android Emulator)

Máy ảo rất tiện lợi để test giao diện và các luồng đi cơ bản (Navigation, Data loading) mà không cần chuẩn bị cáp nối.

### Các Bước Thực Hiện
1. **Khởi động API Server:** Mở terminal, chạy `dotnet run` ở thư mục `VinhKhanh.API` để server backend hoạt động ở port `5283`.
2. **Chọn Emulator:** Trong thanh công cụ của Visual Studio ở góc trên cùng, đảm bảo mục thiết bị đang chọn là một **Android Emulator** (Ví dụ: *Pixel 5 - API 34*).
3. **Chạy Ứng Dụng:** Nhấn nút mũi tên xanh (hoặc phím `F5`) để build và deploy app lên máy ảo.

### Lưu Ý & Khắc Phục Lỗi (Máy Ảo)
- **Lỗi mạng (Connection Timeout):** Máy ảo Android không hiểu `localhost` là máy tính của bạn (nó tự hiểu `localhost` là chính cái điện thoại ảo đó). Để gọi API thành công trên máy ảo, hệ thống đã được cấu hình tự động trỏ về `10.0.2.2:5283`. Bạn không cần sửa code IP.
- **Test GPS (Định vị):** Máy ảo không có GPS thực. Để test tính năng "Nghe Thuyết Minh Tự Động", bạn nhấn vào biểu tượng dấu **3 chấm (...)** trên thanh công cụ phụ bên cạnh máy ảo -> Chọn mục **Location**.
  - Nhập tọa độ vĩ độ (Latitude) và kinh độ (Longitude) của một quán ăn trong database.
  - Nhấn **Set Location**. Hệ thống GPS giả lập sẽ báo vị trí này về App, và tính năng Geofence sẽ được kích hoạt y như thật.

---

## 2. Kiểm Thử Bằng Máy Thật (Physical Phone)

Kiểm thử bằng máy thật là bắt buộc khi bạn muốn test thực tế hiệu năng âm thanh, GPS đi lại ngoài trời, Scan QR bằng camera, hoặc ứng dụng chạy ngầm (Background Service).

### Giai Đoạn 1: Chuẩn Bị Thiết Bị
1. Cắm cáp USB kết nối điện thoại Android vào máy tính tính.
2. Trên điện thoại, vào **Cài đặt (Settings)** -> **Giới thiệu điện thoại (About Phone)**. 
3. Nhấn 7 lần liên tục vào **Số bản dựng (Build Number)** để bật chế độ nhà phát triển.
4. Quay lại màn hình Cài đặt, vào **Tùy chọn nhà phát triển (Developer Options)**.
5. Bật tính năng **Gỡ lỗi USB (USB Debugging)**. Lúc này, điện thoại sẽ hiện bảng hỏi quyền "Allow USB Debugging?", bạn chọn **Allow** (hoặc OK).

> [!TIP]
> Bạn có thể mở Terminal và gõ lệnh `adb devices`. Nếu thấy thiết bị của bạn hiện lên với chữ `device` (không phải `unauthorized`), nghĩa là kết nối đã thành công.

### Giai Đoạn 2: Đồng Bộ Mạng (Cực Kỳ Quan Trọng)
Điện thoại và máy tính **bắt buộc phải kết nối chung một mạng Wi-Fi**. Máy thật sẽ không thể dùng `localhost` hay `10.0.2.2` để kết nối vào máy tính được.

1. Lấy địa chỉ IP Wi-Fi của máy tính: 
   - Mở Terminal/Command Prompt gõ lệnh: `ipconfig` (trên Windows) hoặc `ifconfig` (trên Mac).
   - Tìm mục **IPv4 Address** (Ví dụ: `192.168.1.5` hoặc `172.16.0.152`).
2. Sửa file `ApiClientService.cs` trong `VinhKhanh.App/Services/`:
   ```csharp
   // Tìm dòng code số 39 và cập nhật lại bằng IP MỚI NHẤT của máy tính bạn:
   return Microsoft.Maui.Storage.Preferences.Get(AppPreferences.ApiBaseUrl, "http://172.16.0.152:5283/");
   ```
3. **Mở Tường Lửa (Firewall):** Máy tính mặc định sẽ chặn các thiết bị khác truy cập vào port 5283. Bạn cần mở CMD dưới quyền Admin và chạy lệnh sau để mở Port:
   ```cmd
   netsh advfirewall firewall add rule name="Open Port 5283" dir=in action=allow protocol=TCP localport=5283
   ```

### Giai Đoạn 3: Khởi Chạy
1. Đảm bảo Backend API Server (port `5283`) vẫn đang chạy bình thường qua lệnh `dotnet run`.
2. Trên thanh công cụ Visual Studio, click vào biểu tượng thiết bị, chọn thiết bị thật của bạn (Nó thường có tên như *Samsung SM-G998B* hoặc *Android Local Device*).
3. Bấm **F5** để cài đặt ứng dụng vào máy thật.

### Lưu Ý & Khắc Phục Lỗi (Máy Thật)
- **Lỗi trắng màn hình / Đứng hình khi load danh sách Quán ăn:** 99% là do điện thoại không thể kết nối tới IP máy tính. Bạn hãy kiểm tra lại kết nối chung Wi-Fi và tắt thử Tường lửa Windows (Windows Defender Firewall) trong chốc lát để test.
- **Nếu đang code mà IP Wi-Fi bị nhảy:** Đôi khi bạn cắm lại mạng hoặc đổi quán Cafe, IP của máy tính sẽ bị thay đổi. Đừng quên mở file `ApiClientService.cs` ra cập nhật lại IP mới!
- Nếu khi Build báo lỗi liên quan tới `System.IO.IOException: The process cannot access the file ...` là do hệ điều hành đang khoá file thừa từ lần chạy trước. Chỉ cần tắt ứng dụng trên điện thoại, Clean Solution và bấm F5 lại là được.

https://vinh-khanh-food-street-gvhceeg4gbakhjgc.eastasia-01.azurewebsites.net/