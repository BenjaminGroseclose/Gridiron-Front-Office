using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace GridironFrontOffice.Desktop.Diagnostics;

/// <summary>What the player chose in the crash dialog.</summary>
internal enum CrashChoice
{
    Close,
    Restart,
}

/// <summary>The crash dialog (LOGGING.md §8): a small native window, shown after the crash is already in the log.</summary>
internal sealed partial class CrashWindow : Window
{
    private readonly string _logsFolder;

    public CrashWindow(Exception exception, string logsFolder)
    {
        _logsFolder = logsFolder;
        InitializeComponent();
        Detail.Text = $"{exception.GetType().Name}: {exception.Message}";
        Loaded += (_, _) => RestartButton.Focus();
        SourceInitialized += (_, _) => UseDarkTitleBar();
    }

    public CrashChoice Choice { get; private set; } = CrashChoice.Close;

    /// <summary>A dark caption to match the Stadium dark body (DWMWA_USE_IMMERSIVE_DARK_MODE).</summary>
    private void UseDarkTitleBar()
    {
        var dark = 1;
        _ = DwmSetWindowAttribute(new WindowInteropHelper(this).Handle, 20, ref dark, sizeof(int));
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    private void OnOpenLogs(object sender, RoutedEventArgs e)
    {
        if (Directory.Exists(_logsFolder))
        {
            Process.Start(new ProcessStartInfo("explorer.exe") { ArgumentList = { _logsFolder }, UseShellExecute = false })?.Dispose();
        }
    }

    private void OnRestart(object sender, RoutedEventArgs e)
    {
        Choice = CrashChoice.Restart;
        Close();
    }

    private void OnClose(object sender, RoutedEventArgs e)
    {
        Choice = CrashChoice.Close;
        Close();
    }
}
