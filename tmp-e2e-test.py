#!/usr/bin/env python
"""End-to-end test of the full tournament workflow against the running API (Postgres)."""
import json
import urllib.request
import urllib.error
import random
import string

B = "http://localhost:5599"
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


def mk_user(username, password):
    req("POST", "/api/auth/register",
        {"username": username, "password": password, "confirmPassword": password}, expect=200)
    code, body = req("POST", "/api/auth/login", {"username": username, "password": password}, expect=200)
    return body["token"]


def play_round(t_id, admin, tmode):
    """Set winners for all pending matches in the earliest round; re-fetch between rounds."""
    while True:
        _, t = req("GET", f"/api/tournaments/{t_id}", token=admin, expect=200)
        pending = [m for m in t["matches"] if m["status"] == "Pending"]
        if not pending:
            break
        m = min(pending, key=lambda x: (x["round"], x["slot"]))
        if m["participant1Id"] is None or m["participant2Id"] is None:
            check(f"pending match has both entrants", False, json.dumps(m))
            return
        slot = m["participant1Id"]
        req("POST", f"/api/tournaments/{t_id}/matches/{m['id']}/winner",
            {"winnerId": slot}, token=admin, expect=200)


# ---------- Auth ----------
U, P = "e2e_pg_" + rnd(), "Str0ng!Pass1"
code, body = req("POST", "/api/auth/login", {"username": "nope_" + rnd(), "password": "Wrong!123x"})
check("bad login -> 401", code == 401 and "nvalid" in body.get("message", ""), f"{code} {body}")

for weak in ["Ab1!x" + "y" * 2 + "z" * 0, "alllowercase1!", "ALLUPPERCASE1!", "NoSymbols123", "NoDigits!!a"]:
    c, _ = req("POST", "/api/auth/register", {"username": "w" + rnd(4), "password": weak, "confirmPassword": weak})
    check(f"weak password rejected: {weak}", c == 400, str(c))

admin_token = mk_user("pgadm_" + rnd(), "Adm1n!Pass1")
_, me = req("GET", "/api/auth/me", token=admin_token, expect=200)
# Promote to admin directly in the test Postgres (real deployments use a seeded admin)
import subprocess

def psql(sql):
    subprocess.run(["docker", "exec", "cp-pg-test", "psql", "-U", "postgres", "-d", "clubplaytime", "-c", sql],
                   check=True, capture_output=True)

def sql1(sql):
    r = subprocess.run(["docker", "exec", "cp-pg-test", "psql", "-U", "postgres", "-d", "clubplaytime", "-tAc", sql],
                       check=True, capture_output=True)
    return r.stdout.decode().strip()

psql(f'UPDATE "Users" SET "Role"=\'Admin\' WHERE "Username"=\'{me["username"]}\';')
# Re-login so the JWT carries the Admin role
code, body = req("POST", "/api/auth/login", {"username": me["username"], "password": "Adm1n!Pass1"}, expect=200)
admin_token = body["token"]
admin_id = me["id"]


def mk_player_user(prefix, password):
    """Create a user + linked tracker player (players come from the tracker in production)."""
    uname = prefix + rnd(6)
    req("POST", "/api/auth/register", {"username": uname, "password": password, "confirmPassword": password},
        expect=200)
    roblox = random.randint(10 ** 9, 9 * 10 ** 9)
    pid = sql1(f"INSERT INTO \"Players\" (\"Username\",\"ProfileUrl\",\"RobloxUserId\",\"TotalPlaySeconds\",\"Club\",\"CreatedAt\") "
               f"VALUES ('{uname}', 'https://roblox.com/u/{roblox}', {roblox}, 0, 'PIH', now(), now()) RETURNING \"Id\";").splitlines()[0]
    _, u = req("POST", "/api/auth/login", {"username": uname, "password": password}, expect=200)
    _, me_u = req("GET", "/api/auth/me", token=u["token"], expect=200)
    req("PUT", f"/api/admin/users/{me_u['id']}", {"username": uname, "role": "User", "playerId": int(pid)},
        token=admin_token, expect=200)
    code2, body2 = req("POST", "/api/auth/login", {"username": uname, "password": password}, expect=200)
    return body2["token"]

# ---------- Permissions ----------
code, _ = req("POST", "/api/tournaments", {"name": "x", "format": "SingleElimination", "teamMode": "Solo",
                                            "registrationStartsAt": "2026-09-20T10:00:00Z",
                                            "registrationDeadline": "2026-09-25T10:00:00Z",
                                            "startsAt": "2026-09-26T10:00:00Z", "maxParticipants": 16})
check("anonymous cannot create tournament", code == 401, str(code))

MODES = {1: "Solo", 2: "DuoRandom", 3: "DuoPredefined"}

def make_tournament(name, mode, maxp=16):
    return req("POST", "/api/tournaments",
               {"name": name, "description": "e2e", "format": "SingleElimination", "teamMode": MODES[mode],
                "registrationStartsAt": "2026-09-12T00:00:00Z",
                "registrationDeadline": "2026-09-20T00:00:00Z",
                "startsAt": "2026-09-21T00:00:00Z", "maxParticipants": maxp,
                "prizeInfo": "1st: 100"}, token=admin_token, expect=201)[1]

# ---------- 1v1 with BYEs ----------
t1 = make_tournament("PG E2E 1v1 " + rnd(4), 1)
players = [mk_player_user("p", "Play3r!Pass1") for _ in range(6)]
for tok in players:
    req("POST", f"/api/tournaments/{t1['id']}/register", {}, token=tok, expect=200)

code, _ = req("POST", f"/api/tournaments/{t1['id']}/register", {}, token=players[0], expect=200)
check("duplicate registration blocked", code == 409, str(code))

req("POST", f"/api/tournaments/{t1['id']}/start", None, token=admin_token, expect=200)
req("POST", f"/api/tournaments/{t1['id']}/generate-bracket", None, token=admin_token, expect=200)

_, t = req("GET", f"/api/tournaments/{t1['id']}", token=admin_token, expect=200)
check("bracket persisted (8 slots, 6 entrants)", len(t["matches"]) == 7, str(len(t["matches"])))
r1 = [m for m in t["matches"] if m["round"] == 1]
byes = [m for m in r1 if m["participant2Id"] is None]
check("2 BYE matches with auto-advance", len(byes) == 2 and all(m["winnerId"] for m in byes),
      json.dumps([{"p1": m["participant1Id"], "w": m["winnerId"]} for m in byes]))
real = [m for m in r1 if m["participant2Id"] is not None]
check("2 real R1 matches", len(real) == 2, str(len(real)))

play_round(t1["id"], admin_token, 1)
_, t = req("GET", f"/api/tournaments/{t1['id']}", token=admin_token, expect=200)
check("semis filled", all(m["participant1Id"] and m["participant2Id"]
                          for m in t["matches"] if m["round"] == 2), "semis incomplete")

# cascade confirmation on completed match edit
m1 = min((m for m in t["matches"] if m["round"] == 1 and m["status"] == "Completed"),
         key=lambda x: x["id"])
code, body = req("POST", f"/api/tournaments/{t1['id']}/matches/{m1['id']}/winner",
                 {"winnerId": m1["participant2Id"]}, token=admin_token)
check("completed match edit -> 409 confirm required", code == 409 and body.get("requiresConfirmation"), f"{code} {body}")
code, _ = req("POST", f"/api/tournaments/{t1['id']}/matches/{m1['id']}/winner",
              {"winnerId": m1["participant2Id"], "confirmCascade": True}, token=admin_token, expect=200)
check("confirmed cascade edit applied", code == 200)

# finish remaining rounds
play_round(t1["id"], admin_token, 1)
_, t = req("GET", f"/api/tournaments/{t1['id']}", token=admin_token, expect=200)
check("tournament COMPLETED", t["status"] == "COMPLETED", t["status"])
check("places recorded", t["firstPlace"] and t["secondPlace"], json.dumps(
    {"1": t.get("firstPlace"), "2": t.get("secondPlace"), "3": t.get("thirdPlace")}))
code, _ = req("POST", f"/api/tournaments/{t1['id']}/start", None, token=admin_token)
check("completed tournament locked", code in (400, 409), str(code))

# ---------- 2v2 random ----------
t2 = make_tournament("PG E2E 2v2R " + rnd(4), 2)
p8 = [mk_player_user("q", "Play3r!Pass1") for _ in range(8)]
for tok in p8:
    req("POST", f"/api/tournaments/{t2['id']}/register", {}, token=tok, expect=200)
req("POST", f"/api/tournaments/{t2['id']}/create-random-teams", None, token=admin_token, expect=200)
_, t = req("GET", f"/api/tournaments/{t2['id']}", token=admin_token, expect=200)
teams = t["teams"]
check("4 random teams of 2", len(teams) == 4 and all(len(x["memberIds"]) == 2 for x in teams),
      json.dumps([len(x["memberIds"]) for x in teams]))
allm = [mid for x in teams for mid in x["memberIds"]]
check("no player in two teams", len(allm) == len(set(allm)))
req("POST", f"/api/tournaments/{t2['id']}/start", None, token=admin_token, expect=200)
req("POST", f"/api/tournaments/{t2['id']}/generate-bracket", None, token=admin_token, expect=200)
play_round(t2["id"], admin_token, 2)
play_round(t2["id"], admin_token, 2)
_, t = req("GET", f"/api/tournaments/{t2['id']}", token=admin_token, expect=200)
check("2v2 random COMPLETED with team names", t["status"] == "COMPLETED" and t["firstPlace"], t["status"])

# ---------- 2v2 predefined ----------
t3 = make_tournament("PG E2E 2v2P " + rnd(4), 3)
p4 = [mk_player_user("r", "Play3r!Pass1") for _ in range(4)]
for tok in p4:
    req("POST", f"/api/tournaments/{t3['id']}/register", {}, token=tok, expect=200)
team_ids = []
for i in range(2):
    _, tm = req("POST", f"/api/tournaments/{t3['id']}/teams",
                {"name": f"Team{i}" + rnd(3), "memberIds": [p4[2 * i], p4[2 * i + 1]]},
                token=admin_token, expect=201)
    team_ids.append(tm["id"])
code, _ = req("POST", f"/api/tournaments/{t3['id']}/teams",
              {"name": "Bad", "memberIds": [p4[0]]}, token=admin_token)
check("3rd team w/ dup player rejected", code == 400, str(code))
req("POST", f"/api/tournaments/{t3['id']}/start", None, token=admin_token, expect=200)
req("POST", f"/api/tournaments/{t3['id']}/generate-bracket", None, token=admin_token, expect=200)
play_round(t3["id"], admin_token, 3)
_, t = req("GET", f"/api/tournaments/{t3['id']}", token=admin_token, expect=200)
check("2v2 predefined COMPLETED", t["status"] == "COMPLETED", t["status"])

# ---------- cleanup ----------
for tid in [t1["id"], t2["id"], t3["id"]]:
    req("DELETE", f"/api/tournaments/{tid}", None, token=admin_token, expect=204)

print(f"\n{passed} passed, {failed} failed")
if failures:
    print("FAILURES:", failures)
    raise SystemExit(1)
