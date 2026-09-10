using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using ClubPlaytime.DiscordBot.Modules;

namespace ClubPlaytime.DiscordBot.Services;

public sealed class DiscordBotService : IHostedService
{
    // How long the gateway may be down (or silent) before we kill the process and
    // let the host (e.g. Railway) restart it with a fresh connection.
    private static readonly TimeSpan DeadConnectionThreshold = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan WatchdogInterval = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan MaxTimeToFirstConnect = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan StartupGracePeriod = TimeSpan.FromSeconds(60);

    private readonly DiscordSocketClient _client;
    private readonly InteractionService _interactions;
    private readonly IServiceProvider _services;
    private readonly ILogger<DiscordBotService> _logger;
    private readonly IConfiguration _configuration;
    private readonly CancellationTokenSource _watchdogCts = new();

    private long _startedAtTicks;
    private long _lastConnectedTicks;
    private long _lastDisconnectedTicks;
    private long _lastHeartbeatTicks;
    private bool _started;

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

    /// <summary>
    /// True only when the Discord gateway is genuinely alive: connected AND heartbeats
    /// are still arriving. Used by the /health endpoint so the host can detect a
    /// process that is "up" while the bot is actually offline in Discord.
    /// </summary>
    public bool IsGatewayHealthy
    {
        get
        {
            if (!_started) return false;

            // Give the initial connection a grace window before reporting unhealthy.
            if (DateTime.UtcNow.Ticks - Interlocked.Read(ref _startedAtTicks) < StartupGracePeriod.Ticks)
                return true;

            if (_client.ConnectionState != ConnectionState.Connected) return false;

            var lastHeartbeat = Interlocked.Read(ref _lastHeartbeatTicks);
            if (lastHeartbeat == 0) return false; // connected but no heartbeat yet

            return DateTime.UtcNow.Ticks - lastHeartbeat < DeadConnectionThreshold.Ticks;
        }
    }

    public string? BotUsername => _client.CurrentUser?.Username;

    public int? GatewayLatencyMs =>
        _started && _client.ConnectionState == ConnectionState.Connected ? _client.Latency : null;

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

        _client.Log += OnLogAsync;
        _client.InteractionCreated += OnInteractionCreatedAsync;
        _client.Ready += OnReadyAsync;
        _client.Connected += OnConnectedAsync;
        _client.Disconnected += OnDisconnectedAsync;
        _client.LatencyUpdated += OnLatencyUpdatedAsync;

        await _interactions.AddModuleAsync<PlaytimeModule>(_services);

        var token = _configuration["Discord:Token"];
        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.LogError(
                "Discord bot token is missing! Set the Discord__Token environment variable (double " +
                "underscore, e.g. on Railway) or Discord:Token in appsettings.json with your bot token " +
                "from https://discord.com/developers/applications. Failing fast instead of starting offline.");
            throw new InvalidOperationException("Discord bot token is missing — the bot cannot connect without it.");
        }

        await _client.LoginAsync(TokenType.Bot, token);
        await _client.StartAsync();

        _started = true;
        Interlocked.Exchange(ref _startedAtTicks, DateTime.UtcNow.Ticks);

        // Watchdog in the background: if the gateway stays dead it kills the process,
        // so the host restarts us with a clean connection instead of sitting offline.
        _ = Task.Run(() => WatchdogLoopAsync(_watchdogCts.Token));
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _watchdogCts.Cancel();
        await _client.StopAsync();
        await _client.LogoutAsync();
    }

    private async Task WatchdogLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(WatchdogInterval, token);

                if (!_started) continue;

                if (_client.ConnectionState == ConnectionState.Connected)
                {
                    var lastHeartbeat = Interlocked.Read(ref _lastHeartbeatTicks);
                    if (lastHeartbeat == 0) continue; // initial handshake still in progress

                    var silentFor = TimeSpan.FromTicks(DateTime.UtcNow.Ticks - lastHeartbeat);
                    if (silentFor > DeadConnectionThreshold)
                    {
                        _logger.LogError(
                            "Gateway looks dead: state is Connected but no heartbeat for {Minutes:F1} minutes. " +
                            "Exiting so the host restarts the bot with a fresh connection.",
                            silentFor.TotalMinutes);
                        Environment.Exit(1);
                    }
                }
                else
                {
                    var lastConnected = Interlocked.Read(ref _lastConnectedTicks);
                    var now = DateTime.UtcNow.Ticks;

                    if (lastConnected == 0)
                    {
                        // Never managed a single successful connection.
                        var uptime = TimeSpan.FromTicks(now - Interlocked.Read(ref _startedAtTicks));
                        if (uptime > MaxTimeToFirstConnect)
                        {
                            _logger.LogError(
                                "Could not connect to the Discord gateway within {Minutes:F0} minutes of startup. " +
                                "Exiting so the host can retry.",
                                uptime.TotalMinutes);
                            Environment.Exit(1);
                        }
                    }
                    else
                    {
                        var downFor = TimeSpan.FromTicks(now - Interlocked.Read(ref _lastDisconnectedTicks));
                        if (downFor > DeadConnectionThreshold)
                        {
                            _logger.LogError(
                                "Gateway has been down for {Minutes:F1} minutes and Discord.Net gave up reconnecting. " +
                                "Exiting so the host restarts the bot.",
                                downFor.TotalMinutes);
                            Environment.Exit(1);
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Gateway watchdog iteration failed.");
            }
        }
    }

    private Task OnConnectedAsync()
    {
        Interlocked.Exchange(ref _lastConnectedTicks, DateTime.UtcNow.Ticks);
        Interlocked.Exchange(ref _lastHeartbeatTicks, DateTime.UtcNow.Ticks);
        _logger.LogInformation("Gateway connected.");
        return Task.CompletedTask;
    }

    private Task OnDisconnectedAsync(Exception? ex)
    {
        Interlocked.Exchange(ref _lastDisconnectedTicks, DateTime.UtcNow.Ticks);
        _logger.LogWarning(ex,
            "Gateway disconnected. Discord.Net will try to reconnect automatically; " +
            "the watchdog will restart the process if it cannot.");
        return Task.CompletedTask;
    }

    private Task OnLatencyUpdatedAsync(int oldLatency, int newLatency)
    {
        Interlocked.Exchange(ref _lastHeartbeatTicks, DateTime.UtcNow.Ticks);
        if (newLatency >= 500)
        {
            _logger.LogWarning("High gateway latency: {Latency}ms", newLatency);
        }
        return Task.CompletedTask;
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
