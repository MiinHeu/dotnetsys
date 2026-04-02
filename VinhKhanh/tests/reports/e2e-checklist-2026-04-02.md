# E2E Checklist Report - 2026-04-02 (Updated)

## Scope
- Project: VinhKhanh production system (API + MAUI app + Web CMS)
- Goal:
  - Re-run PoC/MVP checklist after fixes
  - Record pass/fail with concrete command evidence

## Environment
- OS: Windows 10.0.26200
- .NET SDK: 10.0.201
- Node: v24.13.1, npm: 11.8.0
- Workspace path: `D:\C#\VinhKhanh`

## Core Fixes Applied In This Round
1. Stabilized build graph to remove `Build FAILED (0 errors)` race
- Set `BuildInParallel=false` for:
  - `src/VinhKhanh.API/VinhKhanh.API.csproj`
  - `src/VinhKhanh.App/VinhKhanh.App.csproj`
  - `tests/VinhKhanh.API.IntegrationTests/VinhKhanh.API.IntegrationTests.csproj`

2. Fixed MAUI QR camera logic compile/runtime wiring
- `src/VinhKhanh.App/QrScanPage.xaml.cs`
  - Awaited `GetAvailableCameras()`
  - Selected first camera safely

3. Fixed integration test host logging crash on Windows EventLog permission
- `src/VinhKhanh.API/Program.cs`
  - Clear default providers, use Console/Debug logging

4. Minor null-safety fix in local tour cache write path
- `src/VinhKhanh.App/Services/LocalDbService.cs`

## Automated Execution Summary
1. API build: PASS
- Command:
  - `dotnet build src/VinhKhanh.API/VinhKhanh.API.csproj -c Debug --no-restore -v minimal`
- Result:
  - Build succeeded, 0 errors.

2. MAUI app build (Windows): PASS
- Command:
  - `dotnet build src/VinhKhanh.App/VinhKhanh.App.csproj -c Debug -f net10.0-windows10.0.19041.0 --no-restore -v minimal`
- Result:
  - Build succeeded.
  - Warnings remain (MVVMTK0045), no blocking errors.

3. MAUI app build (Android): PASS
- Command:
  - `dotnet build src/VinhKhanh.App/VinhKhanh.App.csproj -c Debug -f net10.0-android --no-restore -v minimal`
- Result:
  - Build succeeded.
  - Non-blocking warnings remain (CS860x, CA1416/CA1422, XA0141).

4. Web build: PASS
- Command:
  - `npm.cmd run build`
- Result:
  - Production `dist` generated successfully.
  - Non-blocking warnings remain (`esbuild` unresolved import warning in vite internals, chunk-size warning, font file runtime resolution warning).

5. API integration tests: PASS
- Command:
  - `dotnet test tests/VinhKhanh.API.IntegrationTests/VinhKhanh.API.IntegrationTests.csproj -c Debug --no-build -v minimal`
- Result:
  - Passed 5/5.

## PoC Checklist (Pass/Fail)
1. GPS tracking realtime (foreground/background)
- Backend ingest endpoint (`/api/movement/batch`): PASS (covered in integration tests)
- MAUI compile readiness (Windows/Android): PASS
- Real device background-service behavior: PENDING MANUAL

2. Geofence trigger / POI activation
- Backend nearby logic (`/api/poi/nearby`): PASS
- MAUI geofence code path compile readiness: PASS
- Real movement trigger on device map route: PENDING MANUAL

3. Automatic narration (TTS/audio queue)
- AI chat fallback when external provider unavailable: PASS
- TTS controlled error fallback (`/api/ai/tts`): PASS
- MAUI narration flow compile readiness: PASS
- Real audio focus/interruption behavior on device: PENDING MANUAL

4. POI data management
- API POI CRUD + translation + deactivation: PASS

5. Map view
- MAUI Mapsui compile blockers previously fixed: PASS
- Real device map render + pin interaction: PENDING MANUAL

## MVP Checklist (Pass/Fail)
1. CMS (POI/Audio/Translation/History/Tour)
- Backend APIs: PASS
- Web production build: PASS

2. Analytics (movement, top POI, listening duration, heatmap)
- Backend APIs: PASS

3. QR activation (scan -> content)
- Backend QR POI resolution API: PASS
- MAUI QR scan code path compile readiness: PASS
- Real camera hardware scan end-to-end: PENDING MANUAL

## Integration Tests Included
- `tests/VinhKhanh.API.IntegrationTests/ApiIntegrationTests.cs`
  - Auth login seeded users
  - POI QR/nearby + CRUD + translation
  - Tour CRUD + duplicate stop-order validation
  - Movement + Analytics + History
  - AI fallback + TTS controlled behavior

## Final Status
- Backend logic validation: PASS (build + integration tests)
- Web build pipeline: PASS
- MAUI compile pipeline (Windows + Android): PASS
- Remaining risk before production rollout:
  - Hardware/on-device behavior (GPS background, camera scanning, audio focus, map interaction) still requires manual UAT on real devices.
