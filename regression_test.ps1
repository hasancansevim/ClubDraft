$ErrorActionPreference = "Stop"

Write-Host "Creating Session and Room..."
$createRoomRes = Invoke-RestMethod -Uri "http://localhost:5001/api/draft/rooms" -Method Post -Body '{}' -ContentType "application/json"
$roomId = $createRoomRes.roomId

Write-Host "Joining Room... (Club 1)"
$club1Res = Invoke-RestMethod -Uri "http://localhost:5001/api/draft/rooms/$roomId/join" -Method Post -Body '{"presidentName": "Pres 1", "clubName": "Club A"}' -ContentType "application/json"
$club1Id = $club1Res.clubId

Write-Host "Joining Room... (Club 2)"
$club2Res = Invoke-RestMethod -Uri "http://localhost:5001/api/draft/rooms/$roomId/join" -Method Post -Body '{"presidentName": "Pres 2", "clubName": "Club B"}' -ContentType "application/json"
$club2Id = $club2Res.clubId

Write-Host "Starting Draft..."
Invoke-RestMethod -Uri "http://localhost:5001/api/draft/rooms/$roomId/start" -Method Post -Body '{}' -ContentType "application/json"

Start-Sleep -Seconds 2

Write-Host "Club A making a pick..."
$pick1Res = Invoke-RestMethod -Uri "http://localhost:5001/api/draft/rooms/$roomId/pick" -Method Post -Body '{"clubId": "'$club1Id'", "position": "ST"}' -ContentType "application/json"
Start-Sleep -Seconds 3

Write-Host "Club B making a pick..."
$pick2Res = Invoke-RestMethod -Uri "http://localhost:5001/api/draft/rooms/$roomId/pick" -Method Post -Body '{"clubId": "'$club2Id'", "position": "GK"}' -ContentType "application/json"
Start-Sleep -Seconds 3

Write-Host "Making Weekly Decision for Club A (Stadium Investment)..."
Invoke-RestMethod -Uri "http://localhost:5002/api/clubmanagement/clubs/$club1Id/weekly-decisions" -Method Post -Body '{"week": 1, "decisionType": 1}' -ContentType "application/json"
Start-Sleep -Seconds 2

Write-Host "Simulating Matches for Week 1..."
Invoke-RestMethod -Uri "http://localhost:5006/api/matchengine/simulate" -Method Post -Body '{"week": 1}' -ContentType "application/json"
Start-Sleep -Seconds 5

Write-Host "Regression testing completed! Check databases for results."
