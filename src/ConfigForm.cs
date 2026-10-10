namespace RobotSaver;

// Initial Windows /c dialog. Advanced settings will be added without changing /c.
public sealed class ConfigForm : Form
{
    public ConfigForm()
    {
        Text = "RobotSaver — " + L.T("Ayarlar", "Settings");
        ClientSize = new Size(460, 295);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        var settings = Settings.Current.Clone();
        var label = new Label { Text = L.T("Pil süresi (dakika)", "Battery duration (minutes)"), Location = new Point(24, 25), AutoSize = true };
        var battery = new NumericUpDown { Minimum = 1, Maximum = 120, Value = Math.Clamp((decimal)settings.BatteryMinutes, 1, 120), Location = new Point(280, 22), Width = 130 };
        var backgroundLabel = new Label { Text = L.T("Arka plan", "Background"), Location = new Point(24, 66), AutoSize = true };
        var background = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(180, 62), Width = 230 };
        background.Items.AddRange([L.T("Masaüstü görüntüsü", "Desktop screenshot"), L.T("Düz renk", "Solid color"), L.T("Resim dosyası", "Image file")]);
        background.SelectedIndex = (int)settings.Background;
        var imageLabel = new Label { Text = L.T("Resim", "Image"), Location = new Point(24, 105), AutoSize = true };
        var imagePath = new TextBox { Text = settings.BgImage, Location = new Point(85, 101), Width = 275 };
        var browse = new Button { Text = "…", Location = new Point(370, 99), Size = new Size(40, 27) };
        void UpdateImageControls() => imageLabel.Enabled = imagePath.Enabled = browse.Enabled = background.SelectedIndex == (int)BgMode.Image;
        background.SelectedIndexChanged += (_, _) => UpdateImageControls();
        browse.Click += (_, _) =>
        {
            using var dialog = new OpenFileDialog { Filter = L.T("Resim dosyaları|*.png;*.jpg;*.jpeg;*.bmp|Tüm dosyalar|*.*", "Image files|*.png;*.jpg;*.jpeg;*.bmp|All files|*.*"), CheckFileExists = true };
            if (dialog.ShowDialog(this) == DialogResult.OK) imagePath.Text = dialog.FileName;
        };
        UpdateImageControls();
        var info = new Label { Text = L.T("Ekran koruyucuda fare/tuş çıkış yapar.\nSürüş için RobotSaver.exe /play kullanın.\nMasaüstü görüntüsü yalnızca bellekte tutulur.", "Mouse/keyboard exits the screensaver.\nUse RobotSaver.exe /play for driving.\nThe desktop screenshot is kept in memory only."), Location = new Point(24, 145), Size = new Size(410, 70) };
        var save = new Button { Text = L.T("Kaydet", "Save"), Location = new Point(225, 235), Width = 90 };
        var cancel = new Button { Text = L.T("İptal", "Cancel"), Location = new Point(325, 235), Width = 90, DialogResult = DialogResult.Cancel };
        save.Click += (_, _) =>
        {
            try
            {
                if (background.SelectedIndex == (int)BgMode.Image && !File.Exists(imagePath.Text))
                {
                    MessageBox.Show(this, L.T("Geçerli bir arka plan resmi seçin.", "Select a valid background image."), "RobotSaver", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                settings.BatteryMinutes = (float)battery.Value;
                settings.Background = (BgMode)background.SelectedIndex;
                settings.BgImage = imagePath.Text;
                settings.Save(); Close();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { MessageBox.Show(this, ex.Message, "RobotSaver", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        };
        Controls.AddRange([label, battery, backgroundLabel, background, imageLabel, imagePath, browse, info, save, cancel]);
        AcceptButton = save; CancelButton = cancel;
    }
}
