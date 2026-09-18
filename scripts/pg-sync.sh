#!/usr/bin/env bash
#
# Copy the club database between two Postgres servers (Neon -> Supabase/Railway) using the
# official postgres client image, so no local psql/pg_dump install is needed.
#
# Written for the Neon move: a provider that has exhausted its compute quota
# refuses every connection (SQLSTATE 53000) until the quota resets or the plan is
# upgraded, so the dump can only be taken the moment the source comes back. This
# script makes that moment a single command, and refuses to do anything
# destructive in the meantime.
#
# Usage:
#   export SRC_URL='postgresql://…'      # source (Neon)
#   export TARGET_URL='postgresql://…'   # target (Railway)
#
#   ./scripts/pg-sync.sh dump            # SRC_URL      -> $DUMP_FILE
#   ./scripts/pg-sync.sh restore         # $DUMP_FILE   -> TARGET_URL
#   ./scripts/pg-sync.sh verify          # row counts on TARGET_URL
#   ./scripts/pg-sync.sh all             # dump + restore + verify
#
# Optional overrides:
#   DUMP_FILE (default ./clubplaytime.dump)   PG_IMAGE (default postgres:18)
#
# Keep PG_IMAGE at or above the server's major version: pg_dump refuses to read a
# server that is newer than itself ("aborting because of server version mismatch"),
# which looks like an outage but is not one. Neon runs Postgres 18.
set -euo pipefail

DUMP_FILE="${DUMP_FILE:-clubplaytime.dump}"
PG_IMAGE="${PG_IMAGE:-postgres:18}"

COUNTS_SQL='SELECT (SELECT count(*) FROM "Players") AS players,
       (SELECT count(*) FROM "DailyPlaytime") AS daily_rows,
       (SELECT count(*) FROM "PlayerActivityEvents") AS events,
       (SELECT count(*) FROM "Users") AS website_accounts,
       (SELECT sum("PlaySeconds") FROM "DailyPlaytime") AS playtime_seconds,
       (SELECT max("Date") FROM "DailyPlaytime") AS latest_day;'

usage() {
  # Print the leading comment block (everything from line 2 until the first
  # line of real code) so the help text can never drift from the header.
  awk 'NR==1 { next } /^#/ { sub(/^# ?/, ""); print; next } { exit }' "$0"
  echo
  echo "  SRC_URL    = $(mask "${SRC_URL:-<not set>}")"
  echo "  TARGET_URL = $(mask "${TARGET_URL:-<not set>}")"
  echo "  DUMP_FILE  = $DUMP_FILE"
  echo "  PG_IMAGE   = $PG_IMAGE"
}

# Print a connection string with its password blanked out, so it is safe to
# paste into a chat or an issue.
mask() {
  printf '%s\n' "$1" | sed -E 's#(://[^:@/]*):[^@]*@#\1:***@#'
}

# need VAR_NAME [why it is needed]
need() {
  if [ -z "${!1:-}" ]; then
    echo "error: \$$1 is not set${2:+ — $2}" >&2
    exit 1
  fi
}

do_dump() {
  need SRC_URL "the database to copy FROM (Neon)"
  echo "-> dumping $(mask "$SRC_URL") to $DUMP_FILE"

  # Dump straight into a temporary file, then check the size before accepting it.
  # A refused connection makes pg_dump exit non-zero without writing anything;
  # without this guard that becomes a 0-byte file that looks like a success and
  # only fails later as "input file is too short (read 0, expected 5)".
  local partial="$DUMP_FILE.partial"
  local errfile="$DUMP_FILE.partial.err"
  rm -f "$partial" "$errfile"
  if ! docker run --rm "$PG_IMAGE" pg_dump --no-owner --no-privileges \
        --format=custom "$SRC_URL" > "$partial" 2>"$errfile"; then
    rm -f "$partial"
    # A client older than the server is a setup mistake, not an outage, and the
    # default "not serving connections" message below would send you chasing the
    # provider instead of the image tag.
    if grep -q "server version mismatch" "$errfile"; then
      echo "error: the pg_dump client ($PG_IMAGE) is older than the server." >&2
      grep -E "pg_dump: detail:" "$errfile" >&2 || true
      echo "       pg_dump refuses to read a newer server. Retry with a matching client:" >&2
      echo "         PG_IMAGE=postgres:18 $0 dump" >&2
    else
      echo "error: pg_dump failed — the source is not serving connections." >&2
    fi
    exit 1
  fi
  rm -f "$errfile"
  if [ ! -s "$partial" ]; then
    rm -f "$partial"
    echo "error: dump is empty. A provider out of compute quota (SQLSTATE 53000) refuses every" >&2
    echo "       connection until the quota resets or the plan is upgraded; nothing can be" >&2
    echo "       exported until then." >&2
    exit 1
  fi
  mv "$partial" "$DUMP_FILE"
  echo "   OK — $(wc -c < "$DUMP_FILE") bytes"
  if command -v sha256sum >/dev/null 2>&1; then
    sha256sum "$DUMP_FILE"
  fi
}

do_restore() {
  need TARGET_URL "the database to copy TO (Railway)"
  if [ ! -s "$DUMP_FILE" ]; then
    echo "error: $DUMP_FILE is missing or empty — run '$0 dump' first." >&2
    exit 1
  fi

  # --clean makes pg_restore DROP the objects it finds in the target first. That
  # is what we want for a fresh Railway database, and catastrophic if the target
  # is the source by mistake.
  if [ -n "${SRC_URL:-}" ] && [ "$SRC_URL" = "$TARGET_URL" ]; then
    echo "error: SRC_URL and TARGET_URL are identical — refusing to run a --clean restore" >&2
    echo "       against the source you are trying to preserve." >&2
    exit 1
  fi

  echo "-> restoring $DUMP_FILE into $(mask "$TARGET_URL")"
  local rc=0
  docker run --rm -i "$PG_IMAGE" pg_restore --no-owner --no-privileges \
    --clean --if-exists --dbname="$TARGET_URL" < "$DUMP_FILE" || rc=$?
  if [ "$rc" -ne 0 ]; then
    echo "   pg_restore exited $rc. Owner/privilege/'does not exist' messages are expected with" >&2
    echo "   --no-owner; run '$0 verify' to see whether the data actually arrived." >&2
  else
    echo "   restore finished cleanly"
  fi
}

do_verify() {
  need TARGET_URL "the database to check"
  echo "-> row counts on $(mask "$TARGET_URL")"
  docker run --rm "$PG_IMAGE" psql --no-psqlrc -x "$TARGET_URL" -c "$COUNTS_SQL"

  local players
  players=$(docker run --rm "$PG_IMAGE" psql --no-psqlrc -tA "$TARGET_URL" \
    -c 'SELECT count(*) FROM "Players";')
  players="${players//[[:space:]]/}"
  if [ "${players:-0}" -lt 1 ] 2>/dev/null; then
    echo "error: target has no players — do NOT point the app at it." >&2
    exit 1
  fi
  echo "   OK — $players players. Compare that with your leaderboard, and check latest_day is recent."
}

case "${1:-}" in
  dump)    do_dump ;;
  restore) do_restore ;;
  verify)  do_verify ;;
  all)     do_dump; do_restore; do_verify ;;
  *)       usage; exit 1 ;;
esac
