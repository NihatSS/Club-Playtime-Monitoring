import { useCallback, useEffect, useMemo, useState } from 'react';
import {
  Activity,
  ArrowDown,
  ArrowUp,
  BarChart3,
  CalendarClock,
  Circle,
  Clock,
  Download,
  Gamepad2,
  LayoutGrid,
  Play,
  RefreshCw,
  Search,
  Table2,
  Trash2,
  Trophy,
  UserPlus,
  X
} from 'lucide-react';
import {
  Bar,
  BarChart,
  CartesianGrid,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis
} from 'recharts';
import { api } from './lib/api';
import { formatDateTime, formatDuration, shortDate } from './lib/format';

const sortLabels = {
  daily: 'Daily playtime',
  total: 'Total playtime',
  username: 'Username'
};

function statusMeta(status) {
  if (status?.startsWith('Playing')) {
    return {
      label: status,
      dot: 'bg-emerald-400',
      badge: 'border-emerald-400/30 bg-emerald-400/10 text-emerald-200'
    };
  }

  if (status === 'Online') {
    return {
      label: 'Online',
      dot: 'bg-green-400',
      badge: 'border-green-400/30 bg-green-400/10 text-green-200'
    };
  }

  return {
    label: 'Offline',
    dot: 'bg-zinc-300',
    badge: 'border-zinc-300/30 bg-zinc-300/10 text-zinc-200'
  };
}

function Avatar({ player, size = 'md' }) {
  const sizeClass = size === 'lg' ? 'h-16 w-16 text-xl' : 'h-12 w-12 text-base';
  const initial = player?.username?.slice(0, 1)?.toUpperCase() ?? '?';

  if (player?.avatarUrl) {
    return (
      <img
        src={player.avatarUrl}
        alt={`${player.username} avatar`}
        className={`${sizeClass} rounded-md border border-line bg-panelSoft object-cover`}
      />
    );
  }

  return (
    <div className={`${sizeClass} grid place-items-center rounded-md border border-line bg-panelSoft font-semibold text-zinc-200`}>
      {initial}
    </div>
  );
}

function Metric({ icon: Icon, label, value }) {
  return (
    <div className="rounded-lg border border-line bg-panel p-4 shadow-glow">
      <div className="flex items-center gap-2 text-xs font-medium uppercase tracking-wide text-mist">
        <Icon className="h-4 w-4" />
        {label}
      </div>
      <div className="mt-3 text-2xl font-semibold text-zinc-50">{value}</div>
    </div>
  );
}

function SplitMetric({ icon: Icon, label, pihValue, p1hValue }) {
  return (
    <div className="rounded-lg border border-line bg-panel p-4 shadow-glow">
      <div className="flex items-center gap-2 text-xs font-medium uppercase tracking-wide text-mist">
        <Icon className="h-4 w-4" />
        {label}
      </div>
      <div className="mt-3 flex items-center gap-3">
        <div className="flex items-center gap-1.5">
          <span className="h-2 w-2 rounded-full bg-violet-400" />
          <span className="text-lg font-semibold text-violet-200">{pihValue}</span>
        </div>
        <span className="text-xs text-mist">/</span>
        <div className="flex items-center gap-1.5">
          <span className="h-2 w-2 rounded-full bg-amber-400" />
          <span className="text-lg font-semibold text-amber-200">{p1hValue}</span>
        </div>
      </div>
    </div>
  );
}

function PlayerCard({ player, onSelect, selected }) {
  const meta = statusMeta(player.currentStatus);
  const clubColor = player.club === 'PIH' ? 'border-violet-400/30 bg-violet-400/10 text-violet-200' : 'border-amber-400/30 bg-amber-400/10 text-amber-200';

  return (
    <button
      type="button"
      onClick={() => onSelect(player.id)}
      className={`group min-h-[220px] rounded-lg border bg-panel p-4 text-left shadow-glow transition hover:-translate-y-0.5 hover:border-zinc-500 ${
        selected ? 'border-sky-400/70' : 'border-line'
      }`}
    >
      <div className="flex items-start justify-between gap-3">
        <div className="flex min-w-0 items-center gap-3">
          <Avatar player={player} />
          <div className="min-w-0">
            <div className="flex items-center gap-2">
              <span className="truncate text-base font-semibold text-zinc-50">{player.username}</span>
              <span className={`inline-flex shrink-0 items-center rounded-md border px-1.5 py-0.5 text-[10px] font-semibold uppercase tracking-wider ${clubColor}`}>
                {player.club}
              </span>
            </div>
            <div className="mt-1 truncate text-sm text-mist">#{player.robloxUserId}</div>
            {player.discordUserId && <div className="mt-0.5 text-xs text-sky-300/70">Discord: {player.discordUserId}</div>}
          </div>
        </div>
        <span className={`inline-flex shrink-0 items-center gap-2 rounded-md border px-2 py-1 text-xs font-medium ${meta.badge}`}>
          <span className={`h-2 w-2 rounded-full ${meta.dot}`} />
          {meta.label}
        </span>
      </div>

      <div className="mt-5 grid grid-cols-2 gap-3">
        <div>
          <div className="text-xs uppercase tracking-wide text-mist">Today</div>
          <div className="mt-1 text-xl font-semibold text-zinc-50">{formatDuration(player.todayPlaySeconds)}</div>
        </div>
        <div>
          <div className="text-xs uppercase tracking-wide text-mist">Total</div>
          <div className="mt-1 text-xl font-semibold text-zinc-50">{formatDuration(player.totalPlaySeconds)}</div>
        </div>
      </div>

      <div className="mt-5 space-y-2 text-sm text-zinc-300">
        <div className="flex items-center gap-2">
          <Gamepad2 className="h-4 w-4 text-sky-300" />
          <span className="truncate">{player.currentGame ?? 'No active game'}</span>
        </div>
        <div className="flex items-center gap-2">
          <CalendarClock className="h-4 w-4 text-emerald-300" />
          <span>{formatDateTime(player.lastSeenPlaying)}</span>
        </div>
      </div>
    </button>
  );
}

function SortButton({ field, sortBy, sortDirection, onSort, children }) {
  const active = sortBy === field;
  const Icon = active && sortDirection === 'asc' ? ArrowUp : ArrowDown;

  return (
    <button
      type="button"
      onClick={() => onSort(field)}
      className={`inline-flex items-center gap-1 font-medium ${active ? 'text-zinc-50' : 'text-mist hover:text-zinc-200'}`}
    >
      {children}
      <Icon className={`h-3.5 w-3.5 ${active ? 'opacity-100' : 'opacity-35'}`} />
    </button>
  );
}

function PlayersTable({ players, selectedId, onSelect, sortBy, sortDirection, onSort }) {
  return (
    <div className="overflow-hidden rounded-lg border border-line bg-panel shadow-glow">
      <div className="overflow-x-auto thin-scrollbar">
        <table className="min-w-[920px] w-full text-left text-sm">
          <thead className="border-b border-line bg-panelSoft text-xs uppercase tracking-wide text-mist">
            <tr>
              <th className="px-4 py-3">Avatar</th>
              <th className="px-4 py-3">
                <SortButton field="username" sortBy={sortBy} sortDirection={sortDirection} onSort={onSort}>
                  Username
                </SortButton>
              </th>
              <th className="px-4 py-3">Club</th>
              <th className="px-4 py-3">Current Status</th>
              <th className="px-4 py-3">
                <SortButton field="daily" sortBy={sortBy} sortDirection={sortDirection} onSort={onSort}>
                  Today
                </SortButton>
              </th>
              <th className="px-4 py-3">
                <SortButton field="total" sortBy={sortBy} sortDirection={sortDirection} onSort={onSort}>
                  Total
                </SortButton>
              </th>
              <th className="px-4 py-3">Current Game</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-line">
            {players.map((player) => {
              const meta = statusMeta(player.currentStatus);
              const clubColor = player.club === 'PIH' ? 'border-violet-400/30 bg-violet-400/10 text-violet-200' : 'border-amber-400/30 bg-amber-400/10 text-amber-200';
              return (
                <tr
                  key={player.id}
                  onClick={() => onSelect(player.id)}
                  className={`cursor-pointer transition hover:bg-zinc-800/40 ${selectedId === player.id ? 'bg-sky-400/10' : ''}`}
                >
                  <td className="px-4 py-3">
                    <Avatar player={player} />
                  </td>
                  <td className="px-4 py-3 font-medium text-zinc-50">
                    <div>{player.username}</div>
                    {player.discordUserId && <div className="mt-0.5 text-xs text-sky-300/70">Discord: {player.discordUserId}</div>}
                  </td>
                  <td className="px-4 py-3">
                    <span className={`inline-flex items-center rounded-md border px-1.5 py-0.5 text-[10px] font-semibold uppercase tracking-wider ${clubColor}`}>
                      {player.club}
                    </span>
                  </td>
                  <td className="px-4 py-3">
                    <span className={`inline-flex items-center gap-2 rounded-md border px-2 py-1 text-xs font-medium ${meta.badge}`}>
                      <span className={`h-2 w-2 rounded-full ${meta.dot}`} />
                      {meta.label}
                    </span>
                  </td>
                  <td className="px-4 py-3 text-zinc-200">{formatDuration(player.todayPlaySeconds)}</td>
                  <td className="px-4 py-3 text-zinc-200">{formatDuration(player.totalPlaySeconds)}</td>
                  <td className="max-w-[240px] truncate px-4 py-3 text-mist">{player.currentGame ?? 'No active game'}</td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>
    </div>
  );
}

function AddPlayerForm({ onAdd, busy }) {
  const [form, setForm] = useState({ username: '', robloxUserId: '', club: 'PIH', discordUserId: '' });

  async function submit(event) {
    event.preventDefault();
    await onAdd({
      username: form.username.trim(),
      robloxUserId: Number(form.robloxUserId),
      club: form.club,
      discordUserId: form.discordUserId.trim()
    });
    setForm({ username: '', robloxUserId: '', club: 'PIH', discordUserId: '' });
  }

  return (
    <form onSubmit={submit} className="grid gap-3 rounded-lg border border-line bg-panel p-4 shadow-glow md:grid-cols-[1fr_160px_160px_140px_auto]">
      <label className="sr-only" htmlFor="username">Username</label>
      <input
        id="username"
        value={form.username}
        onChange={(event) => setForm((current) => ({ ...current, username: event.target.value }))}
        placeholder="Username"
        className="min-h-11 rounded-md border border-line bg-ink px-3 text-sm text-zinc-50 placeholder:text-zinc-500"
        required
        maxLength={100}
      />
      <label className="sr-only" htmlFor="robloxUserId">Roblox user ID</label>
      <input
        id="robloxUserId"
        value={form.robloxUserId}
        onChange={(event) => setForm((current) => ({ ...current, robloxUserId: event.target.value }))}
        placeholder="Roblox user ID"
        className="min-h-11 rounded-md border border-line bg-ink px-3 text-sm text-zinc-50 placeholder:text-zinc-500"
        inputMode="numeric"
        required
      />
      <label className="sr-only" htmlFor="discordUserId">Discord user ID</label>
      <input
        id="discordUserId"
        value={form.discordUserId}
        onChange={(event) => setForm((current) => ({ ...current, discordUserId: event.target.value }))}
        placeholder="Discord user ID"
        className="min-h-11 rounded-md border border-line bg-ink px-3 text-sm text-zinc-50 placeholder:text-zinc-500"
      />
      <label className="sr-only" htmlFor="club">Club</label>
      <select
        id="club"
        value={form.club}
        onChange={(event) => setForm((current) => ({ ...current, club: event.target.value }))}
        className="min-h-11 rounded-md border border-line bg-ink px-3 text-sm text-zinc-50"
        required
      >
        <option value="PIH">PIH (Main)</option>
        <option value="P1H">P1H (Second)</option>
      </select>
      <button
        type="submit"
        disabled={busy}
        className="inline-flex min-h-11 items-center justify-center gap-2 rounded-md bg-emerald-400 px-4 text-sm font-semibold text-zinc-950 transition hover:bg-emerald-300 disabled:cursor-wait disabled:opacity-60"
      >
        <UserPlus className="h-4 w-4" />
        Add
      </button>
    </form>
  );
}

function DetailPanel({ details, onClose, onDelete, busy }) {
    if (!details) {
    return (
      <aside className="rounded-lg border border-line bg-panel p-6 shadow-glow">
        <div className="flex items-center gap-2 text-mist">
          <Circle className="h-4 w-4" />
          Select a player
        </div>
      </aside>
    );
  }

  const meta = statusMeta(details.currentStatus);
  const chartData = details.last30Days.map((day) => ({
    date: shortDate(day.date),
    playSeconds: day.playSeconds
  }));


  return (
    <aside className="rounded-lg border border-line bg-panel p-5 shadow-glow">
      <div className="flex items-start justify-between gap-3">
        <div className="flex min-w-0 items-center gap-3">
          <Avatar player={details} size="lg" />
          <div className="min-w-0">
            <div className="flex items-center gap-2">
              <h2 className="truncate text-xl font-semibold text-zinc-50">{details.username}</h2>
              <span className={`inline-flex shrink-0 items-center rounded-md border px-1.5 py-0.5 text-[10px] font-semibold uppercase tracking-wider ${details.club === 'PIH' ? 'border-violet-400/30 bg-violet-400/10 text-violet-200' : 'border-amber-400/30 bg-amber-400/10 text-amber-200'}`}>
                {details.club}
              </span>
            </div>
            <a
              href={details.profileUrl}
              target="_blank"
              rel="noreferrer"
              className="mt-1 block truncate text-sm text-sky-300 hover:text-sky-200"
            >
              Roblox profile
            </a>
            {details.discordUserId && (
              <div className="mt-1.5 flex items-center gap-1.5 text-xs text-sky-300/70">
                <svg className="h-3.5 w-3.5" viewBox="0 0 24 24" fill="currentColor">
                  <path d="M20.317 4.37a19.791 19.791 0 0 0-4.885-1.515.074.074 0 0 0-.079.037c-.21.375-.444.864-.608 1.25a18.27 18.27 0 0 0-5.487 0 12.64 12.64 0 0 0-.617-1.25.077.077 0 0 0-.079-.037A19.736 19.736 0 0 0 3.677 4.37a.07.07 0 0 0-.032.054C1.482 8.334.694 12.24.985 16.098a.076.076 0 0 0 .031.058 19.817 19.817 0 0 0 5.993 3.03.073.073 0 0 0 .079-.028 14.935 14.935 0 0 0 1.226-1.997.074.074 0 0 0-.041-.1 13.054 13.054 0 0 1-1.875-.912.075.075 0 0 1-.008-.124c.126-.094.252-.192.372-.292a.075.075 0 0 1 .078-.015c3.89 1.774 8.095 1.774 11.941 0a.075.075 0 0 1 .079.015c.12.1.246.198.373.292a.074.074 0 0 1-.007.123 12.317 12.317 0 0 1-1.876.913.074.074 0 0 0-.04.1c.36.693.77 1.362 1.225 1.997a.072.072 0 0 0 .08.028 19.78 19.78 0 0 0 6.002-3.03.07.07 0 0 0 .031-.058c.358-4.57-.6-8.442-2.668-11.674a.06.06 0 0 0-.032-.054zM8.02 13.474c-1.175 0-2.14-1.077-2.14-2.396 0-1.32.94-2.398 2.14-2.398 1.2 0 2.165 1.077 2.14 2.398 0 1.32-.94 2.396-2.14 2.396zm7.944 0c-1.174 0-2.14-1.077-2.14-2.396 0-1.32.94-2.398 2.14-2.398 1.2 0 2.165 1.077 2.14 2.398 0 1.32-.94 2.396-2.14 2.396z"/>
                </svg>
                {details.discordUserId}
              </div>
            )}
          </div>
        </div>
        <button
          type="button"
          onClick={onClose}
          className="grid h-9 w-9 shrink-0 place-items-center rounded-md border border-line text-mist transition hover:bg-zinc-800 hover:text-zinc-100"
          aria-label="Close player details"
          title="Close"
        >
          <X className="h-4 w-4" />
        </button>
      </div>

      <div className="mt-5">
        <span className={`inline-flex items-center gap-2 rounded-md border px-2 py-1 text-xs font-medium ${meta.badge}`}>
          <span className={`h-2 w-2 rounded-full ${meta.dot}`} />
          {meta.label}
        </span>
        <div className="mt-3 flex items-center gap-2 text-sm text-zinc-300">
          <Gamepad2 className="h-4 w-4 text-sky-300" />
          <span className="truncate">{details.currentGame ?? 'No active game'}</span>
        </div>
      </div>

      <div className="mt-5 grid grid-cols-2 gap-3">
        <MiniMetric label="Today" value={formatDuration(details.todayPlaySeconds)} />
        <MiniMetric label="Week" value={formatDuration(details.weeklyPlaySeconds)} />
        <MiniMetric label="Month" value={formatDuration(details.monthlyPlaySeconds)} />
        <MiniMetric label="Total" value={formatDuration(details.totalPlaySeconds)} />
      </div>

      <div className="mt-6 rounded-lg border border-line bg-ink p-4">
        <div className="mb-3 flex items-center gap-2 text-sm font-semibold text-zinc-100">
          <BarChart3 className="h-4 w-4 text-emerald-300" />
          Last 30 days
        </div>
        <div className="h-56">
          <ResponsiveContainer width="100%" height="100%">
            <BarChart data={chartData}>
              <CartesianGrid stroke="#2a2a33" vertical={false} />
              <XAxis dataKey="date" stroke="#a1a1aa" tickLine={false} axisLine={false} tick={{ fontSize: 11 }} />
              <YAxis
                stroke="#a1a1aa"
                tickLine={false}
                axisLine={false}
                tick={{ fontSize: 11 }}
                tickFormatter={(value) => formatDuration(value)}
              />
              <Tooltip
                cursor={{ fill: 'rgba(56, 189, 248, 0.08)' }}
                contentStyle={{
                  background: '#121217',
                  border: '1px solid #2a2a33',
                  borderRadius: 8,
                  color: '#f4f4f5'
                }}
                formatter={(value) => [formatDuration(value), 'Playtime']}
              />
              <Bar dataKey="playSeconds" fill="#34d399" radius={[4, 4, 0, 0]} />
            </BarChart>
          </ResponsiveContainer>
        </div>
      </div>

      <div className="mt-6 rounded-lg border border-line bg-ink p-4">
        <div className="mb-3 flex items-center gap-2 text-sm font-semibold text-zinc-100">
          <Activity className="h-4 w-4 text-sky-300" />
          Recent activity
        </div>
        <div className="space-y-3">
          {details.recentActivity.length === 0 ? (
            <div className="text-sm text-mist">No activity yet</div>
          ) : (
            details.recentActivity.map((event) => (
              <div key={event.id} className="rounded-md border border-line bg-panel p-3">
                <div className="flex items-center justify-between gap-3">
                  <div className="font-medium text-zinc-100">{event.eventType}</div>
                  <div className="text-xs text-mist">{formatDateTime(event.occurredAt)}</div>
                </div>
                <div className="mt-1 text-sm text-zinc-300">{event.message}</div>
              </div>
            ))
          )}
        </div>
      </div>

      <button
        type="button"
        onClick={() => onDelete(details.id)}
        disabled={busy}
        className="mt-6 inline-flex min-h-10 w-full items-center justify-center gap-2 rounded-md border border-red-400/30 bg-red-400/10 px-4 text-sm font-semibold text-red-200 transition hover:bg-red-400/20 disabled:cursor-wait disabled:opacity-60"
      >
        <Trash2 className="h-4 w-4" />
        Remove Player
      </button>
    </aside>
  );
}

function MiniMetric({ label, value }) {
  return (
    <div className="rounded-lg border border-line bg-ink p-3">
      <div className="text-xs uppercase tracking-wide text-mist">{label}</div>
      <div className="mt-1 text-lg font-semibold text-zinc-50">{value}</div>
    </div>
  );
}

function Leaderboard({ players }) {
  return (
    <div className="rounded-lg border border-line bg-panel p-4 shadow-glow">
      <div className="mb-4 flex items-center gap-2 text-sm font-semibold text-zinc-100">
        <Trophy className="h-4 w-4 text-amber-300" />
        Weekly leaderboard
      </div>
      <div className="space-y-3">
        {players.slice(0, 5).map((player, index) => (
          <div key={player.playerId} className="flex items-center gap-3">
            <div className="grid h-7 w-7 place-items-center rounded-md bg-ink text-xs font-semibold text-mist">
              {index + 1}
            </div>
            <Avatar player={{ username: player.username, avatarUrl: player.avatarUrl }} />
            <div className="min-w-0 flex-1">
              <div className="truncate text-sm font-medium text-zinc-100">{player.username}</div>
              <div className="text-xs text-mist">{formatDuration(player.weeklyPlaySeconds)}</div>
            </div>
          </div>
        ))}
        {players.length === 0 && <div className="text-sm text-mist">No players yet</div>}
      </div>
    </div>
  );
}

export default function App() {
  const [players, setPlayers] = useState([]);
  const [leaderboard, setLeaderboard] = useState([]);
  const [selectedId, setSelectedId] = useState(null);
  const [details, setDetails] = useState(null);
  const [search, setSearch] = useState('');
  const [clubFilter, setClubFilter] = useState('all');
  const [view, setView] = useState('cards');
  const [sortBy, setSortBy] = useState('daily');
  const [sortDirection, setSortDirection] = useState('desc');
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [checking, setChecking] = useState(false);
  const [error, setError] = useState('');
  const [lastUpdated, setLastUpdated] = useState(null);
  const [lastRefreshAt, setLastRefreshAt] = useState(null);
  const [tick, setTick] = useState(0);

  const loadDashboard = useCallback(async (silent = false) => {
    if (!silent) {
      setLoading(true);
    }

    try {
      const [dashboard, weekly] = await Promise.all([
        api.dashboard(),
        api.weeklyLeaderboard()
      ]);
      setPlayers(dashboard);
      setLeaderboard(weekly);
      setLastUpdated(new Date());
      setLastRefreshAt(Date.now());
      setError('');
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  }, []);

  const loadDetails = useCallback(async (id) => {
    if (!id) {
      setDetails(null);
      return;
    }

    try {
      const player = await api.player(id);
      setDetails(player);
      setError('');
    } catch (err) {
      setError(err.message);
    }
  }, []);

  useEffect(() => {
    loadDashboard();
    const timer = window.setInterval(() => {
      loadDashboard(true);
      if (selectedId) {
        loadDetails(selectedId);
      }
    }, 15000);

    return () => window.clearInterval(timer);
  }, [loadDashboard, loadDetails, selectedId]);

  // Live tick every second for real-time playtime animation
  useEffect(() => {
    const timer = setInterval(() => {
      setTick((t) => t + 1);
    }, 1000);
    return () => clearInterval(timer);
  }, []);

  useEffect(() => {
    loadDetails(selectedId);
  }, [loadDetails, selectedId]);

  // Live players with real-time playtime for those currently playing
  const livePlayers = useMemo(() => {
    if (!lastRefreshAt || players.length === 0) return players;
    const elapsed = Math.floor((Date.now() - lastRefreshAt) / 1000);

    if (elapsed <= 0) return players;

    return players.map((player) => {
      if (!player.currentStatus?.startsWith('Playing')) return player;
      return {
        ...player,
        todayPlaySeconds: player.todayPlaySeconds + elapsed,
        totalPlaySeconds: player.totalPlaySeconds + elapsed
      };
    });
  }, [players, lastRefreshAt, tick]);

  const filteredPlayers = useMemo(() => {
    const query = search.trim().toLowerCase();
    const visible = livePlayers.filter((player) => {
      if (clubFilter !== 'all' && player.club !== clubFilter) {
        return false;
      }

      if (!query) {
        return true;
      }

      return [player.username, player.currentStatus, player.currentGame, String(player.robloxUserId), player.club]
        .filter(Boolean)
        .some((value) => value.toLowerCase().includes(query));
    });

    const direction = sortDirection === 'asc' ? 1 : -1;
    return [...visible].sort((a, b) => {
      if (sortBy === 'username') {
        return a.username.localeCompare(b.username) * direction;
      }

      const aValue = sortBy === 'total' ? a.totalPlaySeconds : a.todayPlaySeconds;
      const bValue = sortBy === 'total' ? b.totalPlaySeconds : b.todayPlaySeconds;
      return (aValue - bValue) * direction;
    });
  }, [livePlayers, search, clubFilter, sortBy, sortDirection]);

  const totals = useMemo(() => {
    const byClub = (club) => {
      const filtered = livePlayers.filter((p) => p.club === club);
      return {
        count: filtered.length,
        playing: filtered.filter((p) => p.currentStatus?.startsWith('Playing')).length,
        today: filtered.reduce((sum, p) => sum + p.todayPlaySeconds, 0),
        total: filtered.reduce((sum, p) => sum + p.totalPlaySeconds, 0)
      };
    };

    return {
      playerCount: livePlayers.length,
      playingCount: livePlayers.filter((player) => player.currentStatus?.startsWith('Playing')).length,
      todaySeconds: livePlayers.reduce((sum, player) => sum + player.todayPlaySeconds, 0),
      totalSeconds: livePlayers.reduce((sum, player) => sum + player.totalPlaySeconds, 0),
      club: {
        pih: byClub('PIH'),
        p1h: byClub('P1H')
      }
    };
  }, [livePlayers]);

  // Live details with real-time playtime for the selected player
  const liveDetails = useMemo(() => {
    if (!details || !lastRefreshAt) return details;
    if (!details.currentStatus?.startsWith('Playing')) return details;
    const elapsed = Math.floor((Date.now() - lastRefreshAt) / 1000);
    if (elapsed <= 0) return details;
    return {
      ...details,
      todayPlaySeconds: details.todayPlaySeconds + elapsed,
      totalPlaySeconds: details.totalPlaySeconds + elapsed,
      weeklyPlaySeconds: details.weeklyPlaySeconds + elapsed,
      monthlyPlaySeconds: details.monthlyPlaySeconds + elapsed
    };
  }, [details, lastRefreshAt, tick]);

  function updateSort(field) {
    if (sortBy === field) {
      setSortDirection((current) => (current === 'asc' ? 'desc' : 'asc'));
      return;
    }

    setSortBy(field);
    setSortDirection(field === 'username' ? 'asc' : 'desc');
  }

  async function addPlayer(body) {
    setBusy(true);
    try {
      const created = await api.addPlayer(body);
      await loadDashboard(true);
      setSelectedId(created.id);
      setError('');
    } catch (err) {
      setError(err.message);
    } finally {
      setBusy(false);
    }
  }

  async function runCheckNow() {
    setChecking(true);
    try {
      await api.checkNow();
      await loadDashboard(true);
      if (selectedId) {
        await loadDetails(selectedId);
      }
      setError('');
    } catch (err) {
      setError(err.message);
    } finally {
      setChecking(false);
    }
  }

  async function deletePlayer(id) {
    const confirmed = window.confirm('Remove this player and their playtime history?');
    if (!confirmed) {
      return;
    }

    setBusy(true);
    try {
      await api.deletePlayer(id);
      setSelectedId(null);
      setDetails(null);
      await loadDashboard(true);
      setError('');
    } catch (err) {
      setError(err.message);
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="min-h-screen bg-ink text-zinc-50">
      <header className="sticky top-0 z-20 border-b border-line bg-ink/90 backdrop-blur">
        <div className="mx-auto flex max-w-7xl flex-col gap-4 px-4 py-4 sm:px-6 lg:flex-row lg:items-center lg:justify-between lg:px-8">
          <div className="flex items-center gap-3">
            <div className="grid h-10 w-10 place-items-center rounded-md bg-emerald-400 text-zinc-950">
              <Gamepad2 className="h-5 w-5" />
            </div>
            <div>
              <h1 className="text-lg font-semibold tracking-normal text-zinc-50">Club Playtime</h1>
              <div className="text-sm text-mist">Racket Rivals tracker</div>
            </div>
          </div>

          <div className="flex flex-wrap items-center gap-2">
            <button
              type="button"
              onClick={() => api.downloadCsv()}
              className="inline-flex min-h-10 items-center justify-center gap-2 rounded-md border border-line px-3 text-sm font-medium text-zinc-200 transition hover:bg-zinc-800"
            >
              <Download className="h-4 w-4" />
              CSV
            </button>
            <button
              type="button"
              onClick={runCheckNow}
              disabled={checking}
              className="inline-flex min-h-10 items-center justify-center gap-2 rounded-md bg-sky-300 px-3 text-sm font-semibold text-zinc-950 transition hover:bg-sky-200 disabled:cursor-wait disabled:opacity-60"
            >
              <RefreshCw className={`h-4 w-4 ${checking ? 'animate-spin' : ''}`} />
              Check Now
            </button>
          </div>
        </div>
      </header>

      <main className="mx-auto grid max-w-7xl gap-6 px-4 py-6 sm:px-6 lg:px-8 xl:grid-cols-[minmax(0,1fr)_390px]">
        <div className="min-w-0 space-y-6">
          {error && (
            <div className="rounded-lg border border-red-400/30 bg-red-400/10 px-4 py-3 text-sm text-red-100">
              {error}
            </div>
          )}

          <section className="grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
            <Metric icon={Activity} label="Players" value={totals.playerCount} />
            <Metric icon={Play} label="Playing" value={totals.playingCount} />
            <Metric icon={Clock} label="Today" value={formatDuration(totals.todaySeconds)} />
            <Metric icon={BarChart3} label="All time" value={formatDuration(totals.totalSeconds)} />
          </section>

          <section className="grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
            <SplitMetric icon={Activity} label="Players" pihValue={totals.club.pih.count} p1hValue={totals.club.p1h.count} />
            <SplitMetric icon={Play} label="Playing" pihValue={totals.club.pih.playing} p1hValue={totals.club.p1h.playing} />
            <SplitMetric icon={Clock} label="Today" pihValue={formatDuration(totals.club.pih.today)} p1hValue={formatDuration(totals.club.p1h.today)} />
            <SplitMetric icon={BarChart3} label="All time" pihValue={formatDuration(totals.club.pih.total)} p1hValue={formatDuration(totals.club.p1h.total)} />
          </section>

          <AddPlayerForm onAdd={addPlayer} busy={busy} />

          <section className="rounded-lg border border-line bg-panel p-4 shadow-glow">
            <div className="flex flex-col gap-3 lg:flex-row lg:items-center lg:justify-between">
              <div className="relative min-w-0 flex-1">
                <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-mist" />
                <input
                  value={search}
                  onChange={(event) => setSearch(event.target.value)}
                  placeholder="Search"
                  className="min-h-11 w-full rounded-md border border-line bg-ink py-2 pl-10 pr-3 text-sm text-zinc-50 placeholder:text-zinc-500"
                />
              </div>

              <div className="flex flex-wrap items-center gap-2">
                <select
                  value={sortBy}
                  onChange={(event) => {
                    setSortBy(event.target.value);
                    setSortDirection(event.target.value === 'username' ? 'asc' : 'desc');
                  }}
                  className="min-h-11 rounded-md border border-line bg-ink px-3 text-sm text-zinc-50"
                >
                  {Object.entries(sortLabels).map(([value, label]) => (
                    <option key={value} value={value}>{label}</option>
                  ))}
                </select>

                <div className="inline-flex rounded-md border border-line bg-ink p-1">
                  <button
                    type="button"
                    onClick={() => setView('cards')}
                    className={`grid h-9 w-9 place-items-center rounded ${view === 'cards' ? 'bg-zinc-700 text-zinc-50' : 'text-mist hover:text-zinc-100'}`}
                    aria-label="Card view"
                    title="Cards"
                  >
                    <LayoutGrid className="h-4 w-4" />
                  </button>
                  <button
                    type="button"
                    onClick={() => setView('table')}
                    className={`grid h-9 w-9 place-items-center rounded ${view === 'table' ? 'bg-zinc-700 text-zinc-50' : 'text-mist hover:text-zinc-100'}`}
                    aria-label="Table view"
                    title="Table"
                  >
                    <Table2 className="h-4 w-4" />
                  </button>
                </div>
              </div>
            </div>

            <div className="mt-3 flex flex-wrap items-center gap-2">
              <button
                type="button"
                onClick={() => setClubFilter('all')}
                className={`inline-flex min-h-8 items-center justify-center rounded-md border px-3 text-xs font-semibold transition ${
                  clubFilter === 'all'
                    ? 'border-zinc-500 bg-zinc-700 text-zinc-50'
                    : 'border-line text-mist hover:text-zinc-200'
                }`}
              >
                All
              </button>
              <button
                type="button"
                onClick={() => setClubFilter('PIH')}
                className={`inline-flex min-h-8 items-center justify-center rounded-md border px-3 text-xs font-semibold tracking-wide transition ${
                  clubFilter === 'PIH'
                    ? 'border-violet-400/50 bg-violet-400/15 text-violet-200'
                    : 'border-line text-mist hover:text-zinc-200'
                }`}
              >
                PIH
              </button>
              <button
                type="button"
                onClick={() => setClubFilter('P1H')}
                className={`inline-flex min-h-8 items-center justify-center rounded-md border px-3 text-xs font-semibold tracking-wide transition ${
                  clubFilter === 'P1H'
                    ? 'border-amber-400/50 bg-amber-400/15 text-amber-200'
                    : 'border-line text-mist hover:text-zinc-200'
                }`}
              >
                P1H
              </button>
            </div>

            <div className="mt-3 flex flex-wrap items-center gap-3 text-xs text-mist">
              {lastUpdated && <span>Updated {formatDateTime(lastUpdated.toISOString())}</span>}
              {loading && <span>Loading...</span>}
              <span>{filteredPlayers.length} shown</span>
            </div>
          </section>

          {view === 'cards' ? (
            <section className="grid gap-4 md:grid-cols-2 2xl:grid-cols-3">
              {filteredPlayers.map((player) => (
                <PlayerCard
                  key={player.id}
                  player={player}
                  onSelect={setSelectedId}
                  selected={selectedId === player.id}
                />
              ))}
              {!loading && filteredPlayers.length === 0 && (
                <div className="rounded-lg border border-line bg-panel p-6 text-sm text-mist shadow-glow">
                  No players found
                </div>
              )}
            </section>
          ) : (
            <PlayersTable
              players={filteredPlayers}
              selectedId={selectedId}
              onSelect={setSelectedId}
              sortBy={sortBy}
              sortDirection={sortDirection}
              onSort={updateSort}
            />
          )}
        </div>

        <div className="space-y-6 xl:sticky xl:top-24 xl:self-start">
          <Leaderboard players={leaderboard} />
          <DetailPanel
            details={liveDetails}
            onClose={() => setSelectedId(null)}
            onDelete={deletePlayer}
            busy={busy}
          />
        </div>
      </main>
    </div>
  );
}
