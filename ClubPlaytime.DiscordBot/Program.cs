using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using ClubPlaytime.DiscordBot.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure Discord.NET
builder.Services.AddSingleton(sp => new DiscordSocketClient(new DiscordSocketConfig
{
    GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildMessages,
    LogLevel = LogSeverity.Info
}));

builder.Services.AddSingleton(sp => new InteractionService(
    sp.GetRequiredService<DiscordSocketClient>(),
    new InteractionServiceConfig
    {
        DefaultRunMode = RunMode.Async,
        LogLevel = LogSeverity.Info
    }));

// Register HTTP client for the API with the base URL from config
builder.Services.AddHttpClient<PlaytimeApiClient>(client =>
{
    client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
    client.Timeout = TimeSpan.FromSeconds(20);

    // Read the API base URL from configuration
    var config = builder.Configuration["Api:BaseUrl"];
    if (!string.IsNullOrWhiteSpace(config))
    {
        var baseUrl = config.TrimEnd('/') + "/";
        client.BaseAddress = new Uri(baseUrl);
    }
});

// Register bot service and background services
builder.Services.AddHostedService<DiscordBotService>();
builder.Services.AddHostedService<DailyPlaytimePoster>();

// Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var app = builder.Build();

app.MapGet("/", () => "Discord bot is running.");
app.MapGet("/health", () => Results.Ok());

app.Run();
