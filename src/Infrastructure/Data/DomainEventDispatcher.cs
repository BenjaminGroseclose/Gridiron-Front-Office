using System.Collections.Immutable;
using GridironFrontOffice.Application.Data;
using Microsoft.Extensions.Logging;

namespace GridironFrontOffice.Infrastructure.Data;

/// <summary>
/// The minimal in-house event hub (D-020, no mediator library). SaveWriter dispatches a command's events after it
/// commits; SaveManager dispatches open and close. A failing subscriber is logged and the rest still run, because the
/// change it reacts to is already committed.
/// </summary>
internal sealed class DomainEventDispatcher(ILogger<DomainEventDispatcher> log) : IDomainEventDispatcher
{
    private ImmutableList<Registration> _registrations = [];

    public IDisposable Subscribe<TEvent>(Action<TEvent> handler)
        where TEvent : IDomainEvent
    {
        ArgumentNullException.ThrowIfNull(handler);
        var registration = new Registration(typeof(TEvent), e => handler((TEvent)e));
        ImmutableInterlocked.Update(ref _registrations, list => list.Add(registration));
        return new Unsubscriber(() => ImmutableInterlocked.Update(ref _registrations, list => list.Remove(registration)));
    }

    public void Dispatch(IReadOnlyList<IDomainEvent> events)
    {
        if (events.Count == 0)
        {
            return;
        }

        var registrations = _registrations;   // a snapshot: subscribing from a handler doesn't change this dispatch
        foreach (var domainEvent in events)
        {
            foreach (var registration in registrations.Where(r => r.EventType.IsInstanceOfType(domainEvent)))
            {
                try
                {
                    registration.Handler(domainEvent);
                }
#pragma warning disable CA1031 // Any subscriber failure is logged; the committed change and other subscribers are unaffected.
                catch (Exception ex)
#pragma warning restore CA1031
                {
                    DataLog.EventHandlerFailed(log, ex, domainEvent.GetType().Name);
                }
            }
        }
    }

    private sealed class Registration(Type eventType, Action<IDomainEvent> handler)
    {
        public Type EventType { get; } = eventType;

        public Action<IDomainEvent> Handler { get; } = handler;
    }

    private sealed class Unsubscriber(Action unsubscribe) : IDisposable
    {
        private Action? _unsubscribe = unsubscribe;

        public void Dispose() => Interlocked.Exchange(ref _unsubscribe, null)?.Invoke();
    }
}
