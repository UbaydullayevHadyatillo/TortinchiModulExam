namespace PhotoStorageBot.Models;

public class BotUser
{
    public long UserId { get; set; }
    public string? Username { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public DateTime JoinedAt { get; set; }
}