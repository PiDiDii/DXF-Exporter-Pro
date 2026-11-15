using InventorDxfExporter.Core;
using InventorDxfExporter.Settings;
using System.Globalization;
using System.Windows.Forms;

namespace InventorDxfExporter.UI.Forms;

public sealed class ExportSettingsForm : Form
{
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
        ArgumentNullException.ThrowIfNull(settings);
        _settings = CreateWorkingCopy(settings);
        UiStyles.Initialize(this);
        Text = previewMode ? "Xem trước cài đặt xuất DXF" : "Cài đặt xuất DXF";
        StartPosition = previewMode ? FormStartPosition.CenterScreen : FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimizeBox = false;
        MaximizeBox = true;
        MinimumSize = new System.Drawing.Size(900, 660);
        ClientSize = new System.Drawing.Size(1000, 700);
        _layers.Columns.Add(new DataGridViewCheckBoxColumn { Name = "Enabled", HeaderText = "Xuất", FillWeight = 35 });
        _layers.Columns.Add("Name", "Tên layer");
        _layers.Columns.Add("Description", "Mô tả");
        _layers.Columns[1].ReadOnly = true;
        _layers.Columns[2].ReadOnly = true;
        _layers.Columns[0].FillWeight = 42;
        _layers.Columns[1].FillWeight = 170;
        _layers.Columns[2].FillWeight = 205;
        UiStyles.StyleGrid(_layers);
        _layers.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
        _layers.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        _layers.AllowUserToResizeRows = false;
        _layers.EditMode = DataGridViewEditMode.EditOnEnter;
        _layers.RowTemplate.Height = 34;
        _layers.ColumnHeadersHeight = 34;
        _layers.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        _bend.Items.AddRange(new object[] { "Giữ nguyên đường dài", "Chỉ đánh dấu 2 đầu" });
        _alignment.Items.AddRange(new object[] { "Căn trái", "Căn giữa" });
        Populate(_settings);

        var tabs = new TabControl { Dock = DockStyle.Fill, Padding = new System.Drawing.Point(14, 6), Margin = new Padding(18, 4, 18, 10) };
        var layersPage = new TabPage("Layer DXF") { BackColor = UiStyles.Canvas, Padding = new Padding(12) };
        var bendPage = new TabPage("Đường chấn & TEXT") { BackColor = UiStyles.Canvas, Padding = new Padding(12) };
        layersPage.Controls.Add(BuildLayerGroup());
        bendPage.Controls.Add(BuildBendTextGroup());
        tabs.TabPages.Add(layersPage);
        tabs.TabPages.Add(bendPage);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = Padding.Empty,
            Margin = Padding.Empty,
            ColumnCount = 1,
            RowCount = 4
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 76));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        root.Controls.Add(UiStyles.CreateHeader(
            "Cài đặt xuất DXF",
            previewMode
                ? "Xem bố cục các thiết lập xuất theo từng nhóm."
                : "Chọn cấu hình dự án, sau đó chỉnh Layer, đường chấn hoặc nội dung TEXT."), 0, 0);
        root.Controls.Add(BuildProfiles(previewMode), 0, 1);
        root.Controls.Add(tabs, 0, 2);

        var save = UiStyles.CreateButton(previewMode ? "Đóng" : "Lưu cài đặt", primary: !previewMode);
        save.DialogResult = previewMode ? DialogResult.Cancel : DialogResult.None;
        var cancel = UiStyles.CreateButton("Hủy");
        cancel.DialogResult = DialogResult.Cancel;
        if (!previewMode)
        {
            save.Click += (_, _) =>
            {
                if (!ValidateNumberInputs()) return;
                DialogResult = DialogResult.OK;
                Close();
            };
        }
        root.Controls.Add(previewMode
            ? UiStyles.CreateFooter(save)
            : UiStyles.CreateFooter(save, cancel), 0, 3);
        Controls.Add(root);
        AcceptButton = save;
        CancelButton = previewMode ? save : cancel;
        _bend.SelectedIndexChanged += (_, _) => SetBendInputs();
        _text.CheckedChanged += (_, _) => SetTextInputs();
        SetBendInputs();
        SetTextInputs();
    }

    public void ApplyTo(ExportSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (!ValidateNumberInputs()) throw new InvalidOperationException("Vui lòng nhập các giá trị số hợp lệ.");
        settings.BendMode = _bend.Text; settings.AddInfoText = _text.Checked;
        settings.TextAlignment = _alignment.Text;
        if (_text.Checked)
        {
            settings.TextHeight = ReadNumber(_height); settings.TextLineGap = ReadNumber(_gap); settings.TextYOffset = ReadNumber(_yOffset);
        }
        settings.BendUpLen = ReadNumber(_upLen); settings.BendUpInset = ReadNumber(_upInset); settings.BendDownLen = ReadNumber(_downLen); settings.BendDownInset = ReadNumber(_downInset);
        settings.LayerSettings.Clear();
        _layers.EndEdit();
        foreach (DataGridViewRow row in _layers.Rows)
        {
            var name = Convert.ToString(row.Cells[1].Value, CultureInfo.InvariantCulture) ?? "";
            settings.LayerSettings[name] = Convert.ToBoolean(row.Cells[0].Value ?? false).ToString();
        }
    }

    private Control BuildProfiles(bool previewMode)
    {
        var bar = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = true, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(0, 4, 0, 0) };
        bar.Controls.Add(new Label { Text = "Cấu hình dự án:", AutoSize = true, Margin = new Padding(0, 4, 8, 0) });
        if (!previewMode) RefreshProfiles();
        _profileList.Margin = new Padding(0, 0, 8, 0);
        _profileList.DropDown += (_, _) => RefreshProfiles();
        bar.Controls.Add(_profileList);
        bar.Controls.Add(UiStyles.CreateButton("Nạp").Also(b => b.Margin = new Padding(0, 0, 12, 0)).Also(b => b.Click += (_, _) => LoadProfile()));
        bar.Controls.Add(UiStyles.CreateButton("Lưu mẫu").Also(b => b.Margin = new Padding(0, 0, 8, 0)).Also(b => b.Click += (_, _) => SaveProfile()));
        bar.Controls.Add(UiStyles.CreateButton("Xóa mẫu").Also(b => b.Margin = new Padding(0, 0, 8, 0)).Also(b => b.Click += (_, _) => DeleteProfile()));
        bar.Enabled = !previewMode;
        return bar;
    }

    private Control BuildLayerGroup()
    {
        var group = new GroupBox { Text = "Layer DXF", Dock = DockStyle.Fill };
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 38, Padding = new Padding(4, 2, 4, 2), WrapContents = false };
        buttons.Controls.Add(UiStyles.CreateButton("Chọn tất cả").Also(b => b.Margin = new Padding(0, 0, 8, 0)).Also(b => b.Click += (_, _) => SetLayers(true)));
        buttons.Controls.Add(UiStyles.CreateButton("Bỏ chọn tất cả").Also(b => b.Margin = new Padding(0, 0, 8, 0)).Also(b => b.Click += (_, _) => SetLayers(false)));
        buttons.Controls.Add(UiStyles.CreateButton("Mặc định Outer + Interior").Also(b => b.Margin = new Padding(0, 0, 8, 0)).Also(b => b.Click += (_, _) => SetDefaultLayers()));
        _layers.Dock = DockStyle.Fill;
        group.Controls.Add(_layers); group.Controls.Add(buttons); return group;
    }

    private Control BuildBendTextGroup()
    {
        var group = new GroupBox { Text = "Đường chấn và thông tin TEXT", Dock = DockStyle.Fill };
        var columns = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(12, 8, 12, 8), ColumnCount = 2, RowCount = 1 };
        columns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        columns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

        var bendSettings = CreateBendSettingsColumn();

        var textSettings = CreateSettingsColumn(5, 118, 42);
        _text.Anchor = AnchorStyles.Left;
        _text.Margin = Padding.Empty;
        textSettings.Controls.Add(_text, 0, 0);
        textSettings.SetColumnSpan(_text, 2);
        _alignment.Width = 120;
        AddSettingRow(textSettings, 1, "Căn lề", _alignment);
        AddSettingRow(textSettings, 2, "Cỡ chữ", _height);
        AddSettingRow(textSettings, 3, "Khoảng cách dòng", _gap);
        AddSettingRow(textSettings, 4, "Lệch Y", _yOffset);

        columns.Controls.Add(bendSettings, 0, 0);
        columns.Controls.Add(textSettings, 1, 0);
        group.Controls.Add(columns);
        return group;
    }

    private static TableLayoutPanel CreateSettingsColumn(int rows, int labelWidth, int rowHeight)
    {
        var column = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Top,
            ColumnCount = 2,
            RowCount = rows,
            Padding = Padding.Empty,
            Margin = new Padding(0, 0, 12, 0)
        };
        column.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, labelWidth));
        column.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (var index = 0; index < rows; index++)
            column.RowStyles.Add(new RowStyle(SizeType.Absolute, rowHeight));
        return column;
    }

    private TableLayoutPanel CreateBendSettingsColumn()
    {
        const int rowHeight = 42;
        var column = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Top,
            ColumnCount = 3,
            RowCount = 3,
            Padding = Padding.Empty,
            Margin = new Padding(0, 0, 12, 0)
        };
        column.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 146));
        column.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 82));
        column.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 82));
        for (var index = 0; index < column.RowCount; index++)
            column.RowStyles.Add(new RowStyle(SizeType.Absolute, rowHeight));

        column.Controls.Add(CreateSettingLabel("Kiểu đường chấn"), 0, 0);
        _bend.Anchor = AnchorStyles.Left;
        _bend.Margin = Padding.Empty;
        column.Controls.Add(_bend, 1, 0);
        column.SetColumnSpan(_bend, 2);

        column.Controls.Add(CreateSettingLabel("Bend UP: dài / mép"), 0, 1);
        AddBendNumber(column, _upLen, 1, 1);
        AddBendNumber(column, _upInset, 2, 1);

        column.Controls.Add(CreateSettingLabel("Bend DOWN: dài / mép"), 0, 2);
        AddBendNumber(column, _downLen, 1, 2);
        AddBendNumber(column, _downInset, 2, 2);
        return column;
    }

    private static Label CreateSettingLabel(string text) => new()
    {
        Text = text,
        Dock = DockStyle.Fill,
        TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
        Margin = Padding.Empty
    };

    private static void AddBendNumber(TableLayoutPanel column, TextBox input, int targetColumn, int targetRow)
    {
        input.Anchor = AnchorStyles.Left;
        input.Margin = Padding.Empty;
        column.Controls.Add(input, targetColumn, targetRow);
    }

    private static void AddSettingRow(TableLayoutPanel column, int row, string label, Control input)
    {
        column.Controls.Add(new Label
        {
            Text = label,
            Dock = DockStyle.Fill,
            TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
            Margin = Padding.Empty
        }, 0, row);
        input.Margin = Padding.Empty;
        input.Anchor = AnchorStyles.Left;
        input.Dock = input is FlowLayoutPanel ? DockStyle.Fill : DockStyle.None;
        column.Controls.Add(input, 1, row);
    }

    private void Populate(ExportSettings s)
    {
        _bend.SelectedItem = s.BendMode; if (_bend.SelectedIndex < 0) _bend.SelectedIndex = 0;
        _text.Checked = s.AddInfoText; _alignment.SelectedItem = s.TextAlignment; if (_alignment.SelectedIndex < 0) _alignment.SelectedIndex = 1;
        SetNumber(_height, s.TextHeight, 0.1M, 500M, 1); SetNumber(_gap, s.TextLineGap, 0.1M, 500M, 1); SetNumber(_yOffset, s.TextYOffset, -10000M, 10000M, 1);
        SetNumber(_upLen, s.BendUpLen, 0M, 1000M, 2); SetNumber(_upInset, s.BendUpInset, 0M, 1000M, 2);
        SetNumber(_downLen, s.BendDownLen, 0M, 1000M, 2); SetNumber(_downInset, s.BendDownInset, 0M, 1000M, 2);
        _layers.Rows.Clear();
        foreach (var layer in DxfLayerOptionsBuilder.LayerOptions)
        {
            bool enabled = layer.DefaultEnabled;
            if (s.LayerSettings.TryGetValue(layer.Name, out var stored))
                enabled = bool.TryParse(stored, out var value) && value;
            _layers.Rows.Add(enabled, layer.Name, layer.Description);
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
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Nạp mẫu", MessageBoxButtons.OK, MessageBoxIcon.Error); }
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
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Lưu mẫu", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }
    private void DeleteProfile()
    {
        if (_profileList.SelectedItem is not string name)
        {
            MessageBox.Show(this, "Chọn cấu hình cần xóa.", "Xóa mẫu", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        if (MessageBox.Show(this, "Xóa mẫu '" + name + "'?", "Xóa mẫu", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        try { _profiles.Delete(name); RefreshProfiles(); } catch (Exception ex) { MessageBox.Show(this, ex.Message, "Xóa mẫu", MessageBoxButtons.OK, MessageBoxIcon.Error); }
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
        var save = UiStyles.CreateButton("Lưu");
        save.DialogResult = DialogResult.OK;
        var cancel = UiStyles.CreateButton("Hủy");
        cancel.DialogResult = DialogResult.Cancel;
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
        if (!UiNumberParser.TryParseDecimal(input.Text, CultureInfo.CurrentCulture, out var value) || value < min || value > max || decimal.Round(value, decimals) != value)
        {
            MessageBox.Show(this, $"{label} phải là số từ {min} đến {max}, tối đa {decimals} chữ số thập phân.", "Giá trị không hợp lệ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            input.Focus();
            input.SelectAll();
            return false;
        }
        return true;
    }
    private static double ReadNumber(TextBox input) =>
        (double)UiNumberParser.ParseDecimal(input.Text, CultureInfo.CurrentCulture);

    private static ExportSettings CreateWorkingCopy(ExportSettings settings)
    {
        var copy = new ExportSettings();
        ExportSettingsSnapshot.From(settings).Apply(copy);
        return copy;
    }

}

internal static class ControlExtensions
{
    public static T Also<T>(this T control, Action<T> configure) where T : Control { configure(control); return control; }
}
