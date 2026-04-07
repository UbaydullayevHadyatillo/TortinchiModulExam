using System.Text.Json;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using PhotoStorageBot.Models;

var botToken = "8654326543:AAG-ZLDmd2NRHRhVK0uSmgPbKIJ1vuUAMTI";
var unsplashAccessKey = "55343866-be102f0c48856392a3a33aa77";

var botClient = new TelegramBotClient(botToken);
using var cts = new CancellationTokenSource();
using var httpClient = new HttpClient();

Directory.CreateDirectory("Data");
Directory.CreateDirectory(Path.Combine("Data", "images"));

var receiverOptions = new ReceiverOptions
{
    AllowedUpdates = Array.Empty<UpdateType>()
};

botClient.StartReceiving(
    updateHandler: HandleUpdateAsync,
    errorHandler: HandleErrorAsync,
    receiverOptions: receiverOptions,
    cancellationToken: cts.Token
);

var me = await botClient.GetMe();
Console.WriteLine($"Bot ishga tushdi: @{me.Username}");
Console.WriteLine("To'xtatish uchun Enter bosing...");
Console.ReadLine();

cts.Cancel();

async Task HandleUpdateAsync(ITelegramBotClient client, Update update, CancellationToken cancellationToken)
{
    if (update.Type != UpdateType.Message || update.Message?.Text is null)
        return;

    var message = update.Message;
    var chatId = message.Chat.Id;
    var text = message.Text.Trim();

    if (text.Equals("/start", StringComparison.OrdinalIgnoreCase))
    {
        await SaveUserAsync(message.From);

        await client.SendMessage(
            chatId: chatId,
            text: "Salom. Menga object nomini yozing.\nMasalan: cat, car, phone\nMen sizga 3 ta rasm topib yuboraman.",
            cancellationToken: cancellationToken
        );
        return;
    }

    if (string.IsNullOrWhiteSpace(text) || text.StartsWith("/"))
    {
        await client.SendMessage(
            chatId: chatId,
            text: "Iltimos, oddiy object nomini yozing. Masalan: cat",
            cancellationToken: cancellationToken
        );
        return;
    }

    await client.SendMessage(
        chatId: chatId,
        text: $"\"{text}\" bo'yicha rasmlar qidirilyapti...",
        cancellationToken: cancellationToken
    );

    try
    {
        var photos = await SearchUnsplashImagesAsync(text, unsplashAccessKey);

        if (photos.Count == 0)
        {
            await client.SendMessage(
                chatId: chatId,
                text: "Afsus, rasm topilmadi.",
                cancellationToken: cancellationToken
            );
            return;
        }

        for (int i = 0; i < photos.Count; i++)
        {
            var imageUrl = photos[i].Urls?.Regular ?? photos[i].Urls?.Small;

            if (string.IsNullOrWhiteSpace(imageUrl))
                continue;

            var savedPath = await DownloadImageAsync(imageUrl, text, i + 1);

            await using var stream = File.OpenRead(savedPath);

            await client.SendPhoto(
                chatId: chatId,
                photo: InputFile.FromStream(stream, Path.GetFileName(savedPath)),
                caption: $"Natija #{i + 1}\nQidiruv: {text}\nMuallif: {photos[i].User?.Name ?? "Noma'lum"}\nManba: Unsplash",
                cancellationToken: cancellationToken
            );
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine("Xatolik:");
        Console.WriteLine(ex.Message);

        await client.SendMessage(
            chatId: chatId,
            text: "Rasm qidirishda xatolik yuz berdi. Access Key yoki API so'rovni tekshiring.",
            cancellationToken: cancellationToken
        );
    }
}

Task HandleErrorAsync(ITelegramBotClient client, Exception exception, CancellationToken cancellationToken)
{
    Console.WriteLine("Bot umumiy xatoligi:");
    Console.WriteLine(exception.Message);
    return Task.CompletedTask;
}

async Task SaveUserAsync(Telegram.Bot.Types.User? user)
{
    if (user is null) return;

    var path = Path.Combine("Data", "users.json");
    List<BotUser> users;

    if (File.Exists(path))
    {
        var json = await File.ReadAllTextAsync(path);
        users = JsonSerializer.Deserialize<List<BotUser>>(json) ?? new List<BotUser>();
    }
    else
    {
        users = new List<BotUser>();
    }

    var existing = users.FirstOrDefault(x => x.UserId == user.Id);

    if (existing is null)
    {
        users.Add(new BotUser
        {
            UserId = user.Id,
            Username = user.Username,
            FirstName = user.FirstName,
            LastName = user.LastName,
            JoinedAt = DateTime.Now
        });
    }
    else
    {
        existing.Username = user.Username;
        existing.FirstName = user.FirstName;
        existing.LastName = user.LastName;
        existing.JoinedAt = DateTime.Now;
    }

    var options = new JsonSerializerOptions
    {
        WriteIndented = true
    };

    var updatedJson = JsonSerializer.Serialize(users, options);
    await File.WriteAllTextAsync(path, updatedJson);
}

async Task<List<UnsplashPhoto>> SearchUnsplashImagesAsync(string query, string accessKey)
{
    var url = $"https://api.unsplash.com/search/photos?query={Uri.EscapeDataString(query)}&per_page=3";

    using var request = new HttpRequestMessage(HttpMethod.Get, url);
    request.Headers.Add("Authorization", $"Client-ID {accessKey}");

    using var response = await httpClient.SendAsync(request);
    var json = await response.Content.ReadAsStringAsync();

    Console.WriteLine("Unsplash status: " + response.StatusCode);
    Console.WriteLine("Unsplash response:");
    Console.WriteLine(json);

    response.EnsureSuccessStatusCode();

    var result = JsonSerializer.Deserialize<UnsplashSearchResponse>(json, new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    });

    if (result?.Results is null || result.Results.Count == 0)
        return new List<UnsplashPhoto>();

    return result.Results
        .Where(x => x.Urls != null &&
                    (!string.IsNullOrWhiteSpace(x.Urls.Regular) ||
                     !string.IsNullOrWhiteSpace(x.Urls.Small)))
        .Take(3)
        .ToList();
}

async Task<string> DownloadImageAsync(string imageUrl, string query, int index)
{
    var safeQuery = MakeSafeFileName(query);
    var folder = Path.Combine("Data", "images", safeQuery);

    Directory.CreateDirectory(folder);

    var fileName = $"{DateTime.Now:yyyyMMdd_HHmmss}_{index}.jpg";
    var fullPath = Path.Combine(folder, fileName);

    var bytes = await httpClient.GetByteArrayAsync(imageUrl);
    await File.WriteAllBytesAsync(fullPath, bytes);

    return fullPath;
}

string MakeSafeFileName(string name)
{
    foreach (var c in Path.GetInvalidFileNameChars())
    {
        name = name.Replace(c, '_');
    }

    return name.Replace(" ", "_");
}