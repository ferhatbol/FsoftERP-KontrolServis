using FsoftERP.KontrolServis.Configuration;
using FsoftERP.KontrolServis.Services;

namespace FsoftERP.KontrolServis.UI;

public class TaskEditDialog : Form
{
    private readonly SpTaskConfig _task;
    private readonly SqlService? _sqlService;

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

    // Parameters Section
    private DataGridView _dgvParams = null!;
    private Button _btnAddNullParam = null!;
    private Button _btnAddValueParam = null!;
    private Button _btnRemoveParam = null!;
    private Button _btnFetchParams = null!;
    private Label _lblSqlPreview = null!;

    // Timeout
    private NumericUpDown _numTimeout = null!;

    public SpTaskConfig ResultTask => _task;

    public TaskEditDialog(SpTaskConfig? existingTask = null, SqlService? sqlService = null)
    {
        _task = existingTask?.Clone() ?? new SpTaskConfig();
        _sqlService = sqlService;

        InitializeComponent();
        LoadTaskToUi();
        UpdateSqlPreview();
    }

    private void InitializeComponent()
    {
        Text = string.IsNullOrWhiteSpace(_task.ProcedureName) ? "Yeni Stored Procedure Görevi Tanımla" : $"Görevi Düzenle: {_task.ProcedureName}";
        Size = new Size(740, 750);
        MinimumSize = new Size(680, 640);
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
            Text = "Stored Procedure Görev Yapılandırması & Parametreler",
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
        int widthCtrl = 500;

        // 1. Task Name
        var lblName = new Label { Text = "Görev Adı (Açıklama):", Location = new Point(leftLabel, top + 3), AutoSize = true, Font = new Font("Segoe UI", 9.25f, FontStyle.Bold) };
        _txtName = new TextBox { Location = new Point(leftCtrl, top), Size = new Size(widthCtrl, 26) };
        pnlContent.Controls.AddRange(new Control[] { lblName, _txtName });

        // 2. Procedure Name
        top += 38;
        var lblProc = new Label { Text = "Stored Procedure Adı:", Location = new Point(leftLabel, top + 3), AutoSize = true, Font = new Font("Segoe UI", 9.25f, FontStyle.Bold) };
        _txtProcedure = new TextBox { Location = new Point(leftCtrl, top), Size = new Size(325, 26), Font = new Font("Consolas", 10f, FontStyle.Bold) };
        _txtProcedure.TextChanged += (s, e) => UpdateSqlPreview();

        _btnFetchParams = new Button
        {
            Text = "🔍 Parametreleri SQL'den Çek",
            Location = new Point(leftCtrl + 335, top - 1),
            Size = new Size(165, 28),
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            BackColor = Color.FromArgb(238, 242, 255),
            ForeColor = Color.FromArgb(79, 70, 229),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        _btnFetchParams.FlatAppearance.BorderColor = Color.FromArgb(199, 210, 254);
        _btnFetchParams.Click += async (s, e) => await OnFetchParamsClickAsync();

        pnlContent.Controls.AddRange(new Control[] { lblProc, _txtProcedure, _btnFetchParams });

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
            Size = new Size(widthCtrl + leftCtrl - leftLabel, 240)
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
            Location = new Point(15, 80),
            Size = new Size(650, 150),
            BackColor = Color.FromArgb(248, 250, 252)
        };

        _lstTimes = new ListBox
        {
            Location = new Point(10, 8),
            Size = new Size(130, 130),
            Font = new Font("Consolas", 10.5f, FontStyle.Bold)
        };

        _dtpTime = new DateTimePicker
        {
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "HH:mm",
            ShowUpDown = true,
            Location = new Point(155, 10),
            Size = new Size(90, 26)
        };

        _btnAddTime = new Button
        {
            Text = "➕ Saat Ekle",
            Location = new Point(255, 9),
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
            Location = new Point(155, 45),
            Size = new Size(130, 26),
            Cursor = Cursors.Hand
        };
        _btnRemoveTime.Click += (s, e) =>
        {
            if (_lstTimes.SelectedIndex >= 0) _lstTimes.Items.RemoveAt(_lstTimes.SelectedIndex);
        };

        var lblPresetTitle = new Label { Text = "Hızlı Şablonlar:", Location = new Point(390, 10), AutoSize = true, Font = new Font("Segoe UI", 8.75f, FontStyle.Bold) };
        _btnPreset1Time = new Button { Text = "Günde 1 Kez (12:00)", Location = new Point(390, 32), Size = new Size(230, 26), Cursor = Cursors.Hand };
        _btnPreset1Time.Click += (s, e) => { _lstTimes.Items.Clear(); _lstTimes.Items.Add("12:00"); };

        _btnPreset2Times = new Button { Text = "Günde 2 Kez (12:00, 19:00)", Location = new Point(390, 63), Size = new Size(230, 26), Cursor = Cursors.Hand };
        _btnPreset2Times.Click += (s, e) => { _lstTimes.Items.Clear(); _lstTimes.Items.Add("12:00"); _lstTimes.Items.Add("19:00"); };

        _btnPreset3Times = new Button { Text = "Günde 3 Kez (06:00, 12:00, 21:00)", Location = new Point(390, 94), Size = new Size(230, 26), Cursor = Cursors.Hand };
        _btnPreset3Times.Click += (s, e) => { _lstTimes.Items.Clear(); _lstTimes.Items.Add("06:00"); _lstTimes.Items.Add("12:00"); _lstTimes.Items.Add("21:00"); };

        _pnlSpecificTimes.Controls.AddRange(new Control[]
        {
            _lstTimes, _dtpTime, _btnAddTime, _btnRemoveTime,
            lblPresetTitle, _btnPreset1Time, _btnPreset2Times, _btnPreset3Times
        });

        // Panel: Interval
        _pnlInterval = new Panel
        {
            Location = new Point(15, 80),
            Size = new Size(650, 150),
            BackColor = Color.FromArgb(248, 250, 252)
        };
        var lblIntervalLabel = new Label { Text = "Çalışma Aralığı (Dakika):", Location = new Point(20, 25), AutoSize = true, Font = new Font("Segoe UI", 9.5f) };
        _numIntervalMinutes = new NumericUpDown { Location = new Point(190, 23), Size = new Size(120, 26), Minimum = 1, Maximum = 1440, Value = 3, Font = new Font("Segoe UI", 10f, FontStyle.Bold) };
        var lblIntervalDesc = new Label
        {
            Text = "(Örn: 3 girilirse procedure her 3 dakikada bir otomatik çalıştırılır.\n 60 girilirse saatte bir çalıştırılır.)",
            Location = new Point(190, 60),
            AutoSize = true,
            ForeColor = Color.FromArgb(100, 116, 139)
        };
        _pnlInterval.Controls.AddRange(new Control[] { lblIntervalLabel, _numIntervalMinutes, lblIntervalDesc });

        grpSchedule.Controls.AddRange(new Control[] { _rbSpecificTimes, _rbInterval, _pnlSpecificTimes, _pnlInterval });
        pnlContent.Controls.Add(grpSchedule);

        // 5. Enhanced Parameters Section (NULL & Fixed Value support)
        top += 255;
        var grpParams = new GroupBox
        {
            Text = "Procedure Parametreleri (NULL veya Sabit Değer Atama)",
            Font = new Font("Segoe UI", 9.75f, FontStyle.Bold),
            Location = new Point(leftLabel, top),
            Size = new Size(widthCtrl + leftCtrl - leftLabel, 210)
        };

        var pnlParamToolbar = new FlowLayoutPanel
        {
            Location = new Point(15, 22),
            Size = new Size(660, 34),
            BackColor = Color.Transparent
        };

        _btnAddNullParam = new Button
        {
            Text = "➕ NULL Parametre Ekle",
            Font = new Font("Segoe UI", 8.75f, FontStyle.Bold),
            BackColor = Color.FromArgb(13, 110, 253),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(180, 28),
            Cursor = Cursors.Hand
        };
        _btnAddNullParam.FlatAppearance.BorderSize = 0;
        _btnAddNullParam.Click += (s, e) =>
        {
            int rowIdx = _dgvParams.Rows.Add("@Param" + (_dgvParams.Rows.Count + 1), "NULL", "Metin (String)", "(NULL)");
            _dgvParams.Rows[rowIdx].Cells[3].ReadOnly = true;
            UpdateSqlPreview();
        };

        _btnAddValueParam = new Button
        {
            Text = "➕ Sabit Değerli Parametre Ekle",
            Font = new Font("Segoe UI", 8.75f, FontStyle.Bold),
            BackColor = Color.FromArgb(22, 163, 74),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(220, 28),
            Cursor = Cursors.Hand,
            Margin = new Padding(10, 0, 0, 0)
        };
        _btnAddValueParam.FlatAppearance.BorderSize = 0;
        _btnAddValueParam.Click += (s, e) =>
        {
            int rowIdx = _dgvParams.Rows.Add("@Param" + (_dgvParams.Rows.Count + 1), "Sabit Değer", "Metin (String)", "Deger1");
            _dgvParams.Rows[rowIdx].Cells[3].ReadOnly = false;
            UpdateSqlPreview();
        };

        _btnRemoveParam = new Button
        {
            Text = "➖ Seçili Parametreyi Sil",
            Font = new Font("Segoe UI", 8.75f),
            Size = new Size(170, 28),
            Cursor = Cursors.Hand,
            Margin = new Padding(10, 0, 0, 0)
        };
        _btnRemoveParam.Click += (s, e) =>
        {
            if (_dgvParams.SelectedRows.Count > 0)
            {
                _dgvParams.Rows.RemoveAt(_dgvParams.SelectedRows[0].Index);
                UpdateSqlPreview();
            }
        };

        pnlParamToolbar.Controls.AddRange(new Control[] { _btnAddNullParam, _btnAddValueParam, _btnRemoveParam });

        _dgvParams = new DataGridView
        {
            Location = new Point(15, 60),
            Size = new Size(660, 115),
            BackgroundColor = Color.White,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            RowTemplate = { Height = 28 }
        };

        var colParamName = new DataGridViewTextBoxColumn
        {
            HeaderText = "Parametre Adı",
            Name = "colParamName",
            FillWeight = 30f
        };

        var colState = new DataGridViewComboBoxColumn
        {
            HeaderText = "Değer Durumu",
            Name = "colState",
            FillWeight = 25f
        };
        colState.Items.AddRange("NULL", "Sabit Değer");

        var colType = new DataGridViewComboBoxColumn
        {
            HeaderText = "Veri Tipi",
            Name = "colType",
            FillWeight = 25f
        };
        colType.Items.AddRange("Metin (String)", "Tamsayı (Int)", "Ondalıklı (Decimal)", "Mantıksal (Bit)", "Tarih (DateTime)");

        var colVal = new DataGridViewTextBoxColumn
        {
            HeaderText = "Sabit Değer",
            Name = "colVal",
            FillWeight = 30f
        };

        _dgvParams.Columns.AddRange(colParamName, colState, colType, colVal);

        _dgvParams.CellValueChanged += (s, e) =>
        {
            if (e.RowIndex >= 0)
            {
                if (e.ColumnIndex == 1) // Değer Durumu değişti
                {
                    string state = _dgvParams.Rows[e.RowIndex].Cells[1].Value?.ToString() ?? "NULL";
                    if (state == "NULL")
                    {
                        _dgvParams.Rows[e.RowIndex].Cells[3].Value = "(NULL)";
                        _dgvParams.Rows[e.RowIndex].Cells[3].ReadOnly = true;
                    }
                    else
                    {
                        if (_dgvParams.Rows[e.RowIndex].Cells[3].Value?.ToString() == "(NULL)")
                            _dgvParams.Rows[e.RowIndex].Cells[3].Value = "";
                        _dgvParams.Rows[e.RowIndex].Cells[3].ReadOnly = false;
                    }
                }
                UpdateSqlPreview();
            }
        };

        _dgvParams.CurrentCellDirtyStateChanged += (s, e) =>
        {
            if (_dgvParams.IsCurrentCellDirty)
                _dgvParams.CommitEdit(DataGridViewDataErrorContexts.Commit);
        };

        grpParams.Controls.AddRange(new Control[] { pnlParamToolbar, _dgvParams });
        pnlContent.Controls.Add(grpParams);

        // 6. Live SQL Preview Label
        top += 220;
        var lblPreviewTitle = new Label
        {
            Text = "🔎 SQL Çağrı Önizlemesi:",
            Location = new Point(leftLabel, top),
            AutoSize = true,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = Color.FromArgb(71, 85, 105)
        };
        _lblSqlPreview = new Label
        {
            Text = "EXEC ...",
            Location = new Point(leftCtrl, top),
            Size = new Size(widthCtrl, 24),
            Font = new Font("Consolas", 10f, FontStyle.Bold),
            ForeColor = Color.FromArgb(13, 110, 253),
            AutoEllipsis = true
        };
        pnlContent.Controls.AddRange(new Control[] { lblPreviewTitle, _lblSqlPreview });

        // 7. Timeout
        top += 35;
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
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 0, 10, 0)
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
                string stateStr = p.IsNull ? "NULL" : "Sabit Değer";
                string typeStr = p.DataType switch
                {
                    SpParamType.Int => "Tamsayı (Int)",
                    SpParamType.Decimal => "Ondalıklı (Decimal)",
                    SpParamType.Boolean => "Mantıksal (Bit)",
                    SpParamType.DateTime => "Tarih (DateTime)",
                    _ => "Metin (String)"
                };

                int rowIdx = _dgvParams.Rows.Add(p.Name, stateStr, typeStr, p.IsNull ? "(NULL)" : (p.Value ?? ""));
                _dgvParams.Rows[rowIdx].Cells[3].ReadOnly = p.IsNull;
            }
        }
    }

    private void UpdateSqlPreview()
    {
        if (_lblSqlPreview == null) return;
        string sp = string.IsNullOrWhiteSpace(_txtProcedure?.Text) ? "SP_ADI" : _txtProcedure.Text.Trim();

        var paramParts = new List<string>();
        if (_dgvParams != null)
        {
            foreach (DataGridViewRow row in _dgvParams.Rows)
            {
                string pName = row.Cells[0].Value?.ToString()?.Trim() ?? "";
                if (string.IsNullOrWhiteSpace(pName)) continue;
                if (!pName.StartsWith("@")) pName = "@" + pName;

                string state = row.Cells[1].Value?.ToString() ?? "NULL";
                string val = row.Cells[3].Value?.ToString()?.Trim() ?? "";

                if (state == "NULL" || string.IsNullOrWhiteSpace(val) || val == "(NULL)")
                {
                    paramParts.Add($"{pName} = NULL");
                }
                else
                {
                    paramParts.Add($"{pName} = '{val}'");
                }
            }
        }

        if (paramParts.Count == 0)
        {
            _lblSqlPreview.Text = $"EXEC {sp}";
        }
        else
        {
            _lblSqlPreview.Text = $"EXEC {sp} {string.Join(", ", paramParts)}";
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
            string? pName = row.Cells[0].Value?.ToString()?.Trim();
            if (string.IsNullOrWhiteSpace(pName)) continue;

            string state = row.Cells[1].Value?.ToString() ?? "NULL";
            string typeStr = row.Cells[2].Value?.ToString() ?? "Metin (String)";
            string? pVal = row.Cells[3].Value?.ToString()?.Trim();

            var paramType = typeStr switch
            {
                "Tamsayı (Int)" => SpParamType.Int,
                "Ondalıklı (Decimal)" => SpParamType.Decimal,
                "Mantıksal (Bit)" => SpParamType.Boolean,
                "Tarih (DateTime)" => SpParamType.DateTime,
                _ => SpParamType.String
            };

            bool isNull = state == "NULL" || string.IsNullOrWhiteSpace(pVal) || pVal == "(NULL)";

            _task.Parameters.Add(new SpParameterConfig
            {
                Name = pName,
                IsNull = isNull,
                DataType = paramType,
                Value = isNull ? null : pVal
            });
        }

        DialogResult = DialogResult.OK;
        Close();
    }

    private async Task OnFetchParamsClickAsync()
    {
        string spName = _txtProcedure.Text.Trim();
        if (string.IsNullOrWhiteSpace(spName))
        {
            MessageBox.Show(this, "Lütfen önce Stored Procedure adını yazınız.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _txtProcedure.Focus();
            return;
        }

        if (_sqlService == null)
        {
            MessageBox.Show(this, "Veritabanı servisi hazır değil.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _btnFetchParams.Enabled = false;
        _btnFetchParams.Text = "⏳ Aranıyor...";
        Cursor = Cursors.WaitCursor;

        try
        {
            var detectedParams = await _sqlService.GetSpParametersAsync(spName);

            if (detectedParams.Count == 0)
            {
                MessageBox.Show(this, $"'{spName}' için SQL Server'da parametre bulunamadı veya prosedür parametresiz.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var sbSummary = new System.Text.StringBuilder();
            sbSummary.AppendLine($"'{spName}' için veritabanında {detectedParams.Count} adet parametre tespit edildi:\n");
            foreach (var p in detectedParams)
            {
                sbSummary.AppendLine($"  • {p.Name} ({p.DataType})");
            }
            sbSummary.AppendLine("\nBu parametreler listeye eklensin mi?");

            var ans = MessageBox.Show(this, sbSummary.ToString(), "Parametreler Bulundu", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (ans == DialogResult.Yes)
            {
                _dgvParams.Rows.Clear();
                foreach (var p in detectedParams)
                {
                    string typeDisplay = p.DataType switch
                    {
                        SpParamType.Int => "Tamsayı (Int)",
                        SpParamType.Decimal => "Ondalıklı (Decimal)",
                        SpParamType.Boolean => "Mantıksal (Bit)",
                        SpParamType.DateTime => "Tarih (DateTime)",
                        _ => "Metin (String)"
                    };

                    int rowIdx = _dgvParams.Rows.Add(p.Name, "NULL", typeDisplay, "(NULL)");
                    _dgvParams.Rows[rowIdx].Cells[3].ReadOnly = true;
                    _dgvParams.Rows[rowIdx].Cells[3].Style.BackColor = Color.FromArgb(241, 245, 249);
                }

                UpdateSqlPreview();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Parametreler SQL'den çekilirken hata oluştu:\n{ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _btnFetchParams.Enabled = true;
            _btnFetchParams.Text = "🔍 Parametreleri SQL'den Çek";
            Cursor = Cursors.Default;
        }
    }
}
