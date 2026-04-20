$url = "http://localhost:5283/api/Poi/nearby"
$body = @{
    lat = 10.7532
    lon = 106.67805
} | ConvertTo-Json

$headers = @{
    "Content-Type" = "application/json"
}

Write-Host "Wait for API to start..."

for ($i=0; $i -lt 30; $i++) {
    try {
        $response = Invoke-RestMethod -Uri $url -Method Post -Headers $headers -Body $body -ErrorAction Stop
        Write-Host "SUCCESS! API returned:"
        $response | Select-Object name, priority, triggerRadiusMeters | Format-Table
        exit 0
    } catch {
        Start-Sleep -Seconds 2
    }
}
Write-Host "Failed to connect to API"
