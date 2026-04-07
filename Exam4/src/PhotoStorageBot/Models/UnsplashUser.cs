using System.Text.Json.Serialization;

namespace PhotoStorageBot.Models;

public class UnsplashUser
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }
}