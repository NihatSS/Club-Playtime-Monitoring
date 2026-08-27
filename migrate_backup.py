#!/usr/bin/env python3
"""
Migrate backup SQLite playtime data into the production PostgreSQL database.
Run this script locally, then run the generated SQL against your production DB.

Usage:
  python migrate_backup.py > migrate.sql
  Then run migrate.sql against your PostgreSQL database.
"""

import sqlite3
import sys

BACKUP_DB = "club-playtime (1).db"

def main():
    conn = sqlite3.connect(BACKUP_DB)
    conn.row_factory = sqlite3.Row
    c = conn.cursor()

    # Get all players from backup
    c.execute("SELECT Id, RobloxUserId, Username, TotalPlaySeconds FROM Players")
    players = {row["RobloxUserId"]: dict(row) for row in c.fetchall()}

    # Get all daily playtime from backup
    c.execute("SELECT PlayerId, Date, PlaySeconds FROM DailyPlaytime ORDER BY Date")
    daily_rows = c.fetchall()

    conn.close()

    print("-- Migration script: Import backup playtime data into PostgreSQL")
    print("-- Generated from:", BACKUP_DB)
    print("-- Players:", len(players))
    print("-- Daily records:", len(daily_rows))
    print()

    # Map backup local IDs to RobloxUserIds for reference
    print("-- Player mapping (backup local ID -> RobloxUserId):")
    for roblox_id, p in players.items():
        print(f"--   LocalId={p['Id']} -> RobloxUserId={roblox_id} ({p['Username']}) {p['TotalPlaySeconds']}s")
    print()

    # Generate INSERT statements for DailyPlaytime
    print("-- Insert daily playtime records")
    print("-- These will be inserted with ON CONFLICT to avoid duplicates")
    for row in daily_rows:
        # Find the RobloxUserId for this player's local ID
        roblox_id = None
        for rid, p in players.items():
            if p["Id"] == row["PlayerId"]:
                roblox_id = rid
                break
        if roblox_id is None:
            print(f"-- WARNING: Could not find RobloxUserId for local PlayerId={row['PlayerId']}, skipping")
            continue

        print(f"INSERT INTO \"DailyPlaytime\" (\"PlayerId\", \"Date\", \"PlaySeconds\")")
        print(f"SELECT p.\"Id\", '{row['Date']}', {row['PlaySeconds']}")
        print(f"FROM \"Players\" p WHERE p.\"RobloxUserId\" = {roblox_id}")
        print(f"ON CONFLICT (\"PlayerId\", \"Date\") DO UPDATE SET \"PlaySeconds\" = GREATEST(\"DailyPlaytime\".\"PlaySeconds\", EXCLUDED.\"PlaySeconds\");")
        print()

    # Generate UPDATE statements for TotalPlaySeconds
    print("-- Update TotalPlaySeconds for each player")
    for roblox_id, p in players.items():
        print(f"UPDATE \"Players\" SET \"TotalPlaySeconds\" = {p['TotalPlaySeconds']} WHERE \"RobloxUserId\" = {roblox_id};")

    print()
    print("-- Done! Verify with:")
    print("-- SELECT \"Username\", \"TotalPlaySeconds\" FROM \"Players\" ORDER BY \"TotalPlaySeconds\" DESC LIMIT 10;")
    print("-- SELECT COUNT(*) FROM \"DailyPlaytime\";")

if __name__ == "__main__":
    main()
