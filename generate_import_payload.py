#!/usr/bin/env python3
"""
Generate JSON payload for the /api/admin/import-playtime endpoint.
Run this, then POST the output to your API.

Usage:
  python generate_import_payload.py > import_payload.json
  curl -X POST https://your-api-url/api/admin/import-playtime \
    -H "Authorization: Bearer <token>" \
    -H "Content-Type: application/json" \
    -d @import_payload.json
"""

import sqlite3
import json

BACKUP_DB = "club-playtime (1).db"

def main():
    conn = sqlite3.connect(BACKUP_DB)
    c = conn.cursor()

    # Get all players
    c.execute("SELECT Id, RobloxUserId FROM Players")
    player_map = {row[0]: row[1] for row in c.fetchall()}

    # Get all daily playtime
    c.execute("SELECT PlayerId, Date, PlaySeconds FROM DailyPlaytime")
    daily_records = []

    for row in c.fetchall():
        local_id, date, play_seconds = row
        roblox_id = player_map.get(local_id)
        if roblox_id:
            daily_records.append({
                "robloxUserId": roblox_id,
                "date": date,
                "playSeconds": play_seconds
            })

    conn.close()

    payload = {"dailyPlaytime": daily_records}
    print(json.dumps(payload, indent=2))

if __name__ == "__main__":
    main()
