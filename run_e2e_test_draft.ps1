$ErrorActionPreference = "Stop"

$SessionId = [guid]::NewGuid().ToString()
$Club1Id = [guid]::NewGuid().ToString()

Write-Host "1) Initializing Club 1..."
$clubInitBody = @{
    ClubId = $Club1Id
    RoomId = $SessionId
    PresidentUserId = [guid]::NewGuid().ToString()
    Name = "Test Club"
} | ConvertTo-Json
Invoke-RestMethod -Method Post -Uri "http://localhost:5002/api/Club/initialize" -Body $clubInitBody -ContentType "application/json" | Out-Null
Write-Host "Club 1 initialized: $Club1Id"

Write-Host "2) Starting Draft Session..."
$startDraftBody = @{
    TurnOrder = @($Club1Id)
} | ConvertTo-Json -Depth 10
Invoke-RestMethod -Method Post -Uri "http://localhost:5001/draft-sessions/$SessionId/start" -Body $startDraftBody -ContentType "application/json" | Out-Null
Write-Host "Draft started for session: $SessionId"
Start-Sleep -Seconds 2

Write-Host "3) Fetching Player Pool..."
$pool = Invoke-RestMethod -Method Get -Uri "http://localhost:5001/draft-sessions/$SessionId/pool"
if ($pool.Count -eq 0) {
    Write-Host "Player pool is empty!"
    exit 1
}

$playerId = $pool[0].playerId
Write-Host "Target Player ID: $playerId"

Write-Host "4) Claiming Player..."
$claimBody = @{
    ClubId = $Club1Id
    PlayerId = $playerId
} | ConvertTo-Json
$claimResponse = Invoke-RestMethod -Method Post -Uri "http://localhost:5001/draft-sessions/$SessionId/claim" -Body $claimBody -ContentType "application/json"
Write-Host "Claim Response: $($claimResponse | ConvertTo-Json)"

Write-Host "5) Waiting 5 seconds for Saga to finish..."
Start-Sleep -Seconds 5

Write-Host "6) Checking Draft Session State..."
$stateResponse = Invoke-RestMethod -Method Get -Uri "http://localhost:5001/draft-sessions/$SessionId/state"
Write-Host "State Response: $($stateResponse | ConvertTo-Json -Depth 10)"

$targetPlayerState = $pool | Where-Object { $_.playerId -eq $playerId }
Write-Host "Player IsClaimed (from pool endpoint): $($targetPlayerState.isClaimed)"

Write-Host "Test completed."
