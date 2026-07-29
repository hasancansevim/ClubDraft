# ClubCraft — Proje Spesifikasyonu

## 1. Proje Özeti

ClubCraft, arkadaş gruplarının (4-6 kişi) her birinin bir futbol kulübünün başkanı olduğu, sezonluk ve draft tabanlı bir multiplayer web oyunudur. Oyuncular kulüp yönetimi, kadro oluşturma ve haftalık iş kararları üzerinden rekabet eder. Proje, .NET 8 tabanlı bir microservices mimarisi ile geliştirilir; asıl amaç bir "oyun" üretmek değil, **gerçekçi ölçekte bir mimari CV projesi** ortaya koymaktır (concurrency yönetimi, event-driven iletişim, saga pattern, gerçek zamanlı senkronizasyon).

---

## 2. Oyun Döngüsü (Core Loop)

**Tip:** Sezonluk Draft — kapalı devre, tek sezon oynanır, biter.

### Akış
1. **Oda Kurma** — Host bir oda açar, 4-6 kişi katılır, her biri bir kulüp seçer/oluşturur.
2. **Draft Fazı** — Katılımcılar sırayla oyuncu havuzundan kadrolarını oluşturur (gerçek zamanlı, SignalR ile herkese anlık yansır).
3. **Sezon Fazı** — 10-14 hafta sürer. Her hafta:
   - Fikstüre göre maç oynanır (round-robin, herkes birbiriyle karşılaşır)
   - Katılımcılar haftalık iş kararları alabilir (antrenör kiralama, stadyum yatırımı, moral primi)
   - İtibar eşiğine göre sponsorluk teklifleri gelebilir, kabul/red edilir
   - **Herkes "hazır" (ready) dediğinde** hafta ilerler — senkron ready-check mekanizması
4. **Sezon Sonu** — Lig sıralaması + "Başkanlık Skoru" (lig performansı + itibar + finansal sağlık bileşik skoru) ile final değerlendirme yapılır.

### Lig Yapısı
- Sadece gerçek oyuncular birbirine karşı oynar (bot takım yok)
- 4-6 takımlık mini lig, round-robin fikstür

---

## 3. Oyun Mekanikleri

### 3.1 Kulüp (Club)
- **Bütçe** (başlangıç bütçesi — eşit veya kulüp prestijine göre değişken, TBD)
- **İtibar / Taraftar Skoru**
- **Kadro** (draft ile oluşturulan oyuncu listesi)

### 3.2 Oyuncu (Player) — Basitleştirilmiş Öznitelikler
- **Overall (Genel Güç)** — tek bir sayısal değer
- **Mevki** — GK / DEF / MID / FWD (kaba kategori, detaylı taktik rolleri yok)
- **Yaş**
- **Piyasa Değeri**

> Not: Match Engine'in basit kalması gerektiği için (takım gücü + ağırlıklı random simülasyon), detaylı taktik öznitelikleri (pas, şut, dribling vb.) bilinçli olarak dışarıda bırakılmıştır.

### 3.3 Draft Sistemi
- Sezon başında tek seferlik, sıralı draft
- Sezon içinde serbest transfer market **yok** (kapsam dışı bırakıldı, karmaşıklığı azaltmak için)
- Draft sırasında aynı oyuncuya çakışan seçim → concurrency problemi (mimari açıdan kritik nokta)

### 3.4 Haftalık İş Kararları
Sezon içi bütçe/itibar mekaniğine anlam katmak için:
- **Antrenör/Staff kiralama** — bütçeden düşer, maç gücüne küçük bonus
- **Taraftar etkinliği / stadyum yatırımı** — bütçeden düşer, itibarı artırır
- **Moral/prim ödemesi** — tek seferlik maç öncesi güç bonusu

### 3.5 Sponsorluk
- İtibar eşiği aşılınca sistem otomatik sponsorluk teklifi üretir (event tetikli)
- Başkan teklifi kabul/red eder → kabul ederse bütçeye ek gelir girer

### 3.6 Maç Simülasyonu
- Basit: takım gücü ortalaması + ağırlıklı random
- Karmaşık taktik motoru **yok**
- Sonuç gösterimi: canlı animasyon değil, olay akışı / maç özeti (örn. "23. dakika gol!")

### 3.7 Final Değerlendirme
- **Başkanlık Skoru** = Lig performansı + toplam itibar + finansal sağlık (bileşik skor)

---

## 4. Mimari — Servisler

| Servis | Mimari Stili | Sorumluluk | Veritabanı | Kritik Nokta |
|---|---|---|---|---|
| **Session** | Clean Architecture | Oda kurma, kulüp seçimi, katılımcı yönetimi, ready-check | PostgreSQL | Senkron "herkes hazır" orkestrasyonu |
| **Draft** | Clean Architecture | Draft sırası, oyuncu havuzu, seçim yönetimi | PostgreSQL | **Concurrency kritik**: çakışan oyuncu seçimi, Redis distributed lock |
| **Club Management** | Clean Architecture | Kulüp, kadro, bütçe, haftalık iş kararları (aggregate root) | PostgreSQL | Draft ile Saga ilişkisi (bütçe rezervasyonu) |
| **Match Engine** | Vertical Slice | Fikstür üretimi, haftalık maç simülasyonu | PostgreSQL | Basit tutulmalı, taktik motoru değil |
| **Reputation & Fan** | Event-driven (hafif) | İtibar/taraftar skoru, diğer servislerin event'lerini dinler | PostgreSQL / hafif key-value | Event tüketici |
| **Finance & Sponsorship** | N-Layered | Bütçe hareketleri, event-tetikli sponsorluk teklifleri | PostgreSQL | Event tetikli iş akışı |
| **API Gateway** | YARP | Frontend'in tek giriş noktası | — | — |
| **Realtime Hub** | SignalR | Draft sırası + hafta ilerleme yayını | — (Redis backplane) | Çoklu instance senkronu |

### Servis-arası İletişim
- **RabbitMQ + MassTransit** — asenkron event iletişimi
- **Saga Pattern** (MassTransit State Machine) — Draft ↔ Club Management arası bütçe rezervasyonu / geri alma senaryosu için

### Örnek Domain Event'ler (taslak, detaylandırılacak)
- `PlayerDrafted`
- `WeekAdvanced`
- `MatchSimulated`
- `ReputationThresholdReached`
- `SponsorshipOffered`
- `SponsorshipAccepted`
- `BudgetReserved` / `BudgetReservationFailed` (Saga)

---

## 4.5 Draft Algoritması ve Bütçe Kuralı (Netleşti)

- **Draft tipi:** Snake Draft (1-2-3-4 → 4-3-2-1 → 1-2-3-4 ...) — adil sıra dağılımı için.
- **Draft ücretsiz:** Oyuncu seçmek bütçeden para düşürmez (fantasy-draft mantığı). Bütçe sadece haftalık kararlar ve sponsorluk ile ilişkilidir.
- **Başlangıç bütçesi:** Tüm kulüplere **eşit** (soft-currency, gerçek para değil).

---

## 4.6 Aggregate Root & Domain Event Tasarımı

### Session Servisi
**Aggregate Root: `GameRoom`** — RoomId, HostUserId, Status (Lobby/DraftPhase/SeasonPhase/Completed), Participants[] (ParticipantId, UserId, ClubName, IsReady), CurrentWeek

Domain Events: `RoomCreated`, `ParticipantJoined`, `AllParticipantsReadyForDraft`, `AllParticipantsReadyForNextWeek`, `WeekAdvanced`, `SeasonEnded`

Ready-check mantığı burada yaşar (hem draft başlangıcı hem hafta ilerlemesi için ortak desen).

### Draft Servisi
**Aggregate Root: `DraftSession`** — DraftSessionId, RoomId, Status, TurnOrder[] (snake sıra), CurrentPickIndex, PlayerPool[] (PlayerId, PlayerSnapshot, IsClaimed), Picks[] (PickNumber, ClubId, PlayerId, ClaimedAt)

**Value Object:** `PlayerSnapshot` (Name, Position, Overall, Age, MarketValue — draft anında donmuş kopya)

Domain Events: `DraftStarted`, `PlayerClaimed`, `PlayerClaimRejected` (Reason: sıra değil / zaten alınmış), `DraftTurnAdvanced`, `DraftCompleted`, `PlayerClaimReverted` (Saga compensating)

**Concurrency koruması:** Optimistic concurrency (RowVersion) + CurrentPickIndex kontrolü + Redis distributed lock (aynı draft session için eşzamanlı istekleri serileştirir).

### Club Management Servisi
**Aggregate Root: `Club`** — ClubId, RoomId, PresidentUserId, Name, Budget (Money VO), Roster[] (draft'tan gelen kalıcı kopya), WeeklyDecisions[] (history)

**Sabit haftalık karar kataloğu:** `HireCoach` (maliyet → maç gücü bonusu), `StadiumInvestment` (maliyet → itibar artışı), `MoraleBonus` (maliyet → tek seferlik maç gücü bonusu)

Domain Events: `ClubInitialized` (StartingBudget eşit), `PlayerAddedToRoster`, `PlayerRosterAdditionFailed` (Saga compensating tetikleyici), `WeeklyDecisionMade`, `BudgetDebited` / `BudgetCredited`

### Match Engine Servisi
**Aggregate Root: `Fixture`** — RoomId, Matches[] (Week, HomeClubId, AwayClubId, HomeScore, AwayScore, KeyEvents[])

**Value Object:** `MatchEvent` (Minute, Type=Goal/Card, ClubId)

Domain Events: `FixtureGenerated`, `MatchSimulated`, `WeekSimulationCompleted`

Kulüp gücünü `PlayerAddedToRoster` ve `WeeklyDecisionMade` event'lerini dinleyerek kendi local read-model'inde (ClubPowerRating) hesaplar — ClubManagement'a senkron sorgu atılmaz.

### Reputation & Fan Servisi
**Aggregate Root: `ClubReputation`** — ClubId, Score, History[]

Dinlediği event'ler: `PlayerAddedToRoster`, `WeeklyDecisionMade`, `MatchSimulated`

Domain Events: `ReputationChanged`, `ReputationThresholdReached`

### Finance & Sponsorship Servisi
**Aggregate Root: `SponsorshipOffer`** — OfferId, ClubId, Amount, Status (Pending/Accepted/Rejected/Expired), ExpiresAt

Tetikleyici: `ReputationThresholdReached` dinlenir, otomatik teklif üretilir.

Domain Events: `SponsorshipOffered`, `SponsorshipAccepted`, `SponsorshipRejected`

`SponsorshipAccepted` → Club Management'ta `BudgetCredited` tetikler.

---

## 4.7 Saga: Draft Pick Koordinasyonu

MassTransit State Machine ile uygulanacak somut Saga senaryosu:

```
1. Draft: PlayerClaimed event yayınlanır
2. Saga: ClubManagement'a AddPlayerToRoster komutu gönderir
3a. Başarılı → ClubManagement: PlayerAddedToRoster → Saga tamamlanır
3b. Başarısız (örn. kadro limiti dolu) → ClubManagement: PlayerRosterAdditionFailed
    → Saga, Draft'a ReleasePlayerClaim komutu gönderir (compensating action)
    → Draft: PlayerClaimReverted → oyuncu havuza geri döner, ilgili başkana bildirim gider
```

---

## 4.8 Endpoint Taslakları (Request/Response)

### Session Servisi
```
POST /rooms
  Request:  { hostUserId: string, maxParticipants: int }
  Response: { roomId: string, status: "Lobby" }

POST /rooms/{roomId}/join
  Request:  { userId: string, clubName: string }
  Response: { participantId: string }

POST /rooms/{roomId}/ready
  Request:  { participantId: string, phase: "Draft" | "WeekAdvance" }
  Response: { allReady: bool }

GET /rooms/{roomId}
  Response: { roomId, status, currentWeek, participants: [...] }
```

### Draft Servisi
```
POST /draft-sessions/{roomId}/start
  Response: { draftSessionId, turnOrder: [clubId...] }

GET /draft-sessions/{draftSessionId}/pool
  Response: { players: [{ playerId, name, position, overall, age, marketValue, isClaimed }] }

POST /draft-sessions/{draftSessionId}/claim
  Request:  { clubId: string, playerId: string }
  Response: { success: bool, pickNumber?: int, reason?: string }

GET /draft-sessions/{draftSessionId}/state
  Response: { currentPickIndex, currentClubId, picks: [...] }
```

### Club Management Servisi
```
GET /clubs/{clubId}
  Response: { clubId, name, budget, roster: [...], reputation }

POST /clubs/{clubId}/weekly-decisions
  Request:  { week: int, decisionType: "HireCoach" | "StadiumInvestment" | "MoraleBonus" }
  Response: { success: bool, newBudget: decimal, effect: string }

GET /clubs/{clubId}/decisions-history
  Response: { decisions: [{ week, decisionType, cost, effect }] }
```

### Match Engine Servisi
```
POST /fixtures/{roomId}/generate
  Response: { schedule: [{ week, matches: [{ homeClubId, awayClubId }] }] }

POST /fixtures/{roomId}/weeks/{week}/simulate
  Response: { results: [{ matchId, homeClubId, awayClubId, homeScore, awayScore, keyEvents: [...] }] }

GET /fixtures/{roomId}/standings
  Response: { standings: [{ clubId, played, won, drawn, lost, points }] }
```

### Finance & Sponsorship Servisi
```
GET /clubs/{clubId}/sponsorship-offers
  Response: { offers: [{ offerId, amount, status, expiresAt }] }

POST /sponsorship-offers/{offerId}/respond
  Request:  { decision: "Accept" | "Reject" }
  Response: { success: bool, newBudget?: decimal }
```

---

## 5. Teknoloji Yığını

| Katman | Teknoloji |
|---|---|
| Backend | .NET 8 |
| Mimari Desenler | Clean Architecture, Vertical Slice, CQRS + MediatR, N-Layered (servise göre değişken) |
| Veritabanı | PostgreSQL (database-per-service, her serviste ayrı instance) |
| Mesajlaşma | RabbitMQ + MassTransit |
| Cache / Lock | Redis (distributed lock + SignalR backplane) |
| Gerçek Zamanlı | SignalR |
| Gateway | YARP |
| ORM | EF Core |
| Validasyon | FluentValidation |
| Mapping | Mapster |
| Containerization | Docker Compose |
| Frontend | React veya Angular (backend tamamlanınca kesinleştirilecek — kullanıcının Angular deneyimi var, React de değerlendiriliyor) |

---

## 6. Bilinçli Olarak Kapsam Dışı Bırakılanlar (Scope Cuts)

Projenin CV/mimari odağını korumak için şu karmaşıklıklar bilinçli olarak dışarıda bırakılmıştır:
- Detaylı taktik motoru (pas, şut, dribling bazlı simülasyon)
- Sezon içi serbest transfer market
- Sürekli/kalıcı dünya (çoklu sezon, oyuncu yaşlanması)
- Canlı maç animasyonu (yerine olay akışı/özet kullanılacak)
- Bot takımlarla doldurulmuş büyük ligler

---

## 7. Henüz Netleşmemiş / Sıradaki Adımlar

- [x] Aggregate Root ve Value Object kararları — bkz. §4.6
- [x] Domain Event'lerin tam listesi — bkz. §4.6
- [x] Başlangıç bütçesi modeli — **eşit**, bkz. §4.5
- [x] Draft sırası algoritması — **Snake Draft**, bkz. §4.5
- [x] Endpoint taslakları (Request/Response) — bkz. §4.8
- [x] Saga senaryosu netleşti (Draft Pick koordinasyonu) — bkz. §4.7
- [ ] Frontend framework kesin kararı (React vs Angular) — backend tamamlanınca
- [ ] CI/CD ve deployment stratejisi
- [ ] Player pool'un nereden geleceği (statik seed data mı, JSON dosyası mı, admin panel mi)
- [ ] Kadro limiti (roster cap) — kaç oyuncuya kadar draft yapılabilir

---

*Bu doküman, proje ilerledikçe güncellenecek canlı bir referans dokümanıdır.*
