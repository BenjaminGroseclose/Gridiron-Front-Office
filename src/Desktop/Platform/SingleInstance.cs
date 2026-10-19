using System.Diagnostics;
using System.Runtime.InteropServices;

namespace GridironFrontOffice.Desktop.Platform;

/// <summary>
/// One running app per Windows session (ARCHITECTURE.md §6.1): two processes would share the settings, the logs and
/// possibly a save. A named mutex marks the running instance. A second launch brings that window to the front and
/// exits. The crash dialog's Restart passes <see cref="RestartArgument"/>, so the new process waits for the old one to
/// finish exiting instead of giving up.
/// </summary>
internal sealed class SingleInstance : IDisposable
{
    public const string RestartArgument = "--restart";

    private const string MutexName = @"Local\GridironFrontOffice.Desktop";
    private const int SwRestore = 9;

    private static readonly TimeSpan RestartWait = TimeSpan.FromSeconds(10);

    private readonly Mutex _mutex;

    private SingleInstance(Mutex mutex) => _mutex = mutex;

    /// <summary>The guard if this is the only instance; otherwise null, after bringing the running one to the front.</summary>
    public static SingleInstance? TryAcquire(IEnumerable<string> args) => TryAcquire(args, MutexName, RestartWait);

    /// <summary>The guard under any name (tests use their own, so a running app doesn't interfere).</summary>
    internal static SingleInstance? TryAcquire(IEnumerable<string> args, string mutexName, TimeSpan restartWait)
    {
        var mutex = new Mutex(initiallyOwned: false, mutexName);
        var wait = args.Contains(RestartArgument, StringComparer.Ordinal) ? restartWait : TimeSpan.Zero;
        try
        {
            if (mutex.WaitOne(wait))
            {
                return new SingleInstance(mutex);
            }
        }
        catch (AbandonedMutexException)
        {
            return new SingleInstance(mutex);   // the previous instance ended without releasing it (a crash): it's ours now
        }

        mutex.Dispose();
        BringRunningInstanceToFront();
        return null;
    }

    /// <summary>Releases the mutex. Call from the thread that acquired it (the UI thread).</summary>
    public void Dispose()
    {
        _mutex.ReleaseMutex();
        _mutex.Dispose();
    }

    /// <summary>
    /// The user just launched this process, so Windows lets it hand the foreground to the running window. A window that
    /// isn't up yet (the other instance is still starting) simply appears on its own.
    /// </summary>
    private static void BringRunningInstanceToFront()
    {
        using var current = Process.GetCurrentProcess();
        foreach (var other in Process.GetProcessesByName(current.ProcessName))
        {
            using (other)
            {
                if (other.Id == current.Id || other.SessionId != current.SessionId || other.MainWindowHandle == IntPtr.Zero)
                {
                    continue;
                }

                var window = other.MainWindowHandle;
                if (IsIconic(window))
                {
                    ShowWindow(window, SwRestore);
                }

                SetForegroundWindow(window);
                return;
            }
        }
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsIconic(IntPtr hwnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ShowWindow(IntPtr hwnd, int command);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(IntPtr hwnd);
}
