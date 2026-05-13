import sys

filename = 'VinhKhanh-PRD (1).html'
with open(filename, 'r', encoding='utf-8') as f:
    content = f.read()

target_start_str = "    <div class=\"card\">\n      <h3>📊 20.1 Dashboard — Các Thẻ Thống Kê (Overview Cards) & Thiết Bị Trực Tuyến</h3>"
target_end_str = "      </div>\n    </div>\n\n    <div class=\"card\">\n      <h3>📈 20.2 Dashboard — Biểu Đồ Top Địa Điểm Lắng Nghe</h3>"

start_idx = content.find(target_start_str)
if start_idx == -1:
    print("Could not find start index")
    sys.exit(1)

end_idx = content.find(target_end_str, start_idx)
if end_idx == -1:
    print("Could not find end index")
    sys.exit(1)

# The content to replace is from start_idx up to end_idx (not including end_idx, since end_idx is the start of 20.2)

new_cards = """    <div class="card">
      <h3>📊 20.1.1 Ô Hiển Thị: Tổng Số Quán Ăn (Total POIs)</h3>
      <div class="dw">
        <div class="dt">Sequence Diagram — Fallback Logic cho Số Quán Ăn</div>
        <div class="mermaid">
          sequenceDiagram
          autonumber
          participant UI as 💻 Dashboard
          participant ADM as 🌐 AdminController
          participant POI as 🌐 PoiController
          participant DB as 🗄️ ApplicationDbContext

          par Fetch Chính
              UI->>ADM: GET /api/admin/summary
              activate ADM
              ADM->>DB: db.Pois.Count()
              DB-->>ADM: totalPois
              ADM-->>UI: return { totalPois }
              deactivate ADM
          and Fetch Dự Phòng (Fallback)
              UI->>POI: GET /api/poi?lang=vi
              activate POI
              POI->>DB: Truy vấn danh sách POI
              DB-->>POI: List[Poi]
              POI-->>UI: return List[Poi]
              deactivate POI
          end
          
          alt API summary lỗi hoặc đang tải
              UI->>UI: value = pois.data.length
          else API summary thành công
              UI->>UI: value = summary.data.totalPois
          end
          UI-->>UI: Render thẻ màu Cam (Quán ăn)
        </div>
      </div>
    </div>

    <div class="card">
      <h3>📊 20.1.2 Ô Hiển Thị: Tổng Số Lộ Trình (Total Tours)</h3>
      <div class="dw">
        <div class="dt">Sequence Diagram — Fallback Logic cho Số Lộ Trình</div>
        <div class="mermaid">
          sequenceDiagram
          autonumber
          participant UI as 💻 Dashboard
          participant ADM as 🌐 AdminController
          participant TOUR as 🌐 TourController
          participant DB as 🗄️ ApplicationDbContext

          par Fetch Chính
              UI->>ADM: GET /api/admin/summary
              activate ADM
              ADM->>DB: db.Tours.Count()
              DB-->>ADM: totalTours
              ADM-->>UI: return { totalTours }
              deactivate ADM
          and Fetch Dự Phòng (Fallback)
              UI->>TOUR: GET /api/tour?lang=vi
              activate TOUR
              TOUR->>DB: Truy vấn danh sách Lộ trình
              DB-->>TOUR: List[Tour]
              TOUR-->>UI: return List[Tour]
              deactivate TOUR
          end
          
          alt API summary lỗi hoặc đang tải
              UI->>UI: value = tours.data.length
          else API summary thành công
              UI->>UI: value = summary.data.totalTours
          end
          UI-->>UI: Render thẻ màu Xanh (Lộ trình)
        </div>
      </div>
    </div>

    <div class="card">
      <h3>📊 20.1.3 Ô Hiển Thị: Số Lượt Nghe TTS (Total Visits)</h3>
      <div class="dw">
        <div class="dt">Sequence Diagram — Fallback Logic cho Lượt Nghe</div>
        <div class="mermaid">
          sequenceDiagram
          autonumber
          participant UI as 💻 Dashboard
          participant ADM as 🌐 AdminController
          participant ANY as 🌐 AnalyticsController
          participant DB as 🗄️ ApplicationDbContext

          par Fetch Chính
              UI->>ADM: GET /api/admin/summary
              activate ADM
              ADM->>DB: db.PoiVisitLogs.Count()
              DB-->>ADM: totalVisits
              ADM-->>UI: return { totalVisits }
              deactivate ADM
          and Fetch Dự Phòng (Fallback)
              UI->>ANY: GET /api/analytics/top?days=30
              activate ANY
              ANY->>DB: GroupBy PoiId -> Lấy Count
              DB-->>ANY: Top POI Data
              ANY-->>UI: return [{count: 50}, {count: 20}]
              deactivate ANY
          end
          
          alt API summary lỗi hoặc đang tải
              UI->>UI: value = Sum(analyticsTop.data.count)
          else API summary thành công
              UI->>UI: value = summary.data.totalVisits
          end
          UI-->>UI: Render thẻ màu Đỏ (Lượt nghe TTS)
        </div>
      </div>
    </div>

    <div class="card">
      <h3>📊 20.1.4 Ô Hiển Thị: Số Thiết Bị Cài Đặt (Total Installs)</h3>
      <div class="dw">
        <div class="dt">Sequence Diagram — Thống kê thiết bị duy nhất</div>
        <div class="mermaid">
          sequenceDiagram
          autonumber
          participant UI as 💻 Dashboard
          participant ADM as 🌐 AdminController
          participant DB as 🗄️ ApplicationDbContext

          UI->>ADM: GET /api/admin/summary
          activate ADM
          ADM->>DB: db.DeviceSessions.Select(DeviceId).Distinct().Count()
          DB-->>ADM: totalDevices
          ADM-->>UI: return { totalDevices }
          deactivate ADM
          UI->>UI: value = summary.data.totalDevices
          UI-->>UI: Render thẻ màu Xanh Teal (Thiết bị đã cài)
        </div>
      </div>
    </div>

    <div class="card">
      <h3>🟢 20.1.5 Ô Hiển Thị: Thiết Bị Trực Tuyến (Active Users)</h3>
      <div class="dw">
        <div class="dt">Sequence Diagram — Realtime Tracking Polling</div>
        <div class="mermaid">
          sequenceDiagram
          autonumber
          participant UI as 💻 Dashboard
          participant ADM as 🌐 AdminController
          participant TRK as ⚙️ ConnectionTracker

          loop RefetchInterval: Mỗi 10 giây
              UI->>ADM: GET /api/admin/summary
              activate ADM
              ADM->>TRK: GetOnlineCount() (Đếm ConnectionID trong RAM)
              TRK-->>ADM: activeUsers
              ADM-->>UI: return { activeUsers }
              deactivate ADM
              UI->>UI: value = summary.data.activeUsers
              UI-->>UI: Render thẻ màu Xanh Lá có chấm nhấp nháy (Online)
          end
        </div>
      </div>
    </div>\n\n"""

new_content = content[:start_idx] + new_cards + content[end_idx:]

with open(filename, 'w', encoding='utf-8') as f:
    f.write(new_content)

print("Successfully replaced section 20.1 with 5 separate sequence diagrams.")
