$file = 'C:\Users\nt\dotnetsys\VinhKhanh-PRD-Final.html'
$content = [System.IO.File]::ReadAllText($file, [System.Text.Encoding]::UTF8)

# These Unicode chars get garbled - replace with simple ASCII text
# U+2264 (≤) -> text 'nho hon'
# U+2265 (≥) -> text 'lon hon'  
# U+FF1E (＞) -> text 'lon hon'
$content = $content.Replace([string][char]0x2264, ' nho hon bang ')
$content = $content.Replace([string][char]0x2265, ' lon hon bang ')
$content = $content.Replace([string][char]0xFF1E, ' lon hon ')

# Write back as UTF-8 without BOM
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
[System.IO.File]::WriteAllText($file, $content, $utf8NoBom)

Write-Host "Done. Replaced Unicode math symbols with ASCII text."
