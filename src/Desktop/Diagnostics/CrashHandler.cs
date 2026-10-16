using System.Diagnostics;
using System.Windows;
using GridironFrontOffice.Infrastructure.Logging;
using Microsoft.Extensions.Logging;
using Serilog;

namespace GridironFrontOffice.Desktop.Diagnostics;

/// <summary>
/// Turns a fatal exception into the crash dialog, then ends the process (LOGGING.md §8). The exception is already
/// logged by the hook that called this. Every command is its own transaction, so the save is safe; the app
/// doesn't try to keep running in an unknown state.
/// </summary>
internal sealed class CrashHandler(Microsoft.Extensions.Logging.ILogger logger, string logsFolder)
{
    private int _handling;

    /// <summary>Shows the dialog once (from any thread), records the choice, flushes the log and exits.</summary>
    public void ShowAndExit(Exception exception)
    {
        if (Interlocked.Exchange(ref _handling, 1) == 1)
        {
            return;   // a second failure while the dialog is up: it's logged, one dialog is enough
        }

        var choice = CrashChoice.Close;
        try
        {
            if (System.Windows.Application.Current?.Dispatcher.CheckAccess() == true)
            {
                choice = ShowDialog(exception);
            }
            else
            {
                // A background thread: give the dialog its own STA thread and wait for it.
                var ui = new Thread(() => choice = ShowDialog(exception)) { Name = "gfo-crash-dialog" };
                ui.SetApartmentState(ApartmentState.STA);
                ui.Start();
                ui.Join();
            }
        }
        catch (Exception dialogFailure) when (dialogFailure is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            var failure = $"dialog failed: {dialogFailure.GetType().Name}: {dialogFailure.Message}";
            HostLog.CrashDialogChoice(logger, failure);
        }

        var chosen = choice.ToString();
        HostLog.CrashDialogChoice(logger, chosen);
        Log.CloseAndFlush();

        if (choice == CrashChoice.Restart && Environment.ProcessPath is { } exe)
        {
            // WP9's single-instance guard must let this new process wait for the old one to exit.
            var restart = new ProcessStartInfo(exe) { UseShellExecute = false };
            // The original arguments carry over, except a Debug diagnostics test (it would just crash again).
            foreach (var arg in Environment.GetCommandLineArgs().Skip(1).Where(a => !a.StartsWith("--diag-test=", StringComparison.Ordinal)))
            {
                restart.ArgumentList.Add(arg);
            }

            Process.Start(restart)?.Dispose();
        }

        // Exit here rather than returning to the runtime, which would add Windows' own crash report on top of ours.
        Environment.Exit(1);
    }

    private CrashChoice ShowDialog(Exception exception)
    {
        var window = new CrashWindow(exception, logsFolder);
        window.ShowDialog();
        return window.Choice;
    }
}
