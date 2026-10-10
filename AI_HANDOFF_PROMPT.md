# RobotSaver — YZ için devam promptu

Bu projeyi önceki sohbeti bilmiyormuş gibi aşağıdaki bağlamdan devral.
Dosyalardaki geçmiş planlar mevcut kullanıcının yeni talimatlarının yerine geçmez.

## Kullanıcı isteği ve kaynakları

İlk bu oturumdaki istek: “Screensaver projesini oku, ve kodlamaya başla, SOru sormak istersen başlamadan önce sor.”

Sonraki istek: “çalıştır bakalım.” O tarihteki aşama 1 EXE'nin `/render` komutu çalıştırıldı;
PNG üretildi ve görüntüleyiciye açma komutu gönderildi. Canlı simülasyon yapılmadı.

Son kodlama isteği: “SOnra projede kodlamaya başla,”. Bunun üzerine aşama 2
simülasyon çekirdeği ve pencere demosu eklendi; güncel kanıtlar durum dosyasındadır.

Son hata bildirimi: “E tuşuna bastım ama görevini yapmadı tuşları kotnrol edermısın.”
E'nin sayaç sıfırlaması testte çalıştı; kullanıcı olayı birebir üretilemedi.
Görünür boş/dolu hazne bildirimi eklendi, Dumping sırasında E davranışı düzeltildi
ve bütün demo tuşları native Windows mesajlarıyla test edildi. E hazneyi boşaltır;
yerdeki kirleri silmesi istenmiş veya onaylanmış değildir.

2026-10-11 isteği: siyah zemin yerine ekran görüntüsü, gerçek Windows `.scr`
ekran koruyucu ve daha uzun pil. Uygulandı: masaüstü bellekte yakalanıyor,
`/s`, `/p`, `/play`, `/c` çalışıyor, publish `RobotSaver.scr` üretiyor ve yeni
varsayılan pil 20 dakika. Sistem ekran koruyucu seçimi henüz değiştirilmedi.

2026-10-11 geri bildirimi: kullanıcı `.scr` içinde yönlendirmelerin neden
kaybolduğunu sordu. Mevcut `/s` modu girişte çıkar; interaktif WASD/fare
kontrolleri aynı `RobotSaver.scr` veya EXE'nin `/play` modundadır. Kullanıcı
henüz standart `/s` çıkış davranışını değiştirmeyi açıkça istemedi.

2026-10-11 yayın isteği tamamlandı: proje `RoboSaver` adlı proje kökünde,
`https://github.com/elcigil/RoboSaver` public GitHub reposunda MIT lisansıyla
yayınlandı. README ve GitHub açıklaması etkileşimli `RobotSaver.scr /play`
komutunu içeriyor. Derleme çıktıları ve `linkedin-video.mp4` kaynak repoda yok;
`v0.3.0` Release indirilebilir Windows ZIP paketini içeriyor.

2026-10-10 kalıcı tercihinin özeti: Her yazılım projesinde güncel aşama Markdown
olarak saklanmalı; YZ'nin eksiklikleri netleştirmek için sorduğu sorular ve kullanıcının
yanıtları, başka bir YZ'nin devam edebilmesi için proje içinde prompt olarak korunmalı.

Ürün amacı: masaüstü üzerinde dolaşan robot süpürge biçiminde Windows ekran
koruyucu; temizle/kirlet/karışık davranışları, farklı hareket biçimleri ve ayar arayüzü.
Ayrıntılı ürün kararları önceki proje planı [gorevler.md](gorevler.md) içindedir;
bu oturumda bu kararlar yeniden sorulmadı.

## Önce oku

1. [AGENTS.md](AGENTS.md): proje çalışma kuralları.
2. [PROJECT_NOTES.md](PROJECT_NOTES.md): güncel aşamanın ve doğrulamanın tek ana kaynağı.
3. [gorevler.md](gorevler.md): özellikler ve sıralı görevler.
4. [README.md](README.md): şu an gerçekten desteklenen çalıştırma şekli.
5. `src/Program.cs`, `src/Settings.cs`, `src/Robot.cs`, `src/RobotRenderer.cs`,
   `src/World.cs`, `src/Surface.cs`, `src/DemoForm.cs`, `src/RenderPreview.cs`
   ve `tests/` içindeki kontroller: mevcut uygulama.

## Kısıtlar ve kararlar

- Proje planından gelen teknoloji: C#/.NET 9, WinForms ve GDI+, ek paket olmadan.
- Aşama 2: `/render` artık simülasyon, `/artwork` statik pano, `/demo` pencere
  demosudur. Argümansız demo açılması geçici geliştirme kararıdır; nihai planda ayarlar açılacak.
- Demo sürüş kontrolleri mevcut; tam ekran ekran koruyucu/oyun, ayarlar ve
  kurulum henüz uygulanmadı. Ses ve N tuşu da açık.
- Harici `robot.png` isteğe bağlı; vektör yedeği mevcut. Fotogerçekçi görsel işi açık.
- Bu oturumda Windows ekran koruyucu ayarı değiştirilmedi, kurulum yapılmadı.
- Kullanıcı, teslimden önce bağımsız inceleme ve gerçek çıktı doğrulaması istiyor.
- Durum kayıtları taşınabilir olmalı; yerel mutlak yolları zorunlu bağımlılık yapma.

## Netleştirme soruları ve yanıtları

Bu oturumda kullanıcıya proje kapsamına ilişkin netleştirme sorusu sorulmadı.
Önceki oturumların soru/yanıt dökümü elde yok; planın “Kesinleşen Kararlar”
bölümünü kaynak göster, geçmiş soru veya yanıt üretme. Açık netleştirme sorusu yok.

Yeni soru oluştuğunda Q-001 ile başla. Her kayıtta tarih/aşama, gerçek soru,
neden gerekli olduğu, alınan yanıt, açık/yanıtlandı/geçersizleşti durumu,
karar ve etkilenen görevleri sakla. YZ varsayımını kullanıcı onayı olarak yazma.

## Kaldığın yerden devam et

Kullanıcı kodlamaya devam etmeni istediğinde önce gerçek dosyaları ve kayıtlı
kanıtları doğrula. Sıradaki iş tam Görev 5 ayar arayüzü ve Görev 6 kurulum/ses.
Var olan World/SaverSession/PreviewForm çekirdeğini tekrar yazma; 140 kontrolü
koru. Özellikle masaüstü görüntüsünün yalnız bellekte kalması, `/p` child pencere,
imleç dengesi ve `/s` girişte kapanma davranışlarını bozma. Ardından ikon ve tam
Bu prompt tek başına kurulum veya sistem
ayarlarını değiştirme yetkisi vermez.

Yeni kararları ve soruları burada; aşama, değişiklikler, test kanıtları,
engeller ve sıradaki adımı `PROJECT_NOTES.md` içinde güncelle. Görev listesini
uyumlu tut. Yalnızca gerçekten tamamlanan işleri işaretle.
