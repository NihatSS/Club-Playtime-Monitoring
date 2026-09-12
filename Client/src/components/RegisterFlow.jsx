import { useEffect, useRef, useState } from 'react';
import {
  AlertCircle,
  ArrowLeft,
  CheckCircle,
  Copy,
  ExternalLink,
  RefreshCw,
  Search,
  ShieldCheck,
  UserPlus,
  XCircle
} from 'lucide-react';
import { api } from '../lib/api';
import { passwordIssues } from '../lib/password';

const ROBLOX_PROFILE_EDIT_URL = 'https://www.roblox.com/users/profile/edit';

function maskRobloxId(id) {
  const s = String(id);
  if (s.length <= 4) return '•'.repeat(s.length);
  return '•'.repeat(s.length - 4) + s.slice(-4);
}

function StepHeader({ step, title, subtitle }) {
  const steps = ['choice', 'search', 'confirm', 'verify', 'create'];
  const index = steps.indexOf(step);
  return (
    <div className="mb-6 flex flex-col items-center">
      <div className="mb-3 flex items-center gap-1.5">
        {steps.map((s, i) => (
          <span
            key={s}
            className={`h-1.5 w-8 rounded-full transition ${
              i <= index ? 'bg-neon-cyan' : 'bg-zinc-700'
            }`}
          />
        ))}
      </div>
      <h1 className="text-2xl font-bold text-zinc-50">{title}</h1>
      {subtitle && <p className="mt-1 text-sm text-mist text-center max-w-md">{subtitle}</p>}
    </div>
  );
}

function ErrorBanner({ message }) {
  if (!message) return null;
  return (
    <div className="mb-4 flex items-center gap-2 rounded-lg border border-red-400/30 bg-red-400/10 px-4 py-3 text-sm text-red-100">
      <AlertCircle className="h-4 w-4 shrink-0" />
      {message}
    </div>
  );
}

function PasswordChecklist({ password }) {
  const issues = passwordIssues(password);
  const rules = [
    { label: 'At least 8 characters', met: !issues.some((i) => i.startsWith('At least')) },
    { label: 'An uppercase letter', met: !issues.includes('An uppercase letter') },
    { label: 'A lowercase letter', met: !issues.includes('A lowercase letter') },
    { label: 'A number', met: !issues.includes('A number') },
    { label: 'A symbol', met: !issues.includes('A symbol') }
  ];

  return (
    <ul className="mt-2 space-y-1">
      {rules.map((rule) => (
        <li key={rule.label} className={`flex items-center gap-1.5 text-xs ${rule.met ? 'text-emerald-300' : 'text-zinc-500'}`}>
          <span className={`grid h-3.5 w-3.5 shrink-0 place-items-center rounded-full border text-[9px] ${rule.met ? 'border-emerald-400/60 bg-emerald-400/10' : 'border-zinc-600'}`}>
            {rule.met ? '✓' : ''}
          </span>
          {rule.label}
        </li>
      ))}
    </ul>
  );
}

export default function RegisterFlow({ onLogin, onSwitchToLogin }) {
  const [step, setStep] = useState('choice');
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);

  // Claim flow state
  const [query, setQuery] = useState('');
  const [players, setPlayers] = useState([]);
  const [searching, setSearching] = useState(false);
  const [selectedPlayer, setSelectedPlayer] = useState(null);
  const [verification, setVerification] = useState(null); // { verificationId, code, expiresAt }
  const [verifyResult, setVerifyResult] = useState(null); // { verified, message, player, claimToken }
  const [copied, setCopied] = useState(false);

  // Account creation state
  const [form, setForm] = useState({ username: '', password: '', confirm: '', discordUserId: '' });

  const searchTimer = useRef(null);

  useEffect(() => {
    if (step !== 'search') return;
    clearTimeout(searchTimer.current);
    searchTimer.current = setTimeout(() => {
      doSearch(query);
    }, 250);
    return () => clearTimeout(searchTimer.current);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [query, step]);

  async function doSearch(q) {
    setSearching(true);
    setError('');
    try {
      const data = await api.playerSearch(q);
      setPlayers(data);
    } catch (err) {
      setError(err.message);
    } finally {
      setSearching(false);
    }
  }

  function choosePlayer(player) {
    setSelectedPlayer(player);
    setVerification(null);
    setVerifyResult(null);
    setError('');
    setStep('confirm');
  }

  async function startVerification() {
    setBusy(true);
    setError('');
    try {
      const data = await api.verifyStart(selectedPlayer.robloxUserId);
      setVerification(data);
      setStep('verify');
    } catch (err) {
      setError(err.message);
    } finally {
      setBusy(false);
    }
  }

  async function checkVerification() {
    if (!verification) return;
    setBusy(true);
    setError('');
    try {
      const data = await api.verifyCheck(verification.verificationId, verification.code);
      setVerifyResult(data);
      if (data.verified) {
        setStep('create');
      }
    } catch (err) {
      setError(err.message);
    } finally {
      setBusy(false);
    }
  }

  async function createAccount(event) {
    event.preventDefault();
    setBusy(true);
    setError('');

    if (form.password !== form.confirm) {
      setError('Passwords do not match.');
      setBusy(false);
      return;
    }
    const issues = passwordIssues(form.password);
    if (issues.length > 0) {
      setError('Password does not meet the requirements: ' + issues.join(', ') + '.');
      setBusy(false);
      return;
    }

    try {
      const body = {
        username: form.username.trim(),
        password: form.password,
        discordUserId: form.discordUserId.trim()
      };
      if (verifyResult?.claimToken) {
        body.claimToken = verifyResult.claimToken;
      }
      const data = await api.register(body);
      api.setAuth(data.token, data.role, data.username, data.discordUserId, data.id);
      onLogin(data);
    } catch (err) {
      setError(err.message);
    } finally {
      setBusy(false);
    }
  }

  async function copyCode() {
    if (!verification) return;
    try {
      await navigator.clipboard.writeText(verification.code);
      setCopied(true);
      setTimeout(() => setCopied(false), 1500);
    } catch {
      setError('Could not copy. Select and copy the code manually.');
    }
  }

  const inputClass =
    'w-full min-h-11 rounded-lg border border-line bg-ink px-4 text-sm text-zinc-50 placeholder:text-zinc-500 focus:border-neon-cyan/50 focus:outline-none focus:ring-1 focus:ring-neon-cyan/50';
  const primaryBtnClass =
    'inline-flex min-h-11 w-full items-center justify-center gap-2 rounded-lg bg-neon-cyan px-4 text-sm font-semibold text-zinc-950 transition hover:bg-neon-cyan/80 disabled:opacity-60';

  return (
    <div className="rounded-xl border border-line bg-panel p-6">
      {step !== 'choice' && (
        <button
          type="button"
          onClick={() => setStep('choice')}
          className="mb-4 inline-flex h-8 items-center gap-1.5 rounded-lg border border-neon-cyan/[0.08] px-2.5 text-xs font-medium text-zinc-400 transition hover:bg-neon-cyan/[0.06] hover:text-zinc-200"
        >
          <ArrowLeft className="h-3.5 w-3.5" />
          Back
        </button>
      )}
      {error && <ErrorBanner message={error} />}

      {/* ─── STEP: CHOICE ─── */}
      {step === 'choice' && (
        <>
          <StepHeader step="choice" title="Create your account" subtitle="Already registered? Use the button in the top-right to switch to login." />
          <div className="space-y-3">
            <button
              type="button"
              onClick={() => { setError(''); setStep('create'); }}
              className="w-full rounded-xl border border-neon-green/30 bg-neon-green/10 p-4 text-left transition hover:bg-neon-green/20"
            >
              <div className="flex items-center gap-3">
                <div className="grid h-10 w-10 place-items-center rounded-lg bg-neon-green text-zinc-950">
                  <UserPlus className="h-5 w-5" />
                </div>
                <div>
                  <div className="text-sm font-semibold text-neon-green">I'm new to the tracker</div>
                  <div className="text-xs text-mist mt-0.5">Create a new account, then request to join the tracker.</div>
                </div>
              </div>
            </button>
            <button
              type="button"
              onClick={() => { setError(''); setStep('search'); }}
              className="w-full rounded-xl border border-neon-cyan/30 bg-neon-cyan/10 p-4 text-left transition hover:bg-neon-cyan/20"
            >
              <div className="flex items-center gap-3">
                <div className="grid h-10 w-10 place-items-center rounded-lg bg-neon-cyan text-zinc-950">
                  <ShieldCheck className="h-5 w-5" />
                </div>
                <div>
                  <div className="text-sm font-semibold text-neon-cyan">I'm already in the tracker</div>
                  <div className="text-xs text-mist mt-0.5">Claim your existing tracker account and keep your playtime.</div>
                </div>
              </div>
            </button>
          </div>

          {onSwitchToLogin && (
            <div className="mt-6 border-t border-line pt-4 text-center">
              <p className="text-sm text-mist">
                Already have an account?{' '}
                <button
                  type="button"
                  onClick={onSwitchToLogin}
                  className="font-semibold text-neon-cyan hover:text-neon-cyan/80 transition"
                >
                  Login
                </button>
              </p>
            </div>
          )}
        </>
      )}

      {/* ─── STEP: SEARCH ─── */}
      {step === 'search' && (
        <>
          <StepHeader step="search" title="Select your tracker account" subtitle="Search for your Roblox username to find your existing tracker profile." />
          <div className="relative mb-4">
            <Search className="pointer-events-none absolute left-3.5 top-1/2 h-4 w-4 -translate-y-1/2 text-zinc-500" />
            <input
              value={query}
              onChange={(e) => setQuery(e.target.value)}
              placeholder="🔍 Search player..."
              className={`${inputClass} pl-10`}
              autoFocus
            />
          </div>
          <div className="max-h-80 space-y-2 overflow-y-auto thin-scrollbar pr-1">
            {searching && players.length === 0 ? (
              <div className="flex items-center justify-center gap-2 py-8 text-sm text-mist">
                <RefreshCw className="h-4 w-4 animate-spin" /> Searching...
              </div>
            ) : players.length === 0 ? (
              <div className="py-8 text-center text-sm text-mist">No players found{query ? ` for "${query}"` : ''}. If you're not in the tracker yet, go back and choose "I'm new to the tracker".</div>
            ) : (
              players.map((p) => (
                <button
                  key={p.playerId}
                  type="button"
                  disabled={p.isClaimed}
                  onClick={() => choosePlayer(p)}
                  className={`flex w-full items-center gap-3 rounded-lg border border-neon-cyan/[0.08] bg-ink p-3 text-left transition ${
                    p.isClaimed ? 'cursor-not-allowed opacity-40' : 'hover:border-neon-cyan/30 hover:bg-neon-cyan/[0.04]'
                  }`}
                >
                  {p.avatarUrl ? (
                    <img src={p.avatarUrl} alt="" className="h-10 w-10 shrink-0 rounded-lg border border-neon-cyan/[0.12] object-cover" />
                  ) : (
                    <div className="grid h-10 w-10 shrink-0 place-items-center rounded-lg border border-neon-cyan/[0.12] bg-panelSoft font-semibold text-neon-cyan/80">
                      {p.username.slice(0, 1).toUpperCase()}
                    </div>
                  )}
                  <div className="min-w-0 flex-1">
                    <div className="flex items-center gap-2">
                      <span className="truncate text-sm font-semibold text-zinc-100">{p.username}</span>
                      {p.club && (
                        <span className="inline-flex shrink-0 items-center rounded-md border border-neon-purple/40 bg-neon-purple/10 px-1.5 py-0.5 text-[10px] font-semibold uppercase tracking-wider text-neon-purple">
                          {p.club}
                        </span>
                      )}
                    </div>
                    <div className="text-xs text-mist mt-0.5">Roblox ID: {maskRobloxId(p.robloxUserId)}</div>
                  </div>
                  {p.isClaimed ? (
                    <span className="text-[10px] font-semibold uppercase tracking-wider text-zinc-500">Claimed</span>
                  ) : (
                    <span className="text-[11px] font-medium text-neon-cyan">Select</span>
                  )}
                </button>
              ))
            )}
          </div>
        </>
      )}

      {/* ─── STEP: CONFIRM ─── */}
      {step === 'confirm' && selectedPlayer && (
        <>
          <StepHeader step="confirm" title="Is this your account?" subtitle="We need to verify you own this Roblox account before claiming it." />
          <div className="mb-5 flex flex-col items-center rounded-xl border border-neon-cyan/[0.08] bg-ink p-5 text-center">
            {selectedPlayer.avatarUrl ? (
              <img src={selectedPlayer.avatarUrl} alt="" className="h-16 w-16 rounded-xl border border-neon-cyan/[0.12] object-cover" />
            ) : (
              <div className="grid h-16 w-16 place-items-center rounded-xl border border-neon-cyan/[0.12] bg-panelSoft text-xl font-bold text-neon-cyan/80">
                {selectedPlayer.username.slice(0, 1).toUpperCase()}
              </div>
            )}
            <div className="mt-3 text-lg font-semibold text-zinc-50">{selectedPlayer.username}</div>
            {selectedPlayer.club && (
              <span className="mt-1 inline-flex items-center rounded-md border border-neon-purple/40 bg-neon-purple/10 px-2 py-0.5 text-[10px] font-semibold uppercase tracking-wider text-neon-purple">
                {selectedPlayer.club}
              </span>
            )}
            <div className="mt-2 font-mono text-sm text-mist">Roblox ID: {maskRobloxId(selectedPlayer.robloxUserId)}</div>
          </div>
          <div className="space-y-3">
            <button type="button" onClick={startVerification} disabled={busy} className={primaryBtnClass}>
              <ShieldCheck className="h-4 w-4" />
              {busy ? 'Starting...' : 'Yes, this is my account'}
            </button>
            <button
              type="button"
              onClick={() => setStep('search')}
              className="inline-flex min-h-11 w-full items-center justify-center rounded-lg border border-neon-cyan/[0.08] px-4 text-sm font-medium text-zinc-300 transition hover:bg-neon-cyan/[0.06]"
            >
              Choose another account
            </button>
          </div>
        </>
      )}

      {/* ─── STEP: VERIFY ─── */}
      {step === 'verify' && verification && (
        <>
          <StepHeader step="verify" title="Verify you own this Roblox account" subtitle={`We'll check the public profile of ${selectedPlayer.username}'s Roblox account (ID ${selectedPlayer.robloxUserId}).`} />
          <div className="mb-4 rounded-xl border border-neon-amber/30 bg-neon-amber/10 p-4 text-center">
            <div className="text-[11px] font-semibold uppercase tracking-wider text-neon-amber">Your verification code</div>
            <div className="mt-2 font-mono text-3xl font-bold tracking-widest text-zinc-50">{verification.code}</div>
            <button
              type="button"
              onClick={copyCode}
              className="mt-3 inline-flex items-center gap-1.5 rounded-md border border-neon-amber/40 bg-neon-amber/10 px-3 py-1.5 text-xs font-semibold text-neon-amber transition hover:bg-neon-amber/20"
            >
              <Copy className="h-3.5 w-3.5" />
              {copied ? 'Copied!' : 'Copy code'}
            </button>
          </div>
          <ol className="mb-5 space-y-2 text-sm text-mist">
            <li>1. Copy the verification code above.</li>
            <li>
              2. Put it in your Roblox profile <span className="text-zinc-300">About</span> section —{' '}
              <a
                href={ROBLOX_PROFILE_EDIT_URL}
                target="_blank"
                rel="noreferrer"
                className="inline-flex items-center gap-1 font-medium text-neon-cyan underline decoration-neon-cyan/40 underline-offset-2 transition hover:text-neon-cyan/80"
              >
                open your About settings
                <ExternalLink className="h-3 w-3" />
              </a>
            </li>
            <li>3. Come back here and click <span className="text-zinc-300">Verify Roblox Account</span>.</li>
          </ol>
          {verifyResult && !verifyResult.verified && (
            <div className="mb-4 flex items-start gap-2 rounded-lg border border-red-400/30 bg-red-400/10 px-4 py-3 text-sm text-red-100">
              <XCircle className="mt-0.5 h-4 w-4 shrink-0" />
              {verifyResult.message}
            </div>
          )}
          <button type="button" onClick={checkVerification} disabled={busy} className={primaryBtnClass}>
            <ShieldCheck className="h-4 w-4" />
            {busy ? 'Checking...' : 'Verify Roblox Account'}
          </button>
        </>
      )}

      {/* ─── STEP: CREATE ─── */}
      {step === 'create' && (
        <>
          <StepHeader
            step="create"
            title={verifyResult?.verified ? 'Account verified — create your account' : 'Create your account'}
            subtitle={
              verifyResult?.verified
                ? `You verified ${verifyResult.player?.username}. Your existing playtime and club are preserved.`
                : 'Your account will be ready to request to join the tracker.'
            }
          />
          {verifyResult?.verified && (
            <div className="mb-4 flex items-center gap-2 rounded-lg border border-emerald-400/30 bg-emerald-400/10 px-4 py-3 text-sm text-emerald-200">
              <CheckCircle className="h-4 w-4 shrink-0" />
              Roblox account verified. You can now remove the code from your Roblox profile.
            </div>
          )}
          <form onSubmit={createAccount} className="space-y-4">
            <div>
              <label htmlFor="reg-username" className="mb-1.5 block text-sm font-medium text-zinc-300">Username</label>
              <input
                id="reg-username"
                type="text"
                value={form.username}
                onChange={(e) => setForm((c) => ({ ...c, username: e.target.value }))}
                placeholder="Choose a username"
                className={inputClass}
                required
                minLength={3}
                maxLength={50}
                autoComplete="username"
                autoFocus
              />
            </div>
            <div>
              <label htmlFor="reg-password" className="mb-1.5 block text-sm font-medium text-zinc-300">Password</label>
              <input
                id="reg-password"
                type="password"
                value={form.password}
                onChange={(e) => setForm((c) => ({ ...c, password: e.target.value }))}
                placeholder="Create a strong password"
                className={inputClass}
                required
                minLength={8}
                autoComplete="new-password"
              />
              <PasswordChecklist password={form.password} />
            </div>
            <div>
              <label htmlFor="reg-confirm" className="mb-1.5 block text-sm font-medium text-zinc-300">Confirm password</label>
              <input
                id="reg-confirm"
                type="password"
                value={form.confirm}
                onChange={(e) => setForm((c) => ({ ...c, confirm: e.target.value }))}
                placeholder="Repeat your password"
                className={inputClass}
                required
                minLength={8}
                autoComplete="new-password"
              />
              {form.confirm.length > 0 && form.confirm !== form.password && (
                <p className="mt-1.5 text-xs text-red-300">Passwords do not match yet.</p>
              )}
            </div>
            <div>
              <label htmlFor="reg-discord-id" className="mb-1.5 block text-sm font-medium text-zinc-300">
                Discord User ID{verifyResult?.verified ? '' : ' (optional until you join the tracker)'}
              </label>
              <input
                id="reg-discord-id"
                type="text"
                inputMode="numeric"
                value={form.discordUserId}
                onChange={(e) => setForm((c) => ({ ...c, discordUserId: e.target.value.replace(/\s/g, '') }))}
                placeholder="Example: 123456789012345678"
                className={inputClass}
                required={!!verifyResult?.verified}
                minLength={verifyResult?.verified ? 17 : undefined}
                maxLength={20}
              />
              <p className="mt-1.5 text-xs text-mist">Use Discord's Developer Mode, then right-click your profile and choose Copy User ID. This is what <span className="font-mono text-zinc-300">/playtime</span> uses.</p>
            </div>
            <button type="submit" disabled={busy} className={primaryBtnClass}>
              <UserPlus className="h-4 w-4" />
              {busy ? 'Creating account...' : 'Create Account'}
            </button>
          </form>
        </>
      )}
    </div>
  );
}
