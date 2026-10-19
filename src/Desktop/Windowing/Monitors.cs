using System.Runtime.InteropServices;

namespace GridironFrontOffice.Desktop.Windowing;

/// <summary>The monitors attached right now, their work areas and effective DPIs (Win32).</summary>
internal static class Monitors
{
    private const int MonitorInfoPrimary = 1;
    private const int MdtEffectiveDpi = 0;

    public static IReadOnlyList<MonitorArea> Current()
    {
        var monitors = new List<MonitorArea>();
        EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (monitor, _, _, _) =>
        {
            var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
            if (GetMonitorInfo(monitor, ref info))
            {
                var dpi = GetDpiForMonitor(monitor, MdtEffectiveDpi, out var dpiX, out _) == 0 ? (int)dpiX : 96;
                var work = info.Work;
                monitors.Add(new MonitorArea(
                    new PixelRect(work.Left, work.Top, work.Right - work.Left, work.Bottom - work.Top),
                    dpi,
                    (info.Flags & MonitorInfoPrimary) != 0));
            }

            return true;
        }, IntPtr.Zero);
        return monitors;
    }

    private delegate bool MonitorEnumProc(IntPtr monitor, IntPtr hdc, IntPtr rect, IntPtr data);

    [StructLayout(LayoutKind.Sequential)]
    internal struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct MonitorInfo
    {
        public int Size;
        public NativeRect Monitor;
        public NativeRect Work;
        public int Flags;
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr clip, MonitorEnumProc callback, IntPtr data);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);

    [DllImport("shcore.dll")]
    private static extern int GetDpiForMonitor(IntPtr monitor, int dpiType, out uint dpiX, out uint dpiY);
}
