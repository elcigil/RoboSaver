using System.Globalization;

namespace RobotSaver;

public sealed record ToastText(string Title, string Body, string Emoji)
{
    public string Format(int batteryPercent, int binPercent) => string.Format(CultureInfo.CurrentCulture, Body, Math.Clamp(batteryPercent, 0, 100), Math.Clamp(binPercent, 0, 100));
}

public static class L
{
    public static string T(string tr, string en) => Settings.Current.Language == "en" ? en : tr;
    private static string[] Lines(string[] tr, string[] en) => Settings.Current.Language == "en" ? en : tr;
    public static string[] Bump => Lines(["Duvarı kim buraya koydu?", "Ben çarpmadım, masaüstü kaydı.", "Tampon testi başarılı!"], ["Who put that wall here?", "The desktop moved, I swear.", "Bumper test passed!"]);
    public static string[] Stuck => Lines(["Yardım edin!", "Halı beni bırakmıyor.", "Kurtarma ekibi nerede?"], ["Help!", "The carpet won't let go.", "Where's the rescue team?"]);
    public static string[] BinFull => Lines(["Hazne dolu. Konfeti zamanı!", "Bunu sen mi yedin?"], ["Bin full. Confetti time!", "Did you eat all this?"]);
    public static string[] BinFullHome => Lines(["Hazne doldu, eve dönüyorum.", "Çöp molası!"], ["Bin full. Heading home.", "Trash break!"]);
    public static string[] AfterDump => Lines(["Çağdaş sanat yaptım.", "Burası zaten böyleydi."], ["It's modern art.", "It was like this already."]);
    public static string[] LowBattery => Lines(["Pil azaldı, kahve lazım.", "Priz nerede?"], ["Low battery. Need coffee.", "Where's the outlet?"]);
    public static string[] Docked => Lines(["Şarj oluyorum, rahatsız etmeyin.", "Ev gibisi yok."], ["Charging. Do not disturb.", "Home sweet home."]);
    public static string[] DirtyLines => Lines(["Temizliğe kendi yorumumu kattım.", "Biraz da buraya kırıntı."], ["My own take on cleaning.", "A few crumbs over here."]);
    public static ToastText[] Notifications => Settings.Current.Language == "en" ?
    [
        new("Remote control", "Connection lost. Battery {0}%", "😠"),
        new("Bin full", "Did you eat this? Bin {1}%", "🍪"),
        new("Windows Update", "30% more crumbs", "🪟"),
        new("Calendar", "Desktop cleanup postponed again", "📅"),
        new("Outlook", "Meeting about the stain on the carpet", "✉"),
        new("Antivirus", "1 dust threat quarantined", "🛡"),
        new("Cat", "I want a ride on the robot", "🐈")
    ] :
    [
        new("Uzaktan kumanda", "Bağlantı koptu. Pil %{0}", "😠"),
        new("Hazne dolu", "Bunu sen mi yedin? Hazne %{1}", "🍪"),
        new("Windows Update", "%30 daha fazla kırıntı", "🪟"),
        new("Takvim", "Masaüstü temizliği yine ertelendi", "📅"),
        new("Outlook", "Halıdaki leke hakkında toplantı", "✉"),
        new("Antivirüs", "1 toz tehdidi karantinada", "🛡"),
        new("Kedi", "Robota binmek istiyorum", "🐈")
    ];
}
