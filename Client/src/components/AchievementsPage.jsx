import { useCallback, useEffect, useState } from 'react';
import {
  AlertCircle,
  ArrowLeft,
  Lock,
  RefreshCw,
  Search,
  Trophy
} from 'lucide-react';
import { api } from '../lib/api';
import { formatDateTime } from '../lib/format';
import Header from './Header';

function AchievementCard({ achievement }) {
  const unlocked = achievement.unlocked;
  return (
    <div
      className={`rounded-xl border p-4 transition ${
        unlocked
          ? 'border-neon-amber/30 bg-gradient-to-br from-neon-amber/[0.07] to-transparent'
          : 'border-neon-cyan/[0.08] bg-[#08081a]'
      }`}
    >
      <div className="flex items-start gap-3">
        <span
          className={`grid h-12 w-12 shrink-0 place-items-center rounded-xl text-2xl ${
            unlocked
              ? 'bg-neon-amber/15 shadow-[0_0_12px_rgba(255,171,0,0.25)]'
              : 'bg-panelSoft opacity-40 grayscale'
          }`}
        >
          {achievement.icon}
        </span>
        <div className="min-w-0 flex-1">
          <div className="flex items-center gap-2">
            <span className={`truncate text-sm font-semibold ${unlocked ? 'text-neon-amber' : 'text-zinc-300'}`}>
              {achievement.name}
            </span>
            {unlocked ? (
              <Trophy className="h-3.5 w-3.5 shrink-0 text-neon-amber" />
            ) : (
              <Lock className="h-3.5 w-3.5 shrink-0 text-zinc-600" />
            )}
          </div>
          <div className="mt-0.5 text-xs text-mist">{achievement.description}</div>

          {/* Progress */}
          <div className="mt-2.5">
            <div className="flex items-center justify-between text-[10px] text-zinc-500">
              <span>{achievement.progressLabel || (unlocked ? 'Complete' : 'Locked')}</span>
              {unlocked ? (
                <span className="font-semibold text-neon-amber">
                  Unlocked {achievement.unlockedAt ? formatDateTime(achievement.unlockedAt) : ''}
                </span>
              ) : (
                <span>{achievement.progressPercent}%</span>
              )}
            </div>
            <div className="mt-1 h-1.5 w-full overflow-hidden rounded-full bg-panel">
              <div
                className={`h-full rounded-full transition-all ${unlocked ? 'bg-neon-amber' : 'bg-neon-cyan/60'}`}
                style={{ width: `${unlocked ? 100 : Math.min(100, achievement.progressPercent)}%` }}
              />
            </div>
          </div>

          {achievement.detail && (
            <div className="mt-2 rounded-md border border-neon-cyan/[0.08] bg-ink px-2 py-1 text-[11px] text-zinc-400">
              {achievement.detail}
            </div>
          )}
        </div>
      </div>
    </div>
  );
}

export default function AchievementsPage({ playerId: playerIdProp, user, avatarUrl, isAdmin, onLogout, onSignIn }) {
  const [playerId, setPlayerId] = useState(playerIdProp ?? null);
  const [playerQuery, setPlayerQuery] = useState('');
  const [playerOptions, setPlayerOptions] = useState([]);
  const [achievements, setAchievements] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [filter, setFilter] = useState('all'); // all | unlocked | locked
  const [showSearch, setShowSearch] = useState(false);

  // Resolve the player to show: explicit route param, else the signed-in
  // user's linked player, else the first tracker player.
  useEffect(() => {
    let cancelled = false;
    async function resolveDefault() {
      if (playerIdProp) {
        setPlayerId(playerIdProp);
        return;
      }
      if (user) {
        try {
          const profile = await api.myProfile();
          if (!cancelled && profile?.player?.id) {
            setPlayerId(profile.player.id);
            return;
          }
        } catch {
          // fall through to player list
        }
      }
      try {
        const dashboard = await api.dashboard();
        if (!cancelled) {
          setPlayerOptions(dashboard);
          if (dashboard.length > 0) {
            setPlayerId((current) => current ?? dashboard[0].id);
          }
        }
      } catch (err) {
        if (!cancelled) setError(err.message);
      }
    }
    resolveDefault();
    return () => {
      cancelled = true;
    };
  }, [playerIdProp, user]);

  const loadAchievements = useCallback(async (id) => {
    if (!id) return;
    setLoading(true);
    try {
      const data = await api.playerAchievements(id);
      setAchievements(data);
      setError('');
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    loadAchievements(playerId);
  }, [playerId, loadAchievements]);

  const filteredOptions = playerOptions.filter((p) => {
    const q = playerQuery.trim().toLowerCase();
    if (!q) return true;
    return p.username.toLowerCase().includes(q) || String(p.robloxUserId).includes(q);
  });

  const visible = (achievements?.achievements ?? []).filter((a) => {
    if (filter === 'unlocked') return a.unlocked;
    if (filter === 'locked') return !a.unlocked;
    return true;
  });

  return (
    <div className="min-h-screen bg-[#050510] text-zinc-50">
      <Header user={user} avatarUrl={avatarUrl} isAdmin={isAdmin} onLogout={onLogout} onSignIn={onSignIn} />

      <div className="mx-auto max-w-4xl space-y-5 px-5 py-6">
        {/* Header */}
        <div className="flex flex-wrap items-center justify-between gap-3">
          <button
            type="button"
            onClick={() => { window.location.hash = ''; }}
            className="inline-flex items-center gap-1.5 rounded-lg border border-neon-cyan/[0.08] px-3 py-2 text-sm font-medium text-zinc-300 transition hover:bg-neon-cyan/[0.06]"
          >
            <ArrowLeft className="h-4 w-4" />
            Back to tracker
          </button>
          <h1 className="flex items-center gap-2 text-xl font-bold">
            <Trophy className="h-5 w-5 text-neon-amber" />
            Achievements &amp; Badges
          </h1>
        </div>

        {/* Player selector: open by default for visitors, behind a toggle for signed-in users */}
        {(showSearch || !user) && playerIdProp == null && (
          <div className="rounded-xl border border-neon-cyan/[0.08] bg-[#08081a] p-4">
            <div className="relative">
              <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-zinc-500" />
              <input
                value={playerQuery}
                onChange={(e) => setPlayerQuery(e.target.value)}
                placeholder="Search player to view achievements..."
                className="h-10 w-full rounded-lg border border-neon-cyan/[0.08] bg-ink py-2 pl-10 pr-3 text-sm text-zinc-50 placeholder:text-zinc-500 focus:border-zinc-500 transition"
              />
            </div>
            {playerQuery.trim() && (
              <div className="mt-2 max-h-56 space-y-1 overflow-y-auto thin-scrollbar">
                {filteredOptions.length === 0 && (
                  <div className="px-2 py-3 text-sm text-mist">No players match.</div>
                )}
                {filteredOptions.slice(0, 12).map((p) => (
                  <button
                    key={p.id}
                    type="button"
                    onClick={() => { setPlayerId(p.id); setPlayerQuery(''); }}
                    className="flex w-full items-center gap-2.5 rounded-lg px-2.5 py-2 text-left transition hover:bg-neon-cyan/[0.05]"
                  >
                    {p.avatarUrl ? (
                      <img src={p.avatarUrl} alt="" className="h-8 w-8 rounded-lg border border-neon-cyan/[0.12] object-cover" />
                    ) : (
                      <span className="grid h-8 w-8 place-items-center rounded-lg border border-neon-cyan/[0.12] bg-panelSoft text-xs font-bold text-neon-cyan/80">
                        {p.username.slice(0, 1).toUpperCase()}
                      </span>
                    )}
                    <span className="flex-1 truncate text-sm text-zinc-100">{p.username}</span>
                  </button>
                ))}
              </div>
            )}
          </div>
        )}

        {error && (
          <div className="flex items-center gap-2 rounded-lg border border-red-400/30 bg-red-400/10 px-4 py-3 text-sm text-red-100">
            <AlertCircle className="h-4 w-4 shrink-0" />
            {error}
          </div>
        )}

        {loading && !achievements && <div className="py-10 text-center text-sm text-mist">Loading achievements...</div>}

        {achievements && (
          <>
            {/* Player + summary */}
            <div className="flex flex-wrap items-center gap-4 rounded-xl border border-neon-cyan/[0.08] bg-[#08081a] p-4">
              {achievements.avatarUrl ? (
                <img src={achievements.avatarUrl} alt="" className="h-12 w-12 rounded-xl border border-neon-cyan/[0.12] object-cover" />
              ) : (
                <span className="grid h-12 w-12 place-items-center rounded-xl border border-neon-cyan/[0.12] bg-panelSoft text-lg font-bold text-neon-cyan/80">
                  {achievements.username.slice(0, 1).toUpperCase()}
                </span>
              )}
              <div className="min-w-0 flex-1">
                <div className="truncate text-lg font-semibold text-zinc-50">{achievements.username}</div>
                <div className="text-xs text-mist">
                  {achievements.unlockedCount} of {achievements.totalCount} achievements unlocked
                </div>
              </div>
              <div className="flex items-center gap-3">
                <span className="text-2xl font-bold text-neon-amber">
                  {Math.round((achievements.unlockedCount / Math.max(1, achievements.totalCount)) * 100)}%
                </span>
                <button
                  type="button"
                  onClick={() => setShowSearch((s) => !s)}
                  className="rounded-md border border-neon-cyan/[0.08] px-2.5 py-1.5 text-xs font-medium text-zinc-300 transition hover:bg-neon-cyan/[0.06]"
                >
                  {showSearch ? 'Hide search' : 'View another player'}
                </button>
                <button
                  type="button"
                  onClick={() => loadAchievements(playerId)}
                  className="grid h-9 w-9 shrink-0 place-items-center rounded-lg border border-neon-cyan/[0.08] text-mist transition hover:bg-neon-cyan/[0.06] hover:text-zinc-100"
                  title="Refresh"
                >
                  <RefreshCw className={`h-4 w-4 ${loading ? 'animate-spin' : ''}`} />
                </button>
              </div>
            </div>

            {/* Filter chips */}
            <div className="flex items-center gap-1.5">
              {[
                { value: 'all', label: 'All' },
                { value: 'unlocked', label: `Unlocked (${achievements.unlockedCount})` },
                { value: 'locked', label: `Locked (${achievements.totalCount - achievements.unlockedCount})` }
              ].map(({ value, label }) => (
                <button
                  key={value}
                  type="button"
                  onClick={() => setFilter(value)}
                  className={`inline-flex h-7 items-center rounded-md border px-2.5 text-xs font-medium transition ${
                    filter === value
                      ? 'border-zinc-500 bg-zinc-700/50 text-zinc-50'
                      : 'border-transparent text-zinc-400 hover:text-zinc-200'
                  }`}
                >
                  {label}
                </button>
              ))}
            </div>

            {/* Grid */}
            <div className="grid gap-3 md:grid-cols-2">
              {visible.map((a) => (
                <AchievementCard key={a.key} achievement={a} />
              ))}
              {visible.length === 0 && (
                <div className="rounded-xl border border-neon-cyan/[0.08] bg-[#08081a] p-6 text-sm text-mist md:col-span-2">
                  No achievements in this view yet.
                </div>
              )}
            </div>
          </>
        )}
      </div>
    </div>
  );
}
