
## Setup Guide

### Prerequisites

1. **.NET 8 SDK** — Install from [dotnet.microsoft.com](https://dotnet.microsoft.com/download/dotnet/8.0)
2. **A Discord Bot Token** — Create a bot at [discord.com/developers/applications](https://discord.com/developers/applications)
3. **The ClubPlaytime API** — Must be running for the bot to fetch data

### Step 1: Create a Discord Bot Application

1. Go to the [Discord Developer Portal](https://discord.com/developers/applications)
2. Click **New Application** and give it a name (e.g., "Club Playtime Bot")
3. Go to the **Bot** tab in the left sidebar
4. Click **Reset Token** and **Copy** the token — you'll need this later
5. Under **Privileged Gateway Intents**, enable:
   - ✅ **Server Members Intent**
   - ✅ **Message Content Intent**
6. Save changes

### Step 2: Invite the Bot to Your Server

1. Go to the **OAuth2 → URL Generator** tab
2. Under **Scopes**, select:
   - ✅ `bot`
   - ✅ `applications.commands`
3. Under **Bot Permissions**, select:
   - ✅ `Send Messages`
   - ✅ `Embed Links`
   - ✅ `Read Message History`
   - ✅ `Use Slash Commands`
4. Copy the generated URL and open it in your browser
5. Select your server and authorize the bot

### Step 3: Get Your Server & Channel IDs

1. Enable **Developer Mode** in Discord:
   - Settings → Advanced → Developer Mode → ON
2. Right-click your server name → **Copy Server ID** → Save this as `Discord:GuildId`
3. Right-click the channel where you want daily summaries → **Copy Channel ID** → Save this as `Discord:DailyChannelId`

### Step 4: Configure the Bot

Edit `ClubPlaytime.DiscordBot/appsettings.json`:

```json
{
  "Discord": {
    "Token": "YOUR_DISCORD_BOT_TOKEN",
    "GuildId": "YOUR_DISCORD_SERVER_ID",
    "DailyChannelId": "YOUR_CHANNEL_ID_FOR_DAILY_MESSAGE",
    "DailyPostTime": "08:00"
  },
  "Api": {
    "BaseUrl": "http://localhost:5000/api"
  }
}
```

Replace the placeholders:
- `YOUR_DISCORD_BOT_TOKEN` — The bot token from Step 1
- `YOUR_DISCORD_SERVER_ID` — Your server ID from Step 3
- `YOUR_CHANNEL_ID_FOR_DAILY_MESSAGE` — Your channel ID from Step 3
- `DailyPostTime` — Time (UTC) when the daily summary is posted (default: `08:00`)
- `Api:BaseUrl` — URL where the ClubPlaytime API is running