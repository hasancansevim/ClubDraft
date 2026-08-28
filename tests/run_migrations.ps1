$ErrorActionPreference = "Stop"

Write-Host "Applying Migrations automatically..."

$migratedServices = @()

# 1) Standart API/Infrastructure yapısındaki servisler
$apiProjects = Get-ChildItem -Path "src\Services" -Filter "*.API.csproj" -Recurse
foreach ($apiProj in $apiProjects) {
    # MatchEngine.API.csproj -> MatchEngine
    $serviceName = $apiProj.Name.Replace(".API.csproj", "").Replace("ClubCraft.", "")
    $infraProj = Get-ChildItem -Path "src\Services\$serviceName" -Filter "*.Infrastructure.csproj" -Recurse | Select-Object -First 1
    
    if ($infraProj) {
        Write-Host "Migrating Service: $serviceName"
        dotnet ef database update --project "$($infraProj.FullName)" --startup-project "$($apiProj.FullName)"
        
        $migratedServices += $serviceName
    }
}

# 2) SagaOrchestrator gibi monolith servisler
$orchestratorProj = Get-ChildItem -Path "src\Services\SagaOrchestrator" -Filter "*.SagaOrchestrator.csproj" -Recurse | Select-Object -First 1
if ($orchestratorProj) {
    Write-Host "Migrating Service: SagaOrchestrator"
    dotnet ef database update --project "$($orchestratorProj.FullName)" --startup-project "$($orchestratorProj.FullName)"
    
    $migratedServices += "SagaOrchestrator"
}

Write-Host "========================================="
Write-Host "Migrations applied successfully to the following services:"
$migratedServices | ForEach-Object { Write-Host "- $_" }
Write-Host "========================================="
