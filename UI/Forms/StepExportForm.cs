using Inventor;
using InventorDxfExporter.Core;
using InventorDxfExporter.Services;
using InventorDxfExporter.Settings;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using InventorApplication = Inventor.Application;

namespace InventorDxfExporter.UI.Forms;

public sealed class StepExportForm : Form
{
    private readonly InventorApplication _application;
    private readonly Document _document;
    private readonly StepExportService _service;
    private readonly StepExportSettingsStore _settingsStore = new();
    private readonly StepExportSettings _settings;
    private readonly List<StepPartItem> _items;
    private readonly DataGridView _grid = new();
    private readonly System.Windows.Forms.ProgressBar _progress = new();
    private readonly Label _status = new();
    private readonly Button _selectAll = UiStyles.CreateButton("Chọn/Bỏ chọn tất cả");
    private readonly Button _settingsButton = UiStyles.CreateButton("Cài đặt");
    private readonly Button _export = UiStyles.CreateButton("Xuất STEP", primary: true);
    private readonly Button _close = UiStyles.CreateButton("Đóng");
    private bool _exporting;
    private bool _cancelled;

    public StepExportForm(InventorApplication application, Document document)
    {
        _application = application ?? throw new ArgumentNullException(nameof(application));
        _document = document ?? throw new ArgumentNullException(nameof(document));
        _service = new StepExportService(application);
        _settings = _settingsStore.Load();
        var collection = _service.Collect(document);
        _items = collection.Items;

        UiStyles.Initialize(this);
        Text = "Danh sách chi tiết xuất STEP";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(760, 460);
        ClientSize = new Size(910, 700);
        FormBorderStyle = FormBorderStyle.Sizable;

        ConfigureGrid();
        for (var index = 0; index < _items.Count; index++)
        {
            var item = _items[index];
            var rowIndex = _grid.Rows.Add(index + 1, true, item.PartNumber,
                item.Material, item.Quantity, item.Status);
            _grid.Rows[rowIndex].Tag = item;
        }

        _grid.CurrentCellDirtyStateChanged += (_, _) =>
        {
            if (_grid.IsCurrentCellDirty && _grid.CurrentCell is DataGridViewCheckBoxCell)
                _grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
        };
        _grid.CellValueChanged += (_, e) =>
        {
            if (e.RowIndex >= 0 && e.ColumnIndex == 1)
            {
                var item = _grid.Rows[e.RowIndex].Tag as StepPartItem;
                if (item is not null)
                    item.Export = Convert.ToBoolean(_grid.Rows[e.RowIndex].Cells[1].Value);
                UpdateSelection();
            }
        };
        _grid.CellDoubleClick += (_, e) =>
        {
            if (e.RowIndex >= 0 && e.ColumnIndex == 2)
                Preview(_items[e.RowIndex]);
        };

        BuildLayout(collection);
        UpdateSelection();
    }

    private void ConfigureGrid()
    {
        UiStyles.StyleGrid(_grid);
        _grid.Dock = DockStyle.Fill;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.MultiSelect = false;
        _grid.ReadOnly = false;
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Index",
            HeaderText = "STT",
            FillWeight = 42,
            ReadOnly = true,
            SortMode = DataGridViewColumnSortMode.NotSortable
        });
        _grid.Columns.Add(new DataGridViewCheckBoxColumn
        {
            Name = "Include",
            HeaderText = "Xuất",
            FillWeight = 48,
            SortMode = DataGridViewColumnSortMode.NotSortable
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "PartNumber",
            HeaderText = "Part Number (nhấn đúp để xem)",
            FillWeight = 190,
            ReadOnly = true
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Material",
            HeaderText = "Vật liệu",
            FillWeight = 120,
            ReadOnly = true
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Quantity",
            HeaderText = "Số lượng",
            FillWeight = 72,
            ReadOnly = true
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Status",
            HeaderText = "Trạng thái",
            FillWeight = 130,
            ReadOnly = true
        });
    }

    private void BuildLayout(StepCollectResult collection)
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = Padding.Empty
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 76));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));

        root.Controls.Add(UiStyles.CreateHeader("Xuất STEP từng chi tiết",
            BuildInfo(collection)), 0, 0);

        var gridHost = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12, 12, 12, 4),
            BackColor = UiStyles.Canvas
        };
        gridHost.Controls.Add(_grid);
        root.Controls.Add(gridHost, 0, 1);

        var footer = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            BackColor = UiStyles.Surface
        };
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 430));
        footer.RowStyles.Add(new RowStyle(SizeType.Absolute, 20));
        footer.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));

        _progress.Dock = DockStyle.Fill;
        _progress.Visible = false;
        footer.Controls.Add(_progress, 0, 0);
        footer.Controls.Add(_status, 1, 0);
        _status.Dock = DockStyle.Fill;
        _status.AutoSize = false;
        _status.ForeColor = UiStyles.MutedText;

        var buttons = UiStyles.CreateFooter(_selectAll, _settingsButton, _export, _close);
        footer.Controls.Add(buttons, 0, 1);
        footer.SetColumnSpan(buttons, 2);

        root.Controls.Add(footer, 0, 2);
        Controls.Add(root);

        _selectAll.Click += (_, _) => ToggleAll();
        _settingsButton.Click += (_, _) => ShowSettings();
        _export.Click += (_, _) => OnExport();
        _close.Click += (_, _) => Close();
        AcceptButton = _export;
        CancelButton = _close;
    }

    private static string BuildInfo(StepCollectResult collection)
    {
        var skipped = new List<string>();
        if (collection.SkippedLibrary > 0)
            skipped.Add($"{collection.SkippedLibrary} chi tiết thư viện");
        if (collection.SkippedOther > 0)
            skipped.Add($"{collection.SkippedOther} iPart/iAssembly");
        var info = skipped.Count == 0
            ? "Chọn các chi tiết cần xuất STEP AP214."
            : $"Đã bỏ qua: {string.Join(", ", skipped)}.";
        if (collection.Warnings.Count > 0)
            info += $" Có {collection.Warnings.Count} cảnh báo khi quét.";
        return info;
    }

    private void ToggleAll()
    {
        var allSelected = _items.All(item => item.Export);
        foreach (var item in _items)
            item.Export = !allSelected;
        foreach (DataGridViewRow row in _grid.Rows)
            row.Cells[1].Value = (row.Tag as StepPartItem)?.Export ?? false;
        UpdateSelection();
    }

    private void UpdateSelection()
    {
        var selected = _items.Count(item => item.Export);
        _status.Text = $"Đã chọn: {selected} / {_items.Count}";
    }

    private void ShowSettings()
    {
        using var form = new StepExportSettingsForm(_settings);
        if (form.ShowDialog(this) == DialogResult.OK)
            _settingsStore.Save(_settings);
    }

    private void Preview(StepPartItem item)
    {
        if (_exporting)
            return;
        try
        {
            Cursor = Cursors.WaitCursor;
            var previewPath = _service.GetPreview(item);
            using var preview = new StepPreviewForm(item.PartNumber, previewPath);
            preview.ShowDialog(this);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Không tạo được ảnh xem trước:\n" + ex.Message,
                "Xuất STEP", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        finally
        {
            Cursor = Cursors.Default;
        }
    }

    private void OnExport()
    {
        if (_exporting)
        {
            _cancelled = true;
            _export.Enabled = false;
            _status.Text = "Đang dừng sau chi tiết hiện tại...";
            return;
        }

        var selected = _items.Where(item => item.Export).ToList();
        if (selected.Count == 0)
        {
            MessageBox.Show(this, "Chưa chọn chi tiết nào để xuất.", Text,
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var folder = ResolveFolder();
        if (string.IsNullOrWhiteSpace(folder))
            return;

        _cancelled = false;
        _exporting = true;
        _grid.Enabled = false;
        _selectAll.Enabled = false;
        _settingsButton.Enabled = false;
        _close.Enabled = false;
        _export.Text = "Dừng";
        _export.Enabled = true;
        _progress.Visible = true;

        StepExportResult? result = null;
        string? error = null;
        try
        {
            result = _service.Export(selected, folder, _settings.DuplicateMode,
                AskDuplicate, ReportProgress, () => _cancelled);
        }
        catch (Exception ex)
        {
            error = ex.Message;
        }
        finally
        {
            _exporting = false;
            _grid.Enabled = true;
            _selectAll.Enabled = true;
            _settingsButton.Enabled = true;
            _close.Enabled = true;
            _export.Text = "Xuất STEP";
            _progress.Visible = false;
        }

        if (!string.IsNullOrWhiteSpace(error))
        {
            MessageBox.Show(this, "Lỗi khi xuất STEP:\n" + error, Text,
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        _settings.LastFolder = folder;
        _settingsStore.Save(_settings);

        var message = result!.Cancelled ? "Đã dừng." : "Hoàn tất.";
        message += $"\nĐã xuất: {result.Success}\nBỏ qua: {result.Skipped}\nLỗi: {result.Failed}";
        if (result.Errors.Count > 0)
        {
            message += "\n\nChi tiết lỗi:\n - " +
                string.Join("\n - ", result.Errors.Take(10));
            if (result.Errors.Count > 10)
                message += "\n ...";
        }
        message += "\n\nThư mục: " + folder + "\nMở thư mục chứa file?";

        if (MessageBox.Show(this, message, Text,
            MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = "\"" + folder + "\"",
                    UseShellExecute = true
                });
            }
            catch
            {
            }
        }
    }

    private void ReportProgress(int completed, int total, StepPartItem item)
    {
        _progress.Maximum = Math.Max(1, total);
        _progress.Value = Math.Min(completed, _progress.Maximum);
        _status.Text = $"Đang xuất {Math.Min(completed + 1, total)} / {total}: {item.PartNumber}";
        foreach (DataGridViewRow row in _grid.Rows)
        {
            if (ReferenceEquals(row.Tag, item))
            {
                row.Cells[5].Value = item.Status;
                row.Cells[5].Style.BackColor = item.Status switch
                {
                    "Đã xuất" => Color.LightGreen,
                    "Lỗi" => Color.FromArgb(255, 150, 150),
                    "Bỏ qua (đã tồn tại)" => Color.FromArgb(255, 255, 190),
                    _ => Color.White
                };
                break;
            }
        }
        Application.DoEvents();
    }

    private StepDuplicateAnswer AskDuplicate(string path)
    {
        using var form = new StepDuplicateForm(path);
        form.ShowDialog(this);
        return form.Answer;
    }

    private string ResolveFolder()
    {
        try
        {
            switch (_settings.SaveMode)
            {
                case StepSaveMode.FixedFolder:
                    if (string.IsNullOrWhiteSpace(_settings.FixedFolder))
                    {
                        MessageBox.Show(this, "Chưa chọn thư mục cố định. Hãy mở Cài đặt.",
                            Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return string.Empty;
                    }
                    Directory.CreateDirectory(_settings.FixedFolder);
                    return _settings.FixedFolder;

                case StepSaveMode.NextToDocument:
                    if (!File.Exists(_document.FullFileName))
                    {
                        MessageBox.Show(this, "File đang mở chưa được lưu. Hãy lưu file hoặc đổi cách lưu.",
                            Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return string.Empty;
                    }
                    var nextFolder = Path.Combine(
                        Path.GetDirectoryName(_document.FullFileName)!, "STEP");
                    Directory.CreateDirectory(nextFolder);
                    return nextFolder;

                default:
                    using var dialog = new FolderBrowserDialog
                    {
                        Description = "Chọn thư mục lưu file STEP",
                        SelectedPath = Directory.Exists(_settings.LastFolder)
                            ? _settings.LastFolder
                            : (File.Exists(_document.FullFileName)
                                ? Path.GetDirectoryName(_document.FullFileName)!
                                : Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory))
                    };
                    return dialog.ShowDialog(this) == DialogResult.OK
                        ? dialog.SelectedPath
                        : string.Empty;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Không truy cập được thư mục lưu:\n" + ex.Message,
                Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return string.Empty;
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (_exporting)
            e.Cancel = true;
        base.OnFormClosing(e);
    }
}

public sealed class StepExportSettingsForm : Form
{
    private readonly StepExportSettings _settings;

    public StepExportSettingsForm(StepExportSettings settings)
    {
        _settings = settings;
        UiStyles.Initialize(this);
        Text = "Cài đặt xuất STEP";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(660, 260);

        var saveMode = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Location = new Point(250, 40),
            Size = new Size(380, 25)
        };
        saveMode.Items.AddRange(["Hỏi vị trí lưu", "Thư mục cố định", "Thư mục STEP cạnh file đang mở"]);
        saveMode.SelectedIndex = (int)_settings.SaveMode;

        var folder = new TextBox
        {
            Location = new Point(250, 84),
            Size = new Size(310, 25),
            Text = _settings.FixedFolder
        };
        var browse = UiStyles.CreateFolderPicker(this, folder, "...", "Chọn thư mục lưu STEP");
        browse.Location = new Point(566, 83);

        var duplicate = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Location = new Point(250, 128),
            Size = new Size(380, 25)
        };
        duplicate.Items.AddRange(["Hỏi", "Ghi đè", "Bỏ qua"]);
        duplicate.SelectedIndex = (int)_settings.DuplicateMode;

        Controls.Add(MakeLabel("Lựa chọn lưu file STEP", 30, 44));
        Controls.Add(saveMode);
        Controls.Add(MakeLabel("Thư mục cố định", 30, 88));
        Controls.Add(folder);
        Controls.Add(browse);
        Controls.Add(MakeLabel("Khi file STEP đã tồn tại", 30, 132));
        Controls.Add(duplicate);

        var save = UiStyles.CreateButton("Lưu cài đặt", primary: true);
        save.Location = new Point(400, 205);
        var close = UiStyles.CreateButton("Đóng");
        close.Location = new Point(530, 205);
        Controls.Add(save);
        Controls.Add(close);

        void UpdateFolderEnabled()
        {
            folder.Enabled = saveMode.SelectedIndex == (int)StepSaveMode.FixedFolder;
            browse.Enabled = folder.Enabled;
        }
        saveMode.SelectedIndexChanged += (_, _) => UpdateFolderEnabled();
        UpdateFolderEnabled();

        save.Click += (_, _) =>
        {
            if (saveMode.SelectedIndex == (int)StepSaveMode.FixedFolder &&
                string.IsNullOrWhiteSpace(folder.Text))
            {
                MessageBox.Show(this, "Hãy chọn thư mục cố định.", Text,
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            _settings.SaveMode = (StepSaveMode)saveMode.SelectedIndex;
            _settings.FixedFolder = folder.Text.Trim();
            _settings.DuplicateMode = (StepDuplicateMode)duplicate.SelectedIndex;
            DialogResult = DialogResult.OK;
        };
        close.Click += (_, _) => DialogResult = DialogResult.Cancel;
        CancelButton = close;
    }

    private static Label MakeLabel(string text, int x, int y) => new()
    {
        Text = text,
        AutoSize = true,
        Location = new Point(x, y)
    };
}

public sealed class StepDuplicateForm : Form
{
    public StepDuplicateAnswer Answer { get; private set; } = StepDuplicateAnswer.Cancel;

    public StepDuplicateForm(string existingPath)
    {
        UiStyles.Initialize(this);
        Text = "File đã tồn tại";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(520, 210);

        Controls.Add(new Label
        {
            Text = "File STEP đã tồn tại:\n" + existingPath,
            AutoSize = false,
            Location = new Point(20, 20),
            Size = new Size(480, 70)
        });

        var overwrite = UiStyles.CreateButton("Ghi đè");
        overwrite.Location = new Point(20, 120);
        overwrite.Click += (_, _) => { Answer = StepDuplicateAnswer.Overwrite; DialogResult = DialogResult.OK; };
        var overwriteAll = UiStyles.CreateButton("Ghi đè tất cả");
        overwriteAll.Location = new Point(125, 120);
        overwriteAll.Click += (_, _) => { Answer = StepDuplicateAnswer.OverwriteAll; DialogResult = DialogResult.OK; };
        var skip = UiStyles.CreateButton("Bỏ qua");
        skip.Location = new Point(250, 120);
        skip.Click += (_, _) => { Answer = StepDuplicateAnswer.Skip; DialogResult = DialogResult.OK; };
        var skipAll = UiStyles.CreateButton("Bỏ qua tất cả");
        skipAll.Location = new Point(340, 120);
        skipAll.Click += (_, _) => { Answer = StepDuplicateAnswer.SkipAll; DialogResult = DialogResult.OK; };
        var cancel = UiStyles.CreateButton("Hủy");
        cancel.Location = new Point(425, 160);
        cancel.Click += (_, _) => { Answer = StepDuplicateAnswer.Cancel; DialogResult = DialogResult.Cancel; };

        Controls.Add(overwrite);
        Controls.Add(overwriteAll);
        Controls.Add(skip);
        Controls.Add(skipAll);
        Controls.Add(cancel);
        CancelButton = cancel;
    }
}

public sealed class StepPreviewForm : Form
{
    public StepPreviewForm(string title, string previewPath)
    {
        UiStyles.Initialize(this);
        Text = title;
        FormBorderStyle = FormBorderStyle.Sizable;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(560, 420);

        var picture = new PictureBox
        {
            Dock = DockStyle.Fill,
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.White
        };
        picture.Image = Image.FromFile(previewPath);
        Controls.Add(picture);
        CancelButton = new Button();
        KeyPreview = true;
        KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Escape)
                Close();
        };
    }
}
