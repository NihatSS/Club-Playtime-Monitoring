import { useEffect, useRef, useState } from 'react';
import { api } from '../lib/api.js';
import {
  Bell,
  ChevronDown,
  CircleUser,
  Clock,
  Gamepad2,
  Home,
  LayoutGrid,
  LogIn,
  LogOut,
  Megaphone,
  Shield,
  Swords,
  Trophy,
  User
} from 'lucide-react';

const NOTIFICATIONS_SEEN_KEY = 'notificationsLastSeenAt';

function relativeTime(iso, now = Date.now()) {
  const diff = now - new Date(iso).getTime();
  const past = diff >= 0;
  const mins = Math.floor(Math.abs(diff) / 60000);
  let text;
  if (mins < 1) text = 'just now';
  else if (mins < 60) text = `${mins}m`;
  else if (mins < 1440) text = `${Math.floor(mins / 60)}h`;
  else if (mins < 43200) text = `${Math.floor(mins / 1440)}d`;
  else text = new Date(iso).toLocaleDateString();
  if (!past) return text === 'just now' ? text : `in ${text}`;
  return text === 'just now' ? text : `${text} ago`;
}

const TONE_STYLES = {
  cyan: 'bg-neon-cyan/10 text-neon-cyan',
  green: 'bg-emerald-400/10 text-emerald-300',
  amber: 'bg-amber-400/10 text-amber-300',
  purple: 'bg-neon-purple/15 text-neon-purple',
  zinc: 'bg-zinc-700/40 text-zinc-300'
};

/**
 * Builds notification items from live tournament data: new tournaments,
 * open/closing registration, starting soon, live brackets and recent winners.
 * Returns { id, icon, tone, title, body, time, href } sorted newest first.
 */
export function buildNotifications(tournaments, now = Date.now()) {
  const items = [];
  const days = (ms) => ms / 86400000;

  for (const t of tournaments ?? []) {
    const href = `tournaments/${t.id}`;
    const created = new Date(t.createdAt);
    const startsAt = new Date(t.startsAt);
    const deadline = new Date(t.registrationDeadline);

    if (t.status === 'CANCELLED') continue;

    if (t.status === 'COMPLETED') {
      if (t.winnerName && t.completedAt && days(now - new Date(t.completedAt)) < 3) {
        items.push({
          id: `t-${t.id}-winner`,
          icon: Trophy,
          tone: 'amber',
          title: `${t.winnerName} won "${t.name}"`,
          body: 'Tournament finished — check the final bracket.',
          time: t.completedAt,
          href
        });
      }
      continue;
    }

    if (t.status === 'IN_PROGRESS') {
      items.push({
        id: `t-${t.id}-live`,
        icon: Swords,
        tone: 'green',
        title: `"${t.name}" is live`,
        body: `${t.participantCount} participants — bracket in progress.`,
        time: t.startsAt,
        href
      });
      continue;
    }

    // Recently created tournament (any pre-game state).
    if (days(now - created) < 7) {
      items.push({
        id: `t-${t.id}-new`,
        icon: Megaphone,
        tone: 'cyan',
        title: `New tournament: ${t.name}`,
        body: t.status === 'REGISTRATION_OPEN'
          ? `${t.participantCount}/${t.maxParticipants} joined — register before it fills up.`
          : t.prizeInfo ? `Prizes: ${t.prizeInfo}` : 'Take a look and sign up.',
        time: t.createdAt,
        href
      });
    }

    if (t.status === 'REGISTRATION_OPEN' && deadline > now) {
      const daysLeft = days(deadline - now);
      items.push({
        id: `t-${t.id}-reg`,
        icon: Clock,
        tone: daysLeft <= 1 ? 'amber' : 'zinc',
        title: `Registration closes soon: ${t.name}`,
        body: `${t.participantCount}/${t.maxParticipants} joined — closes ${relativeTime(t.registrationDeadline, now)}.`,
        time: (deadline - now) < 86400000 ? t.registrationDeadline : t.createdAt,
        href
      });
    }

    if ((t.status === 'UPCOMING' || t.status === 'REGISTRATION_CLOSED') && startsAt > now && days(startsAt - now) < 3) {
      items.push({
        id: `t-${t.id}-start`,
        icon: Gamepad2,
        tone: 'cyan',
        title: `"${t.name}" starts soon`,
        body: `Begins ${relativeTime(t.startsAt, now)} — good luck!`,
        time: t.startsAt,
        href
      });
    }
  }

  return items
    .sort((a, b) => new Date(b.time) - new Date(a.time))
    .slice(0, 12);
}

function NotificationItem({ item, now, onNavigate, onDelete }) {
  const Icon = item.icon;
  return (
    <button
      type="button"
      onClick={() => {
        if (item.href) window.location.hash = item.href;
        onNavigate?.();
      }}
      className="group flex w-full items-start gap-3 px-4 py-3 text-left transition hover:bg-neon-cyan/[0.05]"
    >
      <span className={`mt-0.5 grid h-8 w-8 shrink-0 place-items-center rounded-lg ${TONE_STYLES[item.tone] ?? TONE_STYLES.zinc}`}>
        <Icon className="h-4 w-4" />
      </span>
      <span className="min-w-0">
        <span className="block truncate text-[13px] font-semibold text-zinc-50">{item.title}</span>
        <span className="mt-0.5 block text-xs leading-snug text-mist">{item.body}</span>
        <span className="mt-1 block text-[10px] uppercase tracking-wider text-zinc-500">{relativeTime(item.time, now)}</span>
      </span>
      {onDelete && (
        <span
          role="button"
          tabIndex={-1}
          title="Delete announcement"
          onClick={(e) => {
            e.stopPropagation();
            onDelete();
          }}
          className="ml-auto hidden h-6 w-6 shrink-0 place-items-center rounded text-zinc-500 transition hover:bg-red-400/10 hover:text-red-300 group-hover:grid"
        >
          <X className="h-3.5 w-3.5" />
        </span>
      )}
    </button>
  );
}

/** Bell button + notifications dropdown, fed by live tournament data and admin announcements. */
function NotificationBell({ isAdmin }) {
  const [open, setOpen] = useState(false);
  const [items, setItems] = useState([]);
  const [seenAt, setSeenAt] = useState(() => Number(localStorage.getItem(NOTIFICATIONS_SEEN_KEY)) || 0);
  const ref = useRef(null);

  useEffect(() => {
    let cancelled = false;
    Promise.all([api.tournaments(), api.announcements(12).catch(() => [])])
      .then(([tournaments, announcements]) => {
        if (cancelled) return;
        const tournamentItems = buildNotifications(tournaments);
        // Admin announcements are pinned first (newest first within the group).
        const announcementItems = (announcements ?? []).map((a) => ({
          id: `a-${a.id}`,
          kind: 'announcement',
          icon: Megaphone,
          tone: 'purple',
          title: a.title,
          body: a.body || (a.linkUrl ? 'Tap to open.' : ''),
          time: a.createdAt,
          href: a.linkUrl || null,
          announcementId: a.id
        }));
        setItems([...announcementItems, ...tournamentItems].slice(0, 14));
      })
      .catch(() => {});
    return () => { cancelled = true; };
  }, [open]);

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

  // Unread = something happened (or was created) after the last time the
  // dropdown was opened. Future-scheduled items ("starts soon") don't keep
  // the dot lit forever — only events that already occurred count.
  const [deletingId, setDeletingId] = useState(null);

  const hasUnread = items.some((it) => {
    const ts = new Date(it.time).getTime();
    return ts <= now && ts > seenAt;
  });
  const now = Date.now();

  function toggle() {
    setOpen((o) => {
      const next = !o;
      if (next) {
        const ts = Date.now();
        localStorage.setItem(NOTIFICATIONS_SEEN_KEY, String(ts));
        setSeenAt(ts);
      }
      return next;
    });
  }

  return (
    <div className="relative shrink-0" ref={ref}>
      <button
        type="button"
        onClick={toggle}
        aria-haspopup="menu"
        aria-expanded={open}
        title="Notifications"
        className={`relative grid h-9 w-9 place-items-center rounded-lg transition ${
          open ? 'bg-white/[0.06] text-zinc-50' : 'text-zinc-300 hover:bg-white/[0.04] hover:text-zinc-50'
        }`}
      >
        <Bell className="h-[18px] w-[18px]" />
        {hasUnread && <span className="absolute right-1.5 top-1.5 h-2 w-2 rounded-full bg-red-500" />}
      </button>

      {open && (
        <div
          role="menu"
          className="animate-pop absolute right-0 top-full z-50 mt-2 w-80 overflow-hidden rounded-xl border border-neon-cyan/[0.12] bg-[#151519] shadow-2xl"
        >
          <div className="flex items-center justify-between px-4 py-3">
            <span className="text-sm font-semibold text-zinc-50">Notifications</span>
            {items.length > 0 && (
              <span className="rounded-full bg-neon-cyan/10 px-2 py-0.5 text-[10px] font-bold text-neon-cyan">{items.length}</span>
            )}
          </div>
          <div className="h-px bg-neon-cyan/10" />
          <div className="max-h-96 overflow-y-auto">
            {items.length === 0 ? (
              <div className="flex flex-col items-center gap-2 px-4 py-8 text-center">
                <Bell className="h-6 w-6 text-zinc-600" />
                <span className="text-[13px] font-medium text-zinc-400">You're all caught up</span>
                <span className="text-xs text-zinc-600">Tournament news and events will show up here.</span>
              </div>
            ) : (
              items.map((item) => (
                <NotificationItem
                  key={item.id}
                  item={item}
                  now={now}
                  onNavigate={() => setOpen(false)}
                  onDelete={isAdmin && item.kind === 'announcement'
                    ? async () => {
                        setDeletingId(item.id);
                        try {
                          await api.deleteAnnouncement(item.announcementId);
                          setItems((list) => list.filter((it) => it.id !== item.id));
                        } catch {
                          // Leave the item in place if the delete failed.
                        } finally {
                          setDeletingId(null);
                        }
                      }
                    : undefined}
                />
              ))
            )}
          </div>
          <div className="h-px bg-neon-cyan/10" />
          {isAdmin && <AnnouncementComposer onPosted={() => { setOpen(false); setOpen(true); }} />}
          {isAdmin && <div className="h-px bg-neon-cyan/10" />}
          <button
            type="button"
            onClick={() => {
              setOpen(false);
              window.location.hash = 'tournaments';
            }}
            className="block w-full px-4 py-2.5 text-center text-xs font-semibold text-neon-cyan transition hover:bg-neon-cyan/[0.05]"
          >
            View tournaments
          </button>
        </div>
      )}
    </div>
  );
}

/** Admin-only inline form to post a new announcement. */
function AnnouncementComposer({ onPosted }) {
  const [expanded, setExpanded] = useState(false);
  const [title, setTitle] = useState('');
  const [body, setBody] = useState('');
  const [linkUrl, setLinkUrl] = useState('');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');

  if (!expanded) {
    return (
      <button
        type="button"
        onClick={() => setExpanded(true)}
        className="block w-full px-4 py-2.5 text-center text-xs font-semibold text-neon-purple transition hover:bg-neon-purple/[0.06]"
      >
        + Post announcement
      </button>
    );
  }

  async function submit(e) {
    e.preventDefault();
    if (!title.trim() || busy) return;
    setBusy(true);
    setError('');
    try {
      await api.createAnnouncement({ title: title.trim(), body: body.trim(), linkUrl: linkUrl.trim() });
      setTitle('');
      setBody('');
      setLinkUrl('');
      setExpanded(false);
      onPosted?.();
    } catch (err) {
      setError(err.message || 'Failed to post.');
    } finally {
      setBusy(false);
    }
  }

  return (
    <form onSubmit={submit} className="space-y-2 px-4 py-3">
      <input
        value={title}
        onChange={(e) => setTitle(e.target.value)}
        placeholder="Title (required)"
        maxLength={120}
        className="w-full rounded-md border border-neon-cyan/[0.12] bg-ink px-2.5 py-1.5 text-xs text-zinc-50 placeholder:text-zinc-500 focus:border-neon-purple/40 transition"
      />
      <textarea
        value={body}
        onChange={(e) => setBody(e.target.value)}
        placeholder="Message (optional)"
        rows={2}
        maxLength={500}
        className="w-full resize-none rounded-md border border-neon-cyan/[0.12] bg-ink px-2.5 py-1.5 text-xs text-zinc-50 placeholder:text-zinc-500 focus:border-neon-purple/40 transition"
      />
      <input
        value={linkUrl}
        onChange={(e) => setLinkUrl(e.target.value)}
        placeholder="Link, e.g. tournaments/3 (optional)"
        maxLength={300}
        className="w-full rounded-md border border-neon-cyan/[0.12] bg-ink px-2.5 py-1.5 text-xs text-zinc-50 placeholder:text-zinc-500 focus:border-neon-purple/40 transition"
      />
      {error && <div className="text-[11px] text-red-300">{error}</div>}
      <div className="flex items-center gap-2">
        <button
          type="submit"
          disabled={busy || !title.trim()}
          className="rounded-md bg-neon-purple px-3 py-1.5 text-xs font-bold text-white transition hover:bg-neon-purple/80 disabled:opacity-50"
        >
          {busy ? 'Posting…' : 'Post'}
        </button>
        <button
          type="button"
          onClick={() => { setExpanded(false); setError(''); }}
          className="rounded-md border border-zinc-700/60 px-3 py-1.5 text-xs font-medium text-zinc-400 transition hover:text-zinc-200"
        >
          Cancel
        </button>
      </div>
    </form>
  );
}

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
          <NotificationBell isAdmin={isAdmin} />

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
