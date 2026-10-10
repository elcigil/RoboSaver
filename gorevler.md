# Görevler — Robot Süpürge Ekran Koruyucu (RobotSaver)

## Projenin Amacı
`linkedin-video.mp4` videosundaki gibi, masaüstünde dolaşan gerçekçi bir **robot süpürgenin** temizlik yaptığı (ya da esprili şekilde kirlettiği) **profesyonel bir Windows ekran koruyucusu** yapmak. Masaüstünde bir kısayol olacak; açılan pencerede ekran koruyucunun tüm parametreleri değiştirilebilecek.

### Videodan çıkarılanlar
- Üstten görünen yuvarlak, gri robot süpürge (ortada dairesel ok ikonlu buton, yan fırçalar).
- Masaüstü görüntüsü üzerinde dolaşıyor, arkasında kahverengi çamur izi ve kırıntı bırakıyor (şaka: temizlemek yerine kirletiyor).
- Şarj istasyonu (dock), sahte Windows bildirimi ("Remote control… Battery 86%" + kızgın surat).
- Kontrol paneli: WASD sürüş, turbo, home, crumbs, map, pause, autopilot, empty bin, cake, reset, sound, hide keys.

## Kesinleşen Kararlar
- **Teknoloji:** C# / .NET 9 (makinede SDK 9.0.311 var) + WinForms, çizim GDI+ (ek paket yok). Tek dosya `RobotSaver.exe`, kopyası `RobotSaver.scr`.
- **Komut satırı:** `/s` ekran koruyucu (fare/tuş = çıkış), `/c` ayarlar (argümansız da ayarlar), `/p <hwnd>` Windows önizleme, `/play` oyun modu (WASD ile sürme, Esc çıkış), `/render <png> <saniye>` ekranı ele geçirmeden test görüntüsü üretir.
- **Davranış modu ayarlardan seçilir:** Temizle / Kirlet / Karışık (önce temizler, hazne dolunca geri döker).
- **Hareket tipi ayarlardan seçilir:** Roomba (çarp-dön), Spiral, Duvar takibi, Rastgele.
- **Arka plan:** Başlangıçta masaüstünün ekran görüntüsü (alternatif: düz renk / resim).
- **Çoklu monitör:** Tüm ekranlar tek sanal alan; robot bitişik kenarlardan ekranlar arası geçer.
- **Robot görseli:** Gerçekçi olmalı. Önce kodla çizilmiş gerçekçi vektör robot; `robot.png` (üstten görünüm, ön tarafı yukarı bakan, şeffaf arka plan) exe yanında veya `%APPDATA%\RobotSaver` içinde varsa o kullanılır. PNG Canva (generate-image + remove-background) ile üretilecek ya da internetten lisanslı bulunacak.
- **Ayarlar:** `%APPDATA%\RobotSaver\settings.json`.
- **Kurulum:** Admin gerektirmez → `%LOCALAPPDATA%\RobotSaver\` içine kopyala, `HKCU\Control Panel\Desktop\SCRNSAVE.EXE` ayarla, 2 masaüstü kısayolu: "Robot Süpürge Ayarları" (`/c`) ve "Robot Süpürge - Sür" (`/play`).

## Durum (son güncelleme: 2026-10-11)
- [x] 2026-10-11: v0.3.0 EXE/SCR/ZIP public indirmeleri ve SHA-256 manifesti yeniden doğrulandı; README doğrudan indirme bağlantıları eklendi.
- [x] Video incelendi, plan onaylandı.
- [x] `src/RobotSaver.csproj` oluşturuldu.
- [x] Görev 1 — `Settings.cs` ve `Lang.cs`: doğrulamalı JSON ayarları ve TR/EN metinler.
- [x] Görev 2 — `Robot.cs` ve `RobotRenderer.cs`: robot modeli, vektör çizim, PNG desteği ve dock.
- [x] Simülasyon çekirdeği — Surface/World, üç davranış, dört hareket, kir/solma, hazne/pil döngüleri, ekran birleşimleri ve robot çarpışmaları.
- [x] Pencere demosu `/demo`, gerçek simülasyon `/render`, eski çizim panosu `/artwork`, klavye kontrolleri (N hariç), balonlar, şaka bildirimi ve mini harita.
- [x] E tuşu geri bildirimi ve Dumping düzeltmesi; tüm demo tuşları Windows mesaj kuyruğuyla kontrol edildi. Toplam 125 kontrol geçti. N henüz ses çalmaz, açıklama gösterir.
- [ ] Görev 3 tamamlayıcıları — ses/N entegrasyonu ve gerçek çoklu monitör oturum testi; çekirdek tamamlandı.
- [x] Görev 4 — `/s`, `/p <HWND>`, `/play`, `/c`, çoklu monitör SaverSession ve girişte kapanma.
- [ ] Görev 5–6 — Tam ayar arayüzü, kurulum ve ses. `/c` pil/arka plan için başlangıç arayüzüne sahip.
- [ ] Görev 7 — 3×4 simülasyon, demo, tam ekran ve Windows önizleme test edildi; ikon açık.
- [ ] Görev 8 — Harici gerçekçi robot görseli (vektör yedeği mevcut).
- [ ] Görev 9 — Kurulum betiği ve nihai kullanım belgesi. Aşama 2 README güncellendi.
- Doğrulama ve devam notları: `PROJECT_NOTES.md`.

---

## Görevler (her biri doğrudan Claude'a verilebilecek prompt)

### Görev 1 — Ayarlar ve dil
> `src/Settings.cs` dosyasını yaz. Enumlar: `CleanMode {Clean, Dirty, Mixed}`, `MoveMode {Roomba, Spiral, WallFollow, Wander}`, `DirtType {Dust, Crumbs, Mud, Confetti}`, `BgMode {Desktop, SolidColor, Image}`. `Settings` sınıfı alanları: RobotCount=1 (1–8), RobotSize=120 px, Speed=160 px/sn, Mode=Mixed, Movement=Roomba, DirtDensity=50 (0–100), Dirt=Crumbs, FadeSeconds=0 (0=kalıcı), BinCapacity=300, BatteryMinutes=3, Background=Desktop, BgColor="#1E1E2E", BgImage="", Bubbles/Notifications/StuckEvents=true, Sound=false, ShowDock=true, Language="tr", Fps=60, MouseSensitivity=10, TimeoutMinutes=5. Statik `Current`, `Load()`/`Save()` (`%APPDATA%\RobotSaver\settings.json`, `JsonStringEnumConverter`), `Clone()`. Ayrıca `src/Lang.cs`: `L.T(tr,en)` ve TR/EN espri cümle dizileri (Bump, Stuck, BinFull, BinFullHome, AfterDump, LowBattery, Docked, DirtyLines) ve sahte bildirim listesi (başlık, gövde `{0}`=pil%, `{1}`=hazne%, emoji): "Uzaktan kumanda bağlantısı koptu 😠", "Hazne dolu, bunu sen mi yedin?", "Windows Update: %30 daha fazla kırıntı", "Takvim: masaüstü temizliği yine ertelendi", "Outlook: halıdaki leke hakkında toplantı", "Antivirüs: 1 toz tehdidi karantinada", "Kedi: robota binmek istiyorum".

### Görev 2 — Robot modeli ve çizimi
> `src/Robot.cs`: `RobotState {Roaming, ReturningHome, Docked, Leaving, Stuck, Dumping}`; alanlar Pos, LastPos, Dock, Angle (radyan, 0=+X), Radius, Battery(0–1), Bin, BrushAngle, BumpFlash, StateTime, SpiralR, StraightTime, AvoidTime, SayCooldown, WanderPhase, ManualControl; `SetState()`. `src/RobotRenderer.cs`: gerçekçi üstten görünüm — gölge, ön tarafta dönen 2 yan fırça, PathGradient gri gövde, ön tampon yayı, iç plaka, ortada dairesel ok ikonlu buton, arka-sağda sensör kubbesi, pil durumuna göre renkli LED (yeşil/turuncu/kırmızı), parlama. `robot.png` varsa onu döndürerek çiz. Şarj istasyonu çizimi (robotun arkasında, yanıp sönen yeşil LED).

### Görev 3 — Dünya / simülasyon motoru
> `src/World.cs`: `Surface` (Bounds, WorkArea, Primary, Background PArgb bitmap, Dirt Argb bitmap, tembel Graphics). `World.CreateForScreens(play)` her ekranın görüntüsünü `CopyFromScreen` ile alır; `World.CreatePreview(size)` küçük önizleme için. Kir sistemi: tipine göre kırıntı/toz/çamur/konfeti çizer ve sayım için listede tutar; leke ve küme serpme; ortama yavaş yavaş yeni kir düşer. Temizle: robot altındaki alanı `SourceCopy` ile şeffaflaştırır, kırıntıları toplar, Bin artar. Kirlet: iki tekerlek izi + orta leke + kırıntı. Karışık: hazne dolunca dönerek etrafa geri döker (Dumping). Temizle modunda hazne dolunca yuvaya gider ve boşaltır. Hareket modları (Roomba/Spiral/Duvar takibi/Rastgele), çarpınca dönme. Geçerlilik kontrolü 5 problu (robot ekran birleşiminin dışına çıkamaz → monitörler arası geçiş bitişik kenarlardan olur) + robotlar arası çarpışma. Pil biter (%15) → yuvaya dön → şarj → çık. Rastgele takılma (titreme + "Yardım edin!"). FadeSeconds>0 ise LockBits ile alfa azaltma. Konuşma balonları, sağ altta kayarak gelen sahte bildirim (toast). Oyun modu tuşları: WASD/oklar sürüş, Shift turbo, H eve, C kırıntı aç/kapa, M mini harita, Space duraklat, F1 tuşları gizle, Enter otopilot, E hazneyi boşalt, K pasta (kırıntı+konfeti patlaması), R sıfırla, N ses, Esc çıkış. Oyun modunda sol altta kontrol paneli + pil/hazne durumu.

### Görev 4 — Ekran koruyucu pencereleri ve giriş noktası
> `src/Program.cs`: argüman ayrıştırma (`/s`, `/c`, `/c:1234`, `/p 1234`, `/p:1234`, `/play`, `/render`, `/makeicon`). `Application.SetHighDpiMode(PerMonitorV2)`, `timeBeginPeriod(1)`. `SaverSession : ApplicationContext` her monitör için bir `SaverForm` açar, tek timer (Fps) ile `world.Update(dt)` + tüm formları Invalidate eder. `src/SaverForm.cs`: çerçevesiz, TopMost, ekran sınırları, double buffer, DPI değişimini iptal et; `/s` modunda imleç gizli, fare MouseSensitivity pikselden fazla oynarsa / tıklanırsa / tuşa basılırsa çıkış (ilk 0.6 sn yok sayılır); `/play` modunda tuşlar dünyaya gider, Esc çıkar. `src/PreviewForm.cs`: `/p` için `SetParent` + `WS_CHILD`, ebeveyn kapanınca kapan.

### Görev 5 — Ayarlar penceresi
> `src/ConfigForm.cs`: üstte mor başlık bandı, sekmeler: **Robot** (sayı, boyut, hız, davranış modu, hareket tipi, pil süresi, hazne kapasitesi), **Kir** (yoğunluk, tip, iz kalıcılığı), **Görünüm** (arka plan türü, renk seçici, resim seçici, yuvayı göster, FPS), **Eğlence** (balonlar, bildirimler, takılma olayları, ses, dil), **Kurulum** (bekleme süresi, fare hassasiyeti, "Ekran koruyucu olarak kur", "Kaldır", "Masaüstü kısayollarını oluştur", "Windows ekran koruyucu ayarlarını aç"). Alt butonlar: Önizle (kaydet + `/s` başlat), Robotu Sür (`/play`), Kaydet ve Kapat, İptal. Tüm metinler `L.T` ile TR/EN.

### Görev 6 — Kurulum, kısayollar, ses
> `src/Installer.cs`: `%LOCALAPPDATA%\RobotSaver\` içine exe + `.scr` (+ `robot.png`) kopyala (çalışan dosyanın üzerine kopyalamayı atla), `HKCU\Control Panel\Desktop` içine `SCRNSAVE.EXE` (kısa yol adı), `ScreenSaveActive=1`, `ScreenSaveTimeOut`; `SystemParametersInfo` ile uygula. Kaldırma. `WScript.Shell` COM ile masaüstüne iki kısayol. `src/Sfx.cs`: bellekte üretilen WAV tonları (çarpma, yuvaya giriş melodisi, hazne dolu bip-bip, üzgün ton, parti, toggle), `SoundPlayer` ile çal, ayar kapalıysa sessiz.

### Görev 7 — Derleme ve test
> `cd src && dotnet build` hatasız olsun. `RobotSaver.exe /render test.png 20` ile ekranı ele geçirmeden 20 sn simüle edip ana ekranı PNG'ye kaydet ve görsel olarak kontrol et (robot, kir, iz, balon, bildirim, dock). Üç mod ve dört hareket tipi için tekrarla. `/makeicon robot.ico` ile ikon üret, csproj zaten varsa `robot.ico`'yu kullanır → tekrar derle. `dotnet publish -c Release` ile tek dosya üret.

### Görev 8 — Gerçekçi robot görseli
> Canva `generate-image` ile "top-down view of a realistic dark grey robot vacuum cleaner, centered, plain white background, product photo, front facing up" üret, `remove-background` ile şeffaflaştır, 512×512 `robot.png` olarak `src/` içine koy ve publish çıktısına kopyalanmasını sağla. Olmazsa ücretsiz lisanslı (CC0) bir görsel bul. Görsel yoksa vektör çizim kullanılmaya devam eder.

### Görev 9 — Kurulum betiği ve README
> Kök dizine `install.ps1` yaz: `dotnet publish` → çıktıyı çalıştırıp `/install` ile kur ve kısayolları oluştur. `README.md`: kurulum, kullanım, tuşlar, ayarlar, kaldırma (Türkçe).

### Görev 10 — v2 (sonra)
> Robotun üstünde gezen kedi, masaüstü ikonlarını (görüntü üzerinde) itme, istatistikler ("temizlenen alan 4,2 km²") ve kapanışta ısı haritası, hazır ayar profilleri ("Sakin ofis", "Kaos", "Sunum modu"), gece modu (22:00 sonrası yavaş ve LED kısık).

---

## Devam etmek için
Yeni oturumda: "AGENTS.md ve AI_HANDOFF_PROMPT.md dosyalarını oku; PROJECT_NOTES.md ve gorevler.md içindeki durumu gerçek kodla doğrula ve ilk tamamlanmamış görevden devam et." Her anlamlı aşamada proje notlarını, soru/yanıt ve devir promptunu, ayrıca bu dosyadaki **Durum** bölümünü güncelle.
