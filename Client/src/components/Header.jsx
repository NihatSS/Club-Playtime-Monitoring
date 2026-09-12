import { useEffect, useRef, useState } from 'react';
import {
  Bell,
  ChevronDown,
  CircleUser,
  Gamepad2,
  Home,
  LayoutGrid,
  LogIn,
  LogOut,
  Shield,
  Trophy,
  User
} from 'lucide-react';

/** Tracks the current hash route so nav links can highlight the active page. */
function useHashRoute() {
  const [route, setRoute] = useState(() => window.location.hash.replace(/^#\/?/, ''));
  useEffect(() => {
    function onHashChange() {
      setRoute(window.location.hash.replace(/^#\/?/, ''));
    }
    window.addEventListener('hashchange', onHashChange);
    return () => window.removeEventListener('hashchange', onHashChange);
  }, []);
  return route;
}

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
      {/* Account pill: avatar + username + role badge + chevron */}
      <button
        type="button"
        onClick={() => setOpen((o) => !o)}
        aria-haspopup="menu"
        aria-expanded={open}
        title="Account menu"
        className="flex items-center gap-2.5 rounded-full border border-transparent py-1 pl-1 pr-2 transition hover:border-zinc-700/80 hover:bg-white/[0.03]"
      >
        {avatarUrl ? (
          <img
            src={avatarUrl}
            alt="My avatar"
            className="h-9 w-9 shrink-0 rounded-full object-cover ring-2 ring-neon-cyan/30"
          />
        ) : (
          <span className="grid h-9 w-9 shrink-0 place-items-center rounded-full bg-panelSoft ring-2 ring-neon-cyan/30">
            <CircleUser className="h-5 w-5 text-zinc-400" />
          </span>
        )}
        <span className="hidden min-w-0 flex-col items-start leading-tight sm:flex">
          <span className="max-w-[140px] truncate text-[13px] font-semibold text-zinc-50">{user.username}</span>
          {isAdmin ? (
            <span className="rounded bg-neon-cyan/15 px-1.5 text-[9px] font-bold uppercase tracking-widest text-neon-cyan">
              Admin
            </span>
          ) : (
            <span className="px-1.5 text-[9px] font-bold uppercase tracking-widest text-mist">User</span>
          )}
        </span>
        <ChevronDown className={`h-4 w-4 shrink-0 text-zinc-500 transition ${open ? 'rotate-180' : ''}`} />
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
              <div className={`text-[10px] font-semibold uppercase tracking-wider ${isAdmin ? 'text-neon-cyan' : 'text-zinc-500'}`}>
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

/** Centered nav link with icon; the active page gets a cyan highlight + underline. */
function NavLink({ icon: Icon, label, active = false, onClick, iconOnly = false }) {
  return (
    <button
      type="button"
      onClick={onClick}
      title={label}
      aria-current={active ? 'page' : undefined}
      className={`relative inline-flex items-center gap-2 rounded-lg border px-3.5 py-2 text-[13px] font-semibold transition ${
        active
          ? 'border-neon-cyan/30 bg-neon-cyan/[0.07] text-neon-cyan'
          : 'border-transparent text-zinc-300 hover:bg-white/[0.03] hover:text-zinc-50'
      } ${iconOnly ? 'px-2.5' : ''}`}
    >
      <Icon className="h-4 w-4 shrink-0" />
      {!iconOnly && label}
      {active && (
        <span className="absolute inset-x-3 -bottom-3.5 h-[2px] rounded-full bg-neon-cyan shadow-[0_0_8px_rgba(0,229,255,0.9)]" />
      )}
    </button>
  );
}

function NavDropdown({ icon: Icon, label, items, active = false }) {
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
        className={`inline-flex items-center gap-2 rounded-lg border px-3.5 py-2 text-[13px] font-semibold transition ${
          active || open
            ? 'border-neon-cyan/30 bg-neon-cyan/[0.07] text-neon-cyan'
            : 'border-transparent text-zinc-300 hover:bg-white/[0.03] hover:text-zinc-50'
        }`}
      >
        <Icon className="h-4 w-4 shrink-0" />
        {label}
        <ChevronDown className={`h-3.5 w-3.5 text-zinc-500 transition ${open ? 'rotate-180' : ''}`} />
      </button>
      {open && (
        <div
          role="menu"
          className="animate-pop absolute left-0 top-full z-50 mt-3 w-48 overflow-hidden rounded-lg border border-zinc-700/60 bg-[#151519] py-1 shadow-2xl"
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
 * Shared global navbar styled after the reference design: cyan gamepad logo
 * tile with the brand name, centered icon nav (Home, Tournaments, More) with
 * an active-page highlight, and a right cluster with page actions (join
 * requests, ...), notification bell and the account pill. Page-specific
 * action buttons are passed as children.
 */
export default function Header({ user, avatarUrl, isAdmin, onSignIn, onLogout, children }) {
  const route = useHashRoute();

  const go = (hash) => () => {
    window.location.hash = hash;
  };

  const isHome = route === '';
  const isTournaments = route === 'tournaments' || /^tournaments\/\d+$/.test(route);

  const moreItems = [
    ...(user ? [{ label: 'Profile', icon: User, onClick: go('profile') }] : []),
    ...(isAdmin ? [{ label: 'Admin Panel', icon: Shield, onClick: go('admin-users') }] : []),
    ...(!user ? [{ label: 'Sign In', icon: LogIn, onClick: onSignIn }] : [])
  ];

  return (
    <header className="sticky top-0 z-20 border-b border-neon-cyan/[0.08] bg-[#0a0a16]/95 backdrop-blur">
      <div className="mx-auto flex h-16 max-w-[1600px] items-center gap-4 px-4">
        {/* Brand: cyan gamepad tile + name */}
        <button
          type="button"
          onClick={() => { window.location.hash = ''; }}
          className="flex shrink-0 items-center gap-3"
          title="Home"
        >
          <span className="grid h-10 w-10 place-items-center rounded-xl bg-gradient-to-br from-cyan-300 to-cyan-600 shadow-[0_0_18px_rgba(0,229,255,0.35)]">
            <Gamepad2 className="h-5 w-5 text-[#04141c]" />
          </span>
          <span className="hidden flex-col items-start leading-tight sm:flex">
            <span className="text-[15px] font-bold text-zinc-50">Club Playtime</span>
            <span className="text-[11px] text-mist">Roblox Playtime Tracker</span>
          </span>
        </button>

        {/* Centered primary nav (desktop) */}
        <nav className="mx-auto hidden items-center gap-1.5 md:flex">
          <NavLink icon={Home} label="Home" active={isHome} onClick={go('')} />
          <NavLink icon={Trophy} label="Tournaments" active={isTournaments} onClick={go('tournaments')} />
          <NavDropdown icon={LayoutGrid} label="More" items={moreItems} active={route === 'profile' || route === 'admin-users'} />
        </nav>

        {/* Mobile fallback: icon-only nav (full nav hidden below md) */}
        <nav className="ml-2 flex items-center gap-1 md:hidden">
          <NavLink icon={Home} label="Home" active={isHome} onClick={go('')} iconOnly />
          <NavLink icon={Trophy} label="Tournaments" active={isTournaments} onClick={go('tournaments')} iconOnly />
          <NavDropdown icon={LayoutGrid} label="" items={moreItems} />
        </nav>

        {/* Right cluster */}
        <div className="ml-auto flex min-w-0 items-center gap-2 md:ml-0">
          {/* Page-specific actions (join requests, check now, ...) */}
          <div className="hidden min-w-0 items-center gap-2 lg:flex">
            {children}
          </div>

          {/* Notifications */}
          <button
            type="button"
            className="relative grid h-9 w-9 shrink-0 place-items-center rounded-lg text-zinc-300 transition hover:bg-white/[0.04] hover:text-zinc-50"
            title="Notifications"
          >
            <Bell className="h-[18px] w-[18px]" />
            <span className="absolute right-1.5 top-1.5 h-2 w-2 rounded-full bg-red-500" />
          </button>

          {/* Account */}
          {user ? (
            <ProfileMenu user={user} avatarUrl={avatarUrl} isAdmin={isAdmin} onLogout={onLogout} />
          ) : (
            <button
              type="button"
              onClick={onSignIn}
              className="inline-flex h-9 shrink-0 items-center justify-center gap-2 rounded-lg bg-neon-cyan px-4 text-[13px] font-bold text-zinc-950 transition hover:bg-neon-cyan/80"
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
