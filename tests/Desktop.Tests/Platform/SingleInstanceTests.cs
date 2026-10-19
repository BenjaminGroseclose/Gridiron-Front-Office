using GridironFrontOffice.Desktop.Platform;

namespace GridironFrontOffice.Desktop.Tests.Platform;

/// <summary>
/// The guard under a per-test mutex name, so a running app doesn't interfere. Each "instance" is its own thread,
/// because a mutex belongs to the thread that acquired it (and re-enters for that thread).
/// </summary>
public sealed class SingleInstanceTests
{
    private readonly string _name = $@"Local\GridironFrontOffice.Tests.{Guid.NewGuid():N}";

    [Fact]
    public void The_first_launch_gets_the_guard_and_a_second_does_not()
    {
        using var first = Instance.Start(_name, []);

        using var second = Instance.Start(_name, []);

        first.Acquired.ShouldBeTrue();
        second.Acquired.ShouldBeFalse();
    }

    [Fact]
    public void Once_the_first_instance_exits_the_next_launch_gets_the_guard()
    {
        Instance.Start(_name, []).Dispose();

        using var next = Instance.Start(_name, []);

        next.Acquired.ShouldBeTrue();
    }

    [Fact]
    public async Task A_restart_waits_for_the_crashed_instance_to_go_away()
    {
        var first = Instance.Start(_name, []);
        var restart = Task.Run(() => Instance.Start(_name, [SingleInstance.RestartArgument]), TestContext.Current.CancellationToken);

        await Task.Delay(200, TestContext.Current.CancellationToken);
        restart.IsCompleted.ShouldBeFalse();
        first.Dispose();

        using var restarted = await restart;
        restarted.Acquired.ShouldBeTrue();
    }

    [Fact]
    public void An_instance_that_died_without_releasing_the_guard_does_not_block_the_next()
    {
        // The thread ends while it owns the mutex, as a crashed process would: the mutex is abandoned.
        var crashed = new Thread(() => SingleInstance.TryAcquire([], _name, TimeSpan.Zero)) { IsBackground = true };
        crashed.Start();
        crashed.Join();

        using var next = Instance.Start(_name, []);

        next.Acquired.ShouldBeTrue();
    }

    /// <summary>One "process": a thread that takes the guard, holds it until disposed, then releases it on that thread.</summary>
    private sealed class Instance : IDisposable
    {
        private readonly ManualResetEventSlim _exit = new();
        private readonly Thread _thread;

        private Instance(string name, string[] args)
        {
            using var started = new ManualResetEventSlim();
            _thread = new Thread(() =>
            {
                var guard = SingleInstance.TryAcquire(args, name, TimeSpan.FromSeconds(5));
                Acquired = guard is not null;
                started.Set();
                _exit.Wait();
                guard?.Dispose();
            })
            { IsBackground = true };
            _thread.Start();
            started.Wait();
        }

        public bool Acquired { get; private set; }

        public static Instance Start(string name, string[] args) => new(name, args);

        public void Dispose()
        {
            _exit.Set();
            _thread.Join();
            _exit.Dispose();
        }
    }
}
