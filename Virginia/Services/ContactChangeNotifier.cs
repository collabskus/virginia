namespace Virginia.Services;

/// <summary>
/// Default implementation of <see cref="IContactChangeNotifier"/>. Catches and
/// logs subscriber exceptions individually so one broken subscriber can't take
/// down the others (or the calling write).
///
/// Dispatch is deliberately decoupled from the caller's execution context. Each
/// subscriber's <c>OnContactChanged</c> handler already marshals its own render
/// onto its circuit via <c>InvokeAsync</c>, but the act of <em>invoking</em> the
/// handlers used to run inline on the writer's circuit thread. With many live
/// circuits, that synchronous fan-out (serialized by the per-circuit parallel
/// invocation cap) is what makes the writer's own request block until every
/// observer has been poked. We instead hand each handler invocation to the
/// thread pool, so <see cref="Publish"/> returns to the writer immediately and
/// the observer renders proceed on their own dispatchers in parallel.
/// </summary>
public sealed partial class ContactChangeNotifier(
    ILogger<ContactChangeNotifier> logger) : IContactChangeNotifier
{
    public event Action<ContactChangeEvent>? Changed;

    public void Publish(ContactChangeEvent evt)
    {
        var handlers = Changed;
        if (handlers is null) return;

        // Snapshot the invocation list once. Subscribers may add/remove during
        // dispatch (a circuit disposing mid-fan-out); the snapshot keeps us
        // iterating a stable set.
        var invocationList = handlers.GetInvocationList();

        foreach (var del in invocationList)
        {
            var handler = (Action<ContactChangeEvent>)del;

            // Dispatch each subscriber off the caller's thread so the writer's
            // circuit is not held while N observers are notified. The handler
            // itself (ContactDetail.OnContactChanged) is cheap and immediately
            // hands off to InvokeAsync on its own circuit, so this thread-pool
            // work item is short-lived.
            _ = Task.Run(() =>
            {
                try
                {
                    handler(evt);
                }
                catch (Exception ex)
                {
                    Log.SubscriberFailed(logger, evt.ContactId, evt.Kind.ToString(), ex);
                }
            });
        }
    }

    private static partial class Log
    {
        [LoggerMessage(Level = LogLevel.Warning,
            Message = "Change-notifier subscriber threw for contact {ContactId} ({Kind})")]
        public static partial void SubscriberFailed(
            ILogger logger, int contactId, string kind, Exception ex);
    }
}
