using System.Text.Json.Serialization;

namespace CollabsKus.BlazorWebAssembly.Models;

public class TimeResponse
{
    [JsonPropertyName("hour")]
    public string Hour { get; set; } = string.Empty;

    [JsonPropertyName("min")]
    public string Min { get; set; } = string.Empty;

    [JsonPropertyName("sec")]
    public string Sec { get; set; } = string.Empty;

    [JsonPropertyName("apOrPm")]
    public string ApOrPm { get; set; } = string.Empty;

    [JsonPropertyName("hourNE")]
    public string HourNE { get; set; } = string.Empty;

    [JsonPropertyName("minNE")]
    public string MinNE { get; set; } = string.Empty;

    [JsonPropertyName("secNE")]
    public string SecNE { get; set; } = string.Empty;
}
