using InventorDxfExporter.Core;
using InventorDxfExporter.Settings;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;

namespace InventorDxfExporter.UI.Forms;

public sealed class ExportSettingsForm : Form
{
    private static readonly (string Name, string Description, bool Default)[] Layers =
    {
        ("IV_OUTER_PROFILE", "Đường bao ngoài", true), ("IV_INTERIOR_PROFILES", "Đường bao trong / lỗ", true),
        ("IV_BEND_UP", "Đường gấp lên (bend up)", false), ("IV_BEND_DOWN", "Đường gấp xuống (bend down)", false),
        ("IV_TOOL_CENTER", "Tâm dụng cụ (tool center)", false), ("IV_ARC_CENTERS", "Tâm cung tròn", false),
        ("IV_TANGENT", "Đường tiếp tuyến", false), ("IV_FEATURE_PROFILES", "Đường bao chi tiết đặc trưng", false),
        ("IV_UNCONSUMED_SKETCHES", "Sketch chưa dùng đến", false), ("IV_ROLL_TANGENT", "Tiếp tuyến đường cán", false),
        ("IV_ROLL", "Đường cán (roll line)", false)
    };
    private static readonly LayerColorOption[] LayerColors =
    {
        new("Đen", Color.FromArgb(0, 0, 0)), new("Xám đậm", Color.FromArgb(64, 64, 64)),
        new("Đỏ sẫm", Color.FromArgb(128, 0, 0)), new("Đỏ", Color.FromArgb(255, 0, 0)),
        new("Cam", Color.FromArgb(255, 128, 0)), new("Vàng", Color.FromArgb(255, 255, 0)),
        new("Vàng chanh", Color.FromArgb(192, 255, 0)), new("Xanh lá", Color.FromArgb(0, 192, 0)),
        new("Xanh lá đậm", Color.FromArgb(0, 96, 0)), new("Xanh ngọc", Color.FromArgb(0, 128, 128)),
        new("Cyan", Color.FromArgb(0, 255, 255)), new("Xanh da trời", Color.FromArgb(0, 160, 255)),
        new("Xanh dương", Color.FromArgb(0, 0, 255)), new("Xanh navy", Color.FromArgb(0, 0, 128)),
        new("Tím", Color.FromArgb(128, 0, 128)), new("Hồng tím", Color.FromArgb(255, 0, 255)),
        new("Hồng", Color.FromArgb(255, 128, 192)), new("Trắng", Color.FromArgb(255, 255, 255)),
        new("Xám sáng", Color.FromArgb(192, 192, 192)), new("Xám", Color.FromArgb(128, 128, 128)),
        new("Nâu", Color.FromArgb(128, 64, 0)), new("Đỏ gạch", Color.FromArgb(192, 64, 64)),
        new("Cam nhạt", Color.FromArgb(255, 192, 128)), new("Vàng nhạt", Color.FromArgb(255, 255, 160)),
        new("Xanh non", Color.FromArgb(128, 255, 128)), new("Xanh bạc hà", Color.FromArgb(128, 255, 192)),
        new("Xanh biển nhạt", Color.FromArgb(128, 224, 255)), new("Xanh lam nhạt", Color.FromArgb(128, 160, 255)),
        new("Tím nhạt", Color.FromArgb(192, 128, 255)), new("Hồng nhạt", Color.FromArgb(255, 192, 224)),
        new("Nâu nhạt", Color.FromArgb(192, 160, 128)), new("Xám xanh", Color.FromArgb(128, 160, 160)),
        new("Xám tím", Color.FromArgb(160, 128, 160)), new("Xám olive", Color.FromArgb(160, 160, 96)),
        new("Xanh rêu", Color.FromArgb(96, 128, 64)), new("Xanh cổ vịt", Color.FromArgb(0, 96, 96)),
        new("Đỏ rượu", Color.FromArgb(128, 0, 64)), new("Tím than", Color.FromArgb(64, 0, 96)),
        new("Xanh đen", Color.FromArgb(0, 64, 128)), new("Xám bạc", Color.FromArgb(224, 224, 224))
    };
    private static readonly LayerLineTypeOption[] LayerLineTypes =
    {
        new("Mặc định", 37648),
        new("Liền nét", 37633),
        new("Nét đứt", 37634),
        new("Nét chấm", 37636),
        new("Gạch chấm", 37638),
        new("Nét ẩn", 37641),
        new("Đường tâm", 37644)
    };
    private static readonly LayerLineWeightOption[] LayerLineWeights =
    {
        new("0,18 mm", 0.18), new("0,25 mm", 0.25),
        new("0,35 mm", 0.35), new("0,50 mm", 0.50), new("1,00 mm", 1.00)
    };
    private readonly ExportSettings _settings;
    private readonly SettingsProfileStore _profiles = new();
    private readonly DataGridView _layers = new() { Dock = DockStyle.Fill, AllowUserToAddRows = false, AllowUserToDeleteRows = false, RowHeadersVisible = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
    private readonly ComboBox _bend = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 200 };
    private readonly CheckBox _text = new() { Text = "Chèn TEXT thông tin vào DXF", AutoSize = true };
    private readonly ComboBox _alignment = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 130 };
    private readonly TextBox _height = Number(10, 1, 92);
    private readonly TextBox _gap = Number(15, 1, 92);
    private readonly TextBox _yOffset = Number(-250, 1, 92);
    private readonly TextBox _upLen = Number(10, 2, 84);
    private readonly TextBox _upInset = Number(3, 2, 84);
    private readonly TextBox _downLen = Number(3, 2, 84);
    private readonly TextBox _downInset = Number(3, 2, 84);
    private readonly ComboBox _profileList = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 190 };

    public ExportSettingsForm(ExportSettings settings, bool previewMode = false)
    {
        _settings = settings;
        Text = previewMode ? "Xem trước cài đặt xuất DXF" : "Cài đặt xuất DXF";
        StartPosition = previewMode ? FormStartPosition.CenterScreen : FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MinimizeBox = false; MaximizeBox = false; ClientSize = new System.Drawing.Size(1000, 590);
        _layers.Columns.Add(new DataGridViewCheckBoxColumn { Name = "Enabled", HeaderText = "Xuất", FillWeight = 35 });
        _layers.Columns.Add("Name", "Tên layer");
        _layers.Columns.Add("Description", "Mô tả");
        _layers.Columns.Add(new DataGridViewButtonColumn
        {
            Name = "Color",
            HeaderText = "Màu",
            Text = "Chọn màu",
            UseColumnTextForButtonValue = false,
            FillWeight = 100,
            SortMode = DataGridViewColumnSortMode.NotSortable
        });
        _layers.Columns.Add(new DataGridViewComboBoxColumn
        {
            Name = "LineType",
            HeaderText = "Kiểu nét",
            DataSource = LayerLineTypes,
            DisplayMember = nameof(LayerLineTypeOption.Name),
            ValueMember = nameof(LayerLineTypeOption.Value),
            ValueType = typeof(int),
            FillWeight = 135,
            FlatStyle = FlatStyle.Flat,
            SortMode = DataGridViewColumnSortMode.NotSortable
        });
        _layers.Columns.Add(new DataGridViewComboBoxColumn
        {
            Name = "LineWeight",
            HeaderText = "Độ dày",
            DataSource = LayerLineWeights,
            DisplayMember = nameof(LayerLineWeightOption.Name),
            ValueMember = nameof(LayerLineWeightOption.Millimeters),
            ValueType = typeof(double),
            FillWeight = 90,
            FlatStyle = FlatStyle.Flat,
            SortMode = DataGridViewColumnSortMode.NotSortable
        });
        _layers.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Appearance",
            HeaderText = "Xem trước",
            FillWeight = 85,
            ReadOnly = true,
            SortMode = DataGridViewColumnSortMode.NotSortable
        });
        _layers.Columns[1].ReadOnly = true;
        _layers.Columns[2].ReadOnly = true;
        _layers.Columns[1].FillWeight = 160;
        _layers.Columns[2].FillWeight = 220;
        _layers.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
        _layers.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        _layers.AllowUserToResizeRows = false;
        _layers.EditMode = DataGridViewEditMode.EditOnEnter;
        _layers.RowTemplate.Height = 28;
        _layers.Columns["Appearance"]!.DefaultCellStyle.SelectionBackColor = _layers.DefaultCellStyle.BackColor;
        _layers.Columns["Appearance"]!.DefaultCellStyle.SelectionForeColor = _layers.DefaultCellStyle.ForeColor;
        _layers.ColumnHeadersHeight = 30;
        _layers.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        _layers.CellPainting += PaintLayerCells;
        _layers.CellContentClick += HandleLayerCellClick;
        _layers.CellToolTipTextNeeded += SetLayerCellToolTip;
        _layers.EditingControlShowing += ConfigureLineTypeEditor;
        _layers.CurrentCellDirtyStateChanged += (_, _) =>
        {
            if (_layers.IsCurrentCellDirty && _layers.CurrentCell is DataGridViewComboBoxCell)
                _layers.CommitEdit(DataGridViewDataErrorContexts.Commit);
        };
        _layers.CellValueChanged += (_, e) =>
        {
            if (e.RowIndex >= 0 && _layers.Columns[e.ColumnIndex].Name is "Color" or "LineType" or "LineWeight")
                _layers.InvalidateCell(_layers.Columns["Appearance"]!.Index, e.RowIndex);
        };
        _bend.Items.AddRange(new object[] { "Giữ nguyên đường dài", "Chỉ đánh dấu 2 đầu" });
        _alignment.Items.AddRange(new object[] { "Căn trái", "Căn giữa" });
        Populate(settings);

        var main = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(12), ColumnCount = 1, RowCount = 3 };
        main.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        main.RowStyles.Add(new RowStyle(SizeType.Percent, 56));
        main.RowStyles.Add(new RowStyle(SizeType.Percent, 44));
        main.Controls.Add(BuildProfiles(previewMode), 0, 0);
        main.Controls.Add(BuildLayerGroup(), 0, 1);
        main.Controls.Add(BuildBendTextGroup(), 0, 2);
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 48, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(8, 4, 8, 4) };
        var save = new Button { Text = previewMode ? "Đóng" : "Lưu cài đặt", DialogResult = previewMode ? DialogResult.Cancel : DialogResult.None, AutoSize = true };
        var cancel = new Button { Text = "Đóng", DialogResult = DialogResult.Cancel, AutoSize = true };
        if (!previewMode)
        {
            save.Click += (_, _) =>
            {
                if (!ValidateNumberInputs()) return;
                DialogResult = DialogResult.OK;
                Close();
            };
        }
        buttons.Controls.Add(save);
        if (!previewMode) buttons.Controls.Add(cancel);
        Controls.Add(main); Controls.Add(buttons); AcceptButton = save; CancelButton = previewMode ? save : cancel;
        _bend.SelectedIndexChanged += (_, _) => SetBendInputs();
        _text.CheckedChanged += (_, _) => SetTextInputs();
        SetBendInputs();
        SetTextInputs();
    }

    public void ApplyTo(ExportSettings settings)
    {
        if (!ValidateNumberInputs()) throw new InvalidOperationException("Vui lòng nhập các giá trị số hợp lệ.");
        settings.FilePattern = ExportSettings.DefaultFilePattern;
        settings.BendMode = _bend.Text; settings.AddInfoText = _text.Checked;
        settings.TextAlignment = _alignment.Text;
        if (_text.Checked)
        {
            settings.TextHeight = ReadNumber(_height); settings.TextLineGap = ReadNumber(_gap); settings.TextYOffset = ReadNumber(_yOffset);
        }
        settings.BendUpLen = ReadNumber(_upLen); settings.BendUpInset = ReadNumber(_upInset); settings.BendDownLen = ReadNumber(_downLen); settings.BendDownInset = ReadNumber(_downInset);
        settings.LayerSettings.Clear();
        settings.LayerStyles.Clear();
        _layers.EndEdit();
        foreach (DataGridViewRow row in _layers.Rows)
        {
            var name = Convert.ToString(row.Cells[1].Value, CultureInfo.InvariantCulture) ?? "";
            settings.LayerSettings[name] = Convert.ToBoolean(row.Cells[0].Value ?? false) ? "True" : "False";
            settings.LayerStyles[name] = new LayerExportStyle(
                Convert.ToString(row.Cells[3].Value, CultureInfo.InvariantCulture) ?? "",
                Convert.ToInt32(row.Cells[4].Value, CultureInfo.InvariantCulture),
                Convert.ToDouble(row.Cells[5].Value, CultureInfo.InvariantCulture));
        }
    }

    private Control BuildProfiles(bool previewMode)
    {
        var bar = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, AutoScroll = false, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(0, 4, 0, 0) };
        bar.Controls.Add(new Label { Text = "Cấu hình dự án:", AutoSize = true, Margin = new Padding(0, 4, 8, 0) });
        if (!previewMode) RefreshProfiles();
        _profileList.Margin = new Padding(0, 0, 8, 0);
        _profileList.DropDown += (_, _) => RefreshProfiles();
        bar.Controls.Add(_profileList);
        bar.Controls.Add(new Button { Text = "Nạp", AutoSize = true, Margin = new Padding(0, 0, 12, 0) }.Also(b => b.Click += (_, _) => LoadProfile()));
        bar.Controls.Add(new Button { Text = "Lưu mẫu", AutoSize = true, Margin = new Padding(0, 0, 8, 0) }.Also(b => b.Click += (_, _) => SaveProfile()));
        bar.Controls.Add(new Button { Text = "Xóa mẫu", AutoSize = true }.Also(b => b.Click += (_, _) => DeleteProfile()));
        bar.Enabled = !previewMode;
        return bar;
    }

    private Control BuildLayerGroup()
    {
        var group = new GroupBox { Text = "Layer DXF (2000)", Dock = DockStyle.Fill };
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 38, Padding = new Padding(4, 2, 4, 2), WrapContents = false };
        buttons.Controls.Add(new Button { Text = "Chọn tất cả", AutoSize = true, Margin = new Padding(0, 0, 8, 0) }.Also(b => b.Click += (_, _) => SetLayers(true)));
        buttons.Controls.Add(new Button { Text = "Bỏ chọn tất cả", AutoSize = true, Margin = new Padding(0, 0, 8, 0) }.Also(b => b.Click += (_, _) => SetLayers(false)));
        buttons.Controls.Add(new Button { Text = "Mặc định Outer + Interior", AutoSize = true }.Also(b => b.Click += (_, _) => SetDefaultLayers()));
        buttons.Controls.Add(new Button { Text = "Màu mặc định", AutoSize = true, Margin = new Padding(8, 0, 0, 0) }
            .Also(b => b.Click += (_, _) => ResetSelectedLayerColors()));
        _layers.Dock = DockStyle.Fill;
        group.Controls.Add(_layers); group.Controls.Add(buttons); return group;
    }

    private Control BuildBendTextGroup()
    {
        var group = new GroupBox { Text = "Đường chấn và thông tin TEXT", Dock = DockStyle.Fill };
        var columns = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(10, 6, 10, 6), ColumnCount = 2, RowCount = 1 };
        columns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        columns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

        var bendSettings = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Margin = new Padding(0, 0, 8, 0) };
        for (int i = 0; i < 3; i++) bendSettings.RowStyles.Add(new RowStyle(SizeType.Percent, 100F / 3));
        bendSettings.Controls.Add(LabeledInput("Kiểu đường chấn", _bend), 0, 0);
        bendSettings.Controls.Add(LabeledInput("Bend UP: dài / mép", Inline(_upLen, _upInset)), 0, 1);
        bendSettings.Controls.Add(LabeledInput("Bend DOWN: dài / mép", Inline(_downLen, _downInset)), 0, 2);

        var textSettings = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Margin = new Padding(8, 0, 0, 0) };
        textSettings.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        textSettings.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var textHeader = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
        textHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        textHeader.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _text.Anchor = AnchorStyles.Left;
        textHeader.Controls.Add(_text, 0, 0);
        var alignment = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, FlowDirection = FlowDirection.LeftToRight, Anchor = AnchorStyles.Right, Padding = new Padding(0, 6, 0, 0) };
        alignment.Controls.Add(FieldLabel("Căn lề"));
        _alignment.Width = 150;
        _alignment.Margin = new Padding(8, 0, 0, 0);
        alignment.Controls.Add(_alignment);
        textHeader.Controls.Add(alignment, 1, 0);
        textSettings.Controls.Add(textHeader, 0, 0);

        var textValues = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 2 };
        for (int i = 0; i < 3; i++) textValues.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F / 3));
        textValues.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        textValues.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        textValues.Controls.Add(FieldLabel("Cỡ chữ"), 0, 0);
        textValues.Controls.Add(FieldLabel("Khoảng cách"), 1, 0);
        textValues.Controls.Add(FieldLabel("Khoảng xuống dòng"), 2, 0);
        textValues.Controls.Add(_height, 0, 1);
        textValues.Controls.Add(_gap, 1, 1);
        textValues.Controls.Add(_yOffset, 2, 1);
        textSettings.Controls.Add(textValues, 0, 1);

        columns.Controls.Add(bendSettings, 0, 0);
        columns.Controls.Add(textSettings, 1, 0);
        group.Controls.Add(columns);
        return group;
    }

    private void Populate(ExportSettings s)
    {
        s.FilePattern = ExportSettings.DefaultFilePattern;
        _bend.SelectedItem = s.BendMode; if (_bend.SelectedIndex < 0) _bend.SelectedIndex = 0;
        _text.Checked = s.AddInfoText; _alignment.SelectedItem = s.TextAlignment; if (_alignment.SelectedIndex < 0) _alignment.SelectedIndex = 1;
        SetNumber(_height, s.TextHeight, 0.1M, 500M, 1); SetNumber(_gap, s.TextLineGap, 0.1M, 500M, 1); SetNumber(_yOffset, s.TextYOffset, -10000M, 10000M, 1);
        SetNumber(_upLen, s.BendUpLen, 0M, 1000M, 2); SetNumber(_upInset, s.BendUpInset, 0M, 1000M, 2);
        SetNumber(_downLen, s.BendDownLen, 0M, 1000M, 2); SetNumber(_downInset, s.BendDownInset, 0M, 1000M, 2);
        _layers.Rows.Clear();
        foreach (var layer in Layers)
        {
            bool enabled = layer.Default;
            if (s.LayerSettings.TryGetValue(layer.Name, out var stored)) enabled = stored.Split('|')[0].Equals("true", StringComparison.OrdinalIgnoreCase);
            var style = s.LayerStyles.TryGetValue(layer.Name, out var configuredStyle) ? configuredStyle : new LayerExportStyle();
            var color = IsValidRgb(style.ColorRgb) ? style.ColorRgb : "";
            var lineType = LayerLineTypes.Any(option => option.Value == style.LineType)
                ? style.LineType
                : 37648;
            var lineWeight = LayerLineWeights.Any(option => option.Millimeters == style.LineWeightMm)
                ? style.LineWeightMm
                : 0.5;
            _layers.Rows.Add(enabled, layer.Name, layer.Description, color, lineType, lineWeight, DBNull.Value);
        }
    }

    private void RefreshProfiles()
    {
        var selected = _profileList.SelectedItem?.ToString(); _profileList.Items.Clear();
        foreach (var profile in _profiles.List()) _profileList.Items.Add(profile);
        if (selected is not null && _profileList.Items.Contains(selected)) _profileList.SelectedItem = selected;
        else if (_profileList.Items.Count > 0) _profileList.SelectedIndex = 0;
    }
    private void LoadProfile()
    {
        try
        {
            RefreshProfiles();
            if (_profileList.SelectedItem is not string name)
            {
                MessageBox.Show(this, "Chưa có cấu hình nào để nạp. Hãy lưu một mẫu trước.", "Nạp cấu hình", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (!_profiles.TryLoad(name, _settings))
            {
                RefreshProfiles();
                MessageBox.Show(this, $"Không tìm thấy file cấu hình '{name}'.", "Nạp cấu hình", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            Populate(_settings);
            MessageBox.Show(this, $"Đã nạp cấu hình '{name}'.", "Nạp cấu hình", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Nạp mẫu", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }
    private void SaveProfile()
    {
        if (!ValidateNumberInputs()) return;
        var initialName = _profileList.SelectedItem?.ToString() ?? string.Empty;
        var name = PromptProfileName(initialName);
        if (name is null) return;
        try
        {
            var existing = _profiles.List().Any(profile => profile.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (existing && MessageBox.Show(this, $"Cấu hình '{name}' đã tồn tại. Ghi đè?", "Lưu mẫu", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;
            ApplyTo(_settings);
            _profiles.Save(name, _settings);
            RefreshProfiles();
            _profileList.SelectedItem = name.Trim();
            MessageBox.Show(this, $"Đã lưu cấu hình '{name.Trim()}'.", "Lưu mẫu", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Lưu mẫu", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }
    private void DeleteProfile()
    {
        if (_profileList.SelectedItem is not string name)
        {
            MessageBox.Show(this, "Chọn cấu hình cần xóa.", "Xóa mẫu", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        if (MessageBox.Show(this, "Xóa mẫu '" + name + "'?", "Xóa mẫu", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        try { _profiles.Delete(name); RefreshProfiles(); } catch (Exception ex) { MessageBox.Show(ex.Message, "Xóa mẫu", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }
    private string? PromptProfileName(string initialName)
    {
        using var dialog = new Form
        {
            Text = "Lưu cấu hình",
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MinimizeBox = false,
            MaximizeBox = false,
            ShowInTaskbar = false,
            ClientSize = new System.Drawing.Size(360, 116)
        };
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(14), ColumnCount = 1, RowCount = 3 };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var prompt = new Label { Text = "Nhập tên khách hàng/dự án:", AutoSize = true, Anchor = AnchorStyles.Left };
        var name = new TextBox { Text = initialName, Dock = DockStyle.Fill };
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, WrapContents = false };
        var save = new Button { Text = "Lưu", DialogResult = DialogResult.OK, AutoSize = true };
        var cancel = new Button { Text = "Hủy", DialogResult = DialogResult.Cancel, AutoSize = true };
        buttons.Controls.Add(save);
        buttons.Controls.Add(cancel);
        layout.Controls.Add(prompt, 0, 0);
        layout.Controls.Add(name, 0, 1);
        layout.Controls.Add(buttons, 0, 2);
        dialog.Controls.Add(layout);
        dialog.AcceptButton = save;
        dialog.CancelButton = cancel;
        dialog.Shown += (_, _) => { name.Focus(); name.SelectAll(); };
        return dialog.ShowDialog(this) == DialogResult.OK ? name.Text.Trim() : null;
    }
    private void SetLayers(bool value) { foreach (DataGridViewRow row in _layers.Rows) row.Cells[0].Value = value; }
    private void SetDefaultLayers() { for (int i = 0; i < _layers.Rows.Count; i++) _layers.Rows[i].Cells[0].Value = i < 2; }
    private void ResetSelectedLayerColors()
    {
        var rows = _layers.SelectedRows.Cast<DataGridViewRow>().ToArray();
        if (rows.Length == 0 && _layers.CurrentRow is { } currentRow)
            rows = new[] { currentRow };
        foreach (var row in rows)
            row.Cells["Color"].Value = "";
        _layers.Invalidate();
    }

    private void HandleLayerCellClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex != _layers.Columns["Color"]!.Index)
            return;

        var cell = _layers.Rows[e.RowIndex].Cells["Color"];
        using var picker = new LayerColorPickerForm(Convert.ToString(cell.Value, CultureInfo.InvariantCulture) ?? "");
        if (picker.ShowDialog(this) == DialogResult.OK)
        {
            cell.Value = picker.ColorRgb;
            _layers.InvalidateCell(e.ColumnIndex, e.RowIndex);
            _layers.InvalidateCell(_layers.Columns["Appearance"]!.Index, e.RowIndex);
        }
    }

    private void PaintLayerCells(object? sender, DataGridViewCellPaintingEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0)
            return;

        var columnName = _layers.Columns[e.ColumnIndex].Name;
        if (columnName == "Color")
        {
            PaintColorCell(e);
            return;
        }

        if (columnName == "LineType")
        {
            PaintLineTypeCell(e);
            return;
        }

        if (columnName == "Appearance")
            PaintAppearanceCell(e);
    }

    private void PaintColorCell(DataGridViewCellPaintingEventArgs e)
    {
        if (e.Graphics is not { } graphics)
            return;
        e.PaintBackground(e.CellBounds, true);
        var bounds = Rectangle.Inflate(e.CellBounds, -2, -2);
        var state = _layers.CurrentCellAddress.X == e.ColumnIndex && _layers.CurrentCellAddress.Y == e.RowIndex
            ? PushButtonState.Pressed
            : PushButtonState.Normal;
        ButtonRenderer.DrawButton(graphics, bounds, state);

        var value = Convert.ToString(e.Value, CultureInfo.InvariantCulture) ?? "";
        var swatch = new Rectangle(bounds.X + 6, bounds.Y + (bounds.Height - 15) / 2, 22, 15);
        DrawColorSwatch(graphics, swatch, ParseRgb(value));
        using var border = new Pen(Color.FromArgb(110, 110, 110));
        graphics.DrawRectangle(border, swatch);
        var textBounds = new Rectangle(swatch.Right + 5, bounds.Y, bounds.Right - swatch.Right - 8, bounds.Height);
        TextRenderer.DrawText(graphics, GetColorLabel(value), _layers.Font, textBounds,
            SystemColors.ControlText, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        e.Paint(e.CellBounds, DataGridViewPaintParts.Border);
        e.Handled = true;
    }

    private void PaintLineTypeCell(DataGridViewCellPaintingEventArgs e)
    {
        if (e.Graphics is not { } graphics)
            return;
        e.PaintBackground(e.CellBounds, true);
        var bounds = e.CellBounds;
        var arrowBounds = new Rectangle(bounds.Right - 19, bounds.Y + 1, 18, bounds.Height - 2);
        ComboBoxRenderer.DrawDropDownButton(graphics, arrowBounds, ComboBoxState.Normal);
        var lineType = Convert.ToInt32(e.Value ?? 37648, CultureInfo.InvariantCulture);
        var sampleBounds = new Rectangle(bounds.X + 5, bounds.Y + 2, Math.Max(24, Math.Min(38, bounds.Width / 3)), bounds.Height - 4);
        DrawLineSample(graphics, sampleBounds, lineType, 37648, 0.5, Color.FromArgb(55, 62, 70));
        var label = LayerLineTypes.FirstOrDefault(option => option.Value == lineType)?.Name ?? "Mặc định";
        var textBounds = new Rectangle(sampleBounds.Right + 3, bounds.Y, arrowBounds.Left - sampleBounds.Right - 5, bounds.Height);
        TextRenderer.DrawText(graphics, label, _layers.Font, textBounds,
            e.CellStyle?.ForeColor ?? SystemColors.ControlText, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        e.Paint(e.CellBounds, DataGridViewPaintParts.Border);
        e.Handled = true;
    }

    private void PaintAppearanceCell(DataGridViewCellPaintingEventArgs e)
    {
        if (e.Graphics is not { } graphics)
            return;
        e.PaintBackground(e.CellBounds, true);
        var row = _layers.Rows[e.RowIndex];
        var bounds = Rectangle.Inflate(e.CellBounds, -7, -3);
        var lineType = Convert.ToInt32(row.Cells["LineType"].Value ?? 37648, CultureInfo.InvariantCulture);
        var lineWeight = Convert.ToDouble(row.Cells["LineWeight"].Value ?? 0.5, CultureInfo.InvariantCulture);
        var color = ParseRgb(Convert.ToString(row.Cells["Color"].Value, CultureInfo.InvariantCulture) ?? "");
        DrawLineSample(graphics, bounds, lineType, 37648, lineWeight, color ?? Color.FromArgb(75, 82, 90));
        e.Paint(e.CellBounds, DataGridViewPaintParts.Border);
        e.Handled = true;
    }

    private void ConfigureLineTypeEditor(object? sender, DataGridViewEditingControlShowingEventArgs e)
    {
        if (e.Control is not ComboBox comboBox)
            return;

        comboBox.DrawItem -= DrawLineTypeOption;
        if (_layers.CurrentCell?.OwningColumn?.Name == "LineType")
        {
            comboBox.DrawMode = DrawMode.OwnerDrawFixed;
            comboBox.ItemHeight = 24;
            comboBox.DrawItem += DrawLineTypeOption;
        }
        else
        {
            comboBox.DrawMode = DrawMode.Normal;
        }
    }

    private void DrawLineTypeOption(object? sender, DrawItemEventArgs e)
    {
        if (sender is not ComboBox comboBox || e.Index < 0 || e.Graphics is not { } graphics
            || comboBox.Items[e.Index] is not LayerLineTypeOption option)
            return;

        e.DrawBackground();
        var sampleBounds = new Rectangle(e.Bounds.X + 5, e.Bounds.Y + 2, 50, e.Bounds.Height - 4);
        DrawLineSample(graphics, sampleBounds, option.Value, 37648, 0.5, SystemColors.ControlText);
        var textBounds = new Rectangle(sampleBounds.Right + 6, e.Bounds.Y, e.Bounds.Right - sampleBounds.Right - 6, e.Bounds.Height);
        TextRenderer.DrawText(graphics, option.Name, comboBox.Font, textBounds,
            e.ForeColor, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        e.DrawFocusRectangle();
    }

    private void SetLayerCellToolTip(object? sender, DataGridViewCellToolTipTextNeededEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0)
            return;

        var row = _layers.Rows[e.RowIndex];
        switch (_layers.Columns[e.ColumnIndex].Name)
        {
            case "Color":
                var rgb = Convert.ToString(row.Cells["Color"].Value, CultureInfo.InvariantCulture) ?? "";
                e.ToolTipText = string.IsNullOrEmpty(rgb) ? "Đang dùng màu mặc định của Inventor. Nhấp để chọn màu." : $"{GetColorLabel(rgb)} ({rgb.Replace(';', ',')}). Nhấp để đổi màu.";
                break;
            case "LineType":
                e.ToolTipText = LayerLineTypes.FirstOrDefault(option =>
                    option.Value == Convert.ToInt32(row.Cells["LineType"].Value ?? 37648, CultureInfo.InvariantCulture))?.Name ?? "Mặc định";
                break;
            case "Appearance":
                e.ToolTipText = $"{GetColorLabel(Convert.ToString(row.Cells["Color"].Value, CultureInfo.InvariantCulture) ?? "")}; "
                    + $"{LayerLineTypes.FirstOrDefault(option => option.Value == Convert.ToInt32(row.Cells["LineType"].Value ?? 37648, CultureInfo.InvariantCulture))?.Name}; "
                    + $"{Convert.ToDouble(row.Cells["LineWeight"].Value ?? 0.5, CultureInfo.InvariantCulture):0.00} mm";
                break;
        }
    }

    private static void DrawLineSample(Graphics graphics, Rectangle bounds, int lineType, int defaultLineType,
        double lineWeight, Color color)
    {
        if (bounds.Width < 4 || bounds.Height < 4)
            return;

        var style = lineType == defaultLineType ? DashStyle.Solid : lineType switch
        {
            37634 or 37641 => DashStyle.Dash,
            37636 => DashStyle.Dot,
            37638 => DashStyle.DashDot,
            37644 => DashStyle.DashDotDot,
            _ => DashStyle.Solid
        };
        var width = (float)Math.Clamp(lineWeight * 2.0, 1.0, 4.0);
        using var pen = new Pen(color, width) { DashStyle = style };
        var y = bounds.Top + bounds.Height / 2f;
        var smoothingMode = graphics.SmoothingMode;
        try
        {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.DrawLine(pen, bounds.Left + 2, y, bounds.Right - 2, y);
        }
        finally
        {
            graphics.SmoothingMode = smoothingMode;
        }
    }

    private static void DrawColorSwatch(Graphics graphics, Rectangle bounds, Color? color)
    {
        if (color is { } selectedColor)
        {
            using var brush = new SolidBrush(selectedColor);
            graphics.FillRectangle(brush, bounds);
            return;
        }

        using var background = new SolidBrush(SystemColors.ControlLightLight);
        using var hatch = new HatchBrush(HatchStyle.LargeCheckerBoard, Color.LightGray, Color.White);
        graphics.FillRectangle(background, bounds);
        graphics.FillRectangle(hatch, bounds);
        TextRenderer.DrawText(graphics, "A", SystemFonts.MessageBoxFont, bounds, Color.DimGray,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
    }

    private static Color? ParseRgb(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var parts = value.Split(';');
        if (parts.Length != 3
            || !int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var red)
            || !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var green)
            || !int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var blue)
            || red is < 0 or > 255 || green is < 0 or > 255 || blue is < 0 or > 255)
            return null;
        return Color.FromArgb(red, green, blue);
    }

    private static bool IsValidRgb(string value) => ParseRgb(value).HasValue;

    private static string GetColorLabel(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "Mặc định";
        var color = ParseRgb(value);
        if (color is null)
            return "Mặc định";
        return LayerColors.FirstOrDefault(option => option.Color.ToArgb() == color.Value.ToArgb())?.Name
            ?? $"#{color.Value.R:X2}{color.Value.G:X2}{color.Value.B:X2}";
    }

    private static string FormatRgb(Color color) => string.Join(";",
        color.R.ToString(CultureInfo.InvariantCulture),
        color.G.ToString(CultureInfo.InvariantCulture),
        color.B.ToString(CultureInfo.InvariantCulture));

    private void SetBendInputs() { bool active = _bend.SelectedIndex == 1; _upLen.Enabled = active; _upInset.Enabled = active; _downLen.Enabled = active; _downInset.Enabled = active; }
    private void SetTextInputs() { _height.Enabled = _text.Checked; _gap.Enabled = _text.Checked; _yOffset.Enabled = _text.Checked; _alignment.Enabled = _text.Checked; }
    private static Label FieldLabel(string text) => new() { Text = text, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 0, 0, 2) };
    private static TextBox Number(decimal value, int decimals, int width) => new()
    {
        Text = value.ToString($"F{decimals}", CultureInfo.CurrentCulture),
        TextAlign = HorizontalAlignment.Left,
        Width = width,
        MaxLength = 16
    };
    private static void SetNumber(TextBox input, double value, decimal min, decimal max, int decimals)
    {
        var numericValue = Math.Clamp((decimal)value, min, max);
        input.Text = numericValue.ToString($"F{decimals}", CultureInfo.CurrentCulture);
    }
    private static FlowLayoutPanel Inline(params Control[] controls)
    {
        var panel = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, FlowDirection = FlowDirection.LeftToRight, Anchor = AnchorStyles.Left };
        foreach (var control in controls)
        {
            control.Margin = new Padding(0, 3, 6, 3);
            panel.Controls.Add(control);
        }
        return panel;
    }
    private static Control LabeledInput(string label, Control input)
    {
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Margin = new Padding(4, 2, 8, 2) };
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 48));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 52));
        layout.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left | AnchorStyles.Bottom, Margin = new Padding(0, 0, 0, 3) }, 0, 0);
        input.Dock = input is FlowLayoutPanel ? DockStyle.Fill : DockStyle.Left;
        input.Anchor = AnchorStyles.Left;
        layout.Controls.Add(input, 0, 1);
        return layout;
    }
    private bool ValidateNumberInputs()
    {
        return (!_text.Checked || ValidateNumber(_height, "Cỡ chữ", 0.1M, 500M, 1))
            && (!_text.Checked || ValidateNumber(_gap, "Khoảng cách", 0.1M, 500M, 1))
            && (!_text.Checked || ValidateNumber(_yOffset, "Lệch Y", -10000M, 10000M, 1))
            && ValidateNumber(_upLen, "Bend UP - dài", 0M, 1000M, 2)
            && ValidateNumber(_upInset, "Bend UP - mép", 0M, 1000M, 2)
            && ValidateNumber(_downLen, "Bend DOWN - dài", 0M, 1000M, 2)
            && ValidateNumber(_downInset, "Bend DOWN - mép", 0M, 1000M, 2);
    }
    private bool ValidateNumber(TextBox input, string label, decimal min, decimal max, int decimals)
    {
        var numberStyles = NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite | NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint;
        if (!decimal.TryParse(input.Text, numberStyles, CultureInfo.CurrentCulture, out var value) || value < min || value > max || decimal.Round(value, decimals) != value)
        {
            MessageBox.Show(this, $"{label} phải là số từ {min} đến {max}, tối đa {decimals} chữ số thập phân.", "Giá trị không hợp lệ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            input.Focus();
            input.SelectAll();
            return false;
        }
        return true;
    }
    private static double ReadNumber(TextBox input) => (double)decimal.Parse(
        input.Text,
        NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite | NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
        CultureInfo.CurrentCulture);

    private sealed record LayerColorOption(string Name, Color Color)
    {
        public string ColorRgb => FormatRgb(Color);
    }
    private sealed record LayerLineTypeOption(string Name, int Value);
    private sealed record LayerLineWeightOption(string Name, double Millimeters);

    private sealed class LayerColorPickerForm : Form
    {
        private readonly string _currentRgb;
        public string ColorRgb { get; private set; }

        public LayerColorPickerForm(string currentRgb)
        {
            _currentRgb = currentRgb;
            ColorRgb = currentRgb;
            Text = "Chọn màu layer";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(310, 245);

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10),
                ColumnCount = 1,
                RowCount = 3
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            layout.Controls.Add(new Label
            {
                Text = "Bảng màu cơ bản · chọn “Màu khác…” để nhập màu tùy chỉnh",
                Dock = DockStyle.Fill,
                AutoEllipsis = true,
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft
            }, 0, 0);

            var palette = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 8,
                RowCount = 5,
                Padding = new Padding(1)
            };
            for (var column = 0; column < palette.ColumnCount; column++)
                palette.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12.5F));
            for (var row = 0; row < palette.RowCount; row++)
                palette.RowStyles.Add(new RowStyle(SizeType.Percent, 20F));

            foreach (var option in LayerColors)
            {
                var tile = new Button
                {
                    Dock = DockStyle.Fill,
                    Margin = new Padding(2),
                    BackColor = option.Color,
                    FlatStyle = FlatStyle.Flat,
                    Text = string.Equals(option.ColorRgb, currentRgb, StringComparison.Ordinal) ? "✓" : "",
                    ForeColor = option.Color.GetBrightness() < 0.5F ? Color.White : Color.Black,
                    AccessibleName = option.Name,
                    TabStop = true
                };
                tile.FlatAppearance.BorderColor = string.Equals(option.ColorRgb, currentRgb, StringComparison.Ordinal)
                    ? SystemColors.Highlight
                    : SystemColors.ControlDark;
                tile.FlatAppearance.BorderSize = string.Equals(option.ColorRgb, currentRgb, StringComparison.Ordinal) ? 2 : 1;
                _toolTip.SetToolTip(tile, option.Name);
                tile.Click += (_, _) => SelectColor(option.ColorRgb);
                var index = Array.IndexOf(LayerColors, option);
                palette.Controls.Add(tile, index % palette.ColumnCount, index / palette.ColumnCount);
            }

            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false
            };
            var cancel = new Button { Text = "Hủy", DialogResult = DialogResult.Cancel, AutoSize = true };
            var custom = new Button { Text = "Màu khác…", AutoSize = true };
            var defaultColor = new Button { Text = "Mặc định", AutoSize = true, Margin = new Padding(0, 0, 8, 0) };
            custom.Click += (_, _) => PickCustomColor();
            defaultColor.Click += (_, _) => SelectColor("");
            buttons.Controls.Add(cancel);
            buttons.Controls.Add(custom);
            buttons.Controls.Add(defaultColor);
            layout.Controls.Add(palette, 0, 1);
            layout.Controls.Add(buttons, 0, 2);
            Controls.Add(layout);
            CancelButton = cancel;
            FormClosed += (_, _) => _toolTip.Dispose();
        }

        private readonly ToolTip _toolTip = new();

        private void SelectColor(string value)
        {
            ColorRgb = value;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void PickCustomColor()
        {
            using var dialog = new ColorDialog
            {
                AllowFullOpen = true,
                AnyColor = true,
                FullOpen = true,
                SolidColorOnly = true,
                Color = ParseRgb(_currentRgb) ?? Color.Black
            };
            if (dialog.ShowDialog(this) == DialogResult.OK)
                SelectColor(FormatRgb(dialog.Color));
        }
    }
}

internal static class ControlExtensions
{
    public static T Also<T>(this T control, Action<T> configure) where T : Control { configure(control); return control; }
}
