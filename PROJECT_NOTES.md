# Proje çalışma notları

## 2026-10-11 — Derlenmiş dosyalara erişimin yeniden doğrulanması

- Kullanıcı derlenmiş dosyaların yüklenmediğini bildirip yüklenmesini istedi.
- GitHub API, v0.3.0 içinde EXE, SCR, ZIP ve SHA256SUMS.txt varlıklarının zaten
  bulunduğunu gösterdi. Üzerlerine yazılmadı ve yinelenen sürüm oluşturulmadı.
- Dört dosya public HTTPS bağlantılarından artifacts/upload-verification içine
  indirildi; EXE, SCR ve ZIP SHA-256 değerleri manifestle birebir eşleşti.
- README İndir bölümüne doğrudan dosya bağlantıları ve Releases → Assets konumu
  eklendi. Uygulama kodu değişmedi; çalışma testleri yeniden yapılmadı.
- Aşama 3 ve sonraki geliştirme işi değişmedi. Yayıncı tercihi Vedat ELCIGIL,
  açık iletişim adresi vedat@elcigil.com; sertifika henüz alınmadı/uygulanmadı.

## Güncel durum — 2026-10-11

- Aşama 3: masaüstü görüntülü gerçek Windows ekran koruyucusu hazır.
  `/s`, `/p`, `/play`, `/c`, `/demo`, `/render` ve `/artwork` destekleniyor.
- Doğrulama: son çalıştırmada 140 kontrol geçti. Yayınlanan `RobotSaver.scr /s`
  gerçek süreçte pencereler oluşturdu ve klavye girdisiyle çıkış kodu 0 kapandı.
- Sıradaki iş: tam ayar arayüzü, kurulum/kısayollar, ses/N ve ikon.
- Açık netleştirme sorusu veya bu aşama için bilinen engel yok.
- Kullanıcı istekleri ve YZ devir promptu: [AI_HANDOFF_PROMPT.md](AI_HANDOFF_PROMPT.md).
- Public GitHub repo: `https://github.com/elcigil/RoboSaver`; branch `main`.
  İlk yayın commit'i `db3883d`; indirilebilir sürüm `v0.3.0`.

## 2026-10-11 — Açık kaynak GitHub yayını

- Kullanıcı projeyi `RoboSaver` klasörüne kopyalamayı, public GitHub reposu
  olarak yayınlamayı ve etkileşimli komutu açıklamaya eklemeyi açıkça istedi.
- Temiz proje yeni `RoboSaver` proje köküne kopyalandı. `artifacts`, `bin`, `obj`
  ve lisansı belirsiz referans `linkedin-video.mp4` kopyalanmadı.
- MIT lisansı eklendi. README, public indirme bağlantısı ve
  `RobotSaver.scr /play` etkileşimli çalıştırma komutuyla güncellendi.
- GitHub: `https://github.com/elcigil/RoboSaver`, public. Sürüm 0.3.0.
- Temiz klasörde 140 kontrol geçti; 26 izlenen dosya doğrulandı. Secret taraması
  eşleşme bulmadı. `linkedin-video.mp4`, artifacts/bin/obj izlenmiyor.
- `RoboSaver-v0.3.0-win-x64.zip` GitHub Release varlığı olarak yayınlandı;
  paket içinde `RobotSaver.scr`, `RobotSaver.exe`, README ve LICENSE var.
- GitHub açıklaması etkileşimli `RobotSaver.scr /play` komutunu içeriyor.
- Yayınlanan ZIP'in içeriği listelendi; `.scr` ve `.exe` SHA-256 olarak aynı.
  Paket içindeki `.scr /s` gerçek süreçte çalıştı ve native tuşla çıkış kodu 0 kapandı.
- Bağımsız yayın incelemesi engelleyici bulgu bulmadı; mutlak proje yolu göreli
  ifadeye çevrildi ve Release bağlantısının yayın sonrası doğrulanması istendi.

## 2026-10-11 — Derlenmiş dosyalar ve SHA-256

- Kullanıcı derlenmiş dosyaların hash bilgileriyle ayrıca yüklenmesini istedi.
- `RobotSaver.exe` ve `RobotSaver.scr`, ZIP'in yanında ayrı Release varlığı
  olarak yüklendi. `SHA256SUMS.txt` ve Release açıklamasında SHA-256 değerleri var.
- EXE/SCR: `4702ec988dc889ef51ecb9ea1b1bcbf1f442db843e988498a41ca29b10d78309`.
- ZIP: `ec55da0c432d52f1fbdefa64a7cb60cfa2972cf052fbd516008106abad3138b8`.
- GitHub asset digestleri bu değerlerle eşleşiyor. Public bağlantılardan EXE,
  SCR, ZIP ve manifest yeniden indirildi; indirilen üç ikili için hash kontrolü geçti.
- Authenticode durumu `NotSigned`; Release notu bilinmeyen yayıncı uyarısını
  ve .NET 9 Windows Desktop Runtime gereksinimini açıklar.
- Bağımsız inceleme manifesti yeniden hesapladı, ZIP içeriğini açtı ve bütün
  yayın varlıklarını onayladı; engelleyici bulgu yok.

## 2026-10-11 — SCR yönlendirme geri bildirimi

- Kullanıcı `.scr` modunda klavye/fare yönlendirmelerinin neden kaybolduğunu sordu.
- Mevcut davranış kasıtlı mod ayrımıdır: `/s` standart ekran koruyucu modunda
  klavye, tıklama veya belirgin fare hareketi çıkış yapar; sürüş kontrolleri
  aynı ikili dosyanın `/play` modundadır. Bu ayrım ilk ürün planındaki kararla
  ve Windows ekran koruyucu davranışıyla uyumludur.
- Kullanıcı henüz `/s` davranışının interaktif olacak şekilde değiştirilmesini
  açıkça istemedi. Böyle bir istek gelirse çıkış yöntemi ayrıca belirlenmelidir.

## 2026-10-11 — Aşama 3: masaüstü görüntüsü ve gerçek SCR

- Kullanıcı siyah zemin yerine ekran görüntüsü, gerçek `.scr` ekran koruyucu ve
  daha uzun pil istedi. Yeni varsayılan pil 3 dakikadan 20 dakikaya çıkarıldı.
  Kullanıcının `%APPDATA%\RobotSaver\settings.json` dosyası yoktu; eski kayıt
  taşınması gerekmedi ve gerçek kullanıcı ayarına yazılmadı.
- `World.CreateDesktopPreview` ve `CreateForScreens`, pencere açılmadan önce
  masaüstünü belleğe alır. Görüntü diske kaydedilmez. Yakalama başarısızsa siyah
  zemine sessiz geçilmez; etkileşimli mod görünür hata verir.
- `SaverSession` her monitör için çerçevesiz üst pencere açar, ortak World/timer
  kullanır, imleci gizleyip çıkışta geri getirir. İlk 0,6 saniye sonrasında
  klavye, tıklama ve fare hareketi `/s` modunu kapatır. Ekran değişiminde çıkar.
- `PreviewForm`, `/p <HWND>` ve `/p:<HWND>` için gerçek `WS_CHILD` penceresidir;
  ebeveyni yeniden boyutlandırır, ebeveyn yok olduğunda kapanır. `/c` pil süresi
  ve Desktop/SolidColor/Image seçimini koruyan başlangıç ayar penceresidir.
- Publish sonrası EXE birebir `RobotSaver.scr` adına kopyalanır. `artifacts/stage3`
  içinde son yayında her iki dosya 242336 bayt ve SHA-256 içerikleri aynı olarak
  üretildi. Sistem kaydı değiştirilmedi.
- Bağımsız inceleme, resim arka planının `/c` kaydında yanlışlıkla düz renge
  dönüşmesi ve etkileşimli yakalama hatalarının sessiz kalması risklerini buldu;
  üç seçenekli arka plan listesi ve görünür hata iletişimi ile düzeltildi.
- Son incelemede boş resim yolu ile Image seçiminin kaydedilebildiği bulundu.
  `/c` penceresine dosya seçici eklendi; geçerli dosya yoksa ayar kaydedilmiyor.
- 140 kontrol geçti: bağımsız bellek içi ekran yakalamayla piksel eşleşmesi,
  native preview child/resize/parent kapanışı, iki ekran penceresi, Esc/tuş/tıklama
  ile tüm pencerelerin kapanması ve 20 dakikalık pilin dakikada yaklaşık %5
  azalması dahil. Önceki 125 kontrol de geçti.
- Son yayınlanan `.scr /s`, `UseShellExecute=false` ile gerçek süreç olarak çalıştırıldı;
  10 üst seviye yardımcı/uygulama penceresi görüldü ve gönderilen tuş mesajıyla
  beş saniye içinde çıkış kodu 0 kapandı. `MainWindowHandle` ShowInTaskbar=false
  nedeniyle 0 kaldığından EnumWindows ile sürece ait pencereler bulundu.
- Sınırlar: fiziksel insan klavyesi yerine native mesaj kullanıldı. Windows ekran
  koruyucu seçimi/kurulumu, ses, tam ayarlar ve ikon henüz yok.

## 2026-10-10 — E tuşu ve klavye kontrolü

- Kullanıcı E'nin görevini yapmadığını bildirip tüm tuşların kontrolünü istedi.
- Düzeltme öncesi gerçek WinForms mesaj kuyruğuna WM_KEYDOWN/WM_KEYUP gönderilerek
  E iki kez denendi; her ikisinde sayaç sıfırlandı. Kullanıcının yaşadığı olay
  birebir üretilemedi; kesin kök neden bulunduğu iddia edilmiyor.
- Bulunan eksikler: boşaltmada görünür geri bildirim yoktu; boş haznede hiçbir
  tepki görünmüyordu; Dumping sırasında E sayacı sıfırlasa da durum sürüyordu.
- `World.cs`: E boş/dolu hazne mesajı verir, Dumping'i sonlandırır ve eski balonu
  temizler. İşlem mesajları balon ayarından bağımsızdır. Diğer komutlar da geri
  bildirim verir; N sesin henüz hazır olmadığını açıklar. E yerdeki kirleri silmez.
- `DemoForm.cs`: tuştan sonra yeniden çizim ister. `tests/KeyboardChecks.cs`:
  native mesaj kuyruğu üzerinden E tekrarları, M/C/F1/Enter/Space aç-kapat,
  K/H/R, sekiz yön tuşunun basma/bırakma davranışı, Shift turbo, N ve Esc.
- Testler: `dotnet run --project tests/RobotSaver.Checks.csproj -c Release -- --artifacts artifacts/checks`
  sonucunda 125 kontrol geçti. `artifacts/checks/keyboard-empty-bin.png`
  açılarak hazne 0/300 ve görünür mesaj doğrulandı. Release yayını
  `artifacts/stage2/RobotSaver.exe` güncellendi.
- Bağımsız inceleme engelleyici bulgu bulmadı. İncelemeci testleri ayrıca
  çalıştırmadı; kodu ve kapsamı inceledi. Fiziksel klavye/odak kaybı/yeniden
  odaklanma bu testte doğrulanmadı; kuyruktaki native tuş mesajları doğrulandı.
- Yeni netleştirme sorusu sorulmadı. Sıradaki ana görev değişmedi.

## 2026-10-10 — Aşama 2: simülasyon ve canlı demo

### Değişiklikler ve kararlar
- Kullanıcı isteği: “SOnra projede kodlamaya başla,”. Yeni netleştirme sorusu
  gerekmedi; mevcut plana göre simülasyonla devam edildi.
- Yeni dosyalar: `src/Surface.cs`, `src/World.cs`, `src/DemoForm.cs`,
  `tests/WorldChecks.cs`, `tests/DemoChecks.cs`. `Program.cs` komutları genişletildi.
- GDI+ kir katmanı, 120 Hz sabit adım, Roomba/Spiral/WallFollow/Wander,
  Clean/Dirty/Mixed, gerçek kapasite sınırı, boşaltma, pil/yuva döngüsü eklendi.
- Monitör birleşimini doğrulamak için yalnızca beş nokta yerine dairesel gövdeyi
  ekranların birleşimiyle karşılaştıran kesit hesabı seçildi; köşe/boşluk testi var.
- Dönüşte geçmiş rota; tıkanınca A* yeniden planlama ve karşılıklı engelde kenara
  çekilme kullanılıyor. Yer olmayan geçitlerde bekleme sürer; ışınlanma yok.
- Bellek sınırı: 8000 parçacık, robot başına 100000 rota noktası aşılınca eve dönüş.
- Geliştirici uygulama kararı: gerçek tam ekran oturumundan önce güvenli pencere
  demosu eklendi. Argümansız çalıştırma şimdilik demo; nihai planda ayarlar olacak.
- Demo/render düz arka plan kullanır. `CreateForScreens` masaüstü veya resim
  arka planı için yazıldı fakat gerçek ekran oturumunda henüz kullanılmadı.

### Doğrulama ve inceleme
- `dotnet run --project tests/RobotSaver.Checks.csproj -c Release -- --artifacts artifacts/checks`
  ile 88 kontrol geçti. Önceki 28 kontrol de bu toplam içindedir.
- 12 davranış/hareket birleşimi 20 saniye simüle edildi ve okunabilir PNG üretti.
- Sekiz robotun 20 saniye sınırlar içinde çarpışmadan hareketi; bitişik, boşluklu,
  negatif koordinatlı ve köşeli sentetik monitör düzenleri doğrulandı.
- Kilitli ayarlar, kirin gerçek alfa silinmesi/solması, hazne kapasitesi ve
  şarjdan tekrar çıkış kontrolleri geçti.
- Bağımsız inceleme ilk 81 kontrolü tekrar çalıştırdı. Kapasite üstü toplama ve
  eve dönüşte tıkanma bulguları düzeltildi. Bir kırıntılık haznede 60 kırıntının
  59'unun kalması, sabit engelin çevresinden dönüş ve karşılıklı dönüş testleri eklendi.
- Karşılıklı dönüş testi önce gerçek tıkanmayı gösterdi; yol verme eklendikten
  sonra iki robot da yuvasına ulaştı. Bağımsız ikinci kontrol, düzeltmeleri ve
  88 kontrolün tamamının geçtiğini ayrıca doğruladı; yeni engelleyici bulgu yok.
- Gerçek STA WinForms mesaj döngüsünde iki kare farklı, Space sonrası kareler
  aynı ve Esc sonrası pencere Dispose edilmiş olarak doğrulandı. Test fiziksel
  klavye yerine formun tuş olaylarını çağırır; pencere ekran dışında tutulur.
- `artifacts/checks/demo-window.png` ve EXE ile üretilen simülasyon PNG'leri
  açılarak incelendi; başlangıç balonunun başlıkla çakışması düzeltildi.
- Son yayın: `dotnet publish src/RobotSaver.csproj -c Release -o artifacts/stage2`
  başarılı. Yayınlanan EXE'de Clean/Dirty/Mixed simülasyonları ve `/artwork`
  çıkış kodu 0; geçersiz mod, NaN zamanı ve fazla artwork argümanı çıkış kodu 1.
  `artifacts/simulation-mixed.png` içinde şaka bildirimi de görsel olarak doğrulandı.

### Açık sınırlar
- `/s`, `/p`, `/play`, ayarlar penceresi, ses/N, kurulum, ikon ve gerçekçi harici
  PNG açık. Görev 3'ün tüm ayrıntıları bitmiş sayılmıyor.
- Ses ayarı henüz etkili değil. Gerçek çoklu monitör yakalama, bağlantı kopması,
  DPI geçişleri ve tam ekran fareyle çıkış test edilmedi.
- A* 12000 düğümle sınırlı. Tüm kalabalık düzenlerde ilerleme garantisi yok;
  kapalı yuvada bekleme beklenen davranış. Uzun süreli performans ölçümü yapılmadı.
- Kullanıcının gerçek ayarları, kayıt defteri ve masaüstü kısayolları değiştirilmedi.

## 2026-10-10 — Kalıcı proje hafızası

- Kullanıcının isteğiyle kişisel `project-memory` skill'i kaydedildi; yazılım
  işlerinde kullanılması için kişisel Codex AGENTS tercihine eklendi.
- Bu projeye `AGENTS.md` ve `AI_HANDOFF_PROMPT.md` eklendi. Mevcut durum dosyası
  korundu; ikinci bir rakip durum belgesi oluşturulmadı.
- Soru/yanıt kaydı için kurallar eklendi; geçmişte sorulmamış sorular üretilmedi.
- Bu değişiklikte uygulama kodu değiştirilmedi; uygulama testleri tekrar çalıştırılmadı.
- Skill doğrulayıcısı başarılı; kaydedilen dosyalar tekrar okundu ve devir
  promptunun yerel bağlantıları kontrol edildi. Bağımsız incelemede önemli bir
  eksik bulunmadı; görev listesindeki eski devam promptu yeni giriş dosyalarıyla
  uyumlu hale getirildi. Otomatik skill seçiminin sonraki oturumdaki davranışı
  ayrıca denenmedi; kalıcı AGENTS kuralı ve açık çağrı kullanılabilir.

## 2026-10-10 — Aşama 1

### Kapsam
- Başlangıçta yalnızca `gorevler.md`, `src/RobotSaver.csproj` ve referans video bulunuyordu; Git deposu yoktu.
- Görev 1: ayarlar, sınır kontrolleri, JSON okuma/yazma, kopyalama, TR/EN diyaloglar ve bildirim metinleri uygulandı.
- Görev 2: robot durumu/verisi, GDI+ robot ve dock çizimi, isteğe bağlı PNG ve vektör yedeği uygulandı.
- Görsel doğrulama için geçici `/render` giriş noktası eklendi. Bu komut yalnızca çizim panosu üretir; Görev 7'deki dünya simülasyonu testinin yerine geçmez.
- Kullanıcının gerçek ayar dosyasına, ekran koruyucu kayıt defteri ayarlarına ve masaüstü kısayollarına yazılmadı.

### Doğrulama
- Release derleme: 0 hata, 0 uyarı.
- Ayrı test uygulaması: ayar dosyası round-trip, Unicode, bozuk JSON, bilinmeyen enum, null/değer sınırları, sonlu olmayan sayılar, kilitli dosya, başarısız kayıtta geçici dosya temizliği, iki dil, robot durum geçişi, Graphics dönüşümünün geri alınması, bozuk görselden dönüş, PNG kilidinin bırakılması, robot yönü ve okunabilir PNG çıktısı.
- Yayınlanan `artifacts/stage1/RobotSaver.exe` gerçekten çalıştırıldı; `/render artifacts/robot-preview.png 20` çıkış kodu 0, üretilen PNG 1280×800 ve görsel olarak açılarak incelendi.
- DPI'ya bağlı başlık sıkışması görsel incelemede saptandı, piksel birimli fontlarla düzeltildi; yeni PNG tekrar açıldı.
- Geçersiz `NaN` zamanı: çıkış kodu 1, çıktı dosyası oluşmadı.
- Bağımsız inceleme ajanı kodu gözden geçirdi, 27 kontrolü ve render komutunu ayrıca çalıştırdı. Geçerli ikinci PNG adayına geçince eski uyarının kalmasını buldu; başarıda uyarı temizlendi ve regresyon kontrolü eklendi.
- Düzeltme sonrası 28 kontrolün tamamı geçti; Release yayını güncellendi. Bağımsız incelemede aşama 1 kapsamında önemli bir güvenlik veya kaynak yönetimi sorunu bulunmadı.

### Sınırlar ve devam
- Simülasyon, çoklu monitör, gerçek /s ve /p, oyun kontrolleri, ayar arayüzü, ses ve kurulum henüz yok. Bu alanlar test edilmiş sayılmaz.
- Mevcut görsel kodla çizilmiş vektördür; fotogerçekçi harici PNG üretilmedi. Görev 8 açık.
- `/render` zaman değeri yalnızca çizim animasyonuna etki eder. Kullanıcının masaüstü görüntüsünü almaz.
- Sonraki adım Görev 3: World/Surface ve simülasyon. Ardından Görev 4–6, gerçek mod/hareket matrisi, ikon ve kurulum testleri.

- Bu erişim düzeltmesi bağımsız incelemeden geçti: dört varlığın hashleri ve yeni README bağlantılarının HTTP 200 yanıtları ayrıca doğrulandı; engelleyici bulgu yok.
