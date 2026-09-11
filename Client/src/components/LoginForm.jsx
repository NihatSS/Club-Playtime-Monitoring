import { useState } from 'react';
import { Gamepad2, LogIn, AlertCircle, Eye, EyeOff } from 'lucide-react';
import { api } from '../lib/api';

export default function LoginForm({ onLogin, onSwitchToRegister }) {
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);

  async function handleSubmit(event) {
    event.preventDefault();
    setBusy(true);
    setError('');

    try {
      const data = await api.login(username, password);
      api.setAuth(data.token, data.role, data.username, data.discordUserId);
      onLogin(data);
    } catch (err) {
      setError(err.message);
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="rounded-xl border border-line bg-panel p-8 shadow-glow">
      {/* Logo */}
      <div className="flex flex-col items-center mb-8">
        <div className="grid h-14 w-14 place-items-center rounded-xl bg-emerald-400 text-zinc-950 mb-4">
          <Gamepad2 className="h-7 w-7" />
        </div>
        <h1 className="text-2xl font-bold text-zinc-50">Welcome back</h1>
        <p className="mt-1 text-sm text-mist">Sign in to your account</p>
      </div>

      {/* Error */}
      {error && (
        <div className="mb-4 flex items-center gap-2 rounded-lg border border-red-400/30 bg-red-400/10 px-4 py-3 text-sm text-red-100">
          <AlertCircle className="h-4 w-4 shrink-0" />
          {error}
        </div>
      )}

      {/* Form */}
      <form onSubmit={handleSubmit} className="space-y-4">
        <div>
          <label htmlFor="username" className="block text-sm font-medium text-zinc-300 mb-1.5">
            Username
          </label>
          <input
            id="username"
            type="text"
            value={username}
            onChange={(e) => setUsername(e.target.value)}
            placeholder="Enter username"
            className="w-full min-h-11 rounded-lg border border-line bg-ink px-4 text-sm text-zinc-50 placeholder:text-zinc-500 focus:border-emerald-400/50 focus:outline-none focus:ring-1 focus:ring-emerald-400/50"
            required
            autoComplete="username"
            autoFocus
          />
        </div>

        <div>
          <label htmlFor="password" className="block text-sm font-medium text-zinc-300 mb-1.5">
            Password
          </label>
          <div className="relative">
            <input
              id="password"
              type={showPassword ? 'text' : 'password'}
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              placeholder="Enter password"
              className="w-full min-h-11 rounded-lg border border-line bg-ink px-4 pr-11 text-sm text-zinc-50 placeholder:text-zinc-500 focus:border-emerald-400/50 focus:outline-none focus:ring-1 focus:ring-emerald-400/50"
              required
              autoComplete="current-password"
            />
            <button
              type="button"
              onClick={() => setShowPassword(!showPassword)}
              className="absolute right-3 top-1/2 -translate-y-1/2 text-zinc-400 hover:text-zinc-200 transition"
              tabIndex={-1}
            >
              {showPassword ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
            </button>
          </div>
        </div>

        <button
          type="submit"
          disabled={busy}
          className="w-full min-h-11 flex items-center justify-center gap-2 rounded-lg bg-emerald-400 px-4 text-sm font-semibold text-zinc-950 transition hover:bg-emerald-300 disabled:cursor-wait disabled:opacity-60"
        >
          <LogIn className="h-4 w-4" />
          {busy ? 'Logging in...' : 'Login'}
        </button>
      </form>

      {/* Switch to register */}
      {onSwitchToRegister && (
        <div className="mt-6 border-t border-line pt-4 text-center">
          <p className="text-sm text-mist">
            Don't have an account?{' '}
            <button
              type="button"
              onClick={onSwitchToRegister}
              className="font-semibold text-neon-cyan hover:text-neon-cyan/80 transition"
            >
              Register
            </button>
          </p>
        </div>
      )}
    </div>
  );
}
