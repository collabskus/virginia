using Virginia.Data;

namespace Virginia.Services;

/// <summary>
/// In-process pub/sub for contact mutations. A singleton — one instance for the
/// whole app — so events raised on one circuit are visible to all other circuits.
///
/// Each event carries an <see cref="ContactChangeEvent.OriginId"/> identifying
/// the circuit that caused the change, so subscribers can ignore their own echoes.
/// </summary>
public interface IContactChangeNotifier
{
    event Action<ContactChangeEvent>? Changed;

    void Publish(ContactChangeEvent evt);
}

public enum ContactChangeKind
{
    Updated,        // FirstName, LastName, Emails, Phones, Addresses
    Deleted,
    PhotoChanged,   // Set or removed
    NoteAdded
}

/// <summary>
/// A single contact mutation. <see cref="Note"/> is populated when
/// <see cref="Kind"/> is <see cref="ContactChangeKind.NoteAdded"/>.
/// </summary>
public sealed record ContactChangeEvent(
    int ContactId,
    ContactChangeKind Kind,
    Guid? OriginId,
    NoteDto? Note = null);
