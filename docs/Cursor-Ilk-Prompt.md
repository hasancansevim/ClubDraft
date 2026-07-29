# Cursor'a Verilecek İlk Prompt

Aşağıdaki metni Cursor'da (Composer/Agent modunda), proje kök dizini açıkken,
`docs/ClubCraft-Spec.md` dosyasını da context'e ekleyerek gönder:

---

Bu repo, ClubCraft adlı bir .NET 8 microservices projesinin başlangıç iskeleti.
`docs/ClubCraft-Spec.md` dosyasında projenin tüm gereksinim analizi, mimari
kararları, aggregate/domain event tasarımı ve API contract taslakları var.
Lütfen başlamadan önce bu dosyayı tamamen oku ve anla.

Kurallarım:

1. **Adım adım ilerle, tek seferde her şeyi yazma.** Sana hangi servisin
   hangi katmanını yapmanı istediğimi söyleyeceğim. Örn. "Draft servisinin
   Domain katmanını yaz" dediğimde sadece onu yap, Application/Infrastructure/API
   katmanlarına geçme.

2. **Mimari kararlarda (aggregate sınırları, yeni domain event eklemek,
   value object tasarımı gibi) benden onay almadan ilerleme.** Spec'te
   net olmayan bir nokta varsa, kendi kararını verip devam etme —
   bana sor, ben karar vereyim.

3. **Spec'te "kapsam dışı" (§6) olarak işaretlenen hiçbir şeyi ekleme.**
   Özellikle: detaylı taktik motoru, sezon içi serbest transfer market,
   sürekli/kalıcı dünya, canlı maç animasyonu, bot takımlar. Bunları
   "faydalı olur" diye kendi başına eklemeye çalışma.

4. **Over-engineering yapma.** Spec'te tanımlanmayan ekstra pattern,
   ekstra soyutlama katmanı, ekstra microservice ekleme. Basit tutulması
   gereken yerler (Match Engine gibi) gerçekten basit kalsın.

5. Her adımdan sonra ne yaptığını kısaca özetle, ben onaylayınca
   bir sonraki adıma geç.

Başlangıç için: `setup.sh` script'ini incele (henüz çalıştırmadım),
solution ve proje referanslarının nasıl kurulacağını orada görebilirsin.
Bana ilk olarak Draft servisinin Domain katmanını (DraftSession aggregate'i,
PlayerSnapshot value object'i, domain event'ler) yazmanı istiyorum —
bunu neden ilk olarak seçtiğimi soracak olursan: concurrency açısından
en kritik servis burası, mimarinin geri kalanı buna göre şekillenecek.

---

## Neden Bu Şekilde Yazıldı

- **"Adım adım" vurgusu:** Cursor'a (ya da herhangi bir AI agent'a) "tüm projeyi
  yap" dersen, muhtemelen 7 servisi de aynı anda, tutarsız kararlarla oluşturur.
  Katman katman, servis servis ilerlemek hem seni kontrolde tutar hem de
  hataları erken yakalarsın.
- **Onay mekanizması:** Aggregate sınırları gibi kararlar spec'te net olsa da,
  kod yazarken agent'lar bazen "daha iyi" diye kendi yorumunu katar. Bunu
  baştan engellemek işini kolaylaştırır.
- **Kapsam dışı hatırlatması:** AI agent'lar "faydalı olur" diye spec'te
  olmayan özellik eklemeye meyilli olabilir (örn. "taktik motoruna basit bir
  şey ekleyeyim" gibi). Bunu özellikle yasaklamak gerekiyor.
