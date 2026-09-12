#!/usr/bin/env python
"""E2E verification of the disqualify/remove walkover-advance bug fix.

Scenario (matches the reported bug):
  - 8-player 1v1 bracket (QF x4, SF x2, Final).
  - Complete the two left-half QFs, then one SF normally -> its winner sits in the Final.
  - In the OTHER semifinal, disqualify a player AFTER their QF winner had advanced:
    the walkover winner must be pushed into the Final slot (the bug: it stayed TBD).

Also covers: remove-from-tournament walkover, completed-final auto-completion of the
tournament, and winner-not-occupying-slot cleanup.
"""
import json
import urllib.request
import urllib.error
import random
import string
import sqlite3

DB = "C:/Users/user/OneDrive/Desktop/Club-Playtime/tmp-api-test/club-playtime.db"

B = "http://localhost:5699"
passed = failed = 0
failures = []


def req(method, path, body=None, token=None, expect=None):
    url = B + path
    data = json.dumps(body).encode() if body is not None else None
    r = urllib.request.Request(url, data=data, method=method)
    r.add_header("Content-Type", "application/json")
    if token:
        r.add_header("Authorization", f"Bearer {token}")
    try:
        with urllib.request.urlopen(r) as resp:
            code, text = resp.status, resp.read().decode()
    except urllib.error.HTTPError as e:
        code, text = e.code, e.read().decode()
    if expect is not None and code != expect:
        raise AssertionError(f"{method} {path} -> {code} (expected {expect}): {text[:300]}")
    return code, (json.loads(text) if text else None)


def check(name, cond, extra=""):
    global passed, failed
    if cond:
        passed += 1
        print(f"PASS {name}")
    else:
        failed += 1
        failures.append(name)
        print(f"FAIL {name} {extra}")


def rnd(n=8):
    return "".join(random.choices(string.ascii_lowercase + string.digits, k=n))


def psql(sql):
    """Run a statement against the test SQLite DB (returns last row for INSERT...RETURNING)."""
    conn = sqlite3.connect(DB)
    try:
        cur = conn.execute(sql)
        out = cur.fetchall()
        conn.commit()
        return str(out[-1][0]) if out else ""
    finally:
        conn.close()


# ---------- admin ----------
U = "dqadm_" + rnd()
req("POST", "/api/auth/register", {"username": U, "password": "Adm1n!Pass1", "confirmPassword": "Adm1n!Pass1"}, expect=200)
code, body = req("POST", "/api/auth/login", {"username": U, "password": "Adm1n!Pass1"}, expect=200)
me = req("GET", "/api/auth/me", token=body["token"], expect=200)[1]
psql(f'UPDATE "Users" SET "Role"=\'Admin\' WHERE "Username"=\'{me["username"]}\';')
admin = req("POST", "/api/auth/login", {"username": U, "password": "Adm1n!Pass1"}, expect=200)[1]["token"]


def mk_player_user(password="Play3r!Pass1"):
    uname = "dq" + rnd(6)
    req("POST", "/api/auth/register", {"username": uname, "password": password, "confirmPassword": password}, expect=200)
    roblox = random.randint(10 ** 9, 9 * 10 ** 9)
    pid = psql(
        f"INSERT INTO Players (Username, ProfileUrl, RobloxUserId, IsOnline, TotalPlaySeconds, Club, CreatedAt, UpdatedAt) "
        f"VALUES ('{uname}', 'https://roblox.com/u/{roblox}', {roblox}, 0, 0, 'PIH', datetime('now'), datetime('now')) RETURNING Id;"
    )
    tok = req("POST", "/api/auth/login", {"username": uname, "password": password}, expect=200)[1]["token"]
    me_u = req("GET", "/api/auth/me", token=tok, expect=200)[1]
    req("PUT", f"/api/admin/users/{me_u['id']}", {"username": uname, "role": "User", "playerId": int(pid)},
        token=admin, expect=200)
    return int(pid)


def set_status(tid, status):
    return req("POST", f"/api/tournaments/{tid}/status", {"status": status}, token=admin, expect=204)


def get_t(tid):
    return req("GET", f"/api/tournaments/{tid}", token=admin, expect=200)[1]


def set_winner(tid, mid, wid):
    return req("POST", f"/api/tournaments/{tid}/matches/{mid}/winner", {"winnerParticipantId": wid}, token=admin, expect=200)


# ---------- 8-player bracket ----------
code, t = req("POST", "/api/tournaments",
              {"name": "DQ FIX E2E " + rnd(4), "description": "dq fix", "format": "SingleElimination",
               "teamMode": "Solo", "registrationStartsAt": "2026-09-12T00:00:00Z",
               "registrationDeadline": "2026-09-20T00:00:00Z", "startsAt": "2026-09-21T00:00:00Z",
               "maxParticipants": 8, "prizeInfo": "1st: gold"}, token=admin, expect=201)
tid = t["id"]

pids = [mk_player_user() for _ in range(8)]
# register via admin endpoint (playerId based) to avoid needing 8 logins
for pid in pids:
    req("POST", f"/api/tournaments/{tid}/participants", {"playerId": pid}, token=admin, expect=200)

set_status(tid, "REGISTRATION_OPEN")
req("POST", f"/api/tournaments/{tid}/bracket", None, token=admin, expect=200)

t = get_t(tid)
check("8 players -> 7 matches", len(t["matches"]) == 7, str(len(t["matches"])))
check("3 rounds", max(m["round"] for m in t["matches"]) == 3, str(max(m["round"] for m in t["matches"])))

qf = sorted([m for m in t["matches"] if m["round"] == 1], key=lambda m: m["slot"])
sf = sorted([m for m in t["matches"] if m["round"] == 2], key=lambda m: m["slot"])
final = [m for m in t["matches"] if m["round"] == 3][0]

# sanity: slot linkage qf slot1+2 -> sf slot1, qf 3+4 -> sf slot2
check("qf1 feeds sf1", qf[0]["nextMatchId"] == sf[0]["id"] and qf[1]["nextMatchId"] == sf[0]["id"],
      f"{qf[0]['nextMatchId']},{qf[1]['nextMatchId']} vs {sf[0]['id']}")
check("qf3 feeds sf2", qf[2]["nextMatchId"] == sf[1]["id"] and qf[3]["nextMatchId"] == sf[1]["id"],
      f"{qf[2]['nextMatchId']},{qf[3]['nextMatchId']} vs {sf[1]['id']}")
check("sf feeds final", sf[0]["nextMatchId"] == final["id"] and sf[1]["nextMatchId"] == final["id"])

# Play ALL 4 QFs -> both semifinals have both entrants (the user's reported scenario).
for m in qf:
    set_winner(tid, m["id"], m["participant1Id"])
t = get_t(tid)
sf1 = [m for m in t["matches"] if m["id"] == sf[0]["id"]][0]
sf2 = [m for m in t["matches"] if m["id"] == sf[1]["id"]][0]
check("both SFs filled after QFs", sf1["participant1Id"] and sf1["participant2Id"]
      and sf2["participant1Id"] and sf2["participant2Id"], json.dumps([sf1, sf2]))

# THE BUG: disqualify ONE participant of sf2. The other gets a walkover win and
# MUST be pushed into the Final slot (previously the bracket showed the win but
# the final stayed TBD).
dq_victim = sf2["participant1Id"]
walkover_winner = sf2["participant2Id"]
code, _ = req("POST", f"/api/tournaments/{tid}/participants/{dq_victim}/disqualify", {"reason": "test dq"},
              token=admin, expect=204)
t = get_t(tid)
sf2b = [m for m in t["matches"] if m["id"] == sf[1]["id"]][0]
finalb = [m for m in t["matches"] if m["id"] == final["id"]][0]
check("walkover completed sf2 with correct winner",
      sf2b["status"] == "COMPLETED" and sf2b["winnerId"] == walkover_winner, json.dumps(sf2b))
check("walkover winner advanced to final (THE BUG FIX)",
      finalb["participant1Id"] == walkover_winner or finalb["participant2Id"] == walkover_winner,
      json.dumps(finalb))
check("final has exactly 1 entrant", (finalb["participant1Id"] is None) != (finalb["participant2Id"] is None),
      json.dumps(finalb))

# ---------- remove-from-tournament walkover (same flow) ----------
set_winner(tid, sf[0]["id"], sf1["participant1Id"])
t = get_t(tid)
finalc = [m for m in t["matches"] if m["id"] == final["id"]][0]
check("final now full", finalc["participant1Id"] and finalc["participant2Id"], json.dumps(finalc))

# Remove one finalist -> other becomes walkover winner, tournament auto-completes.
removing = finalc["participant1Id"]
code, _ = req("DELETE", f"/api/tournaments/{tid}/participants/{removing}", None, token=admin, expect=204)
t = get_t(tid)
finald = [m for m in t["matches"] if m["id"] == final["id"]][0]
rem_dq = [p for p in t["participants"] if p["id"] == removing][0]
check("removed finalist marked disqualified (InProgress remove = DQ)", rem_dq["isDisqualified"],
      json.dumps(rem_dq))
check("walkover final completed", finald["status"] == "COMPLETED", json.dumps(finald))
check("tournament auto-COMPLETED after walkover final", t["status"] == "COMPLETED", t["status"])
check("winner recorded", (t.get("results") or {}).get("firstPlace") is not None,
      json.dumps(t.get("results")))

# ---------- DQ the only occupant of a pending match: slot just clears ----------
code, t3 = req("POST", "/api/tournaments",
               {"name": "DQ SOLO E2E " + rnd(4), "format": "SingleElimination", "teamMode": "Solo",
                "registrationStartsAt": "2026-09-12T00:00:00Z", "registrationDeadline": "2026-09-20T00:00:00Z",
                "startsAt": "2026-09-21T00:00:00Z", "maxParticipants": 8}, token=admin, expect=201)
tid3 = t3["id"]
pids3 = [mk_player_user() for _ in range(8)]
for pid in pids3:
    req("POST", f"/api/tournaments/{tid3}/participants", {"playerId": pid}, token=admin, expect=200)
set_status(tid3, "REGISTRATION_OPEN")
req("POST", f"/api/tournaments/{tid3}/bracket", None, token=admin, expect=200)
t3 = get_t(tid3)
qf3m = sorted([m for m in t3["matches"] if m["round"] == 1], key=lambda m: m["slot"])
sf4 = sorted([m for m in t3["matches"] if m["round"] == 2], key=lambda m: m["slot"])[0]

# Win qf slot1; then DQ that winner while qf slot2 is still pending.
w3 = qf3m[0]["participant1Id"]
set_winner(tid3, qf3m[0]["id"], w3)
code, _ = req("POST", f"/api/tournaments/{tid3}/participants/{w3}/disqualify", {"reason": "solo occupant dq"},
              token=admin, expect=204)
t3 = get_t(tid3)
sf4b = [m for m in t3["matches"] if m["id"] == sf4["id"]][0]
check("DQ clears the won slot from next round",
      sf4b["participant1Id"] != w3 and sf4b["participant2Id"] != w3, json.dumps(sf4b))
req("DELETE", f"/api/tournaments/{tid3}", None, token=admin, expect=204)

# ---------- DQ a player who already WON their QF (downstream reset) ----------
code, t2 = req("POST", "/api/tournaments",
               {"name": "DQ RESET E2E " + rnd(4), "format": "SingleElimination", "teamMode": "Solo",
                "registrationStartsAt": "2026-09-12T00:00:00Z", "registrationDeadline": "2026-09-20T00:00:00Z",
                "startsAt": "2026-09-21T00:00:00Z", "maxParticipants": 8}, token=admin, expect=201)
tid2 = t2["id"]
pids2 = [mk_player_user() for _ in range(8)]
for pid in pids2:
    req("POST", f"/api/tournaments/{tid2}/participants", {"playerId": pid}, token=admin, expect=200)

set_status(tid2, "REGISTRATION_OPEN")
req("POST", f"/api/tournaments/{tid2}/bracket", None, token=admin, expect=200)

t2 = get_t(tid2)
qf2 = sorted([m for m in t2["matches"] if m["round"] == 1], key=lambda m: m["slot"])
sf3 = sorted([m for m in t2["matches"] if m["round"] == 2], key=lambda m: m["slot"])

# qf2[0] winner advances; then DQ that winner -> sf3 slot must clear and qf2 resets.
w = qf2[0]["participant1Id"]
set_winner(tid2, qf2[0]["id"], w)
code, _ = req("POST", f"/api/tournaments/{tid2}/participants/{w}/disqualify", {"reason": "dq after win"},
              token=admin, expect=204)
t2 = get_t(tid2)
qf2b = [m for m in t2["matches"] if m["id"] == qf2[0]["id"]][0]
sf3b = [m for m in t2["matches"] if m["id"] == sf3[0]["id"]][0]
check("DQ-after-win resets the won match",
      qf2b["winnerId"] is None and qf2b["status"] == "PENDING", json.dumps(qf2b))
check("DQ-after-win clears downstream slot",
      sf3b["participant1Id"] != w and sf3b["participant2Id"] != w, json.dumps(sf3b))

# ---------- cleanup ----------
req("DELETE", f"/api/tournaments/{tid}", None, token=admin, expect=204)
req("DELETE", f"/api/tournaments/{tid2}", None, token=admin, expect=204)

print(f"\n{passed} passed, {failed} failed")
if failures:
    print("FAILURES:", failures)
    raise SystemExit(1)
