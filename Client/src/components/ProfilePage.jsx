import { useCallback, useEffect, useState } from 'react';
import {
  AlertCircle,
  ArrowLeft,
  CheckCircle,
  Copy,
  ExternalLink,
  Eye,
  EyeOff,
  Gamepad2,
  KeyRound,
  Link2,
  Pencil,
  Save,
  ShieldCheck,
  Trash2,
  Trophy,
  Unlink,
  User
} from 'lucide-react';
import { api } from '../lib/api';
import { formatDateTime, formatDuration } from '../lib/format';

const DISCORD_COPY_HINT =
  "Use Discord's Developer Mode, then right-click your profile and choose Copy User ID.";

function Section({ icon: Icon, title, accent, children }) {
  return (
    <div className="rounded-xl border border-line bg-panel p-5 shadow-glow">
      <div className="mb-4 flex items-center gap-2">
        <Icon className={`h-5 w-5 ${accent}`} />
        <h2 className="text-lg font-semibold text-zinc-50">{title}</h2>
      </div>
      {children}
    </div>
  );
}

function StatTile({ label, value, color = 'text-zinc-50' }) {
  return (
    <div className="rounded-lg border border-line bg-ink p-3">
      <div className="text-[10px] font-semibold uppercase tracking-wider text-zinc-500">{label}</div>
      <div className={`mt-1 text-base font-bold ${color}`}>{value}</div>
    </div>
  );
}

function InfoRow({ label, value, mono = false, action = null }) {
  return (
    <div className="flex items-center justify-between gap-3 py-1.5">
      <span className="text-sm text-mist">{label}</span>
      <span className={`flex items-center gap-1.5 text-sm font-medium text-zinc-200 ${mono ? 'font-mono' : ''}`}>
        {value}
        {action}
      </span>
    </div>
  );
}

export default function ProfilePage({ onBack }) {
  const [profile, setProfile] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');

  // Discord linking state
  const [discordInput, setDiscordInput] = useState('');
  const [discordBusy, setDiscordBusy] = useState(false);
  const [discordEditing, setDiscordEditing] = useState(false);

  // Password change state
  const [pwForm, setPwForm] = useState({ current: '', next: '', confirm: '' });
  const [pwBusy, setPwBusy] = useState(false);
  const [pwEditing, setPwEditing] = useState(false);
  const [showPw, setShowPw] = useState(false);

  // Game info state (Roblox username / Roblox ID / Discord ID)
  const [gameEditing, setGameEditing] = useState(false);
  const [gameBusy, setGameBusy] = useState(false);
  const [gameForm, setGameForm] = useState({ robloxUsername: '', robloxUserId: '', discordUserId: '' });

  const loadProfile = useCallback(async () => {
    setLoading(true);
    setError('');
    try {
      const data = await api.myProfile();
      setProfile(data);
      setDiscordInput(data.discordUserId ?? '');
      setDiscordEditing(false);
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    loadProfile();
  }, [loadProfile]);

  async function handleDiscordSave() {
    setDiscordBusy(true);
    setNotice('');
    setError('');
    try {
      const result = await api.updateDiscord(discordInput.trim());
      setNotice(result.message);
      await loadProfile();
    } catch (err) {
      setError(err.message);
    } finally {
      setDiscordBusy(false);
    }
  }

  async function handleDiscordUnlink() {
    if (!window.confirm('Unlink your Discord account? The Discord bot will no longer recognize you.')) return;
    setDiscordBusy(true);
    setNotice('');
    setError('');
    try {
      const result = await api.updateDiscord('');
      setNotice(result.message);
      await loadProfile();
    } catch (err) {
      setError(err.message);
    } finally {
      setDiscordBusy(false);
    }
  }

  function startGameEditing() {
    setGameForm({
      robloxUsername: profile.robloxUsername ?? '',
      robloxUserId: profile.robloxUserId != null ? String(profile.robloxUserId) : '',
      discordUserId: profile.discordUserId ?? ''
    });
    setGameEditing(true);
  }

  async function handleGameInfoSave(event) {
    event.preventDefault();
    setGameBusy(true);
    setError('');
    setNotice('');
    try {
      const result = await api.updateGameInfo({
        robloxUsername: gameForm.robloxUsername.trim(),
        robloxUserId: gameForm.robloxUserId.trim() ? Number(gameForm.robloxUserId.trim()) : null,
        discordUserId: gameForm.discordUserId.trim()
      });
      setNotice(result.message ?? 'Game info saved.');
      setGameEditing(false);
      await loadProfile();
    } catch (err) {
      setError(err.message);
    } finally {
      setGameBusy(false);
    }
  }

  async function handlePasswordChange(event) {
    event.preventDefault();
    setError('');
    setNotice('');
    if (pwForm.next !== pwForm.confirm) {
      setError('New passwords do not match.');
      return;
    }
    if (pwForm.next.length < 6) {
      setError('New password must be at least 6 characters.');
      return;
    }
    setPwBusy(true);
    try {
      const result = await api.changePassword({ currentPassword: pwForm.current, newPassword: pwForm.next });
      setNotice(result.message ?? 'Password updated successfully.');
      setPwForm({ current: '', next: '', confirm: '' });
      setPwEditing(false);
    } catch (err) {
      setError(err.message);
    } finally {
      setPwBusy(false);
    }
  }

  if (loading) {
    return (
      <div className="min-h-screen bg-[#050510] px-5 py-8 text-zinc-50">
        <div className="mx-auto max-w-3xl text-sm text-mist">Loading profile...</div>
      </div>
    );
  }

  if (!profile) {
    return (
      <div className="min-h-screen bg-[#050510] px-5 py-8 text-zinc-50">
        <div className="mx-auto max-w-3xl">
          <button type="button" onClick={onBack} className="mb-4 inline-flex items-center gap-1.5 text-sm text-mist hover:text-zinc-200 transition">
            <ArrowLeft className="h-4 w-4" /> Back to tracker
          </button>
          <div className="rounded-xl border border-red-400/30 bg-red-400/10 px-4 py-3 text-sm text-red-100">{error || 'Could not load profile.'}</div>
        </div>
      </div>
    );
  }

  const player = profile.player;
  const joinRequest = profile.joinRequest;
  const discordLinked = !!profile.discordUserId;

  return (
    <div className="min-h-screen bg-[#050510] px-5 py-6 text-zinc-50">
      <div className="mx-auto max-w-3xl space-y-5">
        {/* Header */}
        <div className="flex items-center justify-between">
          <button
            type="button"
            onClick={onBack}
            className="inline-flex items-center gap-1.5 rounded-lg border border-neon-cyan/[0.08] px-3 py-2 text-sm font-medium text-zinc-300 transition hover:bg-neon-cyan/[0.06]"
          >
            <ArrowLeft className="h-4 w-4" />
            Back to tracker
          </button>
          <h1 className="flex items-center gap-2 text-xl font-bold">
            <User className="h-5 w-5 text-neon-cyan" />
            My Profile
          </h1>
        </div>

        {/* Notices */}
        {notice && (
          <div className="flex items-center gap-2 rounded-lg border border-emerald-400/30 bg-emerald-400/10 px-4 py-3 text-sm text-emerald-100">
            <CheckCircle className="h-4 w-4 shrink-0" />
            {notice}
          </div>
        )}
        {error && (
          <div className="flex items-center gap-2 rounded-lg border border-red-400/30 bg-red-400/10 px-4 py-3 text-sm text-red-100">
            <AlertCircle className="h-4 w-4 shrink-0" />
            {error}
          </div>
        )}

        {/* ─── ACCOUNT ─── */}
        <Section icon={ShieldCheck} title="Account" accent="text-neon-green">
          <div className="divide-y divide-neon-cyan/10">
            <InfoRow label="Website username" value={profile.username} />
            <InfoRow label="Role" value={profile.role === 'Admin' ? 'Admin' : 'User'} />
            <InfoRow label="Member since" value={formatDateTime(profile.createdAt)} />
          </div>

          {pwEditing ? (
            <form onSubmit={handlePasswordChange} className="mt-4 space-y-2 rounded-lg border border-line bg-ink p-3">
              <div className="flex items-center gap-2 text-sm font-semibold text-zinc-200">
                <KeyRound className="h-4 w-4 text-neon-green" />
                Change password
              </div>
              <input
                type={showPw ? 'text' : 'password'}
                value={pwForm.current}
                onChange={(e) => setPwForm((c) => ({ ...c, current: e.target.value }))}
                placeholder="Current password"
                className="w-full min-h-9 rounded-md border border-line bg-panel px-3 text-sm text-zinc-50 placeholder:text-zinc-500"
                required
                autoComplete="current-password"
              />
              <div className="relative">
                <input
                  type={showPw ? 'text' : 'password'}
                  value={pwForm.next}
                  onChange={(e) => setPwForm((c) => ({ ...c, next: e.target.value }))}
                  placeholder="New password (min 6 characters)"
                  className="w-full min-h-9 rounded-md border border-line bg-panel px-3 pr-10 text-sm text-zinc-50 placeholder:text-zinc-500"
                  required
                  minLength={6}
                  autoComplete="new-password"
                />
                <button
                  type="button"
                  onClick={() => setShowPw(!showPw)}
                  className="absolute right-3 top-1/2 -translate-y-1/2 text-zinc-500 transition hover:text-zinc-300"
                  tabIndex={-1}
                >
                  {showPw ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
                </button>
              </div>
              <input
                type={showPw ? 'text' : 'password'}
                value={pwForm.confirm}
                onChange={(e) => setPwForm((c) => ({ ...c, confirm: e.target.value }))}
                placeholder="Repeat new password"
                className="w-full min-h-9 rounded-md border border-line bg-panel px-3 text-sm text-zinc-50 placeholder:text-zinc-500"
                required
                minLength={6}
                autoComplete="new-password"
              />
              <div className="flex gap-2 pt-1">
                <button
                  type="submit"
                  disabled={pwBusy}
                  className="inline-flex min-h-9 items-center rounded-lg bg-neon-green px-4 text-sm font-semibold text-zinc-950 transition hover:bg-neon-green/80 disabled:opacity-60"
                >
                  {pwBusy ? 'Saving...' : 'Update password'}
                </button>
                <button
                  type="button"
                  onClick={() => { setPwEditing(false); setPwForm({ current: '', next: '', confirm: '' }); }}
                  className="inline-flex min-h-9 items-center rounded-lg border border-line px-4 text-sm font-medium text-zinc-300 transition hover:bg-zinc-800"
                >
                  Cancel
                </button>
              </div>
            </form>
          ) : (
            <button
              type="button"
              onClick={() => setPwEditing(true)}
              className="mt-4 inline-flex min-h-9 items-center gap-1.5 rounded-lg border border-neon-cyan/[0.08] px-4 text-sm font-medium text-zinc-200 transition hover:bg-neon-cyan/[0.06]"
            >
              <KeyRound className="h-4 w-4" />
              Change password
            </button>
          )}
        </Section>

        {/* ─── GAME INFO ─── */}
        <Section icon={Gamepad2} title="Game Info" accent="text-neon-cyan">
          <p className="mb-3 text-xs text-mist">
            Your game details. This is what your join request will use — keep it up to date so admins can add you to the tracker.
          </p>
          {gameEditing ? (
            <form onSubmit={handleGameInfoSave} className="space-y-3">
              <div>
                <label htmlFor="gi-username" className="mb-1 block text-xs font-medium text-zinc-400">Roblox username</label>
                <input
                  id="gi-username"
                  type="text"
                  value={gameForm.robloxUsername}
                  onChange={(e) => setGameForm((c) => ({ ...c, robloxUsername: e.target.value }))}
                  placeholder="Your Roblox username"
                  className="w-full min-h-10 rounded-lg border border-line bg-ink px-3 text-sm text-zinc-50 placeholder:text-zinc-500"
                  maxLength={100}
                />
              </div>
              <div>
                <label htmlFor="gi-userid" className="mb-1 block text-xs font-medium text-zinc-400">Roblox user ID</label>
                <input
                  id="gi-userid"
                  type="text"
                  inputMode="numeric"
                  value={gameForm.robloxUserId}
                  onChange={(e) => setGameForm((c) => ({ ...c, robloxUserId: e.target.value.replace(/[^0-9]/g, '') }))}
                  placeholder="e.g. 2243793833"
                  className="w-full min-h-10 rounded-lg border border-line bg-ink px-3 text-sm text-zinc-50 placeholder:text-zinc-500"
                />
                <p className="mt-1 text-[11px] text-zinc-500">Find it on your Roblox profile page URL: roblox.com/users/&lt;id&gt;/profile</p>
                {gameForm.robloxUserId ? (
                  <a
                    href={`https://www.roblox.com/users/${gameForm.robloxUserId}/profile`}
                    target="_blank"
                    rel="noreferrer"
                    className="mt-1 inline-flex items-center gap-1 text-[11px] text-neon-cyan hover:text-neon-cyan/80"
                  >
                    <ExternalLink className="h-3 w-3" />
                    Preview your profile
                  </a>
                ) : null}
              </div>
              <div>
                <label htmlFor="gi-discord" className="mb-1 block text-xs font-medium text-zinc-400">Discord User ID (optional)</label>
                <input
                  id="gi-discord"
                  type="text"
                  inputMode="numeric"
                  value={gameForm.discordUserId}
                  onChange={(e) => setGameForm((c) => ({ ...c, discordUserId: e.target.value.replace(/\s/g, '') }))}
                  placeholder="123456789012345678"
                  className="w-full min-h-10 rounded-lg border border-line bg-ink px-3 text-sm text-zinc-50 placeholder:text-zinc-500"
                  maxLength={20}
                />
                <p className="mt-1 text-[11px] text-zinc-500">{DISCORD_COPY_HINT}</p>
              </div>
              <div className="flex gap-2">
                <button
                  type="submit"
                  disabled={gameBusy}
                  className="inline-flex min-h-9 items-center gap-1.5 rounded-lg bg-neon-cyan px-4 text-sm font-semibold text-zinc-950 transition hover:bg-neon-cyan/80 disabled:opacity-60"
                >
                  <Save className="h-4 w-4" />
                  {gameBusy ? 'Saving...' : 'Save game info'}
                  </button>
                <button
                  type="button"
                  onClick={() => setGameEditing(false)}
                  className="inline-flex min-h-9 items-center rounded-lg border border-line px-4 text-sm font-medium text-zinc-300 transition hover:bg-zinc-800"
                >
                  Cancel
                </button>
              </div>
            </form>
          ) : (
            <>
              <div className="divide-y divide-neon-cyan/10">
                <InfoRow label="Roblox username" value={profile.robloxUsername || <span className="text-zinc-500">Not set</span>} />
                <InfoRow label="Roblox user ID" value={profile.robloxUserId ?? <span className="text-zinc-500">Not set</span>} mono />
                <InfoRow
                  label="Discord ID"
                  value={profile.discordUserId || <span className="text-zinc-500">Not set</span>}
                  mono
                  action={
                    <button
                      type="button"
                      onClick={startGameEditing}
                      className="text-zinc-500 transition hover:text-zinc-300"
                      title="Edit game info"
                    >
                      <Pencil className="h-3 w-3" />
                    </button>
                  }
                />
              </div>
              {!profile.player && (
                <button
                  type="button"
                  onClick={startGameEditing}
                  className="mt-3 inline-flex min-h-9 items-center gap-1.5 rounded-lg border border-neon-cyan/[0.08] px-4 text-sm font-medium text-zinc-200 transition hover:bg-neon-cyan/[0.06]"
                >
                  <Pencil className="h-4 w-4" />
                  {profile.robloxUsername || profile.robloxUserId ? 'Edit game info' : 'Add your game info'}
                </button>
                )}
              </>
          )}
        </Section>

        {/* ─── ROBLOX ─── */}
        <Section icon={Gamepad2} title="Roblox" accent="text-neon-cyan">
          {player ? (
            <>
              <div className="flex items-center gap-4">
                {player.avatarUrl ? (
                  <img src={player.avatarUrl} alt="" className="h-16 w-16 rounded-xl border border-neon-cyan/[0.12] object-cover" />
                ) : (
                  <div className="grid h-16 w-16 place-items-center rounded-xl border border-neon-cyan/[0.12] bg-panelSoft text-xl font-bold text-neon-cyan/80">
                    {player.username.slice(0, 1).toUpperCase()}
                  </div>
                )}
                <div>
                  <div className="text-lg font-semibold text-zinc-50">{player.username}</div>
                  <div className="mt-0.5 flex items-center gap-1.5 font-mono text-sm text-mist">
                    ID: {player.robloxUserId}
                    <button
                      type="button"
                      onClick={() => navigator.clipboard.writeText(String(player.robloxUserId))}
                      className="text-zinc-500 transition hover:text-zinc-300"
                      title="Copy Roblox ID"
                    >
                      <Copy className="h-3 w-3" />
                    </button>
                  </div>
                </div>
              </div>
              <a
                href={player.profileUrl}
                target="_blank"
                rel="noreferrer"
                className="mt-3 inline-flex items-center gap-1.5 text-sm text-neon-cyan transition hover:text-neon-cyan/80"
              >
                Open Roblox profile
                <ExternalLink className="h-3.5 w-3.5" />
              </a>
            </>
          ) : (
            <p className="text-sm text-mist">
              No Roblox account linked yet. Claim your tracker account during registration, or request to join the tracker below.
            </p>
          )}
        </Section>

        {/* ─── DISCORD ─── (covered by Game Info for tracker members) ─── */}
        {!player && (
          <Section icon={Link2} title="Discord" accent="text-neon-purple">
            <div className="mb-3 flex items-center gap-2">
            {discordLinked ? (
              <span className="inline-flex items-center gap-1.5 rounded-md border border-emerald-400/30 bg-emerald-400/10 px-2 py-1 text-[10px] font-semibold uppercase tracking-wider text-emerald-300">
                <CheckCircle className="h-3 w-3" />
                Linked
              </span>
            ) : (
              <span className="inline-flex items-center gap-1.5 rounded-md border border-zinc-500/30 bg-zinc-500/10 px-2 py-1 text-[10px] font-semibold uppercase tracking-wider text-zinc-300">
                <Unlink className="h-3 w-3" />
                Not linked
              </span>
            )}
            {discordLinked && <span className="font-mono text-sm text-zinc-200">{profile.discordUserId}</span>}
          </div>

          {discordEditing ? (
            <div className="space-y-2">
              <input
                type="text"
                inputMode="numeric"
                value={discordInput}
                onChange={(e) => setDiscordInput(e.target.value.replace(/\s/g, ''))}
                placeholder="123456789012345678"
                className="w-full min-h-10 rounded-lg border border-line bg-ink px-3 text-sm text-zinc-50 placeholder:text-zinc-500"
                maxLength={20}
                minLength={17}
              />
              <p className="text-xs text-mist">{DISCORD_COPY_HINT}</p>
              <div className="flex gap-2">
                <button
                  type="button"
                  onClick={handleDiscordSave}
                  disabled={discordBusy}
                  className="inline-flex min-h-9 items-center gap-1.5 rounded-lg bg-neon-cyan px-4 text-sm font-semibold text-zinc-950 transition hover:bg-neon-cyan/80 disabled:opacity-60"
                >
                  <Link2 className="h-4 w-4" />
                  {discordBusy ? 'Saving...' : 'Save'}
                </button>
                <button
                  type="button"
                  onClick={() => { setDiscordEditing(false); setDiscordInput(profile.discordUserId ?? ''); }}
                  className="inline-flex min-h-9 items-center rounded-lg border border-line px-4 text-sm font-medium text-zinc-300 transition hover:bg-zinc-800"
                >
                  Cancel
                </button>
              </div>
            </div>
          ) : (
            <div className="flex flex-wrap gap-2">
              <button
                type="button"
                onClick={() => setDiscordEditing(true)}
                className="inline-flex min-h-9 items-center gap-1.5 rounded-lg border border-neon-cyan/[0.08] px-4 text-sm font-medium text-zinc-200 transition hover:bg-neon-cyan/[0.06]"
              >
                <Link2 className="h-4 w-4" />
                {discordLinked ? 'Change Discord ID' : 'Link Discord ID'}
              </button>
              {discordLinked && (
                <button
                  type="button"
                  onClick={handleDiscordUnlink}
                  disabled={discordBusy}
                  className="inline-flex min-h-9 items-center gap-1.5 rounded-lg border border-red-400/30 bg-red-400/10 px-4 text-sm font-semibold text-red-200 transition hover:bg-red-400/20 disabled:opacity-60"
                >
                  <Trash2 className="h-4 w-4" />
                  Unlink
                </button>
              )}
            </div>
          )}
          <p className="mt-3 text-xs text-mist">
            Linking Discord lets the bot's <span className="font-mono text-zinc-300">/playtime</span> command find your stats.
          </p>
          </Section>
        )}

        {/* ─── TRACKER ─── */}
        <Section icon={Trophy} title="Tracker" accent="text-neon-amber">
          {player ? (
            <>
              <div className="mb-3 flex items-center gap-2">
                <span className="inline-flex items-center gap-1.5 rounded-md border border-emerald-400/30 bg-emerald-400/10 px-2 py-1 text-[10px] font-semibold uppercase tracking-wider text-emerald-300">
                  <CheckCircle className="h-3 w-3" />
                  Member
                </span>
                <span className="text-sm text-mist">
                  Club: <span className="font-semibold text-zinc-200">{player.club || 'None'}</span>
                </span>
              </div>
              <div className="grid grid-cols-2 gap-2 sm:grid-cols-3">
                <StatTile label="Today" value={formatDuration(player.todayPlaySeconds)} color="text-neon-amber" />
                <StatTile label="Week" value={formatDuration(player.weeklyPlaySeconds)} color="text-neon-cyan" />
                <StatTile label="Month" value={formatDuration(player.monthlyPlaySeconds)} color="text-neon-purple" />
                <StatTile label="Total" value={formatDuration(player.totalPlaySeconds)} color="text-emerald-400" />
                <StatTile label="Weekly Rank" value={profile.weeklyLeaderboardPosition ? `#${profile.weeklyLeaderboardPosition}` : '—'} />
                <StatTile label="All-Time Rank" value={profile.totalLeaderboardPosition ? `#${profile.totalLeaderboardPosition}` : '—'} />
              </div>
            </>
          ) : joinRequest ? (
            <div className="space-y-2">
              <div className="flex items-center gap-2">
                {joinRequest.status === 'Pending' && (
                  <span className="inline-flex items-center gap-1.5 rounded-md border border-amber-400/30 bg-amber-400/10 px-2 py-1 text-[10px] font-semibold uppercase tracking-wider text-amber-300">
                    Request pending
                  </span>
                )}
                {joinRequest.status === 'Approved' && (
                  <span className="inline-flex items-center gap-1.5 rounded-md border border-emerald-400/30 bg-emerald-400/10 px-2 py-1 text-[10px] font-semibold uppercase tracking-wider text-emerald-300">
                    Approved — linking soon
                  </span>
                )}
                {joinRequest.status === 'Rejected' && (
                  <span className="inline-flex items-center gap-1.5 rounded-md border border-red-400/30 bg-red-400/10 px-2 py-1 text-[10px] font-semibold uppercase tracking-wider text-red-300">
                    Declined — you can reapply
                  </span>
                )}
              </div>
              <div className="rounded-lg border border-line bg-ink p-3 text-sm text-mist">
                <div>Roblox: <span className="text-zinc-200">{joinRequest.robloxUsername}</span> (ID {joinRequest.robloxUserId})</div>
                <div>Club: <span className="text-zinc-200">{joinRequest.club || 'None'}</span></div>
                <div>Submitted: <span className="text-zinc-200">{formatDateTime(joinRequest.createdAt)}</span></div>
                {joinRequest.reviewedAt && (
                  <div>Reviewed: <span className="text-zinc-200">{formatDateTime(joinRequest.reviewedAt)}{joinRequest.reviewedBy ? ` by ${joinRequest.reviewedBy}` : ''}</span></div>
                )}
              </div>
            </div>
          ) : (
            <p className="text-sm text-mist">
              You are not in the tracker yet. Use "Request to Join" on the main page to submit a request.
            </p>
          )}
        </Section>
      </div>
    </div>
  );
}
