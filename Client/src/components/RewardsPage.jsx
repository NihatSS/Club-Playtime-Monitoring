import { useCallback, useEffect, useState } from 'react';
import { Crown, Gift, Pencil, Trophy } from 'lucide-react';
import Header from './Header.jsx';
import { api } from '../lib/api.js';

function formatDuration(totalSeconds) {
  const h = Math.floor(totalSeconds / 3600);
  const m = Math.floor((totalSeconds % 3600) / 60);
  if (h > 0) return `${h}h ${m}m`;
  return `${m}m`;
}

const RANK_STYLES = [
  'bg-gradient-to-br from-yellow-400 to-yellow-600 text-zinc-950',
  'bg-gradient-to-br from-zinc-300 to-zinc-500 text-zinc-950',
  'bg-gradient-to-br from-amber-600 to-amber-800 text-zinc-950'
];

function Avatar({ url, username }) {
  if (url) {
    return <img src={url} alt="" className="h-10 w-10 shrink-0 rounded-full border border-neon-cyan/20 object-cover" />;
  }
  return (
    <span className="grid h-10 w-10 shrink-0 place-items-center rounded-full border border-neon-cyan/20 bg-panelSoft text-sm font-bold text-zinc-400">
      {username?.charAt(0).toUpperCase() ?? '?'}
    </span>
  );
}

/** Inline prize editor (admin only). */
function PrizeEditor({ prize, onSaved }) {
  const [editing, setEditing] = useState(false);
  const [value, setValue] = useState(prize);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');

  useEffect(() => {
    setValue(prize);
  }, [prize]);

  if (!editing) {
    return (
      <button
        type="button"
        onClick={() => setEditing(true)}
        className="inline-flex items-center gap-1.5 rounded-lg border border-neon-cyan/[0.15] px-2.5 py-1.5 text-xs font-medium text-zinc-300 transition hover:bg-neon-cyan/[0.06] hover:text-zinc-100"
        title="Edit prize"
      >
        <Pencil className="h-3.5 w-3.5" />
        Edit prize
      </button>
    );
  }

  async function save(e) {
    e.preventDefault();
    if (!value.trim() || busy) return;
    setBusy(true);
    setError('');
    try {
      await api.updateMonthlyReward(value.trim());
      onSaved();
      setEditing(false);
    } catch (err) {
      setError(err.message || 'Failed to save.');
    } finally {
      setBusy(false);
    }
  }

  return (
    <form onSubmit={save} className="flex flex-wrap items-center gap-2">
      <input
        value={value}
        onChange={(e) => setValue(e.target.value)}
        maxLength={300}
        autoFocus
        placeholder="e.g. 10 USD Roblox gift card"
        className="min-h-9 w-64 rounded-lg border border-neon-cyan/[0.15] bg-ink px-3 text-sm text-zinc-50 placeholder:text-zinc-500 focus:border-neon-cyan/40 transition"
      />
      <button
        type="submit"
        disabled={busy || !value.trim()}
        className="min-h-9 rounded-lg bg-neon-cyan px-3 text-xs font-bold text-zinc-950 transition hover:bg-neon-cyan/80 disabled:opacity-50"
      >
        {busy ? 'Saving…' : 'Save'}
      </button>
      <button
        type="button"
        onClick={() => { setEditing(false); setValue(prize); setError(''); }}
        className="min-h-9 rounded-lg border border-zinc-700/60 px-3 text-xs font-medium text-zinc-400 transition hover:text-zinc-200"
      >
        Cancel
      </button>
      {error && <span className="text-xs text-red-300">{error}</span>}
    </form>
  );
}

export default function RewardsPage({ user, avatarUrl, isAdmin, onSignIn, onLogout }) {
  const [data, setData] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  const load = useCallback(async () => {
    try {
      const reward = await api.monthlyReward();
      setData(reward);
      setError('');
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    load();
  }, [load]);

  const monthName = data
    ? new Date(data.year, data.month - 1, 1).toLocaleDateString(undefined, { month: 'long', year: 'numeric' })
    : '';
  const winner = data?.topPlayers?.[0];

  return (
    <div className="min-h-screen bg-[#050510] text-zinc-50">
      <Header user={user} avatarUrl={avatarUrl} isAdmin={isAdmin} onSignIn={onSignIn} onLogout={onLogout} />
      <div className="mx-auto max-w-3xl space-y-5 px-5 py-6">
        <button
          type="button"
          onClick={() => { window.location.hash = ''; }}
          className="inline-flex items-center gap-1.5 rounded-lg border border-neon-cyan/[0.08] px-3 py-2 text-sm font-medium text-zinc-300 transition hover:bg-neon-cyan/[0.06]"
        >
          ← Back to tracker
        </button>

        <div>
          <h1 className="flex items-center gap-2 text-xl font-bold">
            <Gift className="h-5 w-5 text-neon-cyan" />
            Monthly Reward
          </h1>
          <p className="mt-1 text-sm text-mist">
            The player with the most playtime in {monthName || 'this month'} wins the reward.
          </p>
        </div>

        {error && (
          <div className="rounded-lg border border-red-400/30 bg-red-400/10 px-4 py-3 text-sm text-red-200">{error}</div>
        )}

        {loading ? (
          <div className="rounded-xl border border-neon-cyan/[0.08] bg-[#08081a] p-6 text-sm text-mist">Loading…</div>
        ) : data ? (
          <>
            {/* Prize card */}
            <div className="rounded-xl border border-neon-cyan/[0.12] bg-gradient-to-br from-[#0a0a1a] to-[#12122a] p-5">
              <div className="flex flex-wrap items-start justify-between gap-3">
                <div className="min-w-0">
                  <div className="flex items-center gap-2 text-[10px] font-bold uppercase tracking-widest text-zinc-500">
                    <Gift className="h-3.5 w-3.5 text-neon-cyan" />
                    This month's prize
                  </div>
                  <div className="mt-1.5 text-lg font-bold text-zinc-50">
                    {data.prize || <span className="font-medium text-zinc-500">Not set yet</span>}
                  </div>
                  {data.prize && (
                    <div className="mt-1 text-[11px] text-zinc-500">
                      {winner ? `Goes to ${winner.username} if the month ended today.` : 'Nobody has playtime yet this month.'}
                    </div>
                  )}
                </div>
                {isAdmin && <PrizeEditor prize={data.prize} onSaved={load} />}
              </div>
            </div>

            {/* Current top 10 */}
            <div className="rounded-xl border border-neon-cyan/[0.08] bg-[#08081a]">
              <div className="flex items-center gap-2 border-b border-neon-cyan/[0.08] px-4 py-3 text-sm font-semibold text-zinc-100">
                <Trophy className="h-4 w-4 text-neon-amber" />
                {monthName} leaderboard
              </div>
              {data.topPlayers.length === 0 ? (
                <div className="px-4 py-6 text-sm text-zinc-500">No playtime recorded yet this month.</div>
              ) : (
                <ol className="divide-y divide-neon-cyan/[0.06]">
                  {data.topPlayers.map((p, i) => (
                    <li key={p.playerId} className="flex items-center gap-3 px-4 py-2.5">
                      {i < 3 ? (
                        <span className={`grid h-8 w-8 shrink-0 place-items-center rounded-full text-xs font-black ${RANK_STYLES[i]}`}>
                          {i + 1}
                        </span>
                      ) : (
                        <span className="grid h-8 w-8 shrink-0 place-items-center rounded-full bg-panelSoft text-xs font-bold text-zinc-400">
                          {i + 1}
                        </span>
                      )}
                      <Avatar url={p.avatarUrl} username={p.username} />
                      <div className="min-w-0 flex-1">
                        <div className="flex items-center gap-1.5">
                          <span className="truncate text-sm font-semibold text-zinc-50">{p.username}</span>
                          {i === 0 && <Crown className="h-4 w-4 shrink-0 text-yellow-400" />}
                        </div>
                        <div className="text-[11px] text-zinc-500">Total {formatDuration(p.totalPlaySeconds)}</div>
                      </div>
                      <div className="shrink-0 text-right">
                        <div className="text-sm font-bold text-neon-cyan">{formatDuration(p.playSeconds)}</div>
                        <div className="text-[10px] uppercase tracking-wider text-zinc-500">this month</div>
                      </div>
                    </li>
                  ))}
                </ol>
              )}
            </div>
          </>
        ) : null}
      </div>
    </div>
  );
}
