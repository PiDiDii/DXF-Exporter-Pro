using Inventor;
using InventorDxfExporter.Core;
using InventorDxfExporter.UI.Forms;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using InventorApplication = Inventor.Application;

namespace InventorDxfExporter.Commands;

/// <summary>Creates task-oriented DXF Exporter controls with paired standard/large icons.</summary>
public sealed class RibbonCommandSet
{
    private const string ClientId = "{6B93E5C1-9A6A-4D1B-8FA9-47567B4A1D20}";
    private readonly InventorApplication _application;
    private readonly Dictionary<ButtonDefinitionSink_Event, ButtonDefinitionSink_OnExecuteEventHandler> _subscriptions = new();
    private readonly List<ButtonDefinition> _definitions = new();
    private UserInterfaceEventsSink_Event? _userInterfaceEvents;
    private UserInterfaceEventsSink_OnResetRibbonInterfaceEventHandler? _resetRibbonHandler;
    private static int _isExecuting;
    private static readonly Dictionary<IconKind, IconPair> IconCache = new();

    public RibbonCommandSet(InventorApplication application) =>
        _application = application ?? throw new ArgumentNullException(nameof(application));

    public void Activate(bool firstTime)
    {
        RemoveRetiredRibbonTabs();

        var convert = new ConvertStepCommand(_application);
        var export = new ExportDxfCommand(_application);
        var bom = new ExportBomCommand(_application);
        var changeKFactor = new RunILogicRuleCommand(_application, "Doi_K_Factor_Assembly.vb");

        var convertButton = Define("Convert STEP", "ConvertStep",
            "Chuyển các part STEP trong Assembly thành Sheet Metal và tạo Flat Pattern.", "Convert STEP", IconKind.Convert,
            () =>
            {
                try { convert.Execute(); }
                finally { RestoreRibbonTabs(); }
            });
        var exportButton = Define("Xuất DXF", "ExportDxf",
            "Kiểm tra, xem trước và xuất DXF cho part Sheet Metal trong tài liệu hiện hành.", "Xuất DXF", IconKind.Dxf, export.Execute);
        var bomButton = Define("Xuất BOM", "ExportBom",
            "Tạo bảng BOM Excel (.xlsx) bằng ClosedXML; không yêu cầu cài Microsoft Excel.", "Xuất BOM", IconKind.Bom, bom.Execute);
        var kFactorButton = Define("Đổi hệ số K", "ChangeKFactor",
            "Đổi hệ số K cho các chi tiết Sheet Metal được chọn trong Assembly.", "Đổi hệ số K", IconKind.KFactor, changeKFactor.Execute);
        var partNumberButton = Define("Đổi Part Number", "ChangePartNumber",
            "Đổi Part Number hoặc Browser Name theo cấp lắp ráp; xem trước trước khi áp dụng.",
            "Đổi Part Number", IconKind.PartNumber, () => InventorPartNumberBrowserRenamer.Show(_application));

        _userInterfaceEvents = (UserInterfaceEventsSink_Event)_application.UserInterfaceManager.UserInterfaceEvents;
        _resetRibbonHandler = OnResetRibbonInterface;
        _userInterfaceEvents.OnResetRibbonInterface += _resetRibbonHandler;

        if (firstTime || !HasCurrentRibbonTabs())
        {
            Trace.TraceInformation("Khởi tạo các Ribbon tab DXF Exporter còn thiếu.");
            EnsureRibbonTabs(convertButton, exportButton, bomButton, kFactorButton, partNumberButton);
        }
    }

    private bool HasCurrentRibbonTabs() =>
        HasRibbonTab("Assembly", "InventorDxfExporter.Assembly.V5.Tab")
        && HasRibbonTab("Part", "InventorDxfExporter.Part.V5.Tab");

    private bool HasRibbonTab(string ribbonName, string tabInternalName)
    {
        try
        {
            var tabs = _application.UserInterfaceManager.Ribbons[ribbonName].RibbonTabs;
            for (var index = 1; index <= tabs.Count; index++)
            {
                if (string.Equals(tabs[index].InternalName, tabInternalName, StringComparison.Ordinal))
                    return true;
            }
        }
        catch (System.Runtime.InteropServices.COMException ex)
        {
            Trace.TraceWarning($"Không thể kiểm tra Ribbon '{ribbonName}': {ex.Message}");
        }
        return false;
    }

    private void EnsureRibbonTabs(ButtonDefinition convertButton, ButtonDefinition exportButton,
        ButtonDefinition bomButton, ButtonDefinition kFactorButton, ButtonDefinition partNumberButton)
    {
        AddTab("Assembly", "DXF Exporter", "InventorDxfExporter.Assembly.V5.Tab",
            new PanelLayout("Thao tác", "InventorDxfExporter.Assembly.V5.CommandsPanel", new[]
            {
                new ButtonLayout(convertButton), new ButtonLayout(exportButton), new ButtonLayout(bomButton),
                new ButtonLayout(kFactorButton), new ButtonLayout(partNumberButton)
            }));

        AddTab("Part", "DXF Exporter", "InventorDxfExporter.Part.V5.Tab",
            new PanelLayout("Xuất", "InventorDxfExporter.Part.V5.ExportPanel", new[]
            {
                new ButtonLayout(exportButton), new ButtonLayout(bomButton)
            }));
    }

    private void OnResetRibbonInterface(NameValueMap context)
        => RestoreRibbonTabs();

    private void RestoreRibbonTabs()
    {
        try
        {
            if (_definitions.Count < 5)
                return;
            EnsureRibbonTabs(_definitions[0], _definitions[1], _definitions[2], _definitions[3], _definitions[4]);
        }
        catch (Exception ex)
        {
            Trace.TraceError($"Không thể khôi phục Ribbon DXF Exporter: {ex}");
        }
    }

    private void RemoveRetiredRibbonTabs()
    {
        var retiredTabNames = new HashSet<string>(StringComparer.Ordinal)
        {
            "InventorDxfExporter.Assembly.V1.UtilitiesTab",
            "InventorDxfExporter.Part.V1.UtilitiesTab",
            "InventorDxfExporter.Assembly.V3.Tab",
            "InventorDxfExporter.Part.V3.Tab",
            "InventorDxfExporter.Assembly.V4.Tab",
            "InventorDxfExporter.Part.V4.Tab"
        };
        foreach (var ribbonName in new[] { "Assembly", "Part" })
        {
            try
            {
                var tabs = _application.UserInterfaceManager.Ribbons[ribbonName].RibbonTabs;
                for (var index = tabs.Count; index >= 1; index--)
                {
                    var tab = tabs[index];
                    if (!retiredTabNames.Contains(tab.InternalName))
                        continue;

                    try
                    {
                        var internalName = tab.InternalName;
                        tab.Delete();
                        Trace.TraceInformation($"Đã xóa Ribbon tab cũ '{internalName}'.");
                    }
                    catch (System.Runtime.InteropServices.COMException ex)
                    {
                        Trace.TraceWarning($"Không thể xóa Ribbon tab cũ '{tab.InternalName}': {ex.Message}");
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException ex)
            {
                Trace.TraceWarning($"Không thể kiểm tra Ribbon '{ribbonName}' để dọn tab đã bỏ: {ex.Message}");
            }
        }
    }

    private ButtonDefinition Define(string displayName, string suffix, string description, string toolTip, IconKind iconKind, Action execute)
    {
        var icon = GetIcons(iconKind);
        var button = _application.CommandManager.ControlDefinitions.AddButtonDefinition(
            displayName, $"InventorDxfExporter.{suffix}", CommandTypesEnum.kNonShapeEditCmdType,
            ClientId, description, toolTip, icon.StandardPicture, icon.LargePicture, ButtonDisplayEnum.kAlwaysDisplayText);
        var sink = (ButtonDefinitionSink_Event)button;
        ButtonDefinitionSink_OnExecuteEventHandler handler = _ =>
        {
            if (System.Threading.Interlocked.CompareExchange(ref _isExecuting, 1, 0) != 0)
            {
                Trace.TraceWarning($"Bỏ qua lệnh '{displayName}' vì một thao tác DXF Exporter khác đang chạy.");
                return;
            }
            try { execute(); }
            catch (Exception ex)
            {
                Trace.TraceError($"{displayName}: {ex}");
                MessageBox.Show(new InventorWindowOwner(_application.MainFrameHWND),
                    ex.Message, displayName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally { System.Threading.Interlocked.Exchange(ref _isExecuting, 0); }
        };
        sink.OnExecute += handler;
        _subscriptions.Add(sink, handler);
        _definitions.Add(button);
        return button;
    }

    private void AddTab(string ribbonName, string tabName, string tabInternalName, params PanelLayout[] panels)
    {
        var ribbon = _application.UserInterfaceManager.Ribbons[ribbonName];
        try
        {
            for (var index = 1; index <= ribbon.RibbonTabs.Count; index++)
            {
                if (string.Equals(ribbon.RibbonTabs[index].InternalName, tabInternalName, StringComparison.Ordinal))
                    return;
            }
            var tab = ribbon.RibbonTabs.Add(tabName, tabInternalName, ClientId, string.Empty, false, false);
            foreach (var panelLayout in panels)
            {
                var panel = tab.RibbonPanels.Add(panelLayout.Caption, panelLayout.InternalName, ClientId, string.Empty, false);
                foreach (var button in panelLayout.Buttons)
                    // Large-icon controls with text enabled place each caption below its icon.
                    panel.CommandControls.AddButton(button.Definition, true, true, string.Empty, false);
            }
        }
        catch (System.Runtime.InteropServices.COMException ex)
        {
            Trace.TraceError($"Không thể tạo Ribbon tab '{tabInternalName}': {ex}");
            throw;
        }
    }

    private static IconPair GetIcons(IconKind kind)
    {
        if (IconCache.TryGetValue(kind, out var cached)) return cached;
        var standard = DrawIcon(kind, 16);
        var large = DrawIcon(kind, 32);
        cached = new IconPair(standard, large,
            PictureDispBridge.FromImage(standard), PictureDispBridge.FromImage(large));
        IconCache.Add(kind, cached);
        return cached;
    }

    private static System.Drawing.Bitmap DrawIcon(IconKind kind, int size)
    {
        var bitmap = new System.Drawing.Bitmap(size, size, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using var graphics = System.Drawing.Graphics.FromImage(bitmap);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.Clear(System.Drawing.Color.Transparent);
        graphics.ScaleTransform(size / 64f, size / 64f);

        using var blue = new System.Drawing.Pen(System.Drawing.Color.FromArgb(35, 105, 175), 5) { LineJoin = LineJoin.Round, StartCap = LineCap.Round, EndCap = LineCap.Round };
        using var dark = new System.Drawing.Pen(System.Drawing.Color.FromArgb(55, 68, 82), 4) { LineJoin = LineJoin.Round, StartCap = LineCap.Round, EndCap = LineCap.Round };
        using var accent = new System.Drawing.Pen(System.Drawing.Color.FromArgb(0, 150, 136), 5) { LineJoin = LineJoin.Round, StartCap = LineCap.Round, EndCap = LineCap.Round };
        using var blueFill = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(35, 105, 175));
        using var paleFill = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(222, 238, 250));
        using var whiteFill = new System.Drawing.SolidBrush(System.Drawing.Color.White);

        switch (kind)
        {
            case IconKind.Convert:
                DrawDocumentFrame(graphics, blue, paleFill, 8, 7, 31, 41);
                DrawDocumentFrame(graphics, dark, whiteFill, 25, 16, 31, 41);
                graphics.DrawLine(accent, 15, 49, 47, 49);
                graphics.DrawLine(accent, 39, 42, 47, 49);
                graphics.DrawLine(accent, 39, 56, 47, 49);
                break;
            case IconKind.Dxf:
                DrawDocumentFrame(graphics, blue, paleFill, 14, 7, 38, 50);
                graphics.DrawLine(dark, 22, 43, 31, 33);
                graphics.DrawLine(dark, 31, 33, 38, 39);
                graphics.DrawLine(dark, 38, 39, 46, 25);
                FillDot(graphics, blueFill, 19, 40, 7);
                FillDot(graphics, blueFill, 28, 30, 7);
                FillDot(graphics, blueFill, 35, 36, 7);
                FillDot(graphics, blueFill, 43, 22, 7);
                break;
            case IconKind.Bom:
                using (var grid = new System.Drawing.Pen(System.Drawing.Color.FromArgb(55, 68, 82), 3))
                {
                    graphics.DrawRoundedRectangle(blue, 8, 10, 48, 44, 5);
                    graphics.DrawLine(accent, 9, 22, 55, 22);
                    graphics.DrawLine(grid, 9, 33, 55, 33);
                    graphics.DrawLine(grid, 9, 43, 55, 43);
                    graphics.DrawLine(grid, 24, 11, 24, 53);
                    graphics.DrawLine(grid, 40, 11, 40, 53);
                }
                break;
            case IconKind.KFactor:
                graphics.DrawEllipse(accent, 13, 9, 38, 38);
                using (var font = new System.Drawing.Font("Segoe UI", 25, System.Drawing.FontStyle.Bold))
                    graphics.DrawString("K", font, blueFill, 19, 14);
                graphics.DrawLine(dark, 17, 53, 47, 53);
                break;
            case IconKind.PartNumber:
                DrawDocumentFrame(graphics, blue, paleFill, 10, 7, 44, 50);
                using (var font = new System.Drawing.Font("Segoe UI", 15, System.Drawing.FontStyle.Bold))
                    graphics.DrawString("PN", font, blueFill, 15, 23);
                graphics.DrawLine(accent, 17, 44, 47, 44);
                break;
        }
        return bitmap;
    }

    private static void DrawDocumentFrame(System.Drawing.Graphics graphics, System.Drawing.Pen outline,
        System.Drawing.Brush fill, float x, float y, float width, float height)
    {
        using var path = new System.Drawing.Drawing2D.GraphicsPath();
        path.AddPolygon(new[]
        {
            new System.Drawing.PointF(x, y), new System.Drawing.PointF(x + width - 10, y),
            new System.Drawing.PointF(x + width, y + 10), new System.Drawing.PointF(x + width, y + height),
            new System.Drawing.PointF(x, y + height)
        });
        graphics.FillPath(fill, path);
        graphics.DrawPath(outline, path);
        graphics.DrawLine(outline, x + width - 10, y + 1, x + width - 10, y + 10);
        graphics.DrawLine(outline, x + width - 10, y + 10, x + width - 1, y + 10);
    }

    private static void FillDot(System.Drawing.Graphics graphics, System.Drawing.Brush brush, float x, float y, float diameter) =>
        graphics.FillEllipse(brush, x, y, diameter, diameter);

    public void Deactivate()
    {
        if (_userInterfaceEvents is not null && _resetRibbonHandler is not null)
        {
            try { _userInterfaceEvents.OnResetRibbonInterface -= _resetRibbonHandler; }
            catch (Exception ex) { Trace.TraceWarning($"Không thể gỡ sự kiện khôi phục Ribbon: {ex.Message}"); }
        }
        _userInterfaceEvents = null;
        _resetRibbonHandler = null;
        foreach (var pair in _subscriptions)
        {
            try { pair.Key.OnExecute -= pair.Value; }
            catch (Exception ex) { Trace.TraceWarning($"Không thể gỡ command handler: {ex.Message}"); }
        }
        _subscriptions.Clear();
        _definitions.Clear();
    }

    private enum IconKind { Convert, Dxf, Bom, KFactor, PartNumber }
    private sealed record PanelLayout(string Caption, string InternalName, ButtonLayout[] Buttons);
    private sealed record ButtonLayout(ButtonDefinition Definition);
    private sealed record IconPair(System.Drawing.Bitmap StandardBitmap, System.Drawing.Bitmap LargeBitmap, object StandardPicture, object LargePicture);

}

internal static class GraphicsRoundedRectangleExtensions
{
    public static void DrawRoundedRectangle(this System.Drawing.Graphics graphics, System.Drawing.Pen pen,
        float x, float y, float width, float height, float radius)
    {
        using var path = new System.Drawing.Drawing2D.GraphicsPath();
        float diameter = radius * 2;
        path.AddArc(x, y, diameter, diameter, 180, 90);
        path.AddArc(x + width - diameter, y, diameter, diameter, 270, 90);
        path.AddArc(x + width - diameter, y + height - diameter, diameter, diameter, 0, 90);
        path.AddArc(x, y + height - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        graphics.DrawPath(pen, path);
    }
}
