# 🎯 VINH KHANH PROJECT SKILLS

Bộ kỹ năng này định nghĩa các nguyên tắc và tiêu chuẩn để Antigravity hỗ trợ phát triển hệ thống Thuyết minh tự động đa ngôn ngữ cho Phố ẩm thực Vĩnh Khánh.

## 📍 1. GPS & Geofencing Core (Vibe Code)
- **Kiến trúc**: Luôn bám sát mô hình `Location Service` -> `Geofence Engine` -> `Narration Manager`.
- **Haversine Algorithm**: Sử dụng để tính toán khoảng cách giữa người dùng và POI (Point of Interest) ở phía Client (MAUI).
- **Phân tách Foreground/Background**: 
    - Foreground: Cập nhật UI bản đồ mượt mà (High accuracy).
    - Background: Sử dụng `Foreground Service` (Android) và `Significant Location Change` (iOS) để duy trì Geofencing khi khóa màn hình.
- **Debounce & Cooldown**: Luôn triển khai cơ chế chống spam bằng cách thiết lập thời gian nghỉ (Cooldown) cho từng POI sau khi đã phát thuyết minh.

## 🎙️ 2. Narration Engine & Audio Scheduling
- **Quản lý hàng chờ (Queue)**: Triển khai hàng chờ đa tiến trình (Task-based) để xử lý việc phát audio không bị chồng chéo.
- **Ưu tiên (Priority)**: Các thông báo khẩn cấp hoặc điều hướng App có ưu tiên cao hơn âm thanh thuyết minh du lịch.
- **Audio Strategies**: 
    - **Offline-First**: Ưu tiên đọc từ bộ nhớ SQLite và file audio đã tải trước.
    - **TTS Resilience**: Tự động fallback giữa Local TTS và Cloud (Azure/Ollama) dựa trên trạng thái kết nối mạng (API-and-Interface-Design).

## 🌍 3. AI Assistant & Pivot Translation
- **Pivot Translation Mastery**: Luôn thực hiện quy trình `Vi -> En -> Target` để đảm bảo độ chính xác ngữ nghĩa cho các ngôn ngữ hiếm (Trung, Nhật, Hàn, Thái...).
- **AI Persona**: AI phải có khả năng chuyển đổi giữa 2 vai trò:
    - `Tourist Guide`: Giọng điệu hào hứng, cung cấp thông tin văn hóa ẩm thực.
    - `Public Servant`: Trang trọng, chính xác trong việc điều hướng phòng chức năng dân cư.
- **Source-Driven Development**: Luôn đối soát các bản dịch với dữ liệu gốc của POI để tránh AI bị ảo giác (hallucination).

## 🛡️ 4. Security, Hardening & Analytics
- **Access Control**: Tuân thủ nghiêm ngặt phân quyền:
    - `Admin`: Quản trị toàn sàn.
    - `Owner`: Chỉ quản lý POI mình sở hữu (Security-and-Hardening).
- **Anonymized Tracking**: Tuyến đường di chuyển của khách hàng khi gửi về Server để tạo Heatmap phải được ẩn danh hoàn toàn.

## 🎨 5. UI/UX & Native Engineering
- **Modern Aesthetics**: Sử dụng Vanilla CSS/XAML với các hiệu ứng kính mờ (Glassmorphism), typography hiện đại từ Google Fonts.
- **Frontend-UI-Engineering**: 
    - App Mobile: Thiết kế "Hands-free" (rảnh tay), các nút điều khiển to rõ.
    - Web Admin: Trình duyệt bản đồ trực quan để kéo thả POI.
- **Performance**: Tối ưu hóa việc render bản đồ và quản lý bộ nhớ khi chơi audio dung lượng cao.

## 🛠️ 6. Debugging & Error Recovery
- **Error Boundaries**: Luôn có cơ chế phục hồi khi GPS bị mất tín hiệu đột ngột (như khi đi vào hầm hoặc nhà cao tầng).
- **Incremental Implementation**: Ưu tiên hoàn thiện phần PoC (GPS, Geofence cơ bản) trước khi mở rộng lên MVP (CMS, Analytics).
