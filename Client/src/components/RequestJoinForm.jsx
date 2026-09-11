import { useCallback, useEffect, useState } from 'react';
import {
  AlertCircle,
  CheckCircle,
  Clock,
  ExternalLink,
  Pencil,
  UserCheck,
  UserPlus,
  X,
  XCircle
} from 'lucide-react';
import { api } from '../lib/api';

const ROBLOX_FRIEND_URL = 'https://www.roblox.com/users/11291447439/profile';

const CLUB_OPTIONS = ['PIH', 'P1H', 'Custom', 'None'];

function toFormState(req, defaultDiscordUserId = '') {
  const isCustomClub = !!req?.club && !CLUB_OPTIONS.includes(req.club);
  return {
    robloxUsername: req?.robloxUsername ?? '',
    robloxUserId: req?.robloxUserId != null ? String(req.robloxUserId) : '',
    discordUserId: req?.discordUserId ?? defaultDiscordUserId,
    club: isCustomClub ? 'Custom' : (req?.club || 'PIH'),
    customClub: isCustomClub ? req.club : '',
    note: req?.note ?? '',
    addedFriend: false
  };
}

function StatusPill({ status }) {
  if (status === 'Approved') {
    return (
      <span className="inline-flex items-center gap-1.5 rounded-md border border-emerald-400/30 bg-emerald-400/10 px-2 py-0.5 text-[10px] font-semibold uppercase tracking-wider text-emerald-300">
        <CheckCircle className="h-3 w-3" />
        Accepted
      </span>
    );
  }
  if (status === 'Rejected') {
    return (
      <span className="inline-flex items-center gap-1.5 rounded-md border border-red-400/30 bg-red-400/10 px-2 py-0.5 text-[10px] font-semibold uppercase tracking-wider text-red-300">
        <XCircle className="h-3 w-3" />
        Declined
      </span>
    );
  }
  return (
    <span className="inline-flex items-center gap-1.5 rounded-md border border-amber-400/30 bg-amber-400/10 px-2 py-0.5 text-[10px] font-semibold uppercase tracking-wider text-amber-300">
      <Clock className="h-3 w-3" />
      Waiting for answer
    </span>
  );
}

export default function RequestJoinForm({ onClose, onMyRequestChange, defaultDiscordUserId = '', profile = null }) {
  const [myRequest, setMyRequest] = useState(null);
  const [loadingMine, setLoadingMine] = useState(true);
  const [editing, setEditing] = useState(false);
  const [form, setForm] = useState(() =>
    toFormState(
      profile?.joinRequest ?? null,
      defaultDiscordUserId || profile?.discordUserId || ''
    )
  );
  const [busy, setBusy] = useState(false);
  const [result, setResult] = useState(null);

  const loadMyRequest = useCallback(async () => {
    // Phase 10: signed-in users resolve their request by account, not by a
    // Roblox ID they type in — avoids mismatches after a username change.
    if (profile) {
      try {
        setLoadingMine(true);
        const data = await api.getMyJoinRequestAuthenticated();
        const request = data.exists ? data.request : null;
        setMyRequest(request);
        onMyRequestChange?.(request);
        if (!form.robloxUserId && request) {
          setForm((prev) => ({ ...prev, ...toFormState(request, profile.discordUserId || defaultDiscordUserId), robloxUserId: String(request.robloxUserId) }));
        }
      } catch {
        setMyRequest(null);
        onMyRequestChange?.(null);
      } finally {
        setLoadingMine(false);
      }
      return;
    }

    const userId = (form.robloxUserId || '').trim();
    if (!/^\d+$/.test(userId)) {
      setMyRequest(null);
      setLoadingMine(false);
      onMyRequestChange?.(null);
      return;
    }
    try {
      setLoadingMine(true);
      const data = await api.getMyJoinRequest(userId);
      const request = data.exists ? data.request : null;
      setMyRequest(request);
      onMyRequestChange?.(request);
    } catch {
      setMyRequest(null);
      onMyRequestChange?.(null);
    } finally {
      setLoadingMine(false);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [form.robloxUserId, onMyRequestChange, profile]);

  useEffect(() => {
    loadMyRequest();
  }, [loadMyRequest]);

  function handleChange(field) {
    return (event) => setForm((prev) => ({ ...prev, [field]: event.target.value }));
  }

  // Prefill from profile once it arrives (Phase 10: request uses profile info).
  useEffect(() => {
    if (!profile || myRequest || editing) return;
    setForm((prev) => ({
      ...prev,
      robloxUsername: prev.robloxUsername || profile.player?.username || '',
      robloxUserId: prev.robloxUserId || (profile.player ? String(profile.player.robloxUserId) : ''),
      discordUserId: prev.discordUserId || profile.discordUserId || ''
    }));
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [profile]);

  async function handleSubmit(event) {
    event.preventDefault();

    if (!form.addedFriend) {
      setResult({ type: 'error', message: 'You must confirm that you added the user on Roblox before submitting.' });
      return;
    }

    setBusy(true);
    setResult(null);

    try {
      const clubValue = form.club === 'Custom' ? form.customClub.trim() : form.club;
      const payload = {
        robloxUsername: form.robloxUsername.trim(),
        robloxUserId: Number(form.robloxUserId),
        discordUserId: form.discordUserId.trim(),
        club: clubValue,
        note: form.note.trim() || undefined
      };

      if (editing && myRequest) {
        const updated = await api.updateJoinRequest(myRequest.id, payload);
        setMyRequest(updated);
        setEditing(false);
        setResult({ type: 'success', message: 'Request updated!' });
        onMyRequestChange?.(updated);
      } else {
        const created = await api.submitJoinRequest(payload);
        setMyRequest(created);
        setResult({ type: 'success', message: 'Request submitted! An admin will review it soon.' });
        setForm((prev) => ({ ...prev, addedFriend: false }));
        onMyRequestChange?.(created);
      }
    } catch (err) {
      setResult({ type: 'error', message: err.message });
    } finally {
      setBusy(false);
    }
  }

  function startEditing() {
    setForm((prev) => ({ ...toFormState(myRequest, defaultDiscordUserId), robloxUserId: prev.robloxUserId, addedFriend: false }));
    setEditing(true);
    setResult(null);
  }

  function cancelEditing() {
    setForm((prev) => ({ ...toFormState(null, defaultDiscordUserId), robloxUserId: prev.robloxUserId, addedFriend: false }));
    setEditing(false);
    setResult(null);
  }

  const showForm = !myRequest || editing;

  return (
    <div className="rounded-xl border border-line bg-panel p-6 shadow-glow">
      <div className="flex items-center justify-between mb-4">
        <div className="flex items-center gap-2">
          <UserPlus className="h-5 w-5 text-emerald-400" />
          <h2 className="text-lg font-semibold text-zinc-50">
            {myRequest && !editing ? 'My Join Request' : 'Request to Join'}
          </h2>
        </div>
        {onClose && (
          <button
            type="button"
            onClick={onClose}
            className="grid h-8 w-8 place-items-center rounded-md border border-line text-mist transition hover:bg-zinc-800 hover:text-zinc-100"
          >
            <X className="h-4 w-4" />
          </button>
        )}
      </div>

      {/* Loading my request */}
      {loadingMine && !myRequest && (
        <div className="mb-4 text-sm text-mist">Checking for an existing request...</div>
      )}

      {/* ─── Existing request: status card ─── */}
      {myRequest && !editing && (
        <div className="rounded-lg border border-line bg-ink p-4">
          <div className="flex items-center justify-between gap-3">
            <div className="flex items-center gap-2 min-w-0">
              <span className="truncate text-base font-semibold text-zinc-50">{myRequest.robloxUsername}</span>
              <StatusPill status={myRequest.status} />
            </div>
            {myRequest.status === 'Pending' && (
              <button
                type="button"
                onClick={startEditing}
                className="inline-flex items-center gap-1.5 rounded-md border border-line px-2.5 py-1 text-xs font-medium text-zinc-300 transition hover:bg-zinc-800 hover:text-zinc-100"
              >
                <Pencil className="h-3 w-3" />
                Edit request
              </button>
            )}
          </div>

          <div className="mt-2 grid gap-1 text-sm text-mist">
            <div>Roblox ID: <span className="text-zinc-300">{myRequest.robloxUserId}</span></div>
            <div>Discord ID: <span className="text-zinc-300">{myRequest.discordUserId}</span></div>
            <div>Club: <span className="text-zinc-300">{myRequest.club || 'None'}</span></div>
            {myRequest.note && <div>Note: <span className="text-zinc-300">{myRequest.note}</span></div>}
            <div>Submitted: <span className="text-zinc-300">{new Date(myRequest.createdAt).toLocaleString()}</span></div>
            {myRequest.reviewedAt && (
              <div>
                Reviewed: <span className="text-zinc-300">{new Date(myRequest.reviewedAt).toLocaleString()}{myRequest.reviewedBy ? ` by ${myRequest.reviewedBy}` : ''}</span>
              </div>
            )}
          </div>

          {myRequest.status === 'Pending' && (
            <div className="mt-3 flex items-center gap-2 rounded-md border border-amber-400/20 bg-amber-400/10 px-3 py-2 text-xs text-amber-200">
              <Clock className="h-3.5 w-3.5 shrink-0" />
              Waiting for an admin to review your request. You can still edit it while it's pending.
            </div>
          )}
          {myRequest.status === 'Approved' && (
            <div className="mt-3 flex items-center gap-2 rounded-md border border-emerald-400/20 bg-emerald-400/10 px-3 py-2 text-xs text-emerald-200">
              <CheckCircle className="h-3.5 w-3.5 shrink-0" />
              Your request was accepted! You'll appear in the tracker shortly.
            </div>
          )}
          {myRequest.status === 'Rejected' && (
            <div className="mt-3 flex items-center gap-2 rounded-md border border-red-400/20 bg-red-400/10 px-3 py-2 text-xs text-red-200">
              <XCircle className="h-3.5 w-3.5 shrink-0" />
              Your request was declined. You can submit a new request anytime.
            </div>
          )}
        </div>
      )}

      {/* ─── Form (new request or editing) ─── */}
      {showForm && (
        <>
          {/* Step 1: Add friend on Roblox */}
          <div className="mb-5 rounded-lg border border-sky-400/30 bg-sky-400/10 p-4">
            <div className="flex items-start gap-3">
              <div className="grid h-8 w-8 shrink-0 place-items-center rounded-md bg-sky-400/20">
                <UserCheck className="h-4 w-4 text-sky-300" />
              </div>
              <div className="min-w-0">
                <div className="text-sm font-semibold text-sky-100">Step 1: Add us on Roblox</div>
                <p className="mt-1 text-xs text-sky-200/80">
                  You must add our Roblox account as a friend before submitting your request.
                </p>
                <a
                  href={ROBLOX_FRIEND_URL}
                  target="_blank"
                  rel="noreferrer"
                  className="mt-2 inline-flex items-center gap-1.5 rounded-md border border-sky-400/30 bg-sky-400/10 px-3 py-1.5 text-xs font-semibold text-sky-200 transition hover:bg-sky-400/20"
                >
                  <ExternalLink className="h-3 w-3" />
                  Open Roblox Profile
                </a>
              </div>
            </div>
          </div>

          {/* Result message */}
          {result && (
            <div className={`mb-4 flex items-center gap-2 rounded-lg border px-4 py-3 text-sm ${
              result.type === 'success'
                ? 'border-emerald-400/30 bg-emerald-400/10 text-emerald-100'
                : 'border-red-400/30 bg-red-400/10 text-red-100'
            }`}>
              {result.type === 'success' ? (
                <CheckCircle className="h-4 w-4 shrink-0" />
              ) : (
                <AlertCircle className="h-4 w-4 shrink-0" />
              )}
              {result.message}
            </div>
          )}

          {editing && (
            <p className="mb-4 text-sm text-mist">
              Editing your pending request. The Roblox user ID cannot be changed.
            </p>
          )}

          <form onSubmit={handleSubmit} className="space-y-3">
            <div className="grid gap-3 sm:grid-cols-2">
              <div>
                <label htmlFor="rj-username" className="block text-xs font-medium text-zinc-400 mb-1">
                  Roblox Username *
                </label>
                <input
                  id="rj-username"
                  value={form.robloxUsername}
                  onChange={handleChange('robloxUsername')}
                  placeholder="Your Roblox username"
                  className="w-full min-h-10 rounded-md border border-line bg-ink px-3 text-sm text-zinc-50 placeholder:text-zinc-500"
                  required
                  maxLength={100}
                />
              </div>
              <div>
                <label htmlFor="rj-userid" className="block text-xs font-medium text-zinc-400 mb-1">
                  Roblox User ID *
                </label>
                <input
                  id="rj-userid"
                  value={form.robloxUserId}
                  onChange={handleChange('robloxUserId')}
                  placeholder="Your Roblox user ID"
                  className={`w-full min-h-10 rounded-md border border-line bg-ink px-3 text-sm text-zinc-50 placeholder:text-zinc-500 ${editing ? 'opacity-60 cursor-not-allowed' : ''}`}
                  inputMode="numeric"
                  required
                  readOnly={editing}
                />
                {editing && (
                  <p className="mt-1 text-[11px] text-zinc-500">The Roblox user ID cannot be changed.</p>
                )}
              </div>
            </div>

            <div className="grid gap-3 sm:grid-cols-2">
              <div>
                <label htmlFor="rj-discord" className="block text-xs font-medium text-zinc-400 mb-1">
                  Discord User ID *
                </label>
                <input
                  id="rj-discord"
                  value={form.discordUserId}
                  onChange={(event) => setForm((prev) => ({ ...prev, discordUserId: event.target.value.replace(/\s/g, '') }))}
                  placeholder="123456789012345678"
                  className="w-full min-h-10 rounded-md border border-line bg-ink px-3 text-sm text-zinc-50 placeholder:text-zinc-500"
                  inputMode="numeric"
                  required
                  minLength={17}
                  maxLength={20}
                />
                <p className="mt-1 text-[11px] text-zinc-500">Use Copy User ID in Discord Developer Mode, not your username.</p>
              </div>
              <div>
                <label htmlFor="rj-club" className="block text-xs font-medium text-zinc-400 mb-1">
                  Club *
                </label>
                <select
                  id="rj-club"
                  value={form.club}
                  onChange={handleChange('club')}
                  className="w-full min-h-10 rounded-md border border-line bg-ink px-3 text-sm text-zinc-50"
                  required
                >
                  <option value="PIH">PIH (Main)</option>
                  <option value="P1H">P1H (Second)</option>
                  <option value="Custom">Custom</option>
                  <option value="None">None</option>
                </select>
              </div>
            </div>

            {form.club === 'Custom' && (
              <div>
                <label htmlFor="rj-custom-club" className="block text-xs font-medium text-zinc-400 mb-1">
                  Custom Club Name *
                </label>
                <input
                  id="rj-custom-club"
                  value={form.customClub}
                  onChange={handleChange('customClub')}
                  placeholder="Enter your club name"
                  className="w-full min-h-10 rounded-md border border-line bg-ink px-3 text-sm text-zinc-50 placeholder:text-zinc-500"
                  required={form.club === 'Custom'}
                  maxLength={100}
                />
              </div>
            )}

            <div>
              <label htmlFor="rj-note" className="block text-xs font-medium text-zinc-400 mb-1">
                Note (optional)
              </label>
              <textarea
                id="rj-note"
                value={form.note}
                onChange={handleChange('note')}
                placeholder="Any additional info..."
                rows={2}
                className="w-full rounded-md border border-line bg-ink px-3 py-2 text-sm text-zinc-50 placeholder:text-zinc-500 resize-none"
                maxLength={500}
              />
            </div>

            {/* Step 2: Confirm you added the friend */}
            <div className="rounded-lg border border-line bg-ink p-4">
              <label className="flex items-start gap-3 cursor-pointer">
                <input
                  type="checkbox"
                  checked={form.addedFriend}
                  onChange={(e) => setForm((prev) => ({ ...prev, addedFriend: e.target.checked }))}
                  className="mt-0.5 h-4 w-4 shrink-0 rounded border-zinc-600 bg-zinc-800 text-emerald-400 focus:ring-emerald-400/50"
                />
                <div>
                  <div className="text-sm font-medium text-zinc-100">
                    Did you add the user on Roblox?
                  </div>
                  <div className="mt-0.5 text-xs text-mist">
                    Yes, I added the Roblox account as a friend before submitting this request.
                  </div>
                </div>
              </label>
            </div>

            <button
              type="submit"
              disabled={busy || !form.addedFriend}
              className="w-full min-h-11 flex items-center justify-center gap-2 rounded-lg bg-emerald-400 px-4 text-sm font-semibold text-zinc-950 transition hover:bg-emerald-300 disabled:cursor-not-allowed disabled:opacity-40"
            >
              <UserPlus className="h-4 w-4" />
              {busy ? 'Saving...' : (editing ? 'Save Changes' : 'Submit Request')}
            </button>
            {editing && (
              <button
                type="button"
                onClick={cancelEditing}
                className="w-full min-h-11 flex items-center justify-center rounded-lg border border-line px-4 text-sm font-medium text-zinc-300 transition hover:bg-zinc-800 hover:text-zinc-100"
              >
                Cancel editing
              </button>
            )}
          </form>
        </>
      )}
    </div>
  );
}
