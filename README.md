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

## Security Notes

1. **Change the default admin password** before deploying to production
2. **Change the JWT SecretKey** to a secure random string in production
3. Use HTTPS in production
4. Consider adding rate limiting for the login endpoint
5. Regularly backup your SQLite database file (`club-playtime.db`)
