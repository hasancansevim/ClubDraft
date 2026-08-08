$ErrorActionPreference = "Stop"

$SessionId = [guid]::NewGuid().ToString()
$Club1Id = [guid]::NewGuid().ToString()
$Club2Id = [guid]::NewGuid().ToString()

Write-Host "1) Initializing Clubs..."
$club1InitBody = @{ ClubId = $Club1Id; RoomId = $SessionId; PresidentUserId = [guid]::NewGuid().ToString(); Name = "Test Club 1" } | ConvertTo-Json
Invoke-RestMethod -Method Post -Uri "http://localhost:5002/api/Club/initialize" -Body $club1InitBody -ContentType "application/json" | Out-Null
$club2InitBody = @{ ClubId = $Club2Id; RoomId = $SessionId; PresidentUserId = [guid]::NewGuid().ToString(); Name = "Test Club 2" } | ConvertTo-Json
Invoke-RestMethod -Method Post -Uri "http://localhost:5002/api/Club/initialize" -Body $club2InitBody -ContentType "application/json" | Out-Null
Write-Host "Clubs initialized"

Write-Host "2) Starting Draft Session..."
$startDraftBody = @{ TurnOrder = @($Club1Id, $Club2Id) } | ConvertTo-Json -Depth 10
Invoke-RestMethod -Method Post -Uri "http://localhost:5001/draft-sessions/$SessionId/start" -Body $startDraftBody -ContentType "application/json" | Out-Null
Start-Sleep -Seconds 2

Write-Host "3) Fetching Player Pool & Claiming..."
$pool = Invoke-RestMethod -Method Get -Uri "http://localhost:5001/draft-sessions/$SessionId/pool"
$playerId1 = $pool[0].playerId
$playerId2 = $pool[1].playerId
Invoke-RestMethod -Method Post -Uri "http://localhost:5001/draft-sessions/$SessionId/claim" -Body (@{ClubId = $Club1Id; PlayerId = $playerId1} | ConvertTo-Json) -ContentType "application/json" | Out-Null
Start-Sleep -Seconds 1
Invoke-RestMethod -Method Post -Uri "http://localhost:5001/draft-sessions/$SessionId/claim" -Body (@{ClubId = $Club2Id; PlayerId = $playerId2} | ConvertTo-Json) -ContentType "application/json" | Out-Null
Start-Sleep -Seconds 5

Write-Host "4) Simulating Week 1..."
$simReq = @{
    RoomId = $SessionId
    Week = 1
} | ConvertTo-Json
Invoke-RestMethod -Method Post -Uri "http://localhost:5006/api/debug/simulate-week" -Body $simReq -ContentType "application/json"

Start-Sleep -Seconds 5

Write-Host "5) Verifying Reputation DB..."
$rep = docker exec clubcraft-reputationfan-db psql -U clubcraft -d "clubcraft_reputation" -c "SELECT \""Id\"", \""Score\"" FROM \""ClubReputations\"";"
Write-Host $rep

$repHistory = docker exec clubcraft-reputationfan-db psql -U clubcraft -d "clubcraft_reputation" -c "SELECT \""ClubReputationId\"", \""Delta\"", \""Reason\"" FROM \""ReputationChange\"";"
Write-Host $repHistory

Write-Host "Test completed."
