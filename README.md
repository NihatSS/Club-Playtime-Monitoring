# Roblox Club Activity Tracker

Full-stack tracker for club members playing Racket Rivals on Roblox.

## Stack

- ASP.NET Core Web API targeting .NET 8
- Entity Framework Core
- SQLite by default, SQL Server configurable
- JWT Authentication with role-based access control
- Hosted background service for player checks
- React + Vite + Tailwind CSS

## Run Locally

Backend:

```powershell
cd ClubPlaytime.Api
dotnet restore
dotnet run
```

API and Swagger run on the launch profile URL, currently `http://localhost:5121`.

Frontend:

```powershell
cd Client
npm install
npm run dev
```

Open `http://localhost:5173`.

## Authentication

The API uses JWT (JSON Web Token) authentication with two roles:

- **Admin**: Can add/remove players, adjust playtime, manage users, and trigger monitoring checks
- **User**: Can view dashboard, leaderboards, and export data

### Default Admin Account

A default admin account is seeded on first run:
- Username: `admin`
- Password: `admin123`

**⚠️ Change this password immediately in production!**

### Login

```http
POST /api/auth/login
Content-Type: application/json

{
  "username": "admin",
  "password": "admin123"
}
```

Response:
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "role": "Admin",
  "username": "admin"
}
```

### Using the Token

Include the token in the `Authorization` header for all API requests:

```http
Authorization: Bearer <your-jwt-token>
```

### Login Page

Open `http://localhost:5121` in your browser to access the login page.

## User Management (Admin Only)

```http
# List all users
GET /api/admin/users

# Create a new user
POST /api/admin/users
{
  "username": "newuser",
  "password": "password123",
  "role": "User"
}

# Delete a user
DELETE /api/admin/users/{id}

# Change user password
POST /api/admin/users/{id}/change-password
{
  "newPassword": "newpassword123"
}
```

## Configuration

### JWT Settings

Configure JWT in [appsettings.json](ClubPlaytime.Api/appsettings.json):

```json
"Jwt": {
  "SecretKey": "CHANGE_THIS_TO_A_LONG_RANDOM_STRING_IN_PRODUCTION!",
  "Issuer": "ClubPlaytime",
  "Audience": "ClubPlaytime",
  "ExpirationMinutes": 1440
}
```

**⚠️ Always change the `SecretKey` in production!**

### Database Settings

Tracker settings live in [appsettings.json](ClubPlaytime.Api/appsettings.json):

```json
"Monitoring": {
  "CheckIntervalSeconds": 60,
  "RosterCacheSeconds": 600,
  "TargetGameName": "Racket Rivals",
  "RobloxBaseUrl": "https://www.roblox.com",
  "RequestTimeoutSeconds": 20,
  "EnableDiscordNotifications": false,
  "DiscordWebhookUrl": ""
}
```

Change `TargetGameName` to track another game.

SQLite is the default:

```json
"Database": {
  "Provider": "Sqlite"
}
```

To use SQL Server, set `Provider` to `SqlServer` and update the `SqlServer` connection string.

## API Endpoints

### Public
- `POST /api/auth/login` - Login to get JWT token

### User Role (requires authentication)
- `GET /api/players` - List all players
- `GET /api/players/{id}` - Get player details
- `GET /api/players/by-discord/{discordUserId}` - Get player by Discord ID
- `GET /api/dashboard` - Get dashboard data
- `GET /api/dashboard/leaderboard` - Get leaderboard (daily/weekly/monthly)
- `GET /api/dashboard/leaderboard/weekly` - Get weekly leaderboard

### Admin Role (requires admin authentication)
- `POST /api/players` - Add a new player
- `DELETE /api/players/{id}` - Delete a player
- `POST /api/players/{id}/adjust-playtime` - Adjust player's playtime
- `POST /api/players/link-discord` - Link Discord account to player
- `POST /api/monitor/check-now` - Trigger immediate monitoring check
- `GET /api/admin/users` - List all users
- `POST /api/admin/users` - Create a new user
- `DELETE /api/admin/users/{id}` - Delete a user
- `POST /api/admin/users/{id}/change-password` - Change user password

## Monitoring Behavior

The hosted service checks every configured player at the configured interval. It fetches:

```text
https://www.roblox.com/users/{userid}/profile
```

It reads the configured presence selector and uses the element `title`. If the title contains the configured target game name, the player is treated as actively playing.

`LastSeenPlaying` stores the last accounted UTC polling time. When the next check still sees the player in the target game, elapsed seconds are added to both `Players.TotalPlaySeconds` and the one daily row for that player/date.

Roblox request errors are logged and retried on the next interval. The worker keeps running.

## Hosted Database Usage

The monitor is deliberately database-light so a free or cheap hosted Postgres survives the month:

- Each cycle calls the Roblox presence API only — no database round trip — and compares the result with the last successful sample kept in memory.
- It touches the database only when there is something to record: a player is in the target game (playtime accrues per check), or a player's online/game state changed.
- The player roster is cached in memory for `RosterCacheSeconds` (default 600) instead of being re-read every cycle. Adding/removing a player, or the admin "check now" button, refreshes it immediately.

This matters on serverless Postgres (Neon and similar): those providers meter compute only while the database is awake and suspend it after a few idle minutes, so a tracker that queried the database every 60 seconds kept the project running 24/7 — roughly 720 compute-hours a month against a free allowance of roughly 190. That is why a project can die after about a week with:

```text
Npgsql.PostgresException: 53000: Your account or project has exceeded the compute time quota.
```

When that happens the API answers `503` naming the real reason and the background recovery loop retries every 30 minutes (instead of every 2), so the site comes back on its own once the quota resets or the plan is upgraded. Raise `RosterCacheSeconds` (or, less preferably, `CheckIntervalSeconds`) if a plan still runs out.

## Hosting Cost (Railway)

Railway bills by the second: memory at `$0.00000386 / GB / s` (about **$10.14 per GB-month**),
CPU at `$0.00000772 / vCPU / s` (about **$20 per vCPU-month**), egress at `$0.05 / GB` and volumes at
`$0.155 / GB-month`. The Free plan includes **$1 of usage per month**, so the whole budget is roughly
one container averaging **80–100 MB of RAM**.

`docker stats` on this image, measured while it served requests and ran a full monitor cycle:

| Runtime settings | Idle RSS | Approx. monthly memory cost |
| --- | --- | --- |
| Server GC, no heap cap | ~97 MB | ~$0.98 |
| `Dockerfile` defaults (Workstation GC, 96 MB cap) | ~89 MB | ~$0.90 |
| Same, with `DOTNET_GCHeapHardLimit=0x4000000` (64 MB) | ~78 MB | ~$0.79 |

Idle CPU is `0.01%`, and a full monitor cycle is a brief spike, so CPU costs cents per month —
which is why `CheckIntervalSeconds` stays at 60: raising it to 120 would save about a cent a month
while missing any session shorter than two minutes. **Memory is the only lever that matters.**

To stay inside $1:

1. **Run exactly one service.** A second container (the Discord bot) has the same footprint, and
   two of them are over budget. The Free plan allows 3 services, but each one is billed.
2. **Delete the volume.** The database lives on Supabase now, so the API is stateless.
3. **Batch deploys.** A build (npm + dotnet publish) consumes roughly a cent of the budget, so
   auto-deploy on every push adds up over a month.
4. **Leave App Sleeping off.** It stops the background monitor, which is the point of the app.
5. Keep the runtime settings in the `Dockerfile`; they also stop the heap creeping up over weeks
   of uptime, which uncapped memory does quietly.

If a month still lands over $1, the fix is a host with a flat free tier rather than a smaller
container — an always-on .NET process has a floor of roughly 80 MB regardless of settings.

## Moving Off a Provider That Ran Out of Quota

On the Neon Free plan `CU-hours` are metered, not capped per request: when they run out the compute is
suspended **until the next billing period or an upgrade**. Every connection is refused meanwhile, so a
retry in a minute, a redeploy, or a restart changes nothing — and the stored data is not lost, only
unreachable. If the app keeps dying this way, move it to a host that runs a small always-on instance
instead of metering compute time (Railway Postgres, Aiven's free Postgres, or a paid Neon plan).

The copy is done by [scripts/pg-sync.sh](scripts/pg-sync.sh), which needs no local `psql`/`pg_dump`
(it runs the official `postgres:16` image through Docker) and refuses to write an empty dump:

```bash
# 1. Get the source answering again: upgrade the Neon plan, or wait for the reset
#    date shown in the Neon console. Nothing can be exported before then.

# 2. Copy the data across (Neon -> new host).
export SRC_URL='postgresql://…neon.tech/neondb?sslmode=require'
export TARGET_URL='postgresql://…new-host…'

./scripts/pg-sync.sh dump      # SRC_URL   -> clubplaytime.dump
./scripts/pg-sync.sh restore   # dump      -> TARGET_URL (drops target objects)
./scripts/pg-sync.sh verify    # row counts; fails if the target has no players
```

Prove the dump before trusting it: restore it into a throwaway server first. The client runs
*inside* a container, so a database on this machine is reached as `host.docker.internal`, never
`127.0.0.1` (which resolves to the client container's own loopback).

```bash
docker run -d --name pgtest -e POSTGRES_PASSWORD=test -p 127.0.0.1:55432:5432 postgres:18
export TARGET_URL='postgresql://postgres:test@host.docker.internal:55432/postgres'
./scripts/pg-sync.sh restore && ./scripts/pg-sync.sh verify
docker rm -f pgtest
```

`pg_dump` refuses to read a server newer than itself, so raise `PG_IMAGE` (default `postgres:18`)
when the provider upgrades Postgres — otherwise the dump fails with "server version mismatch",
which reads like an outage but is only a stale image tag.

Then point the app at the new host (Railway → service → Variables) and redeploy:

```text
Database__Provider=Postgres
DATABASE_URL=postgresql://…            # or ConnectionStrings__PostgresConnection=…
```

### Supabase as the target

Use the **session pooler** URI (Dashboard → Connect → Session pooler, port `5432` on
`aws-0-<region>.pooler.supabase.com`). Two reasons: the direct host `db.<ref>.supabase.co`
resolves to IPv6 only, which a Docker container on an IPv4-only network cannot reach, and
`pg_restore --clean` needs a real session to drop objects in — the transaction pooler on port
`6543` multiplexes statements and reports "prepared statement already exists" instead.

`DATABASE_URL` alone is enough: the code takes whichever of the two is set and is not the
`localhost` placeholder from `appsettings.json`. Drop the old Neon variables in the same edit — a
stale `ConnectionStrings__PostgresConnection` left behind is the one thing that silently keeps the
app on the dead database. Keep the Neon project until `verify` reports plausible row counts, then
delete it.

## Security Notes

1. **Change the default admin password** before deploying to production
2. **Change the JWT SecretKey** to a secure random string in production
3. Use HTTPS in production
4. Consider adding rate limiting for the login endpoint
5. Regularly backup your SQLite database file (`club-playtime.db`)
