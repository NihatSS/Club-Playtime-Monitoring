using System.Text;
using ClubPlaytime.Api.BackgroundServices;
using ClubPlaytime.Api.Data;
using ClubPlaytime.Api.Options;
using ClubPlaytime.Api.Repositories;
using ClubPlaytime.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services.Configure<MonitoringOptions>(
    builder.Configuration.GetSection(MonitoringOptions.SectionName));

builder.Services.Configure<JwtOptions>(
    builder.Configuration.GetSection(JwtOptions.SectionName));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter your JWT token"
    });
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("ReactClient", policy =>
    {
        policy
            .SetIsOriginAllowed(_ => true) // Allow all origins for API access
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()!;
if (string.IsNullOrWhiteSpace(jwtSettings.SecretKey))
{
    throw new InvalidOperationException(
        "JWT SecretKey is not configured. Set the Jwt__SecretKey environment variable or update appsettings.json.");
}
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey))
    };
});
builder.Services.AddAuthorization();

var databaseProvider = builder.Configuration.GetValue<string>("Database:Provider") ?? "Sqlite";
builder.Services.AddDbContext<ClubPlaytimeDbContext>(options =>
{
    if (databaseProvider.Equals("Postgres", StringComparison.OrdinalIgnoreCase) ||
        databaseProvider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase))
    {
        var pgConn = builder.Configuration.GetConnectionString("PostgresConnection")
                     ?? builder.Configuration["DATABASE_URL"];

        // Neon/Railway may provide a URI like postgresql://user:pass@host/db?sslmode=require
        // Railway truncates it at '=' signs, so we parse and rebuild as key=value format
        if (!string.IsNullOrEmpty(pgConn) && pgConn.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        {
            var uri = new Uri(pgConn);
            var host = uri.Host;
            var port = uri.Port > 0 ? uri.Port : 5432;
            var database = uri.AbsolutePath.TrimStart('/');
            var userInfo = uri.UserInfo.Split(':');
            var username = userInfo.Length > 0 ? Uri.UnescapeDataString(userInfo[0]) : "";
            var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "";
            pgConn = $"Host={host};Port={port};Database={database};Username={username};Password={password};SSL Mode=require;Trust Server Certificate=true";
        }

        options.UseNpgsql(pgConn);
    }
    else if (databaseProvider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
    {
        options.UseSqlServer(builder.Configuration.GetConnectionString("SqlServer"));
    }
    else
    {
        options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"));
    }
});

builder.Services.AddScoped<IPlayerRepository, PlayerRepository>();
builder.Services.AddScoped<IDailyPlaytimeRepository, DailyPlaytimeRepository>();
builder.Services.AddScoped<IActivityRepository, ActivityRepository>();
builder.Services.AddScoped<IPlayerStatsService, PlayerStatsService>();
builder.Services.AddHttpClient<IRobloxPresenceClient, RobloxPresenceClient>(client =>
{
    client.DefaultRequestHeaders.UserAgent.ParseAdd("ClubPlaytimeTracker/1.0");
    client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
    client.Timeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddHttpClient<IRobloxAvatarClient, RobloxAvatarClient>();
builder.Services.AddHttpClient<IDiscordNotifier, DiscordNotifier>();
builder.Services.AddSingleton<IPlayerMonitorRunner, PlayerMonitorRunner>();
builder.Services.AddHostedService<PlayerMonitoringHostedService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ClubPlaytimeDbContext>();
    if (databaseProvider.Equals("Postgres", StringComparison.OrdinalIgnoreCase) ||
        databaseProvider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase))
    {
        // PostgreSQL: create schema from model (existing migrations are SQLite-specific)
        dbContext.Database.EnsureCreated();
    }
    else
    {
        // SQLite / SqlServer: apply existing migrations
        dbContext.Database.Migrate();
    }

    // Fix inflated playtime: cap daily records to 24h and recalculate totals.
    const long maxDailySeconds = 86_400;
    var dailyRows = await dbContext.DailyPlaytime.ToListAsync();
    var cappedCount = 0;
    foreach (var row in dailyRows)
    {
        if (row.PlaySeconds > maxDailySeconds)
        {
            row.PlaySeconds = maxDailySeconds;
            cappedCount++;
        }
    }

    var playerTotals = await dbContext.DailyPlaytime
        .GroupBy(d => d.PlayerId)
        .Select(g => new { PlayerId = g.Key, Total = g.Sum(d => d.PlaySeconds) })
        .ToListAsync();
    var players = await dbContext.Players.ToListAsync();
    var recalcCount = 0;
    foreach (var player in players)
    {
        var correctTotal = playerTotals.FirstOrDefault(t => t.PlayerId == player.Id)?.Total ?? 0;
        if (player.TotalPlaySeconds != correctTotal)
        {
            player.TotalPlaySeconds = correctTotal;
            recalcCount++;
        }
    }

    if (cappedCount > 0 || recalcCount > 0)
    {
        await dbContext.SaveChangesAsync();
        app.Logger.LogInformation("Fixed playtime: {Capped} daily records capped, {Recalc} player totals recalculated", cappedCount, recalcCount);
    }
    else
    {
        app.Logger.LogInformation("No inflated playtime records found.");
    }
}

app.UseHttpsRedirection();
app.UseDefaultFiles(); // Serve index.html by default
app.UseStaticFiles(); // Serve static files from wwwroot
app.UseCors("ReactClient");
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();
