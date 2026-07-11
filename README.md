# Roblox Club Activity Tracker

Full-stack tracker for club members playing Racket Rivals on Roblox.

## Stack

- ASP.NET Core Web API targeting .NET 8
- Entity Framework Core
- SQLite by default, SQL Server configurable
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

## Configuration

Tracker settings live in [appsettings.json](ClubPlaytime.Api/appsettings.json):

```json
"Monitoring": {
  "CheckIntervalSeconds": 60,
  "TargetGameName": "Racket Rivals",
  "RobloxBaseUrl": "https://www.roblox.com",
  "PresenceSelector": "span[data-testid='presence-icon']",
  "MaxConcurrentRequests": 5,
  "RequestTimeoutSeconds": 20,
  "EnableDiscordNotifications": false,
  "DiscordWebhookUrl": ""
}
```

Change `TargetGameName` to track another game. Change `PresenceSelector` if Roblox changes the profile HTML.

SQLite is the default:

```json
"Database": {
  "Provider": "Sqlite"
}
```

To use SQL Server, set `Provider` to `SqlServer` and update the `SqlServer` connection string.

## API

- `GET /api/players`
- `POST /api/players`
- `DELETE /api/players/{id}`
- `GET /api/players/{id}`
- `POST /api/players/{id}/adjust-playtime`
- `GET /api/dashboard`
- `GET /api/dashboard/leaderboard/weekly`
- `POST /api/monitor/check-now`
- `GET /api/export/playtime.csv`

## Monitoring Behavior

The hosted service checks every configured player at the configured interval. It fetches:

```text
https://www.roblox.com/users/{userid}/profile
```

It reads the configured presence selector and uses the element `title`. If the title contains the configured target game name, the player is treated as actively playing.

`LastSeenPlaying` stores the last accounted UTC polling time. When the next check still sees the player in the target game, elapsed seconds are added to both `Players.TotalPlaySeconds` and the one daily row for that player/date.

Roblox request errors are logged and retried on the next interval. The worker keeps running.
