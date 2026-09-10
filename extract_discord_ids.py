#!/usr/bin/env python3
"""
Extract Discord IDs from the local SQLite database and output as JSON
for syncing to the production PostgreSQL database via the API.
"""

import sqlite3
import json
import sys

def extract_discord_ids(db_path='club-playtime.db'):
    conn = sqlite3.connect(db_path)
    cursor = conn.cursor()
    
    # Get all players with Discord IDs
    cursor.execute('SELECT RobloxUserId, DiscordUserId FROM Players WHERE DiscordUserId IS NOT NULL AND DiscordUserId != ""')
    rows = cursor.fetchall()
    
    mappings = []
    for roblox_id, discord_id in rows:
        mappings.append({
            "RobloxUserId": roblox_id,
            "DiscordUserId": str(discord_id).strip()
        })
    
    conn.close()
    
    return {
        "mappings": mappings,
        "count": len(mappings)
    }

if __name__ == "__main__":
    result = extract_discord_ids()
    print(json.dumps(result, indent=2))
    print(f"\nExtracted {result['count']} Discord ID mappings", file=sys.stderr)
