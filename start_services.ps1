# ClubCraft Servis Baslatici
# Tek pencerede, sirayla, log dosyalarina yazarak baslatir.
# Cikis icin Ctrl+C'ye basin.

$root = $PSScriptRoot
$logDir = "$root\logs"
if (!(Test-Path $logDir)) { New-Item -ItemType Directory -Path $logDir | Out-Null }

$services = @(
    @{ Name = "Session";          Project = "src\Services\Session\API\ClubCraft.Session.API.csproj" },
    @{ Name = "ClubManagement";   Project = "src\Services\ClubManagement\API\ClubCraft.ClubManagement.API.csproj" },
    @{ Name = "Draft";            Project = "src\Services\Draft\API\ClubCraft.Draft.API.csproj" },
    @{ Name = "RealtimeHub";      Project = "src\Services\RealtimeHub\ClubCraft.RealtimeHub.API\ClubCraft.RealtimeHub.API.csproj" },
    @{ Name = "ApiGateway";       Project = "src\ApiGateway\ClubCraft.ApiGateway.csproj" },
    @{ Name = "SagaOrchestrator"; Project = "src\Services\SagaOrchestrator\ClubCraft.SagaOrchestrator.csproj" },
    @{ Name = "MatchEngine";      Project = "src\Services\MatchEngine\API\ClubCraft.MatchEngine.API.csproj" },
    @{ Name = "ReputationFan";    Project = "src\Services\ReputationFan\API\ClubCraft.ReputationFan.API.csproj" },
    @{ Name = "Finance";          Project = "src\Services\FinanceSponsorship\API\ClubCraft.FinanceSponsorship.API.csproj" }
)

$jobs = @()

Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  ClubCraft Servis Baslatici" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  Servisler 3'er saniye arayla baslatilacak." -ForegroundColor Yellow
Write-Host "  Loglar: $logDir" -ForegroundColor Yellow
Write-Host "  Durdurmak icin Ctrl+C'ye basin." -ForegroundColor Yellow
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

# --- Migration kontrolu (servisleri baslatmadan ONCE, senkron) ---
# Gerekce: bir servis, eksik tablolar (orn. MassTransit OutboxState) yuzunden HTTP
# portunda sorunsuzca dinlemeye baslayabilir ama arka planda Outbox/Inbox islemleri
# sessizce hata verip event yayinlamayabilir. Bu, "servisler ayakta ama hicbir event
# gitmiyor" seklinde fark edilmesi zor bir duruma yol acar. Bu yuzden her calistirmada
# migration'lar once, senkron olarak uygulanir; basarisiz olursa servisler HIC baslatilmaz.
$migrationScript = "$root\tests\run_migrations.ps1"
if (Test-Path $migrationScript) {
    Write-Host "  [*] Migration kontrolu yapiliyor (dotnet ef database update)..." -ForegroundColor Cyan
    try {
        & $migrationScript
        if ($LASTEXITCODE -ne 0) { throw "run_migrations.ps1 exit code $LASTEXITCODE" }
        Write-Host "  [+] Migration kontrolu tamamlandi, tum veritabanlari guncel." -ForegroundColor Green
    } catch {
        Write-Host ""
        Write-Host "  [!] HATA: Migration uygulanamadi, servisler BASLATILMIYOR." -ForegroundColor Red
        Write-Host "      Detay: $_" -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host "  [!] UYARI: run_migrations.ps1 bulunamadi, migration kontrolu ATLANIYOR." -ForegroundColor Yellow
}
Write-Host ""

foreach ($svc in $services) {
    $logFile = "$logDir\$($svc.Name).log"
    $projectPath = "$root\$($svc.Project)"

    Write-Host "  [+] Baslatiliyor: $($svc.Name)..." -ForegroundColor Green

    $job = Start-Job -ScriptBlock {
        param($proj, $log)
        dotnet run --project $proj 2>&1 | Tee-Object -FilePath $log
    } -ArgumentList $projectPath, $logFile

    $jobs += @{ Job = $job; Name = $svc.Name }

    # Servislerin ayni anda kaynak yememesi icin bekliyoruz
    Start-Sleep -Seconds 3
}

# Frontend
Write-Host ""
Write-Host "  [+] Frontend baslatiliyor..." -ForegroundColor Green
$frontendLog = "$logDir\Frontend.log"
$frontendJob = Start-Job -ScriptBlock {
    param($dir, $log)
    Set-Location $dir
    npm run dev 2>&1 | Tee-Object -FilePath $log
} -ArgumentList "$root\frontend", $frontendLog

$jobs += @{ Job = $frontendJob; Name = "Frontend" }

Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  Tum servisler arka planda baslatildi!" -ForegroundColor Green
Write-Host ""
Write-Host "  Log dosyalari:" -ForegroundColor Yellow
foreach ($j in $jobs) {
    Write-Host "    $($j.Name): $logDir\$($j.Name).log" -ForegroundColor DarkYellow
}
Write-Host ""
Write-Host "  Canli log izlemek icin:" -ForegroundColor Yellow
Write-Host "    Get-Content .\logs\Draft.log -Wait -Tail 30" -ForegroundColor White
Write-Host "    Get-Content .\logs\ApiGateway.log -Wait -Tail 30" -ForegroundColor White
Write-Host ""
Write-Host "  Durdurmak icin bu pencerede Ctrl+C'ye basin." -ForegroundColor Red
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

# Ctrl+C'ye basilana kadar bekle, sonra temizlik yap
try {
    while ($true) {
        Start-Sleep -Seconds 5
        # Coken servisleri raporla
        foreach ($j in $jobs) {
            if ($j.Job.State -eq 'Failed') {
                Write-Host "  [!] HATA: $($j.Name) coktu! Log: $logDir\$($j.Name).log" -ForegroundColor Red
            }
        }
    }
} finally {
    Write-Host ""
    Write-Host "  Tum servisler durduruluyor..." -ForegroundColor Yellow
    foreach ($j in $jobs) {
        Stop-Job -Job $j.Job -ErrorAction SilentlyContinue
        Remove-Job -Job $j.Job -ErrorAction SilentlyContinue
    }
    Write-Host "  Temizlik tamamlandi." -ForegroundColor Green
}
