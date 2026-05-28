using Virginia.Extensions;
using Xunit;

namespace Virginia.Tests;

public sealed class NaturalComparerTests
{
    [Fact]
    public void NullValues_HandledConsistently()
    {
        var c = NaturalComparer.OrdinalIgnoreCase;
        Assert.Equal(0, c.Compare(null, null));
        Assert.True(c.Compare(null, "a") < 0);
        Assert.True(c.Compare("a", null) > 0);
    }

    [Fact]
    public void Equal_Strings_Return_Zero()
    {
        var c = NaturalComparer.OrdinalIgnoreCase;
        Assert.Equal(0, c.Compare("abc", "abc"));
        Assert.Equal(0, c.Compare("File 10", "File 10"));
    }

    [Fact]
    public void Embedded_Numbers_Compared_Numerically()
    {
        var c = NaturalComparer.OrdinalIgnoreCase;
        Assert.True(c.Compare("Item 2", "Item 10") < 0);
        Assert.True(c.Compare("Item 10", "Item 2") > 0);
        Assert.True(c.Compare("Item 9", "Item 10") < 0);
    }

    private static readonly string[] expected = ["1", "3", "20", "100", "1000"];
    private static readonly string[] expectedArray = ["Unit 1", "Unit 2", "Unit 3", "Unit 10", "Unit 20"];
    private static readonly string[] expectedArray0 = ["Alice", "Bob", "Charlie"];
    private static readonly string[] expectedArray1 = ["Building 1", "Building 2", "Building 10"];
    private static readonly string[] expectedArray2 = ["Item 10", "Item 2", "Item 1"];
    private static readonly string[] expectedArray3 =
            [
                "Apartment 1A",
                "Apartment 1B",
                "Apartment 2A",
                "Apartment 10A",
                "Apartment 20A"
            ];

    [Fact]
    public void Pure_Numeric_Strings_Sort_Numerically()
    {
        var input = new[] { "100", "20", "3", "1000", "1" };
        Array.Sort(input, NaturalComparer.OrdinalIgnoreCase);
        Assert.Equal(expected, input);
    }

    [Fact]
    public void Case_Insensitive_By_Default()
    {
        var c = NaturalComparer.OrdinalIgnoreCase;
        Assert.Equal(0, c.Compare("ABC", "abc"));
        Assert.True(c.Compare("apple", "BANANA") < 0);
    }

    [Fact]
    public void Case_Sensitive_When_Requested()
    {
        var c = NaturalComparer.Ordinal;
        // In ordinal byte order, 'A' (65) < 'a' (97)
        Assert.True(c.Compare("ABC", "abc") < 0);
    }

    [Fact]
    public void Leading_Zeros_Treated_As_Equal_Magnitude()
    {
        var c = NaturalComparer.OrdinalIgnoreCase;
        // "File 02" and "File 2" are numerically equal; ordering should be
        // stable and deterministic.
        Assert.Equal(0, c.Compare("File 2", "File 2"));
        // With leading zeros stripped they're equal in magnitude — comparer
        // breaks the tie deterministically.
        var result = c.Compare("File 02", "File 2");
        Assert.NotEqual(0, result);
    }

    [Fact]
    public void Mixed_AlphaNumeric_Sort_Like_Humans_Expect()
    {
        var input = new[]
        {
            "Unit 10",
            "Unit 2",
            "Unit 1",
            "Unit 20",
            "Unit 3"
        };
        Array.Sort(input, NaturalComparer.OrdinalIgnoreCase);
        Assert.Equal(
            expectedArray,
            input);
    }

    [Fact]
    public void Pure_Alpha_Strings_Behave_Like_String_Comparison()
    {
        var input = new[] { "Charlie", "Alice", "Bob" };
        Array.Sort(input, NaturalComparer.OrdinalIgnoreCase);
        Assert.Equal(expectedArray0, input);
    }

    [Fact]
    public void OrderByNatural_Extension_Works_On_Selector()
    {
        var input = new[]
        {
            new { Name = "Building 10" },
            new { Name = "Building 2" },
            new { Name = "Building 1" }
        };
        var sorted = input.OrderByNatural(x => x.Name).Select(x => x.Name).ToArray();
        Assert.Equal(expectedArray1, sorted);
    }

    [Fact]
    public void OrderByNaturalDescending_Reverses_Order()
    {
        var input = new[] { "Item 1", "Item 2", "Item 10" };
        var sorted = input.OrderByNaturalDescending(x => x).ToArray();
        Assert.Equal(expectedArray2, sorted);
    }

    [Fact]
    public void Determinism_Across_Platforms_Smoke_Test()
    {
        // The whole point of the managed comparer: identical results
        // regardless of the host OS or culture.
        var inputs = new[]
        {
            "Apartment 1A", "Apartment 1B", "Apartment 10A",
            "Apartment 2A", "Apartment 20A"
        };
        Array.Sort(inputs, NaturalComparer.OrdinalIgnoreCase);
        Assert.Equal(
            expectedArray3,
            inputs);
    }

    [Fact]
    public void Empty_Strings_Compared_Consistently()
    {
        var c = NaturalComparer.OrdinalIgnoreCase;
        Assert.Equal(0, c.Compare("", ""));
        Assert.True(c.Compare("", "a") < 0);
        Assert.True(c.Compare("a", "") > 0);
    }
}
