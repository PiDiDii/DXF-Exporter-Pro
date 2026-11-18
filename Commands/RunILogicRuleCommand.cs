using Autodesk.iLogic.Interfaces;
using Inventor;
using InventorDxfExporter.Core;
using InventorDxfExporter.UI.Forms;
using System.IO;
using System.Windows.Forms;
using InventorApplication = Inventor.Application;
using IOFile = System.IO.File;
using IOPath = System.IO.Path;

namespace InventorDxfExporter.Commands;

public sealed class RunILogicRuleCommand
{
    private readonly InventorApplication _application;
    private readonly string _ruleFileName;

    public RunILogicRuleCommand(InventorApplication application, string ruleFileName)
    {
        _application = application ?? throw new ArgumentNullException(nameof(application));
        _ruleFileName = ruleFileName ?? throw new ArgumentNullException(nameof(ruleFileName));
    }

    public void Execute()
    {
        var document = _application.ActiveDocument;
        if (document is not AssemblyDocument)
        {
            MessageBox.Show(new InventorWindowOwner(_application.MainFrameHWND),
                "Hãy mở file Assembly (.iam) rồi chạy lại lệnh này.",
                "DXF Exporter Pro", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var addInDirectory = IOPath.GetDirectoryName(typeof(RunILogicRuleCommand).Assembly.Location)
            ?? throw new InvalidOperationException("Không xác định được thư mục cài đặt add-in.");
        var rulePath = IOPath.Combine(addInDirectory, _ruleFileName);
        if (!IOFile.Exists(rulePath))
            throw new FileNotFoundException(
                $"Không tìm thấy rule '{_ruleFileName}'. Hãy cài lại DXF Exporter Pro.", rulePath);

        var iLogicAddIn = FindILogicAddIn();
        if (!iLogicAddIn.Activated)
            iLogicAddIn.Activate();

        if (iLogicAddIn.Automation is not IiLogicAutomation automation)
            throw new InvalidOperationException("Không thể khởi tạo giao diện tự động hóa iLogic của Inventor.");

        var result = automation.RunExternalRule(document, rulePath);
        if (result != 0)
            throw new InvalidOperationException(
                $"iLogic không chạy được rule '{_ruleFileName}' (mã lỗi {result}).");
    }

    private ApplicationAddIn FindILogicAddIn()
    {
        var addIns = _application.ApplicationAddIns;
        for (var index = 1; index <= addIns.Count; index++)
        {
            var addIn = addIns[index];
            var identity = $"{addIn.DisplayName} {addIn.ShortDisplayName} {addIn.Description}";
            if (identity.Contains("iLogic", StringComparison.OrdinalIgnoreCase))
                return addIn;
        }

        throw new InvalidOperationException(
            "Không tìm thấy add-in iLogic của Inventor. Hãy cài đặt hoặc bật iLogic rồi thử lại.");
    }
}
