import { useCallback, useEffect, useState } from 'react';
import {
  Activity,
  AlertCircle,
  ArrowLeft,
  BarChart3,
  CalendarDays,
  Flame,
  Medal,
  RefreshCw,
  Search,
  Target,
  TrendingUp
} from 'lucide-react';
import {
  Area,
  AreaChart,
  Bar,
  BarChart,
  CartesianGrid,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis
} from 'recharts';
import { api } from '../lib/api';
import { formatDuration, shortDate } from '../lib/format';
import Header from './Header';

const CHART_COLORS = {
  cyan: '#00e5ff',
  purple: '#b347ea',
  amber: '#ffab00'
};

const tooltipStyle = {
  background: '#0a0a1a',
  border: '1px solid rgba(0,229,255,0.2)',
  borderRadius: 8,
  color: '#f4f4f5',
  fontSize: 12
};

function StatTile({ label, value, icon: Icon, accent = 'text-neon-cyan', sub }) {
  return (
    <div className="rounded-lg border border-neon-cyan/[0.08] bg-ink p-3">
      <div className="flex items-center gap-1.5 text-[10px] font-semibold uppercase tracking-wider text-zinc-500">
        <Icon className={`h-3 w-3 ${accent}`} />
        {label}
      </div>
      <div className="mt-1 text-lg font-bold text-zinc-50">{value}</div>
      {sub && <div className="mt-0.5 text-[10px] text-zinc-500">{sub}</div>}
    </div>
  );
}

function ChartCard({ title, icon: Icon, accent, data, type = 'area', height = 160 }) {
  const dataKey = 'playSeconds';
  return (
    <div className="rounded-lg border border-neon-cyan/[0.08] bg-ink p-4">
      <div className="mb-3 flex items-center gap-2 text-sm font-semibold text-zinc-100">
        <Icon className={`h-4 w-4 ${accent}`} />
        {title}
      </div>
      <div style={{ height }}>
        <ResponsiveContainer width="100%" height="100%">
          {type === 'area' ? (
            <AreaChart data={data} margin={{ top: 5, right: 5, left: 0, bottom: 0 }}>
              <defs>
                <linearGradient id={`grad-${title.replace(/\s/g, '')}`} x1="0" y1="0" x2="0" y2="1">
                  <stop offset="0%" stopColor={CHART_COLORS.cyan} stopOpacity={0.3} />
                  <stop offset="100%" stopColor={CHART_COLORS.cyan} stopOpacity={0} />
                </linearGradient>
              </defs>
              <CartesianGrid stroke="#2a2a33" vertical={false} />
              <XAxis dataKey="label" stroke="#52525b" tickLine={false} axisLine={false} tick={{ fontSize: 10 }} minTickGap={24} />
              <YAxis stroke="#52525b" tickLine={false} axisLine={false} tick={{ fontSize: 10 }} tickFormatter={(v) => formatDuration(v)} width={52} />
              <Tooltip
                contentStyle={tooltipStyle}
                formatter={(value) => [formatDuration(value), 'Playtime']}
                cursor={{ stroke: CHART_COLORS.cyan, strokeWidth: 1 }}
              />
              <Area type="monotone" dataKey={dataKey} stroke={CHART_COLORS.cyan} fill={`url(#grad-${title.replace(/\s/g, '')})`} strokeWidth={2} />
            </AreaChart>
          ) : (
            <BarChart data={data} margin={{ top: 5, right: 5, left: 0, bottom: 0 }}>
              <CartesianGrid stroke="#2a2a33" vertical={false} />
              <XAxis dataKey="label" stroke="#52525b" tickLine={false} axisLine={false} tick={{ fontSize: 10 }} minTickGap={24} />
              <YAxis stroke="#52525b" tickLine={false} axisLine={false} tick={{ fontSize: 10 }} tickFormatter={(v) => formatDuration(v)} width={52} />
              <Tooltip
                contentStyle={tooltipStyle}
                formatter={(value) => [formatDuration(value), 'Playtime']}
                cursor={{ fill: 'rgba(0,229,255,0.05)' }}
              />
              <Bar dataKey={dataKey} fill={CHART_COLORS.purple} radius={[3, 3, 0, 0]} />
            </BarChart>
          )}
        </ResponsiveContainer>
      </div>
    </div>
  );
}

export default function PlayerStatsPage({ playerId: playerIdProp, user, avatarUrl, isAdmin, onLogout, onSignIn }) {
  const [playerId, setPlayerId] = useState(playerIdProp ?? null);
  const [playerQuery, setPlayerQuery] = useState('');
  const [playerOptions, setPlayerOptions] = useState([]);
  const [stats, setStats] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [showSearch, setShowSearch] = useState(false);

  // Resolve which player to show. A route param (#/stats/{id}) always wins —
  // that's how the player detail panel deep-links here. Without one, show the
  // signed-in user's linked player, else the first tracker player.
  // The profile and dashboard calls run in PARALLEL (they used to be awaited in
  // sequence, doubling the wait before the stats request could even start; both
  // are output-cached and micro-cached, so repeat visits are instant).
  useEffect(() => {
    let cancelled = false;
    async function resolveDefault() {
      const dashboardPromise = api.dashboard().catch(() => null);
      let profilePromise = null;
      if (user && playerIdProp == null) {
        profilePromise = api.myProfile().catch(() => null);
      }

      if (playerIdProp) {
        setPlayerId(playerIdProp);
        const dashboard = await dashboardPromise;
        if (!cancelled && dashboard) setPlayerOptions(dashboard);
        return;
      }

      const [profile, dashboard] = await Promise.all([profilePromise, dashboardPromise]);
      if (cancelled) return;

      if (profile?.player?.id) {
        setPlayerId(profile.player.id);
      }
      if (dashboard) {
        setPlayerOptions(dashboard);
        setPlayerId((current) => current ?? (dashboard.length > 0 ? dashboard[0].id : null));
      }
      if (!profile?.player?.id && !dashboard) {
        setError('Could not load the tracker data. Please refresh.');
      }
    }
    resolveDefault();
    return () => {
      cancelled = true;
    };
  }, [playerIdProp, user]);

  const loadStats = useCallback(async (id) => {
    if (!id) return;
    setLoading(true);
    setStats(null); // avoid flashing the previous player's numbers
    try {
      const data = await api.playerStats(id);
      setStats(data);
      setError('');
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    loadStats(playerId);
  }, [playerId, loadStats]);

  const filteredOptions = playerOptions.filter((p) => {
    const q = playerQuery.trim().toLowerCase();
    if (!q) return true;
    return p.username.toLowerCase().includes(q) || String(p.robloxUserId).includes(q);
  });

  const selectedPlayer = playerOptions.find((p) => p.id === playerId);

  return (
    <div className="min-h-screen bg-[#050510] text-zinc-50">
      <Header user={user} avatarUrl={avatarUrl} isAdmin={isAdmin} onLogout={onLogout} onSignIn={onSignIn} />

      <div className="mx-auto max-w-5xl space-y-5 px-5 py-6">
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
            <TrendingUp className="h-5 w-5 text-neon-cyan" />
            Player Statistics
          </h1>
        </div>

        {/* Player selector: open by default for visitors, behind a toggle for signed-in users */}
        {(showSearch || !user) && (
          <div className="rounded-xl border border-neon-cyan/[0.08] bg-[#08081a] p-4">
            <div className="relative">
              <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-zinc-500" />
              <input
                value={playerQuery}
                onChange={(e) => setPlayerQuery(e.target.value)}
                placeholder="Search player to view statistics..."
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
                    onClick={() => { setPlayerId(p.id); setPlayerQuery(''); window.location.hash = `stats/${p.id}`; }}
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
                    <span className="text-xs text-mist">{formatDuration(p.totalPlaySeconds)}</span>
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

        {loading && !stats && <div className="py-10 text-center text-sm text-mist">Loading statistics...</div>}

        {stats && (
          <>
            {/* Player identity */}
            <div className="flex items-center gap-3 rounded-xl border border-neon-cyan/[0.08] bg-[#08081a] p-4">
              {stats.avatarUrl ? (
                <img src={stats.avatarUrl} alt="" className="h-12 w-12 rounded-xl border border-neon-cyan/[0.12] object-cover" />
              ) : (
                <span className="grid h-12 w-12 place-items-center rounded-xl border border-neon-cyan/[0.12] bg-panelSoft text-lg font-bold text-neon-cyan/80">
                  {stats.username.slice(0, 1).toUpperCase()}
                </span>
              )}
              <div className="min-w-0 flex-1">
                <div className="flex items-center gap-2">
                  <span className="truncate text-lg font-semibold text-zinc-50">{stats.username}</span>
                  <span className="rounded-md border border-neon-purple/40 bg-neon-purple/10 px-1.5 py-0.5 text-[10px] font-semibold uppercase tracking-wider text-neon-purple">
                    {stats.club}
                  </span>
                </div>
                <div className="text-xs text-mist">
                  {stats.lastActiveDate ? `Last active ${shortDate(stats.lastActiveDate)}` : 'No playtime recorded yet'}
                </div>
              </div>
              <div className="flex flex-wrap items-center gap-2">
                <button
                  type="button"
                  onClick={() => setShowSearch((s) => !s)}
                  className="rounded-md border border-neon-cyan/[0.08] px-2.5 py-1.5 text-xs font-medium text-zinc-300 transition hover:bg-neon-cyan/[0.06]"
                >
                  {showSearch ? 'Hide search' : 'View another player'}
                </button>
                <button
                  type="button"
                  onClick={() => loadStats(playerId)}
                  className="grid h-9 w-9 shrink-0 place-items-center rounded-lg border border-neon-cyan/[0.08] text-mist transition hover:bg-neon-cyan/[0.06] hover:text-zinc-100"
                  title="Refresh"
                >
                  <RefreshCw className={`h-4 w-4 ${loading ? 'animate-spin' : ''}`} />
                </button>
              </div>
            </div>

            {/* Core stats */}
            <section className="grid grid-cols-2 gap-3 sm:grid-cols-4">
              <StatTile label="Today" value={formatDuration(stats.todayPlaySeconds)} icon={Activity} accent="text-neon-amber" />
              <StatTile label="This Week" value={formatDuration(stats.weekPlaySeconds)} icon={CalendarDays} accent="text-neon-cyan" />
              <StatTile label="This Month" value={formatDuration(stats.monthPlaySeconds)} icon={CalendarDays} accent="text-neon-purple" />
              <StatTile label="Total" value={formatDuration(stats.totalPlaySeconds)} icon={TrendingUp} accent="text-emerald-400" />
              <StatTile label="Current Streak" value={`${stats.currentStreak} day${stats.currentStreak === 1 ? '' : 's'}`} icon={Flame} accent="text-neon-amber" />
              <StatTile label="Longest Streak" value={`${stats.longestStreak} day${stats.longestStreak === 1 ? '' : 's'}`} icon={Flame} accent="text-neon-green" />
              <StatTile label="Days Played" value={String(stats.daysPlayed)} icon={Target} accent="text-neon-cyan" />
              <StatTile
                label="Avg / Active Day"
                value={formatDuration(stats.averageDailyPlaySeconds)}
                icon={BarChart3}
                accent="text-neon-purple"
              />
            </section>

            {/* Rank + achievements strip */}
            <section className="grid gap-3 sm:grid-cols-2">
              <div className="flex items-center gap-3 rounded-xl border border-neon-cyan/[0.08] bg-[#08081a] p-4">
                <Medal className="h-8 w-8 shrink-0 text-neon-amber" />
                <div>
                  <div className="text-[10px] font-semibold uppercase tracking-wider text-zinc-500">All-Time Rank</div>
                  <div className="text-xl font-bold text-zinc-50">
                    {stats.totalRank ? `#${stats.totalRank}` : '—'}
                  </div>
                </div>
              </div>
              <button
                type="button"
                onClick={() => { window.location.hash = `achievements/${playerId}`; }}
                className="flex items-center gap-3 rounded-xl border border-neon-cyan/[0.08] bg-[#08081a] p-4 text-left transition hover:border-neon-cyan/30"
              >
                <Medal className="h-8 w-8 shrink-0 text-neon-purple" />
                <div className="min-w-0 flex-1">
                  <div className="text-[10px] font-semibold uppercase tracking-wider text-zinc-500">Achievements</div>
                  <div className="text-xl font-bold text-zinc-50">
                    {stats.achievementsUnlocked} / {stats.totalAchievements}
                  </div>
                </div>
                <span className="text-xs font-medium text-neon-cyan">View &rarr;</span>
              </button>
            </section>

            {/* Charts */}
            <div className="space-y-4">
              <ChartCard title="Daily playtime" icon={BarChart3} accent="text-neon-cyan" data={stats.dailyChart.map((d) => ({ label: d.label, playSeconds: d.playSeconds }))} type="area" />
              <ChartCard title="Weekly activity" icon={BarChart3} accent="text-neon-purple" data={stats.weeklyChart.map((d) => ({ label: d.label, playSeconds: d.playSeconds }))} type="bar" />
              <ChartCard title="Monthly activity" icon={BarChart3} accent="text-neon-amber" data={stats.monthlyChart.map((d) => ({ label: d.label, playSeconds: d.playSeconds }))} type="bar" />
            </div>
          </>
        )}
      </div>
    </div>
  );
}
