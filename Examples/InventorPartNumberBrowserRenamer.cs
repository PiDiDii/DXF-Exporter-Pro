// InventorPartNumberBrowserRenamer.cs
// Reusable WinForms component for an Autodesk Inventor .NET Add-in.
// Reference Autodesk.Inventor.Interop.dll and System.Windows.Forms/System.Drawing.
// Call from an add-in command: InventorPartNumberBrowserRenamer.Show(_inventorApplication);
// No file is renamed or moved. Apply is intentionally NOT saved automatically.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Inventor;
using InventorApplication = Inventor.Application;
using Point = System.Drawing.Point;
using Color = System.Drawing.Color;
using Path = System.IO.Path;
using File = System.IO.File;
using Environment = System.Environment;
using WinFormsTextBox = System.Windows.Forms.TextBox;

namespace InventorDxfExporter.Commands
{
    public static class InventorPartNumberBrowserRenamer
    {
        public static void Show(InventorApplication inventorApplication)
        {
            if (_openForm is { IsDisposed: false })
            {
                _openForm.Activate();
                return;
            }

            if (inventorApplication == null) throw new ArgumentNullException(nameof(inventorApplication));
            Document active = inventorApplication.ActiveDocument;
            if (active == null || active.DocumentType != DocumentTypeEnum.kAssemblyDocumentObject)
            {
                MessageBox.Show(new InventorDxfExporter.UI.Forms.InventorWindowOwner(inventorApplication.MainFrameHWND),
                    "Hãy mở file Assembly (.iam) rồi chạy lệnh này.",
                    "Part Number / Browser Name", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _openForm = new RenamerForm(inventorApplication, (AssemblyDocument)active);
            _openForm.FormClosed += (_, _) => _openForm = null;
            _openForm.Show(new InventorDxfExporter.UI.Forms.InventorWindowOwner(inventorApplication.MainFrameHWND));
        }

        public static Form CreatePreviewForm() => new RenamerForm(previewOnly: true);

        private static RenamerForm? _openForm;
    }

    internal sealed class RenamerForm : Form
    {
        private const string SheetMetalSubtype = "{9C464203-9BAE-11D3-8BAD-0060B0CE6BB4}";
        private readonly InventorApplication? _app;
        private readonly AssemblyDocument? _assembly;
        private bool _previewOnly;
        private readonly List<DocumentRow> _documents = new List<DocumentRow>();
        private readonly List<OccurrenceRow> _occurrences = new List<OccurrenceRow>();
        private readonly List<string> _libraryPaths = new List<string>();

        private readonly WinFormsTextBox _prefix = new WinFormsTextBox();
        private readonly NumericUpDown _digits = new NumericUpDown();
        private readonly ComboBox _mode = new ComboBox();
        private readonly CheckBox _overwrite = new CheckBox();
        private readonly CheckBox _includeLibrary = new CheckBox();
        private readonly WinFormsTextBox _total = new WinFormsTextBox();
        private readonly Button _previewButton = new Button();
        private readonly Button _applyButton = new Button();
        private readonly DataGridView _grid = new DataGridView();
        private readonly Label _status = new Label();
        private readonly Label _structureSubtitle = new Label();
        private readonly Label _structureBody = new Label();
        private readonly WinFormsTextBox _log = new WinFormsTextBox();
        private readonly HashSet<string> _skipKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> _manualNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private bool _hasPreview;
        private bool _busy;
        private bool _applied;
        private Dictionary<string, string> _newPartNumbers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private enum NamingMode { BrowserName, PartNumber }

        private sealed class DocumentRow
        {
            public Document? Document;
            public string Key = string.Empty;
            public string OldPartNumber = string.Empty;
            public string NewPartNumber = string.Empty;
            public string Kind = string.Empty;
            public string KindText = string.Empty;
            public int Depth;
            public bool IsTop;
            public bool IsLibrary;
            public bool Skip;
            public string IgnoreReason = string.Empty;
            public string Status = string.Empty;
        }

        private sealed class OccurrenceRow
        {
            public ComponentOccurrence? Occurrence;
            public string DocumentKey = string.Empty;
            public string OldName = string.Empty;
            public int Depth;
            public string KindText = string.Empty;
            public string Status = string.Empty;
        }

        internal RenamerForm(InventorApplication app, AssemblyDocument assembly)
        {
            _app = app;
            _assembly = assembly;
            Initialize(previewOnly: false);
        }

        internal RenamerForm(bool previewOnly)
        {
            _previewOnly = previewOnly;
            Initialize(previewOnly);
        }

        private void Initialize(bool previewOnly)
        {
            _previewOnly = previewOnly;
            Text = "Đổi Part Number / Browser Name theo phân cấp lắp ráp";
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(820, 680);
            Size = new Size(1000, 820);
            Font = new Font("Segoe UI", 9F);
            BuildUi();
            if (_previewOnly)
                LoadPreviewData();
            else
                ScanAssembly();
            UpdatePreview();
        }

        private void BuildUi()
        {
            var top = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12, 8, 12, 4) };
            var settings = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 7,
                RowCount = 3,
                Margin = Padding.Empty
            };
            settings.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 55));
            settings.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190));
            settings.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
            settings.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 58));
            settings.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 105));
            settings.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70));
            settings.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            settings.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            settings.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            settings.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var lblPrefix = new Label { Text = "Prefix:", AutoSize = true, Anchor = AnchorStyles.Left };
            _prefix.Dock = DockStyle.Fill;
            _prefix.Margin = new Padding(0, 3, 10, 3);
            var lblTotal = new Label
            {
                Text = "Tổng SL chi tiết/cụm lắp:",
                AutoSize = true,
                Anchor = AnchorStyles.Left
            };
            _total.Dock = DockStyle.Fill;
            _total.Margin = new Padding(0, 3, 10, 3);
            _total.ReadOnly = true;
            _total.TextAlign = HorizontalAlignment.Center;
            var lblDigits = new Label { Text = "Số ký tự hậu tố:", AutoSize = true, Anchor = AnchorStyles.Left };
            _digits.Dock = DockStyle.Fill;
            _digits.Margin = new Padding(0, 3, 10, 3);
            _digits.Minimum = 1;
            _digits.Maximum = 6;
            _digits.Value = 2;

            var lblMode = new Label { Text = "Chế độ:", AutoSize = true, Anchor = AnchorStyles.Left };
            _mode.DropDownStyle = ComboBoxStyle.DropDownList;
            _mode.Items.Add("Browser Name (chỉ đổi tên hiển thị)");
            _mode.Items.Add("Part Number (chỉ đổi iProperty)");
            _mode.SelectedIndex = 0;
            _mode.Dock = DockStyle.Fill;
            _mode.Margin = new Padding(0, 3, 10, 3);

            _overwrite.Text = "Ghi mới hoàn toàn (bỏ qua mã hiện có)";
            _overwrite.Dock = DockStyle.Fill;
            _overwrite.AutoSize = false;
            _overwrite.Margin = new Padding(0, 3, 0, 3);
            _includeLibrary.Text = "Đổi cả chi tiết thư viện";
            _includeLibrary.Dock = DockStyle.Fill;
            _includeLibrary.AutoSize = false;
            _includeLibrary.Margin = new Padding(0, 3, 0, 3);

            _previewButton.Text = "Xem trước";
            _previewButton.Size = new Size(105, 32);
            _applyButton.Text = "Thực hiện";
            _applyButton.Size = new Size(120, 32);
            var actions = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                Margin = Padding.Empty,
                Padding = new Padding(0, 1, 0, 0)
            };
            actions.Controls.Add(_applyButton);
            actions.Controls.Add(_previewButton);

            settings.Controls.Add(lblPrefix, 0, 0);
            settings.Controls.Add(_prefix, 1, 0);
            settings.Controls.Add(lblTotal, 2, 0);
            settings.Controls.Add(_total, 3, 0);
            settings.Controls.Add(lblDigits, 4, 0);
            settings.Controls.Add(_digits, 5, 0);
            settings.Controls.Add(lblMode, 0, 1);
            settings.Controls.Add(_mode, 1, 1);
            settings.SetColumnSpan(_mode, 2);
            settings.Controls.Add(_overwrite, 3, 1);
            settings.SetColumnSpan(_overwrite, 4);
            settings.Controls.Add(_includeLibrary, 0, 2);
            settings.SetColumnSpan(_includeLibrary, 4);
            settings.Controls.Add(actions, 4, 2);
            settings.SetColumnSpan(actions, 3);
            top.Controls.Add(settings);

            var structure = new Panel
            {
                Dock = DockStyle.Top,
                Height = 142,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White
            };
            var structureTitle = new Label
            {
                Text = "Cấu trúc mã Part Number",
                Dock = DockStyle.Top,
                Height = 28,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 0, 128)
            };
            _structureSubtitle.Dock = DockStyle.Top;
            _structureSubtitle.Height = 22;
            _structureSubtitle.TextAlign = ContentAlignment.MiddleCenter;
            _structureSubtitle.ForeColor = Color.DimGray;
            _structureBody.Dock = DockStyle.Fill;
            _structureBody.Padding = new Padding(16, 5, 0, 0);
            _structureBody.Font = new Font("Consolas", 9.5F, FontStyle.Bold);
            _structureBody.ForeColor = Color.FromArgb(0, 0, 128);
            structure.Controls.Add(_structureBody);
            structure.Controls.Add(_structureSubtitle);
            structure.Controls.Add(structureTitle);

            _grid.Dock = DockStyle.Fill;
            _grid.ReadOnly = false;
            _grid.AllowUserToAddRows = false;
            _grid.AllowUserToDeleteRows = false;
            _grid.AllowUserToResizeRows = false;
            _grid.RowHeadersVisible = false;
            _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            _grid.BackgroundColor = Color.White;
            _grid.ColumnHeadersHeight = 42;
            _grid.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.True;

            _log.Multiline = true;
            _log.ReadOnly = true;
            _log.ScrollBars = ScrollBars.Vertical;
            _log.Dock = DockStyle.Bottom;
            _log.Height = 112;
            _log.BackColor = Color.White;

            _status.Dock = DockStyle.Bottom;
            _status.Height = 28;
            _status.Padding = new Padding(10, 5, 0, 0);
            _status.Text = "Chưa lưu tự động. Kiểm tra kết quả rồi Save các tài liệu trong Inventor.";

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 5,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 132));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 142));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 112));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            layout.Controls.Add(top, 0, 0);
            layout.Controls.Add(structure, 0, 1);
            layout.Controls.Add(_grid, 0, 2);
            layout.Controls.Add(_log, 0, 3);
            layout.Controls.Add(_status, 0, 4);
            Controls.Add(layout);

            _mode.SelectedIndexChanged += (s, e) => UpdatePreview();
            _prefix.TextChanged += (s, e) => { if (_hasPreview) UpdatePreview(); };
            _digits.ValueChanged += (s, e) => { if (_hasPreview) UpdatePreview(); };
            _overwrite.CheckedChanged += (s, e) => { if (_hasPreview) UpdatePreview(); };
            _includeLibrary.CheckedChanged += (s, e) => { if (_hasPreview) UpdatePreview(); };
            _previewButton.Click += (s, e) =>
            {
                if (!_previewOnly) ScanAssembly();
                UpdatePreview();
                LogPreviewSummary();
            };
            _applyButton.Click += ApplySelectedMode;
            _grid.CurrentCellDirtyStateChanged += (_, _) =>
            {
                if (_grid.IsCurrentCellDirty && _grid.CurrentCell?.ColumnIndex == _grid.Columns.Count - 1)
                    _grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };
            _grid.CellValueChanged += OnSkipChanged;
            _grid.CellValueChanged += OnManualNameChanged;
            _prefix.Text = _previewOnly ? "ZC-ATS-" : "";
            _applyButton.Enabled = true;
            _previewButton.Enabled = true;
            if (_previewOnly)
            {
                AppendLog("Chế độ xem trước: dữ liệu mẫu, không kết nối Inventor và không thực hiện thay đổi.");
            }
        }

        private static string DocumentKey(Document doc)
        {
            var value = string.IsNullOrWhiteSpace(doc.FullDocumentName)
                ? string.IsNullOrWhiteSpace(doc.FullFileName) ? doc.DisplayName : doc.FullFileName
                : doc.FullDocumentName;
            return value.ToUpperInvariant();
        }

        private static string GetPartNumber(Document doc)
        {
            return Convert.ToString(doc.PropertySets["Design Tracking Properties"]["Part Number"].Value)?.Trim()
                ?? string.Empty;
        }

        private static void SetPartNumber(Document doc, string value)
        {
            doc.PropertySets["Design Tracking Properties"]["Part Number"].Value = value;
        }

        private static string GetKind(Document doc, out string kindText)
        {
            kindText = "Không hỗ trợ";
            if (doc.DocumentType == DocumentTypeEnum.kAssemblyDocumentObject)
            {
                kindText = "Assembly";
                return "A";
            }
            if (doc.DocumentType == DocumentTypeEnum.kPartDocumentObject)
            {
                if (string.Equals(doc.SubType, SheetMetalSubtype, StringComparison.OrdinalIgnoreCase))
                {
                    kindText = "Tấm (Sheet Metal)";
                    return "T";
                }
                kindText = "Part";
                return "P";
            }
            return string.Empty;
        }

        private bool IsLibraryDocument(Document doc, object definition)
        {
            if (definition is PartComponentDefinition partDefinition
                && (partDefinition.IsContentMember || partDefinition.IsiPartMember))
                return true;
            if (definition is AssemblyComponentDefinition assemblyDefinition
                && (assemblyDefinition.IsContentMember || assemblyDefinition.IsiAssemblyMember))
                return true;

            var fileName = doc.FullFileName;
            return !string.IsNullOrWhiteSpace(fileName)
                && _libraryPaths.Any(path => fileName.StartsWith(path, StringComparison.OrdinalIgnoreCase));
        }

        private void ScanAssembly()
        {
            _documents.Clear();
            _occurrences.Clear();
            _libraryPaths.Clear();
            var byKey = new Dictionary<string, DocumentRow>(StringComparer.OrdinalIgnoreCase);

            if (_assembly == null) return;
            LoadLibraryPaths();
            var root = MakeDocumentRow((Document)_assembly, _assembly.ComponentDefinition, 0, true);
            _documents.Add(root);
            byKey[root.Key] = root;

            WalkOccurrences(_assembly.ComponentDefinition.Occurrences, 1, byKey);
            var ordered = _documents.OrderBy(row => row.IsTop ? 0 : row.Depth).ToArray();
            _documents.Clear();
            _documents.AddRange(ordered);
            _hasPreview = true;
            AppendLog($"Đã quét {_documents.Count} tài liệu duy nhất và {_occurrences.Count} occurrence.");
        }

        private void LoadLibraryPaths()
        {
            if (_app == null) return;
            try
            {
                var project = _app.DesignProjectManager.ActiveDesignProject;
                var projectDirectory = Path.GetDirectoryName(project.FullFileName) ?? string.Empty;
                var paths = project.LibraryPaths;
                for (var index = 1; index <= paths.Count; index++)
                    AddLibraryPath(paths[index].Path, projectDirectory);
                AddLibraryPath(project.ContentCenterPath, projectDirectory);
            }
            catch (Exception ex)
            {
                AppendLog($"Không đọc được đường dẫn thư viện của project: {Flatten(ex.Message)}");
            }
        }

        private void AddLibraryPath(string? path, string projectDirectory)
        {
            if (string.IsNullOrWhiteSpace(path)) return;
            try
            {
                if (!Path.IsPathRooted(path))
                    path = Path.Combine(projectDirectory, path);
                var normalized = Path.GetFullPath(path);
                if (!normalized.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
                    normalized += Path.DirectorySeparatorChar;
                _libraryPaths.Add(normalized);
            }
            catch (Exception ex)
            {
                AppendLog($"Bỏ qua đường dẫn thư viện không hợp lệ '{path}': {Flatten(ex.Message)}");
            }
        }

        private DocumentRow MakeDocumentRow(Document doc, object definition, int depth, bool isTop)
        {
            string kindText;
            string kind = GetKind(doc, out kindText);
            var row = new DocumentRow
            {
                Document = doc,
                Key = DocumentKey(doc),
                OldPartNumber = string.Empty,
                Kind = kind,
                KindText = kindText,
                Depth = depth,
                IsTop = isTop
            };
            if (!isTop)
            {
                if (kind.Length == 0)
                    row.IgnoreReason = "Không hỗ trợ (bỏ qua)";
                try
                {
                    if (definition is PartComponentDefinition partDefinition
                        && (partDefinition.IsiPartMember || partDefinition.IsiPartFactory)
                        || definition is AssemblyComponentDefinition assemblyDefinition
                        && (assemblyDefinition.IsiAssemblyMember || assemblyDefinition.IsiAssemblyFactory))
                        row.IgnoreReason = "iPart/iAssembly (bỏ qua)";
                    else if (row.IgnoreReason.Length == 0)
                        row.IsLibrary = IsLibraryDocument(doc, definition);
                }
                catch (Exception ex)
                {
                    row.IgnoreReason = "Không kiểm tra được loại tài liệu";
                    AppendLog($"Không kiểm tra được '{doc.DisplayName}': {Flatten(ex.Message)}");
                }
            }
            try
            {
                row.OldPartNumber = GetPartNumber(doc);
            }
            catch (Exception ex)
            {
                row.OldPartNumber = string.Empty;
                AppendLog($"Không đọc được Part Number của '{doc.DisplayName}': {Flatten(ex.Message)}");
            }
            return row;
        }

        private void WalkOccurrences(ComponentOccurrences occurrences, int depth,
            Dictionary<string, DocumentRow> byKey)
        {
            foreach (ComponentOccurrence occurrence in occurrences)
                WalkOccurrence(occurrence, depth, byKey);
        }

        private void WalkOccurrences(ComponentOccurrencesEnumerator occurrences, int depth,
            Dictionary<string, DocumentRow> byKey)
        {
            for (var index = 1; index <= occurrences.Count; index++)
                WalkOccurrence(occurrences[index], depth, byKey);
        }

        private void WalkOccurrence(ComponentOccurrence occurrence, int depth,
            Dictionary<string, DocumentRow> byKey)
        {
            try
            {
                if (occurrence.Suppressed) return;
                if (occurrence.Definition is VirtualComponentDefinition) return;

                Document child = (Document)occurrence.Definition.Document;
                string key = DocumentKey(child);
                string kindText;
                GetKind(child, out kindText);

                _occurrences.Add(new OccurrenceRow
                {
                    Occurrence = occurrence,
                    DocumentKey = key,
                    OldName = occurrence.Name,
                    Depth = depth,
                    KindText = kindText
                });

                if (!byKey.ContainsKey(key))
                {
                    var row = MakeDocumentRow(child, occurrence.Definition, depth, false);
                    byKey[key] = row;
                    row.Skip = _skipKeys.Contains(row.Key);
                    _documents.Add(row);
                }

                if (child.DocumentType == DocumentTypeEnum.kAssemblyDocumentObject)
                    WalkOccurrences(occurrence.SubOccurrences, depth + 1, byKey);
            }
            catch (COMException ex)
            {
                AppendLog($"Không đọc được thành phần assembly: {Flatten(ex.Message)}");
            }
            catch (Exception ex)
            {
                AppendLog($"Không đọc được occurrence: {Flatten(ex.Message)}");
            }
        }

        private void LoadPreviewData()
        {
            var samples = new[]
            {
                new DocumentRow
                {
                    Key = @"C:\Preview\Assembly.iam", Depth = 0, Kind = "A", KindText = "Assembly",
                    OldPartNumber = "ZC-ATS-A00", IsTop = true
                },
                new DocumentRow
                {
                    Key = @"C:\Preview\Subassembly.iam", Depth = 1, Kind = "A", KindText = "Assembly",
                    OldPartNumber = "SUBASSEMBLY-OLD"
                },
                new DocumentRow
                {
                    Key = @"C:\Preview\Frame.ipt", Depth = 1, Kind = "P", KindText = "Part",
                    OldPartNumber = "FRAME-OLD"
                },
                new DocumentRow
                {
                    Key = @"C:\Preview\Cover.ipt", Depth = 1, Kind = "T", KindText = "Tấm (Sheet Metal)",
                    OldPartNumber = "COVER-OLD"
                },
                new DocumentRow
                {
                    Key = @"C:\Preview\Library.ipt", Depth = 1, Kind = "P", KindText = "Part",
                    OldPartNumber = "STD-PART-01", IsLibrary = true
                }
            };
            _documents.AddRange(samples);
            _occurrences.AddRange(new[]
            {
                new OccurrenceRow { DocumentKey = samples[1].Key, OldName = "Cụm gá:1", Depth = 1, KindText = "Assembly" },
                new OccurrenceRow { DocumentKey = samples[2].Key, OldName = "Khung:1", Depth = 1, KindText = "Part" },
                new OccurrenceRow { DocumentKey = samples[2].Key, OldName = "Khung:2", Depth = 1, KindText = "Part" },
                new OccurrenceRow { DocumentKey = samples[3].Key, OldName = "Nắp:1", Depth = 1, KindText = "Tấm (Sheet Metal)" },
                new OccurrenceRow { DocumentKey = samples[4].Key, OldName = "Vít tiêu chuẩn:1", Depth = 1, KindText = "Part" }
            });
            _hasPreview = true;
        }

        private string Compose(string kind, int number)
        {
            return _prefix.Text.Trim() + kind + number.ToString("D" + ((int)_digits.Value).ToString());
        }

        private static string Flatten(string value) =>
            value.Replace('\r', ' ').Replace('\n', ' ');

        private static int ParseExisting(string value, string prefix, string kind, int digits)
        {
            if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(kind)) return -1;
            var match = Regex.Match(value, "^" + Regex.Escape(prefix) + Regex.Escape(kind) + "([0-9]{" + digits + "})$");
            int result;
            return match.Success && int.TryParse(match.Groups[1].Value, out result) ? result : -1;
        }

        private void ComputePartNumbers()
        {
            int digits = (int)_digits.Value;
            int maxValue = (int)Math.Pow(10, digits) - 1;
            var used = new Dictionary<string, HashSet<int>>
            {
                ["A"] = new HashSet<int> { 0 },
                ["P"] = new HashSet<int>(),
                ["T"] = new HashSet<int>()
            };
            foreach (var row in _documents)
            {
                row.NewPartNumber = string.Empty;
                row.Status = string.Empty;
                row.Skip = _skipKeys.Contains(row.Key);
                if (!string.IsNullOrEmpty(row.IgnoreReason))
                    row.Status = row.IgnoreReason;
                else if (row.IsLibrary && !_includeLibrary.Checked)
                    row.Status = "Thư viện (bỏ qua)";
                else if (row.Skip)
                    row.Status = "Bỏ qua (người dùng)";
            }

            foreach (var row in _documents)
            {
                if (string.IsNullOrEmpty(row.Kind)) continue;
                if (row.IsTop)
                {
                    int topNumber = ParseExisting(row.OldPartNumber, _prefix.Text.Trim(), row.Kind, digits);
                    if (topNumber >= 0) used[row.Kind].Add(topNumber);
                    continue;
                }
                if (row.Status.Length == 0) continue;
                int reserved = ParseExisting(row.OldPartNumber, _prefix.Text.Trim(), row.Kind, digits);
                if (reserved >= 0) used[row.Kind].Add(reserved);
            }

            if (!_overwrite.Checked)
            {
                foreach (var row in _documents)
                {
                    if (row.IsTop || row.Status.Length != 0 || string.IsNullOrEmpty(row.Kind)) continue;
                    int n = ParseExisting(row.OldPartNumber, _prefix.Text.Trim(), row.Kind, digits);
                    if (n >= 0 && used[row.Kind].Add(n))
                        row.NewPartNumber = row.OldPartNumber;
                }
            }

            var counters = new Dictionary<string, int> { ["A"] = 0, ["P"] = 0, ["T"] = 0 };
            foreach (var row in _documents)
            {
                if (row.Status.Length != 0 || string.IsNullOrEmpty(row.Kind)) continue;
                if (row.IsTop)
                {
                    row.NewPartNumber = row.OldPartNumber;
                    row.Status = "Gốc (cụm lắp tổng)";
                    continue;
                }
                if (!string.IsNullOrEmpty(row.NewPartNumber)) continue;
                int n = counters[row.Kind];
                do { n++; } while (used[row.Kind].Contains(n));
                counters[row.Kind] = n;
                if (n > maxValue)
                {
                    row.Status = "Vượt giới hạn hậu tố";
                    continue;
                }
                used[row.Kind].Add(n);
                row.NewPartNumber = Compose(row.Kind, n);
            }

            foreach (var row in _documents)
            {
                if (row.Status.Length != 0 || row.IsTop) continue;
                row.Status = string.Equals(row.OldPartNumber, row.NewPartNumber, StringComparison.Ordinal)
                    ? "Giữ nguyên (đã đúng)"
                    : "Sẽ đổi tên";
            }
        }

        private void UpdatePreview()
        {
            UpdateStructure();
            if (_documents.Count == 0)
            {
                _applyButton.Enabled = false;
                return;
            }
            if (!_applied)
                ComputePartNumbers();
            _newPartNumbers = _documents.ToDictionary(x => x.Key, x => x.NewPartNumber,
                StringComparer.OrdinalIgnoreCase);

            _busy = true;
            try
            {
                _grid.Columns.Clear();
                _grid.Rows.Clear();
                bool browserMode = _mode.SelectedIndex == (int)NamingMode.BrowserName;
                if (browserMode)
                {
                    AddColumn("Cấp", 12);
                    AddColumn("Loại", 16);
                    AddColumn("Browser Name hiện tại", 30);
                    AddColumn("Browser Name sau khi áp dụng", 32, false);
                    AddColumn("Part Number nguồn", 20);
                    AddColumn("Trạng thái", 22);
                    AddSkipColumn();
                    foreach (var occurrence in _occurrences)
                    {
                        var docRow = FindDocument(occurrence.DocumentKey);
                        if (docRow == null) continue;
                        string next = occurrence.OldName;
                        var manualKey = GetOccurrenceManualKey(occurrence);
                        if (_manualNames.TryGetValue(manualKey, out var manualName)
                            && !string.IsNullOrWhiteSpace(manualName))
                        {
                            next = manualName;
                        }
                        else if (_newPartNumbers.TryGetValue(occurrence.DocumentKey, out var pn)
                            && !string.IsNullOrWhiteSpace(pn))
                        {
                            string suffix = GetOccurrenceSuffix(occurrence.OldName);
                            next = suffix.Length == 0 ? pn : pn + ":" + suffix;
                        }
                        var status = GetOccurrenceStatus(occurrence, docRow);
                        int i = _grid.Rows.Add("Cấp " + occurrence.Depth, occurrence.KindText,
                            occurrence.OldName, next, docRow.OldPartNumber, status, docRow.Skip);
                        _grid.Rows[i].Tag = occurrence;
                        ColorRow(_grid.Rows[i], status);
                        _grid.Rows[i].Cells[6].ReadOnly = IsSkipReadOnly(docRow);
                    }
                    _status.Text = "Preview Browser Name: " + _occurrences.Count + " occurrence. Chỉ đổi nhãn trong Assembly; không đổi iProperty hoặc file.";
                }
                else
                {
                    AddColumn("Cấp lắp ráp", 14);
                    AddColumn("Loại", 18);
                    AddColumn("Part Number hiện tại", 28);
                    AddColumn("Part Number sau khi áp dụng", 30, false);
                    AddColumn("Trạng thái", 22);
                    AddSkipColumn();
                    foreach (var row in _documents)
                    {
                        string state = row.IsTop ? "Gốc (cụm lắp tổng)" : GetStatus(row);
                        var target = _manualNames.TryGetValue(row.Key, out var manualName)
                            && !string.IsNullOrWhiteSpace(manualName)
                                ? manualName
                                : row.NewPartNumber;
                        int i = _grid.Rows.Add(row.IsTop ? "Gốc" : "Cấp " + row.Depth,
                            row.KindText, row.OldPartNumber, target, state, row.Skip);
                        _grid.Rows[i].Tag = row;
                        ColorRow(_grid.Rows[i], state);
                        _grid.Rows[i].Cells[5].ReadOnly = IsSkipReadOnly(row);
                    }
                    _status.Text = "Preview Part Number: " + _documents.Count(x => !x.IsTop) + " tài liệu duy nhất. Giữ nguyên Browser Name và tên file.";
                }
            }
            finally
            {
                _busy = false;
            }
            _total.Text = _documents.Count(row => !row.IsTop
                && string.IsNullOrEmpty(row.IgnoreReason)
                && (!row.IsLibrary || _includeLibrary.Checked)).ToString();
            _applyButton.Enabled = !_applied
                && !string.IsNullOrWhiteSpace(_prefix.Text)
                && HasChanges()
                && !_documents.Any(row => GetStatus(row) == "Vượt giới hạn hậu tố");
        }

        private DocumentRow? FindDocument(string key) =>
            _documents.FirstOrDefault(row => string.Equals(row.Key, key, StringComparison.OrdinalIgnoreCase));

        private bool IsSkipReadOnly(DocumentRow row) =>
            _applied || row.IsTop || !string.IsNullOrEmpty(row.IgnoreReason)
            || row.IsLibrary && !_includeLibrary.Checked;

        private static string GetStatus(DocumentRow row) =>
            row.Status.Length == 0 ? "—" : row.Status;

        private string GetOccurrenceStatus(OccurrenceRow occurrence, DocumentRow row)
        {
            if (occurrence.Status.Length != 0)
                return occurrence.Status;
            if (row.Status.StartsWith("Lỗi", StringComparison.Ordinal) || _applied && row.Status == "Đã đổi")
                return row.Status;
            if (row.Status is "Thư viện (bỏ qua)" or "Bỏ qua (người dùng)" or "iPart/iAssembly (bỏ qua)"
                or "Không hỗ trợ (bỏ qua)" or "Không kiểm tra được loại")
                return row.Status;
            var manualKey = GetOccurrenceManualKey(occurrence);
            if (_manualNames.TryGetValue(manualKey, out var manualName)
                && !string.IsNullOrWhiteSpace(manualName))
            {
                return string.Equals(occurrence.OldName, manualName, StringComparison.Ordinal)
                    ? "Giữ nguyên (đã đúng)"
                    : "Sẽ đổi tên";
            }
            if (string.IsNullOrWhiteSpace(row.NewPartNumber))
                return GetStatus(row);
            var suffix = GetOccurrenceSuffix(occurrence.OldName);
            var target = suffix.Length == 0 ? row.NewPartNumber : row.NewPartNumber + ":" + suffix;
            return string.Equals(occurrence.OldName, target, StringComparison.Ordinal)
                ? "Giữ nguyên (đã đúng)"
                : "Sẽ đổi tên";
        }

        private bool HasChanges()
        {
            if (_mode.SelectedIndex == (int)NamingMode.BrowserName)
                return _occurrences.Any(occurrence =>
                {
                    var row = FindDocument(occurrence.DocumentKey);
                    return row != null && !row.IsTop && !row.Skip
                        && string.IsNullOrEmpty(row.IgnoreReason)
                        && (!row.IsLibrary || _includeLibrary.Checked)
                        && (_manualNames.ContainsKey(GetOccurrenceManualKey(occurrence))
                            || GetOccurrenceStatus(occurrence, row) == "Sẽ đổi tên");
                });
            return _documents.Any(row => !row.IsTop
                && (_manualNames.TryGetValue(row.Key, out var manualName)
                    ? !string.Equals(row.OldPartNumber, manualName, StringComparison.Ordinal)
                    : GetStatus(row) == "Sẽ đổi tên"));
        }

        private static void ColorRow(DataGridViewRow row, string status)
        {
            var color = status switch
            {
                "Sẽ đổi tên" => Color.LightGreen,
                "Đã đổi" => Color.FromArgb(150, 200, 255),
                "Giữ nguyên (đã đúng)" => Color.FromArgb(255, 255, 190),
                _ when status.StartsWith("Lỗi", StringComparison.Ordinal) => Color.FromArgb(255, 150, 150),
                _ => Color.FromArgb(220, 220, 220)
            };
            foreach (DataGridViewCell cell in row.Cells)
                cell.Style.BackColor = color;
        }

        private void AddColumn(string title, float weight)
        {
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = title,
                FillWeight = weight,
                SortMode = DataGridViewColumnSortMode.NotSortable
            });
        }

        private void AddColumn(string title, float weight, bool readOnly)
        {
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = title,
                FillWeight = weight,
                SortMode = DataGridViewColumnSortMode.NotSortable,
                ReadOnly = readOnly
            });
        }

        private void AddSkipColumn()
        {
            _grid.Columns.Add(new DataGridViewCheckBoxColumn
            {
                HeaderText = "Bỏ qua",
                FillWeight = 8,
                SortMode = DataGridViewColumnSortMode.NotSortable
            });
        }

        private void OnSkipChanged(object? sender, DataGridViewCellEventArgs e)
        {
            if (_busy || e.RowIndex < 0 || e.ColumnIndex != _grid.Columns.Count - 1) return;
            var gridRow = _grid.Rows[e.RowIndex];
            var documentRow = gridRow.Tag is OccurrenceRow occurrence
                ? FindDocument(occurrence.DocumentKey)
                : gridRow.Tag as DocumentRow;
            if (documentRow == null || IsSkipReadOnly(documentRow)) return;

            documentRow.Skip = Convert.ToBoolean(gridRow.Cells[e.ColumnIndex].Value);
            if (documentRow.Skip)
                _skipKeys.Add(documentRow.Key);
            else
                _skipKeys.Remove(documentRow.Key);
            UpdatePreview();
        }

        private void OnManualNameChanged(object? sender, DataGridViewCellEventArgs e)
        {
            if (_busy || e.RowIndex < 0 || e.ColumnIndex != 3) return;
            var gridRow = _grid.Rows[e.RowIndex];
            var value = Convert.ToString(gridRow.Cells[e.ColumnIndex].Value)?.Trim() ?? string.Empty;
            if (gridRow.Tag is OccurrenceRow occurrence)
                _manualNames[GetOccurrenceManualKey(occurrence)] = value;
            else if (gridRow.Tag is DocumentRow documentRow)
            {
                _manualNames[documentRow.Key] = value;
                documentRow.NewPartNumber = value;
            }
            UpdatePreview();
        }

        private static string GetOccurrenceManualKey(OccurrenceRow occurrence) =>
            occurrence.DocumentKey + "|" + occurrence.Depth + "|" + occurrence.OldName;

        private void UpdateStructure()
        {
            var prefix = _prefix.Text.Trim();
            var digits = (int)_digits.Value;
            _structureSubtitle.Text = $"Prefix + [A|P|T] + Hậu tố ({digits} ký tự)";
            _structureBody.Text =
                $"Cụm lắp tổng (đang mở, mã cố định): {Compose("A", 0)}{Environment.NewLine}" +
                $"Cụm lắp con (A, tăng dần từ 1): {Compose("A", 1)}, {Compose("A", 2)}, ...{Environment.NewLine}" +
                $"Chi tiết thường (P, tăng dần từ 1): {Compose("P", 1)}, {Compose("P", 2)}, ...{Environment.NewLine}" +
                $"Chi tiết dạng tấm (T, tăng dần từ 1): {Compose("T", 1)}, {Compose("T", 2)}, ...";
            if (_hasPreview)
                _status.Text = string.IsNullOrWhiteSpace(prefix)
                    ? "Nhập Prefix để xem sơ đồ mã."
                    : "Chưa lưu tự động. Kiểm tra kết quả rồi Save các tài liệu trong Inventor.";
        }

        private void AppendLog(string message)
        {
            if (_log.IsDisposed) return;
            _log.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
        }

        private void LogPreviewSummary()
        {
            AppendLog($"Tổng số chi tiết/cụm lắp cần đánh số: {_total.Text}");
            AppendLog($"Sơ đồ mã: Prefix + [A|P|T] + hậu tố ({(int)_digits.Value} ký tự); cụm lắp tổng: {Compose("A", 0)}.");
            var changes = _mode.SelectedIndex == (int)NamingMode.BrowserName
                ? _occurrences.Count(occurrence =>
                {
                    var row = FindDocument(occurrence.DocumentKey);
                    return row != null && GetOccurrenceStatus(occurrence, row) == "Sẽ đổi tên";
                })
                : _documents.Count(row => !row.IsTop && GetStatus(row) == "Sẽ đổi tên");
            var keeps = _mode.SelectedIndex == (int)NamingMode.BrowserName
                ? _occurrences.Count(occurrence =>
                {
                    var row = FindDocument(occurrence.DocumentKey);
                    return row != null && GetOccurrenceStatus(occurrence, row) == "Giữ nguyên (đã đúng)";
                })
                : _documents.Count(row => !row.IsTop && GetStatus(row) == "Giữ nguyên (đã đúng)");
            AppendLog($"Sẽ đổi: {changes} | " +
                $"Giữ nguyên: {keeps} | " +
                $"Thư viện bỏ qua: {_documents.Count(row => GetStatus(row) == "Thư viện (bỏ qua)")} | " +
                $"Bỏ qua người dùng: {_documents.Count(row => GetStatus(row) == "Bỏ qua (người dùng)")} | " +
                $"Vượt giới hạn hậu tố: {_documents.Count(row => GetStatus(row) == "Vượt giới hạn hậu tố")}.");
            AppendLog("================================");
        }

        private static string GetOccurrenceSuffix(string name)
        {
            if (string.IsNullOrEmpty(name)) return string.Empty;
            Match m = Regex.Match(name, ":([0-9]+)$");
            return m.Success ? m.Groups[1].Value : string.Empty;
        }

        private void ApplySelectedMode(object? sender, EventArgs e)
        {
            if (!_hasPreview || string.IsNullOrWhiteSpace(_prefix.Text))
            {
                MessageBox.Show("Nhập Prefix và bấm Xem trước trước khi thực hiện.", Text,
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            bool browserMode = _mode.SelectedIndex == (int)NamingMode.BrowserName;
            if (_previewOnly)
            {
                if (MessageBox.Show("Chế độ xem trước: thực hiện mô phỏng, không thay đổi Inventor hay file.\r\n\r\nTiếp tục?",
                        Text, MessageBoxButtons.YesNo, MessageBoxIcon.Information,
                        MessageBoxDefaultButton.Button1) != DialogResult.Yes)
                    return;
                _applied = true;
                UpdatePreview();
                _prefix.Enabled = false;
                _digits.Enabled = false;
                _mode.Enabled = false;
                _overwrite.Enabled = false;
                _includeLibrary.Enabled = false;
                _previewButton.Enabled = false;
                _applyButton.Enabled = false;
                AppendLog("Đã mô phỏng thực hiện trong dữ liệu xem trước. Không có tài liệu hoặc file nào bị thay đổi.");
                _status.Text = "Đã mô phỏng thực hiện. Không thay đổi Inventor hoặc file.";
                return;
            }
            string action = browserMode
                ? "Đổi Browser Name của các occurrence không bị bỏ qua theo Part Number xem trước?\r\nPart Number iProperty và tên file sẽ không đổi."
                : "Ghi Part Number iProperty theo bảng xem trước?\r\nBrowser Name và tên file sẽ không đổi.";
            if (MessageBox.Show(action + "\r\n\r\nKhông tự lưu tài liệu. Tiếp tục?", Text,
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;

            if (!browserMode)
            {
                try
                {
                    var backupPath = WriteBackup();
                    AppendLog($"Đã sao lưu Part Number trước khi ghi: {backupPath}");
                }
                catch (Exception ex)
                {
                    if (MessageBox.Show(
                            $"Không ghi được file sao lưu Part Number: {Flatten(ex.Message)}\r\n\r\nVẫn tiếp tục?",
                            Text, MessageBoxButtons.YesNo, MessageBoxIcon.Warning,
                            MessageBoxDefaultButton.Button2) != DialogResult.Yes)
                        return;
                    AppendLog($"CẢNH BÁO: không ghi được file sao lưu: {Flatten(ex.Message)}");
                }
            }

            int changed = 0;
            var errors = new List<string>();
            if (browserMode)
            {
                foreach (var occurrence in _occurrences)
                {
                    var row = FindDocument(occurrence.DocumentKey);
                    if (row == null || row.IsTop || row.Skip || row.IgnoreReason.Length != 0
                        || row.IsLibrary && !_includeLibrary.Checked
                        || GetOccurrenceStatus(occurrence, row) != "Sẽ đổi tên")
                        continue;
                    try
                    {
                        if (occurrence.Occurrence == null)
                            throw new InvalidOperationException("Occurrence mẫu không thể được sửa.");
                        var manualKey = GetOccurrenceManualKey(occurrence);
                        string target;
                        if (_manualNames.TryGetValue(manualKey, out var manualName)
                            && !string.IsNullOrWhiteSpace(manualName))
                        {
                            target = manualName;
                        }
                        else
                        {
                            if (!_newPartNumbers.TryGetValue(occurrence.DocumentKey, out var pn)
                                || string.IsNullOrWhiteSpace(pn))
                                continue;
                            string suffix = GetOccurrenceSuffix(occurrence.OldName);
                            target = suffix.Length == 0 ? pn : pn + ":" + suffix;
                        }
                        if (!string.Equals(occurrence.Occurrence.Name, target, StringComparison.Ordinal))
                        {
                            occurrence.Occurrence.Name = target;
                            occurrence.OldName = target;
                            changed++;
                        }
                        occurrence.Status = "Đã đổi";
                    }
                    catch (Exception ex)
                    {
                        occurrence.Status = "Lỗi: " + Flatten(ex.Message);
                        errors.Add(occurrence.OldName + ": " + Flatten(ex.Message));
                    }
                }
            }
            else
            {
                foreach (var row in _documents)
                {
                    var target = _manualNames.TryGetValue(row.Key, out var manualName)
                        && !string.IsNullOrWhiteSpace(manualName)
                            ? manualName
                            : row.NewPartNumber;
                    if (row.IsTop || row.Skip || row.IgnoreReason.Length != 0
                        || row.IsLibrary && !_includeLibrary.Checked
                        || string.IsNullOrEmpty(target)
                        || row.OldPartNumber == target)
                        continue;
                    try
                    {
                        if (row.Document == null)
                            throw new InvalidOperationException("Tài liệu mẫu không thể được sửa.");
                        SetPartNumber(row.Document, target);
                        if (!string.Equals(GetPartNumber(row.Document), target, StringComparison.Ordinal))
                            throw new InvalidOperationException("Kiểm tra lại iProperty không khớp sau khi ghi.");
                        changed++;
                        row.Status = "Đã đổi";
                        AppendLog($"{row.Document.DisplayName}: {row.OldPartNumber} -> {target}");
                    }
                    catch (Exception ex)
                    {
                        row.Status = "Lỗi: " + Flatten(ex.Message);
                        errors.Add($"{row.Document?.DisplayName ?? row.Key}: {Flatten(ex.Message)}");
                    }
                }
            }

            string summary = "Đã cập nhật " + changed + (browserMode ? " Browser Name occurrence." : " iProperty Part Number.") +
                "\r\nCác thay đổi chưa được lưu.";
            if (errors.Count > 0) summary += "\r\n\r\nLỗi (" + errors.Count + "): " + string.Join("\r\n", errors.Take(10));
            MessageBox.Show(summary, Text, MessageBoxButtons.OK,
                errors.Count == 0 ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
            _applied = true;
            _prefix.Enabled = false;
            _digits.Enabled = false;
            _mode.Enabled = false;
            _overwrite.Enabled = false;
            _includeLibrary.Enabled = false;
            _previewButton.Enabled = false;
            UpdatePreview();
            _applyButton.Enabled = false;
            AppendLog($"Đã cập nhật: {changed} | Lỗi: {errors.Count}. Các tài liệu chưa được lưu.");
        }

        private string WriteBackup()
        {
            var rows = new List<string> { "File,PartNumber_Cu,PartNumber_Moi,TrangThai" };
            foreach (var row in _documents.Where(row => !row.IsTop && GetStatus(row) == "Sẽ đổi tên"))
            {
                rows.Add($"{Csv(row.Document?.FullFileName ?? row.Key)},{Csv(row.OldPartNumber)},{Csv(row.NewPartNumber)},{Csv(GetStatus(row))}");
            }

            var rootFileName = _assembly?.FullFileName;
            var baseName = string.IsNullOrWhiteSpace(rootFileName)
                ? "Assembly"
                : Path.GetFileNameWithoutExtension(rootFileName);
            var fileName = $"{baseName}_PartNumber_backup_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            var directories = new[]
            {
                string.IsNullOrWhiteSpace(rootFileName) ? null : Path.GetDirectoryName(rootFileName),
                Path.GetTempPath()
            };
            Exception? lastError = null;
            foreach (var directory in directories.Where(path => !string.IsNullOrWhiteSpace(path)))
            {
                try
                {
                    var path = Path.Combine(directory!, fileName);
                    File.WriteAllText(path, string.Join(Environment.NewLine, rows), new UTF8Encoding(true));
                    return path;
                }
                catch (Exception ex)
                {
                    lastError = ex;
                }
            }
            throw new IOException("Không ghi được file sao lưu vào thư mục assembly hoặc thư mục tạm.", lastError);
        }

        private static string Csv(string? value) =>
            $"\"{(value ?? string.Empty).Replace("\"", "\"\"")}\"";
    }
}
