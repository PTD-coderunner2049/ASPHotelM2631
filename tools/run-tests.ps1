<#
 Chay toan bo pipeline test roi sinh tai lieu. Chay tai thu muc goc repo.
   .\tools\run-tests.ps1                 # Unit + API (+HTTPS in-process): khong can database/trinh duyet
   .\tools\run-tests.ps1 -Live -Ui       # them test server that + Selenium (bat ung dung truoc, xem README)
 Ket qua: TestResults\*.trx, TestResults\evidence\<TC-ID>\*.png, docs\testing\generated\{TestCases.md,TestResult.md,TestResult.html}
 Exit code 0 chi khi MOI test case trong pham vi da chay va dat.
 (File nay co y chi dung ky tu ASCII de Windows PowerShell 5.1 doc dung moi bang ma.)
#>
param([switch]$Live, [switch]$Ui, [switch]$RequireUiSignoff)

$ErrorActionPreference = "Continue"
$env:PYTHONUTF8 = "1"          # de Python in/ghi tieng Viet dung tren Windows
$failed = $false

# Xoa ket qua cu de khong dung nham
if (Test-Path "TestResults") { Remove-Item "TestResults" -Recurse -Force }

function Run-Suite($project, $name, $filter) {
    Write-Host "=== $name ===" -ForegroundColor Cyan
    $dotnetArgs = @("test", "tests/$project", "-c", "Release",
                    "--logger", "trx;LogFileName=$name.trx",
                    "--results-directory", "TestResults")
    if ($filter) { $dotnetArgs += @("--filter", $filter) }
    & dotnet @dotnetArgs
    if ($LASTEXITCODE -ne 0) { $script:failed = $true }
}

Run-Suite "ASPHotelM2631.Tests.Unit" "unit" $null
Run-Suite "ASPHotelM2631.Tests.Api"  "api"  "Category!=Live"

$suites = "Unit,Api"

if ($Live) {
    if (-not $env:LIVE_HTTP_URL)  { $env:LIVE_HTTP_URL  = "http://localhost:5160" }
    if (-not $env:LIVE_HTTPS_URL) { $env:LIVE_HTTPS_URL = "https://localhost:7069" }
    Run-Suite "ASPHotelM2631.Tests.Api" "live" "Category=Live"
    $suites += ",Live"
}

if ($Ui) {
    New-Item -ItemType Directory -Force -Path "TestResults\evidence" | Out-Null
    $env:EVIDENCE_DIR = (Resolve-Path "TestResults\evidence").Path
    Write-Host "Chrome se mo ra - hay NHIN no chay, roi ghi ten ban vao docs/testing/ui-signoff.csv" -ForegroundColor Yellow
    Run-Suite "ASPHotelM2631.Tests.Ui" "ui" "Category=UI"
    $suites += ",Ui"
}

# Chon lenh Python co san tren may (python hoac py)
$py = $null
foreach ($candidate in @("python", "py")) {
    if (Get-Command $candidate -ErrorAction SilentlyContinue) { $py = $candidate; break }
}
if (-not $py) {
    Write-Host "Khong tim thay Python (lenh python hoac py). Cai Python 3 roi chay lai." -ForegroundColor Red
    exit 1
}

$genArgs = @("tools/Generate-TestDocs.py", "--suites", $suites)
if ($RequireUiSignoff) { $genArgs += "--require-ui-signoff" }
& $py @genArgs
if ($LASTEXITCODE -ne 0) { $failed = $true }

if ($failed) {
    Write-Host "KET LUAN: FAILED" -ForegroundColor Red
    exit 1
}
Write-Host "KET LUAN: PASSED" -ForegroundColor Green
exit 0
