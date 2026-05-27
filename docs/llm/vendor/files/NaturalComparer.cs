using System.Globalization;

namespace Virginia.Extensions;

/// <summary>
/// Compares strings using natural ordering: embedded digit runs are compared
/// as numbers rather than character-by-character. "Item 2" sorts before
/// "Item 10". Non-digit runs are compared with the supplied
/// <see cref="StringComparison"/> (default: <c>OrdinalIgnoreCase</c>).
/// Fully managed, cross-platform, deterministic.
/// </summary>
public sealed class NaturalComparer : IComparer<string?>
{
    public static readonly NaturalComparer OrdinalIgnoreCase = new(StringComparison.OrdinalIgnoreCase);
    public static readonly NaturalComparer Ordinal = new(StringComparison.Ordinal);
    public static readonly NaturalComparer CurrentCultureIgnoreCase = new(StringComparison.CurrentCultureIgnoreCase);

    private readonly StringComparison _comparison;

    public NaturalComparer(StringComparison comparison = StringComparison.OrdinalIgnoreCase)
    {
        _comparison = comparison;
    }

    public int Compare(string? x, string? y)
    {
        if (ReferenceEquals(x, y)) return 0;
        if (x is null) return -1;
        if (y is null) return 1;

        ReadOnlySpan<char> a = x.AsSpan();
        ReadOnlySpan<char> b = y.AsSpan();
        int ia = 0, ib = 0;

        while (ia < a.Length && ib < b.Length)
        {
            bool digitA = char.IsDigit(a[ia]);
            bool digitB = char.IsDigit(b[ib]);

            if (digitA && digitB)
            {
                int endA = ia;
                while (endA < a.Length && char.IsDigit(a[endA])) endA++;
                int endB = ib;
                while (endB < b.Length && char.IsDigit(b[endB])) endB++;

                ReadOnlySpan<char> numA = a[ia..endA];
                ReadOnlySpan<char> numB = b[ib..endB];

                // Strip leading zeros for true numeric comparison; preserve
                // a single zero if the run is all zeros.
                ReadOnlySpan<char> trimmedA = TrimLeadingZeros(numA);
                ReadOnlySpan<char> trimmedB = TrimLeadingZeros(numB);

                if (trimmedA.Length != trimmedB.Length)
                    return trimmedA.Length - trimmedB.Length;

                int cmp = trimmedA.SequenceCompareTo(trimmedB);
                if (cmp != 0) return cmp;

                // Equal numerically; the one with more leading zeros sorts
                // first so ordering is stable.
                int leadDiff = (numA.Length - trimmedA.Length) - (numB.Length - trimmedB.Length);
                if (leadDiff != 0) return -leadDiff;

                ia = endA;
                ib = endB;
            }
            else
            {
                int endA = ia;
                while (endA < a.Length && !char.IsDigit(a[endA])) endA++;
                int endB = ib;
                while (endB < b.Length && !char.IsDigit(b[endB])) endB++;

                int len = Math.Min(endA - ia, endB - ib);
                int cmp = string.Compare(
                    x, ia, y, ib, len, _comparison);
                if (cmp != 0) return cmp;

                int lenDiff = (endA - ia) - (endB - ib);
                if (lenDiff != 0) return lenDiff;

                ia = endA;
                ib = endB;
            }
        }

        return (a.Length - ia) - (b.Length - ib);
    }

    private static ReadOnlySpan<char> TrimLeadingZeros(ReadOnlySpan<char> s)
    {
        int i = 0;
        while (i < s.Length - 1 && s[i] == '0') i++;
        return s[i..];
    }
}

public static class NaturalSortExtensions
{
    public static IOrderedEnumerable<T> OrderByNatural<T>(
        this IEnumerable<T> source, Func<T, string?> keySelector) =>
        source.OrderBy(keySelector, NaturalComparer.OrdinalIgnoreCase);

    public static IOrderedEnumerable<T> OrderByNaturalDescending<T>(
        this IEnumerable<T> source, Func<T, string?> keySelector) =>
        source.OrderByDescending(keySelector, NaturalComparer.OrdinalIgnoreCase);

    public static IOrderedEnumerable<T> ThenByNatural<T>(
        this IOrderedEnumerable<T> source, Func<T, string?> keySelector) =>
        source.ThenBy(keySelector, NaturalComparer.OrdinalIgnoreCase);
}
