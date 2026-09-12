import { useCallback, useEffect, useState } from 'react';
import {
  AlertCircle,
  ArrowLeft,
  CheckCircle,
  Clock,
  Crown,
  Pencil,
  Plus,
  RefreshCw,
  Swords,
  Trash2,
  Trophy,
  Users
} from 'lucide-react';
import { api } from '../lib/api';
import { formatDateTime } from '../lib/format';
import Header from './Header';

const TEAM_MODE_LABELS = {
  Solo: '1v1',
  DuoRandom: '2v2 Random Teams',
  DuoPredefined: '2v2 Predefined Teams'
};

const STATUS_STYLES = {
  DRAFT: 'border-zinc-500/30 bg-zinc-500/10 text-zinc-300',
  REGISTRATION_OPEN: 'border-emerald-400/30 bg-emerald-400/10 text-emerald-300',
  REGISTRATION_CLOSED: 'border-amber-400/30 bg-amber-400/10 text-amber-300',
  UPCOMING: 'border-neon-cyan/30 bg-neon-cyan/10 text-neon-cyan',
  IN_PROGRESS: 'border-neon-purple/40 bg-neon-purple/10 text-neon-purple',
  COMPLETED: 'border-neon-green/30 bg-neon-green/10 text-neon-green',
  CANCELLED: 'border-red-400/30 bg-red-400/10 text-red-300'
};

const STATUS_LABELS = {
  DRAFT: 'Draft',
  REGISTRATION_OPEN: 'Registration Open',
  REGISTRATION_CLOSED: 'Registration Closed',
  UPCOMING: 'Upcoming',
  IN_PROGRESS: 'In Progress',
  COMPLETED: 'Completed',
  CANCELLED: 'Cancelled'
};

export function StatusBadge({ status }) {
  return (
    <span
      className={`inline-flex items-center rounded-md border px-2 py-0.5 text-[10px] font-semibold uppercase tracking-wider ${
        STATUS_STYLES[status] ?? STATUS_STYLES.DRAFT
      }`}
    >
      {STATUS_LABELS[status] ?? status}
    </span>
  );
}

// Local datetime string -> ISO UTC for the API.
function toUtcIso(localValue) {
  if (!localValue) return null;
  return new Date(localValue).toISOString();
}

function TournamentForm({ initial, busy, error, onSubmit, onCancel }) {
  const [form, setForm] = useState(
    initial ?? {
      name: '',
      description: '',
      rulesText: '',
      teamMode: 'Solo',
      registrationStart: '',
      registrationDeadline: '',
      startsAt: '',
      maxParticipants: 0,
      prizeInfo: '',
      prize1: '',
      prize2: '',
      prize3: ''
    }
  );

  const inputClass =
    'w-full min-h-10 rounded-lg border border-neon-cyan/[0.08] bg-ink px-3 text-sm text-zinc-50 placeholder:text-zinc-500';

  function submit(event) {
    event.preventDefault();
    const prizes = [];
    if (form.prize1?.trim()) prizes.push({ placement: 1, description: form.prize1.trim() });
    if (form.prize2?.trim()) prizes.push({ placement: 2, description: form.prize2.trim() });
    if (form.prize3?.trim()) prizes.push({ placement: 3, description: form.prize3.trim() });

    onSubmit({
      name: form.name.trim(),
      description: form.description.trim(),
      rulesText: form.rulesText.trim(),
      teamMode: form.teamMode,
      registrationStartsAt: toUtcIso(form.registrationStart),
      registrationDeadline: toUtcIso(form.registrationDeadline),
      startsAt: toUtcIso(form.startsAt),
      maxParticipants: Number(form.maxParticipants) || 0,
      prizeInfo: form.prizeInfo.trim(),
      prizes
    });
  }

  return (
    <form onSubmit={submit} className="space-y-4 rounded-xl border border-neon-cyan/[0.12] bg-[#08081a] p-5">
      <div className="grid gap-3 sm:grid-cols-2">
        <div>
          <label htmlFor="t-name" className="mb-1 block text-xs font-medium text-zinc-400">Tournament name *</label>
          <input
            id="t-name"
            value={form.name}
            onChange={(e) => setForm((c) => ({ ...c, name: e.target.value }))}
            className={inputClass}
            required
            maxLength={120}
          />
        </div>
        <div>
          <label htmlFor="t-mode" className="mb-1 block text-xs font-medium text-zinc-400">Format *</label>
          <select
            id="t-mode"
            value={form.teamMode}
            onChange={(e) => setForm((c) => ({ ...c, teamMode: e.target.value }))}
            className={inputClass}
            disabled={!!initial}
          >
            <option value="Solo">1v1</option>
            <option value="DuoRandom">2v2 — Random Teams</option>
            <option value="DuoPredefined">2v2 — Predefined Teams</option>
          </select>
        </div>
      </div>

      <div>
        <label htmlFor="t-desc" className="mb-1 block text-xs font-medium text-zinc-400">Description</label>
        <textarea
          id="t-desc"
          value={form.description}
          onChange={(e) => setForm((c) => ({ ...c, description: e.target.value }))}
          className={`${inputClass} min-h-20`}
          maxLength={4000}
          placeholder="What is this tournament about? Shown above the schedule."
        />
      </div>

      <div>
        <label htmlFor="t-rules" className="mb-1 block text-xs font-medium text-zinc-400">Rules (one per line)</label>
        <textarea
          id="t-rules"
          value={form.rulesText}
          onChange={(e) => setForm((c) => ({ ...c, rulesText: e.target.value }))}
          className={`${inputClass} min-h-24`}
          maxLength={4000}
          placeholder={'Awakenings r not allowed\nEach team can select 1 spirit to ban\nCheating is not allowed'}
        />
        <p className="mt-1 text-[11px] text-zinc-500">Each line becomes a bullet in the Rules tab.</p>
      </div>

      <div className="grid gap-3 sm:grid-cols-3">
        <div>
          <label htmlFor="t-regstart" className="mb-1 block text-xs font-medium text-zinc-400">Registration opens</label>
          <input
            id="t-regstart"
            type="datetime-local"
            value={form.registrationStart}
            onChange={(e) => setForm((c) => ({ ...c, registrationStart: e.target.value }))}
            className={inputClass}
          />
        </div>
        <div>
          <label htmlFor="t-regdeadline" className="mb-1 block text-xs font-medium text-zinc-400">Registration deadline *</label>
          <input
            id="t-regdeadline"
            type="datetime-local"
            value={form.registrationDeadline}
            onChange={(e) => setForm((c) => ({ ...c, registrationDeadline: e.target.value }))}
            className={inputClass}
            required
          />
        </div>
        <div>
          <label htmlFor="t-starts" className="mb-1 block text-xs font-medium text-zinc-400">Tournament start *</label>
          <input
            id="t-starts"
            type="datetime-local"
            value={form.startsAt}
            onChange={(e) => setForm((c) => ({ ...c, startsAt: e.target.value }))}
            className={inputClass}
            required
          />
        </div>
      </div>

      <div className="grid gap-3 sm:grid-cols-2">
        <div>
          <label htmlFor="t-max" className="mb-1 block text-xs font-medium text-zinc-400">Max participants (0 = unlimited)</label>
          <input
            id="t-max"
            type="number"
            min={0}
            max={1024}
            value={form.maxParticipants}
            onChange={(e) => setForm((c) => ({ ...c, maxParticipants: e.target.value }))}
            className={inputClass}
          />
        </div>
        <div>
          <label htmlFor="t-prizeinfo" className="mb-1 block text-xs font-medium text-zinc-400">Prize summary (optional)</label>
          <input
            id="t-prizeinfo"
            value={form.prizeInfo}
            onChange={(e) => setForm((c) => ({ ...c, prizeInfo: e.target.value }))}
            className={inputClass}
            maxLength={500}
          />
        </div>
      </div>

      <div className="grid gap-3 sm:grid-cols-3">
        <div>
          <label htmlFor="t-prize1" className="mb-1 block text-xs font-medium text-amber-300">🥇 1st place prize</label>
          <input
            id="t-prize1"
            value={form.prize1}
            onChange={(e) => setForm((c) => ({ ...c, prize1: e.target.value }))}
            className={inputClass}
            maxLength={300}
          />
        </div>
        <div>
          <label htmlFor="t-prize2" className="mb-1 block text-xs font-medium text-zinc-300">🥈 2nd place prize</label>
          <input
            id="t-prize2"
            value={form.prize2}
            onChange={(e) => setForm((c) => ({ ...c, prize2: e.target.value }))}
            className={inputClass}
            maxLength={300}
          />
        </div>
        <div>
          <label htmlFor="t-prize3" className="mb-1 block text-xs font-medium text-amber-600">🥉 3rd place prize</label>
          <input
            id="t-prize3"
            value={form.prize3}
            onChange={(e) => setForm((c) => ({ ...c, prize3: e.target.value }))}
            className={inputClass}
            maxLength={300}
          />
        </div>
      </div>

      {error && (
        <div className="flex items-center gap-2 rounded-lg border border-red-400/30 bg-red-400/10 px-4 py-3 text-sm text-red-100">
          <AlertCircle className="h-4 w-4 shrink-0" />
          {error}
        </div>
      )}

      <div className="flex flex-wrap gap-2">
        <button
          type="submit"
          disabled={busy}
          className="inline-flex min-h-10 items-center gap-2 rounded-lg bg-neon-cyan px-4 text-sm font-semibold text-zinc-950 transition hover:bg-neon-cyan/80 disabled:opacity-60"
        >
          <CheckCircle className="h-4 w-4" />
          {busy ? 'Saving...' : initial ? 'Save changes' : 'Create tournament'}
        </button>
        <button
          type="button"
          onClick={onCancel}
          className="inline-flex min-h-10 items-center rounded-lg border border-neon-cyan/[0.08] px-4 text-sm font-medium text-zinc-300 transition hover:bg-neon-cyan/[0.06]"
        >
          Cancel
        </button>
      </div>
    </form>
  );
}

export default function TournamentsPage({ user, avatarUrl, isAdmin, onLogout, onSignIn }) {
  const [tournaments, setTournaments] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [showCreate, setShowCreate] = useState(false);
  const [createBusy, setCreateBusy] = useState(false);
  const [createError, setCreateError] = useState('');
  const [notice, setNotice] = useState('');

  const load = useCallback(async () => {
    try {
      const data = await api.tournaments();
      setTournaments(data);
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

  async function handleCreate(body) {
    setCreateBusy(true);
    setCreateError('');
    try {
      await api.createTournament(body);
      setNotice('Tournament created as a draft. Open it to review, then open registration.');
      setShowCreate(false);
      await load();
    } catch (err) {
      setCreateError(err.message);
    } finally {
      setCreateBusy(false);
    }
  }

  async function handleDelete(id, name) {
    if (!window.confirm(`Delete tournament '${name}'? This also deletes its participants, teams and bracket.`)) return;
    try {
      await api.deleteTournament(id);
      await load();
    } catch (err) {
      setError(err.message);
    }
  }

  return (
    <div className="min-h-screen bg-[#050510] text-zinc-50">
      <Header user={user} avatarUrl={avatarUrl} isAdmin={isAdmin} onSignIn={onSignIn} onLogout={onLogout} />
      <div className="mx-auto max-w-5xl space-y-5 px-5 py-6">
        <div className="flex items-center justify-between">
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
            Tournaments
          </h1>
          {isAdmin && (
            <button
              type="button"
              onClick={() => { setShowCreate((v) => !v); setCreateError(''); }}
              className="inline-flex items-center gap-1.5 rounded-lg bg-neon-cyan px-3 py-2 text-sm font-semibold text-zinc-950 transition hover:bg-neon-cyan/80"
            >
              <Plus className="h-4 w-4" />
              New tournament
            </button>
          )}
        </div>

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

        {isAdmin && showCreate && (
          <TournamentForm
            busy={createBusy}
            error={createError}
            onSubmit={handleCreate}
            onCancel={() => setShowCreate(false)}
          />
        )}

        {loading ? (
          <div className="rounded-xl border border-neon-cyan/[0.08] bg-[#08081a] p-8 text-center text-sm text-mist">
            Loading tournaments...
          </div>
        ) : tournaments.length === 0 ? (
          <div className="rounded-xl border border-neon-cyan/[0.08] bg-[#08081a] p-8 text-center">
            <Swords className="mx-auto h-10 w-10 text-zinc-600" />
            <p className="mt-3 text-sm text-mist">No tournaments yet.</p>
            {isAdmin && <p className="mt-1 text-xs text-zinc-500">Create the first one with the button above.</p>}
          </div>
        ) : (
          <div className="grid gap-3">
            {tournaments.map((t) => (
              <div
                key={t.id}
                className="rounded-xl border border-neon-cyan/[0.08] bg-[#08081a] p-4 transition hover:border-neon-cyan/25"
              >
                <div className="flex flex-wrap items-start justify-between gap-3">
                  <button
                    type="button"
                    onClick={() => { window.location.hash = `tournaments/${t.id}`; }}
                    className="min-w-0 flex-1 text-left"
                  >
                    <div className="flex flex-wrap items-center gap-2">
                      <span className="text-base font-semibold text-zinc-50 hover:text-neon-cyan transition">{t.name}</span>
                      <StatusBadge status={t.status} />
                      <span className="inline-flex items-center rounded-md border border-line bg-ink px-1.5 py-0.5 text-[10px] font-semibold uppercase tracking-wider text-zinc-300">
                        {TEAM_MODE_LABELS[t.teamMode] ?? t.teamMode}
                      </span>
                    </div>
                    {t.description && (
                      <p className="mt-1 line-clamp-2 max-w-2xl text-sm text-mist">{t.description}</p>
                    )}
                    <div className="mt-2 flex flex-wrap items-center gap-x-4 gap-y-1 text-xs text-zinc-500">
                      <span className="flex items-center gap-1">
                        <Users className="h-3 w-3" />
                        {t.participantCount}{t.maxParticipants > 0 ? ` / ${t.maxParticipants}` : ''} participants
                      </span>
                      <span className="flex items-center gap-1">
                        <Clock className="h-3 w-3" />
                        Starts {formatDateTime(t.startsAt)}
                      </span>
                      <span className="flex items-center gap-1">
                        <Swords className="h-3 w-3" />
                        Register by {formatDateTime(t.registrationDeadline)}
                      </span>
                      {t.winnerName && (
                        <span className="flex items-center gap-1 font-semibold text-amber-300">
                          <Crown className="h-3 w-3" />
                          Winner: {t.winnerName}
                        </span>
                      )}
                    </div>
                  </button>
                  {isAdmin && (
                    <div className="flex items-center gap-2">
                      <button
                        type="button"
                        onClick={() => { window.location.hash = `tournaments/${t.id}`; }}
                        className="inline-flex h-8 items-center gap-1.5 rounded-lg border border-neon-cyan/[0.08] px-3 text-xs font-medium text-zinc-200 transition hover:bg-neon-cyan/[0.06]"
                      >
                        <Pencil className="h-3.5 w-3.5" />
                        Manage
                      </button>
                      <button
                        type="button"
                        onClick={() => handleDelete(t.id, t.name)}
                        className="grid h-8 w-8 place-items-center rounded-lg border border-red-400/20 text-red-300 transition hover:bg-red-400/10"
                        title="Delete tournament"
                      >
                        <Trash2 className="h-3.5 w-3.5" />
                      </button>
                    </div>
                  )}
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}
