# VinhKhanh — Thuyết Minh Tự Động Đa Ngôn Ngữ Cho Phố Ẩm Thực Vĩnh Khánh

> Đồ án: Hệ thống thuyết minh tự động theo vị trí (GPS/QR), đa ngôn ngữ, cho phố ẩm thực Quận 4, TP.HCM.

## PoC ✓ đã hoàn thành
- [x] GPS tracking (foreground + background, mock mode, tiết kiệm pin)
- [x] Geofence với Haversine + hysteresis + cooldown + consecutive hits
- [x] Thuyết minh tự động: Audio thu sẵn + TTS device + API TTS
- [x] Narration Queue: semaphore, chống trùng, duplicate window, **priority interrupt**
- [x] POI + Tour CRUD API
- [x] SignalR real-time
- [x] AI Chat assistant (Ollama / Azure)
- [x] **Chống trùng vị trí POI** — Create/Update check lat/lon trùng với POI khác
- [x] **Owner isolation** — Owner (1:n với POI) chỉ xem/sửa/xóa POI của mình, không chạm POI owner khác
- [x] **Migration `AddOwnerIdToPoi`** — `Poi.OwnerUserId` → 1 owner quản lý nhiều POI

## MVP còn lại
- [ ] CMS web admin hoàn chỉnh (POI, Audio, Bản dịch, Tour, Lịch sử) — pages đã tạo, cần test chức năng + owner filter UI
- [ ] Content layer offline/online sync — OutboxService + LocalDbService còn cơ bản, cần hoàn thiện sync logic

## Project Structure
```
VinhKhanh/
├── src/
│   ├── VinhKhanh.API/                # ASP.NET Core 9 — REST + SignalR
│   │   ├── Controllers/              # Poi, Tour, Auth, Ai, Tts, Audio, Translation, Analytics, History, Movement, Admin
│   │   ├── Services/                 # AzureAi, OllamaAi, VoiceRssTts, AzureTts, 3xTranslation, Redis
│   │   └── Hubs/                     # VinhKhanhHub (broadcast POI/Tour events)
│   ├── VinhKhanh.App/                # .NET MAUI — Android/iOS mobile app
│   │   ├── Services/                 # GPS, Geofence, Narration, Cache, Outbox, Session, Connectivity
│   │   ├── Models/                   # PoiSnapshot, TourSnapshot
│   │   └── ViewModels/               # Main, Tours, Chat, PoiDetail, Settings
│   ├── vinh-khanh-web/               # Web admin (Vite + React + TS + shadcn/ui)
│   │   └── src/pages/                # Dashboard, AdminMap, Pois, PoiEditor, ToursAdmin, TourEditor, AudioPage, HistoryPage, AnalyticsPage, Translations, Login
│   ├── VinhKhanh.Infrastructure/     # EF Core DbContext, Migrations, AppUser entity
│   ├── VinhKhanh.Shared/             # GeoMath (Haversine), DTOs
│   └── tests/                        # Unit tests
```

## Tech Stack
| Part | Tech |
|------|------|
| Backend | .NET 9, Minimal APIs, EF Core, SQLite (dev) → PostgreSQL (prod) |
| Mobile | .NET MAUI 10 (Android/iOS), XAML, CommunityToolkit, ZXing, SkiaSharp/Mapsui |
| Web | Vite, React, TypeScript, TailwindCSS, shadcn/ui, SignalR, React Query |
| AI | Ollama (default) / Azure OpenAI |
| TTS | VoiceRss / Azure Cognitive Services / Device TTS |
| Translation | Microsoft Translator / LibreTranslate / Ollama Translation |
| Auth | JWT + BCrypt |
| GPS | Geolocation (MAUI), FusedLocationProvider (Android), CLLocationManager (iOS) |

## Architecture — Mobile App Flow

```
GPS (every 3s) ─→ GeofenceService ─→ trigger POIs (hysteresis + cooldown + consecutive)
                                          │
                                          ▼
                                   NarrationService
                          ┌───────────────┤───────────────┐
                          ▼               ▼               ▼
                    Audio URL        API TTS        Device TTS
                   (audio thu sẵn)  (server)      (local fallback)

POI cache ─→ Local SQLite ─→ Sync khi có WiFi
Movement/Analytics ─→ Outbox (offline queue) ─→ Batch upload khi online
```

## Architecture — Web Admin

```
Admin ─→ Login (JWT) ─→ Dashboard / AdminMap / Pois / Tours / Analytics / History / Translations / Audio
                    └─→ SignalR real-time updates từ API
```

## Key Endpoints
| Method | Path | Role | Mục đích |
|--------|------|------|----------|
| GET | `api/poi` | public | Danh sách POI |
| GET | `api/poi/qrcode/{code}` | public | Tìm POI qua QR |
| POST | `api/poi/nearby` | public | Tìm POI gần GPS |
| CRUD | `api/poi` | Admin/Owner | Quản lý POI |
| CRUD | `api/tour` | Admin | Quản lý tour |
| POST | `api/ai/chat` | public | AI chat |
| POST | `api/ai/tts` | public | Server TTS |
| POST | `api/translation/text` | public | Dịch text |
| POST | `api/audio/upload` | Admin/Owner | Upload audio |
| POST | `api/analytics/log` | public | Log visit |
| GET | `api/analytics/top` | Admin | Top POI theo lượt nghe |
| GET | `api/movement/heatmap` | Admin | Heatmap di chuyển |
| POST | `api/history/log` | public | Log event |
| GET | `api/history` | Admin | Query history |
| POST | `api/auth/login` | public | Login (JWT) |

## Service Resolution (API startup)
- AI: có Azure config → `AzureAiService`, không → `OllamaAiService`
- TTS: có VoiceRss key → `VoiceRssTtsService`, không → `AzureTtsService`
- Translation: có Ollama → Ollama, có LibreTranslate → Libre, không → Microsoft
- Redis: `NoOpRedisService` (đã tắt)

## MAUI App — Key Services
| Service | Interface | Nhiệm vụ |
|---------|-----------|----------|
| `GpsService` | `IGpsService` | GPS tracking, mock mode, foreground service (Android) |
| `GeofenceService` | `IGeofenceService` | Haversine + hysteresis + cooldown + consecutive hits |
| `NarrationService` | `INarrationService` | Audio queue, semaphore, duplicate prevention, TTS fallback |
| `LocalDbService` | `ILocalDbService` | Local SQLite cache |
| `LocalPoiCacheService` | — | Cache POI offline |
| `OutboxService` | `IOutboxService` | Offline queue → sync khi online |
| `SessionService` | — | Session ID management |
| `ConnectivityService` | — | Detect WiFi reconnect, auto-sync |
| `GeofenceCooldownStore` | — | Persist cooldown state |

## Geofence Logic
- Haversine distance vs `TriggerRadiusMeters`
- Exit hysteresis: `ExitHysteresisFactor = 1.2x`
- Consecutive hits: cần 2 lần liên tiếp để trigger
- Cooldown: `poi.CooldownSeconds` (0–7200)
- Priority: xếp theo `poi.Priority`

## Narration Logic
- SemaphoreSlim (1) — không phát trùng
- Queue + `_queuedKeys` HashSet — debounce enqueue
- `_recentlyPlayed` — 25s duplicate window
- Fallback hierarchy: Audio URL → Server TTS → Device TTS
- Translation tự động nếu không có text theo ngôn ngữ

## Common Operations

### Build
```bash
# API
dotnet build VinhKhanh/src/VinhKhanh.API/VinhKhanh.API.csproj

# Mobile (Android)
dotnet build VinhKhanh/src/VinhKhanh.App/VinhKhanh.App.csproj -f net10.0-android

# Web
cd VinhKhanh/src/vinh-khanh-web && npm install && npm run dev
```

### Run
```bash
dotnet run --project VinhKhanh/src/VinhKhanh.API/VinhKhanh.API.csproj
```

### Docker Compose (dev)
Services: PostgreSQL :5440, Redis :6389
File: `VinhKhanh/docker-compose.dev.yml`

## .gitignore Rules
Không commit: `*.db*`, `*.log`, `bin/`, `obj/`, `wwwroot/audio/`

## Development Guidelines
- **UTF-8 bắt buộc** cho tất cả source files (đặc biệt XAML trên Windows)
- **Vietnamese label** trong UI và API messages
- **Async/await** ở tất cả I/O
- **SignalR events**: PoiCreated, PoiUpdated, TourCreated, TourUpdated
- **CORS**: localhost, 127.0.0.1, 10.0.2.2 (Android emulator)
- **API default port**: 5283

## Things to Avoid
- Không commit log/DB/audio files
- Không hardcode API keys (check `appsettings.*.json`)
- Không sửa DB schema mà không cập nhật migrations
- Không bỏ qua encoding UTF-8 — MAUI build lỗi trên Windows nếu file không UTF-8
- Không gọi `db.SaveChangesAsync()` trong lock/narration — dùng outbox pattern
