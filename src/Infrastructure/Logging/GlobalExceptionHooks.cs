using Microsoft.Extensions.Logging;

namespace GridironFrontOffice.Infrastructure.Logging;

/// <summary>
/// The process-wide failure hooks both hosts install (LOGGING.md §8). The WPF UI-thread hook
/// (<c>Application.DispatcherUnhandledException</c>) lives in the Desktop host, next to the crash dialog.
/// </summary>
public static class GlobalExceptionHooks
{
    /// <param name="logger">The host's logger.</param>
    /// <param name="onUnhandled">
    /// Called after an unhandled background-thread exception is logged, before the process goes down.
    /// The Desktop host shows the crash dialog here; the DevHost passes null.
    /// </param>
    public static void Install(ILogger logger, Action<Exception>? onUnhandled = null)
    {
        ArgumentNullException.ThrowIfNull(logger);

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            var exception = e.ExceptionObject as Exception
                ?? new InvalidOperationException($"A non-exception object was thrown: {e.ExceptionObject}");
            HostLog.UnhandledException(logger, exception, e.IsTerminating);
            onUnhandled?.Invoke(exception);
        };

        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            HostLog.UnobservedTaskException(logger, e.Exception);
            e.SetObserved();
        };
    }
}
