using InventorDxfExporter.Core;
using InventorDxfExporter.Settings;
using System.Globalization;
using System.Windows.Forms;

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
        new("Mặc định", ""),
        new("Đen", "0;0;0"),
        new("Đỏ", "255;0;0"),
        new("Vàng", "255;255;0"),
        new("Xanh lá", "0;255;0"),
        new("Cyan", "0;255;255"),
        new("Xanh dương", "0;0;255"),
        new("Tím", "255;0;255"),
        new("Trắng", "255;255;255")
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
        new("0,13 mm", 0.13), new("0,18 mm", 0.18), new("0,25 mm", 0.25),
        new("0,35 mm", 0.35), new("0,50 mm", 0.50), new("0,70 mm", 0.70),
        new("1,00 mm", 1.00), new("1,40 mm", 1.40), new("2,00 mm", 2.00)
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
        MinimizeBox = false; MaximizeBox = false; ClientSize = new System.Drawing.Size(1200, 830);
        _layers.Columns.Add(new DataGridViewCheckBoxColumn { Name = "Enabled", HeaderText = "Xuất", FillWeight = 35 });
        _layers.Columns.Add("Name", "Tên layer");
        _layers.Columns.Add("Description", "Mô tả");
        _layers.Columns.Add(new DataGridViewComboBoxColumn
        {
            Name = "Color",
            HeaderText = "Màu",
            DataSource = LayerColors,
            DisplayMember = nameof(LayerColorOption.Name),
            ValueMember = nameof(LayerColorOption.ColorRgb),
            ValueType = typeof(string),
            FillWeight = 100,
            FlatStyle = FlatStyle.Flat
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
            FlatStyle = FlatStyle.Flat
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
            FlatStyle = FlatStyle.Flat
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
        _layers.ColumnHeadersHeight = 30;
        _layers.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
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
            var color = LayerColors.Any(option => string.Equals(option.ColorRgb, style.ColorRgb, StringComparison.Ordinal))
                ? style.ColorRgb
                : "";
            var lineType = LayerLineTypes.Any(option => option.Value == style.LineType)
                ? style.LineType
                : 37648;
            var lineWeight = LayerLineWeights.Any(option => option.Millimeters == style.LineWeightMm)
                ? style.LineWeightMm
                : 0.5;
            _layers.Rows.Add(enabled, layer.Name, layer.Description, color, lineType, lineWeight);
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

    private sealed record LayerColorOption(string Name, string ColorRgb);
    private sealed record LayerLineTypeOption(string Name, int Value);
    private sealed record LayerLineWeightOption(string Name, double Millimeters);
}

internal static class ControlExtensions
{
    public static T Also<T>(this T control, Action<T> configure) where T : Control { configure(control); return control; }
}
