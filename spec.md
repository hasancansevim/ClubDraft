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

Dokuz servisin tamamı (Draft, ClubManagement, MatchEngine, ReputationFan,
FinanceSponsorship, Session, SagaOrchestrator, RealtimeHub, ApiGateway)
gerçek Docker altyapısında ve Frontend entegrasyonuyla uçtan uca senaryolarla doğrulandı:
- Draft→ClubManagement Saga (concurrency, timeout, compensating action)
- ClubManagement→MatchEngine→ReputationFan→FinanceSponsorship→ClubManagement
  event zinciri (Outbox/Inbox ile atomik ve idempotent)
- Session→Draft otomatik tetikleme (ready-check → snake draft başlatma)
- RealtimeHub üzerinden gerçek zamanlı SignalR event akışı (Frontend'den test edildi)
- API Gateway (YARP) üzerinden CORS, WebSocket upgrade ve IPv6 loopback sorunları çözülerek tam iletişim sağlandı
- Kısa oda kodu (ShortCode) üretimi Session API'ye entegre edildi

**Sıradaki aşama:** Frontend (React) Lobi ve Draft arayüzünün tamamlanması.

## 6. Frontend (Karar Verildi)

- **Framework:** React (Angular deneyimi göz önüne alınarak değerlendirildi, ama
  veri-odaklı ekran ağırlığı, geniş ekosistem ve "backend .NET + frontend React"
  piyasa eşleşmesi nedeniyle React seçildi).
- **Oda Katılım UX'i:** Link + kısa kod ikisi birden.
  - Ana yöntem: paylaşılabilir link (`clubcraft.app/join/{roomId}`), tıklanınca
    otomatik katılım akışına düşer.
  - Yedek yöntem: 6 haneli insan-okur kısa kod (örn. `TIGER42`).
  - **Backend'e gereken ek iş:** Session servisine, `RoomId`'nin yanında kısa
    kod üretme/saklama eklenmeli (Yapıldı ve Entegre Edildi).
- **Sayfa haritası:** Ana Sayfa (kur/katıl) → Lobi (ready-check) → Draft Ekranı
  (canlı, SignalR) → Sezon Dashboard'u (fikstür/kadro/bütçe/haftalık kararlar)
  → Maç Sonuç Ekranı → Sponsorluk Bildirimi → Sezon Sonu/Skor Tablosu.
- **İnşa sırası:** (1) proje iskeleti + routing, (2) API + SignalR entegrasyon
  katmanı (merkezi servis/hook katmanı), (3) sayfa sayfa UI, Lobi'den başlayarak.

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
- [x] Frontend framework kesin kararı (React vs Angular) — backend tamamlanınca
- [ ] CI/CD ve deployment stratejisi
- [ ] Player pool'un nereden geleceği (statik seed data mı, JSON dosyası mı, admin panel mi)
- [ ] Kadro limiti (roster cap) — kaç oyuncuya kadar draft yapılabilir

---

*Bu doküman, proje ilerledikçe güncellenecek canlı bir referans dokümanıdır.*
