using System.Text.Json.Serialization;

namespace PhotoStorageBot.Models;

public class UnsplashUrls
{
    [JsonPropertyName("small")]
    public string? Small { get; set; }

    [JsonPropertyName("regular")]
    public string? Regular { get; set; }
}