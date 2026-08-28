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

> ⚠️ **Idempotency notu (ClubManagement kurulumunda keşfedildi):** RabbitMQ
> at-least-once teslimat garantisi verir, yani bir integration event (örn.
> `PlayerAddedToRosterEvent`) birden fazla kez teslim edilebilir. Event'i
> **üreten** servis kendi state'inde idempotent davransa bile (örn. aynı
> oyuncuyu iki kez roster'a eklemez), event'i **tüketen** her servis
> (Reputation & Fan, Finance & Sponsorship, Match Engine) kendi tarafında da
> "bu event ID'sini daha önce işledim mi" kontrolü yapmalı — MassTransit'in
> **Inbox pattern**'i (Outbox'ın tüketici tarafındaki eşleniği) bunun için
> kullanılacak. Her yeni event tüketici servis yazılırken bu unutulmamalı.

---

## 4.9 Draft Pick Saga — Finalize Edilmiş Tasarım

**Correlation:** `PlayerClaimedEvent` (ve zincirdeki tüm event/command'lar) bir `PickAttemptId` (Guid) taşır — Draft servisinde her `ClaimPlayer()` çağrısında üretilir. Saga bu ID üzerinden correlate eder (`PlayerId` değil — aynı oyuncu farklı zamanlarda tekrar claim edilebileceği için).

**Timeout:** `AddingToRoster` state'ine girerken MassTransit `UseDelayedMessageScheduler` (RabbitMQ delayed exchange, `Dockerfile.rabbitmq` ile etkinleştirilir) ile 30 saniyelik timeout zamanlanır. Başarı/hata event'i gelirse `Unschedule` edilir.

**State Diyagramı:**
```text
(Start)
   |
   | [PlayerClaimedEvent (PickAttemptId)]
   v
[AddingToRoster] -------------------------------------------------------------
   |                                    |                                    |
   | [PlayerAddedToRosterEvent]         | [PlayerRosterAdditionFailedEvent]  | [Timeout Expired (30s)]
   v                                    v                                    v
(Final)                          [RevertingDraftClaim]               [RevertingDraftClaim]
                                        |                                    |
                                        | (Sends ReleasePlayerClaimCommand)  | (Sends ReleasePlayerClaimCommand)
                                        |                                    |
                                        |   <-----------------------------   |
                                        |   | [PlayerAddedToRosterEvent] |   |
                                        |   | (Gecikmiş başarı — race)   |   |
                                        |   | -> ReleasePlayerFromRosterCommand
                                        |   |                            |   |
                                        | [PlayerClaimRevertedEvent]         | [PlayerClaimRevertedEvent]
                                        v                                    v
                                     (Final)                              (Final)
                                        |
                                        | [Geç Gelen / Tekrarlayan Event'ler]
                                        v
                                     (Ignore - sessizce yutulur)
```

**Kritik edge-case (race condition):** `RevertingDraftClaim` state'indeyken gecikmeli bir `PlayerAddedToRosterEvent` gelirse (ClubManagement aslında başarılı olmuş ama geç bildirmiş), saga `ReleasePlayerFromRosterCommand` gönderir — bu, `Club.RemovePlayerFromRoster(playerId)` metodunu tetikleyen gerçek bir düzeltici komuttur (başlangıçta "no-op iskelet" olarak düşünülmüştü, bu senaryo sayesinde gerçek işlevine kavuştu).

**`Final` state'te:** Tüm event'ler `Ignore()` ile açıkça yok sayılır — varsayılan davranışa bırakılmaz.

**Idempotency notu:** RabbitMQ at-least-once teslimat yaptığı için event tüketen her servis (Reputation & Fan, Finance & Sponsorship dahil) kendi tarafında Inbox pattern uygulamalı — bkz. §5 üstündeki uyarı notu.

**Host kararı:** Saga, `SagaOrchestrator` adında bağımsız bir Worker Service'te (API'siz, sadece BackgroundService/Generic Host) host edilir, kendi PostgreSQL instance'ı (`sagaorchestrator-db`) ile saga state'ini (`DraftPickState`) EF Core Saga Repository üzerinden saklar. Draft ve ClubManagement'ın kendi bounded context'lerine bu orkestrasyon sorumluluğu gömülmez.

---

**Doğrulanmış (E2E test edildi):** Draft ↔ ClubManagement ↔ SagaOrchestrator üçgeni, gerçek Docker altyapısı üzerinde uçtan uca test edildi (draft başlat → oyuncu claim et → Saga → roster'a doğru veriyle ekleme). Karşılaşılan ve çözülen gerçek hatalar:
- EF Core, dışarıdan `Guid` ID verilen owned entity'lerde `Insert`'i `Update` sanabilir → `ValueGeneratedNever()` ile düzeltildi.
- Saga state'indeki `Version`/concurrency kolonu PostgreSQL'de `.IsRowVersion()` değil `.IsConcurrencyToken()` (int tipi) ile yapılandırılmalı.
- Domain event → integration event dönüşümünde value object alanları (örn. `PlayerSnapshot`'ın Name/Position/Overall/Age/MarketValue'su) elle map'lenirken unutulabilir — her yeni event eklerken bu mapping'in tam olduğu satır satır kontrol edilmeli.

---

> ⚠️ **Kritik hata notu (Match Engine kurulumu sırasında bulundu):** `ClubManagement.Infrastructure`'da
> domain event'leri integration event'e çevirip Outbox'a yazan bir interceptor
> (`PublishDomainEventsInterceptor` / `DbContext.SaveChangesAsync` override) **eksikti**.
> Sonuç: `PlayerAddedToRosterEvent` hiçbir zaman yayınlanmadı, Saga bunu 30 saniyelik
> timeout ile "başarısız" sandı ve sessizce `ReleasePlayerClaimCommand` gönderdi —
> oysa ClubManagement DB'sinde oyuncu zaten roster'a eklenmişti. Sonuç: iki servis
> arasında sessiz bir tutarsızlık oluştu, ama RabbitMQ kuyrukları "temiz" göründüğü
> için önceki E2E doğrulamamız bunu **yakalayamadı**.
>
> **Test metodolojisi dersi:** "Kuyruklar temiz, hata yok" tek başına yeterli bir
> E2E doğrulama değil. Saga/event-driven akışlarda **her iki tarafın (event üreten
> ve tüketen) aynı gerçeği anlattığı** ayrıca kontrol edilmeli (örn. Draft "claimed"
> diyorsa ClubManagement da "roster'da" demeli — ikisi arasında çapraz sorgu şart).
> Her yeni servis için: DbContext'e domain-event-publish interceptor'ının gerçekten
> bağlı olduğu, ilk E2E testinde zaman damgalı loglarla (timeout'a değil gerçek
> event'e göre ilerlediği) doğrulanmalı.

> ⚠️ **Minimal API + Outbox tuzağı (FinanceSponsorship kurulumunda bulundu):** MediatR
> tabanlı (Controller + CommandHandler) servislerde `IPublishEndpoint` doğal olarak
> Scoped enjekte edilir ve Outbox'a doğru yazar. Ama **Minimal API** (`app.MapPost`)
> kullanılan servislerde, endpoint parametresine doğrudan `IPublishEndpoint` yazmak
> bazen **Singleton/Global Bus** örneğini getirebilir — bu, Outbox'ı tamamen bypass
> edip mesajı doğrudan RabbitMQ'ya gönderir ve DB-transaction atomikliğini bozar.
> **Çözüm:** Minimal API endpoint'lerinde `httpContext.RequestServices.GetRequiredService<IPublishEndpoint>()`
> ile açıkça Scoped instance'ı çek. Yeni bir Minimal-API-tabanlı servis/endpoint
> yazılırken bu mutlaka test edilmeli (Outbox tablosunda INSERT→DELETE log izi
> aranarak doğrulanmalı, sadece "hata yok" yeterli değil).

**Durum (bu not itibarıyla):** Draft, ClubManagement, MatchEngine, ReputationFan,
FinanceSponsorship servislerinin tamamı E2E doğrulanmış ve onaylanmıştır — tam zincir
(Draft→ClubManagement→MatchEngine→ReputationFan→FinanceSponsorship→ClubManagement)
gerçek Docker altyapısında ölçüm ve DB sorgularıyla kanıtlanmıştır. Kalan servisler:
Session, API Gateway, Realtime Hub.

> 💡 **Test tekniği notu (Session kurulumunda kullanıldı):** Çok-servisli bağımlılık
> zincirlerini (örn. MatchEngine → Session) uçtan uca test etmek ağır kalıyorsa,
> ortadaki servisi tam ayağa kaldırmak yerine, hedef servise geçici bir debug
> endpoint'i (`/api/debug/publish-X-event`) ekleyip doğrudan ilgili integration
> event'i RabbitMQ'ya publish ederek tüketen tarafın tepkisini izole test etmek
> geçerli ve hafif bir yöntemdir — DB log'larında beklenen UPDATE/concurrency
> sorgularının göründüğü doğrulanmalı.

**Durum:** Session servisi de E2E doğrulandı (Lobby→Draft geçişi, snake draft
turn order — round sayısı `MaxRosterSize` (20) ile hizalandı, `AdvanceWeek`
zinciri xmin/optimistic-concurrency ile). Kalan: RealtimeHub (SignalR), API Gateway.

> ⚠️ **YARP + SignalR/WebSocket notu (API Gateway kurulumunda bulundu):** YARP
> reverse proxy, varsayılan olarak WebSocket upgrade'i proxy'lemez —
> `Program.cs`'te `app.UseWebSockets()` middleware'i **açıkça** eklenmeli, yoksa
> SignalR bağlantıları gateway üzerinden kurulamaz. Ayrıca `appsettings.json`'daki
> cluster hedef portlarının gerçek servis portlarıyla senkron olduğu ayrıca
> doğrulanmalı — YARP config'i "yazılmış" olması "doğru" olduğu anlamına gelmez,
> gerçek bir SignalR bağlantısı kurup log'da `101 Switching Protocols` yanıtının
> göründüğü teyit edilmeli.

---

## BACKEND TAMAMLANDI (bu not itibarıyla)

Sekiz servisin tamamı (Draft, ClubManagement, MatchEngine, ReputationFan,
FinanceSponsorship, Session, SagaOrchestrator, RealtimeHub) + API Gateway (YARP),
gerçek Docker altyapısında, sentetik olmayan uçtan uca senaryolarla doğrulandı:
- Draft→ClubManagement Saga (concurrency, timeout, compensating action)
- ClubManagement→MatchEngine→ReputationFan→FinanceSponsorship→ClubManagement
  event zinciri (Outbox/Inbox ile atomik ve idempotent)
- Session→Draft otomatik tetikleme (ready-check → snake draft başlatma)
- RealtimeHub üzerinden gerçek zamanlı SignalR event akışı
- Tüm bunların **API Gateway (YARP) üzerinden**, WebSocket upgrade dahil, çalıştığı

**Sıradaki aşama: Frontend.** İlk konuşulacak konu (unutulmaması için not düşüldü):
**oda kodu / davet linki UX'i** — şu an backend sadece ham `RoomId` (Guid)
üretiyor, kullanıcı dostu bir paylaşım mekanizması (kısa kod, link) henüz
tasarlanmadı.

## 6. Frontend (Karar Verildi)

- **Framework:** React (Angular deneyimi göz önüne alınarak değerlendirildi, ama
  veri-odaklı ekran ağırlığı, geniş ekosistem ve "backend .NET + frontend React"
  piyasa eşleşmesi nedeniyle React seçildi).
- **Oda Katılım UX'i:** Link + kısa kod ikisi birden.
  - Ana yöntem: paylaşılabilir link (`clubcraft.app/join/{roomId}`), tıklanınca
    otomatik katılım akışına düşer.
  - Yedek yöntem: 6 haneli insan-okur kısa kod (örn. `TIGER42`).
  - **Backend'e gereken ek iş:** Session servisine, `RoomId`'nin yanında kısa
    kod üretme/saklama eklenmeli (henüz yapılmadı).
- **Sayfa haritası:** Ana Sayfa (kur/katıl) → Lobi (ready-check) → Draft Ekranı
  (canlı, SignalR) → Sezon Dashboard'u (fikstür/kadro/bütçe/haftalık kararlar)
  → Maç Sonuç Ekranı → Sponsorluk Bildirimi → Sezon Sonu/Skor Tablosu.
- **İnşa sırası:** (1) proje iskeleti + routing, (2) API + SignalR entegrasyon
  katmanı (merkezi servis/hook katmanı), (3) sayfa sayfa UI, Lobi'den başlayarak.

### 6.1 Görsel Tasarım Sistemi (Karar Verildi)

**Stil yönü:** Modern spor dashboard'u (FIFA Ultimate Team / Football Manager esintili), kart-temelli arayüz.

- **Zemin:** Çok koyu lacivert/siyah (`#0A0E17` civarı)
- **Kart yüzeyi:** Bir ton açık (`#131826` civarı), ince border, yumuşak köşe (12-16px)
- **Vurgu rengi:** Elektrik yeşil (`#39FF88` civarı) — pozitif aksiyon/aktif durum; kritik/uyarı için ayrı bir turuncu/kırmızı ton
- **Metin:** Kırık beyaz ana metin, gri-mavi ikincil metin
- **Tipografi:** Başlıklarda sportif/dar bir font (Rajdhani/Orbitron/Barlow Condensed ailesi), gövdede Inter
- **Oyuncu/kulüp kartları:** Overall değeri büyük ve belirgin, mevki rozetleri renk kodlu (GK/DEF/MID/FWD), hover'da hafif yükselme/glow
- **Butonlar:** Dolgun, vurgu renginde, hafif glow efekti

Bu tasarım sistemi, ilk olarak global bir tema (CSS variables/design tokens) olarak kurulup, mevcut placeholder sayfalara uygulanacak.

### 6.2 Frontend İlerleme Durumu (güncel)

**Tamamlanan:**
- Proje iskeleti (Vite + React + TS), routing, tasarım sistemi (§6.1) uygulandı
- Ana Sayfa: "Oda Kur" ve "Odaya Katıl" (kısa kod), gerçek API'ye bağlı
- API client katmanı (`src/api/sessionApi.ts`) ve SignalR hook'u (`src/hooks/useSignalR.ts`)
- **Lobi ekranı çalışıyor:** kısa kod → gerçek RoomId çözümleme, kulüp adıyla katılma,
  katılımcı listesi SignalR ile birden fazla tarayıcı sekmesi arasında canlı senkron
  (gerçek iki-sekme testiyle doğrulandı)

> ⚠️ **Not (Lobi kurulumunda karşılaşıldı):** "Katıl" isteği tarayıcıda 400 hatası
> gösterdi ama katılımcı aslında eklenmişti (sayfa yenilenince görünüyordu). Kök
> sebep netleştirilip düzeltildi. Ders: "sayfa yenileyince çalışıyor" gibi
> belirtiler gerçek bir hatayı gizleyebilir (yanlış status code, response/state
> tutarsızlığı gibi) — sadece "sonuçta çalışıyor" deyip geçmemeli, network
> payload'ı ile asıl sebep görülmeli.

**Sırada:**
1. ~~"Hazırım" butonu~~ ✅ Tamamlandı — canlı senkron doğrulandı
2. ~~Draft ekranı~~ ✅ Tamamlandı — iki gizli pencereyle gerçek zamanlı seçim/sıra
   güncelleme kanıtlandı (ekran görüntüleriyle)
3. **ClubId teknik borcunun kapatılması** (yukarıdaki nota bkz.) — Sezon
   Dashboard'a geçmeden önce yapılmalı, çünkü bütçe/roster gerçek `Club`
   verisine bağlı olacak
4. Sezon Dashboard, Sponsorluk, Özet ekranları
5. UI/UX zenginleştirme: arama/filtre/sıralama (oyuncu havuzu), native
   `alert()` yerine tasarım sistemine uygun toast/modal, genel görsel
   zenginlik — kullanıcı bunu bilinçli olarak "önce çekirdek döngü" sonrasına
   erteledi

> 🚨 **Ciddi regresyon notu (frontend Lobi entegrasyonunda git geçmişi taranarak
> bulundu):** Bir önceki oturumda, RealtimeHub E2E testindeki bir zamanlama
> (timing) sorununu "çözmek" için Session servisinde `UseBusOutbox()` bilinçli
> olarak kaldırılmış ve Minimal API endpoint'leri Scoped `IPublishEndpoint`
> yerine global `IBus`/`IPublishEndpoint` kullanacak şekilde değiştirilmişti —
> yani atomiklik garantisi tamamen feda edilmişti, hem de **kritik** bir event
> için (`AllParticipantsReadyForDraftEvent`, Draft servisini tetikleyen).
> Bu, hiçbir E2E testte fark edilmemişti çünkü "event nihayetinde gitti"
> (asenkron/best-effort) ile "event atomik ve garantili gitti" arasındaki farkı
> mevcut testler ayırt etmiyordu. **Ders:** Outbox/atomiklik ile ilgili herhangi
> bir kod her kaldırıldığında/bypass edildiğinde (commit mesajında "timing",
> "geçici çözüm" gibi ifadeler varsa özellikle) bu mutlaka ayrıca sorgulanmalı
> — "test geçti" tek başına yeterli kanıt değil. Şüpheli bir davranış
> görüldüğünde `git log -S` ile arama, kök sebebi bulmakta çok etkili oldu.

> 📌 **Bilinen teknik borç (frontend Draft entegrasyonunda tespit edildi):**
> Draft servisi şu an `TurnOrder`/`ClubId` olarak Session'ın ürettiği ham
> `ParticipantId`'yi kullanıyor — `ClubManagement`'ın gerçek `Club.ClubId`'si
> hiç devreye girmiyor (Lobi'de katılırken ClubManagement'a `InitializeClub`
> çağrısı yapılmıyor). Bu, frontend'i çalışır kılmak için bilinçli olarak
> şimdilik kabul edilen bir kısayol. **Sonuç:** Draft'ta seçilen oyuncular
> şu anki haliyle gerçek bir `Club` aggregate'ine bağlanamaz — Saga
> (`AddPlayerToRosterCommand`) böyle bir `ClubId` bulamayıp timeout'a düşer.
> **Yapılacak (frontend akışı bittikten sonra):** Lobi'de "Katıl" akışına
> ClubManagement'ın `InitializeClub` çağrısını ekleyip, dönen gerçek `ClubId`'yi
> Session'a/Draft'a taşıyan bir düzeltme turu planlanmalı.

> 📌 **Oyuncu havuzu veri kaynağı notu:** Draft servisinin oyuncu havuzu, Kaggle'daki
> "EA Sports FC 24 Complete Player Dataset" (sofifa.com'dan derlenmiş, topluluk
> kaynaklı) baz alınarak oluşturuldu — 8 lig (Premier League, La Liga, Serie A,
> Bundesliga, Ligue 1, Süper Lig, Primeira Liga, Saudi Pro League), overall
> eşiği Saudi Pro League için ≥78, diğerleri için ≥68. Bu, gerçek oyuncu
> isimleri/verileri içerir — bilinçli bir risk kabulüyle kullanılmıştır
> (kişisel/portfolyo projesi, ticari amaç yok). Yayınlarken/paylaşırken bu
> şeffaf şekilde belirtilmeli.

> 🚨 **Saga timeout / ortam-duyarsız sabit değer notu (frontend Draft turlarında bulundu):**
> `DraftPickStateMachine`'deki 30 saniyelik sabit `s.Delay` (Saga timeout), geliştirme
> ortamındaki sık servis yeniden başlatmaları + soğuk RabbitMQ/DB bağlantıları
> yüzünden **erken tetikleniyordu** — tamamlanmış pick'ler `RevertClaim` ile geri
> alınıyor, kullanıcıya "kadro sıfırlandı, oyuncu tekrar seçilebilir oldu" olarak
> yansıyordu. **Kanıt:** SagaOrchestrator DB'sinde 50+ kayıt `RevertingDraftClaim`
> state'inde takılı bulundu. **Kalıcı çözüm:** Timeout süresi `appsettings.json`'a
> taşındı (`DraftPick:PickTimeoutSeconds`), production'da 30s, development'ta 120s.
> **Ders:** Saga/timeout gibi zamanlamaya dayalı sabitler asla hardcode edilmemeli,
> ortam bazlı konfigüre edilebilir olmalı — geliştirme ortamının doğası
> (sık restart, soğuk başlangıç) production'dan farklıdır.

**Durum:** Draft ekranı (arama/filtre/sıralama, kadro sayacı, lineup sürükle-bırak,
sayfa yenileme sonrası state korunumu, çoklu-istemci senkronizasyonu, Saga
timeout düzeltmesi) uçtan uca doğrulandı ve **tamamlandı**. ClubId akışı
zaten kapatılmıştı.

> 🎯 **Kök sebep bulundu ve kapatıldı (birkaç günlük tekrarlayan Draft bug'larının
> gerçek ortak kökeni):** `ClubManagement` ve `Draft` servislerinde birden fazla
> `ReceiveEndpoint`'te `UseEntityFrameworkOutbox<...>(context)` çağrısı **eksikti**
> (`club-management-commands`, `draft-commands`, `draft-events`). Sonuç: bu
> endpoint'lerden yayınlanan integration event'ler (`PlayerAddedToRosterEvent`
> dahil) Outbox'ı bypass ediyordu, Saga bunları hiç göremiyor, her pick 120s
> timeout'a düşüp `RevertClaim` ile geri alınıyordu — "kadro sıfırlanıyor,
> aynı oyuncu tekrar seçilebiliyor" olarak yansıyordu. **Sistematik tarama**
> (tüm servislerin tüm `ReceiveEndpoint`'leri tek tek listelenip Outbox
> varlığı kontrol edilerek) ile bulundu ve düzeltildi. `DraftSession.RevertClaim()`
> tasarımı (Insert/"makeup pick", index'e dokunma) doğru olduğu teyit edilip
> korundu. **Doğrulama:** İki kulüp de 20/20 kadroyu, hiçbir deadlock
> yaşanmadan tamamladı (ekran görüntüsüyle kanıtlandı).
> **Ders:** Bir serviste bir endpoint'te unutulan bir konfigürasyon
> (Outbox gibi), o servisin **diğer** endpoint'lerinde de unutulmuş olabilir —
> bulunca hemen tüm servisler için sistematik tarama yapılmalı, sadece
> bulunan yeri düzeltip geçilmemeli.

**UI Polish turu:** Debug paneli kaldırıldı (artık gerekmiyor, kök sebep
bulunduğu için), `alert()` → Toast sistemi, FUT-style oyuncu kartları,
Orbitron/Rajdhani font sistemi, glassmorphism/animasyonlar eklendi.

**Sezon Dashboard — TAMAMLANDI:**
- **1. Faz** — Üst özet şeridi (Bütçe/İtibar/Hafta/Sıralama), Kadro
  (saha+yedekler görünümü), Haftalık Kararlar paneli (HireCoach/
  StadiumInvestment/MoraleBonus) — doğrulandı (bütçe/itibar DB ile eşleşiyor,
  bir enum mapping hatası bulunup düzeltildi).
- **2. Faz** — "Hazırım" akışı, maç simülasyonu, hafta ilerletme, lig tablosu —
  Playwright ile gerçek bir tarayıcıda iki ayrı browser context (iki istemci)
  kullanılarak otomatik uçtan uca test edildi: draft API üzerinden tamamlandı
  (40 pick), her iki istemci de Season Dashboard'a düştü, ikisi de "Hazırım"
  butonuna bastı, maç simülasyonu tetiklendi. DB'den doğrulandı: `Match`
  tablosunda `IsPlayed=true`, skor 3-0; itibar skorları maç sonucuna göre
  güncellendi (kazanan 35→38, kaybeden 35→25).

Sonra: Sponsorluk, Özet ekranları.

> 💡 **İki ders (Sezon Dashboard Faz 2 testinde bulundu, 2026-08-28):**
> 1. **shortCode/RoomId karışıklığı tekrarlandı** — Lobi/Draft'ta çözdüğümüz
>    "URL'deki kısa kodu API çağrılarında doğrudan kullanma" hatası Sezon
>    Dashboard'da da yapılmıştı: `SeasonDashboard.tsx`'in `handleReadyClick`'i
>    `roomId` (aslında short code, örn. `TIGER42`) ile
>    `POST /api/sessions/{id:guid}/ready` çağırıyordu — route `:guid`
>    constraint'i taşıdığı için bu istek her zaman 404 dönüyordu. **Ders:**
>    Yeni bir sayfa/ekran eklenirken, "kısa kod → gerçek RoomId çözümleme"
>    adımının (Lobi/Draft'taki `realRoomId` deseni) o sayfada da uygulandığı
>    açıkça kontrol edilmeli — bu bir kerelik düzeltme değil, her yeni route
>    için tekrar edilmesi gereken bir kalıp. Düzeltme, iki-istemci Playwright
>    testiyle doğrulandı (bkz. yukarı).
> 2. **Migration'ların "servis ayakta = migration uygulanmış" varsayımı
>    yanlıştı** — 6 servisin (Session, Draft, FinanceSponsorship, MatchEngine,
>    ReputationFan, SagaOrchestrator) DB'sinde migration hiç uygulanmamışken
>    bile servisler hatasız başlıyordu (`OutboxState` tablosu olmadan, arka
>    planda sessizce hata veriyorlardı). **Kalıcı çözüm:** `start_services.ps1`
>    artık servisleri başlatmadan önce `tests/run_migrations.ps1`'i (tüm
>    servisler için `dotnet ef database update`) senkron çalıştırıyor,
>    başarısız olursa servisleri hiç başlatmıyor.

> ⚠️ **Açık konu — düşük öncelik (Sezon Dashboard testinde gözlemlendi):**
> Draft/ClubManagement arasındaki "geç gelen başarı" race condition koruması
> (bkz. §4.9 — `RevertingDraftClaim` state'inde geç `PlayerAddedToRosterEvent`
> gelirse `ReleasePlayerFromRosterCommand` ile telafi) soğuk başlangıçta
> (servisler yeni ayağa kalkarken) tetiklenmemiş gibi görünüyor — Draft ve
> ClubManagement roster sayıları geçici olarak ayrışmıştı (19 vs 20).
> `start_services.ps1` ile servisleri sıcak tutmak bunu pratikte nadirleştiriyor.
> **Zaman bulununca:** `DraftPickStateMachine`'deki `RevertingDraftClaim` state
> handler'ının gerçekten `ReleasePlayerFromRosterCommand` gönderdiğini
> doğrulamak/düzeltmek gerekiyor. Bloklayıcı değil, sonraki fazları engellemiyor.

**Commit'ler (2026-08-28):** Bu iki düzeltme bilinçli olarak ayrı commit'lere
bölündü, çünkü nedenleri farklı ("bug fix" vs "kalıcı altyapı iyileştirmesi")
ve git geçmişinde ayrı ayrı okunabilir olmalı:
- `fix(frontend): resolve real RoomId on Season Dashboard before API calls`
- `feat(scripts): auto-run EF migrations before starting services`

**Sponsorluk ve Özet ekranları — TAMAMLANDI (2026-08-28):**
- **Sponsorluk** (`/sponsorship/:shortCode`) — bekleyen tekliflerin kartlar
  halinde gösterimi, kabul/red akışı, kabul sonrası bütçenin taze veriyle
  anında güncellenmesi, geçmiş kararlar listesi. Playwright ile hem kabul
  hem red akışı uçtan uca doğrulandı (DB'den bütçe artışı teyit edildi).
- **Özet** (`/summary/:shortCode`) — Başkanlık Skoru = (Lig Puanı × 10) +
  İtibar Skoru + (Bütçe ÷ 50.000), tüm kulüpler için hesaplanıp sıralanıyor;
  sezon bitince (`CurrentWeek >= 14`) lider "🏆 Şampiyon" olarak vurgulanıyor.

> 🎯 **Kök sebep bulundu ve kapatıldı (Sponsorluk/Özet ekranları için standings
> doğrulanırken ortaya çıktı):** `IPlayerAddedToRosterEvent` hiç `RoomId`
> taşımıyordu. MatchEngine'in `PlayerAddedToRosterCommandConsumer`'ı,
> `ClubPowerRating`'i lazy-create ederken `RoomId` için sabit `Guid.Empty`
> kullanıyordu — `GetStandingsQueryHandler`'ın oda bazlı filtrelediği sorgu
> hiçbir zaman eşleşmiyordu, **standings her zaman boş dönüyordu**. Bu, Season
> Dashboard'daki lig tablosunun ta baştan beri sessizce boş görünmesine sebep
> olmuştu ama fark edilmemişti (o testte lig tablosu odak noktası değildi).
> Zincir boyunca düzeltildi: `Club` aggregate (RoomId zaten vardı) →
> `PlayerAddedToRosterEvent` domain event → `IPlayerAddedToRosterEvent`
> contract → iki ayrı publish noktası → MatchEngine consumer artık
> `msg.RoomId` kullanıyor. **Bonus tespit:** `ClubRepository.SaveAsync` ve
> `PublishDomainEventsInterceptor`, aynı domain event'leri entegrasyon
> event'ine çevirmek için birbirinden bağımsız iki paralel switch-case
> barındırıyor — biri (`ClubRepository`) fiilen çalışıyor (domain event'leri
> `SaveChangesAsync`'ten önce temizliyor), diğeri (interceptor) bu akış için
> hiçbir zaman tetiklenmiyor. İleride tek bir mekanizmaya birleştirilmeli.
> **Ders:** Bir integration event contract'ı tasarlanırken, event'i tüketen
> her servisin ihtiyaç duyacağı alanların (burada RoomId) baştan düşünülmesi
> gerekiyor — eksik bir alan, tüketen tarafta sessizce yanlış bir varsayılan
> değere (`Guid.Empty` gibi) düşebilir ve bu, ilgili özelliği görünür kılan
> bir ekran inşa edilene kadar fark edilmeyebilir.

> 💡 **RealtimeHub kapsam notu:** Sponsorluk teklifi oluşturma zinciri
> (`ReputationThresholdReachedEventConsumer`) yeni bir integration event
> yayınlamıyor, dolayısıyla RealtimeHub'da `SponsorshipOffered` diye bir
> consumer/event de yok. Sponsorluk ekranı bu yüzden SignalR yerine kısa
> aralıklı polling kullanıyor. İstenirse ileride gerçek bir event zinciri
> (FinanceSponsorship → RealtimeHub → `onSponsorshipOffered`) eklenebilir.

### 4 haftalık ilerletme testinde bulunan 3 bug (2026-08-28)

**Bug A — Hafta ilerleme/maç simülasyonu tutarsız — İKİ bağımsız kök sebep:**
1. `RoundRobinFixtureGenerator` sadece **tek** bir round-robin turu üretiyordu
   (`teams.Count - 1` hafta) — 2 kulüp için bu tam 1 hafta demekti, oysa
   sezon 10-14 hafta sürüyor. Round bitince o haftalar için hiç `Match`
   satırı kalmıyordu. **Düzeltme:** `SeasonLengthWeeks=14`'e kadar döngü
   tekrarlanıyor (double/triple round-robin gibi).
2. `GameRoom.MarkReady(WeekAdvance)`, `AllParticipantsReadyForNextWeekEvent`'i
   yayınladıktan sonra ready flag'lerini sıfırlamıyor ve `CurrentWeek`'i hemen
   ilerletmiyordu — bu, MatchEngine'in simülasyonu bitirip
   `WeekSimulationCompletedEvent` ile bildirmesini bekleyen asenkron bir adım
   (`AdvanceWeek()`'te gerçekleşiyor). Bu pencerede `ready()` tekrar
   çağrılırsa (hızlı üst üste tıklama, network gecikmesi), event **aynı**
   hafta numarasıyla ikinci kez ateşleniyor; MatchEngine o haftayı tekrar
   simüle etmeye çalışıyor (maç zaten oynanmış, hiçbir şey yapmıyor) ama her
   iki `WeekSimulationCompletedEvent` de kendi `AdvanceWeek()`'ini
   tetiklediği için `CurrentWeek` net 2 artıyor — bir hafta hiç maç
   oynanmadan atlanıyor. **Düzeltme:** yeni `WeekAdvancePending` flag'i
   (migration gerektirdi), bir hafta ilerletme "uçuştayken" yenisinin
   tetiklenmesini engelliyor. `SimulateMatchesForWeekCommandHandler`'a da
   savunma amaçlı bir uyarı logu eklendi (fikstürde o hafta için hiç maç
   yoksa artık sessizce geçilmiyor).
   **Doğrulama:** rapid-fire iki ready-check döngüsü sonrası DB'de Week 1
   VE Week 2'nin ikisi de `IsPlayed=true`, hiçbir "hiç maç yok" uyarısı
   tetiklenmedi.

**Bug B — Türkçe karakter bozukluğu:** `index.html`'de `<meta charset=UTF-8>`
zaten vardı, API yanıtları da zaten `charset=utf-8` dönüyordu — config
eksikliği değildi. `SeasonDashboard.tsx`'te 5 string, önceki bir "UI Polish"
turunda bozuk encoding ile kaydedilmiş, U+FFFD/literal `??` olarak **geri
kurtarılamaz** şekilde kaybolmuştu. Metinler elle doğru Türkçe'ye geri
yazıldı (transkripsiyon değil, çünkü orijinal baytlar kalıcı olarak yoktu).

**Bug C — Sponsorluk hiç tetiklenmiyor:** `ClubReputation.AddReputation()`,
eşik aşıldığında `ReputationThresholdReachedEvent`'i aggregate'in dahili
domain event listesine ekliyor ama bunu okuyup publish etmek **çağıranın**
sorumluluğunda (`ClubReputationRepository.SaveAsync` bunu kendisi yapmıyor).
Üç tüketiciden sadece `MatchSimulatedEventConsumer` bunu doğru yapıyordu;
`PlayerAddedToRosterEventConsumer` ve `WeeklyDecisionMadeEventConsumer` ise
domain event'leri hiç okumadan direkt `SaveAsync`'e geçiyordu — event
sessizce kayboluyordu. Hasan FC vakasında itibar roster-ekleme + haftalık
karar bonuslarından 50'yi geçmişti, tam da bu iki kırık yoldan. **Bonus
tespit:** `WeeklyDecisionMadeEventConsumer`'daki `msg.Type == 1` kontrolü
StadiumInvestment sanılıyordu ama gerçek enum'da (`HireCoach=1,
StadiumInvestment=2`) bu HireCoach'a karşılık geliyordu — düzeltildi
(`Type==2`). **Doğrulama (manuel DB müdahalesi OLMADAN, tamamen organik):**
yeni bir kulüp roster + `StadiumInvestment` kararlarıyla itibarı 51'e
ulaştırdı → Finance loglarında "Created sponsorship offer ... threshold 50"
→ `GET /api/finances/{clubId}/offers` gerçek bir Pending teklif döndürdü →
Sponsorluk ekranında ekran görüntüsüyle kanıtlandı, kabul sonrası bütçenin
DB'de arttığı teyit edildi.

**Ek iyileştirmeler:** Match Engine'e `/api/matches/{roomId}/fixture`
endpoint'i eklendi; Sezon Dashboard'a **Maç Geçmişi** (kullanıcının kendi
kulübünün oynadığı maçlar: hafta/rakip/skor/G-B-M) ve **Fikstür** (önümüzdeki
haftaların tüm eşleşmeleri) panelleri eklendi. Ayrıca sponsorluk kabul
akışında bir eventual-consistency detayı bulundu: bütçe kredisi
`ISponsorshipAcceptedEvent` üzerinden asenkron işlendiği için tek seferlik
hemen-sonrası fetch bazen krediden önce yetişip eski bütçeyi gösterebiliyordu
— polling ve kısa gecikmeli tekrar denemelerle düzeltildi.

### Pozisyon sistemi detaylandırma + formasyon seçimi (2026-08-28)

**Kaba kategoriden (GK/DEF/MID/FWD) detaylı pozisyona geçildi.** Yeni
`PlayerPosition` enum'ı (`BuildingBlocks.Common.Enums` — Draft.Domain ve
ClubManagement.Domain zaten bu projeye referans veriyordu, iki ayrı enum'un
elle senkron tutulma riskinden kaçınmak için tek yerde tanımlandı) 15 değer
taşıyor: `GK, CB, RB, LB, RWB, LWB, CDM, CM, CAM, RM, LM, RW, LW, ST, CF`.
JSON'da her zaman **string** olarak serialize ediliyor
(`[JsonConverter(JsonStringEnumConverter)]`) — `WeeklyDecisionType`'ta
yaşanan servisler-arası int-değeri karışıklığı burada tekrarlanmasın diye.
Değişen katmanlar: `PlayerSnapshot`, `Player`, `IPlayerClaimedEvent`,
`IAddPlayerToRosterCommand`, tüm ilgili DTO'lar; DB kolonları
`.HasConversion<string>()` ile okunabilir tutuldu.

`process_players.py`, CSV'nin `player_positions` alanının birincil kodunu
artık kaba kategoriye indirgemeden birebir kullanıyor.
`draft-player-pool.json` 3149 oyuncuyla yeniden üretildi (hiçbiri
atlanmadı): CB 591, ST 406, CM 381, GK 289, CDM 276, RB 214, LB 211,
CAM 172, RM 165, LM 133, RW 84, LW 82, RWB 57, CF 46, LWB 42.

Geliştirme verisi tamamen sıfırlandı (`docker compose down -v` + `up -d`),
tüm servislerin migration'ları bu değişiklikle birlikte yeniden uygulandı.

**Formasyon seçimi eklendi.** `Club.Formation` alanı (varsayılan `4-4-2`),
`PUT /api/clubs/{clubId}/formation` ile değiştiriliyor — formasyon
değişince `LineupJson` bilinçli olarak sıfırlanıyor (eski slot ID'leri yeni
formasyonda anlamlı olmayabilir). Sezon Dashboard'da "İlk 11" başlığının
yanına bir dropdown eklendi; 4 formasyon (**4-4-2, 4-3-3, 4-2-3-1, 3-5-2**)
arasında seçim yapılabiliyor, her biri farklı slot sayısı/dağılımı taşıyor
(örn. 3-5-2'de 3 CB + 5 orta saha). Draft filtre sekmeleri de 5 kaba
kategoriden 16 detaylı koda genişletildi (örn. CB'yi DEF'ten ayrı
filtreleyebilmek için — farklı formasyonların farklı slot ihtiyaçları
olduğu için önemli).

**Doğrulama (Playwright, gerçek tarayıcı):** Sıfırdan bir draft tamamlandı,
draft pool'un ve roster'ın detaylı pozisyonları doğru döndürdüğü API'den
teyit edildi; Draft ekranında 16 filtre sekmesi (CDM filtresi tek başına
25 CDM oyuncusunu doğru listeledi) ve renk-kodlu rozetler ekran
görüntüsüyle kanıtlandı; Sezon Dashboard'da formasyon 4-4-2'den 4-3-3'e
değiştirilip sahanın doğru slotlarla yeniden çizildiği, backend'de
`Formation` alanının kalıcı olduğu API'den teyit edildi. Hiçbir yerde eski
GK/DEF/MID/FWD kalıntısı (badge metni olarak) görünmüyor — renk gruplaması
iç uygulama detayı olarak kaldı, kullanıcıya her zaman tam detaylı kod
gösteriliyor.

## 5. Teknoloji Yığını

> ⚠️ **Önemli sürüm notu (Draft servisi kurulumunda keşfedildi):** MassTransit
> 8.3+ ve 9.x sürümleri lisans anahtarı gerektiriyor (açık kaynak kullanım
> için bile). **Tüm servislerde MassTransit paketleri `8.2.2` sürümüne
> sabitlenmeli** (`MassTransit`, `MassTransit.RabbitMQ`, `MassTransit.EntityFrameworkCore`).
> Ayrıca connection string'lerde `localhost` yerine `127.0.0.1` kullanılmalı
> (Windows'ta IPv6 çözümleme sorunlarını önlemek için).

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
