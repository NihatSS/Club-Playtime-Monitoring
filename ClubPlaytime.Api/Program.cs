using System.Text;
using ClubPlaytime.Api.BackgroundServices;
using ClubPlaytime.Api.Data;
using ClubPlaytime.Api.Models;
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
        var connString = builder.Configuration.GetConnectionString("DefaultConnection")
                         ?? "Data Source=club-playtime.db";

        // Pin relative SQLite paths to the content root so every launch opens the SAME
        // database file regardless of the working directory the process was started
        // from. Previously a relative "club-playtime.db" resolved against the process
        // CWD, which silently switched between the repo-root copy and the API copy and
        // caused "user exists in one DB but not the other" for the Discord bot.
        var sourceMatch = System.Text.RegularExpressions.Regex.Match(
            connString, @"Data\s*Source\s*=\s*([^;]+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (sourceMatch.Success
            && !string.IsNullOrWhiteSpace(sourceMatch.Groups[1].Value)
            && !sourceMatch.Groups[1].Value.StartsWith(":") // not :memory:
            && !Path.IsPathRooted(sourceMatch.Groups[1].Value)
            && !sourceMatch.Groups[1].Value.Contains(':')) // not a different drive / URI
        {
            var absolute = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, sourceMatch.Groups[1].Value));
            connString = connString.Replace(sourceMatch.Groups[1].Value, absolute);
        }

        options.UseSqlite(connString);
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
builder.Services.AddHttpClient<IRobloxProfileClient, RobloxProfileClient>(client =>
{
    client.DefaultRequestHeaders.UserAgent.ParseAdd("ClubPlaytimeTracker/1.0");
    client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
    client.Timeout = TimeSpan.FromSeconds(20);
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
        // PostgreSQL: create a new schema from the model, then apply the small,
        // additive compatibility upgrade below for databases that already existed.
        // EnsureCreated intentionally does not update an existing database.
        dbContext.Database.EnsureCreated();
        ApplyPostgresAccountLinkSchema(dbContext);
    }
    else
    {
        // SQLite / SqlServer: apply existing migrations
        dbContext.Database.Migrate();
    }

    // Seed matiaspro admin user if it doesn't exist yet
    if (!dbContext.Users.Any(u => u.Username == "matiaspro"))
    {
        dbContext.Users.Add(new User
        {
            Username = "matiaspro",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Playtimetracker123_"),
            Role = "Admin",
            CreatedAt = DateTime.UtcNow
        });
        dbContext.SaveChanges();
    }
}

// EF's checked-in migrations target SQLite.  The production PostgreSQL database
// was created independently, so EnsureCreated cannot add fields introduced after
// its first deployment.  This upgrade is deliberately additive: it neither
// changes nor deletes tracker players, playtime, clubs, or website accounts.
static void ApplyPostgresAccountLinkSchema(ClubPlaytimeDbContext dbContext)
{
    dbContext.Database.ExecuteSqlRaw("""
        ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "PlayerId" integer NULL;
        ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "DiscordUserId" character varying(100) NULL;

        CREATE TABLE IF NOT EXISTS "VerificationCodes" (
            "Id" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
            "RobloxUserId" bigint NOT NULL,
            "Code" character varying(32) NOT NULL,
            "CreatedAt" timestamp with time zone NOT NULL,
            "ExpiresAt" timestamp with time zone NOT NULL,
            "UsedAt" timestamp with time zone NULL,
            "ClaimToken" character varying(64) NULL,
            "ClaimedAt" timestamp with time zone NULL
        );

        CREATE UNIQUE INDEX IF NOT EXISTS "IX_Users_PlayerId" ON "Users" ("PlayerId") WHERE "PlayerId" IS NOT NULL;
        CREATE UNIQUE INDEX IF NOT EXISTS "IX_VerificationCodes_Code" ON "VerificationCodes" ("Code");
        CREATE INDEX IF NOT EXISTS "IX_VerificationCodes_RobloxUserId_CreatedAt" ON "VerificationCodes" ("RobloxUserId", "CreatedAt");
        """);

    dbContext.Database.ExecuteSqlRaw("""
        DO $$
        BEGIN
            IF NOT EXISTS (
                SELECT 1
                FROM pg_constraint
                WHERE conname = 'FK_Users_Players_PlayerId'
                  AND conrelid = '"Users"'::regclass
            ) THEN
                ALTER TABLE "Users"
                    ADD CONSTRAINT "FK_Users_Players_PlayerId"
                    FOREIGN KEY ("PlayerId") REFERENCES "Players" ("Id") ON DELETE SET NULL;
            END IF;
        END $$;
        """);
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
