using GridironFrontOffice.Application.Data;
using GridironFrontOffice.Application.Platform;
using GridironFrontOffice.Application.Saves;
using GridironFrontOffice.Core.Results;
using GridironFrontOffice.UI.Components.Overlays;

namespace GridironFrontOffice.UI.Tests.TestDoubles;

/// <summary>The save list the load screen and Continue read.</summary>
internal sealed class FakeSaveCatalog : ISaveCatalog
{
    public List<SaveCard> Cards { get; } = [];

    public int Reads { get; private set; }

    public Task<IReadOnlyList<SaveCard>> ListAsync(CancellationToken cancellationToken = default)
    {
        Reads++;
        return Task.FromResult<IReadOnlyList<SaveCard>>([.. Cards]);
    }

    /// <summary>A save that opens as it is, last played <paramref name="daysAgo"/> days before 2026-10-19.</summary>
    public SaveCard Add(string league, int daysAgo = 0, SaveCardStatus status = SaveCardStatus.Ready, string? problem = null)
    {
        var card = new SaveCard($@"C:\Saves\{league.Replace(' ', '-')}.gfo", status)
        {
            SaveID = Guid.NewGuid(),
            LeagueName = status == SaveCardStatus.Unreadable ? "" : league,
            CreatedAtUtc = new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc),
            LastPlayedAtUtc = new DateTime(2026, 10, 19, 12, 0, 0, DateTimeKind.Utc).AddDays(-daysAgo),
            PlayTimeSeconds = 8100,
            Problem = problem,
        };
        Cards.Add(card);
        return card;
    }
}

/// <summary>An in-memory hub: Publish delivers to the subscribers of that event type.</summary>
internal sealed class FakeDomainEvents : IDomainEventDispatcher
{
    private readonly List<(Type Type, Action<IDomainEvent> Handler)> _handlers = [];

    public IDisposable Subscribe<TEvent>(Action<TEvent> handler)
        where TEvent : IDomainEvent
    {
        var entry = (typeof(TEvent), (Action<IDomainEvent>)(e => handler((TEvent)e)));
        _handlers.Add(entry);
        return new Unsubscriber(() => _handlers.Remove(entry));
    }

    public int Subscribers => _handlers.Count;

    public void Publish(IDomainEvent e)
    {
        foreach (var (type, handler) in _handlers.ToList().Where(h => h.Type == e.GetType()))
        {
            handler(e);
        }
    }

    private sealed class Unsubscriber(Action dispose) : IDisposable
    {
        public void Dispose() => dispose();
    }
}

/// <summary>
/// Records what the screens asked for and answers like SaveManager: Create refuses an empty name, Open and Delete
/// succeed unless a test sets a failure, and the events go out through <see cref="FakeDomainEvents"/>.
/// </summary>
internal sealed class FakeSaveManager(FakeDomainEvents events) : ISaveManager
{
    public OpenSave? Current { get; private set; }

    public List<string> Opened { get; } = [];

    public List<string> Deleted { get; } = [];

    public int Closes { get; private set; }

    public DomainError? OpenFailure { get; set; }

    public DomainError? DeleteFailure { get; set; }

    public bool OpenUpgrades { get; set; }

    public Task<Result<OpenSave>> CreateAsync(string leagueName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(leagueName))
        {
            return Task.FromResult(Result.Fail<OpenSave>(SaveErrors.InvalidName("A league needs a name.")));
        }

        return Task.FromResult(Result.Ok(Open(new OpenSave(Guid.NewGuid(), leagueName.Trim(), $@"C:\Saves\{leagueName.Trim()}.gfo", 1, Upgraded: false))));
    }

    public Task<Result<OpenSave>> OpenAsync(string path, CancellationToken cancellationToken = default)
    {
        Opened.Add(path);
        if (OpenFailure is { } failure)
        {
            return Task.FromResult(Result.Fail<OpenSave>(failure));
        }

        var league = Path.GetFileNameWithoutExtension(path).Replace('-', ' ');
        return Task.FromResult(Result.Ok(Open(new OpenSave(Guid.NewGuid(), league, path, 1, OpenUpgrades))));
    }

    public Task CloseAsync(CancellationToken cancellationToken = default)
    {
        Closes++;
        if (Current is { } open)
        {
            Current = null;
            events.Publish(new SaveClosed(open.SaveID));
        }

        return Task.CompletedTask;
    }

    public Task<Result<OpenSave>> RestoreAsync(SnapshotInfo snapshot, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("Restore arrives with the Time Machine (M3).");

    public Task<Result<Unit>> DeleteAsync(string path, CancellationToken cancellationToken = default)
    {
        if (DeleteFailure is { } failure)
        {
            return Task.FromResult(Result.Fail<Unit>(failure));
        }

        Deleted.Add(path);
        return Task.FromResult(Result.Ok());
    }

    /// <summary>Makes a league current, as if it had been opened, and announces it.</summary>
    public OpenSave Open(OpenSave save)
    {
        Current = save;
        events.Publish(new SaveOpened(save));
        return save;
    }
}

internal sealed class FakePlatformServices : IPlatformServices
{
    public List<string> ShownInFolder { get; } = [];

    public bool SendToRecycleBin(IReadOnlyList<string> paths) => true;

    public void ShowInFolder(string path) => ShownInFolder.Add(path);
}

/// <summary>Answers every confirmation with <see cref="Answer"/> and records the question.</summary>
internal sealed class FakeDialogs : IGfoDialogs
{
    public bool Answer { get; set; } = true;

    public List<(string Title, string Message, string Confirm)> Asked { get; } = [];

    public Task<bool> ConfirmAsync(string title, string message, string confirm = "OK", string cancel = "Cancel")
    {
        Asked.Add((title, message, confirm));
        return Task.FromResult(Answer);
    }

    public Task PeekAsync<TComponent>(string title, IReadOnlyDictionary<string, object?>? parameters = null, string width = "480px")
        where TComponent : Microsoft.AspNetCore.Components.ComponentBase => Task.CompletedTask;
}

internal sealed class FakeToasts : IGfoToasts
{
    public List<(string Kind, string Title, string? Message)> Shown { get; } = [];

    public Task SuccessAsync(string title, string? message = null) => Record("success", title, message);

    public Task InfoAsync(string title, string? message = null) => Record("info", title, message);

    public Task WarningAsync(string title, string? message = null) => Record("warning", title, message);

    public Task ErrorAsync(string title, string? message = null) => Record("error", title, message);

    private Task Record(string kind, string title, string? message)
    {
        Shown.Add((kind, title, message));
        return Task.CompletedTask;
    }
}
