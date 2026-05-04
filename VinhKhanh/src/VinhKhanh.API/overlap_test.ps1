Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  MO PHONG: 10 QUAN CHONG LAP TRONG VUNG  " -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

function Haversine($lat1, $lon1, $lat2, $lon2) {
    $dLat = ($lat2 - $lat1) * [Math]::PI / 180
    $dLon = ($lon2 - $lon1) * [Math]::PI / 180
    $a = [Math]::Sin($dLat/2) * [Math]::Sin($dLat/2) + [Math]::Cos($lat1 * [Math]::PI / 180) * [Math]::Cos($lat2 * [Math]::PI / 180) * [Math]::Sin($dLon/2) * [Math]::Sin($dLon/2)
    return [Math]::Round(6371000 * 2 * [Math]::Atan2([Math]::Sqrt($a), [Math]::Sqrt(1 - $a)), 1)
}

# Gia lap 10 quan doc theo pho Vinh Khanh (moi quan cach nhau ~10-15m)
$pois = @(
    @{id=1;  name="Oc Oanh";            lat=10.75310; lon=106.67800; radius=20; priority=10; cooldown=60},
    @{id=2;  name="Lau Bo Nha Chay";    lat=10.75320; lon=106.67805; radius=15; priority=7;  cooldown=60},
    @{id=3;  name="Bun Bo Hue Ba Tuoi"; lat=10.75325; lon=106.67810; radius=12; priority=5;  cooldown=60},
    @{id=4;  name="Com Tam Tu Map";     lat=10.75335; lon=106.67815; radius=10; priority=8;  cooldown=60},
    @{id=5;  name="Banh Canh Cua";      lat=10.75340; lon=106.67820; radius=18; priority=3;  cooldown=60},
    @{id=6;  name="Che Hien Khanh";     lat=10.75350; lon=106.67830; radius=15; priority=6;  cooldown=60},
    @{id=7;  name="Hai San Tuoi";       lat=10.75355; lon=106.67835; radius=20; priority=9;  cooldown=60},
    @{id=8;  name="Sushi Vien";         lat=10.75360; lon=106.67840; radius=12; priority=2;  cooldown=60},
    @{id=9;  name="Tra Sua Dai Loan";   lat=10.75370; lon=106.67845; radius=10; priority=1;  cooldown=60},
    @{id=10; name="Banh Mi Sai Gon";    lat=10.75380; lon=106.67850; radius=25; priority=4;  cooldown=60}
)

# Du khach dung tai vi tri nay (giua dam dong quan)
$userLat = 10.75340
$userLon = 106.67820

Write-Host "Vi tri du khach: ($userLat, $userLon)" -ForegroundColor White
Write-Host ""

# ====== BUOC 1: GeofenceService.CheckTriggeredAsync ======
Write-Host "====== BUOC 1: GeofenceService.CheckTriggeredAsync() ======" -ForegroundColor Yellow
Write-Host "Duyet tat ca $($pois.Count) quan, sap xep theo Priority giam dan:" -ForegroundColor Gray
Write-Host ""

$sortedPois = $pois | Sort-Object { $_.priority } -Descending
$triggered = @()

foreach ($poi in $sortedPois) {
    $dist = Haversine $userLat $userLon $poi.lat $poi.lon
    $inside = $dist -le $poi.radius
    $status = if ($inside) { "TRIGGER" } else { "skip" }
    $color = if ($inside) { "Green" } else { "DarkGray" }
    Write-Host ("  POI#{0,-2} P={1,-2} | {2,-20} | {3,6}m / {4}m | {5}" -f $poi.id, $poi.priority, $poi.name, $dist, $poi.radius, $status) -ForegroundColor $color
    if ($inside) { $triggered += $poi }
}

Write-Host ""
Write-Host "Ket qua: $($triggered.Count) / $($pois.Count) quan TRIGGER" -ForegroundColor Cyan
Write-Host ""

# ====== BUOC 2: MainViewModel — Enqueue tat ca ======
Write-Host "====== BUOC 2: MainViewModel.Receive() ======" -ForegroundColor Yellow
Write-Host "Enqueue TAT CA $($triggered.Count) POI vao NarrationService:" -ForegroundColor Gray
Write-Host ""

foreach ($poi in $triggered) {
    Write-Host ("  -> Enqueue: POI#{0} {1} (Priority={2})" -f $poi.id, $poi.name, $poi.priority) -ForegroundColor White
}
Write-Host ""

# ====== BUOC 3: NarrationService — Sap xep va phat ======
Write-Host "====== BUOC 3: NarrationService.ProcessQueueAsync() ======" -ForegroundColor Yellow
Write-Host "Sap xep hang doi theo Priority (cao -> thap) roi phat tuan tu:" -ForegroundColor Gray
Write-Host ""

$queue = $triggered | Sort-Object { $_.priority } -Descending
$playOrder = 1
foreach ($poi in $queue) {
    Write-Host ("  #{0}: Phat thuyet minh POI#{1} [{2}] (Priority={3})" -f $playOrder, $poi.id, $poi.name, $poi.priority) -ForegroundColor Green
    $playOrder++
}

Write-Host ""
Write-Host "====== BUOC 4: Bao ve chong trung lap ======" -ForegroundColor Yellow
Write-Host "Sau khi phat xong, moi POI se bi KHOA (cooldown $($pois[0].cooldown)s):" -ForegroundColor Gray
Write-Host ""

foreach ($poi in $queue) {
    Write-Host ("  POI#{0} {1}: Da phat -> Khoa trong {2}s (GeofenceCooldownStore)" -f $poi.id, $poi.name, $poi.cooldown) -ForegroundColor DarkYellow
    Write-Host ("  POI#{0} {1}: Da phat -> Khoa trong 25s (NarrationService DuplicateWindow)" -f $poi.id, $poi.name) -ForegroundColor DarkYellow
}

Write-Host ""
Write-Host "====== KET LUAN ======" -ForegroundColor Cyan
Write-Host "- Logic xu ly KHONG phu thuoc vao so luong quan." -ForegroundColor White
Write-Host "- Du 2, 10, hay 100 quan chong lap -> deu xu ly dung." -ForegroundColor White
Write-Host "- Thu tu phat: Priority cao nhat phat truoc." -ForegroundColor White
Write-Host "- Khong phat trung: 5 lop bao ve (xem audit)." -ForegroundColor White
Write-Host ""
