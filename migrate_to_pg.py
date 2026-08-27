#!/usr/bin/env python3
"""Migrate data from SQLite to PostgreSQL (Neon)."""
import sqlite3
import psycopg2
import os

# SQLite path
SQLITE_PATH = "ClubPlaytime.Api/club-playtime.db"

# PostgreSQL connection (from Railway env / Neon)
PG_CONN = os.environ.get(
    "DATABASE_URL",
    "host=ep-young-wave-aytqmhdg.c-5.us-east-2.aws.neon.tech "
    "port=5432 dbname=neondb user=neondb_owner "
    "password=npg_hdD0Ui2nxGPc sslmode=require sslrootcert=verify-full"
)

def main():
    # Connect to both databases
    sqlite_db = sqlite3.connect(SQLITE_PATH)
    sqlite_db.row_factory = sqlite3.Row
    pg_db = psycopg2.connect(PG_CONN, connect_timeout=15)
    pg_cur = pg_db.cursor()
    s_cur = sqlite_db.cursor()

    # 1. Migrate Users
    print("Migrating Users...")
    users = s_cur.execute("SELECT * FROM Users").fetchall()
    for u in users:
        pg_cur.execute("""
            INSERT INTO Users (Id, Username, PasswordHash, Role, CreatedAt)
            VALUES (%s, %s, %s, %s, %s)
            ON CONFLICT (Id) DO NOTHING
        """, (u['Id'], u['Username'], u['PasswordHash'], u['Role'], u['CreatedAt']))
    print(f"  -> {len(users)} users migrated")

    # 2. Migrate Players
    print("Migrating Players...")
    players = s_cur.execute("SELECT * FROM Players").fetchall()
    for p in players:
        pg_cur.execute("""
            INSERT INTO Players (Id, Username, ProfileUrl, RobloxUserId, IsOnline,
                CurrentlyPlaying, LastSeenPlaying, TotalPlaySeconds, AvatarUrl,
                Club, CreatedAt, UpdatedAt, DiscordUserId)
            VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s)
            ON CONFLICT (Id) DO NOTHING
        """, (
            p['Id'], p['Username'], p['ProfileUrl'], p['RobloxUserId'],
            bool(p['IsOnline']), p['CurrentlyPlaying'], p['LastSeenPlaying'],
            p['TotalPlaySeconds'], p['AvatarUrl'], p['Club'],
            p['CreatedAt'], p['UpdatedAt'], p['DiscordUserId']
        ))
    print(f"  -> {len(players)} players migrated")

    # 3. Migrate DailyPlaytime
    print("Migrating DailyPlaytime...")
    daily = s_cur.execute("SELECT * FROM DailyPlaytime").fetchall()
    for d in daily:
        pg_cur.execute("""
            INSERT INTO DailyPlaytime (Id, PlayerId, Date, PlaySeconds)
            VALUES (%s, %s, %s, %s)
            ON CONFLICT (Id) DO NOTHING
        """, (d['Id'], d['PlayerId'], d['Date'], d['PlaySeconds']))
    print(f"  -> {len(daily)} daily records migrated")

    # 4. Migrate PlayerActivityEvents
    print("Migrating PlayerActivityEvents...")
    events = s_cur.execute("SELECT * FROM PlayerActivityEvents").fetchall()
    for e in events:
        pg_cur.execute("""
            INSERT INTO PlayerActivityEvents (Id, PlayerId, EventType, Message, OccurredAt, DeltaSeconds)
            VALUES (%s, %s, %s, %s, %s, %s)
            ON CONFLICT (Id) DO NOTHING
        """, (e['Id'], e['PlayerId'], e['EventType'], e['Message'], e['OccurredAt'], e['DeltaSeconds']))
    print(f"  -> {len(events)} events migrated")

    # 5. Migrate JoinRequests
    print("Migrating JoinRequests...")
    requests = s_cur.execute("SELECT * FROM JoinRequests").fetchall()
    for r in requests:
        pg_cur.execute("""
            INSERT INTO JoinRequests (Id, RobloxUsername, RobloxUserId, DiscordUserId,
                Club, Status, Note, CreatedAt, ReviewedAt, ReviewedBy)
            VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s, %s)
            ON CONFLICT (Id) DO NOTHING
        """, (
            r['Id'], r['RobloxUsername'], r['RobloxUserId'], r['DiscordUserId'],
            r['Club'], r['Status'], r['Note'], r['CreatedAt'],
            r['ReviewedAt'], r['ReviewedBy']
        ))
    print(f"  -> {len(requests)} join requests migrated")

    # Update sequences so new inserts don't conflict
    print("\nUpdating sequences...")
    for table, col in [("Players", "Id"), ("Users", "Id"), ("DailyPlaytime", "Id"),
                        ("PlayerActivityEvents", "Id"), ("JoinRequests", "Id")]:
        pg_cur.execute(f"""
            SELECT setval(pg_get_serial_sequence('{table}', '{col}'),
                          COALESCE((SELECT MAX({col}) FROM {table}), 1))
        """)

    pg_db.commit()
    print("\n✅ Migration complete!")
    print(f"   Users: {len(users)}, Players: {len(players)}, DailyPlaytime: {len(daily)}, "
          f"Events: {len(events)}, JoinRequests: {len(requests)}")

    sqlite_db.close()
    pg_db.close()

if __name__ == "__main__":
    main()
