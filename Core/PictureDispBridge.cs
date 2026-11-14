using System.Drawing;
using System.Windows.Forms;

namespace InventorDxfExporter.Core;

internal static class PictureDispBridge
{
    public static object FromImage(Image image) =>
        BridgeHost.ToPictureDisp(image) ?? throw new InvalidOperationException("Không thể chuyển ảnh thành biểu tượng Ribbon.");

    public static Image ToImage(object picture) =>
        BridgeHost.FromPictureDisp(picture) ?? throw new InvalidOperationException("Không thể đọc thumbnail của tài liệu Inventor.");

    private sealed class BridgeHost : AxHost
    {
        private BridgeHost() : base(string.Empty) { }

        internal static object? ToPictureDisp(Image image) => GetIPictureDispFromPicture(image);

        internal static Image? FromPictureDisp(object picture) => GetPictureFromIPictureDisp(picture);
    }
}
