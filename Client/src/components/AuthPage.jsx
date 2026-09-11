import { useState } from 'react';
import { Gamepad2 } from 'lucide-react';
import LoginForm from './LoginForm';
import RegisterFlow from './RegisterFlow';

export default function AuthPage({ onLogin }) {
  const [mode, setMode] = useState('login');

  return (
    <div className="min-h-screen bg-[#050510] text-zinc-50">
      <header className="sticky top-0 z-20 border-b border-neon-cyan/[0.08] bg-[#050510]/90 backdrop-blur-md">
        <div className="mx-auto flex max-w-md items-center justify-between px-5 py-3">
          <div className="flex items-center gap-3">
            <div className="grid h-9 w-9 place-items-center rounded-lg bg-neon-cyan text-zinc-950">
              <Gamepad2 className="h-5 w-5" />
            </div>
            <div>
              <h1 className="text-base font-bold tracking-tight text-zinc-50">Club Playtime</h1>
              <div className="text-[11px] text-mist">Racket Rivals tracker</div>
            </div>
          </div>
          <button
            type="button"
            onClick={() => setMode((m) => (m === 'login' ? 'register' : 'login'))}
            className="inline-flex h-9 items-center gap-1.5 rounded-lg border border-neon-cyan/[0.08] px-3 text-sm font-medium text-zinc-300 transition hover:bg-neon-cyan/[0.06]"
          >
            {mode === 'login' ? 'Register' : 'Back to login'}
          </button>
        </div>
      </header>

      <main className="mx-auto max-w-md px-5 py-8">
        {mode === 'login' ? (
          <LoginForm onLogin={onLogin} onSwitchToRegister={() => setMode('register')} />
        ) : (
          <RegisterFlow onLogin={onLogin} onSwitchToLogin={() => setMode('login')} />
        )}
      </main>
    </div>
  );
}
