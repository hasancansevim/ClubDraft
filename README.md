# ClubCraft

Arkadaş gruplarının futbol kulübü başkanlığı yaptığı, sezonluk draft tabanlı multiplayer web oyunu.
Microservices mimarisi ile geliştirilen bir CV/portfolyo projesidir.

Detaylı gereksinim ve mimari dokümanı için: [`docs/ClubCraft-Spec.md`](docs/ClubCraft-Spec.md)

## Klasör Yapısı

```
ClubCraft/
├── docs/                        # Spec, mimari kararlar, ADR'ler
├── docker/                      # docker-compose ve servis bazlı Dockerfile'lar
├── src/
│   ├── ApiGateway/               # YARP tabanlı gateway
│   ├── BuildingBlocks/
│   │   ├── Common/                # Ortak kernel (base entity, result pattern vb.)
│   │   ├── Contracts/             # Servisler arası paylaşılan event/mesaj sözleşmeleri
│   │   └── Messaging/             # MassTransit/RabbitMQ ortak konfigürasyon
│   └── Services/
│       ├── Session/               # Oda kurma, katılımcı, ready-check (Clean Architecture)
│       ├── Draft/                 # Draft sırası, oyuncu havuzu (Clean Architecture)
│       ├── ClubManagement/        # Kulüp, kadro, bütçe, iş kararları (Clean Architecture)
│       ├── MatchEngine/           # Fikstür ve maç simülasyonu (Vertical Slice)
│       ├── ReputationFan/         # İtibar/taraftar skoru (Event-driven, hafif)
│       ├── FinanceSponsorship/    # Bütçe hareketleri, sponsorluk (N-Layered)
│       └── RealtimeHub/           # SignalR hub (draft + hafta ilerleme yayını)
```

Her servis klasörü altında (RealtimeHub hariç) 4 katman placeholder'ı var:
`Domain / Application / Infrastructure / API`

## Kurulum (Kendi Makinende)

Bu iskelet sadece klasör yapısını içerir; gerçek .NET proje dosyaları (.sln, .csproj)
henüz oluşturulmadı çünkü bu ortamda .NET SDK yok. Kendi makinende aşağıdaki adımları
izleyerek gerçek projeleri oluşturabilirsin:

```bash
# 1) Solution oluştur
dotnet new sln -n ClubCraft

# 2) Her servis için katman projelerini oluştur (örnek: Draft servisi)
cd src/Services/Draft
dotnet new classlib -n ClubCraft.Draft.Domain -o Domain
dotnet new classlib -n ClubCraft.Draft.Application -o Application
dotnet new classlib -n ClubCraft.Draft.Infrastructure -o Infrastructure
dotnet new webapi -n ClubCraft.Draft.API -o API
cd ../../..

# 3) Projeleri solution'a ekle
dotnet sln add src/Services/Draft/Domain/ClubCraft.Draft.Domain.csproj
dotnet sln add src/Services/Draft/Application/ClubCraft.Draft.Application.csproj
dotnet sln add src/Services/Draft/Infrastructure/ClubCraft.Draft.Infrastructure.csproj
dotnet sln add src/Services/Draft/API/ClubCraft.Draft.API.csproj

# ... aynı pattern diğer servisler için tekrarlanır (Session, ClubManagement,
#     MatchEngine, ReputationFan, FinanceSponsorship)

# 4) ApiGateway ve RealtimeHub için
cd src/ApiGateway && dotnet new web -n ClubCraft.ApiGateway && cd ../..
cd src/Services/RealtimeHub && dotnet new web -n ClubCraft.RealtimeHub && cd ../../..
```

> Not: `setup.sh` script'i bu adımların tamamını otomatik çalıştırır — bkz. aşağı.

## Teknoloji Yığını

.NET 8, PostgreSQL (database-per-service), RabbitMQ + MassTransit, Redis,
SignalR, YARP, EF Core, FluentValidation, Mapster, Docker Compose.

Detaylar için bkz. [`docs/ClubCraft-Spec.md`](docs/ClubCraft-Spec.md) §5.
