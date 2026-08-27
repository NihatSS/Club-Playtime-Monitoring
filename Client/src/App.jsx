import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import {
  Activity,
  ArrowDown,
  ArrowUp,
  BarChart3,
  CalendarClock,
  CheckCircle,
  Circle,
  Clock,
  Copy,
  Download,
  ExternalLink,
  Gamepad2,
  Globe,
  LayoutGrid,
  LogIn,
  LogOut,
  Play,
  RefreshCw,
  Search,
  Shield,
  Table2,
  Trash2,
  Trophy,
  UserPlus,
  Users,
  X,
  XCircle
} from 'lucide-react';
import {
  Area,
  AreaChart,
  CartesianGrid,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis
} from 'recharts';
import { api, setOnAuthExpired } from './lib/api';
import { formatDateTime, formatDuration, shortDate } from './lib/format';
import LoginPage from './components/LoginPage';
import RequestJoinForm from './components/RequestJoinForm';

// ─── Helpers ─────────────────────────────────────────────────
function statusMeta(status) {
  if (status?.startsWith('Playing')) {
    return {
      label: status,
      dot: 'bg-neon-green',
      badge: 'border-neon-green/30 bg-neon-green/10 text-neon-green'
    };
  }
  if (status === 'Online') {
    return {
      label: 'Online',
      dot: 'bg-neon-cyan',
      badge: 'border-neon-cyan/30 bg-neon-cyan/[0.06] text-neon-cyan'
    };
  }
  return {
    label: 'Offline',
    dot: 'bg-zinc-500',
    badge: 'border-zinc-500/30 bg-zinc-500/10 text-zinc-300'
  };
}

function clubBadgeClass(club) {
  if (club === 'PIH') return 'border-neon-purple/40 bg-neon-purple/10 text-neon-purple';
  if (club === 'P1H') return 'border-neon-amber/40 bg-neon-amber/10 text-neon-amber';
  return 'border-zinc-500/30 bg-zinc-500/10 text-zinc-300';
}

function clubLabel(club) {
  if (club === 'PIH') return 'PIH';
  if (club === 'P1H') return 'P1H';
  return 'None';
}

function CopyIdButton({ id }) {
  const [copied, setCopied] = useState(false);
  return (
    <button
      type="button"
      onClick={(e) => {
        e.stopPropagation();
        navigator.clipboard.writeText(String(id));
        setCopied(true);
        setTimeout(() => setCopied(false), 1200);
      }}
      className="inline-flex items-center text-zinc-500 hover:text-zinc-300 transition"
      title="Copy ID"
    >
      <Copy className="h-3 w-3" />
      {copied && <span className="ml-1 text-[10px] text-neon-green">Copied</span>}
    </button>
  );
}

function CopyTextButton({ text, label }) {
  const [copied, setCopied] = useState(false);
  return (
    <button
      type="button"
      onClick={(e) => {
        e.stopPropagation();
        navigator.clipboard.writeText(String(text));
        setCopied(true);
        setTimeout(() => setCopied(false), 1200);
      }}
      className="inline-flex items-center gap-1 rounded-md border border-neon-cyan/[0.08] px-1.5 py-0.5 text-[10px] font-medium text-zinc-400 transition hover:bg-neon-cyan/[0.06] hover:text-zinc-200"
      title={`Copy ${label}`}
    >
      <Copy className="h-2.5 w-2.5" />
      {copied ? 'Copied!' : label}
    </button>
  );
}

function Avatar({ player, size = 'md' }) {
  const sizeClass = size === 'lg' ? 'h-14 w-14 text-lg' : size === 'sm' ? 'h-8 w-8 text-xs' : 'h-10 w-10 text-sm';
  const initial = player?.username?.slice(0, 1)?.toUpperCase() ?? '?';
  if (player?.avatarUrl) {
    return (
      <img
        src={player.avatarUrl}
        alt={`${player.username} avatar`}
        className={`${sizeClass} rounded-lg border border-neon-cyan/[0.12] bg-panelSoft object-cover shrink-0`}
      />
    );
  }
  return (
    <div className={`${sizeClass} grid place-items-center rounded-lg border border-neon-cyan/[0.12] bg-panelSoft font-semibold text-neon-cyan/80 shrink-0`}>
      {initial}
    </div>
  );
}

// ─── Join Requests Dropdown ──────────────────────────────────
function JoinRequestsDropdown() {
  const [open, setOpen] = useState(false);
  const [requests, setRequests] = useState([]);
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');
  const ref = useRef(null);

  const pendingCount = requests.filter((r) => r.status === 'Pending').length;

  const loadRequests = useCallback(async (silent = false) => {
    if (!silent) setLoading(true);
    try {
      const data = await api.getJoinRequests('Pending');
      setRequests(data);
      setError('');
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    loadRequests();
    const timer = window.setInterval(() => loadRequests(true), 15000);
    return () => window.clearInterval(timer);
  }, [loadRequests]);

  useEffect(() => {
    if (!open) return;
    function handleClick(e) {
      if (ref.current && !ref.current.contains(e.target)) setOpen(false);
    }
    document.addEventListener('mousedown', handleClick);
    return () => document.removeEventListener('mousedown', handleClick);
  }, [open]);

  async function handleDelete(id) {
    setBusy(true);
    try {
      await api.deleteJoinRequest(id);
      await loadRequests(true);
    } catch (err) {
      setError(err.message);
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="relative" ref={ref}>
      <button
        type="button"
        onClick={() => setOpen((o) => !o)}
        className="inline-flex h-9 items-center justify-center gap-2 rounded-lg border border-neon-cyan/[0.08] px-3 text-sm font-medium text-zinc-200 transition hover:bg-neon-cyan/[0.06]"
      >
        <Users className="h-4 w-4 text-neon-purple" />
        Join Requests
        {pendingCount > 0 && (
          <span className="inline-flex h-5 min-w-5 items-center justify-center rounded-full bg-neon-amber px-1 text-[10px] font-bold text-zinc-950">
            {pendingCount}
          </span>
        )}
      </button>

      {open && (
        <div className="absolute right-0 top-full z-50 mt-2 w-[380px] max-h-[480px] overflow-hidden rounded-xl border border-neon-cyan/[0.08] bg-[#151519] shadow-2xl flex flex-col">
          <div className="flex items-center justify-between border-b border-neon-cyan/[0.08] px-4 py-3">
            <span className="text-sm font-semibold text-zinc-100">Pending Requests</span>
            <button
              type="button"
              onClick={() => loadRequests(true)}
              className="grid h-7 w-7 place-items-center rounded-md border border-neon-cyan/[0.08] text-mist transition hover:bg-neon-cyan/[0.06] hover:text-zinc-100"
            >
              <RefreshCw className={`h-3.5 w-3.5 ${loading ? 'animate-spin' : ''}`} />
            </button>
          </div>

          {error && (
            <div className="border-b border-neon-cyan/[0.08] bg-red-400/10 px-4 py-2 text-xs text-red-100">
              {error}
            </div>
          )}

          <div className="overflow-y-auto thin-scrollbar flex-1">
            {loading && requests.length === 0 ? (
              <div className="px-4 py-8 text-center text-sm text-mist">Loading...</div>
            ) : requests.length === 0 ? (
              <div className="px-4 py-8 text-center">
                <Users className="mx-auto h-8 w-8 text-zinc-600" />
                <div className="mt-2 text-sm text-mist">No pending join requests</div>
              </div>
            ) : (
              <div className="divide-y divide-neon-cyan/10">
                {requests.map((req) => (
                  <div key={req.id} className="px-4 py-3 transition hover:bg-neon-cyan/[0.03]">
                    <div className="flex items-start gap-3">
                      <Avatar player={{ username: req.robloxUsername }} size="sm" />
                      <div className="min-w-0 flex-1">
                        <div className="flex items-center gap-2">
                          <span className="truncate text-sm font-semibold text-zinc-100">{req.robloxUsername}</span>
                          <span className={`inline-flex shrink-0 items-center rounded-md border px-1.5 py-0.5 text-[10px] font-semibold uppercase tracking-wider ${clubBadgeClass(req.club)}`}>
                            {clubLabel(req.club)}
                          </span>
                        </div>
                        <div className="mt-0.5 flex items-center gap-1 text-xs text-mist">
                          ID: {req.robloxUserId}
                          <CopyIdButton id={req.robloxUserId} />
                          {req.discordUserId && <span className="ml-2">Discord: {req.discordUserId}</span>}
                        </div>
                        <div className="mt-1.5 flex items-center gap-1.5">
                          <CopyTextButton text={req.robloxUsername} label="Name" />
                          <CopyTextButton text={String(req.robloxUserId)} label="User ID" />
                          {req.discordUserId && <CopyTextButton text={req.discordUserId} label="Discord" />}
                        </div>
                        <div className="mt-0.5 text-[11px] text-zinc-500">
                          {formatDateTime(req.createdAt)}
                        </div>
                        {req.note && (
                          <div className="mt-1.5 rounded-md border border-neon-cyan/[0.08] bg-ink px-2.5 py-1.5 text-xs text-zinc-300">
                            {req.note}
                          </div>
                        )}
                      </div>
                    </div>
                    <div className="mt-2.5 flex items-center justify-end gap-2">
                      <a
                        href={`https://www.roblox.com/users/${req.robloxUserId}/profile`}
                        target="_blank"
                        rel="noreferrer"
                        className="inline-flex items-center gap-1 rounded-md border border-neon-cyan/[0.08] px-2 py-1 text-[11px] font-medium text-zinc-400 transition hover:bg-neon-cyan/[0.06] hover:text-zinc-200"
                      >
                        <ExternalLink className="h-3 w-3" />
                        Profile
                      </a>
                      <button
                        type="button"
                        onClick={() => handleDelete(req.id)}
                        disabled={busy}
                        className="inline-flex items-center gap-1 rounded-md border border-neon-cyan/[0.08] px-2 py-1 text-[11px] font-medium text-zinc-400 transition hover:bg-neon-cyan/[0.06] hover:text-red-300 disabled:opacity-50"
                      >
                        <Trash2 className="h-3 w-3" />
                      </button>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>
        </div>
      )}
    </div>
  );
}

// ─── Card View Components ────────────────────────────────────
function PlayerCard({ player, onSelect, selected, rank }) {
  const meta = statusMeta(player.currentStatus);
  const isPlaying = player.currentStatus?.startsWith('Playing');
  const style = rank <= 3 ? rankStyles[rank - 1] : null;

  return (
    <button
      type="button"
      onClick={() => onSelect(player.id)}
      className={`group rounded-xl border p-4 text-left transition ${
        style
          ? `${style.bg} ${style.border}`
          : selected
            ? 'border-neon-cyan/40 shadow-neon-cyan bg-[#08081a]'
            : 'border-line bg-[#08081a] hover:border-zinc-500/50'
      }`}
    >
      {/* Top: Rank Badge + Avatar + Name + Status */}
      <div className="flex items-start gap-3">
        <RankBadge rank={rank} />
        <Avatar player={player} />
        <div className="min-w-0 flex-1">
          <div className="flex items-center gap-2 flex-wrap">
            <span className={`truncate text-base font-semibold ${style ? style.text : 'text-zinc-50'}`}>{player.username}</span>
            <span className={`inline-flex shrink-0 items-center rounded-md border px-1.5 py-0.5 text-[10px] font-semibold uppercase tracking-wider ${clubBadgeClass(player.club)}`}>
              {clubLabel(player.club)}
            </span>
            <span className={`inline-flex shrink-0 items-center gap-1.5 rounded-md border px-1.5 py-0.5 text-[10px] font-medium ${meta.badge}`}>
              <span className={`h-1.5 w-1.5 rounded-full ${meta.dot}`} />
              {meta.label}
            </span>
          </div>
          <div className="mt-1 flex items-center gap-1 text-sm text-mist">
            <span className="font-mono text-xs">{player.robloxUserId}</span>
            <CopyIdButton id={player.robloxUserId} />
          </div>
        </div>
      </div>

      {/* Stats row */}
      <div className="mt-4 grid grid-cols-3 gap-2">
        <div>
          <div className="text-[10px] font-semibold uppercase tracking-wider text-zinc-500">Today</div>
          <div className="mt-0.5 text-base font-semibold text-zinc-50">{formatDuration(player.todayPlaySeconds)}</div>
        </div>
        <div>
          <div className="text-[10px] font-semibold uppercase tracking-wider text-zinc-500">Total</div>
          <div className={`mt-0.5 text-base font-semibold ${style ? style.text : 'text-zinc-50'}`}>{formatDuration(player.totalPlaySeconds)}</div>
        </div>
        <div>
          <div className="text-[10px] font-semibold uppercase tracking-wider text-zinc-500">Current Game</div>
          <div className="mt-0.5 flex items-center gap-1 text-xs text-mist">
            <Gamepad2 className="h-3 w-3 shrink-0 text-neon-cyan" />
            <span className="truncate">{player.currentGame ?? 'No active game'}</span>
          </div>
        </div>
      </div>

      {/* Bottom: Last seen */}
      <div className="mt-3 flex items-center gap-1.5 text-xs text-mist">
        <Clock className="h-3 w-3" />
        <span>Last seen</span>
        <span className="text-zinc-500">·</span>
        {isPlaying || player.currentStatus === 'Online' ? (
          <span className="text-neon-green font-medium">Online now</span>
        ) : (
          <span>{player.lastSeenPlaying ? formatDateTime(player.lastSeenPlaying) : 'Never'}</span>
        )}
      </div>
    </button>
  );
}

// ─── Table View ──────────────────────────────────────────────
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
      <Icon className={`h-3.5 w-3.5 ${active ? 'opacity-100' : 'opacity-30'}`} />
    </button>
  );
}

function PlayersTable({ players, selectedId, onSelect, sortBy, sortDirection, onSort, startIndex = 0 }) {
  return (
    <div className="overflow-hidden rounded-xl border border-neon-cyan/[0.08] bg-[#08081a]">
      <div className="overflow-x-auto thin-scrollbar">
        <table className="min-w-[900px] w-full text-left text-sm">
          <thead className="border-b border-neon-cyan/[0.08] bg-[#0a0a20] text-[11px] uppercase tracking-wider text-zinc-500">
            <tr>
              <th className="w-12 px-4 py-3">#</th>
              <th className="px-4 py-3">
                <SortButton field="username" sortBy={sortBy} sortDirection={sortDirection} onSort={onSort}>Player</SortButton>
              </th>
              <th className="px-4 py-3">Club</th>
              <th className="px-4 py-3">Status</th>
              <th className="px-4 py-3">
                <SortButton field="daily" sortBy={sortBy} sortDirection={sortDirection} onSort={onSort}>Today</SortButton>
              </th>
              <th className="px-4 py-3">
                <SortButton field="total" sortBy={sortBy} sortDirection={sortDirection} onSort={onSort}>Total</SortButton>
              </th>
              <th className="px-4 py-3">Current Game</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-neon-cyan/10">
            {players.map((player, index) => {
              const globalRank = startIndex + index + 1;
              const style = globalRank <= 3 ? rankStyles[globalRank - 1] : null;
              const meta = statusMeta(player.currentStatus);
              const isPlaying = player.currentStatus?.startsWith('Playing');
              return (
                <tr
                  key={player.id}
                  onClick={() => onSelect(player.id)}
                  className={`cursor-pointer transition ${
                    style
                      ? `${style.bg}`
                      : `hover:bg-neon-cyan/[0.03] ${selectedId === player.id ? 'bg-neon-cyan/[0.06]' : ''}`
                  }`}
                >
                  <td className="px-4 py-3">
                    <RankBadge rank={globalRank} />
                  </td>
                  <td className="px-4 py-3">
                    <div className="flex items-center gap-3">
                      <Avatar player={player} size="sm" />
                      <div>
                        <div className={`font-medium ${style ? style.text : 'text-zinc-50'}`}>{player.username}</div>
                        <div className="flex items-center gap-1 text-xs text-mist">
                          <span>ID: {player.robloxUserId}</span>
                          <CopyIdButton id={player.robloxUserId} />
                        </div>
                      </div>
                    </div>
                  </td>
                  <td className="px-4 py-3">
                    <span className={`inline-flex items-center rounded-md border px-1.5 py-0.5 text-[10px] font-semibold uppercase tracking-wider ${clubBadgeClass(player.club)}`}>
                      {clubLabel(player.club)}
                    </span>
                  </td>
                  <td className="px-4 py-3">
                    <span className={`inline-flex items-center gap-1.5 rounded-md border px-2 py-1 text-xs font-medium ${meta.badge}`}>
                      <span className={`h-1.5 w-1.5 rounded-full ${meta.dot}`} />
                      {meta.label}
                    </span>
                  </td>
                  <td className="px-4 py-3 font-medium text-zinc-50">{formatDuration(player.todayPlaySeconds)}</td>
                  <td className={`px-4 py-3 ${style ? style.text : 'text-zinc-300'}`}>{formatDuration(player.totalPlaySeconds)}</td>
                  <td className="max-w-[200px] truncate px-4 py-3 text-mist">
                    <div className="flex items-center gap-1.5">
                      <Gamepad2 className="h-3.5 w-3.5 shrink-0 text-neon-cyan" />
                      <span className="truncate">{player.currentGame ?? 'No active game'}</span>
                    </div>
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>
    </div>
  );
}

// ─── Add Player Form ─────────────────────────────────────────
function AddPlayerForm({ onAdd, busy }) {
  const [form, setForm] = useState({ username: '', robloxUserId: '', club: 'PIH', customClub: '', discordUserId: '' });

  async function submit(event) {
    event.preventDefault();
    const clubValue = form.club === 'custom' ? form.customClub.trim() : form.club === 'none' ? '' : form.club;
    await onAdd({
      username: form.username.trim(),
      robloxUserId: Number(form.robloxUserId),
      club: clubValue,
      discordUserId: form.discordUserId.trim()
    });
    setForm({ username: '', robloxUserId: '', club: 'PIH', customClub: '', discordUserId: '' });
  }

  const showCustomInput = form.club === 'custom';

  return (
    <form onSubmit={submit} className="rounded-xl border border-neon-cyan/[0.08] bg-[#08081a] p-4 space-y-3">
      <div className="grid gap-3 md:grid-cols-[1fr_160px_160px_140px_auto]">
        <input value={form.username} onChange={(e) => setForm((c) => ({ ...c, username: e.target.value }))} placeholder="Username" className="min-h-10 rounded-lg border border-neon-cyan/[0.08] bg-ink px-3 text-sm text-zinc-50 placeholder:text-zinc-500" required maxLength={100} />
        <input value={form.robloxUserId} onChange={(e) => setForm((c) => ({ ...c, robloxUserId: e.target.value }))} placeholder="Roblox user ID" className="min-h-10 rounded-lg border border-neon-cyan/[0.08] bg-ink px-3 text-sm text-zinc-50 placeholder:text-zinc-500" inputMode="numeric" required />
        <input value={form.discordUserId} onChange={(e) => setForm((c) => ({ ...c, discordUserId: e.target.value }))} placeholder="Discord user ID" className="min-h-10 rounded-lg border border-neon-cyan/[0.08] bg-ink px-3 text-sm text-zinc-50 placeholder:text-zinc-500" />
        <select value={form.club} onChange={(e) => setForm((c) => ({ ...c, club: e.target.value }))} className="min-h-10 rounded-lg border border-neon-cyan/[0.08] bg-ink px-3 text-sm text-zinc-50" required>
          <option value="PIH">PIH</option>
          <option value="P1H">P1H</option>
          <option value="custom">Custom</option>
          <option value="none">None</option>
        </select>
        <button type="submit" disabled={busy} className="inline-flex min-h-10 items-center justify-center gap-2 rounded-lg bg-neon-green px-4 text-sm font-semibold text-zinc-950 transition hover:bg-neon-green/80 disabled:opacity-60">
          <UserPlus className="h-4 w-4" />
          Add
        </button>
      </div>
      {showCustomInput && (
        <input
          value={form.customClub}
          onChange={(e) => setForm((c) => ({ ...c, customClub: e.target.value }))}
          placeholder="Enter custom club name"
          className="min-h-10 w-full rounded-lg border border-neon-cyan/[0.08] bg-ink px-3 text-sm text-zinc-50 placeholder:text-zinc-500"
          required
          maxLength={10}
        />
      )}
    </form>
  );
}

// ─── Sidebar: Detail Panel ───────────────────────────────────
function DetailPanel({ details, onClose, onDelete, busy, isAdmin, onClubChange }) {
  const [editingClub, setEditingClub] = useState(false);
  const [newClub, setNewClub] = useState('');

  function handleClubSave() {
    onClubChange?.(details.id, newClub);
    setEditingClub(false);
  }

  if (!details) {
    return (
      <div className="rounded-xl border border-neon-cyan/[0.08] bg-[#08081a] p-6">
        <div className="flex items-center gap-2 text-sm text-mist">
          <Circle className="h-4 w-4" />
          Select a player
        </div>
      </div>
    );
  }

  const meta = statusMeta(details.currentStatus);
  const chartData = (details.last30Days || []).map((day) => ({
    date: shortDate(day.date),
    playSeconds: day.playSeconds
  }));

  return (
    <div className="rounded-xl border border-neon-cyan/[0.08] bg-[#08081a] p-5 space-y-4">
      {/* Header */}
      <div className="flex items-start justify-between gap-3">
        <div className="flex min-w-0 items-center gap-3">
          <Avatar player={details} size="lg" />
          <div className="min-w-0">
            <div className="flex items-center gap-2">
              <h2 className="truncate text-lg font-semibold text-zinc-50">{details.username}</h2>
              <span className={`inline-flex shrink-0 items-center rounded-md border px-1.5 py-0.5 text-[10px] font-semibold uppercase tracking-wider ${clubBadgeClass(details.club)}`}>
                {clubLabel(details.club)}
              </span>
              {isAdmin && !editingClub && (
                <button type="button" onClick={() => { setEditingClub(true); setNewClub(details.club || ''); }} className="inline-flex h-5 w-5 items-center justify-center rounded border border-neon-cyan/[0.12] text-zinc-500 transition hover:bg-neon-cyan/[0.06] hover:text-zinc-200" title="Edit club">
                  <svg className="h-3 w-3" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}><path strokeLinecap="round" strokeLinejoin="round" d="M16.862 4.487l1.687-1.688a1.875 1.875 0 112.652 2.652L10.582 16.07a4.5 4.5 0 01-1.897 1.13L6 18l.8-2.685a4.5 4.5 0 011.13-1.897l8.932-8.931z" /></svg>
                </button>
              )}
            </div>
            <div className="mt-0.5 flex items-center gap-1 text-sm text-mist">
              <span className="font-mono text-xs">ID: {details.robloxUserId}</span>
              <CopyIdButton id={details.robloxUserId} />
            </div>
            <a
              href={details.profileUrl}
              target="_blank"
              rel="noreferrer"
              className="mt-1 inline-flex items-center gap-1 text-sm text-neon-cyan hover:text-neon-cyan/80 transition"
            >
              Roblox profile
              <ExternalLink className="h-3 w-3" />
            </a>
          </div>
        </div>
        <button
          type="button"
          onClick={onClose}
          className="grid h-8 w-8 shrink-0 place-items-center rounded-lg border border-neon-cyan/[0.08] text-mist transition hover:bg-neon-cyan/[0.06] hover:text-zinc-100"
        >
          <X className="h-4 w-4" />
        </button>
      </div>

      {/* Club badge + edit button (admin only) */}
      {isAdmin && editingClub && (
        <div className="flex items-center gap-2 rounded-lg border border-neon-cyan/[0.12] bg-ink p-2">
          <select
            value={newClub}
            onChange={(e) => setNewClub(e.target.value)}
            className="h-8 flex-1 rounded-md border border-neon-cyan/[0.15] bg-panel px-2 text-xs text-zinc-200 focus:border-neon-cyan/30 transition"
          >
            <option value="PIH">PIH</option>
            <option value="P1H">P1H</option>
            <option value="">None</option>
          </select>
          <button type="button" onClick={handleClubSave} disabled={busy} className="h-8 rounded-md bg-neon-cyan px-3 text-[11px] font-semibold text-zinc-950 transition hover:bg-neon-cyan/80 disabled:opacity-50">Save</button>
          <button type="button" onClick={() => setEditingClub(false)} className="h-8 rounded-md border border-neon-cyan/[0.12] px-3 text-[11px] font-medium text-zinc-400 transition hover:bg-neon-cyan/[0.06] hover:text-zinc-200">Cancel</button>
        </div>
      )}

      {/* Status + Game */}
      <div className="flex items-center gap-2">
        <span className={`inline-flex items-center gap-1.5 rounded-md border px-2 py-1 text-xs font-medium ${meta.badge}`}>
          <span className={`h-1.5 w-1.5 rounded-full ${meta.dot}`} />
          {meta.label}
        </span>
      </div>
      <div className="flex items-center gap-2 text-sm text-mist">
        <Gamepad2 className="h-4 w-4 text-neon-cyan" />
        <span className="truncate">{details.currentGame ?? 'No active game'}</span>
      </div>

      {/* Stats grid */}
      <div className="grid grid-cols-2 gap-2">
        {[
          { label: 'Today', value: formatDuration(details.todayPlaySeconds), icon: Clock, color: 'text-neon-amber' },
          { label: 'Week', value: formatDuration(details.weeklyPlaySeconds), icon: CalendarClock, color: 'text-neon-cyan' },
          { label: 'Month', value: formatDuration(details.monthlyPlaySeconds), icon: CalendarClock, color: 'text-neon-purple' },
          { label: 'Total', value: formatDuration(details.totalPlaySeconds), icon: Trophy, color: 'text-emerald-400' }
        ].map(({ label, value, icon: Icon, color }) => (
          <div key={label} className="rounded-lg border border-neon-cyan/[0.08] bg-ink p-3">
            <div className="flex items-center gap-1.5 text-[10px] font-semibold uppercase tracking-wider text-zinc-500">
              <Icon className={`h-3 w-3 ${color}`} />
              {label}
            </div>
            <div className="mt-1 text-lg font-bold text-zinc-50">{value}</div>
          </div>
        ))}
      </div>

      {/* Playtime overview chart */}
      <div className="rounded-lg border border-neon-cyan/[0.08] bg-ink p-4">
        <div className="mb-3 flex items-center justify-between">
          <div className="flex items-center gap-2 text-sm font-semibold text-zinc-100">
            <BarChart3 className="h-4 w-4 text-neon-purple" />
            Playtime overview
          </div>
          <span className="text-[10px] text-zinc-500">Last 30 days</span>
        </div>
        <div className="h-44">
          <ResponsiveContainer width="100%" height="100%">
            <AreaChart data={chartData}>
              <defs>
                <linearGradient id="playtimeGrad" x1="0" y1="0" x2="0" y2="1">
                  <stop offset="0%" stopColor="#b347ea" stopOpacity={0.3} />
                  <stop offset="100%" stopColor="#b347ea" stopOpacity={0} />
                </linearGradient>
              </defs>
              <CartesianGrid stroke="#2a2a33" vertical={false} />
              <XAxis dataKey="date" stroke="#52525b" tickLine={false} axisLine={false} tick={{ fontSize: 10 }} />
              <YAxis stroke="#52525b" tickLine={false} axisLine={false} tick={{ fontSize: 10 }} tickFormatter={(v) => formatDuration(v)} width={45} />
              <Tooltip
                cursor={{ stroke: '#b347ea', strokeWidth: 1 }}
                contentStyle={{ background: '#0a0a1a', border: '1px solid rgba(0,229,255,0.2)', borderRadius: 8, color: '#f4f4f5', fontSize: 12 }}
                formatter={(value) => [formatDuration(value), 'Playtime']}
              />
              <Area type="monotone" dataKey="playSeconds" stroke="#b347ea" fill="url(#playtimeGrad)" strokeWidth={2} />
            </AreaChart>
          </ResponsiveContainer>
        </div>
      </div>

      {/* Statistics */}
      <div className="rounded-lg border border-neon-cyan/[0.08] bg-ink p-4">
        <div className="mb-3 flex items-center gap-2 text-sm font-semibold text-zinc-100">
          <BarChart3 className="h-4 w-4 text-neon-cyan" />
          Statistics
        </div>
        <div className="space-y-2.5">
          {[
            { label: 'First seen', value: details.createdAt ? formatDateTime(details.createdAt) : 'N/A' },
            { label: 'Last seen', value: details.lastSeenPlaying ? formatDateTime(details.lastSeenPlaying) : 'Never' },
            { label: 'Last seen status', value: meta.label, dot: meta.dot }
          ].map(({ label, value, dot }) => (
            <div key={label} className="flex items-center justify-between text-xs">
              <span className="flex items-center gap-1.5 text-mist">
                <Clock className="h-3 w-3" />
                {label}
              </span>
              <span className="flex items-center gap-1.5 font-medium text-zinc-200">
                {dot && <span className={`h-1.5 w-1.5 rounded-full ${dot}`} />}
                {value}
              </span>
            </div>
          ))}
        </div>
      </div>

      {/* Delete button */}
      {isAdmin && (
        <button
          type="button"
          onClick={() => onDelete(details.id)}
          disabled={busy}
          className="inline-flex min-h-10 w-full items-center justify-center gap-2 rounded-lg border border-red-400/30 bg-red-400/10 px-4 text-sm font-semibold text-red-200 transition hover:bg-red-400/20 disabled:opacity-60"
        >
          <Trash2 className="h-4 w-4" />
          Remove Player
        </button>
      )}
    </div>
  );
}

// ─── Sidebar: Leaderboard ────────────────────────────────────
const rankStyles = [
  {
    bg: 'bg-gradient-to-r from-yellow-500/10 to-yellow-600/5',
    border: 'border-yellow-500/30',
    badge: 'bg-gradient-to-br from-yellow-400 to-yellow-600 text-zinc-950',
    text: 'text-yellow-400',
    icon: '👑'
  },
  {
    bg: 'bg-gradient-to-r from-zinc-400/10 to-zinc-500/5',
    border: 'border-zinc-400/30',
    badge: 'bg-gradient-to-br from-zinc-300 to-zinc-500 text-zinc-950',
    text: 'text-zinc-300',
    icon: '🥈'
  },
  {
    bg: 'bg-gradient-to-r from-amber-700/10 to-amber-800/5',
    border: 'border-amber-700/30',
    badge: 'bg-gradient-to-br from-amber-600 to-amber-800 text-zinc-950',
    text: 'text-amber-600',
    icon: '🥉'
  }
];

function RankBadge({ rank }) {
  const style = rankStyles[rank - 1];
  if (!style) return <span className="w-6 text-center text-xs font-bold text-zinc-500">{rank}.</span>;
  return (
    <span className={`inline-flex h-6 w-6 items-center justify-center rounded-full text-[10px] font-bold ${style.badge}`}>
      {rank}
    </span>
  );
}

function Leaderboard({ players }) {
  return (
    <div className="rounded-xl border border-neon-cyan/[0.08] bg-[#08081a] p-4">
      <div className="mb-3 flex items-center gap-2 text-sm font-semibold text-zinc-100">
        <Trophy className="h-4 w-4 text-neon-amber" />
        Weekly leaderboard
      </div>
      <div className="space-y-2">
        {players.slice(0, 5).map((player, index) => {
          const isTop3 = index < 3;
          const style = isTop3 ? rankStyles[index] : null;
          return (
            <div
              key={player.playerId}
              className={`flex items-center gap-2.5 rounded-lg px-2.5 py-2 transition ${
                isTop3
                  ? `${style.bg} border ${style.border}`
                  : 'hover:bg-neon-cyan/[0.03]'
              }`}
            >
              <RankBadge rank={index + 1} />
              <Avatar player={{ username: player.username, avatarUrl: player.avatarUrl }} size="sm" />
              <div className="min-w-0 flex-1">
                <div className={`truncate text-sm font-medium ${isTop3 ? style.text : 'text-zinc-100'}`}>
                  {player.username}
                </div>
                <div className="text-xs text-mist">{formatDuration(player.weeklyPlaySeconds)}</div>
              </div>
            </div>
          );
        })}
        {players.length === 0 && <div className="text-sm text-mist">No players yet</div>}
      </div>
    </div>
  );
}

// ─── Main App ────────────────────────────────────────────────
export default function App() {
  const [user, setUser] = useState(() => {
    const token = localStorage.getItem('token');
    const role = localStorage.getItem('role');
    const username = localStorage.getItem('username');
    if (token && role && username) return { token, role, username };
    return null;
  });
  const [showLogin, setShowLogin] = useState(false);
  const [showRequestForm, setShowRequestForm] = useState(false);

  useEffect(() => {
    setOnAuthExpired(() => { setUser(null); setShowLogin(false); });
  }, []);

  const isAdmin = user?.role === 'Admin';

  const handleLogin = useCallback((data) => { setUser(data); setShowLogin(false); }, []);
  const handleLogout = useCallback(() => { api.logout(); setUser(null); }, []);

  const [players, setPlayers] = useState([]);
  const [leaderboard, setLeaderboard] = useState([]);
  const [selectedId, setSelectedId] = useState(null);
  const [details, setDetails] = useState(null);
  const [search, setSearch] = useState('');
  const [clubFilter, setClubFilter] = useState('all');
  const [view, setView] = useState('cards');
  const [sortBy, setSortBy] = useState('daily');
  const [sortDirection, setSortDirection] = useState('desc');
  const [page, setPage] = useState(1);
  const PAGE_SIZE = 10;
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [checking, setChecking] = useState(false);
  const [error, setError] = useState('');
  const [lastUpdated, setLastUpdated] = useState(null);
  const [lastRefreshAt, setLastRefreshAt] = useState(null);
  const [tick, setTick] = useState(0);

  const loadDashboard = useCallback(async (silent = false) => {
    if (!silent) setLoading(true);
    try {
      const [dashboard, weekly] = await Promise.all([api.dashboard(), api.weeklyLeaderboard()]);
      setPlayers(dashboard);
      setLeaderboard(weekly);
      setLastUpdated(new Date());
      setLastRefreshAt(Date.now());
      setError('');
    } catch (err) { setError(err.message); } finally { setLoading(false); }
  }, []);

  const loadDetails = useCallback(async (id) => {
    if (!id) { setDetails(null); return; }
    try { const player = await api.player(id); setDetails(player); setError(''); } catch (err) { setError(err.message); }
  }, []);

  useEffect(() => {
    loadDashboard();
    const timer = window.setInterval(() => { loadDashboard(true); if (selectedId) loadDetails(selectedId); }, 15000);
    return () => window.clearInterval(timer);
  }, [loadDashboard, loadDetails, selectedId]);

  useEffect(() => { const t = setInterval(() => setTick((x) => x + 1), 1000); return () => clearInterval(t); }, []);
  useEffect(() => { loadDetails(selectedId); }, [loadDetails, selectedId]);

  const livePlayers = useMemo(() => {
    if (!lastRefreshAt || players.length === 0) return players;
    const elapsed = Math.floor((Date.now() - lastRefreshAt) / 1000);
    if (elapsed <= 0) return players;
    return players.map((p) => {
      if (!p.currentStatus?.startsWith('Playing')) return p;
      return { ...p, todayPlaySeconds: p.todayPlaySeconds + elapsed, totalPlaySeconds: p.totalPlaySeconds + elapsed };
    });
  }, [players, lastRefreshAt, tick]);

  const filteredPlayers = useMemo(() => {
    const q = search.trim().toLowerCase();
    const visible = livePlayers.filter((p) => {
      if (clubFilter !== 'all' && p.club !== clubFilter) return false;
      if (!q) return true;
      return [p.username, p.currentStatus, p.currentGame, String(p.robloxUserId), p.club].filter(Boolean).some((v) => v.toLowerCase().includes(q));
    });
    const dir = sortDirection === 'asc' ? 1 : -1;
    return [...visible].sort((a, b) => {
      if (sortBy === 'username') return a.username.localeCompare(b.username) * dir;
      const av = sortBy === 'total' ? a.totalPlaySeconds : a.todayPlaySeconds;
      const bv = sortBy === 'total' ? b.totalPlaySeconds : b.todayPlaySeconds;
      return (av - bv) * dir;
    });
  }, [livePlayers, search, clubFilter, sortBy, sortDirection]);

  const totals = useMemo(() => {
    const onlineCount = livePlayers.filter((p) => p.currentStatus === 'Online' || p.currentStatus?.startsWith('Playing')).length;
    const offlineCount = livePlayers.length - onlineCount;
    const inGameCount = livePlayers.filter((p) => p.currentStatus?.startsWith('Playing')).length;
    const inWebsiteCount = livePlayers.filter((p) => p.currentStatus === 'Online').length;
    return {
      playerCount: livePlayers.length,
      playingCount: livePlayers.filter((p) => p.currentStatus?.startsWith('Playing')).length,
      todaySeconds: livePlayers.reduce((s, p) => s + p.todayPlaySeconds, 0),
      totalSeconds: livePlayers.reduce((s, p) => s + p.totalPlaySeconds, 0),
      onlineCount,
      offlineCount,
      inGameCount,
      inWebsiteCount,
      todayInGame: livePlayers.filter((p) => p.currentStatus?.startsWith('Playing')).reduce((s, p) => s + p.todayPlaySeconds, 0),
      todayInWebsite: livePlayers.filter((p) => p.currentStatus === 'Online').reduce((s, p) => s + p.todayPlaySeconds, 0),
      totalInGame: livePlayers.filter((p) => p.currentStatus?.startsWith('Playing')).reduce((s, p) => s + p.totalPlaySeconds, 0),
      totalInWebsite: livePlayers.filter((p) => p.currentStatus === 'Online').reduce((s, p) => s + p.totalPlaySeconds, 0)
    };
  }, [livePlayers]);

  const liveDetails = useMemo(() => {
    if (!details || !lastRefreshAt) return details;
    if (!details.currentStatus?.startsWith('Playing')) return details;
    const elapsed = Math.floor((Date.now() - lastRefreshAt) / 1000);
    if (elapsed <= 0) return details;
    return { ...details, todayPlaySeconds: details.todayPlaySeconds + elapsed, totalPlaySeconds: details.totalPlaySeconds + elapsed, weeklyPlaySeconds: details.weeklyPlaySeconds + elapsed, monthlyPlaySeconds: details.monthlyPlaySeconds + elapsed };
  }, [details, lastRefreshAt, tick]);

  function updateSort(field) {
    if (sortBy === field) { setSortDirection((c) => (c === 'asc' ? 'desc' : 'asc')); return; }
    setSortBy(field);
    setSortDirection(field === 'username' ? 'asc' : 'desc');
  }

  async function addPlayer(body) {
    setBusy(true);
    try { const c = await api.addPlayer(body); await loadDashboard(true); setSelectedId(c.id); setError(''); } catch (err) { setError(err.message); } finally { setBusy(false); }
  }

  async function runCheckNow() {
    setChecking(true);
    try { await api.checkNow(); await loadDashboard(true); if (selectedId) await loadDetails(selectedId); setError(''); } catch (err) { setError(err.message); } finally { setChecking(false); }
  }

  async function deletePlayer(id) {
    if (!window.confirm('Remove this player and their playtime history?')) return;
    setBusy(true);
    try { await api.deletePlayer(id); setSelectedId(null); setDetails(null); await loadDashboard(true); setError(''); } catch (err) { setError(err.message); } finally { setBusy(false); }
  }

  async function changeClub(id, club) {
    setBusy(true);
    try { await api.updateClub(id, club); await loadDetails(id); await loadDashboard(true); setError(''); } catch (err) { setError(err.message); } finally { setBusy(false); }
  }

  if (showLogin) return <LoginPage onLogin={handleLogin} />;

  return (
    <div className="min-h-screen bg-[#050510] text-zinc-50">
      {/* ─── Header ─── */}
      <header className="sticky top-0 z-20 border-b border-neon-cyan/[0.08] bg-[#050510]/90 backdrop-blur-md">
        <div className="mx-auto flex max-w-[1400px] items-center justify-between px-5 py-3">
          <div className="flex items-center gap-3">
            <div className="grid h-9 w-9 place-items-center rounded-lg bg-neon-cyan text-zinc-950">
              <Gamepad2 className="h-5 w-5" />
            </div>
            <div>
              <h1 className="text-base font-bold tracking-tight text-zinc-50">Club Playtime</h1>
              <div className="text-[11px] text-mist">Racket Rivals tracker</div>
            </div>
          </div>

          <div className="flex items-center gap-2">
            {!isAdmin && (
              <button type="button" onClick={() => setShowRequestForm(!showRequestForm)} className="inline-flex h-9 items-center justify-center gap-2 rounded-lg border border-neon-green/30 bg-neon-green/10 px-3 text-sm font-medium text-neon-green transition hover:bg-neon-green/20">
                <UserPlus className="h-4 w-4" />
                Request to Join
              </button>
            )}

            {isAdmin && <JoinRequestsDropdown />}

            <button type="button" onClick={() => api.downloadCsv()} className="inline-flex h-9 items-center justify-center gap-2 rounded-lg border border-neon-cyan/[0.08] px-3 text-sm font-medium text-zinc-200 transition hover:bg-neon-cyan/[0.06]">
              <Download className="h-4 w-4" />
              CSV
            </button>

            {isAdmin && (
              <button type="button" onClick={runCheckNow} disabled={checking} className="inline-flex h-9 items-center justify-center gap-2 rounded-lg bg-neon-cyan px-3 text-sm font-semibold text-zinc-950 transition hover:bg-neon-cyan/80 disabled:opacity-60">
                <RefreshCw className={`h-4 w-4 ${checking ? 'animate-spin' : ''}`} />
                Check Now
              </button>
            )}

            {user ? (
              <div className="flex items-center gap-2">
                <span className="inline-flex items-center gap-1.5 rounded-lg border border-neon-cyan/[0.08] px-3 py-1.5 text-xs font-medium text-zinc-300">
                  <Shield className="h-3.5 w-3.5" />
                  {user.username}
                  {isAdmin && <span className="text-neon-green">(Admin)</span>}
                </span>
                <button type="button" onClick={handleLogout} className="grid h-9 w-9 place-items-center rounded-lg border border-neon-cyan/[0.08] text-mist transition hover:bg-neon-cyan/[0.06] hover:text-zinc-100">
                  <LogOut className="h-4 w-4" />
                </button>
              </div>
            ) : (
              <button type="button" onClick={() => setShowLogin(true)} className="inline-flex h-9 items-center justify-center gap-2 rounded-lg bg-neon-cyan px-3 text-sm font-semibold text-zinc-950 transition hover:bg-neon-cyan/80">
                <LogIn className="h-4 w-4" />
                Admin Login
              </button>
            )}
          </div>
        </div>
      </header>

      {showRequestForm && (
        <div className="mx-auto max-w-[1400px] px-5 pt-4">
          <RequestJoinForm onClose={() => setShowRequestForm(false)} />
        </div>
      )}

      {/* ─── Main Content ─── */}
      <main className="mx-auto max-w-[1400px] px-5 py-5">
        <div className="flex gap-6">
          {/* Left: main area */}
          <div className="min-w-0 flex-1 space-y-5">
            {error && (
              <div className="rounded-lg border border-red-400/30 bg-red-400/10 px-4 py-3 text-sm text-red-100">{error}</div>
            )}

            {/* Stats */}
            <section className="grid gap-3 grid-cols-2 lg:grid-cols-4">
              <div className="rounded-xl border border-neon-cyan/[0.08] bg-[#08081a] p-4">
                <div className="flex items-center gap-2 text-[11px] font-semibold uppercase tracking-wider text-zinc-500">
                  <Activity className="h-3.5 w-3.5" />
                  Players
                </div>
                <div className="mt-2 text-2xl font-bold text-zinc-50">{totals.playerCount}</div>
                <div className="mt-1 flex items-center gap-2 text-[11px] text-mist">
                  <span className="flex items-center gap-1"><span className="h-1.5 w-1.5 rounded-full bg-neon-green" />{totals.onlineCount} Online</span>
                  <span>/</span>
                  <span className="flex items-center gap-1"><span className="h-1.5 w-1.5 rounded-full bg-neon-amber" />{totals.offlineCount} Offline</span>
                </div>
              </div>

              <div className="rounded-xl border border-neon-cyan/[0.08] bg-[#08081a] p-4">
                <div className="flex items-center gap-2 text-[11px] font-semibold uppercase tracking-wider text-zinc-500">
                  <Play className="h-3.5 w-3.5" />
                  Playing Now
                </div>
                <div className="mt-2 text-2xl font-bold text-zinc-50">{totals.playingCount}</div>
                <div className="mt-1 flex items-center gap-2 text-[11px] text-mist">
                  <span className="flex items-center gap-1"><span className="h-1.5 w-1.5 rounded-full bg-neon-green" />{totals.inGameCount} In-game</span>
                  <span>/</span>
                  <span className="flex items-center gap-1"><span className="h-1.5 w-1.5 rounded-full bg-neon-cyan" />{totals.inWebsiteCount} In website</span>
                </div>
              </div>

              <div className="rounded-xl border border-neon-cyan/[0.08] bg-[#08081a] p-4">
                <div className="flex items-center gap-2 text-[11px] font-semibold uppercase tracking-wider text-zinc-500">
                  <Clock className="h-3.5 w-3.5" />
                  Today
                </div>
                <div className="mt-2 text-2xl font-bold text-zinc-50">{formatDuration(totals.todaySeconds)}</div>
                <div className="mt-1 flex items-center gap-2 text-[11px] text-mist">
                  <span className="flex items-center gap-1"><span className="h-1.5 w-1.5 rounded-full bg-neon-green" />{formatDuration(totals.todayInGame)} In-game</span>
                  <span>/</span>
                  <span className="flex items-center gap-1"><span className="h-1.5 w-1.5 rounded-full bg-neon-cyan" />{formatDuration(totals.todayInWebsite)} In website</span>
                </div>
              </div>

              <div className="rounded-xl border border-neon-cyan/[0.08] bg-[#08081a] p-4">
                <div className="flex items-center gap-2 text-[11px] font-semibold uppercase tracking-wider text-zinc-500">
                  <BarChart3 className="h-3.5 w-3.5" />
                  All Time
                </div>
                <div className="mt-2 text-2xl font-bold text-zinc-50">{formatDuration(totals.totalSeconds)}</div>
                <div className="mt-1 flex items-center gap-2 text-[11px] text-mist">
                  <span className="flex items-center gap-1"><span className="h-1.5 w-1.5 rounded-full bg-neon-green" />{formatDuration(totals.totalInGame)} In-game</span>
                  <span>/</span>
                  <span className="flex items-center gap-1"><span className="h-1.5 w-1.5 rounded-full bg-neon-cyan" />{formatDuration(totals.totalInWebsite)} In website</span>
                </div>
              </div>
            </section>

            {/* Add Player (admin only) */}
            {isAdmin && <AddPlayerForm onAdd={addPlayer} busy={busy} />}

            {/* Search + Filter + Sort + View */}
            <section className="rounded-xl border border-neon-cyan/[0.08] bg-[#08081a] p-4 space-y-3">
              <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
                <div className="relative flex-1 max-w-md">
                  <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-zinc-500" />
                  <input
                    value={search}
                    onChange={(e) => { setSearch(e.target.value); setPage(1); }}
                    placeholder="Search player..."
                    className="h-10 w-full rounded-lg border border-neon-cyan/[0.08] bg-ink py-2 pl-10 pr-3 text-sm text-zinc-50 placeholder:text-zinc-500 focus:border-zinc-500 transition"
                  />
                </div>

                <div className="flex items-center gap-2">
                  <select
                    value={sortBy}
                    onChange={(e) => { setSortBy(e.target.value); setSortDirection(e.target.value === 'username' ? 'asc' : 'desc'); setPage(1); }}
                    className="h-10 rounded-lg border border-neon-cyan/[0.08] bg-ink px-3 text-sm text-zinc-50"
                  >
                    <option value="daily">Daily playtime</option>
                    <option value="total">Total playtime</option>
                    <option value="username">Username</option>
                  </select>

                  <div className="inline-flex rounded-lg border border-neon-cyan/[0.08] bg-ink p-1">
                    <button type="button" onClick={() => setView('cards')} className={`grid h-8 w-8 place-items-center rounded-md transition ${view === 'cards' ? 'bg-zinc-700 text-zinc-50' : 'text-mist hover:text-zinc-100'}`} title="Cards">
                      <LayoutGrid className="h-4 w-4" />
                    </button>
                    <button type="button" onClick={() => setView('table')} className={`grid h-8 w-8 place-items-center rounded-md transition ${view === 'table' ? 'bg-zinc-700 text-zinc-50' : 'text-mist hover:text-zinc-100'}`} title="Table">
                      <Table2 className="h-4 w-4" />
                    </button>
                  </div>
                </div>
              </div>

              <div className="flex items-center justify-between border-t border-neon-cyan/[0.08] pt-3">
                <div className="flex items-center gap-1.5">
                  {[
                    { value: 'all', label: 'All', dot: 'bg-zinc-400' },
                    { value: 'PIH', label: 'PIH', dot: 'bg-neon-purple' },
                    { value: 'P1H', label: 'P1H', dot: 'bg-neon-amber' },
                    { value: '__none__', label: 'None', dot: 'bg-zinc-600' }
                  ].map(({ value, label, dot }) => (
                    <button
                      key={value}
                      type="button"
                      onClick={() => { setClubFilter(value === '__none__' ? '' : value); setPage(1); }}
                      className={`inline-flex h-7 items-center gap-1.5 rounded-md border px-2.5 text-xs font-medium transition ${
                        (value === 'all' && clubFilter === 'all') || (value === '__none__' && clubFilter === '') || clubFilter === value
                          ? 'border-zinc-500 bg-zinc-700/50 text-zinc-50'
                          : 'border-transparent text-zinc-400 hover:text-zinc-200'
                      }`}
                    >
                      <span className={`h-2 w-2 rounded-full ${dot}`} />
                      {label}
                    </button>
                  ))}
                </div>

                <div className="flex items-center gap-3 text-xs text-zinc-500">
                  {lastUpdated && <span>Updated {formatDateTime(lastUpdated.toISOString())}</span>}
                  {loading && <span>Loading...</span>}
                  <span>{filteredPlayers.length} shown</span>
                </div>
              </div>
            </section>

            {/* Players */}
            {(() => {
              const totalPages = Math.max(1, Math.ceil(filteredPlayers.length / PAGE_SIZE));
              const safePage = Math.min(page, totalPages);
              const pageStart = (safePage - 1) * PAGE_SIZE;
              const pagePlayers = filteredPlayers.slice(pageStart, pageStart + PAGE_SIZE);

              return (
                <>
                  {view === 'cards' ? (
                    <section className="grid gap-3 md:grid-cols-2 xl:grid-cols-3">
                      {pagePlayers.map((player, i) => (
                        <PlayerCard key={player.id} player={player} onSelect={setSelectedId} selected={selectedId === player.id} rank={pageStart + i + 1} />
                      ))}
                      {!loading && filteredPlayers.length === 0 && (
                        <div className="rounded-xl border border-neon-cyan/[0.08] bg-[#08081a] p-6 text-sm text-mist">No players found</div>
                      )}
                    </section>
                  ) : (
                    <PlayersTable players={pagePlayers} selectedId={selectedId} onSelect={setSelectedId} sortBy={sortBy} sortDirection={sortDirection} onSort={updateSort} startIndex={pageStart} />
                  )}

                  {/* Pagination */}
                  {totalPages > 1 && (
                    <div className="flex items-center justify-between text-xs text-zinc-500 pt-1">
                      <span>Showing {pageStart + 1} to {Math.min(pageStart + PAGE_SIZE, filteredPlayers.length)} of {filteredPlayers.length} players</span>
                      <div className="flex items-center gap-1">
                        <button
                          type="button"
                          onClick={() => setPage((p) => Math.max(1, p - 1))}
                          disabled={safePage <= 1}
                          className="grid h-7 w-7 place-items-center rounded border border-neon-cyan/[0.08] text-mist transition hover:bg-neon-cyan/[0.06] hover:text-zinc-100 disabled:opacity-30"
                        >
                          ◀
                        </button>
                        {Array.from({ length: totalPages }, (_, i) => i + 1).map((p) => (
                          <button
                            key={p}
                            type="button"
                            onClick={() => setPage(p)}
                            className={`grid h-7 w-7 place-items-center rounded text-[11px] font-medium transition ${
                              p === safePage
                                ? 'bg-zinc-700 text-zinc-50'
                                : 'text-zinc-400 hover:bg-neon-cyan/[0.06] hover:text-zinc-100'
                            }`}
                          >
                            {p}
                          </button>
                        ))}
                        <button
                          type="button"
                          onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
                          disabled={safePage >= totalPages}
                          className="grid h-7 w-7 place-items-center rounded border border-neon-cyan/[0.08] text-mist transition hover:bg-neon-cyan/[0.06] hover:text-zinc-100 disabled:opacity-30"
                        >
                          ▶
                        </button>
                      </div>
                    </div>
                  )}
                </>
              );
            })()}
          </div>

          {/* Right: Sidebar */}
          <div className="hidden w-[340px] shrink-0 space-y-4 xl:block xl:sticky xl:top-16 xl:self-start">
            <Leaderboard players={leaderboard} />
            <DetailPanel details={liveDetails} onClose={() => setSelectedId(null)} onDelete={deletePlayer} busy={busy} isAdmin={isAdmin} onClubChange={changeClub} />
          </div>
        </div>
      </main>
    </div>
  );
}
