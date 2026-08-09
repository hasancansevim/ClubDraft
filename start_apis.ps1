$ErrorActionPreference = "Continue"
Write-Host "Killing old API processes if any..."
Stop-Process -Name dotnet -Force -ErrorAction SilentlyContinue

Write-Host "Starting Draft API..."
Start-Process -NoNewWindow dotnet "run --no-build --project src/Services/Draft/API/ClubCraft.Draft.API.csproj --urls http://localhost:5004" -RedirectStandardOutput draft.log -RedirectStandardError draft_err.log
Write-Host "Starting ClubManagement API..."
Start-Process -NoNewWindow dotnet "run --no-build --project src/Services/ClubManagement/API/ClubCraft.ClubManagement.API.csproj --urls http://localhost:5002" -RedirectStandardOutput club.log -RedirectStandardError club_err.log
Write-Host "Starting Session API..."
Start-Process -NoNewWindow dotnet "run --no-build --project src/Services/Session/API/ClubCraft.Session.API.csproj --urls http://localhost:5006" -RedirectStandardOutput session.log -RedirectStandardError session_err.log
Write-Host "Starting SagaOrchestrator..."
Start-Process -NoNewWindow dotnet "run --no-build --project src/Services/SagaOrchestrator/ClubCraft.SagaOrchestrator.csproj" -RedirectStandardOutput saga.log -RedirectStandardError saga_err.log
Write-Host "Starting MatchEngine API..."
Start-Process -NoNewWindow dotnet "run --no-build --project src/Services/MatchEngine/API/ClubCraft.MatchEngine.API.csproj --urls http://localhost:5010" -RedirectStandardOutput matchengine.log -RedirectStandardError matchengine_err.log
Write-Host "Starting ReputationFan API..."
Start-Process -NoNewWindow dotnet "run --no-build --project src/Services/ReputationFan/API/ClubCraft.ReputationFan.API.csproj --urls http://localhost:5007" -RedirectStandardOutput reputation.log -RedirectStandardError reputation_err.log
Write-Host "Starting FinanceSponsorship API..."
Start-Process -NoNewWindow dotnet "run --no-build --project src/Services/FinanceSponsorship/API/ClubCraft.FinanceSponsorship.API.csproj --urls http://localhost:5008" -RedirectStandardOutput finance.log -RedirectStandardError finance_err.log
Start-Sleep -Seconds 10
Write-Host "Services started!"
Wait-Process -Name dotnet
