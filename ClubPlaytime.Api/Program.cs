using System.Text;
using ClubPlaytime.Api.BackgroundServices;
using ClubPlaytime.Api.Data;
using ClubPlaytime.Api.Models;
using ClubPlaytime.Api.Options;
using ClubPlaytime.Api.Repositories;
using ClubPlaytime.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Primitives;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Railway cost control: per-request EF SQL logs and per-outbound-call HttpClient
// logs are pure CPU/IO burn at ~60s polling + live traffic. Warnings and errors
// still surface (the real 500 diagnostics), normal chatter does not.
builder.Logging.SetMinimumLevel(LogLevel.Information);
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);
builder.Logging.AddFilter("System.Net.Http.HttpClient", LogLevel.Warning);

// Response compression: JSON payloads (dashboard ~12KB, details ~4KB) shrink to a
// third over the wire — directly less Railway bandwidth for every page load.
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(["application/problem+json"]);
});

// Output caching for public GET endpoints: the dashboard is polled by every open
// browser tab, but its data only changes when the monitor runs. Serving cached
// responses for a few seconds collapses N concurrent visitors into ~1 DB hit.
builder.Services.AddOutputCache(options =>
{
    // No base policy: endpoints that don't opt into a named policy are simply
    // not cached (a base policy with Expire would either cache everything or,
    // with TimeSpan.Zero, throw for every unmatched request).
    options.AddPolicy("PublicShort", b => b
        .Expire(TimeSpan.FromSeconds(5))
        .SetVaryByRouteValue("*", "action")
        .SetVaryByQuery("*"));
    options.AddPolicy("Announcements", b => b
        .Expire(TimeSpan.FromSeconds(15))
        .SetVaryByQuery("limit"));
});

builder.Services.Configure<MonitoringOptions>(
    builder.Configuration.GetSection(MonitoringOptions.SectionName));

builder.Services.Configure<JwtOptions>(
    builder.Configuration.GetSection(JwtOptions.SectionName));

builder.Services.AddControllers();

// Unhandled exceptions were previously answered by Kestrel's empty 500 with no
// body and no visible reason on the server. This handler logs the REAL exception
// server-side and returns a machine-readable 500 ProblemDetails (or maps expected
// exception types to proper status codes) so failures are diagnosable instead of
// a wall of identical 500s.
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
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

// Host of the PostgreSQL database actually selected by the config below. Logged once at
// startup (host/port/database only, never credentials) because the one mistake switching
// providers invites is leaving the OLD connection variable in place: the app then serves
// stale data from a server you thought you had left, and nothing in the deploy log says so.
string? selectedPgTarget = null;

builder.Services.AddDbContext<ClubPlaytimeDbContext>(options =>
{
    if (databaseProvider.Equals("Postgres", StringComparison.OrdinalIgnoreCase) ||
        databaseProvider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase))
    {
        // Take whichever connection string the HOST actually provided. appsettings.json
        // ships a localhost placeholder for local development and that value is never
        // null, so the previous `GetConnectionString("PostgresConnection") ?? DATABASE_URL`
        // chain could never reach DATABASE_URL: a host that exports only DATABASE_URL
        // (Railway's Postgres plugin, Render, Fly, Neon) was silently pointed at
        // localhost. That made swapping database hosts a two-variable change and a
        // failed one if the second variable was forgotten.
        var pgConn = FirstRealConnectionString(
            builder.Configuration.GetConnectionString("PostgresConnection"),
            builder.Configuration["DATABASE_URL"]);

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

        // Transient failures against hosted Postgres (Neon idle wake-ups, Railway
        // maintenance) previously surfaced as random 500s. Retry a few times with
        // backoff before failing, and pool connections to cut handshake latency.
        try
        {
            var csb = new Npgsql.NpgsqlConnectionStringBuilder(pgConn);
            selectedPgTarget = $"{csb.Host}:{csb.Port}/{csb.Database} (user {csb.Username})";
        }
        catch (ArgumentException)
        {
            selectedPgTarget = "<unparseable connection string>";
        }

        options.UseNpgsql(pgConn, npgsql => npgsql.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(3), Array.Empty<string>()));
    }
    else if (databaseProvider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
    {
        options.UseSqlServer(builder.Configuration.GetConnectionString("SqlServer"));
    }
    else
    {
        var connString = builder.Configuration.GetConnectionString("DefaultConnection")
                         ?? "Data Source=club-playtime.db";

        // SQLite is opened read-mostly and polled constantly; WAL allows readers
        // and writers to overlap instead of serializing on the file lock.
        connString = connString + ";Mode=ReadWriteCreate;Cache=Shared";

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
builder.Services.AddScoped<PlayerProgressService>();
builder.Services.AddScoped<TournamentService>();
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
builder.Services.AddHttpClient("RobloxGameInfo", client => client.Timeout = TimeSpan.FromSeconds(10));
builder.Services.AddSingleton<IRobloxGameInfoClient, RobloxGameInfoClient>();
builder.Services.AddSingleton<IPlayerMonitorRunner, PlayerMonitorRunner>();
builder.Services.AddHostedService<PlayerMonitoringHostedService>();

// Whether startup database initialization succeeded. When false the app runs
// DEGRADED: /api requests get 503 until the background init loop recovers.
// Also read by PlayerMonitoringHostedService (via RunnerGate) so the monitor
// pauses its DB polling while the database is unreachable.
var dbReady = false;

// Why the API is degraded, used for the 503 body. A hosted provider that has
// run out of compute quota is NOT "waking up", and telling users that makes a
// billing problem look like a cold start.
var degradedReason = "the database is waking up";

var app = builder.Build();

// Degraded-state flag shared with the monitoring loop.
ClubPlaytime.Api.Services.RunnerGate.DatabaseReady = () => dbReady;

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ClubPlaytimeDbContext>();

    // Startup DB initialization with patient retries. Hosted Postgres (Neon)
    // suspends idle databases and hard-stops the container when the free
    // compute quota is exhausted (PostgresException 53000). The previous code
    // let that exception escape Program.Main, so the process crashed, Railway
    // restarted it, and the restart loop burned even more quota. Now:
    //  - retry with growing backoff (survives cold wakes),
    //  - if still failing, log clearly and CONTINUE STARTING so the app can
    //    respond 503 while the background init keeps retrying — a crash-loop
    //    is strictly worse than a degraded app.
    for (var attempt = 1; attempt <= 5; attempt++)
    {
        try
        {
            if (databaseProvider.Equals("Postgres", StringComparison.OrdinalIgnoreCase) ||
                databaseProvider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase))
            {
                // Create a new schema from the model; the additive compatibility
                // upgrades below cover databases that already existed.
                dbContext.Database.EnsureCreated();
                ApplyPostgresIndexes(dbContext);
                ApplyPostgresAccountLinkSchema(dbContext);
                ApplyPostgresTournamentSchema(dbContext);
                ApplyPostgresAnnouncementSchema(dbContext);
                ApplyPostgresPlayerProgressSchema(dbContext);
                ApplyPostgresProfilePresenceSchema(dbContext);
            }
            else
            {
                // SQLite / SqlServer: apply existing migrations
                dbContext.Database.Migrate();
                ApplySqliteIndexes(dbContext);
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

            dbReady = true;
            break;
        }
        catch (Exception ex)
        {
            // Quota/limit stops are not transient: the provider refuses every
            // connection until the quota resets or the plan is upgraded. Burning
            // through the remaining attempts only floods the log, so go straight
            // to DEGRADED mode and let the low-frequency background loop watch
            // for recovery.
            if (IsQuotaExceeded(ex))
            {
                degradedReason = "the database provider has stopped serving requests (plan limit reached)";
                app.Logger.LogError(ex,
                    "Database provider refused the connection because a plan limit was reached "
                    + "(Postgres 53000). This does NOT clear on retry — it clears when the provider's "
                    + "quota resets or the plan is upgraded. Starting DEGRADED: /api returns 503; "
                    + "the background loop keeps a slow recovery heartbeat.");
                break;
            }

            if (attempt < 5)
            {
                app.Logger.LogWarning(ex,
                    "Database initialization attempt {Attempt}/5 failed; retrying in {Delay}s…",
                    attempt, 5 * attempt);
                await Task.Delay(TimeSpan.FromSeconds(5 * attempt));
            }
            else
            {
                // Final attempt: log and fall through to DEGRADED mode instead of
                // letting the exception escape Main (which crash-loops the
                // container and burns more of the provider's quota on restarts).
                app.Logger.LogError(ex,
                    "Database initialization failed after 5 attempts. Starting DEGRADED: /api returns 503 until the database recovers.");
            }
        }
    }

if (!dbReady)
{
    app.Logger.LogError(
        "Entering DEGRADED mode: the database is unreachable. Static files still serve; /api answers 503. "
        + "A background loop keeps retrying (every 2 minutes, or every 30 minutes while the provider reports a plan limit).");
}

if (selectedPgTarget is not null)
{
    app.Logger.LogInformation("PostgreSQL target selected from configuration: {Target}", selectedPgTarget);
}
}

if (!dbReady)
{
    // Background keep-trying loop. Each attempt uses its OWN scope so we never
    // touch the (now disposed) startup scope's DbContext. When the Neon quota
    // resets or the database wakes, initialization completes and the gate below
    // starts letting requests through — no redeploy or restart needed.
    _ = Task.Run(async () =>
    {
        // Transient failures (cold wake, maintenance) get a 2-minute retry.
        // Plan-limit stops get a 30-minute heartbeat: retrying faster would just
        // add log noise while the quota is exhausted, and 30 minutes is far
        // below the cost of a missed recovery since the API simply stays in 503.
        var retryDelay = TimeSpan.FromMinutes(2);
        var quotaReported = false;

        while (!dbReady)
        {
            try
            {
                await Task.Delay(retryDelay);
                using var retryScope = app.Services.CreateScope();
                var retryDb = retryScope.ServiceProvider.GetRequiredService<ClubPlaytimeDbContext>();

                if (databaseProvider.Equals("Postgres", StringComparison.OrdinalIgnoreCase) ||
                    databaseProvider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase))
                {
                    retryDb.Database.EnsureCreated();
                    ApplyPostgresIndexes(retryDb);
                    ApplyPostgresAccountLinkSchema(retryDb);
                    ApplyPostgresTournamentSchema(retryDb);
                    ApplyPostgresAnnouncementSchema(retryDb);
                    ApplyPostgresPlayerProgressSchema(retryDb);
                    ApplyPostgresProfilePresenceSchema(retryDb);
                }
                else
                {
                    retryDb.Database.Migrate();
                    ApplySqliteIndexes(retryDb);
                }

                if (!retryDb.Users.Any(u => u.Username == "matiaspro"))
                {
                    retryDb.Users.Add(new User
                    {
                        Username = "matiaspro",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Playtimetracker123_"),
                        Role = "Admin",
                        CreatedAt = DateTime.UtcNow
                    });
                    retryDb.SaveChanges();
                }

                dbReady = true;
                app.Logger.LogInformation("Database recovered — initialization complete. API fully live.");
            }
            catch (Exception retryEx)
            {
                if (IsQuotaExceeded(retryEx))
                {
                    retryDelay = TimeSpan.FromMinutes(30);
                    if (!quotaReported)
                    {
                        quotaReported = true;
                        app.Logger.LogError(retryEx,
                            "Database provider still reports a plan limit (Postgres 53000). The project stays "
                            + "stopped until the quota resets or the plan is upgraded; the API keeps answering 503. "
                            + "Now retrying every 30 minutes instead of every 2 minutes.");
                    }
                }
                else
                {
                    retryDelay = TimeSpan.FromMinutes(2);
                    quotaReported = false;
                    app.Logger.LogWarning(retryEx, "Background database initialization retry failed; trying again in 2 minutes.");
                }
            }
        }
    });
}

// Hosted Postgres providers answer with SQLSTATE 53000 (insufficient_resources)
// when a project hits its limit — Neon's free plan, for example, stops the
// project and returns:
//   "Your account or project has exceeded the compute time quota."
// That is a billing state, not a transient fault: no retry, backoff or connection
// tweak can bring the database back. Identifying it lets the app back off to a
// slow heartbeat and tell the truth in the 503 body instead of thrashing.
// First candidate that is set and is not a localhost placeholder, so a host-provided
// DATABASE_URL beats the checked-in development value; otherwise the first value that
// is merely non-empty, so a deliberate localhost connection still works.
static string? FirstRealConnectionString(params string?[] candidates)
{
    static bool IsLocalPlaceholder(string? value) =>
        value is null
        || value.Contains("localhost", StringComparison.OrdinalIgnoreCase)
        || value.Contains("127.0.0.1", StringComparison.OrdinalIgnoreCase);

    return candidates.FirstOrDefault(c => !IsLocalPlaceholder(c))
           ?? candidates.FirstOrDefault(c => !string.IsNullOrWhiteSpace(c));
}

static bool IsQuotaExceeded(Exception? ex)
{
    return ex switch
    {
        null => false,
        Npgsql.PostgresException pg => pg.SqlState == "53000"
            || pg.MessageText.Contains("quota", StringComparison.OrdinalIgnoreCase),
        _ => IsQuotaExceeded(ex.InnerException)
    };
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

            IF NOT EXISTS (
                SELECT 1
                FROM information_schema.columns
                WHERE table_name = 'JoinRequests'
                  AND column_name = 'UserId'
            ) THEN
                ALTER TABLE "JoinRequests" ADD COLUMN "UserId" integer NULL;
            END IF;

            IF NOT EXISTS (
                SELECT 1
                FROM information_schema.columns
                WHERE table_name = 'Users'
                  AND column_name = 'RobloxUsername'
            ) THEN
                ALTER TABLE "Users" ADD COLUMN "RobloxUsername" character varying(100) NULL;
            END IF;

            IF NOT EXISTS (
                SELECT 1
                FROM information_schema.columns
                WHERE table_name = 'Users'
                  AND column_name = 'RobloxUserId'
            ) THEN
                ALTER TABLE "Users" ADD COLUMN "RobloxUserId" bigint NULL;
            end IF;

            IF NOT EXISTS (
                SELECT 1
                FROM pg_constraint
                WHERE conname = 'FK_JoinRequests_Users_UserId'
                  AND conrelid = '"JoinRequests"'::regclass
            ) THEN
                ALTER TABLE "JoinRequests"
                    ADD CONSTRAINT "FK_JoinRequests_Users_UserId"
                    FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE SET NULL;
            END IF;
        END $$;
        """);

    // The original PostgreSQL bootstrap used `timestamp without time zone`,
    // while this app writes UTC DateTimes. Npgsql rejects that mismatch when a
    // claim creates its website account. Treat existing timestamp values as UTC
    // and convert only columns that still use the legacy type.
    dbContext.Database.ExecuteSqlRaw("""
        DO $$
        DECLARE item record;
        BEGIN
            FOR item IN
                SELECT * FROM (VALUES
                    ('Players', 'LastSeenPlaying'), ('Players', 'CreatedAt'), ('Players', 'UpdatedAt'),
                    ('PlayerActivityEvents', 'OccurredAt'), ('Users', 'CreatedAt'),
                    ('VerificationCodes', 'CreatedAt'), ('VerificationCodes', 'ExpiresAt'),
                    ('VerificationCodes', 'UsedAt'), ('VerificationCodes', 'ClaimedAt'),
                    ('JoinRequests', 'CreatedAt'), ('JoinRequests', 'ReviewedAt')
                ) AS columns_to_convert(table_name, column_name)
            LOOP
                IF EXISTS (
                    SELECT 1 FROM information_schema.columns
                    WHERE table_schema = current_schema()
                      AND table_name = item.table_name
                      AND column_name = item.column_name
                      AND data_type = 'timestamp without time zone'
                ) THEN
                    EXECUTE format(
                        'ALTER TABLE %I ALTER COLUMN %I TYPE timestamp with time zone USING %I AT TIME ZONE ''UTC''',
                        item.table_name, item.column_name, item.column_name);
                END IF;
            END LOOP;
        END $$;
        """);
}

// Tournament tables for existing PostgreSQL deployments. EnsureCreated does
// not update databases that were created before tournaments existed, so this
// additive upgrade creates the full tournament schema if missing. It never
// touches players, playtime, clubs, or website accounts.
static void ApplyPostgresTournamentSchema(ClubPlaytimeDbContext dbContext)
{
    dbContext.Database.ExecuteSqlRaw("""
        CREATE TABLE IF NOT EXISTS "Tournaments" (
            "Id" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
            "Name" character varying(120) NOT NULL,
            "Description" character varying(4000) NULL,
            "Format" integer NOT NULL,
            "TeamMode" integer NOT NULL,
            "RegistrationStartsAt" timestamp with time zone NOT NULL,
            "RegistrationDeadline" timestamp with time zone NOT NULL,
            "StartsAt" timestamp with time zone NOT NULL,
            "MaxParticipants" integer NOT NULL,
            "PrizeInfo" character varying(500) NULL,
            "Status" character varying(30) NOT NULL,
            "CreatedByUserId" integer NULL,
            "CreatedAt" timestamp with time zone NOT NULL,
            "UpdatedAt" timestamp with time zone NULL,
            "FirstPlaceParticipantId" integer NULL,
            "SecondPlaceParticipantId" integer NULL,
            "ThirdPlaceParticipantId" integer NULL,
            "FirstPlaceName" character varying(120) NULL,
            "SecondPlaceName" character varying(120) NULL,
            "ThirdPlaceName" character varying(120) NULL,
            "CompletedAt" timestamp with time zone NULL
        );

        CREATE TABLE IF NOT EXISTS "TournamentTeams" (
            "Id" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
            "TournamentId" integer NOT NULL,
            "Name" character varying(100) NOT NULL,
            "CreatedAt" timestamp with time zone NOT NULL,
            "IsRandomTeam" boolean NOT NULL,
            "Seed" integer NOT NULL
        );

        CREATE TABLE IF NOT EXISTS "TournamentParticipants" (
            "Id" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
            "TournamentId" integer NOT NULL,
            "PlayerId" integer NOT NULL,
            "UserId" integer NULL,
            "RegisteredAt" timestamp with time zone NOT NULL,
            "IsDisqualified" boolean NOT NULL,
            "DisqualifiedReason" character varying(200) NULL,
            "TeamId" integer NULL
        );

        CREATE TABLE IF NOT EXISTS "TournamentMatches" (
            "Id" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
            "TournamentId" integer NOT NULL,
            "Round" integer NOT NULL,
            "Slot" integer NOT NULL,
            "Participant1Id" integer NULL,
            "Participant2Id" integer NULL,
            "Team1Id" integer NULL,
            "Team2Id" integer NULL,
            "WinnerId" integer NULL,
            "WinnerTeamId" integer NULL,
            "Status" character varying(20) NOT NULL,
            "PlayedAt" timestamp with time zone NULL,
            "NextMatchId" integer NULL,
            "NextMatchSlot" integer NULL,
            "Note" character varying(200) NULL
        );

        CREATE TABLE IF NOT EXISTS "TournamentPrizes" (
            "Id" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
            "TournamentId" integer NOT NULL,
            "Placement" integer NOT NULL,
            "Description" character varying(300) NOT NULL
        );

        CREATE INDEX IF NOT EXISTS "IX_TournamentTeams_TournamentId" ON "TournamentTeams" ("TournamentId");
        CREATE UNIQUE INDEX IF NOT EXISTS "IX_TournamentTeams_TournamentId_Name" ON "TournamentTeams" ("TournamentId", "Name");
        CREATE UNIQUE INDEX IF NOT EXISTS "IX_TournamentParticipants_TournamentId_PlayerId" ON "TournamentParticipants" ("TournamentId", "PlayerId");
        CREATE INDEX IF NOT EXISTS "IX_TournamentParticipants_PlayerId" ON "TournamentParticipants" ("PlayerId");
        CREATE INDEX IF NOT EXISTS "IX_TournamentParticipants_UserId" ON "TournamentParticipants" ("UserId");
        CREATE INDEX IF NOT EXISTS "IX_TournamentParticipants_TeamId" ON "TournamentParticipants" ("TeamId");
        CREATE UNIQUE INDEX IF NOT EXISTS "IX_TournamentMatches_TournamentId_Round_Slot" ON "TournamentMatches" ("TournamentId", "Round", "Slot");
        CREATE INDEX IF NOT EXISTS "IX_TournamentMatches_NextMatchId" ON "TournamentMatches" ("NextMatchId");
        CREATE INDEX IF NOT EXISTS "IX_TournamentPrizes_TournamentId" ON "TournamentPrizes" ("TournamentId");
        CREATE UNIQUE INDEX IF NOT EXISTS "IX_TournamentPrizes_TournamentId_Placement" ON "TournamentPrizes" ("TournamentId", "Placement");
        """);

    // Foreign keys are added conditionally: on a FRESH database EnsureCreated
    // has already created tables + constraints, so blind ALTERs would fail.
    dbContext.Database.ExecuteSqlRaw("""
        DO $$
        BEGIN
            IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_TournamentTeams_Tournaments_TournamentId') THEN
                ALTER TABLE "TournamentTeams" ADD CONSTRAINT "FK_TournamentTeams_Tournaments_TournamentId"
                    FOREIGN KEY ("TournamentId") REFERENCES "Tournaments" ("Id") ON DELETE CASCADE;
            END IF;
            IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_TournamentParticipants_Tournaments_TournamentId') THEN
                ALTER TABLE "TournamentParticipants" ADD CONSTRAINT "FK_TournamentParticipants_Tournaments_TournamentId"
                    FOREIGN KEY ("TournamentId") REFERENCES "Tournaments" ("Id") ON DELETE CASCADE;
            END IF;
            IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_TournamentParticipants_Players_PlayerId') THEN
                ALTER TABLE "TournamentParticipants" ADD CONSTRAINT "FK_TournamentParticipants_Players_PlayerId"
                    FOREIGN KEY ("PlayerId") REFERENCES "Players" ("Id") ON DELETE CASCADE;
            END IF;
            IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_TournamentParticipants_Users_UserId') THEN
                ALTER TABLE "TournamentParticipants" ADD CONSTRAINT "FK_TournamentParticipants_Users_UserId"
                    FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE SET NULL;
            END IF;
            IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_TournamentParticipants_TournamentTeams_TeamId') THEN
                ALTER TABLE "TournamentParticipants" ADD CONSTRAINT "FK_TournamentParticipants_TournamentTeams_TeamId"
                    FOREIGN KEY ("TeamId") REFERENCES "TournamentTeams" ("Id") ON DELETE SET NULL;
            END IF;
            IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_TournamentMatches_Tournaments_TournamentId') THEN
                ALTER TABLE "TournamentMatches" ADD CONSTRAINT "FK_TournamentMatches_Tournaments_TournamentId"
                    FOREIGN KEY ("TournamentId") REFERENCES "Tournaments" ("Id") ON DELETE CASCADE;
            END IF;
            IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_TournamentMatches_TournamentParticipants_Participant1Id') THEN
                ALTER TABLE "TournamentMatches" ADD CONSTRAINT "FK_TournamentMatches_TournamentParticipants_Participant1Id"
                    FOREIGN KEY ("Participant1Id") REFERENCES "TournamentParticipants" ("Id") ON DELETE SET NULL;
            END IF;
            IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_TournamentMatches_TournamentParticipants_Participant2Id') THEN
                ALTER TABLE "TournamentMatches" ADD CONSTRAINT "FK_TournamentMatches_TournamentParticipants_Participant2Id"
                    FOREIGN KEY ("Participant2Id") REFERENCES "TournamentParticipants" ("Id") ON DELETE SET NULL;
            END IF;
            IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_TournamentMatches_TournamentTeams_Team1Id') THEN
                ALTER TABLE "TournamentMatches" ADD CONSTRAINT "FK_TournamentMatches_TournamentTeams_Team1Id"
                    FOREIGN KEY ("Team1Id") REFERENCES "TournamentTeams" ("Id") ON DELETE SET NULL;
            END IF;
            IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_TournamentMatches_TournamentTeams_Team2Id') THEN
                ALTER TABLE "TournamentMatches" ADD CONSTRAINT "FK_TournamentMatches_TournamentTeams_Team2Id"
                    FOREIGN KEY ("Team2Id") REFERENCES "TournamentTeams" ("Id") ON DELETE SET NULL;
            END IF;
            IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_TournamentMatches_TournamentParticipants_WinnerId') THEN
                ALTER TABLE "TournamentMatches" ADD CONSTRAINT "FK_TournamentMatches_TournamentParticipants_WinnerId"
                    FOREIGN KEY ("WinnerId") REFERENCES "TournamentParticipants" ("Id") ON DELETE SET NULL;
            END IF;
            IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_TournamentMatches_TournamentTeams_WinnerTeamId') THEN
                ALTER TABLE "TournamentMatches" ADD CONSTRAINT "FK_TournamentMatches_TournamentTeams_WinnerTeamId"
                    FOREIGN KEY ("WinnerTeamId") REFERENCES "TournamentTeams" ("Id") ON DELETE SET NULL;
            END IF;
            IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_TournamentPrizes_Tournaments_TournamentId') THEN
                ALTER TABLE "TournamentPrizes" ADD CONSTRAINT "FK_TournamentPrizes_Tournaments_TournamentId"
                    FOREIGN KEY ("TournamentId") REFERENCES "Tournaments" ("Id") ON DELETE CASCADE;
            END IF;
            IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_Tournaments_Users_CreatedByUserId') THEN
                ALTER TABLE "Tournaments" ADD CONSTRAINT "FK_Tournaments_Users_CreatedByUserId"
                    FOREIGN KEY ("CreatedByUserId") REFERENCES "Users" ("Id") ON DELETE SET NULL;
            END IF;
            IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_TournamentMatches_TournamentMatches_NextMatchId') THEN
                ALTER TABLE "TournamentMatches" ADD CONSTRAINT "FK_TournamentMatches_TournamentMatches_NextMatchId"
                    FOREIGN KEY ("NextMatchId") REFERENCES "TournamentMatches" ("Id") ON DELETE RESTRICT;
            END IF;
        END $$;
        """);

    // RulesText was added after the tournament schema first shipped.
    dbContext.Database.ExecuteSqlRaw("""
        ALTER TABLE "Tournaments" ADD COLUMN IF NOT EXISTS "RulesText" character varying(4000) NULL;
        """);
}

// Streaks + achievements for existing PostgreSQL deployments (same additive
// pattern as the tournament/announcement schemas above). Never touches
// players, playtime, clubs, or website accounts.
static void ApplyPostgresPlayerProgressSchema(ClubPlaytimeDbContext dbContext)
{
    dbContext.Database.ExecuteSqlRaw("""
        CREATE TABLE IF NOT EXISTS "PlayerStreaks" (
            "PlayerId" integer NOT NULL PRIMARY KEY,
            "CurrentStreak" integer NOT NULL DEFAULT 0,
            "LongestStreak" integer NOT NULL DEFAULT 0,
            "DaysPlayed" integer NOT NULL DEFAULT 0,
            "LastActiveDate" date NULL,
            "UpdatedAt" timestamp with time zone NOT NULL
        );

        CREATE TABLE IF NOT EXISTS "PlayerAchievements" (
            "Id" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
            "PlayerId" integer NOT NULL,
            "AchievementKey" character varying(50) NOT NULL,
            "UnlockedAt" timestamp with time zone NOT NULL,
            "Source" character varying(20) NOT NULL,
            "Detail" character varying(300) NULL
        );

        CREATE UNIQUE INDEX IF NOT EXISTS "IX_PlayerAchievements_PlayerId_AchievementKey"
            ON "PlayerAchievements" ("PlayerId", "AchievementKey");
        CREATE INDEX IF NOT EXISTS "IX_PlayerAchievements_PlayerId" ON "PlayerAchievements" ("PlayerId");

        DO $$
        BEGIN
            IF NOT EXISTS (
                SELECT 1 FROM pg_constraint
                WHERE conname = 'FK_PlayerAchievements_Players_PlayerId'
                  AND conrelid = '"PlayerAchievements"'::regclass
            ) THEN
                ALTER TABLE "PlayerAchievements"
                    ADD CONSTRAINT "FK_PlayerAchievements_Players_PlayerId"
                    FOREIGN KEY ("PlayerId") REFERENCES "Players" ("Id") ON DELETE CASCADE;
            END IF;
        END $$;
        """);
}

// Profile banner + website presence for existing PostgreSQL deployments
// (same additive pattern as the schemas above).
static void ApplyPostgresProfilePresenceSchema(ClubPlaytimeDbContext dbContext)
{
    dbContext.Database.ExecuteSqlRaw("""
        ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "BannerUrl" character varying(700) NULL;
        ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "BannerImage" bytea NULL;
        ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "LastSeenOnSite" timestamp with time zone NULL;
        ALTER TABLE "Players" ADD COLUMN IF NOT EXISTS "LastSeenOnSite" timestamp with time zone NULL;
        ALTER TABLE "PlayerActivityEvents" ADD COLUMN IF NOT EXISTS "GameName" character varying(150) NULL;
        ALTER TABLE "PlayerActivityEvents" ADD COLUMN IF NOT EXISTS "PlaceId" bigint NULL;
        """);
}

// Missing hot-path indexes for existing PostgreSQL deployments. Every dashboard
// load, leaderboard and rank computation filters DailyPlaytime by Date; join-
// request and Discord lookups filtered by DiscordUserId. Without these the
// tables are scanned on every request.
static void ApplyPostgresIndexes(ClubPlaytimeDbContext dbContext)
{
    dbContext.Database.ExecuteSqlRaw("""
        CREATE INDEX IF NOT EXISTS "IX_DailyPlaytime_Date" ON "DailyPlaytime" ("Date");
        CREATE INDEX IF NOT EXISTS "IX_Players_DiscordUserId" ON "Players" ("DiscordUserId");
        CREATE INDEX IF NOT EXISTS "IX_Users_DiscordUserId" ON "Users" ("DiscordUserId");
        CREATE INDEX IF NOT EXISTS "IX_VerificationCodes_ClaimToken" ON "VerificationCodes" ("ClaimToken");
        CREATE INDEX IF NOT EXISTS "IX_JoinRequests_UserId_CreatedAt" ON "JoinRequests" ("UserId", "CreatedAt");
        CREATE INDEX IF NOT EXISTS "IX_TournamentMatches_TournamentId_Round" ON "TournamentMatches" ("TournamentId", "Round");
        CREATE INDEX IF NOT EXISTS "IX_Players_TotalPlaySeconds" ON "Players" ("TotalPlaySeconds");
        """);
}

// Same additive index set for SQLite/SqlServer deployments that applied the
// checked-in migrations before these indexes existed. SQLite supports
// CREATE INDEX IF NOT EXISTS natively, so no existence probe is needed.
static void ApplySqliteIndexes(ClubPlaytimeDbContext dbContext)
{
    dbContext.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS \"IX_DailyPlaytime_Date\" ON \"DailyPlaytime\" (\"Date\")");
    dbContext.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS \"IX_Players_DiscordUserId\" ON \"Players\" (\"DiscordUserId\")");
    dbContext.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS \"IX_Users_DiscordUserId\" ON \"Users\" (\"DiscordUserId\")");
    dbContext.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS \"IX_VerificationCodes_ClaimToken\" ON \"VerificationCodes\" (\"ClaimToken\")");
    dbContext.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS \"IX_JoinRequests_UserId_CreatedAt\" ON \"JoinRequests\" (\"UserId\", \"CreatedAt\")");
    dbContext.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS \"IX_TournamentMatches_TournamentId_Round\" ON \"TournamentMatches\" (\"TournamentId\", \"Round\")");
    dbContext.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS \"IX_Players_TotalPlaySeconds\" ON \"Players\" (\"TotalPlaySeconds\")");
}

// Announcements table for existing PostgreSQL deployments (same additive
// pattern as the tournament schema above).
static void ApplyPostgresAnnouncementSchema(ClubPlaytimeDbContext dbContext)
{
    dbContext.Database.ExecuteSqlRaw("""
        CREATE TABLE IF NOT EXISTS "Announcements" (
            "Id" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
            "Title" character varying(120) NOT NULL,
            "Body" character varying(500) NULL,
            "LinkUrl" character varying(300) NULL,
            "CreatedBy" character varying(50) NOT NULL,
            "CreatedAt" timestamp with time zone NOT NULL
        );

        CREATE INDEX IF NOT EXISTS "IX_Announcements_CreatedAt" ON "Announcements" ("CreatedAt");
        """);

    // Monthly reward singleton for existing PostgreSQL deployments.
    dbContext.Database.ExecuteSqlRaw("""
        CREATE TABLE IF NOT EXISTS "MonthlyRewardSetting" (
            "Id" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
            "Prize" character varying(300) NOT NULL,
            "UpdatedAt" timestamp with time zone NOT NULL,
            "UpdatedBy" character varying(50) NULL
        );
        """);
}

app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseResponseCompression();
app.UseDefaultFiles(); // Serve index.html by default

// Hash-busted static assets (vite emits content-hashed filenames) are immutable
// for a year — browsers stop re-downloading the whole app on every visit.
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        var path = ctx.Context.Request.Path;
        if (path.StartsWithSegments("/assets"))
        {
            ctx.Context.Response.Headers.CacheControl = "public,max-age=31536000,immutable";
        }
        else if (path.HasValue && (path.Value.EndsWith(".html") || path.Value.EndsWith("/")))
        {
            ctx.Context.Response.Headers.CacheControl = "no-cache";
        }
    }
});

app.UseCors("ReactClient");
app.UseAuthentication();
app.UseAuthorization();

// Banner images are versioned by URL (?v=changes on every upload), so they are
// safely cacheable — repeat profile views stop re-downloading them entirely.
app.Use(async (context, next) =>
{
    context.Response.OnStarting(() =>
    {
        if (context.Request.Path.StartsWithSegments("/api/profile/banner-image"))
        {
            context.Response.Headers.CacheControl = "public,max-age=604800";
        }
        return Task.CompletedTask;
    });
    await next();
});
app.UseOutputCache();

// Degraded-mode gate: if startup DB initialization never succeeded (e.g. Neon
// quota exhausted), answer /api with a clean 503 instead of letting every
// request throw and surface as a 500. Static files still serve, so users see
// the site shell; the background init loop flips this off automatically when
// the database recovers — no restart or redeploy needed.
app.Use(async (context, next) =>
{
    if (!dbReady && context.Request.Path.StartsWithSegments("/api"))
    {
        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        context.Response.Headers.RetryAfter = "60";
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(new
        {
            message = $"Service temporarily unavailable — {degradedReason}. Please retry in a minute.",
            retryAfterSeconds = 60
        }));
        return;
    }
    await next();
});

// Website-presence heartbeat: for authenticated requests from users with a
// linked tracker player, stamp LastSeenOnSite on the user and player rows at
// most once per 4 minutes. Lets the client show "on the website" vs "in game"
// vs "offline" without any extra requests or infrastructure.
// One set-based UPDATE per 4-minute window (previously two tracked-entity loads
// plus a full save per authenticated request, adding writes and latency).
app.Use(async (context, next) =>
{
    if (dbReady && context.User.Identity?.IsAuthenticated == true)
    {
        try
        {
            var db = context.RequestServices.GetRequiredService<ClubPlaytime.Api.Data.ClubPlaytimeDbContext>();
            var now = DateTime.UtcNow;
            var cutoff = now.AddMinutes(-4);
            var identityName = context.User.Identity!.Name;

            // Single round-trip: bump both rows only when the window has elapsed.
            var updated = await db.Users
                .Where(u => u.Username == identityName && (u.LastSeenOnSite == null || u.LastSeenOnSite < cutoff))
                .ExecuteUpdateAsync(s => s
                    .SetProperty(u => u.LastSeenOnSite, now), cancellationToken: context.RequestAborted);

            if (updated > 0)
            {
                await db.Players
                    .Where(p => p.Id == db.Users.Where(u => u.Username == identityName).Select(u => u.PlayerId).FirstOrDefault())
                    .ExecuteUpdateAsync(s => s.SetProperty(p => p.LastSeenOnSite, now), cancellationToken: context.RequestAborted);
            }
        }
        catch
        {
            // Presence stamping must never break a request.
        }
    }

    await next();
});

// Client-aborted requests (user navigates away mid-fetch) are normal noise:
// close the connection instead of letting them surface as errors.
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (OperationCanceledException)
    {
        context.Abort();
    }
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();

// ─── Global exception handler ────────────────────────────────────────────────
// Logs the real exception server-side and returns a proper ProblemDetails 500
// instead of an empty Kestrel 500, so production "500 everywhere" incidents are
// diagnosable from the browser console AND the server log.
internal sealed class GlobalExceptionHandler(ILogger<Program> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title) = exception switch
        {
            OperationCanceledException => (StatusCodes.Status499ClientClosedRequest, "Request cancelled"),
            Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "The record was modified by someone else. Reload and try again."),
            UnauthorizedAccessException => (StatusCodes.Status403Forbidden, "Forbidden"),
            // DB became unavailable mid-run (quota stop, cold wake, maintenance):
            // a clean, retryable 503 beats a misleading 500.
            Npgsql.NpgsqlException or
            Npgsql.PostgresException or
            Microsoft.EntityFrameworkCore.Storage.RetryLimitExceededException
                => (StatusCodes.Status503ServiceUnavailable, "The database is temporarily unavailable. Please retry shortly."),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred")
        };

        if (status == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception,
                "Unhandled exception for {Method} {Path} (trace {TraceId})",
                httpContext.Request.Method, httpContext.Request.Path, httpContext.TraceIdentifier);
        }
        else if (status == StatusCodes.Status503ServiceUnavailable)
        {
            // DB outages are expected operational states, not bugs — one warning
            // line per event instead of a full stack trace per request.
            logger.LogWarning("{Status} for {Method} {Path}: {Message}",
                status, httpContext.Request.Method, httpContext.Request.Path, exception.Message);
        }
        else if (status != StatusCodes.Status499ClientClosedRequest)
        {
            logger.LogWarning(exception, "{Status} for {Method} {Path}: {Message}",
                status, httpContext.Request.Method, httpContext.Request.Path, exception.Message);
        }

        if (status == StatusCodes.Status499ClientClosedRequest)
        {
            return true; // client is gone; nothing to write
        }

        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(
            new
            {
                type = "https://tools.ietf.org/html/rfc9110#section-15.6.1",
                title,
                status,
                traceId = httpContext.TraceIdentifier
            },
            cancellationToken);
        return true;
    }
}
