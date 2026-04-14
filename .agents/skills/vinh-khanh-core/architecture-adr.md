# 🏛️ Architecture Decision Records (ADR) - Dự án Vĩnh Khánh

Tài liệu này ghi lại các quyết định kiến trúc quan trọng để đảm bảo tính nhất quán và hiệu quả trong phát triển dự án.

## ADR 01: Thuật toán Geofencing (Haversine Formula)

- **Bối cảnh**: Cần xác định khi nào người dùng đi vào "vùng ảnh hưởng" của một POI để phát thuyết minh.
- **Quyết định**: Sử dụng công thức **Haversine** để tính khoảng cách đường chim bay giữa 2 điểm tọa độ (Lat, Lon) trên Client.
- **Lý do**: 
    - Chính xác ở khoảng cách ngắn (duới 1km).
    - Hiệu năng cao, có thể chạy liên tục trong vòng lặp tracking mà không gây lag.
    - Hoạt động Offline hoàn toàn.

## ADR 02: Cơ chế dịch trung gian (Pivot Translation)

- **Bối cảnh**: Việc dịch trực tiếp từ Tiếng Việt sang các ngôn ngữ ít phổ biến (Thái, Khmer, Trung Quốc phồn thể) thường cho kết quả không tự nhiên.
- **Quyết định**: Áp dụng quy trình **Vietnamese -> English -> Target Language**.
- **Lý do**: 
    - Tận dụng khả năng xử lý ngôn ngữ Anh của các LLM (Llama 3.1, Gemini) vốn đã được huấn luyện tốt nhất.
    - Giảm thiểu sai sót ngữ pháp và ảo giác định dạng.
    - Dễ dàng kiểm soát chất lượng qua bản dịch tiếng Anh trung gian.

## ADR 03: Chiến lược lưu trữ Offline-First

- **Bối cảnh**: Phố ẩm thực có thể có những điểm sóng Wifi/4G yếu hoặc chập chờn.
- **Quyết định**: Sử dụng **SQLite** làm cơ sở dữ liệu cục bộ và cơ chế **Pre-download** file âm thanh.
- **Lý do**: 
    - Đảm bảo thuyết minh không bị ngắt quãng khi người dùng di chuyển giữa các vùng phủ sóng.
    - Giảm tải cho Server và tiết kiệm băng thông data di động cho người dùng.

## ADR 04: Quản lý Audio Queue (Task-based Parallelism)

- **Bối cảnh**: Có thể nhiều POI bị kích hoạt cùng lúc hoặc có thông báo hệ thống xen ngang.
- **Quyết định**: Sử dụng `Task Queue` với cơ chế ưu tiên (Priority).
- **Lý do**: 
    - Tránh việc 2 âm thanh phát đè lên nhau.
    - Cho phép âm thanh quan trọng (cảnh báo, chỉ dẫn) được phát ngay lập tức.
