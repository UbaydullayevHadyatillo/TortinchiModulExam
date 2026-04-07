using System.Text.Json.Serialization;

namespace PhotoStorageBot.Models;

public class UnsplashSearchResponse
{
    [JsonPropertyName("results")]
    public List<UnsplashPhoto> Results { get; set; } = new();
}