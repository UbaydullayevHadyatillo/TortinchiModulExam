using System.Text.Json.Serialization;

namespace PhotoStorageBot.Models;

public class UnsplashPhoto
{
    [JsonPropertyName("urls")]
    public UnsplashUrls? Urls { get; set; }

    [JsonPropertyName("user")]
    public UnsplashUser? User { get; set; }
}