import sys

filename = 'VinhKhanh-PRD (1).html'
with open(filename, 'r', encoding='utf-8') as f:
    content = f.read()

target = """
    <div class="card">
      <h3>🗑️ 20.8 Quản Lý Lịch Sử Truy Cập (SessionsPage Delete)</h3>
      <div class="dw">
        <div class="dt">Sequence Diagram — AnalyticsController Delete Log</div>
        <div class="mermaid">
          sequenceDiagram
          autonumber
          participant Admin as 👨‍💼 Quản Trị Viên
          participant UI as 💻 SessionsPage
          participant API as 🌐 AnalyticsController
          participant DB as 🗄️ ApplicationDbContext
          participant Cache as 🧠 MemoryCache

          Admin->>UI: Bấm Xóa (Delete) trên 1 dòng lịch sử truy cập
          UI->>UI: window.confirm("Bạn có chắc muốn xóa?")
          UI->>API: DELETE /api/analytics/log/{id} (Kèm JWT)
          activate API
          API->>DB: db.PoiVisitLogs.FindAsync(id)
          DB-->>API: entity
          alt Nếu tìm thấy (entity != null)
            API->>DB: db.PoiVisitLogs.Remove(entity)
            API->>DB: await db.SaveChangesAsync()
            API->>Cache: _cache.Remove("admin_summary_stats") (Xóa cache thống kê)
            API-->>UI: return 200 OK
          else Lỗi hoặc không tồn tại
            API-->>UI: return 404 NotFound
          end
          deactivate API
          UI->>UI: queryClient.invalidateQueries(['sessions'])
          UI-->>Admin: Cập nhật lại danh sách lượt nghe trên UI
        </div>
      </div>
    </div>"""

# Remove target with an optional leading newline
target_with_newline = "\n" + target

if target_with_newline in content:
    content = content.replace(target_with_newline, '')
elif target in content:
    content = content.replace(target, '')
else:
    print("Target not found exactly.")
    sys.exit(1)

with open(filename, 'w', encoding='utf-8') as f:
    f.write(content)

print("Successfully removed section 20.8")
