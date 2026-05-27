using System.Text.Json.Serialization;

namespace CollabsKus.BlazorWebAssembly.Models;

public class CalendarResponse
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("res")]
    public CalendarData Res { get; set; } = new();
}

public class CalendarData
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("year")]
    public string Year { get; set; } = string.Empty;

    [JsonPropertyName("eng1")]
    public string Eng1 { get; set; } = string.Empty;

    [JsonPropertyName("eng2")]
    public string Eng2 { get; set; } = string.Empty;

    [JsonPropertyName("engYear")]
    public int EngYear { get; set; }

    [JsonPropertyName("weeksNeFull")]
    public List<string> WeeksNeFull { get; set; } = new();

    [JsonPropertyName("weeksNeMini")]
    public List<string> WeeksNeMini { get; set; } = new();

    [JsonPropertyName("days")]
    public List<CalendarDay> Days { get; set; } = new();
}

public class CalendarDay
{
    [JsonPropertyName("bs")]
    public string Bs { get; set; } = string.Empty;

    [JsonPropertyName("ad")]
    public string Ad { get; set; } = string.Empty;

    [JsonPropertyName("tag")]
    public string? Tag { get; set; }
}
