# Script build và triển khai ứng dụng VinhKhanh lên nhiều thiết bị Android cùng lúc
# Cách dùng: .\run-multi-android.ps1

$projectName = "VinhKhanh.App"
$projectPath = ".\src\VinhKhanh.App\VinhKhanh.App.csproj"
$framework = "net10.0-android"

# 1. Tìm ADB
$adb = "adb"
if (!(Get-Command $adb -ErrorAction SilentlyContinue)) {
    $commonAdbPaths = @(
        "$env:LOCALAPPDATA\Android\Sdk\platform-tools\adb.exe",
        "C:\Users\$env:USERNAME\AppData\Local\Android\Sdk\platform-tools\adb.exe",
        "C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe",
        "D:\Android\Sdk\platform-tools\adb.exe"
    )
    foreach ($path in $commonAdbPaths) {
        if (Test-Path $path) {
            $adb = $path
            break
        }
    }
}

if (!(Get-Command $adb -ErrorAction SilentlyContinue) -and !(Test-Path $adb)) {
    Write-Host "CẢNH BÁO: Không tìm thấy ADB trong PATH hoặc các đường dẫn phổ biến." -ForegroundColor Yellow
}

Write-Host "--- Bước 1: Biên dịch APK (Build Release) ---" -ForegroundColor Cyan
dotnet build $projectPath -f $framework -c Release /p:AndroidPackageFormat=apk

if ($LASTEXITCODE -ne 0) {
    Write-Error "Lỗi khi build project."
    exit
}

# 2. Tìm file APK vừa tạo
$apkDir = ".\src\VinhKhanh.App\bin\Release\net10.0-android\publish"
if (!(Test-Path $apkDir)) {
    $apkDir = ".\src\VinhKhanh.App\bin\Release\net10.0-android"
}
$apkFile = Get-ChildItem -Path $apkDir -Filter "*.apk" -Recurse | Sort-Object LastWriteTime -Descending | Select-Object -First 1

if ($null -eq $apkFile) {
    Write-Error "Không tìm thấy file APK sau khi build."
    exit
}

Write-Host "Đã tìm thấy APK: $($apkFile.FullName)" -ForegroundColor Green

# 3. Lấy danh sách thiết bị
$adbExists = $false
if (Get-Command $adb -ErrorAction SilentlyContinue) { $adbExists = $true }
elseif (Test-Path $adb) { $adbExists = $true }

if ($adbExists) {
    $devicesOutput = & $adb devices
    $devices = $devicesOutput | Select-String -Pattern "\tdevice$" | ForEach-Object { $_.ToString().Split("`t")[0].Trim() }

    if ($devices.Count -eq 0) {
        Write-Warning "Không tìm thấy thiết bị Android nào đang kết nối (hãy bật USB Debugging)."
        Write-Host "Bạn có thể lấy file APK tại: $($apkFile.FullName) để cài thủ công."
        exit
    }

    Write-Host "Tìm thấy $($devices.Count) thiết bị: $($devices -join ', ')" -ForegroundColor Cyan

    # 4. Cài đặt và chạy đồng thời
    Write-Host "--- Bước 2: Cài đặt và Khởi chạy ---" -ForegroundColor Cyan
    $packageName = "com.companyname.vinhkhanh.app"
    $mainActivity = "$packageName/crc647f52f399f2b3394c.MainActivity"

    foreach ($deviceId in $devices) {
        Write-Host "[$deviceId] Đang cài đặt và khởi chạy..."
        # Chạy song song bằng Start-Process để nhanh hơn
        Start-Process -FilePath $adb -ArgumentList "-s $deviceId install -r `"$($apkFile.FullName)`"" -Wait -NoNewWindow
        Start-Process -FilePath $adb -ArgumentList "-s $deviceId shell am start -n $mainActivity" -NoNewWindow
    }

    Write-Host "Hoàn tất triển khai!" -ForegroundColor Green
} else {
    Write-Host "Không thể triển khai tự động vì thiếu ADB. File APK đã được tạo tại: $($apkFile.FullName)" -ForegroundColor Yellow
}
