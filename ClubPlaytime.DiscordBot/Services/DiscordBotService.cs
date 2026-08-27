using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using ClubPlaytime.DiscordBot.Modules;

namespace ClubPlaytime.DiscordBot.Services;

public sealed class DiscordBotService : IHostedService
{
    private readonly DiscordSocketClient _client;
    private readonly InteractionService _interactions;
    private readonly IServiceProvider _services;
    private readonly ILogger<DiscordBotService> _logger;
    private readonly IConfiguration _configuration;

    public DiscordBotService(
        DiscordSocketClient client,
        InteractionService interactions,
        IServiceProvider services,
        ILogger<DiscordBotService> logger,
        IConfiguration configuration)
    {
        _client = client;
        _interactions = interactions;
        _services = services;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // Check if the API base URL is configured
        var apiClient = _services.GetRequiredService<PlaytimeApiClient>();
        if (!apiClient.IsConfigured)
        {
            _logger.LogWarning(
                "ClubPlaytime API base URL is not configured. Set Api:BaseUrl in appsettings.json " +
                "(e.g. \"http://localhost:5121/api\"). Commands that need the API will fail.");
        }

        _client.Ready += OnReadyAsync;
        _client.Log += OnLogAsync;
        _client.InteractionCreated += OnInteractionCreatedAsync;

        await _interactions.AddModuleAsync<PlaytimeModule>(_services);

        var token = _configuration["Discord:Token"];
        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.LogError(
                "Discord bot token is missing! Set Discord:Token in appsettings.json with your bot token " +
                "from https://discord.com/developers/applications");
            return;
        }

        await _client.LoginAsync(TokenType.Bot, token);
        await _client.StartAsync();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _client.StopAsync();
        await _client.LogoutAsync();
    }

    private async Task OnReadyAsync()
    {
        _logger.LogInformation("Discord bot connected as {BotUsername}", _client.CurrentUser.Username);

        // Register commands from modules, then remove any stale commands
        try
        {
            var guildId = _configuration["Discord:GuildId"];
            if (ulong.TryParse(guildId, out var guildUlong))
            {
                // Delete ALL global commands first — we only use guild commands
                await DeleteAllGlobalCommandsAsync();
                await _interactions.RegisterCommandsToGuildAsync(guildUlong);
                _logger.LogInformation("Registered commands to guild {GuildId}", guildId);
                await RemoveStaleGuildCommandsAsync(guildUlong);
            }
            else
            {
                await _interactions.RegisterCommandsGloballyAsync();
                _logger.LogInformation("Registered commands globally.");
                await RemoveStaleGlobalCommandsAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register commands.");
        }
    }

    private async Task RemoveStaleGuildCommandsAsync(ulong guildId)
    {
        var validNames = new HashSet<string>(_interactions.SlashCommands.Select(s => s.Name), StringComparer.OrdinalIgnoreCase);
        var existing = await _client.Rest.GetGuildApplicationCommands(guildId);
        foreach (var cmd in existing)
        {
            if (!validNames.Contains(cmd.Name))
            {
                _logger.LogInformation("Removing stale guild command: {CommandName}", cmd.Name);
                await cmd.DeleteAsync();
            }
        }
    }

    private async Task RemoveStaleGlobalCommandsAsync()
    {
        var validNames = new HashSet<string>(_interactions.SlashCommands.Select(s => s.Name), StringComparer.OrdinalIgnoreCase);
        var existing = await _client.GetGlobalApplicationCommandsAsync();
        foreach (var cmd in existing)
        {
            if (!validNames.Contains(cmd.Name))
            {
                _logger.LogInformation("Removing stale global command: {CommandName}", cmd.Name);
                await cmd.DeleteAsync();
            }
        }
    }

    private async Task DeleteAllGlobalCommandsAsync()
    {
        var existing = await _client.GetGlobalApplicationCommandsAsync();
        foreach (var cmd in existing)
        {
            _logger.LogInformation("Deleting global command: {CommandName}", cmd.Name);
            await cmd.DeleteAsync();
        }
    }

    private Task OnLogAsync(LogMessage message)
    {
        switch (message.Severity)
        {
            case LogSeverity.Critical:
            case LogSeverity.Error:
                _logger.LogError(message.Exception, "{Source}: {Message}", message.Source, message.Message);
                break;
            case LogSeverity.Warning:
                _logger.LogWarning(message.Exception, "{Source}: {Message}", message.Source, message.Message);
                break;
            case LogSeverity.Info:
                _logger.LogInformation("{Source}: {Message}", message.Source, message.Message);
                break;
            default:
                _logger.LogDebug("{Source}: {Message}", message.Source, message.Message);
                break;
        }
        return Task.CompletedTask;
    }

    private async Task OnInteractionCreatedAsync(SocketInteraction interaction)
    {
        var ctx = new SocketInteractionContext(_client, interaction);
        await _interactions.ExecuteCommandAsync(ctx, _services);
    }
}
