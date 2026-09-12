import { useEffect, useRef, useState } from 'react';
import {
  Bell,
  ChevronDown,
  CircleUser,
  LogIn,
  LogOut,
  Palette,
  Shield,
  ShoppingCart,
  User,
  Wallet
} from 'lucide-react';

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
        className="block h-8 w-8 shrink-0 overflow-hidden rounded-full border border-zinc-600/60 transition hover:border-zinc-400"
      >
        {avatarUrl ? (
          <img src={avatarUrl} alt="My avatar" className="h-full w-full object-cover" />
        ) : (
          <span className="grid h-full w-full place-items-center bg-panelSoft">
            <CircleUser className="h-5 w-5 text-zinc-400" />
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

function NavDropdown({ label, items }) {
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
    document.addEventListener('mousedown', onDocMouseDown);
    document.addEventListener('keydown', onKeyDown);
    return () => {
      document.removeEventListener('mousedown', onDocMouseDown);
      document.removeEventListener('keydown', onKeyDown);
    };
  }, [open]);

  return (
    <div className="relative" ref={ref}>
      <button
        type="button"
        onClick={() => setOpen((o) => !o)}
        aria-haspopup="menu"
        aria-expanded={open}
        className="inline-flex items-center gap-1 text-[13px] font-medium text-zinc-300 transition hover:text-zinc-50"
      >
        {label}
        <ChevronDown className={`h-3.5 w-3.5 text-zinc-500 transition ${open ? 'rotate-180' : ''}`} />
      </button>
      {open && (
        <div
          role="menu"
          className="animate-pop absolute left-0 top-full z-50 mt-3 w-44 overflow-hidden rounded-lg border border-zinc-700/60 bg-[#151519] py-1 shadow-2xl"
        >
          {items.map((item) => (
            <button
              key={item.label}
              type="button"
              role="menuitem"
              onClick={() => {
                setOpen(false);
                item.onClick();
              }}
              className="flex w-full items-center gap-2 px-3.5 py-2 text-left text-[13px] text-zinc-300 transition hover:bg-zinc-700/40 hover:text-zinc-50"
            >
              {item.icon && <item.icon className="h-3.5 w-3.5 text-zinc-500" />}
              {item.label}
            </button>
          ))}
        </div>
      )}
    </div>
  );
}

/**
 * Shared global navbar styled after the reference design: slim dark bar with a
 * square logo tile, inline nav links (Tools has a dropdown), and a right-side
 * cluster with balance pill, currency/language selectors, icon buttons and the
 * round account button. Page-specific action buttons are passed as children and
 * render to the left of the profile menu.
 */
export default function Header({ user, avatarUrl, isAdmin, onSignIn, onLogout, children }) {
  const go = (hash) => () => {
    window.location.hash = hash;
  };

  return (
    <header className="sticky top-0 z-20 border-b border-zinc-800/80 bg-[#101014]">
      <div className="mx-auto flex h-14 max-w-[1600px] items-center gap-6 px-4">
        {/* Logo: square tile */}
        <button
          type="button"
          onClick={() => { window.location.hash = ''; }}
          className="grid h-9 w-9 shrink-0 place-items-center rounded-lg bg-zinc-800 text-zinc-50 transition hover:bg-zinc-700"
          title="Home"
        >
          <span className="text-lg font-black leading-none">f</span>
        </button>

        {/* Primary nav */}
        <nav className="hidden min-w-0 items-center gap-7 md:flex">
          <button
            type="button"
            onClick={() => { window.location.hash = ''; }}
            className="text-[13px] font-medium text-zinc-300 transition hover:text-zinc-50"
          >
            Market
          </button>
          <button
            type="button"
            onClick={() => { window.location.hash = ''; }}
            className="text-[13px] font-medium text-zinc-300 transition hover:text-zinc-50"
          >
            Database
          </button>
          <button
            type="button"
            onClick={() => { window.location.hash = ''; }}
            className="text-[13px] font-medium text-zinc-300 transition hover:text-zinc-50"
          >
            Loadout
          </button>
          <NavDropdown
            label="Tools"
            items={[
              { label: 'Tournaments', icon: User, onClick: go('tournaments') },
              { label: 'Tracker', onClick: go('') },
              ...(isAdmin ? [{ label: 'Admin Panel', icon: Shield, onClick: go('admin-users') }] : [])
            ]}
          />
        </nav>

        {/* Mobile fallback for tournaments (nav hidden below md) */}
        <button
          type="button"
          onClick={() => { window.location.hash = 'tournaments'; }}
          className="text-[13px] font-medium text-zinc-300 transition hover:text-zinc-50 md:hidden"
        >
          Tournaments
        </button>

        {/* Right cluster */}
        <div className="ml-auto flex min-w-0 items-center gap-3">
          {/* Page-specific actions (join requests, check now, ...) */}
          <div className="hidden min-w-0 items-center gap-2 lg:flex">
            {children}
          </div>

          {/* Balance pill */}
          <div className="hidden items-center sm:flex">
            <span className="inline-flex h-8 items-center gap-1.5 rounded-md bg-zinc-800/80 px-3 text-[13px] font-bold text-zinc-50">
              <Wallet className="h-3.5 w-3.5 text-zinc-400" />
              $8.08
            </span>
          </div>

          {/* Currency selector */}
          <button
            type="button"
            className="hidden items-center gap-1 text-[13px] font-bold text-zinc-50 transition hover:text-zinc-300 sm:inline-flex"
            title="Currency"
          >
            USD
            <ChevronDown className="h-3 w-3 text-zinc-500" />
          </button>

          {/* Language selector */}
          <button
            type="button"
            className="hidden items-center gap-1 text-[13px] font-bold text-zinc-50 transition hover:text-zinc-300 sm:inline-flex"
            title="Language"
          >
            EN
            <ChevronDown className="h-3 w-3 text-zinc-500" />
          </button>

          {/* Icon buttons */}
          <button
            type="button"
            className="grid h-8 w-8 place-items-center rounded-md text-zinc-300 transition hover:bg-zinc-800 hover:text-zinc-50"
            title="Cart"
          >
            <ShoppingCart className="h-[18px] w-[18px]" />
          </button>
          <button
            type="button"
            className="grid h-8 w-8 place-items-center rounded-md text-zinc-300 transition hover:bg-zinc-800 hover:text-zinc-50"
            title="Notifications"
          >
            <Bell className="h-[18px] w-[18px]" />
          </button>
          <button
            type="button"
            className="grid h-8 w-8 place-items-center rounded-md text-zinc-300 transition hover:bg-zinc-800 hover:text-zinc-50"
            title="Theme"
          >
            <Palette className="h-[18px] w-[18px]" />
          </button>

          {/* Account */}
          {user ? (
            <ProfileMenu user={user} avatarUrl={avatarUrl} isAdmin={isAdmin} onLogout={onLogout} />
          ) : (
            <button
              type="button"
              onClick={onSignIn}
              className="inline-flex h-8 items-center justify-center gap-2 rounded-md bg-zinc-800 px-3 text-[13px] font-semibold text-zinc-50 transition hover:bg-zinc-700"
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
