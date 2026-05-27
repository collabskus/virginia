namespace Virginia.Services;

/// <summary>
/// Default implementation of <see cref="IContactChangeNotifier"/>. Catches and
/// logs subscriber exceptions individually so one broken subscriber can't take
/// down the others (or the calling write).
/// </summary>
public sealed partial class ContactChangeNotifier(
    ILogger<ContactChangeNotifier> logger) : IContactChangeNotifier
{
    public event Action<ContactChangeEvent>? Changed;

    public void Publish(ContactChangeEvent evt)
    {
        var handlers = Changed;
        if (handlers is null) return;

        foreach (var handler in handlers.GetInvocationList().Cast<Action<ContactChangeEvent>>())
        {
            try
            {
                handler(evt);
            }
            catch (Exception ex)
            {
                Log.SubscriberFailed(logger, evt.ContactId, evt.Kind.ToString(), ex);
            }
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
