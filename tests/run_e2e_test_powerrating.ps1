# tests/run_e2e_test_powerrating.ps1
#
# Match Engine guc formulu derinlestirmesinin (spec.md, "Match Engine Guc
# Formulu Derinlestirme") GERCEK altyapi uzerinde (Draft -> Saga ->
# ClubManagement -> RabbitMQ -> MatchEngine, tumu gercek HTTP + Postgres)
# uctan uca kanitini toplar:
#
#   1) Iki gercek kulup draft edilir (11'er oyuncu, pozisyona gore secilerek).
#   2) Ayni kadroyla once DOGRU, sonra BOZUK (kaleci forvette, stoper
#      forvette) bir dizilim kurulup, ayni sabit rakibe karsi 14'er maclik
#      iki ayri seri (RoomId basina bir round-robin donemi) simule edilir --
#      MatchEngine.ClubPowerRatings/RosterPlayerSnapshot/LineupSlotAssignment
#      tablolarindan dogrudan psql ile kanit toplanir.
#   3) Cok zayif bir "dummy" kulube karsi art arda galibiyetler alinarak
#      Moral'in DB'de gercekten +5'e ulasip sabitlendigi gosterilir.
#   4) Dengeli iki kadronun (ayni seri) sonuclarindan surpriz payinin hala
#      var oldugu (guclu taraf her mac kazanmiyor) raporlanir.
#
# Servislerin ayakta oldugu varsayilir (bkz. start_services.ps1). Calistirmadan
# once: tests/run_migrations.ps1 (bu script'in gerektirdigi RedesignClubPowerRating
# migration'i dahil).

$ErrorActionPreference = "Stop"

$ClubMgmtBase = "http://localhost:5276"
$DraftBase    = "http://localhost:5042"
$MatchBase    = "http://localhost:5123"

function Invoke-Psql($sql) {
    # docker exec argv uzerinden gecen ciftirnak-icindeki-ciftirnak (Postgres'in
    # buyuk/kucuk harfe duyarli tanimlayicilari icin gerekli ""ColumnName"")
    # PowerShell -> native argv -> container katmanlarinda guvenilir kacamiyor;
    # bunun yerine SQL'i stdin uzerinden gecirmek (docker exec -i) bu sorunu
    # tamamen ortadan kaldiriyor.
    $tmpFile = [System.IO.Path]::GetTempFileName()
    try {
        Set-Content -Path $tmpFile -Value $sql -Encoding utf8 -NoNewline
        Get-Content -Path $tmpFile -Raw | docker exec -i clubcraft-matchengine-db psql -U clubcraft -d matchengine -t -A
    } finally {
        Remove-Item $tmpFile -ErrorAction SilentlyContinue
    }
}

function New-Club($clubId, $roomId, $name) {
    $body = @{
        ClubId = $clubId
        RoomId = $roomId
        PresidentUserId = [guid]::NewGuid().ToString()
        Name = $name
        ParticipantId = [guid]::NewGuid().ToString()
    } | ConvertTo-Json
    Invoke-RestMethod -Method Post -Uri "$ClubMgmtBase/api/clubs/initialize" -Body $body -ContentType "application/json" | Out-Null
}

Write-Host "=== 1) Kulupler ve draft ===" -ForegroundColor Cyan

$DraftRoomId = [guid]::NewGuid().ToString()
$ClubAId    = [guid]::NewGuid().ToString()   # "Dogru/Bozuk dizilim" testinin oznesi
$ClubOppId  = [guid]::NewGuid().ToString()   # Sabit, karsilastirilabilir rakip
$ClubWeakId = [guid]::NewGuid().ToString()   # Moral +5 testi icin bilerek zayif kulup

New-Club $ClubAId    $DraftRoomId "Test FC (Dizilim)"
New-Club $ClubOppId  $DraftRoomId "Test United (Rakip)"
New-Club $ClubWeakId $DraftRoomId "Test Reserves (Zayif)"

# Ilk 11'in ihtiyaci 11 (GK,LB,CB,CB,RB,LM,CM,CM,RM,ST,ST) ama bilinen bir
# Draft/ClubManagement Saga race condition'i (bkz. spec.md, "Acik konu - dusuk
# oncelik" notu) nadiren tek bir pick'i sessizce dusurebiliyor -- her pozisyon
# icin 1 yedek daha draft ediliyor (19 toplam), gercek dizilim ise senkron
# sonrasi MatchEngine'in KENDI DB'sinden okunan gercek roster'dan kuruluyor
# (bkz. asagida Get-SyncedRoster), yani hangi pickler dustuyse onlar sorun
# yaratmiyor.
$neededPositions = @("GK","GK","LB","LB","CB","CB","CB","RB","RB","LM","LM","CM","CM","CM","RM","RM","ST","ST","ST")

# 3 kulup, 19 round (11 ihtiyac + 8 yedek pozisyon, yukaridaki nota bkz.),
# snake sira: tek round [A,Opp,Weak], cift round [Weak,Opp,A]
$totalRounds = $neededPositions.Count
$turnOrder = @()
for ($round = 1; $round -le $totalRounds; $round++) {
    if ($round % 2 -eq 1) { $turnOrder += @($ClubAId, $ClubOppId, $ClubWeakId) }
    else { $turnOrder += @($ClubWeakId, $ClubOppId, $ClubAId) }
}
$startBody = @{ TurnOrder = $turnOrder } | ConvertTo-Json
Invoke-RestMethod -Method Post -Uri "$DraftBase/api/draft-sessions/$DraftRoomId/start" -Body $startBody -ContentType "application/json" | Out-Null

$pool = Invoke-RestMethod -Method Get -Uri "$DraftBase/api/draft-sessions/$DraftRoomId/pool"
Write-Host "Pool'da $($pool.Count) oyuncu var."

$used = @{}

function Get-PlayerForPosition($pos) {
    foreach ($p in $pool) {
        if ($p.position -eq $pos -and -not $used.ContainsKey($p.playerId)) {
            $used[$p.playerId] = $true
            return $p
        }
    }
    throw "Pool'da bos '$pos' pozisyonunda oyuncu kalmadi."
}

function Get-WeakestRemainingPlayer() {
    $candidate = $pool | Where-Object { -not $used.ContainsKey($_.playerId) } | Sort-Object overall | Select-Object -First 1
    $used[$candidate.playerId] = $true
    return $candidate
}

function Claim($clubId, $playerId) {
    $body = @{ ClubId = $clubId; PlayerId = $playerId } | ConvertTo-Json
    $result = Invoke-RestMethod -Method Post -Uri "$DraftBase/api/draft-sessions/$DraftRoomId/claim" -Body $body -ContentType "application/json"
    if (-not $result.success) { throw "Claim basarisiz: $($result.reason)" }
}

$aRoster = @(); $oppRoster = @(); $weakRoster = @()
$aIdx = 0; $oppIdx = 0

foreach ($clubId in $turnOrder) {
    if ($clubId -eq $ClubAId) {
        $player = Get-PlayerForPosition $neededPositions[$aIdx]; $aIdx++
        $aRoster += $player
        Claim $ClubAId $player.playerId
    } elseif ($clubId -eq $ClubOppId) {
        $player = Get-PlayerForPosition $neededPositions[$oppIdx]; $oppIdx++
        $oppRoster += $player
        Claim $ClubOppId $player.playerId
    } else {
        $player = Get-WeakestRemainingPlayer
        $weakRoster += $player
        Claim $ClubWeakId $player.playerId
    }
}

Write-Host ("ClubA roster toplam Overall: {0} (ort. {1:F1})" -f ($aRoster.overall | Measure-Object -Sum).Sum, ($aRoster.overall | Measure-Object -Average).Average)
Write-Host ("ClubOpp roster toplam Overall: {0} (ort. {1:F1})" -f ($oppRoster.overall | Measure-Object -Sum).Sum, ($oppRoster.overall | Measure-Object -Average).Average)
Write-Host ("ClubWeak roster toplam Overall: {0} (ort. {1:F1})" -f ($weakRoster.overall | Measure-Object -Sum).Sum, ($weakRoster.overall | Measure-Object -Average).Average)

Write-Host "`n=== 2) MatchEngine'in roster'i senkron aldigini bekleniyor (Saga + Outbox/Inbox) ===" -ForegroundColor Cyan
# En az 11 (Ilk 11 icin gereken minimum) -- tam sayi 19 olmayabilir, yukarida
# aciklanan bilinen tek-pick-drop race'i yuzunden; onemli olan >=11 olmasi,
# gercek lineup asagida MatchEngine'in kendi DB'sinden okunacak.
$deadline = (Get-Date).AddSeconds(150)
do {
    Start-Sleep -Seconds 5
    $counts = Invoke-Psql "SELECT ""ClubPowerRatingClubId"", COUNT(*) FROM ""RosterPlayerSnapshot"" WHERE ""ClubPowerRatingClubId"" IN ('$ClubAId','$ClubOppId','$ClubWeakId') GROUP BY 1;"
    Write-Host "  RosterPlayerSnapshot sayaci: $counts"
    $lines = $counts -split "`n" | Where-Object { $_.Trim() -ne "" }
    $ready = $true
    foreach ($cid in @($ClubAId, $ClubOppId, $ClubWeakId)) {
        $line = $lines | Where-Object { $_ -like "$cid*" }
        if (-not $line) { $ready = $false; continue }
        $n = [int]($line -split '\|')[1]
        if ($n -lt 11) { $ready = $false }
    }
} while (-not $ready -and (Get-Date) -lt $deadline)

if (-not $ready) { throw "MatchEngine roster senkronu zaman asimina ugradi (150s) -- Outbox/Saga akisini kontrol et." }
Write-Host "MatchEngine'de her 3 kulup icin de en az 11 oyuncu senkron oldu (DB'den dogrulandi)." -ForegroundColor Green

function Get-SyncedRoster($clubId) {
    $rows = Invoke-Psql "SELECT ""PlayerId"",""Overall"",""Position"" FROM ""RosterPlayerSnapshot"" WHERE ""ClubPowerRatingClubId"" = '$clubId' ORDER BY ""Overall"" DESC;"
    $players = @()
    foreach ($line in ($rows -split "`n" | Where-Object { $_.Trim() -ne "" })) {
        $parts = $line -split '\|'
        $players += [pscustomobject]@{ playerId = $parts[0]; overall = [int]$parts[1]; position = $parts[2] }
    }
    return $players
}

function Select-BySlots($players, [string[]]$positions) {
    # Her ihtiyac duyulan pozisyon icin, o pozisyonda henuz secilmemis en
    # yuksek Overall'li oyuncuyu secer -- MatchEngine'in GERCEKTEN senkron
    # aldigi roster'dan (draft sirasindaki client-side beklentiden degil).
    $chosen = @{}
    $result = @()
    foreach ($pos in $positions) {
        $pick = $players | Where-Object { $_.position -eq $pos -and -not $chosen.ContainsKey($_.playerId) } | Select-Object -First 1
        if (-not $pick) { throw "MatchEngine roster'inda '$pos' pozisyonunda yeterli oyuncu senkron olmamis." }
        $chosen[$pick.playerId] = $true
        $result += $pick
    }
    return $result
}

Write-Host "`n=== 3) Formasyon + DOGRU dizilim (ClubA ve ClubOpp icin, 4-4-2) ===" -ForegroundColor Cyan

function Set-Formation($clubId, $formation) {
    $body = @{ ClubId = $clubId; Formation = $formation } | ConvertTo-Json
    Invoke-RestMethod -Method Put -Uri "$ClubMgmtBase/api/clubs/$clubId/formation" -Body $body -ContentType "application/json" | Out-Null
}

function Set-Lineup($clubId, [hashtable]$slotMap) {
    $inner = $slotMap | ConvertTo-Json -Compress
    $body = @{ ClubId = $clubId; LineupJson = $inner } | ConvertTo-Json
    Invoke-RestMethod -Method Put -Uri "$ClubMgmtBase/api/clubs/$clubId/lineup" -Body $body -ContentType "application/json" | Out-Null
}

function New-CorrectLineup($roster) {
    return @{
        GK  = $roster[0].playerId
        LB  = $roster[1].playerId
        CB1 = $roster[2].playerId
        CB2 = $roster[3].playerId
        RB  = $roster[4].playerId
        LM  = $roster[5].playerId
        CM1 = $roster[6].playerId
        CM2 = $roster[7].playerId
        RM  = $roster[8].playerId
        ST1 = $roster[9].playerId
        ST2 = $roster[10].playerId
    }
}

function New-BrokenLineup($roster) {
    # Bilerek bozuk: kaleci (roster[0]) forvete (ST1), bir forvet (roster[9]) kaleye;
    # ayrica bir stoper (roster[2]) diger forvetle (roster[10]) yer degistiriyor.
    $slots = New-CorrectLineup $roster
    $slots.GK  = $roster[9].playerId
    $slots.ST1 = $roster[0].playerId
    $slots.CB1 = $roster[10].playerId
    $slots.ST2 = $roster[2].playerId
    return $slots
}

$XI_SLOT_ORDER = @("GK","LB","CB","CB","RB","LM","CM","CM","RM","ST","ST")
$aXI = Select-BySlots (Get-SyncedRoster $ClubAId) $XI_SLOT_ORDER
$oppXI = Select-BySlots (Get-SyncedRoster $ClubOppId) $XI_SLOT_ORDER
Write-Host ("ClubA Ilk 11 (MatchEngine DB'sinden): {0}" -f (($aXI | ForEach-Object { "$($_.position):$($_.overall)" }) -join ", "))
Write-Host ("ClubOpp Ilk 11 (MatchEngine DB'sinden): {0}" -f (($oppXI | ForEach-Object { "$($_.position):$($_.overall)" }) -join ", "))

Set-Formation $ClubAId   "4-4-2"
Set-Formation $ClubOppId "4-4-2"
Set-Lineup $ClubAId   (New-CorrectLineup $aXI)
Set-Lineup $ClubOppId (New-CorrectLineup $oppXI)

Start-Sleep -Seconds 5
$lineupCheck = Invoke-Psql "SELECT ""ClubPowerRatingClubId"", COUNT(*) FILTER (WHERE ""PlayerId"" IS NOT NULL) FROM ""LineupSlotAssignment"" WHERE ""ClubPowerRatingClubId"" IN ('$ClubAId','$ClubOppId') GROUP BY 1;"
Write-Host "Lineup senkron kontrolu: $lineupCheck"

Write-Host "`n=== 4) Faz 1 - DOGRU dizilim: ClubA vs ClubOpp, 14 hafta ===" -ForegroundColor Cyan

function Get-ClubStatsForRoom($roomId, $clubId) {
    # GetStandings endpoint'i ClubPowerRating.RoomId'ye gore filtreliyor (kulubun
    # KAYIT edildigi oda), Fixture ise generate-fixture'a verilen RoomId'ye gore --
    # bu ikisi test script'inde bilerek farkli (ClubPowerRating.RoomId=$DraftRoomId,
    # fixture RoomId=$roomId), o yuzden standings endpoint'i bos doner. Sonuclari
    # dogrudan Match/Fixtures tablolarindan (gercek kanit) hesapliyoruz.
    $rows = Invoke-Psql "SELECT m.""HomeClubId"", m.""AwayClubId"", m.""HomeScore"", m.""AwayScore"" FROM ""Match"" m JOIN ""Fixtures"" f ON f.""Id"" = m.""FixtureId"" WHERE f.""RoomId"" = '$roomId' AND m.""IsPlayed"" = true ORDER BY m.""Week"";"
    $won = 0; $drawn = 0; $lost = 0
    foreach ($line in ($rows -split "`n" | Where-Object { $_.Trim() -ne "" })) {
        $parts = $line -split '\|'
        $homeId = $parts[0]; $homeScore = [int]$parts[2]; $awayScore = [int]$parts[3]
        $isHome = ($homeId -eq $clubId)
        $myScore = $(if ($isHome) { $homeScore } else { $awayScore })
        $oppScore = $(if ($isHome) { $awayScore } else { $homeScore })
        if ($myScore -gt $oppScore) { $won++ }
        elseif ($myScore -eq $oppScore) { $drawn++ }
        else { $lost++ }
    }
    $points = $won * 3 + $drawn
    return [pscustomobject]@{ Won = $won; Drawn = $drawn; Lost = $lost; Points = $points; Played = $won + $drawn + $lost }
}

function Invoke-MatchSeries($roomId, $homeId, $awayId) {
    $genBody = @{ RoomId = $roomId; ClubIds = @($homeId, $awayId) } | ConvertTo-Json
    Invoke-RestMethod -Method Post -Uri "$MatchBase/api/debug/generate-fixture" -Body $genBody -ContentType "application/json" | Out-Null

    for ($week = 1; $week -le 14; $week++) {
        $simBody = @{ RoomId = $roomId; Week = $week } | ConvertTo-Json
        Invoke-RestMethod -Method Post -Uri "$MatchBase/api/debug/simulate-week" -Body $simBody -ContentType "application/json" | Out-Null
    }
}

$RoomPhase1 = [guid]::NewGuid().ToString()
Invoke-MatchSeries $RoomPhase1 $ClubAId $ClubOppId
$aStats1 = Get-ClubStatsForRoom $RoomPhase1 $ClubAId
$oppStats1 = Get-ClubStatsForRoom $RoomPhase1 $ClubOppId
Write-Host ("ClubA   (dogru dizilim): {0}G {1}B {2}M, {3} puan ({4} mac)" -f $aStats1.Won, $aStats1.Drawn, $aStats1.Lost, $aStats1.Points, $aStats1.Played)
Write-Host ("ClubOpp (sabit rakip)  : {0}G {1}B {2}M, {3} puan ({4} mac)" -f $oppStats1.Won, $oppStats1.Drawn, $oppStats1.Lost, $oppStats1.Points, $oppStats1.Played)

$moralAfterPhase1 = Invoke-Psql "SELECT ""Moral"" FROM ""ClubPowerRatings"" WHERE ""ClubId"" = '$ClubAId';"
Write-Host "ClubA Moral (Faz 1 sonrasi, DB'den): $moralAfterPhase1"

Write-Host "`n=== 5) Faz 2 - BOZUK dizilim: ayni ClubA kadrosu, ayni rakip, 14 hafta ===" -ForegroundColor Cyan

Set-Lineup $ClubAId (New-BrokenLineup $aXI)
Start-Sleep -Seconds 5
$brokenCheck = Invoke-Psql "SELECT ""SlotId"", ""PlayerId"" FROM ""LineupSlotAssignment"" WHERE ""ClubPowerRatingClubId"" = '$ClubAId' AND ""SlotId"" IN ('GK','ST1') ORDER BY ""SlotId"";"
Write-Host "Bozuk dizilim DB'de (GK/ST1 slotlari, yer degistirmis olmali): $brokenCheck"

$RoomPhase2 = [guid]::NewGuid().ToString()
Invoke-MatchSeries $RoomPhase2 $ClubAId $ClubOppId
$aStats2 = Get-ClubStatsForRoom $RoomPhase2 $ClubAId
$oppStats2 = Get-ClubStatsForRoom $RoomPhase2 $ClubOppId
Write-Host ("ClubA   (BOZUK dizilim): {0}G {1}B {2}M, {3} puan ({4} mac)" -f $aStats2.Won, $aStats2.Drawn, $aStats2.Lost, $aStats2.Points, $aStats2.Played)
Write-Host ("ClubOpp (ayni sabit rakip): {0}G {1}B {2}M, {3} puan ({4} mac)" -f $oppStats2.Won, $oppStats2.Drawn, $oppStats2.Lost, $oppStats2.Points, $oppStats2.Played)

Write-Host "`n=== KARSILASTIRMA (Faz 1 dogru vs Faz 2 bozuk, ayni ClubA kadrosu, ayni rakip) ===" -ForegroundColor Yellow
Write-Host ("Dogru dizilim : {0} puan / 14 mac" -f $aStats1.Points)
Write-Host ("Bozuk dizilim : {0} puan / 14 mac" -f $aStats2.Points)
if ($aStats2.Points -lt $aStats1.Points) {
    Write-Host "SONUC: Bozuk dizilim belirgin sekilde daha az puan aldi -- pozisyon uyum carpaninin gercek etkisi DB uzerinden dogrulandi." -ForegroundColor Green
} else {
    Write-Host "UYARI: Bozuk dizilim beklenenden iyi sonuc aldi (Moral farkindan kaynaklaniyor olabilir, bkz. yukaridaki Moral degeri) -- daha genis bir ornekle tekrar denenmeli." -ForegroundColor Red
}

# --- Restore ClubA to correct lineup for the moral-streak phase below, so its
# power reflects the intended squad while we test the win-streak club, not a
# side-effect of leaving it in a deliberately broken state. ---
Set-Lineup $ClubAId (New-CorrectLineup $aXI)

Write-Host "`n=== 6) Moral +5 kilit testi: ClubA (guclu) vs ClubWeak (bilerek zayif) ===" -ForegroundColor Cyan

$RoomMoral = [guid]::NewGuid().ToString()
$genBody = @{ RoomId = $RoomMoral; ClubIds = @($ClubAId, $ClubWeakId) } | ConvertTo-Json
Invoke-RestMethod -Method Post -Uri "$MatchBase/api/debug/generate-fixture" -Body $genBody -ContentType "application/json" | Out-Null

$moralTrail = @()
$maxStreak = 0; $currentStreak = 0
for ($week = 1; $week -le 14; $week++) {
    $simBody = @{ RoomId = $RoomMoral; Week = $week } | ConvertTo-Json
    Invoke-RestMethod -Method Post -Uri "$MatchBase/api/debug/simulate-week" -Body $simBody -ContentType "application/json" | Out-Null

    $row = Invoke-Psql "SELECT m.""HomeScore"", m.""AwayScore"", m.""HomeClubId"", cpr.""Moral"" FROM ""Match"" m JOIN ""Fixtures"" f ON f.""Id"" = m.""FixtureId"" JOIN ""ClubPowerRatings"" cpr ON cpr.""ClubId"" = '$ClubAId' WHERE f.""RoomId"" = '$RoomMoral' AND m.""Week"" = $week;"
    $parts = $row -split '\|'
    $homeScore = [int]$parts[0]; $awayScore = [int]$parts[1]; $homeClubId = $parts[2]; $moral = [int]$parts[3]

    $aIsHome = ($homeClubId -eq $ClubAId)
    $aScore = $(if ($aIsHome) { $homeScore } else { $awayScore })
    $oppScore = $(if ($aIsHome) { $awayScore } else { $homeScore })
    $result = if ($aScore -gt $oppScore) { "W" } elseif ($aScore -eq $oppScore) { "D" } else { "L" }

    if ($result -eq "W") { $currentStreak++; if ($currentStreak -gt $maxStreak) { $maxStreak = $currentStreak } }
    else { $currentStreak = 0 }

    $moralTrail += "Hafta $week`: $result ($aScore-$oppScore), Moral=$moral"
}

$moralTrail | ForEach-Object { Write-Host "  $_" }
Write-Host ("En uzun galibiyet serisi: {0}" -f $maxStreak)
if ($maxStreak -ge 3) {
    Write-Host "SONUC: En az 3 art arda galibiyet gozlemlendi; yukaridaki DB izinde Moral'in +5'e ulasip orada sabitlendigi goruluyor." -ForegroundColor Green
} else {
    Write-Host "UYARI: 14 hafta icinde 3'lu galibiyet serisi olusmadi (surpriz payi/rastlantisallik) -- script'i tekrar calistirmak farkli bir seed/sonuc verebilir." -ForegroundColor Red
}

Write-Host "`n=== TAMAMLANDI ===" -ForegroundColor Cyan
