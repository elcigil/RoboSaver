# RobotSaver

Windows için robot süpürge ekran koruyucu projesi. **Aşama 3: gerçek Windows ekran koruyucusu hazır.** Robot başlangıçta alınan masaüstü görüntüsü üzerinde hareket eder, kir toplar veya iz bırakır, haznesini boşaltır ve şarja döner.

## İndir

Hazır Windows paketini [GitHub Releases](https://github.com/elcigil/RoboSaver/releases/latest) sayfasından indirin. Derlenmiş `.exe` ve `.scr` dosyaları ayrı ayrı da sunulur. İndirdikten sonra [SHA256SUMS.txt](https://github.com/elcigil/RoboSaver/releases/latest/download/SHA256SUMS.txt) ile dosya bütünlüğünü doğrulayabilirsiniz. Kaynak kod MIT lisansıyla açıktır.

```powershell
Get-FileHash .\RobotSaver.scr -Algorithm SHA256
```

## Derleme ve çalıştırma

Windows x64 ve .NET 9 SDK ile proje kökünde:

```powershell
dotnet build src/RobotSaver.csproj -c Release
dotnet run --project tests/RobotSaver.Checks.csproj -c Release
dotnet publish src/RobotSaver.csproj -c Release -o artifacts/stage3
Start-Process .\artifacts\stage3\RobotSaver.scr -ArgumentList '/s'
```

Yayınlanan `RobotSaver.scr` ve `RobotSaver.exe` aynı programdır ve .NET 9 Windows Desktop Runtime gerektirir. `.scr /s` tam ekran ekran koruyucuyu, `.scr /c` ayarları, Windows'un kullandığı `.scr /p <HWND>` ise küçük önizlemeyi açar. Argümansız çalıştırma ayarları açar. `/play` masaüstü üzerinde kontrollü tam ekran sürüşü, `/demo Dirty Roomba` pencere demosudur.

Ekran koruyucu açılmadan önce her monitörün görüntüsünü yalnız belleğe alır. Ekran koruyucunun kendi penceresi yakalanmaz ve görüntü dosyaya kaydedilmez. `/s` modunda ilk 0,6 saniyeden sonra tuş, tıklama veya belirlenen eşiği aşan fare hareketi bütün monitörlerde ekran koruyucuyu kapatır.

### Etkileşimli çalıştırma

Robotu klavyeyle sürmek için paketi açtığınız klasörde PowerShell çalıştırın:

```powershell
Start-Process -FilePath .\RobotSaver.scr -ArgumentList '/play'
```

WASD veya yön tuşları sürüş yapar; `Shift` turbo, `H` yuvaya dönüş, `Esc` çıkıştır. Standart `/s` ekran koruyucu modunda Windows davranışına uygun olarak klavye ve fare ekran koruyucuyu kapatır.

| Seçenek | Değerler |
|---|---|
| Davranış | `Clean`, `Dirty`, `Mixed` |
| Hareket | `Roomba`, `Spiral`, `WallFollow`, `Wander` |

## Tuşlar

| Tuş | İşlem |
|---|---|
| WASD / oklar | İlk robotu elle sür; manuel moda geçer |
| Shift | Manuel sürüşte turbo |
| Enter | Manuel / otopilot |
| H | Şarj yuvasına dön |
| Space | Duraklat / devam |
| C | Yeni ortam kiri ve kirletme izlerini aç/kapat |
| E | Hazneyi boşalt |
| K | Kırıntı ve konfeti serp |
| M | Mini harita |
| R | Dünyayı sıfırla |
| F1 | Tuş yardımını gizle/göster |
| Esc | Demoyu kapat |

Şarja dönüş ve şarj durumları manuel sürüşten önceliklidir. **E haznedeki sayacı boşaltır; yerdeki kirleri silmez.** Boşaltma veya zaten boş hazne için ekranda mesaj gösterilir. Aktif boşaltma animasyonu E ile sonlanır. Ses henüz uygulanmadı; `N` tuşu bunu belirten mesaj gösterir. Bildirimler uygulamanın çizdiği, “Şaka bildirimi” olarak işaretlenmiş görsellerdir; gerçek Windows bildirimi gönderilmez.

## Görsel çıktı

20 saniye simüle edip son kareyi PNG olarak kaydetmek için:

```powershell
Start-Process -FilePath .\artifacts\stage3\RobotSaver.exe -ArgumentList '/render artifacts/simulation.png 20 Dirty Roomba' -WindowStyle Hidden -Wait
```

`/render <png> [saniye] [davranış] [hareket]` masaüstünü kaplamadan, sabit 1280×800 alanda çalışır. Saniye verilmezse başlangıç karesi kaydedilir. Ondalık ayırıcı noktadır; 0–3600 arası sonlu değer kabul edilir. Üst klasör mevcut olmalıdır. Başarılı işlem 0, geçersiz komut/yazma hatası 1 çıkış kodu döndürür. Render ve demo düz arka plan kullanır; masaüstü görüntüsü almaz. Aynı ayarlar ve başlangıç tohumu aynı simülasyonu üretir.

Önceki aşamanın statik robot çizim panosu `/artwork <png> [saniye]` komutundadır. `/install` ve `/makeicon` henüz desteklenmez.

## Ayarlar ve görsel

`%APPDATA%\RobotSaver\settings.json` ayarları okunur. `/c` şu anda arka plan türünü ve pil süresini değiştirir; resim arka planı seçildiğinde geçerli bir dosya ister. Tam ayar arayüzü sonraki aşamadır. Yeni kurulumda pil süresi 20 dakikadır. Mevcut ayar dosyasındaki değer korunur. Eksik dosyada varsayılanlar kullanılır; bozuk dosya yüklemede değiştirilmez. Demo ve render ayar kaydetmez.

EXE yanında veya `%APPDATA%\RobotSaver` içinde isteğe bağlı `robot.png` aranır: şeffaf arka planlı, üstten görünüş, ön taraf yukarı, en fazla 4096×4096. Dosyayı yayın klasörüne elle kopyalayın. Görsel okunamazsa vektör robot çizilir.

## Sınırlar

- Kir/parçacık sayısı 8000 ile sınırlıdır. Sınıra ulaşınca yeni kir eklenmez; temizleme, solma veya sıfırlama yer açar.
- Dönüş, kaydedilmiş yolu izler; engel varsa yeniden rota planlar ve karşılıklı beklemede yol verir. Fiziksel geçit veya yuva tamamen kapalıysa engelin kalkmasını bekler. Bütün olası kalabalık/dar ekran düzenleri için ilerleme garantisi yoktur.
- Küçük önizleme alanlarında robot boyutu ve sığan robot sayısı azaltılabilir.
- Çoklu ekran pencereleri ve sentetik ekran geometrileri test edildi. Bu bilgisayarda iki ekran görüldü; yayınlanan `.scr /s` gerçek süreç olarak başlatılıp klavye girdisiyle kapatıldı.
- Ses, tam ayar penceresi ve kurulum sonraki işlerdir. Windows'un seçili ekran koruyucu kayıt defteri ayarı değiştirilmedi; `.scr` dosyası hazırlandı fakat sisteme kurulmadı.

Güncel aşama ve test kanıtları: [PROJECT_NOTES.md](PROJECT_NOTES.md). Başka bir YZ ile devam: [AI_HANDOFF_PROMPT.md](AI_HANDOFF_PROMPT.md).

## Lisans

[MIT](LICENSE) — kullanabilir, değiştirebilir ve dağıtabilirsiniz; lisans ve telif bildirimi korunmalıdır.
