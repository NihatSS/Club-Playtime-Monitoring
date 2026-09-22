using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using ClubPlaytime.DiscordBot.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure Discord.NET
builder.Services.AddSingleton(sp => new DiscordSocketClient(new DiscordSocketConfig
{
    // Guilds is all this bot needs: every command is an interaction (slash command,
    // button, modal), and interactions are not gated behind any intent, while the
    // guild cache is what resolves the daily-post channel. GuildMessages made
    // Discord push every message posted in the guild to this process, and
    // Discord.Net parses and retains them (MessageCacheSize per channel) even
    // though nothing here handles messages — pure resident memory on a container
    // that is billed by the MB.
    GatewayIntents = GatewayIntents.Guilds,

    // Message caching disabled for the same reason: no code path in this bot reads
    // a cached message, and the default (100 messages per channel) accumulates for
    // the whole lifetime of the process.
    MessageCacheSize = 0,

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

    // The API routes are rooted at /api. An environment override that provides
    // only the site origin must not silently turn lookups into /players/... 404s.
    var configuredBaseUrl = builder.Configuration["Api:BaseUrl"];
    if (!string.IsNullOrWhiteSpace(configuredBaseUrl))
    {
        var configuredUri = new Uri(configuredBaseUrl.Trim(), UriKind.Absolute);
        client.BaseAddress = new Uri(configuredUri.GetLeftPart(UriPartial.Authority) + "/api/");
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

// Health reflects the actual Discord gateway state, not just the HTTP server.
// A 503 tells Railway (or any monitor) the bot is NOT actually available, and with
// a restart policy / health-check-based restart this recovers dead connections.
app.MapGet("/health", (DiscordBotService bot) => bot.IsGatewayHealthy
    ? Results.Ok(new { status = "healthy", bot = bot.BotUsername, latencyMs = bot.GatewayLatencyMs })
    : Results.Json(new { status = "unhealthy", bot = bot.BotUsername }, statusCode: 503));

app.Run();
