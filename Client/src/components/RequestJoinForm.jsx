import { useState } from 'react';
import { UserPlus, CheckCircle, AlertCircle, X, ExternalLink, UserCheck } from 'lucide-react';
import { api } from '../lib/api';

const ROBLOX_FRIEND_URL = 'https://www.roblox.com/users/11291447439/profile';

export default function RequestJoinForm({ onClose }) {
  const [form, setForm] = useState({
    robloxUsername: '',
    robloxUserId: '',
    discordUserId: '',
    club: 'PIH',
    customClub: '',
    note: '',
    addedFriend: false
  });
  const [busy, setBusy] = useState(false);
  const [result, setResult] = useState(null);

  async function handleSubmit(event) {
    event.preventDefault();

    if (!form.addedFriend) {
      setResult({ type: 'error', message: 'You must confirm that you added the user on Roblox before submitting.' });
      return;
    }

    setBusy(true);
    setResult(null);

    try {
      await api.submitJoinRequest({
        robloxUsername: form.robloxUsername.trim(),
        robloxUserId: Number(form.robloxUserId),
        discordUserId: form.discordUserId.trim(),
        club: form.club === 'Custom' ? form.customClub : form.club,
        note: form.note.trim() || undefined
      });
      setResult({ type: 'success', message: 'Request submitted! An admin will review it soon.' });
      setForm({ robloxUsername: '', robloxUserId: '', discordUserId: '', club: 'PIH', customClub: '', note: '', addedFriend: false });
    } catch (err) {
      setResult({ type: 'error', message: err.message });
    } finally {
      setBusy(false);
    }
  }

  function handleChange(field) {
    return (event) => setForm((prev) => ({ ...prev, [field]: event.target.value }));
  }

  return (
    <div className="rounded-xl border border-line bg-panel p-6 shadow-glow">
      <div className="flex items-center justify-between mb-4">
        <div className="flex items-center gap-2">
          <UserPlus className="h-5 w-5 text-emerald-400" />
          <h2 className="text-lg font-semibold text-zinc-50">Request to Join</h2>
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

      <p className="mb-4 text-sm text-mist">
        Fill in your Roblox details to request being added to the tracker. An admin will review your request.
      </p>

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
              className="w-full min-h-10 rounded-md border border-line bg-ink px-3 text-sm text-zinc-50 placeholder:text-zinc-500"
              inputMode="numeric"
              required
            />
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
              onChange={handleChange('discordUserId')}
              placeholder="Your Discord user ID"
              className="w-full min-h-10 rounded-md border border-line bg-ink px-3 text-sm text-zinc-50 placeholder:text-zinc-500"
              required
            />
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
          {busy ? 'Submitting...' : 'Submit Request'}
        </button>
      </form>
    </div>
  );
}
