using System.Diagnostics;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using GridironFrontOffice.Application.Platform;

namespace GridironFrontOffice.Desktop.Platform;

/// <summary>
/// <see cref="IPlatformServices"/> on Windows: the shell's Recycle Bin (<c>SHFileOperationW</c>, which needs no
/// package) and Explorer.
/// </summary>
internal sealed class WindowsPlatformServices : IPlatformServices
{
    private const uint FoDelete = 0x0003;
    private const ushort FofSilent = 0x0004;            // no progress dialog
    private const ushort FofNoConfirmation = 0x0010;    // the app already confirmed, naming the league
    private const ushort FofAllowUndo = 0x0040;         // the Recycle Bin, not a permanent delete
    private const ushort FofNoErrorUi = 0x0400;         // failures come back as a code
    private const ushort FofWantNukeWarning = 0x4000;   // ask before anything would be deleted permanently

    public bool SendToRecycleBin(IReadOnlyList<string> paths)
    {
        ArgumentNullException.ThrowIfNull(paths);

        // The shell expects an STA thread, and callers continue on the thread pool after their awaits.
        var moved = false;
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                moved = Recycle(paths);
            }
            catch (IOException ex)
            {
                failure = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (failure is not null)
        {
            ExceptionDispatchInfo.Throw(failure);
        }

        return moved;
    }

    public void ShowInFolder(string path) =>
        Process.Start(new ProcessStartInfo("explorer.exe") { Arguments = $"/select,\"{path}\"", UseShellExecute = false })?.Dispose();

    private static bool Recycle(IReadOnlyList<string> paths)
    {
        var operation = new ShFileOpStruct
        {
            Func = FoDelete,
            From = string.Join('\0', paths.Select(Path.GetFullPath)) + "\0\0",   // a double-null-terminated list
            Flags = FofAllowUndo | FofNoConfirmation | FofWantNukeWarning | FofSilent | FofNoErrorUi,
        };

        var code = SHFileOperationW(ref operation);
        if (operation.AnyOperationsAborted)
        {
            return false;   // the player said no to a permanent delete
        }

        return code == 0 ? true : throw new IOException($"Windows couldn't move it to the Recycle Bin (shell error 0x{code:X})");
    }

    /// <summary>SHFILEOPSTRUCTW with the 64-bit layout (GFO ships x64 only; the 32-bit header packs it to 1 byte).</summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct ShFileOpStruct
    {
        public IntPtr Hwnd;
        public uint Func;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string From;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string? To;
        public ushort Flags;
        [MarshalAs(UnmanagedType.Bool)]
        public bool AnyOperationsAborted;
        public IntPtr NameMappings;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string? ProgressTitle;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SHFileOperationW(ref ShFileOpStruct operation);
}
