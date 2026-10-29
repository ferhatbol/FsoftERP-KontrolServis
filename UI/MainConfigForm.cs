using System.Diagnostics;
using FsoftERP.KontrolServis.Configuration;
using FsoftERP.KontrolServis.Services;

namespace FsoftERP.KontrolServis.UI;

public class MainConfigForm : Form
{
    private readonly AppConfig _config;
    private readonly SqlService _sqlService;

    // Header Controls
    private Button _btnHeaderSave = null!;

    // Tab 1: Database
    private TextBox _txtServer = null!;
    private TextBox _txtDatabase = null!;
    private CheckBox _chkWindowsAuth = null!;
    private TextBox _txtUserId = null!;
    private TextBox _txtPassword = null!;
    private Button _btnTogglePassword = null!;
    private NumericUpDown _numConnTimeout = null!;
    private Button _btnTestConnection = null!;
    private Label _lblConnTestResult = null!;

    // Tab 2: Depo Stok Tarihce
    private CheckBox _chkDepoEnabled = null!;
    private ListBox _lstDepoTimes = null!;
    private DateTimePicker _dtpNewTime = null!;
    private Button _btnAddTime = null!;
    private Button _btnRemoveTime = null!;
    private Button _btnClearTimes = null!;
    private Button _btnPreset2Times = null!;
    private Button _btnPreset3Times = null!;
    private NumericUpDown _numDepoTimeout = null!;
    private Button _btnRunDepoNow = null!;

    // Tab 3: Sparis Durum Kontrol
    private CheckBox _chkSparisEnabled = null!;
    private NumericUpDown _numSparisInterval = null!;
    private TextBox _txtSparisNo = null!;
    private NumericUpDown _numSparisTimeout = null!;
    private Button _btnRunSparisNow = null!;

    // Tab 4: Service & Logs
    private Label _lblServiceStatus = null!;
    private Button _btnInstallService = null!;
    private Button _btnUninstallService = null!;
    private Button _btnStartService = null!;
    private Button _btnStopService = null!;
    private Button _btnRefreshService = null!;
    private RichTextBox _rtbLogs = null!;
    private Button _btnClearLogs = null!;
    private Button _btnOpenLogsFolder = null!;

    // Bottom Bar
    private Label _lblStatus = null!;
    private Button _btnBottomSave = null!;
    private Button _btnOpenAppFolder = null!;

    public MainConfigForm()
    {
        _config = AppConfig.Load();
        _sqlService = new SqlService(_config);

        InitializeComponent();
        LoadConfigToUi();
        UpdateServiceStatus();

        AppLogger.OnLogMessage += AppendLogMessage;
        AppLogger.Info("Yönetim Arayüzü açıldı. v1.0.0.1");
    }

    private void InitializeComponent()
    {
        Text = "FsoftERP Kontrol Servisi - Ayar ve Yönetim Paneli (v1.0.0.1)";
        Size = new Size(980, 750);
        MinimumSize = new Size(880, 640);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9.25f, FontStyle.Regular);
        BackColor = Color.FromArgb(245, 247, 250);
        KeyPreview = true;
        KeyDown += (s, e) =>
        {
            if (e.Control && e.KeyCode == Keys.S)
            {
                e.SuppressKeyPress = true;
                SaveConfigFromUi();
            }
        };

        try
        {
            string icoPath = Path.Combine(AppContext.BaseDirectory, "Resources", "fsoft.ico");
            if (File.Exists(icoPath))
                Icon = new Icon(icoPath);
            else if (Icon.ExtractAssociatedIcon(Application.ExecutablePath) is { } assoc)
                Icon = assoc;
        }
        catch { }

        // Root Layout
        var rootTable = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = Color.White
        };
        rootTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        rootTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 70f)); // Header
        rootTable.RowStyles.Add(new RowStyle(SizeType.Percent, 100f)); // Tabs
        rootTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 52f)); // Bottom bar

        // 1. Header
        rootTable.Controls.Add(CreateHeaderPanel(), 0, 0);

        // 2. Tab Control
        var tabControl = new TabControl
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
            Padding = new Point(14, 8)
        };

        var tabDb = new TabPage("🗄️ Veritabanı Bağlantısı") { BackColor = Color.White, Padding = new Padding(15) };
        tabDb.Controls.Add(CreateDatabaseTab());
        tabControl.TabPages.Add(tabDb);

        var tabDepo = new TabPage("📦 Depo Stok Tarihçe") { BackColor = Color.White, Padding = new Padding(15) };
        tabDepo.Controls.Add(CreateDepoTab());
        tabControl.TabPages.Add(tabDepo);

        var tabSparis = new TabPage("📋 Sipariş Durum Kontrol") { BackColor = Color.White, Padding = new Padding(15) };
        tabSparis.Controls.Add(CreateSparisTab());
        tabControl.TabPages.Add(tabSparis);

        var tabService = new TabPage("⚙️ Servis Yönetimi & Loglar") { BackColor = Color.White, Padding = new Padding(15) };
        tabService.Controls.Add(CreateServiceTab());
        tabControl.TabPages.Add(tabService);

        rootTable.Controls.Add(tabControl, 0, 1);

        // 3. Bottom Bar
        rootTable.Controls.Add(CreateBottomBar(), 0, 2);

        Controls.Add(rootTable);
    }

    #region Layout Panels
    private Panel CreateHeaderPanel()
    {
        var pnl = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(15, 23, 42), // Slate 900
            Padding = new Padding(15, 10, 15, 10)
        };

        // Fsoft Logo or Icon
        var picLogo = new PictureBox
        {
            Size = new Size(130, 48),
            Location = new Point(15, 11),
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.Transparent
        };

        string logoPath = Path.Combine(AppContext.BaseDirectory, "Resources", "fsoft_logo.png");
        string iconPath = Path.Combine(AppContext.BaseDirectory, "Resources", "fsoft_icon.png");
        if (File.Exists(logoPath))
        {
            try { picLogo.Image = Image.FromFile(logoPath); } catch { }
        }
        else if (File.Exists(iconPath))
        {
            try { picLogo.Image = Image.FromFile(iconPath); } catch { }
        }

        var lblTitle = new Label
        {
            Text = "FsoftERP Kontrol Servisi",
            Font = new Font("Segoe UI", 12.5f, FontStyle.Bold),
            ForeColor = Color.White,
            Location = new Point(155, 11),
            AutoSize = true
        };

        var lblSub = new Label
        {
            Text = "Depo Stok Tarihçe (Saatlik) & Sipariş Durum Kontrol (Periyodik) | FsoftERPSKWS v1.0.0.1",
            Font = new Font("Segoe UI", 8.75f, FontStyle.Regular),
            ForeColor = Color.FromArgb(148, 163, 184),
            Location = new Point(156, 36),
            AutoSize = true
        };

        // Header Save Button (Top Right)
        var pnlRight = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            AutoSize = true,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 4, 5, 0)
        };

        _btnHeaderSave = new Button
        {
            Text = "💾 AYARLARI KAYDET (Ctrl+S)",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            BackColor = Color.FromArgb(22, 163, 74), // Emerald green
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(230, 42),
            Cursor = Cursors.Hand
        };
        _btnHeaderSave.FlatAppearance.BorderSize = 0;
        _btnHeaderSave.Click += (s, e) => SaveConfigFromUi();
        pnlRight.Controls.Add(_btnHeaderSave);

        pnl.Controls.AddRange(new Control[] { picLogo, lblTitle, lblSub, pnlRight });
        return pnl;
    }

    private Control CreateDatabaseTab()
    {
        var pnl = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(10) };

        var grp = new GroupBox
        {
            Text = "MSSQL Server Bağlantı Parametreleri",
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            Location = new Point(10, 10),
            Size = new Size(760, 360)
        };

        int leftLabel = 20;
        int leftControl = 220;
        int widthControl = 380;
        int rowHeight = 40;
        int top = 35;

        // Server
        var lblServer = new Label { Text = "Sunucu / Instance:", Location = new Point(leftLabel, top + 3), AutoSize = true, Font = new Font("Segoe UI", 9.25f) };
        _txtServer = new TextBox { Location = new Point(leftControl, top), Size = new Size(widthControl, 26), Font = new Font("Segoe UI", 9.25f) };
        grp.Controls.AddRange(new Control[] { lblServer, _txtServer });

        // Database
        top += rowHeight;
        var lblDb = new Label { Text = "Veritabanı Adı:", Location = new Point(leftLabel, top + 3), AutoSize = true, Font = new Font("Segoe UI", 9.25f) };
        _txtDatabase = new TextBox { Location = new Point(leftControl, top), Size = new Size(widthControl, 26), Font = new Font("Segoe UI", 9.25f) };
        grp.Controls.AddRange(new Control[] { lblDb, _txtDatabase });

        // Windows Auth Checkbox
        top += rowHeight;
        _chkWindowsAuth = new CheckBox
        {
            Text = "Windows Kimlik Doğrulaması Kullan (Integrated Security)",
            Location = new Point(leftControl, top),
            Size = new Size(widthControl, 24),
            Font = new Font("Segoe UI", 9.25f)
        };
        _chkWindowsAuth.CheckedChanged += (s, e) =>
        {
            _txtUserId.Enabled = !_chkWindowsAuth.Checked;
            _txtPassword.Enabled = !_chkWindowsAuth.Checked;
            _btnTogglePassword.Enabled = !_chkWindowsAuth.Checked;
        };
        grp.Controls.Add(_chkWindowsAuth);

        // User ID
        top += rowHeight;
        var lblUser = new Label { Text = "Kullanıcı Adı (SQL Auth):", Location = new Point(leftLabel, top + 3), AutoSize = true, Font = new Font("Segoe UI", 9.25f) };
        _txtUserId = new TextBox { Location = new Point(leftControl, top), Size = new Size(widthControl, 26), Font = new Font("Segoe UI", 9.25f) };
        grp.Controls.AddRange(new Control[] { lblUser, _txtUserId });

        // Password
        top += rowHeight;
        var lblPass = new Label { Text = "Şifre (SQL Auth):", Location = new Point(leftLabel, top + 3), AutoSize = true, Font = new Font("Segoe UI", 9.25f) };
        _txtPassword = new TextBox { Location = new Point(leftControl, top), Size = new Size(widthControl - 50, 26), UseSystemPasswordChar = true, Font = new Font("Segoe UI", 9.25f) };
        _btnTogglePassword = new Button
        {
            Text = "👁️",
            Location = new Point(leftControl + widthControl - 45, top),
            Size = new Size(45, 26),
            Cursor = Cursors.Hand
        };
        _btnTogglePassword.Click += (s, e) =>
        {
            _txtPassword.UseSystemPasswordChar = !_txtPassword.UseSystemPasswordChar;
        };
        grp.Controls.AddRange(new Control[] { lblPass, _txtPassword, _btnTogglePassword });

        // Timeout
        top += rowHeight;
        var lblTimeout = new Label { Text = "Bağlantı Zaman Aşımı (sn):", Location = new Point(leftLabel, top + 3), AutoSize = true, Font = new Font("Segoe UI", 9.25f) };
        _numConnTimeout = new NumericUpDown { Location = new Point(leftControl, top), Size = new Size(100, 26), Minimum = 5, Maximum = 300, Value = 30, Font = new Font("Segoe UI", 9.25f) };
        grp.Controls.AddRange(new Control[] { lblTimeout, _numConnTimeout });

        // Test Connection Button & Result
        top += 48;
        _btnTestConnection = new Button
        {
            Text = "🔌 Bağlantıyı Test Et",
            Location = new Point(leftControl, top),
            Size = new Size(200, 36),
            BackColor = Color.FromArgb(13, 110, 253),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        _btnTestConnection.FlatAppearance.BorderSize = 0;
        _btnTestConnection.Click += async (s, e) => await OnTestConnectionClick();

        _lblConnTestResult = new Label
        {
            Text = "Bağlantı henüz test edilmedi.",
            Location = new Point(leftControl + 215, top + 8),
            AutoSize = true,
            Font = new Font("Segoe UI", 9.25f, FontStyle.Italic),
            ForeColor = Color.FromArgb(100, 116, 139)
        };
        grp.Controls.AddRange(new Control[] { _btnTestConnection, _lblConnTestResult });

        pnl.Controls.Add(grp);
        return pnl;
    }

    private Control CreateDepoTab()
    {
        var pnl = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(10) };

        _chkDepoEnabled = new CheckBox
        {
            Text = "✅ Sp_Set_DepoStokTarihce Görevini Etkinleştir (Aktif / Pasif)",
            Location = new Point(15, 15),
            Size = new Size(600, 28),
            Font = new Font("Segoe UI", 11f, FontStyle.Bold),
            ForeColor = Color.FromArgb(15, 23, 42)
        };
        pnl.Controls.Add(_chkDepoEnabled);

        var grp = new GroupBox
        {
            Text = "Çalışma Saatleri ve Parametreleri (Günde Belirtilen Saatlerde Çalışır)",
            Font = new Font("Segoe UI", 9.75f, FontStyle.Bold),
            Location = new Point(10, 50),
            Size = new Size(760, 440)
        };

        var lblTimes = new Label { Text = "Planlanan Saatler Listesi (HH:mm):", Location = new Point(20, 30), AutoSize = true, Font = new Font("Segoe UI", 9.25f) };
        _lstDepoTimes = new ListBox
        {
            Location = new Point(20, 55),
            Size = new Size(220, 200),
            Font = new Font("Consolas", 11f, FontStyle.Bold)
        };

        // Time add controls
        var lblNewTime = new Label { Text = "Saat Seç:", Location = new Point(260, 55), AutoSize = true, Font = new Font("Segoe UI", 9f) };
        _dtpNewTime = new DateTimePicker
        {
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "HH:mm",
            ShowUpDown = true,
            Location = new Point(260, 78),
            Size = new Size(110, 26),
            Font = new Font("Segoe UI", 10f)
        };

        _btnAddTime = new Button
        {
            Text = "➕ Saati Ekle",
            Location = new Point(380, 77),
            Size = new Size(120, 28),
            BackColor = Color.FromArgb(22, 163, 74),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        _btnAddTime.FlatAppearance.BorderSize = 0;
        _btnAddTime.Click += (s, e) =>
        {
            string newTime = _dtpNewTime.Value.ToString("HH:mm");
            if (!_lstDepoTimes.Items.Contains(newTime))
            {
                _lstDepoTimes.Items.Add(newTime);
                SortDepoTimesList();
            }
        };

        _btnRemoveTime = new Button
        {
            Text = "➖ Seçili Saati Sil",
            Location = new Point(260, 115),
            Size = new Size(140, 28),
            Cursor = Cursors.Hand
        };
        _btnRemoveTime.Click += (s, e) =>
        {
            if (_lstDepoTimes.SelectedIndex >= 0)
                _lstDepoTimes.Items.RemoveAt(_lstDepoTimes.SelectedIndex);
        };

        _btnClearTimes = new Button
        {
            Text = "🧹 Tüm Saatleri Temizle",
            Location = new Point(410, 115),
            Size = new Size(160, 28),
            Cursor = Cursors.Hand
        };
        _btnClearTimes.Click += (s, e) => _lstDepoTimes.Items.Clear();

        // Preset templates
        var lblPresets = new Label { Text = "Hızlı Şablonlar:", Location = new Point(260, 160), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
        _btnPreset2Times = new Button
        {
            Text = "📌 Günde 2 Kez (12:00, 19:00)",
            Location = new Point(260, 185),
            Size = new Size(240, 30),
            Cursor = Cursors.Hand
        };
        _btnPreset2Times.Click += (s, e) =>
        {
            _lstDepoTimes.Items.Clear();
            _lstDepoTimes.Items.Add("12:00");
            _lstDepoTimes.Items.Add("19:00");
        };

        _btnPreset3Times = new Button
        {
            Text = "📌 Günde 3 Kez (06:00, 12:00, 21:00)",
            Location = new Point(260, 222),
            Size = new Size(240, 30),
            Cursor = Cursors.Hand
        };
        _btnPreset3Times.Click += (s, e) =>
        {
            _lstDepoTimes.Items.Clear();
            _lstDepoTimes.Items.Add("06:00");
            _lstDepoTimes.Items.Add("12:00");
            _lstDepoTimes.Items.Add("21:00");
        };

        // Timeout
        var lblTimeout = new Label { Text = "Komut Zaman Aşımı (Timeout sn):", Location = new Point(20, 280), AutoSize = true, Font = new Font("Segoe UI", 9.25f) };
        _numDepoTimeout = new NumericUpDown
        {
            Location = new Point(260, 277),
            Size = new Size(120, 26),
            Minimum = 10,
            Maximum = 7200,
            Value = 1000
        };

        // Manual Run Button
        _btnRunDepoNow = new Button
        {
            Text = "▶️ Sp_Set_DepoStokTarihce Şimdi Çalıştır (Manuel Test)",
            Location = new Point(20, 330),
            Size = new Size(420, 42),
            BackColor = Color.FromArgb(13, 110, 253),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.75f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        _btnRunDepoNow.FlatAppearance.BorderSize = 0;
        _btnRunDepoNow.Click += async (s, e) =>
        {
            _btnRunDepoNow.Enabled = false;
            _btnRunDepoNow.Text = "Çalıştırılıyor, lütfen bekleyin...";
            try
            {
                var cfg = BuildCurrentConfig();
                var sql = new SqlService(cfg);
                var res = await sql.ExecuteDepoStokTarihceAsync();
                MessageBox.Show(this, res.Message, res.Success ? "Başarılı" : "Hata", MessageBoxButtons.OK, res.Success ? MessageBoxIcon.Information : MessageBoxIcon.Error);
            }
            finally
            {
                _btnRunDepoNow.Enabled = true;
                _btnRunDepoNow.Text = "▶️ Sp_Set_DepoStokTarihce Şimdi Çalıştır (Manuel Test)";
            }
        };

        grp.Controls.AddRange(new Control[]
        {
            lblTimes, _lstDepoTimes, lblNewTime, _dtpNewTime, _btnAddTime, _btnRemoveTime, _btnClearTimes,
            lblPresets, _btnPreset2Times, _btnPreset3Times,
            lblTimeout, _numDepoTimeout, _btnRunDepoNow
        });

        pnl.Controls.Add(grp);
        return pnl;
    }

    private Control CreateSparisTab()
    {
        var pnl = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(10) };

        _chkSparisEnabled = new CheckBox
        {
            Text = "✅ Sp_Set_SparisDurumKontrol Görevini Etkinleştir (Aktif / Pasif)",
            Location = new Point(15, 15),
            Size = new Size(600, 28),
            Font = new Font("Segoe UI", 11f, FontStyle.Bold),
            ForeColor = Color.FromArgb(15, 23, 42)
        };
        pnl.Controls.Add(_chkSparisEnabled);

        var grp = new GroupBox
        {
            Text = "Periyodik Çalışma Parametreleri (Dakika Aralıklarıyla Çalışır)",
            Font = new Font("Segoe UI", 9.75f, FontStyle.Bold),
            Location = new Point(10, 50),
            Size = new Size(760, 360)
        };

        int leftLabel = 20;
        int leftControl = 260;
        int top = 35;

        // Interval
        var lblInterval = new Label { Text = "Çalışma Aralığı (Dakika):", Location = new Point(leftLabel, top + 3), AutoSize = true, Font = new Font("Segoe UI", 9.25f) };
        _numSparisInterval = new NumericUpDown
        {
            Location = new Point(leftControl, top),
            Size = new Size(120, 26),
            Minimum = 1,
            Maximum = 1440,
            Value = 3,
            Font = new Font("Segoe UI", 9.25f)
        };
        var lblIntervalHint = new Label
        {
            Text = "(Örn: 3 yazarsanız her 3 dakikada bir otomatik kontrol edilir)",
            Location = new Point(leftControl + 130, top + 4),
            AutoSize = true,
            ForeColor = Color.FromArgb(100, 116, 139),
            Font = new Font("Segoe UI", 8.75f)
        };
        grp.Controls.AddRange(new Control[] { lblInterval, _numSparisInterval, lblIntervalHint });

        // SiparisNo parameter
        top += 45;
        var lblSiparisNo = new Label { Text = "Sipariş No Parametresi:", Location = new Point(leftLabel, top + 3), AutoSize = true, Font = new Font("Segoe UI", 9.25f) };
        _txtSparisNo = new TextBox { Location = new Point(leftControl, top), Size = new Size(240, 26), Font = new Font("Segoe UI", 9.25f) };
        var lblSiparisNoHint = new Label
        {
            Text = "(Boş bırakılırsa @SiparisNo = NULL gönderilir, tüm siparişler kontrol edilir)",
            Location = new Point(leftControl + 250, top + 4),
            AutoSize = true,
            ForeColor = Color.FromArgb(100, 116, 139),
            Font = new Font("Segoe UI", 8.75f)
        };
        grp.Controls.AddRange(new Control[] { lblSiparisNo, _txtSparisNo, lblSiparisNoHint });

        // Timeout
        top += 45;
        var lblTimeout = new Label { Text = "Komut Zaman Aşımı (Timeout sn):", Location = new Point(leftLabel, top + 3), AutoSize = true, Font = new Font("Segoe UI", 9.25f) };
        _numSparisTimeout = new NumericUpDown
        {
            Location = new Point(leftControl, top),
            Size = new Size(120, 26),
            Minimum = 10,
            Maximum = 7200,
            Value = 1000,
            Font = new Font("Segoe UI", 9.25f)
        };
        grp.Controls.AddRange(new Control[] { lblTimeout, _numSparisTimeout });

        // Manual Run Button
        top += 60;
        _btnRunSparisNow = new Button
        {
            Text = "▶️ Sp_Set_SparisDurumKontrol Şimdi Çalıştır (Manuel Test)",
            Location = new Point(leftLabel, top),
            Size = new Size(440, 42),
            BackColor = Color.FromArgb(13, 110, 253),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.75f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        _btnRunSparisNow.FlatAppearance.BorderSize = 0;
        _btnRunSparisNow.Click += async (s, e) =>
        {
            _btnRunSparisNow.Enabled = false;
            _btnRunSparisNow.Text = "Çalıştırılıyor, lütfen bekleyin...";
            try
            {
                var cfg = BuildCurrentConfig();
                var sql = new SqlService(cfg);
                var res = await sql.ExecuteSparisDurumKontrolAsync();
                MessageBox.Show(this, res.Message, res.Success ? "Başarılı" : "Hata", MessageBoxButtons.OK, res.Success ? MessageBoxIcon.Information : MessageBoxIcon.Error);
            }
            finally
            {
                _btnRunSparisNow.Enabled = true;
                _btnRunSparisNow.Text = "▶️ Sp_Set_SparisDurumKontrol Şimdi Çalıştır (Manuel Test)";
            }
        };
        grp.Controls.Add(_btnRunSparisNow);

        pnl.Controls.Add(grp);
        return pnl;
    }

    private Control CreateServiceTab()
    {
        var pnl = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(10) };

        // Group 1: Windows Service Management
        var grpService = new GroupBox
        {
            Text = "Windows Arka Plan Servis Yönetimi (FsoftERPSKWS)",
            Font = new Font("Segoe UI", 9.75f, FontStyle.Bold),
            Location = new Point(10, 10),
            Size = new Size(760, 150)
        };

        var lblStatusTitle = new Label { Text = "Servis Durumu:", Location = new Point(20, 30), AutoSize = true, Font = new Font("Segoe UI", 9.25f) };
        _lblServiceStatus = new Label
        {
            Text = "Kontrol ediliyor...",
            Location = new Point(130, 28),
            AutoSize = true,
            Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(100, 116, 139)
        };
        grpService.Controls.AddRange(new Control[] { lblStatusTitle, _lblServiceStatus });

        // Service buttons
        _btnInstallService = new Button
        {
            Text = "🛠️ Servisi Kur",
            Location = new Point(20, 70),
            Size = new Size(130, 36),
            BackColor = Color.FromArgb(245, 247, 250),
            Cursor = Cursors.Hand
        };
        _btnInstallService.Click += (s, e) =>
        {
            var cfg = BuildCurrentConfig();
            var res = ServiceManager.InstallService(cfg);
            MessageBox.Show(this, res.Message, res.Success ? "Bilgi" : "Hata", MessageBoxButtons.OK, res.Success ? MessageBoxIcon.Information : MessageBoxIcon.Error);
            UpdateServiceStatus();
        };

        _btnUninstallService = new Button
        {
            Text = "🗑️ Servisi Kaldır",
            Location = new Point(160, 70),
            Size = new Size(130, 36),
            BackColor = Color.FromArgb(245, 247, 250),
            Cursor = Cursors.Hand
        };
        _btnUninstallService.Click += (s, e) =>
        {
            if (MessageBox.Show(this, "Windows Servisini kaldırmak istediğinize emin misiniz?", "Onay", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                var res = ServiceManager.UninstallService(_config.Service.ServiceName);
                MessageBox.Show(this, res.Message, res.Success ? "Bilgi" : "Hata", MessageBoxButtons.OK, res.Success ? MessageBoxIcon.Information : MessageBoxIcon.Error);
                UpdateServiceStatus();
            }
        };

        _btnStartService = new Button
        {
            Text = "▶️ Servisi Başlat",
            Location = new Point(300, 70),
            Size = new Size(130, 36),
            BackColor = Color.FromArgb(22, 163, 74),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        _btnStartService.FlatAppearance.BorderSize = 0;
        _btnStartService.Click += (s, e) =>
        {
            var res = ServiceManager.StartService(_config.Service.ServiceName);
            UpdateServiceStatus();
        };

        _btnStopService = new Button
        {
            Text = "⏹️ Servisi Durdur",
            Location = new Point(440, 70),
            Size = new Size(130, 36),
            BackColor = Color.FromArgb(220, 38, 38),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        _btnStopService.FlatAppearance.BorderSize = 0;
        _btnStopService.Click += (s, e) =>
        {
            var res = ServiceManager.StopService(_config.Service.ServiceName);
            UpdateServiceStatus();
        };

        _btnRefreshService = new Button
        {
            Text = "🔄 Yenile",
            Location = new Point(580, 70),
            Size = new Size(90, 36),
            Cursor = Cursors.Hand
        };
        _btnRefreshService.Click += (s, e) => UpdateServiceStatus();

        grpService.Controls.AddRange(new Control[] { _btnInstallService, _btnUninstallService, _btnStartService, _btnStopService, _btnRefreshService });
        pnl.Controls.Add(grpService);

        // Group 2: Live Logs Console
        var grpLogs = new GroupBox
        {
            Text = "Canlı Konsol ve Olay Günlüğü (Logs)",
            Font = new Font("Segoe UI", 9.75f, FontStyle.Bold),
            Location = new Point(10, 175),
            Size = new Size(760, 350)
        };

        _rtbLogs = new RichTextBox
        {
            Location = new Point(15, 28),
            Size = new Size(730, 270),
            ReadOnly = true,
            BackColor = Color.FromArgb(15, 23, 42),
            ForeColor = Color.FromArgb(226, 232, 240),
            Font = new Font("Consolas", 9.25f),
            WordWrap = true
        };

        _btnClearLogs = new Button
        {
            Text = "🧹 Ekranı Temizle",
            Location = new Point(15, 308),
            Size = new Size(130, 30),
            Cursor = Cursors.Hand
        };
        _btnClearLogs.Click += (s, e) => _rtbLogs.Clear();

        _btnOpenLogsFolder = new Button
        {
            Text = "📁 Log Klasörünü Aç",
            Location = new Point(155, 308),
            Size = new Size(160, 30),
            Cursor = Cursors.Hand
        };
        _btnOpenLogsFolder.Click += (s, e) =>
        {
            string dir = Path.Combine(AppContext.BaseDirectory, "logs");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            Process.Start(new ProcessStartInfo("explorer.exe", dir) { UseShellExecute = true });
        };

        grpLogs.Controls.AddRange(new Control[] { _rtbLogs, _btnClearLogs, _btnOpenLogsFolder });
        pnl.Controls.Add(grpLogs);

        return pnl;
    }

    private Control CreateBottomBar()
    {
        var pnl = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(241, 245, 249),
            Padding = new Padding(15, 8, 15, 8)
        };

        _lblStatus = new Label
        {
            Text = "Durum: Hazır (Değişiklikleri kaydetmek için 'Ayarları Kaydet' butonuna basın veya Ctrl+S)",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Color.FromArgb(100, 116, 139),
            Font = new Font("Segoe UI", 9f)
        };

        var pnlButtons = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            AutoSize = true,
            BackColor = Color.Transparent
        };

        _btnBottomSave = new Button
        {
            Text = "💾 TÜM AYARLARI KAYDET",
            Font = new Font("Segoe UI", 9.75f, FontStyle.Bold),
            BackColor = Color.FromArgb(22, 163, 74),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(220, 36),
            Cursor = Cursors.Hand
        };
        _btnBottomSave.FlatAppearance.BorderSize = 0;
        _btnBottomSave.Click += (s, e) => SaveConfigFromUi();

        _btnOpenAppFolder = new Button
        {
            Text = "📁 Klasörü Aç",
            Font = new Font("Segoe UI", 9f),
            Size = new Size(120, 36),
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 0, 10, 0)
        };
        _btnOpenAppFolder.Click += (s, e) =>
        {
            Process.Start(new ProcessStartInfo("explorer.exe", AppContext.BaseDirectory) { UseShellExecute = true });
        };

        pnlButtons.Controls.Add(_btnBottomSave);
        pnlButtons.Controls.Add(_btnOpenAppFolder);

        pnl.Controls.Add(_lblStatus);
        pnl.Controls.Add(pnlButtons);
        pnlButtons.BringToFront();
        _lblStatus.BringToFront();

        return pnl;
    }
    #endregion

    #region Data Binding & Logic
    private void LoadConfigToUi()
    {
        // Database
        _txtServer.Text = _config.Database.Server;
        _txtDatabase.Text = _config.Database.Database;
        _chkWindowsAuth.Checked = _config.Database.UseWindowsAuth;
        _txtUserId.Text = _config.Database.UserId;
        _txtPassword.Text = _config.Database.PlainPassword;
        _numConnTimeout.Value = Math.Clamp(_config.Database.ConnectionTimeoutSeconds, 5, 300);

        _txtUserId.Enabled = !_chkWindowsAuth.Checked;
        _txtPassword.Enabled = !_chkWindowsAuth.Checked;
        _btnTogglePassword.Enabled = !_chkWindowsAuth.Checked;

        // Depo
        _chkDepoEnabled.Checked = _config.DepoStokTarihce.Enabled;
        _lstDepoTimes.Items.Clear();
        foreach (var t in _config.DepoStokTarihce.ExecutionTimes)
        {
            _lstDepoTimes.Items.Add(t);
        }
        SortDepoTimesList();
        _numDepoTimeout.Value = Math.Clamp(_config.DepoStokTarihce.CommandTimeoutSeconds, 10, 7200);

        // Sparis
        _chkSparisEnabled.Checked = _config.SparisDurumKontrol.Enabled;
        _numSparisInterval.Value = Math.Clamp(_config.SparisDurumKontrol.IntervalMinutes, 1, 1440);
        _txtSparisNo.Text = _config.SparisDurumKontrol.SiparisNo ?? "";
        _numSparisTimeout.Value = Math.Clamp(_config.SparisDurumKontrol.CommandTimeoutSeconds, 10, 7200);
    }

    private AppConfig BuildCurrentConfig()
    {
        var cfg = new AppConfig();

        // Database
        cfg.Database.Server = _txtServer.Text.Trim();
        cfg.Database.Database = _txtDatabase.Text.Trim();
        cfg.Database.UseWindowsAuth = _chkWindowsAuth.Checked;
        cfg.Database.UserId = _txtUserId.Text.Trim();
        cfg.Database.Password = _txtPassword.Text; // Will be protected automatically on save
        cfg.Database.ConnectionTimeoutSeconds = (int)_numConnTimeout.Value;

        // Depo
        cfg.DepoStokTarihce.Enabled = _chkDepoEnabled.Checked;
        cfg.DepoStokTarihce.ExecutionTimes = _lstDepoTimes.Items.Cast<string>().ToList();
        cfg.DepoStokTarihce.CommandTimeoutSeconds = (int)_numDepoTimeout.Value;

        // Sparis
        cfg.SparisDurumKontrol.Enabled = _chkSparisEnabled.Checked;
        cfg.SparisDurumKontrol.IntervalMinutes = (int)_numSparisInterval.Value;
        cfg.SparisDurumKontrol.SiparisNo = string.IsNullOrWhiteSpace(_txtSparisNo.Text) ? null : _txtSparisNo.Text.Trim();
        cfg.SparisDurumKontrol.CommandTimeoutSeconds = (int)_numSparisTimeout.Value;

        // Service
        cfg.Service.ServiceName = _config.Service.ServiceName;
        cfg.Service.DisplayName = _config.Service.DisplayName;
        cfg.Service.Description = _config.Service.Description;

        return cfg;
    }

    private void SaveConfigFromUi()
    {
        try
        {
            var cfg = BuildCurrentConfig();
            AppConfig.Save(AppConfig.DefaultConfigPath, cfg);

            _lblStatus.Text = $"✅ Ayarlar başarıyla kaydedildi ({DateTime.Now:HH:mm:ss})";
            _lblStatus.ForeColor = Color.FromArgb(22, 163, 74);

            MessageBox.Show(this,
                "Tüm ayarlar başarıyla kaydedildi! ✅\n\n" +
                $"• Sunucu: {cfg.Database.Server} / {cfg.Database.Database}\n" +
                $"• Depo Stok Tarihçe: {(cfg.DepoStokTarihce.Enabled ? "Aktif (" + string.Join(", ", cfg.DepoStokTarihce.ExecutionTimes) + ")" : "Pasif")}\n" +
                $"• Sipariş Durum Kontrol: {(cfg.SparisDurumKontrol.Enabled ? "Aktif (" + cfg.SparisDurumKontrol.IntervalMinutes + " dakikada bir)" : "Pasif")}\n\n" +
                "Arka plan servisi çalışıyorsa yeni ayarlar otomatik olarak devreye girecektir.",
                "Kayıt Başarılı",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Ayarlar kaydedilirken hata oluştu:\n{ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task OnTestConnectionClick()
    {
        _btnTestConnection.Enabled = false;
        _lblConnTestResult.Text = "Bağlanılıyor...";
        _lblConnTestResult.ForeColor = Color.FromArgb(13, 110, 253);

        try
        {
            var cfg = BuildCurrentConfig();
            var sql = new SqlService(cfg);
            var res = await sql.TestConnectionAsync();

            _lblConnTestResult.Text = res.Message;
            _lblConnTestResult.ForeColor = res.Success ? Color.FromArgb(22, 163, 74) : Color.FromArgb(220, 38, 38);
        }
        finally
        {
            _btnTestConnection.Enabled = true;
        }
    }

    private void UpdateServiceStatus()
    {
        var status = ServiceManager.GetServiceStatus(_config.Service.ServiceName);
        if (!status.Installed)
        {
            _lblServiceStatus.Text = "⚪ Kurulu Değil";
            _lblServiceStatus.ForeColor = Color.FromArgb(100, 116, 139);
            _btnInstallService.Enabled = true;
            _btnUninstallService.Enabled = false;
            _btnStartService.Enabled = false;
            _btnStopService.Enabled = false;
        }
        else if (status.Running)
        {
            _lblServiceStatus.Text = "🟢 Çalışıyor (RUNNING)";
            _lblServiceStatus.ForeColor = Color.FromArgb(22, 163, 74);
            _btnInstallService.Enabled = false;
            _btnUninstallService.Enabled = true;
            _btnStartService.Enabled = false;
            _btnStopService.Enabled = true;
        }
        else
        {
            _lblServiceStatus.Text = "🔴 Durduruldu (STOPPED)";
            _lblServiceStatus.ForeColor = Color.FromArgb(220, 38, 38);
            _btnInstallService.Enabled = false;
            _btnUninstallService.Enabled = true;
            _btnStartService.Enabled = true;
            _btnStopService.Enabled = false;
        }
    }

    private void SortDepoTimesList()
    {
        var list = _lstDepoTimes.Items.Cast<string>().OrderBy(x => x).ToList();
        _lstDepoTimes.Items.Clear();
        foreach (var item in list) _lstDepoTimes.Items.Add(item);
    }

    private void AppendLogMessage(string message)
    {
        if (InvokeRequired)
        {
            try { BeginInvoke(() => AppendLogMessage(message)); } catch { }
            return;
        }

        try
        {
            if (_rtbLogs.TextLength > 30000)
            {
                _rtbLogs.Select(0, 10000);
                _rtbLogs.SelectedText = "";
            }

            _rtbLogs.AppendText(message + Environment.NewLine);
            _rtbLogs.ScrollToCaret();
        }
        catch { }
    }
    #endregion
}
