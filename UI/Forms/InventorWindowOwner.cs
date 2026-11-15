using System.Windows.Forms;

namespace InventorDxfExporter.UI.Forms;

internal sealed class InventorWindowOwner : IWin32Window
{
    public InventorWindowOwner(int handle)
    {
        if (handle == 0)
            throw new ArgumentOutOfRangeException(nameof(handle), "Inventor returned an invalid main window handle.");

        Handle = new IntPtr(handle);
    }

    public IntPtr Handle { get; }
}
