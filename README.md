# ClubCraft

Arkadaş gruplarının (4-6 kişi) her birinin bir futbol kulübünün başkanı
olduğu, sezonluk ve draft tabanlı bir multiplayer web oyunu. Asıl amaç bir
"oyun" üretmek değil, **gerçekçi ölçekte bir mimari CV projesi** ortaya
koymak: concurrency yönetimi, event-driven iletişim, saga pattern, gerçek
zamanlı senkronizasyon.

Tüm gereksinim analizi, mimari kararlar, aggregate/domain event tasarımı,
API contract taslakları ve geliştirme sürecinde bulunup çözülen mimari
sorunların kronolojik kaydı için: **[`docs/ClubCraft-Spec.md`](docs/ClubCraft-Spec.md)**.

## Oyun Döngüsü (özet)

Oda kur → kulüp seç → sıralı draft (snake, gerçek zamanlı) → 10-14 haftalık
sezon (her hafta: maç simülasyonu + haftalık iş kararları + sponsorluk) →
sezon sonu Başkanlık Skoru. Detaylar için spec §2-§3.

## Mimari

| Servis | Stil | Sorumluluk | Kritik Nokta |
|---|---|---|---|
| **Session** | Clean Architecture | Oda kurma, kulüp seçimi, ready-check | Senkron "herkes hazır" orkestrasyonu |
| **Draft** | Clean Architecture | Draft sırası, oyuncu havuzu | Redis distributed lock, optimistic concurrency |
| **ClubManagement** | Clean Architecture | Kulüp, kadro, bütçe, haftalık kararlar | Draft ile Saga ilişkisi |
| **MatchEngine** | Vertical Slice | Fikstür üretimi, maç simülasyonu, güç formülü | Bilinçli olarak basit tutulan taktik motoru |
| **ReputationFan** | Event-driven | İtibar/taraftar skoru | Event tüketici |
| **FinanceSponsorship** | N-Layered | Bütçe hareketleri, sponsorluk teklifleri | Event tetikli iş akışı |
| **SagaOrchestrator** | Worker Service | Draft ↔ ClubManagement pick koordinasyonu | MassTransit State Machine |
| **RealtimeHub** | SignalR | Draft + hafta ilerleme yayını | Redis backplane |
| **ApiGateway** | YARP | Frontend'in tek giriş noktası | WebSocket upgrade proxy'leme |

Servisler arası iletişim RabbitMQ + MassTransit (Outbox/Inbox pattern ile
at-least-once teslimat + idempotency); her serviste ayrı bir PostgreSQL
instance'ı (database-per-service).

## Teknoloji Yığını

.NET (C#), PostgreSQL, RabbitMQ + MassTransit, Redis, SignalR, YARP,
EF Core, FluentValidation, Mapster, Docker Compose — backend.
React + TypeScript + Vite — frontend.

## Durum

Backend'in 8 servisi + API Gateway + RealtimeHub, gerçek Docker
altyapısında uçtan uca doğrulandı (Draft→ClubManagement Saga, tam event
zinciri, SignalR üzerinden gerçek zamanlı akış, hepsi API Gateway
üzerinden WebSocket upgrade dahil). Frontend'de tüm akış çalışıyor: Ana
Sayfa → Lobi → Draft → Sezon Dashboard'u (fikstür/kadro/formasyon/bütçe/
haftalık kararlar) → Sponsorluk → Sezon Sonu. Devam eden çalışma ve
bulunan/çözülen tüm hatalar için `docs/ClubCraft-Spec.md`'deki kronolojik
notlara bakın.

## Klasör Yapısı

```
ClubCraft/
├── docs/                        # Spec ve mimari karar günlüğü
├── docker/                      # docker-compose (Postgres x7, RabbitMQ, Redis)
├── frontend/                    # React + TS + Vite
├── tests/                       # Manuel E2E doğrulama script'leri (bkz. tests/README.md)
├── src/
│   ├── ApiGateway/               # YARP tabanlı gateway
│   ├── BuildingBlocks/
│   │   ├── Common/                # Ortak kernel (base entity, enum'lar vb.)
│   │   ├── Contracts/             # Servisler arası paylaşılan event sözleşmeleri
│   │   └── Sagas/                 # MassTransit saga state machine'leri
│   └── Services/
│       ├── Session/               # Oda kurma, katılımcı, ready-check
│       ├── Draft/                 # Draft sırası, oyuncu havuzu
│       ├── ClubManagement/        # Kulüp, kadro, bütçe, iş kararları
│       ├── MatchEngine/           # Fikstür, maç simülasyonu, güç formülü
│       ├── ReputationFan/         # İtibar/taraftar skoru
│       ├── FinanceSponsorship/    # Bütçe hareketleri, sponsorluk
│       ├── RealtimeHub/           # SignalR hub
│       └── SagaOrchestrator/      # Draft↔ClubManagement saga
```

Her servis (RealtimeHub ve SagaOrchestrator hariç) 4 katmanlı:
`Domain / Application / Infrastructure / API`.

## Kendi Makinende Çalıştırma

```powershell
# 1) Altyapı (7x PostgreSQL, RabbitMQ, Redis)
cd docker
docker compose up -d
cd ..

# 2) Tüm backend servisleri (migration kontrolü dahil) + frontend
.\start_services.ps1
```

`start_services.ps1`, servisleri başlatmadan önce `tests/run_migrations.ps1`'i
senkron çalıştırır (eksik migration'ların sessizce atlanıp servislerin
bozuk şemayla ayağa kalkmasını önlemek için), ardından 9 backend servisini
ve frontend'i (`npm run dev`) arka planda başlatır. Loglar `logs/` altında.

Frontend'i tek başına çalıştırmak için: `cd frontend && npm install && npm run dev`.
