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

Write-Host "5) Verifying Fixture in DB..."
$fixture = docker exec clubcraft-matchengine-db psql -U clubcraft -d "matchengine" -t -c "SELECT \""Id\"", \""RoomId\"" FROM \""Fixtures\"";"
$fixtureId = $fixture.Trim().Split('|')[0].Trim()
Write-Host "Generated FixtureId: $fixtureId"

$matches = docker exec clubcraft-matchengine-db psql -U clubcraft -d "matchengine" -c "SELECT \""Id\"", \""HomeClubId\"", \""AwayClubId\"", \""Week\"" FROM \""Match\"";"
Write-Host $matches

Write-Host "6) Injecting Morale Bonus manually for Club1 to test reset..."
docker exec clubcraft-matchengine-db psql -U clubcraft -d "matchengine" -c "UPDATE \""ClubPowerRatings\"" SET \""MoraleBonus\"" = 10 WHERE \""ClubId\"" = '$Club1Id';"
$morale = docker exec clubcraft-matchengine-db psql -U clubcraft -d "matchengine" -c "SELECT \""ClubId\"", \""MoraleBonus\"" FROM \""ClubPowerRatings\"";"
Write-Host $morale

Write-Host "7) Simulating Week 1..."
$simReq = @{
    RoomId = $SessionId
    Week = 1
} | ConvertTo-Json
Invoke-RestMethod -Method Post -Uri "http://localhost:5006/api/debug/simulate-week" -Body $simReq -ContentType "application/json"

Write-Host "8) Verifying Morale Bonus reset in DB..."
$moraleAfter = docker exec clubcraft-matchengine-db psql -U clubcraft -d "matchengine" -c "SELECT \""ClubId\"", \""MoraleBonus\"" FROM \""ClubPowerRatings\"";"
Write-Host $moraleAfter

Write-Host "Test completed."
