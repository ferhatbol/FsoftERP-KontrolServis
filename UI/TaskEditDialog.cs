using FsoftERP.KontrolServis.Configuration;

namespace FsoftERP.KontrolServis.UI;

public class TaskEditDialog : Form
{
    private readonly SpTaskConfig _task;

    private TextBox _txtName = null!;
    private TextBox _txtProcedure = null!;
    private CheckBox _chkEnabled = null!;

    private RadioButton _rbSpecificTimes = null!;
    private RadioButton _rbInterval = null!;

    // Specific Times Panel
    private Panel _pnlSpecificTimes = null!;
    private ListBox _lstTimes = null!;
    private DateTimePicker _dtpTime = null!;
    private Button _btnAddTime = null!;
    private Button _btnRemoveTime = null!;
    private Button _btnPreset1Time = null!;
    private Button _btnPreset2Times = null!;
    private Button _btnPreset3Times = null!;

    // Interval Panel
    private Panel _pnlInterval = null!;
    private NumericUpDown _numIntervalMinutes = null!;

    // Parameters
    private DataGridView _dgvParams = null!;

    // Timeout
    private NumericUpDown _numTimeout = null!;

    public SpTaskConfig ResultTask => _task;

    public TaskEditDialog(SpTaskConfig? existingTask = null)
    {
        _task = existingTask?.Clone() ?? new SpTaskConfig();

        InitializeComponent();
        LoadTaskToUi();
    }

    private void InitializeComponent()
    {
        Text = string.IsNullOrWhiteSpace(_task.ProcedureName) ? "Yeni Stored Procedure Görevi Tanımla" : $"Görevi Düzenle: {_task.ProcedureName}";
        Size = new Size(680, 680);
        MinimumSize = new Size(600, 580);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Font = new Font("Segoe UI", 9.25f);
        BackColor = Color.White;

        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 55,
            BackColor = Color.FromArgb(15, 23, 42),
            Padding = new Padding(15, 12, 15, 10)
        };
        var lblHeader = new Label
        {
            Text = "Stored Procedure Görev Yapılandırması",
            Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true
        };
        pnlHeader.Controls.Add(lblHeader);

        var pnlContent = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Padding = new Padding(15)
        };

        int top = 10;
        int leftLabel = 10;
        int leftCtrl = 190;
        int widthCtrl = 430;

        // 1. Task Name
        var lblName = new Label { Text = "Görev Adı (Açıklama):", Location = new Point(leftLabel, top + 3), AutoSize = true, Font = new Font("Segoe UI", 9.25f, FontStyle.Bold) };
        _txtName = new TextBox { Location = new Point(leftCtrl, top), Size = new Size(widthCtrl, 26) };
        pnlContent.Controls.AddRange(new Control[] { lblName, _txtName });

        // 2. Procedure Name
        top += 38;
        var lblProc = new Label { Text = "Stored Procedure Adı:", Location = new Point(leftLabel, top + 3), AutoSize = true, Font = new Font("Segoe UI", 9.25f, FontStyle.Bold) };
        _txtProcedure = new TextBox { Location = new Point(leftCtrl, top), Size = new Size(widthCtrl, 26), Font = new Font("Consolas", 10f, FontStyle.Bold) };
        pnlContent.Controls.AddRange(new Control[] { lblProc, _txtProcedure });

        // 3. Enabled Checkbox
        top += 38;
        _chkEnabled = new CheckBox
        {
            Text = "Bu Görevi Etkinleştir (Aktif)",
            Location = new Point(leftCtrl, top),
            Size = new Size(widthCtrl, 24),
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(22, 163, 74)
        };
        pnlContent.Controls.Add(_chkEnabled);

        // 4. Schedule Type GroupBox
        top += 38;
        var grpSchedule = new GroupBox
        {
            Text = "Çalışma Zamanı / Planlama Modu",
            Font = new Font("Segoe UI", 9.75f, FontStyle.Bold),
            Location = new Point(leftLabel, top),
            Size = new Size(widthCtrl + leftCtrl - leftLabel, 260)
        };

        _rbSpecificTimes = new RadioButton
        {
            Text = "🕒 Günde Belirtilen Saatlerde Çalıştır (Örn: 12:00, 19:00)",
            Location = new Point(15, 26),
            Size = new Size(500, 24),
            Font = new Font("Segoe UI", 9.25f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        _rbSpecificTimes.CheckedChanged += (s, e) => ToggleSchedulePanels();

        _rbInterval = new RadioButton
        {
            Text = "⏱️ Periyodik Dakika Aralığıyla Çalıştır (Örn: Her 3 dakikada bir)",
            Location = new Point(15, 52),
            Size = new Size(500, 24),
            Font = new Font("Segoe UI", 9.25f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        _rbInterval.CheckedChanged += (s, e) => ToggleSchedulePanels();

        // Panel: Specific Times
        _pnlSpecificTimes = new Panel
        {
            Location = new Point(15, 82),
            Size = new Size(580, 165),
            BackColor = Color.FromArgb(248, 250, 252)
        };

        _lstTimes = new ListBox
        {
            Location = new Point(10, 10),
            Size = new Size(130, 140),
            Font = new Font("Consolas", 10.5f, FontStyle.Bold)
        };

        _dtpTime = new DateTimePicker
        {
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "HH:mm",
            ShowUpDown = true,
            Location = new Point(155, 12),
            Size = new Size(90, 26)
        };

        _btnAddTime = new Button
        {
            Text = "➕ Saat Ekle",
            Location = new Point(255, 11),
            Size = new Size(110, 28),
            BackColor = Color.FromArgb(22, 163, 74),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        _btnAddTime.FlatAppearance.BorderSize = 0;
        _btnAddTime.Click += (s, e) =>
        {
            string t = _dtpTime.Value.ToString("HH:mm");
            if (!_lstTimes.Items.Contains(t))
            {
                _lstTimes.Items.Add(t);
                SortTimes();
            }
        };

        _btnRemoveTime = new Button
        {
            Text = "➖ Seçili Saati Sil",
            Location = new Point(155, 48),
            Size = new Size(130, 26),
            Cursor = Cursors.Hand
        };
        _btnRemoveTime.Click += (s, e) =>
        {
            if (_lstTimes.SelectedIndex >= 0) _lstTimes.Items.RemoveAt(_lstTimes.SelectedIndex);
        };

        var lblPresetTitle = new Label { Text = "Hızlı Şablonlar:", Location = new Point(380, 15), AutoSize = true, Font = new Font("Segoe UI", 8.75f, FontStyle.Bold) };
        _btnPreset1Time = new Button { Text = "Günde 1 Kez (12:00)", Location = new Point(380, 38), Size = new Size(185, 26), Cursor = Cursors.Hand };
        _btnPreset1Time.Click += (s, e) => { _lstTimes.Items.Clear(); _lstTimes.Items.Add("12:00"); };

        _btnPreset2Times = new Button { Text = "Günde 2 Kez (12:00, 19:00)", Location = new Point(380, 70), Size = new Size(185, 26), Cursor = Cursors.Hand };
        _btnPreset2Times.Click += (s, e) => { _lstTimes.Items.Clear(); _lstTimes.Items.Add("12:00"); _lstTimes.Items.Add("19:00"); };

        _btnPreset3Times = new Button { Text = "Günde 3 Kez (06:00, 12:00, 21:00)", Location = new Point(380, 102), Size = new Size(185, 26), Cursor = Cursors.Hand };
        _btnPreset3Times.Click += (s, e) => { _lstTimes.Items.Clear(); _lstTimes.Items.Add("06:00"); _lstTimes.Items.Add("12:00"); _lstTimes.Items.Add("21:00"); };

        _pnlSpecificTimes.Controls.AddRange(new Control[]
        {
            _lstTimes, _dtpTime, _btnAddTime, _btnRemoveTime,
            lblPresetTitle, _btnPreset1Time, _btnPreset2Times, _btnPreset3Times
        });

        // Panel: Interval
        _pnlInterval = new Panel
        {
            Location = new Point(15, 82),
            Size = new Size(580, 165),
            BackColor = Color.FromArgb(248, 250, 252)
        };
        var lblIntervalLabel = new Label { Text = "Çalışma Aralığı (Dakika):", Location = new Point(20, 30), AutoSize = true, Font = new Font("Segoe UI", 9.5f) };
        _numIntervalMinutes = new NumericUpDown { Location = new Point(190, 28), Size = new Size(120, 26), Minimum = 1, Maximum = 1440, Value = 3, Font = new Font("Segoe UI", 10f, FontStyle.Bold) };
        var lblIntervalDesc = new Label
        {
            Text = "(Örn: 3 girilirse procedure her 3 dakikada bir otomatik çalıştırılır.\n 60 girilirse saatte bir çalıştırılır.)",
            Location = new Point(190, 65),
            AutoSize = true,
            ForeColor = Color.FromArgb(100, 116, 139)
        };
        _pnlInterval.Controls.AddRange(new Control[] { lblIntervalLabel, _numIntervalMinutes, lblIntervalDesc });

        grpSchedule.Controls.AddRange(new Control[] { _rbSpecificTimes, _rbInterval, _pnlSpecificTimes, _pnlInterval });
        pnlContent.Controls.Add(grpSchedule);

        // 5. Parameters Section
        top += 275;
        var grpParams = new GroupBox
        {
            Text = "Procedure Parametreleri (İsteğe Bağlı)",
            Font = new Font("Segoe UI", 9.75f, FontStyle.Bold),
            Location = new Point(leftLabel, top),
            Size = new Size(widthCtrl + leftCtrl - leftLabel, 150)
        };

        _dgvParams = new DataGridView
        {
            Location = new Point(15, 25),
            Size = new Size(580, 110),
            BackgroundColor = Color.White,
            RowHeadersVisible = false,
            AllowUserToResizeRows = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        };
        _dgvParams.Columns.Add("colName", "Parametre Adı (Örn: @SiparisNo)");
        _dgvParams.Columns.Add("colVal", "Değer (Boş veya NULL yazılırsa NULL gönderilir)");

        grpParams.Controls.Add(_dgvParams);
        pnlContent.Controls.Add(grpParams);

        // 6. Timeout
        top += 160;
        var lblTimeout = new Label { Text = "Komut Zaman Aşımı (Timeout sn):", Location = new Point(leftLabel, top + 3), AutoSize = true, Font = new Font("Segoe UI", 9.25f, FontStyle.Bold) };
        _numTimeout = new NumericUpDown { Location = new Point(leftCtrl, top), Size = new Size(120, 26), Minimum = 10, Maximum = 7200, Value = 1000 };
        pnlContent.Controls.AddRange(new Control[] { lblTimeout, _numTimeout });

        // Bottom Action Bar
        var pnlBottom = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 55,
            BackColor = Color.FromArgb(241, 245, 249),
            Padding = new Padding(12)
        };

        var btnSave = new Button
        {
            Text = "💾 Görevi Uygula / Kaydet",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            BackColor = Color.FromArgb(22, 163, 74),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(200, 34),
            Dock = DockStyle.Right,
            Cursor = Cursors.Hand
        };
        btnSave.FlatAppearance.BorderSize = 0;
        btnSave.Click += OnSaveClick;

        var btnCancel = new Button
        {
            Text = "İptal",
            Size = new Size(100, 34),
            Dock = DockStyle.Right,
            DialogResult = DialogResult.Cancel,
            Cursor = Cursors.Hand
        };

        pnlBottom.Controls.Add(btnSave);
        pnlBottom.Controls.Add(btnCancel);

        Controls.Add(pnlContent);
        Controls.Add(pnlBottom);
        Controls.Add(pnlHeader);
    }

    private void ToggleSchedulePanels()
    {
        _pnlSpecificTimes.Visible = _rbSpecificTimes.Checked;
        _pnlInterval.Visible = _rbInterval.Checked;
    }

    private void SortTimes()
    {
        var list = _lstTimes.Items.Cast<string>().OrderBy(x => x).ToList();
        _lstTimes.Items.Clear();
        foreach (var item in list) _lstTimes.Items.Add(item);
    }

    private void LoadTaskToUi()
    {
        _txtName.Text = _task.Name;
        _txtProcedure.Text = _task.ProcedureName;
        _chkEnabled.Checked = _task.Enabled;

        if (_task.ScheduleType == SpScheduleType.SpecificTimes)
            _rbSpecificTimes.Checked = true;
        else
            _rbInterval.Checked = true;

        ToggleSchedulePanels();

        _lstTimes.Items.Clear();
        foreach (var t in _task.ExecutionTimes) _lstTimes.Items.Add(t);
        SortTimes();

        _numIntervalMinutes.Value = Math.Clamp(_task.IntervalMinutes, 1, 1440);
        _numTimeout.Value = Math.Clamp(_task.CommandTimeoutSeconds, 10, 7200);

        _dgvParams.Rows.Clear();
        if (_task.Parameters != null)
        {
            foreach (var p in _task.Parameters)
            {
                _dgvParams.Rows.Add(p.Name, p.Value ?? "NULL");
            }
        }
    }

    private void OnSaveClick(object? sender, EventArgs e)
    {
        string proc = _txtProcedure.Text.Trim();
        if (string.IsNullOrWhiteSpace(proc))
        {
            MessageBox.Show(this, "Lütfen çalıştırılacak Stored Procedure adını giriniz!", "Eksik Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _txtProcedure.Focus();
            return;
        }

        _task.ProcedureName = proc;
        _task.Name = string.IsNullOrWhiteSpace(_txtName.Text) ? proc : _txtName.Text.Trim();
        _task.Enabled = _chkEnabled.Checked;
        _task.ScheduleType = _rbSpecificTimes.Checked ? SpScheduleType.SpecificTimes : SpScheduleType.IntervalMinutes;
        _task.IntervalMinutes = (int)_numIntervalMinutes.Value;
        _task.CommandTimeoutSeconds = (int)_numTimeout.Value;

        _task.ExecutionTimes = _lstTimes.Items.Cast<string>().ToList();
        if (_task.ScheduleType == SpScheduleType.SpecificTimes && _task.ExecutionTimes.Count == 0)
        {
            MessageBox.Show(this, "Belirtilen saatlerde çalışması için en az 1 saat eklemelisiniz!", "Eksik Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _task.Parameters.Clear();
        foreach (DataGridViewRow row in _dgvParams.Rows)
        {
            if (row.IsNewRow) continue;
            string? pName = row.Cells[0].Value?.ToString();
            string? pVal = row.Cells[1].Value?.ToString();

            if (!string.IsNullOrWhiteSpace(pName))
            {
                _task.Parameters.Add(new SpParameterConfig
                {
                    Name = pName.Trim(),
                    Value = string.IsNullOrWhiteSpace(pVal) || string.Equals(pVal, "NULL", StringComparison.OrdinalIgnoreCase) ? null : pVal.Trim()
                });
            }
        }

        DialogResult = DialogResult.OK;
        Close();
    }
}
