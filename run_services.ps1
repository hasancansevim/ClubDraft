$ErrorActionPreference = "Continue"
Start-Process -NoNewWindow dotnet "run --project src/Services/Draft/API/ClubCraft.Draft.API/ClubCraft.Draft.API.csproj --urls http://localhost:5001"
Start-Process -NoNewWindow dotnet "run --project src/Services/ClubManagement/API/ClubCraft.ClubManagement.API/ClubCraft.ClubManagement.API.csproj --urls http://localhost:5002"
Start-Process -NoNewWindow dotnet "run --project src/Services/SagaOrchestrator/ClubCraft.SagaOrchestrator/ClubCraft.SagaOrchestrator.csproj"
Start-Sleep -Seconds 15
Write-Host "Services started."
