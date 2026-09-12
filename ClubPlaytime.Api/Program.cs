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
        // additive compatibility upgrades below for databases that already existed.
        // EnsureCreated intentionally does not update an existing database.
        dbContext.Database.EnsureCreated();
        ApplyPostgresAccountLinkSchema(dbContext);
        ApplyPostgresTournamentSchema(dbContext);
        ApplyPostgresAnnouncementSchema(dbContext);
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
