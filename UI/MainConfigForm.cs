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

    // Tab 1: Dynamic Tasks Grid
    private DataGridView _dgvTasks = null!;
    private Button _btnAddTask = null!;
    private Button _btnEditTask = null!;
    private Button _btnDeleteTask = null!;
    private Button _btnCloneTask = null!;
    private Button _btnRunTaskNow = null!;
    private Button _btnMoveUp = null!;
    private Button _btnMoveDown = null!;

    // Tab 2: Database
    private TextBox _txtServer = null!;
    private TextBox _txtDatabase = null!;
    private CheckBox _chkWindowsAuth = null!;
    private TextBox _txtUserId = null!;
    private TextBox _txtPassword = null!;
    private Button _btnTogglePassword = null!;
    private NumericUpDown _numConnTimeout = null!;
    private Button _btnTestConnection = null!;
    private Label _lblConnTestResult = null!;

    // Tab 3: Service & Logs
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
        AppLogger.Info("FsoftERP Kontrol Servisi Yönetim Paneli açıldı. v1.0.3.0");
    }

    private void InitializeComponent()
    {
        Text = "FsoftERP Kontrol Servisi - Stored Procedure Otomasyon ve Yönetim Paneli (v1.0.3.0)";
        Size = new Size(1020, 760);
        MinimumSize = new Size(920, 640);
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

        // Root Layout Table
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

        var tabTasks = new TabPage("⚡ Stored Procedure Görevleri") { BackColor = Color.White, Padding = new Padding(15) };
        tabTasks.Controls.Add(CreateTasksTab());
        tabControl.TabPages.Add(tabTasks);

        var tabDb = new TabPage("🗄️ Veritabanı Bağlantısı") { BackColor = Color.White, Padding = new Padding(15) };
        tabDb.Controls.Add(CreateDatabaseTab());
        tabControl.TabPages.Add(tabDb);

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

        string version = GetType().Assembly.GetName().Version?.ToString() ?? "1.0.3.0";
        var lblSub = new Label
        {
            Text = $"Dinamik Stored Procedure Otomasyon Motoru | FsoftERPSKWS v{version}",
            Font = new Font("Segoe UI", 8.75f, FontStyle.Regular),
            ForeColor = Color.FromArgb(148, 163, 184),
            Location = new Point(156, 36),
            AutoSize = true
        };

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
            BackColor = Color.FromArgb(22, 163, 74),
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

    private Control CreateTasksTab()
    {
        var pnl = new Panel { Dock = DockStyle.Fill, Padding = new Padding(5) };

        // Top Toolbar
        var pnlToolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 46,
            BackColor = Color.FromArgb(248, 250, 252),
            Padding = new Padding(4, 5, 4, 5)
        };

        _btnAddTask = new Button
        {
            Text = "➕ Yeni SP Görevi Ekle",
            Font = new Font("Segoe UI", 9.25f, FontStyle.Bold),
            BackColor = Color.FromArgb(22, 163, 74),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(180, 34),
            Cursor = Cursors.Hand
        };
        _btnAddTask.FlatAppearance.BorderSize = 0;
        _btnAddTask.Click += OnAddTaskClick;

        _btnEditTask = new Button
        {
            Text = "✏️ Görevi Düzenle",
            Font = new Font("Segoe UI", 9f),
            Size = new Size(130, 34),
            Cursor = Cursors.Hand
        };
        _btnEditTask.Click += OnEditTaskClick;

        _btnDeleteTask = new Button
        {
            Text = "🗑️ Görevi Sil",
            Font = new Font("Segoe UI", 9f),
            ForeColor = Color.FromArgb(220, 38, 38),
            Size = new Size(110, 34),
            Cursor = Cursors.Hand
        };
        _btnDeleteTask.Click += OnDeleteTaskClick;

        _btnCloneTask = new Button
        {
            Text = "📋 Çoğalt (Klonla)",
            Font = new Font("Segoe UI", 9f),
            Size = new Size(130, 34),
            Cursor = Cursors.Hand
        };
        _btnCloneTask.Click += OnCloneTaskClick;

        _btnRunTaskNow = new Button
        {
            Text = "▶️ Seçili SP'yi Şimdi Çalıştır (Test)",
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            BackColor = Color.FromArgb(13, 110, 253),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(240, 34),
            Cursor = Cursors.Hand
        };
        _btnRunTaskNow.FlatAppearance.BorderSize = 0;
        _btnRunTaskNow.Click += async (s, e) => await OnRunTaskNowClick();

        _btnMoveUp = new Button { Text = "⬆️", Size = new Size(40, 34), Cursor = Cursors.Hand };
        _btnMoveUp.Click += (s, e) => MoveSelectedTask(-1);

        _btnMoveDown = new Button { Text = "⬇️", Size = new Size(40, 34), Cursor = Cursors.Hand };
        _btnMoveDown.Click += (s, e) => MoveSelectedTask(1);

        pnlToolbar.Controls.AddRange(new Control[]
        {
            _btnAddTask, _btnEditTask, _btnDeleteTask, _btnCloneTask,
            _btnRunTaskNow, _btnMoveUp, _btnMoveDown
        });

        // Grid
        _dgvTasks = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.Fixed3D,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            RowTemplate = { Height = 34 }
        };

        var colEnabled = new DataGridViewCheckBoxColumn
        {
            HeaderText = "Aktif",
            Name = "colEnabled",
            Width = 55,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None
        };

        var colName = new DataGridViewTextBoxColumn
        {
            HeaderText = "Görev Adı",
            Name = "colName",
            ReadOnly = true,
            FillWeight = 25f
        };

        var colProc = new DataGridViewTextBoxColumn
        {
            HeaderText = "Stored Procedure",
            Name = "colProc",
            ReadOnly = true,
            FillWeight = 25f
        };

        var colParams = new DataGridViewTextBoxColumn
        {
            HeaderText = "Parametreler",
            Name = "colParams",
            ReadOnly = true,
            FillWeight = 25f
        };

        var colSchedule = new DataGridViewTextBoxColumn
        {
            HeaderText = "Çalışma Planı (Zamanlama)",
            Name = "colSchedule",
            ReadOnly = true,
            FillWeight = 25f
        };

        var colTimeout = new DataGridViewTextBoxColumn
        {
            HeaderText = "Timeout (sn)",
            Name = "colTimeout",
            ReadOnly = true,
            Width = 90,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None
        };

        _dgvTasks.Columns.AddRange(colEnabled, colName, colProc, colParams, colSchedule, colTimeout);
        _dgvTasks.DataError += (s, e) => { e.ThrowException = false; };
        _dgvTasks.CellDoubleClick += (s, e) =>
        {
            if (e.RowIndex >= 0) OnEditTaskClick(s, e);
        };

        _dgvTasks.CellValueChanged += (s, e) =>
        {
            if (e.RowIndex >= 0 && e.ColumnIndex == 0)
            {
                bool isChecked = Convert.ToBoolean(_dgvTasks.Rows[e.RowIndex].Cells[0].Value);
                if (e.RowIndex < _config.Tasks.Count)
                {
                    _config.Tasks[e.RowIndex].Enabled = isChecked;
                }
            }
        };

        _dgvTasks.CurrentCellDirtyStateChanged += (s, e) =>
        {
            if (_dgvTasks.IsCurrentCellDirty)
                _dgvTasks.CommitEdit(DataGridViewDataErrorContexts.Commit);
        };

        pnl.Controls.Add(_dgvTasks);
        pnl.Controls.Add(pnlToolbar);
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

    private Control CreateServiceTab()
    {
        var pnl = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(10) };

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
            SyncDatabaseSettingsFromUi();
            var res = ServiceManager.InstallService(_config);
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
            if (MessageBox.Show(this, "Windows Servisini sistemden kaldırmak istediğinize emin misiniz?", "Onay", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
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

    #region Task Grid Management
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

        // Tasks Grid
        RefreshTasksGrid();
    }

    private void RefreshTasksGrid()
    {
        int selectedIndex = _dgvTasks.SelectedRows.Count > 0 ? _dgvTasks.SelectedRows[0].Index : -1;
        _dgvTasks.Rows.Clear();

        foreach (var task in _config.Tasks)
        {
            int rowIdx = _dgvTasks.Rows.Add(
                task.Enabled,
                task.Name,
                task.ProcedureName,
                task.GetParametersSummary(),
                task.GetScheduleSummary(),
                task.CommandTimeoutSeconds
            );
            _dgvTasks.Rows[rowIdx].Tag = task;
        }

        if (selectedIndex >= 0 && selectedIndex < _dgvTasks.Rows.Count)
        {
            _dgvTasks.Rows[selectedIndex].Selected = true;
        }
    }

    private void OnAddTaskClick(object? sender, EventArgs e)
    {
        using var dlg = new TaskEditDialog(null, _sqlService);
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            _config.Tasks.Add(dlg.ResultTask);
            RefreshTasksGrid();
            _lblStatus.Text = $"Yeni görev eklendi: {dlg.ResultTask.ProcedureName} (Kaydetmeyi unutmayınız)";
        }
    }

    private void OnEditTaskClick(object? sender, EventArgs e)
    {
        if (_dgvTasks.SelectedRows.Count == 0) return;
        int idx = _dgvTasks.SelectedRows[0].Index;
        if (idx < 0 || idx >= _config.Tasks.Count) return;

        var task = _config.Tasks[idx];
        using var dlg = new TaskEditDialog(task, _sqlService);
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            _config.Tasks[idx] = dlg.ResultTask;
            RefreshTasksGrid();
            _lblStatus.Text = $"Görev güncellendi: {dlg.ResultTask.ProcedureName} (Kaydetmeyi unutmayınız)";
        }
    }

    private void OnDeleteTaskClick(object? sender, EventArgs e)
    {
        if (_dgvTasks.SelectedRows.Count == 0) return;
        int idx = _dgvTasks.SelectedRows[0].Index;
        if (idx < 0 || idx >= _config.Tasks.Count) return;

        var task = _config.Tasks[idx];
        var res = MessageBox.Show(this,
            $"'{task.ProcedureName}' görevini silmek istediğinize emin misiniz?",
            "Görev Sil",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (res == DialogResult.Yes)
        {
            _config.Tasks.RemoveAt(idx);
            RefreshTasksGrid();
            _lblStatus.Text = $"Görev silindi: {task.ProcedureName}";
        }
    }

    private void OnCloneTaskClick(object? sender, EventArgs e)
    {
        if (_dgvTasks.SelectedRows.Count == 0) return;
        int idx = _dgvTasks.SelectedRows[0].Index;
        if (idx < 0 || idx >= _config.Tasks.Count) return;

        var task = _config.Tasks[idx];
        var cloned = task.Clone();
        cloned.Name = string.IsNullOrWhiteSpace(task.Name)
            ? $"{task.ProcedureName} (Kopya)"
            : $"{task.Name} (Kopya)";
        _config.Tasks.Insert(idx + 1, cloned);
        RefreshTasksGrid();
        _lblStatus.Text = $"Görev klonlandı: {cloned.Name} (Kaydetmeyi unutmayınız)";
    }

    private async Task OnRunTaskNowClick()
    {
        if (_dgvTasks.SelectedRows.Count == 0)
        {
            MessageBox.Show(this, "Lütfen çalıştırmak istediğiniz görevi tablodan seçiniz!", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        int idx = _dgvTasks.SelectedRows[0].Index;
        if (idx < 0 || idx >= _config.Tasks.Count) return;

        var task = _config.Tasks[idx];
        SyncDatabaseSettingsFromUi();

        _btnRunTaskNow.Enabled = false;
        _btnRunTaskNow.Text = "Çalıştırılıyor, bekleyin...";

        try
        {
            var sql = new SqlService(_config);
            var res = await sql.ExecuteTaskAsync(task);

            MessageBox.Show(this, res.Message, res.Success ? "Test Başarılı" : "Hata", MessageBoxButtons.OK, res.Success ? MessageBoxIcon.Information : MessageBoxIcon.Error);
        }
        finally
        {
            _btnRunTaskNow.Enabled = true;
            _btnRunTaskNow.Text = "▶️ Seçili SP'yi Şimdi Çalıştır (Test)";
        }
    }

    private void MoveSelectedTask(int direction)
    {
        if (_dgvTasks.SelectedRows.Count == 0) return;
        int idx = _dgvTasks.SelectedRows[0].Index;
        int newIdx = idx + direction;

        if (newIdx < 0 || newIdx >= _config.Tasks.Count) return;

        var item = _config.Tasks[idx];
        _config.Tasks.RemoveAt(idx);
        _config.Tasks.Insert(newIdx, item);

        RefreshTasksGrid();
        _dgvTasks.Rows[newIdx].Selected = true;
    }
    #endregion

    #region Save & Test Logic
    private void SyncDatabaseSettingsFromUi()
    {
        _config.Database.Server = _txtServer.Text.Trim();
        _config.Database.Database = _txtDatabase.Text.Trim();
        _config.Database.UseWindowsAuth = _chkWindowsAuth.Checked;
        _config.Database.UserId = _txtUserId.Text.Trim();
        _config.Database.Password = _txtPassword.Text;
        _config.Database.ConnectionTimeoutSeconds = (int)_numConnTimeout.Value;
    }

    private void SaveConfigFromUi()
    {
        try
        {
            SyncDatabaseSettingsFromUi();
            AppConfig.Save(AppConfig.DefaultConfigPath, _config);

            _lblStatus.Text = $"✅ Ayarlar başarıyla kaydedildi ({DateTime.Now:HH:mm:ss})";
            _lblStatus.ForeColor = Color.FromArgb(22, 163, 74);

            MessageBox.Show(this,
                "Tüm ayarlar ve Stored Procedure görevleri başarıyla kaydedildi! ✅\n\n" +
                $"• Sunucu: {_config.Database.Server} / {_config.Database.Database}\n" +
                $"• Tanımlı SP Görev Sayısı: {_config.Tasks.Count}\n" +
                $"• Aktif Görevler: {_config.Tasks.Count(t => t.Enabled)}\n\n" +
                "Arka plan servisi çalışıyorsa yeni görevler otomatik olarak devreye girecektir.",
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
            SyncDatabaseSettingsFromUi();
            var sql = new SqlService(_config);
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
