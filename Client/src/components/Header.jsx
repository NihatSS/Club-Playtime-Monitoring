import { useEffect, useRef, useState } from 'react';
import { CircleUser, Gamepad2, LogIn, LogOut, Shield, Swords, User } from 'lucide-react';

function MenuItem({ icon: Icon, label, onClick, danger = false }) {
  return (
    <button
      type="button"
      role="menuitem"
      onClick={onClick}
      className={`flex w-full items-center gap-2.5 px-4 py-2.5 text-sm font-medium transition ${
        danger ? 'text-zinc-300 hover:bg-red-400/10 hover:text-red-300' : 'text-zinc-200 hover:bg-neon-cyan/[0.06] hover:text-zinc-50'
      }`}
    >
      <Icon className={`h-4 w-4 ${danger ? 'text-zinc-500' : 'text-mist'}`} />
      {label}
    </button>
  );
}

/**
 * Circular account button + dropdown. One instance per page, mounted only by
 * the shared Header. Closes on outside click, Escape, and navigation.
 */
function ProfileMenu({ user, avatarUrl, isAdmin, onLogout }) {
  const [open, setOpen] = useState(false);
  const ref = useRef(null);

  useEffect(() => {
    if (!open) return;
    function onDocMouseDown(e) {
      if (ref.current && !ref.current.contains(e.target)) setOpen(false);
    }
    function onKeyDown(e) {
      if (e.key === 'Escape') setOpen(false);
    }
    function onHashChange() {
      setOpen(false);
    }
    document.addEventListener('mousedown', onDocMouseDown);
    document.addEventListener('keydown', onKeyDown);
    window.addEventListener('hashchange', onHashChange);
    return () => {
      document.removeEventListener('mousedown', onDocMouseDown);
      document.removeEventListener('keydown', onKeyDown);
      window.removeEventListener('hashchange', onHashChange);
    };
  }, [open]);

  // Signed out — nothing to show (Header renders the Sign In button instead).
  if (!user) return null;

  function go(hash) {
    setOpen(false);
    window.location.hash = hash;
  }

  return (
    <div className="relative" ref={ref}>
      <button
        type="button"
        onClick={() => setOpen((o) => !o)}
        aria-haspopup="menu"
        aria-expanded={open}
        title="Account menu"
        className={`grid h-10 w-10 shrink-0 place-items-center overflow-hidden rounded-full border transition ${
          open
            ? 'border-neon-cyan/60 shadow-[0_0_14px_rgba(0,229,255,0.35)]'
            : 'border-neon-cyan/25 hover:border-neon-cyan/50 hover:shadow-[0_0_10px_rgba(0,229,255,0.2)]'
        }`}
      >
        {avatarUrl ? (
          <img src={avatarUrl} alt="My avatar" className="h-full w-full object-cover" />
        ) : (
          <span className="grid h-full w-full place-items-center bg-panelSoft">
            <CircleUser className="h-6 w-6 text-neon-cyan/80" />
          </span>
        )}
      </button>

      {open && (
        <div
          role="menu"
          className="animate-pop absolute right-0 top-full z-50 mt-2 w-56 overflow-hidden rounded-xl border border-neon-cyan/[0.12] bg-[#151519] shadow-2xl"
        >
          {/* Identity block */}
          <div className="flex items-center gap-3 px-4 py-3">
            {avatarUrl ? (
              <img src={avatarUrl} alt="" className="h-9 w-9 shrink-0 rounded-full border border-neon-cyan/25 object-cover" />
            ) : (
              <span className="grid h-9 w-9 shrink-0 place-items-center rounded-full border border-neon-cyan/25 bg-panelSoft">
                <CircleUser className="h-5 w-5 text-neon-cyan/80" />
              </span>
            )}
            <div className="min-w-0">
              <div className="truncate text-sm font-semibold text-zinc-50">{user.username}</div>
              <div className={`text-[10px] font-semibold uppercase tracking-wider ${isAdmin ? 'text-neon-green' : 'text-zinc-500'}`}>
                {isAdmin ? 'Admin' : 'User'}
              </div>
            </div>
          </div>

          <div className="h-px bg-neon-cyan/10" />

          <MenuItem icon={User} label="Profile" onClick={() => go('profile')} />
          {isAdmin && <MenuItem icon={Shield} label="Admin Panel" onClick={() => go('admin-users')} />}

          <div className="h-px bg-neon-cyan/10" />

          <MenuItem
            icon={LogOut}
            label="Logout"
            danger
            onClick={() => {
              setOpen(false);
              onLogout?.();
            }}
          />
        </div>
      )}
    </div>
  );
}

/**
 * Shared global header used by every normal user-facing page so the account
 * button appears consistently. Page-specific action buttons are passed as
 * children and render to the left of the profile menu.
 */
export default function Header({ user, avatarUrl, isAdmin, onSignIn, onLogout, children }) {
  return (
    <header className="sticky top-0 z-20 border-b border-neon-cyan/[0.08] bg-[#050510]/90 backdrop-blur-md">
      <div className="mx-auto flex max-w-[1400px] flex-wrap items-center justify-between gap-x-4 gap-y-2 px-5 py-3">
        <div className="flex items-center gap-4">
          <button
            type="button"
            onClick={() => { window.location.hash = ''; }}
            className="flex items-center gap-3 text-left"
            title="Home"
          >
            <span className="grid h-9 w-9 place-items-center rounded-lg bg-neon-cyan text-zinc-950">
              <Gamepad2 className="h-5 w-5" />
            </span>
            <span>
              <span className="block text-base font-bold tracking-tight text-zinc-50">Club Playtime</span>
              <span className="block text-[11px] text-mist">Racket Rivals tracker</span>
            </span>
          </button>
          <button
            type="button"
            onClick={() => { window.location.hash = 'tournaments'; }}
            className="inline-flex h-9 items-center gap-1.5 rounded-lg border border-neon-cyan/[0.08] px-3 text-sm font-medium text-zinc-300 transition hover:bg-neon-cyan/[0.06] hover:text-zinc-100"
          >
            <Swords className="h-4 w-4 text-neon-purple" />
            Tournaments
          </button>
        </div>

        <div className="flex flex-wrap items-center justify-end gap-2">
          {children}
          {user ? (
            <ProfileMenu user={user} avatarUrl={avatarUrl} isAdmin={isAdmin} onLogout={onLogout} />
          ) : (
            <button
              type="button"
              onClick={onSignIn}
              className="inline-flex h-9 items-center justify-center gap-2 rounded-lg bg-neon-cyan px-3 text-sm font-semibold text-zinc-950 transition hover:bg-neon-cyan/80"
            >
              <LogIn className="h-4 w-4" />
              Sign In
            </button>
          )}
        </div>
      </div>
    </header>
  );
}
