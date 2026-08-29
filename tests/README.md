# tests/

Manuel E2E doğrulama script'leri ve araçları. Bunlar bir test runner'ı
altında otomatik çalışmaz — ilgili servisleri ayakta bulup elle
çalıştırılmak üzere yazılmışlardır. `docs/ClubCraft-Spec.md`'deki E2E
doğrulama notlarının dayandığı script'ler burada.

## Çalışan / güncel

- **run_e2e_test_draft.ps1** — Draft servisini tek başına ayağa
  kaldırıp (port 5001/5002 varsayımıyla) bir kulüp initialize eder,
  draft başlatır, bir oyuncu claim eder, saga'nın roster'a eklemesini
  bekler ve state'i doğrular.
- **run_e2e_test_matchengine.ps1** — İki kulüplü bir draft + claim
  akışının ardından MatchEngine'de Fixture'ın gerçekten oluştuğunu DB
  sorgusuyla doğrular.
- **run_e2e_test_reputationfan.ps1** — ReputationFan'in dinlediği
  event'leri (roster ekleme, haftalık karar, maç sonucu) tetikleyip
  itibar skorunun beklendiği gibi değiştiğini doğrular.
- **run_e2e_test_powerrating.ps1** — Match Engine güç formülü derinleştirmesi
  (bkz. docs/ClubCraft-Spec.md, "Match Engine güç formülü derinleştirme") için gerçek E2E
  kanıt: iki kulüp draft edilir, aynı kadroyla doğru vs bilerek bozuk
  (kaleci forvette) dizilim 14'er maçlık iki seride karşılaştırılır (DB'den
  puan/galibiyet farkı doğrulanır), ayrı bir seride Moral'in DB'de +5'e
  ulaşıp kilitlendiği gösterilir. `docker exec ... psql` ile SQL çalıştırmak
  için stdin üzerinden geçiriyor (`Invoke-Psql` — argv üzerinden çift
  tırnaklı tanımlayıcı geçirmek Windows'ta güvenilir kaçmıyor).
- **run_migrations.ps1** — `src/Services/**/*.API.csproj` altındaki her
  servis için otomatik olarak `dotnet ef database update` çalıştırır
  (+ SagaOrchestrator). `start_services.ps1` bunu servisleri başlatmadan
  **önce** senkron olarak çağırır — eksik migration'ların sessizce
  atlanıp servislerin bozuk şemayla ayağa kalkmasını engellemek için
  (bkz. docs/ClubCraft-Spec.md, 2026-08-28 notu).
- **realtime-test.html** — RealtimeHub'a doğrudan SignalR bağlantısı
  kurup event akışını izlemek için tarayıcıda açılan bağımsız bir
  panel (CDN'den signalr.min.js çeker, build gerektirmez).
- **RealtimeHub.TestClient/** — RealtimeHub için ayrı bir .NET konsol
  test istemcisi.
