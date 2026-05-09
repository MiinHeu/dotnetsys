Write-Host "--- Dang chuan bi Build APK cho Pho Vinh Khanh ---" -ForegroundColor Cyan
$ProjectDir = "$PSScriptRoot\VinhKhanh\src\VinhKhanh.App"
cd $ProjectDir

Write-Host "Dang chay lenh dotnet publish..." -ForegroundColor Yellow
dotnet publish VinhKhanh.App.csproj -f net10.0-android -c Release /p:AndroidPackageFormat=apk /p:AndroidKeyStore=false

if ($LASTEXITCODE -eq 0) {
    Write-Host "`n--- BUILD THANH CONG! ---" -ForegroundColor Green
    $OutputPath = Get-ChildItem -Path "bin\Release\net10.0-android\publish\*-Signed.apk" | Select-Object -First 1
    Write-Host "File APK cua ban tai: $($OutputPath.FullName)" -ForegroundColor White
} else {
    Write-Host "`n--- BUILD THAT BAI. Vui long kiem tra loi ben tren. ---" -ForegroundColor Red
}
Write-Host "Nhan phim bat ky de thoat..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
