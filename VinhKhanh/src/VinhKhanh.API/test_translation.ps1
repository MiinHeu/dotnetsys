$ErrorActionPreference = "Stop"
try {
    Write-Host "Logging in..."
    $loginRes = Invoke-RestMethod -Method POST -Uri "http://localhost:5283/api/auth/login" -ContentType "application/json" -Body '{"username":"admin","password":"Admin@2026"}'
    $token = $loginRes.token
    Write-Host "Token obtained: $($token.Substring(0, 20))..."
    
    Write-Host "Translating 'Hello'..."
    $transRes = Invoke-RestMethod -Method POST -Uri "http://localhost:5283/api/translation/text" -Headers @{Authorization="Bearer $token"} -ContentType "application/json" -Body '{"text":"Hello","from":"en","to":"vi"}'
    Write-Host "Translation Result:"
    $transRes | ConvertTo-Json
} catch {
    Write-Error "Error: $_"
}
