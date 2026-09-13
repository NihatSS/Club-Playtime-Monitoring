import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import {
  AlertCircle,
  ArrowLeft,
  BarChart3,
  CalendarDays,
  CheckCircle,
  Clock,
  Copy,
  Crown,
  ExternalLink,
  Eye,
  EyeOff,
  Flame,
  Gamepad2,
  ImagePlus,
  KeyRound,
  Link2,
  Medal,
  Pencil,
  Play,
  RotateCcw,
  Save,
  ShieldCheck,
  Square,
  Star,
  Target,
  Trash2,
  TrendingUp,
  Trophy,
  Unlink,
  Upload,
  User
} from 'lucide-react';
import { Cell, Pie, PieChart, ResponsiveContainer } from 'recharts';
import { api } from '../lib/api';
import { passwordIssues } from '../lib/password';
import { formatDateTime, formatDuration } from '../lib/format';
import Header from './Header';

const DISCORD_COPY_HINT =
  "Use Discord's Developer Mode, then right-click your profile and choose Copy User ID.";

/**
 * Website presence: derived from real tracker data.
 *  - green  = in the tracked game right now (player.IsOnline from the monitor)
 *  - blue   = active on the website in the last 10 minutes (LastSeenOnSite heartbeat)
 *  - grey   = offline
 */
function getPresence(details) {
  if (details?.currentStatus === 'Online') return 'game';
  const seen = details?.lastSeenOnSite ? new Date(details.lastSeenOnSite).getTime() : 0;
  if (seen && Date.now() - seen < 10 * 60 * 1000) return 'site';
  return 'offline';
}

const PRESENCE_STYLES = {
  game: { dot: 'bg-emerald-400 shadow-[0_0_6px_rgba(52,211,153,0.9)]', label: 'In game', text: 'text-emerald-300' },
  site: { dot: 'bg-sky-400 shadow-[0_0_6px_rgba(56,189,248,0.9)]', label: 'On the website', text: 'text-sky-300' },
  offline: { dot: 'bg-zinc-500', label: 'Offline', text: 'text-mist' }
};

function PresenceDot({ details, size = 'h-4 w-4', border = 'border-2 border-panel' }) {
  const presence = getPresence(details);
  const style = PRESENCE_STYLES[presence];
  return (
    <span
      className={`absolute -bottom-0.5 -right-0.5 rounded-full ${size} ${border} ${style.dot}`}
      title={style.label}
    />
  );
}

export function PasswordChecklist({ password }) {
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

function Card({ icon: Icon, title, accent = 'text-neon-cyan', action = null, children, className = '' }) {
  return (
    <section className={`rounded-xl border border-line bg-panel shadow-glow ${className}`}>
      <div className="flex items-center gap-2 border-b border-line/60 px-5 py-3.5">
        {Icon && <Icon className={`h-5 w-5 ${accent}`} />}
        <h2 className="text-base font-semibold text-zinc-50">{title}</h2>
        {action && <div className="ml-auto">{action}</div>}
      </div>
      <div className="p-5">{children}</div>
    </section>
  );
}

function CopyButton({ value, title = 'Copy' }) {
  const [copied, setCopied] = useState(false);
  return (
    <button
      type="button"
      onClick={() => {
        navigator.clipboard.writeText(String(value));
        setCopied(true);
        setTimeout(() => setCopied(false), 1200);
      }}
      className="text-zinc-500 transition hover:text-neon-cyan"
      title={title}
    >
      {copied ? <CheckCircle className="h-3.5 w-3.5 text-emerald-400" /> : <Copy className="h-3.5 w-3.5" />}
    </button>
  );
}

function QuickInfoRow({ label, value, mono = false, editable = false, onEdit = null }) {
  return (
    <div className="flex items-center justify-between gap-3 py-2">
      <span className="text-sm text-mist">{label}</span>
      <span className="flex items-center gap-1.5 text-sm font-medium text-zinc-200">
        <span className={mono ? 'font-mono' : ''}>{value}</span>
        {value ? <CopyButton value={value} title={`Copy ${label}`} /> : null}
        {editable && onEdit ? (
          <button
            type="button"
            onClick={onEdit}
            className="text-zinc-500 transition hover:text-neon-cyan"
            title="Edit"
          >
            <Pencil className="h-3.5 w-3.5" />
          </button>
        ) : null}
      </span>
    </div>
  );
}

function TrackerTile({ icon: Icon, iconClass, label, value, valueClass = 'text-zinc-50', sub = null, progress = null }) {
  return (
    <div className="rounded-lg border border-line bg-ink p-3.5">
      <div className="flex items-center gap-1.5 text-[11px] font-medium text-mist">
        <Icon className={`h-4 w-4 ${iconClass}`} />
        {label}
      </div>
      <div className={`mt-1.5 text-xl font-bold ${valueClass}`}>{value}</div>
      {progress != null && (
        <div className="mt-2 h-1.5 w-full overflow-hidden rounded-full bg-line">
          <div
            className="h-full rounded-full bg-neon-cyan"
            style={{ width: `${Math.min(100, Math.max(2, progress))}%` }}
          />
        </div>
      )}
      {sub && <div className="mt-1 text-[11px] text-mist">{sub}</div>}
    </div>
  );
}

const DONUT_COLORS = ['#00e5ff', '#b347ea', '#ffab00', '#39ff14', '#ff006e', '#2979ff', '#8888aa'];

function timeAgo(value) {
  const seconds = Math.floor((Date.now() - new Date(value).getTime()) / 1000);
  if (seconds < 60) return 'just now';
  const minutes = Math.floor(seconds / 60);
  if (minutes < 60) return `${minutes}m ago`;
  const hours = Math.floor(minutes / 60);
  if (hours < 24) return `${hours}h ago`;
  const days = Math.floor(hours / 24);
  if (days < 30) return `${days}d ago`;
  return formatDateTime(value);
}

function longDate(value) {
  return new Intl.DateTimeFormat(undefined, { year: 'numeric', month: 'short', day: 'numeric' }).format(new Date(value));
}

export default function ProfilePage({ onBack, user, avatarUrl, isAdmin, onLogout }) {
  const [profile, setProfile] = useState(null);
  const [details, setDetails] = useState(null); // linked player's full tracker details (activity, 30-day history, status)
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');

  // Discord linking state
  const [discordInput, setDiscordInput] = useState('');
  const [discordBusy, setDiscordBusy] = useState(false);
  const [discordEditing, setDiscordEditing] = useState(false);

  // Password change state
  const [pwForm, setPwForm] = useState({ current: '', next: '', confirm: '' });
  const [pwBusy, setPwBusy] = useState(false);
  const [pwEditing, setPwEditing] = useState(false);
  const [showPw, setShowPw] = useState(false);

  // Game info state (Roblox username / Roblox ID / Discord ID)
  const [gameEditing, setGameEditing] = useState(false);
  const [gameBusy, setGameBusy] = useState(false);
  const [gameForm, setGameForm] = useState({ robloxUsername: '', robloxUserId: '', discordUserId: '' });

  // Banner picker state (own profile only)
  const [bannerEditing, setBannerEditing] = useState(false);
  const [bannerInput, setBannerInput] = useState('');
  const [bannerBusy, setBannerBusy] = useState(false);
  const [bannerUploading, setBannerUploading] = useState(false);
  const [bannerFileVersion, setBannerFileVersion] = useState(null);
  const bannerFileRef = useRef(null);

  const loadProfile = useCallback(async () => {
    setLoading(true);
    setError('');
    try {
      const data = await api.myProfile();
      setProfile(data);
      setBannerFileVersion(data.bannerImageVersion ?? null);
      setDiscordInput(data.discordUserId ?? '');
      setDiscordEditing(false);
      if (data.player?.id) {
        try {
          const playerDetails = await api.player(data.player.id);
          setDetails(playerDetails);
        } catch {
          setDetails(null); // activity/status cards are optional
        }
      } else {
        setDetails(null);
      }
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    loadProfile();
  }, [loadProfile]);

  async function handleDiscordSave() {
    setDiscordBusy(true);
    setNotice('');
    setError('');
    try {
      const result = await api.updateDiscord(discordInput.trim());
      setNotice(result.message);
      await loadProfile();
    } catch (err) {
      setError(err.message);
    } finally {
      setDiscordBusy(false);
    }
  }

  async function handleDiscordUnlink() {
    if (!window.confirm('Unlink your Discord account? The Discord bot will no longer recognize you.')) return;
    setDiscordBusy(true);
    setNotice('');
    setError('');
    try {
      const result = await api.updateDiscord('');
      setNotice(result.message);
      await loadProfile();
    } catch (err) {
      setError(err.message);
    } finally {
      setDiscordBusy(false);
    }
  }

  function startGameEditing() {
    setGameForm({
      robloxUsername: profile.robloxUsername ?? '',
      robloxUserId: profile.robloxUserId != null ? String(profile.robloxUserId) : '',
      discordUserId: profile.discordUserId ?? ''
    });
    setGameEditing(true);
  }

  async function handleGameInfoSave(event) {
    event.preventDefault();
    setGameBusy(true);
    setError('');
    setNotice('');
    try {
      const result = await api.updateGameInfo({
        robloxUsername: gameForm.robloxUsername.trim(),
        robloxUserId: gameForm.robloxUserId.trim() ? Number(gameForm.robloxUserId.trim()) : null,
        discordUserId: gameForm.discordUserId.trim()
      });
      setNotice(result.message ?? 'Game info saved.');
      setGameEditing(false);
      await loadProfile();
    } catch (err) {
      setError(err.message);
    } finally {
      setGameBusy(false);
    }
  }

  async function handleBannerSave(reset = false) {
    setBannerBusy(true);
    setError('');
    setNotice('');
    try {
      const value = reset ? '' : bannerInput.trim();
      const result = await api.updateBanner(value);
      setNotice(result.message ?? 'Banner updated.');
      setBannerEditing(false);
      setBannerInput('');
      await loadProfile();
    } catch (err) {
      setError(err.message);
    } finally {
      setBannerBusy(false);
    }
  }

  // Resize an image file to at most 1600x400 (banner shape) and re-encode as
  // JPEG so uploads from the user's PC stay small (<2 MB server limit).
  function resizeImageFile(file, maxWidth = 1600, maxHeight = 400) {
    return new Promise((resolve, reject) => {
      const objectUrl = URL.createObjectURL(file);
      const img = new Image();
      img.onload = () => {
        URL.revokeObjectURL(objectUrl);
        try {
          // Compute the biggest centered cover-crop that fits the limits.
          const scale = Math.min(maxWidth / img.width, maxHeight / img.height);
          const cropW = Math.min(img.width, Math.max(1, Math.round(maxWidth / Math.max(1, scale))));
          const cropH = Math.min(img.height, Math.max(1, Math.round(maxHeight / Math.max(1, scale))));
          const srcX = Math.floor((img.width - cropW) / 2);
          const srcY = Math.floor((img.height - cropH) / 2);

          // Render at native resolution first, then step down for quality.
          let canvas = document.createElement('canvas');
          let ctx = canvas.getContext('2d');
          canvas.width = cropW;
          canvas.height = cropH;
          ctx.imageSmoothingEnabled = true;
          ctx.imageSmoothingQuality = 'high';
          ctx.drawImage(img, srcX, srcY, cropW, cropH, 0, 0, cropW, cropH);

          let quality = 0.85;
          const stepDown = () => {
            let dataUrl;
            try {
              dataUrl = canvas.toDataURL('image/jpeg', quality);
            } catch (err) {
              reject(err);
              return;
            }
            const approxBytes = Math.floor((dataUrl.length - 'data:image/jpeg;base64,'.length) * 0.75);
            if (approxBytes > 1_500_000 && quality > 0.4) {
              quality -= 0.15;
              stepDown();
              return;
            }
            if (approxBytes > 1_500_000) {
              // Still too big: halve the pixel dimensions and retry once.
              const half = document.createElement('canvas');
              half.width = Math.max(1, Math.floor(canvas.width / 2));
              half.height = Math.max(1, Math.floor(canvas.height / 2));
              half.getContext('2d').drawImage(canvas, 0, 0, half.width, half.height);
              canvas = half;
              quality = 0.8;
              stepDown();
              return;
            }
            resolve(dataUrl);
          };
          stepDown();
        } catch (err) {
          reject(err);
        }
      };
      img.onerror = () => {
        URL.revokeObjectURL(objectUrl);
        reject(new Error('Could not read that image. Try a different file.'));
      };
      img.src = objectUrl;
    });
  }

  async function handleBannerFile(event) {
    const file = event.target.files?.[0];
    event.target.value = '';
    if (!file) return;

    if (!file.type.startsWith('image/')) {
      setError('That file is not an image.');
      return;
    }

    setError('');
    setNotice('');
    setBannerUploading(true);
    try {
      const dataUrl = await resizeImageFile(file);
      const blob = await (await fetch(dataUrl)).blob();
      const result = await api.uploadBanner(new File([blob], 'banner.jpg', { type: 'image/jpeg' }));
      setNotice(result.message ?? 'Banner image uploaded.');
      setBannerEditing(false);
      setBannerFileVersion(result.bannerImageVersion ?? String(Date.now()));
      await loadProfile();
    } catch (err) {
      setError(err.message);
    } finally {
      setBannerUploading(false);
    }
  }

  async function handlePasswordChange(event) {
    event.preventDefault();
    setError('');
    setNotice('');
    if (pwForm.next !== pwForm.confirm) {
      setError('New passwords do not match.');
      return;
    }
    const issues = passwordIssues(pwForm.next);
    if (issues.length > 0) {
      setError('Password does not meet the requirements: ' + issues.join(', ') + '.');
      return;
    }
    setPwBusy(true);
    try {
      const result = await api.changePassword({ currentPassword: pwForm.current, newPassword: pwForm.next });
      setNotice(result.message ?? 'Password updated successfully.');
      setPwForm({ current: '', next: '', confirm: '' });
      setPwEditing(false);
    } catch (err) {
      setError(err.message);
    } finally {
      setPwBusy(false);
    }
  }

  // Per-game Recent Activity feed: sessions computed by the API from real
  // Started/Stopped events (all games, not only the tracked one), newest first.
  const gameSessions = useMemo(() => {
    const sessions = details?.gameSessions;
    if (Array.isArray(sessions) && sessions.length > 0) return sessions;
    return [];
  }, [details]);

  // Last-7-days playtime distribution from the real DailyPlaytime history —
  // drives the donut in the Player Stats card.
  const weekDonut = useMemo(() => {
    const days = details?.last30Days?.slice(-7) ?? [];
    const total = days.reduce((sum, d) => sum + (d.playSeconds || 0), 0);
    if (total <= 0) return [];
    return days.map((d) => ({
      name: new Intl.DateTimeFormat(undefined, { weekday: 'short' }).format(new Date(`${d.date}T00:00:00Z`)),
      value: d.playSeconds || 0,
      seconds: d.playSeconds || 0
    })).filter((d) => d.value > 0);
  }, [details]);

  if (loading) {
    return (
      <div className="min-h-screen bg-[#050510] text-zinc-50">
        <Header user={user} avatarUrl={avatarUrl} isAdmin={isAdmin} onLogout={onLogout} />
        <div className="mx-auto max-w-3xl px-5 py-8 text-sm text-mist">Loading profile...</div>
      </div>
    );
  }

  if (!profile) {
    return (
      <div className="min-h-screen bg-[#050510] text-zinc-50">
        <Header user={user} avatarUrl={avatarUrl} isAdmin={isAdmin} onLogout={onLogout} />
        <div className="mx-auto max-w-3xl px-5 py-8">
          <button type="button" onClick={onBack} className="mb-4 inline-flex items-center gap-1.5 text-sm text-mist hover:text-zinc-200 transition">
            <ArrowLeft className="h-4 w-4" /> Back to tracker
          </button>
          <div className="rounded-xl border border-red-400/30 bg-red-400/10 px-4 py-3 text-sm text-red-100">{error || 'Could not load profile.'}</div>
        </div>
      </div>
    );
  }

  const player = profile.player;
  const joinRequest = profile.joinRequest;
  const discordLinked = !!profile.discordUserId;
  const progress = profile.progress;
  const presence = getPresence(details);
  const presenceStyle = PRESENCE_STYLES[presence];
  const displayName = player?.username ?? profile.robloxUsername ?? profile.username;
  const displayAvatar = player?.avatarUrl ?? avatarUrl;
  const displayRobloxId = player?.robloxUserId ?? profile.robloxUserId;
  const weekDonutTotal = weekDonut.reduce((s, d) => s + d.value, 0);
  const bannerImageUrl = profile.bannerImageVersion
    ? `/api/profile/banner-image/${profile.id}?v=${profile.bannerImageVersion}`
    : null;
  const heroImage = bannerImageUrl ?? profile.bannerUrl;

  return (
    <div className="min-h-screen bg-[#050510] text-zinc-50">
      <Header user={user} avatarUrl={avatarUrl} isAdmin={isAdmin} onLogout={onLogout} />

      <div className="mx-auto max-w-7xl space-y-5 px-5 py-6">
        {/* Header row */}
        <div className="flex items-center justify-between">
          <button
            type="button"
            onClick={onBack}
            className="inline-flex items-center gap-1.5 rounded-lg border border-neon-cyan/[0.08] px-3 py-2 text-sm font-medium text-zinc-300 transition hover:bg-neon-cyan/[0.06]"
          >
            <ArrowLeft className="h-4 w-4" />
            Back to tracker
          </button>
          <h1 className="flex items-center gap-2 text-xl font-bold">
            <User className="h-5 w-5 text-neon-cyan" />
            My Profile
          </h1>
        </div>

        {/* Notices */}
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

        <div className="grid items-start gap-5 lg:grid-cols-[minmax(0,1fr)_330px]">
          {/* ══════════ LEFT COLUMN ══════════ */}
          <div className="space-y-5">
            {/* ─── HERO BANNER ─── */}
            <div className="relative overflow-hidden rounded-xl border border-line shadow-glow">
              {/* Custom banner image (uploaded file or URL, if set), otherwise the default gradient */}
              {heroImage ? (
                <>
                  <div className="absolute inset-0 bg-gradient-to-br from-[#221a4d] via-[#1b1440] to-[#0a0a1a]" />
                  <img
                    src={heroImage}
                    alt=""
                    className="absolute inset-0 h-full w-full object-cover"
                    onError={(e) => { e.currentTarget.style.display = 'none'; }}
                  />
                  <div className="absolute inset-0 bg-gradient-to-t from-[#0a0a1a]/80 via-transparent to-[#0a0a1a]/30" />
                </>
              ) : (
                <>
                  <div className="absolute inset-0 bg-gradient-to-br from-[#221a4d] via-[#1b1440] to-[#0a0a1a]" />
                  <div className="absolute inset-0 bg-[radial-gradient(ellipse_at_70%_20%,rgba(179,71,234,0.18),transparent_55%),radial-gradient(ellipse_at_20%_90%,rgba(0,229,255,0.12),transparent_50%)]" />
                </>
              )}

              {/* Banner image controls (own profile) */}
              {bannerEditing ? (
                <div className="absolute right-3 top-3 z-10 w-72 rounded-lg border border-neon-cyan/20 bg-[#0d0d1a]/95 p-3 shadow-2xl backdrop-blur">
                  <div className="text-xs font-semibold text-zinc-200">Banner image</div>
                  {/* Pick an image from this device */}
                  <input
                    ref={bannerFileRef}
                    type="file"
                    accept="image/png,image/jpeg,image/gif,image/webp"
                    className="hidden"
                    onChange={handleBannerFile}
                  />
                  <button
                    type="button"
                    onClick={() => bannerFileRef.current?.click()}
                    disabled={bannerUploading || bannerBusy}
                    className="mt-2 flex w-full items-center justify-center gap-1.5 rounded-md border border-neon-cyan/40 bg-neon-cyan/10 px-2.5 py-2 text-xs font-semibold text-neon-cyan transition hover:bg-neon-cyan/20 disabled:opacity-50"
                  >
                    <Upload className="h-3.5 w-3.5" />
                    {bannerUploading ? 'Uploading…' : 'Upload from your PC'}
                  </button>
                  <div className="mt-2 text-[10px] text-zinc-500">or paste an image URL</div>
                  <input
                    type="url"
                    value={bannerInput}
                    onChange={(e) => setBannerInput(e.target.value)}
                    placeholder="https://example.com/banner.jpg"
                    className="mt-1 w-full rounded-md border border-line bg-ink px-2.5 py-1.5 text-xs text-zinc-50 placeholder:text-zinc-500 focus:border-neon-cyan/50 focus:outline-none"
                  />
                  <div className="mt-2 flex items-center gap-1.5">
                    <button
                      type="button"
                      onClick={() => handleBannerSave(false)}
                      disabled={bannerBusy || !bannerInput.trim()}
                      className="inline-flex items-center gap-1 rounded-md bg-neon-cyan px-2.5 py-1 text-[11px] font-bold text-zinc-950 transition hover:bg-neon-cyan/80 disabled:opacity-50"
                    >
                      <Save className="h-3 w-3" />
                      {bannerBusy ? 'Saving…' : 'Save URL'}
                    </button>
                    {profile.bannerUrl && (
                      <button
                        type="button"
                        onClick={() => handleBannerSave(true)}
                        disabled={bannerBusy}
                        className="inline-flex items-center gap-1 rounded-md border border-zinc-700 px-2.5 py-1 text-[11px] font-medium text-zinc-300 transition hover:text-zinc-100 disabled:opacity-50"
                      >
                        <RotateCcw className="h-3 w-3" />
                        Reset
                      </button>
                    )}
                    <button
                      type="button"
                      onClick={() => { setBannerEditing(false); setBannerInput(''); }}
                      className="ml-auto text-[11px] text-zinc-500 transition hover:text-zinc-300"
                    >
                      Cancel
                    </button>
                  </div>
                </div>
              ) : (
                <button
                  type="button"
                  onClick={() => { setBannerEditing(true); setBannerInput(profile.bannerUrl ?? ''); }}
                  title="Set or change banner image"
                  className="absolute right-3 top-3 z-10 inline-flex items-center gap-1.5 rounded-lg border border-white/10 bg-black/40 px-2.5 py-1.5 text-[11px] font-semibold text-zinc-200 backdrop-blur transition hover:bg-black/60"
                >
                  <ImagePlus className="h-3.5 w-3.5" />
                  {profile.bannerUrl ? 'Change banner' : 'Add banner'}
                </button>
              )}

              <div className="relative flex flex-wrap items-center gap-5 p-6">
                <div className="relative shrink-0">
                  {displayAvatar ? (
                    <img src={displayAvatar} alt="" className="h-20 w-20 rounded-2xl border-2 border-neon-cyan/30 object-cover" />
                  ) : (
                    <div className="grid h-20 w-20 place-items-center rounded-2xl border-2 border-neon-cyan/30 bg-panelSoft text-2xl font-bold text-neon-cyan/80">
                      {displayName.slice(0, 1).toUpperCase()}
                    </div>
                  )}
                  {player && <PresenceDot details={details} />}
                </div>
                <div className="min-w-0 flex-1">
                  <div className="truncate text-2xl font-bold text-zinc-50">{displayName}</div>
                  <div className="mt-1.5 flex flex-wrap items-center gap-2">
                    <span className="inline-flex items-center gap-1 rounded-md bg-neon-cyan/15 px-2 py-0.5 text-[11px] font-semibold text-neon-cyan">
                      {profile.role === 'Admin' ? 'Admin' : 'Member'}
                    </span>
                    <span className="inline-flex items-center gap-1 rounded-md bg-white/5 px-2 py-0.5 text-[11px] font-medium text-zinc-300">
                      <Gamepad2 className="h-3 w-3" />
                      Roblox User
                    </span>
                  </div>
                  {displayRobloxId != null && (
                    <div className="mt-2 flex items-center gap-1.5 font-mono text-sm text-mist">
                      ID: {displayRobloxId}
                      <CopyButton value={displayRobloxId} title="Copy Roblox ID" />
                    </div>
                  )}
                  <div className="mt-2 flex flex-wrap items-center gap-x-4 gap-y-1 text-xs text-mist">
                    <span className="inline-flex items-center gap-1.5">
                      <CalendarDays className="h-3.5 w-3.5" />
                      Joined {longDate(profile.createdAt)}
                    </span>
                    {player && (
                      <span>
                        Club: <span className="font-semibold text-neon-cyan">{player.club || 'None'}</span>
                      </span>
                    )}
                  </div>
                </div>
                <button
                  type="button"
                  onClick={() => setPwEditing((s) => !s)}
                  className="inline-flex shrink-0 items-center gap-1.5 self-end rounded-lg border border-white/10 bg-white/5 px-3.5 py-2 text-sm font-medium text-zinc-200 transition hover:bg-white/10"
                >
                  <KeyRound className="h-4 w-4" />
                  Change password
                </button>
              </div>
            </div>

            {/* Password form (opened from the banner) */}
            {pwEditing && (
              <form onSubmit={handlePasswordChange} className="space-y-2 rounded-xl border border-line bg-panel p-5 shadow-glow">
                <div className="flex items-center gap-2 text-sm font-semibold text-zinc-200">
                  <ShieldCheck className="h-4 w-4 text-neon-green" />
                  Change password
                </div>
                <input
                  type={showPw ? 'text' : 'password'}
                  value={pwForm.current}
                  onChange={(e) => setPwForm((c) => ({ ...c, current: e.target.value }))}
                  placeholder="Current password"
                  className="w-full min-h-9 rounded-md border border-line bg-ink px-3 text-sm text-zinc-50 placeholder:text-zinc-500"
                  required
                  autoComplete="current-password"
                />
                <div className="relative">
                  <input
                    type={showPw ? 'text' : 'password'}
                    value={pwForm.next}
                    onChange={(e) => setPwForm((c) => ({ ...c, next: e.target.value }))}
                    placeholder="New password"
                    className="w-full min-h-9 rounded-md border border-line bg-ink px-3 pr-10 text-sm text-zinc-50 placeholder:text-zinc-500"
                    required
                    minLength={8}
                    autoComplete="new-password"
                  />
                  <button
                    type="button"
                    onClick={() => setShowPw(!showPw)}
                    className="absolute right-3 top-1/2 -translate-y-1/2 text-zinc-500 transition hover:text-zinc-300"
                    tabIndex={-1}
                  >
                    {showPw ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
                  </button>
                </div>
                <PasswordChecklist password={pwForm.next} />
                <input
                  type={showPw ? 'text' : 'password'}
                  value={pwForm.confirm}
                  onChange={(e) => setPwForm((c) => ({ ...c, confirm: e.target.value }))}
                  placeholder="Repeat new password"
                  className="w-full min-h-9 rounded-md border border-line bg-ink px-3 text-sm text-zinc-50 placeholder:text-zinc-500"
                  required
                  minLength={8}
                  autoComplete="new-password"
                />
                <div className="flex gap-2 pt-1">
                  <button
                    type="submit"
                    disabled={pwBusy}
                    className="inline-flex min-h-9 items-center rounded-lg bg-neon-green px-4 text-sm font-semibold text-zinc-950 transition hover:bg-neon-green/80 disabled:opacity-60"
                  >
                    {pwBusy ? 'Saving...' : 'Update password'}
                  </button>
                  <button
                    type="button"
                    onClick={() => { setPwEditing(false); setPwForm({ current: '', next: '', confirm: '' }); }}
                    className="inline-flex min-h-9 items-center rounded-lg border border-line px-4 text-sm font-medium text-zinc-300 transition hover:bg-zinc-800"
                  >
                    Cancel
                  </button>
                </div>
              </form>
            )}

            {/* ─── TRACKER ─── */}
            <Card
              icon={Trophy}
              title="Tracker"
              accent="text-neon-amber"
              action={
                player ? (
                  <span className="flex items-center gap-2 text-xs text-mist">
                    Club: <span className="font-semibold text-zinc-200">{player.club || 'None'}</span>
                    <span className="inline-flex items-center gap-1 rounded-md border border-emerald-400/30 bg-emerald-400/10 px-1.5 py-0.5 text-[10px] font-semibold uppercase tracking-wider text-emerald-300">
                      <CheckCircle className="h-3 w-3" />
                      Member
                    </span>
                  </span>
                ) : null
              }
            >
              {player ? (
                <>
                  <div className="grid gap-3 sm:grid-cols-3">
                    <TrackerTile
                      icon={Clock}
                      iconClass="text-neon-cyan"
                      label="Total Playtime"
                      value={formatDuration(player.totalPlaySeconds)}
                      valueClass="text-neon-amber"
                      sub={`Today ${formatDuration(player.todayPlaySeconds)}`}
                    />
                    <TrackerTile
                      icon={Crown}
                      iconClass="text-neon-purple"
                      label="Weekly Rank"
                      value={profile.weeklyLeaderboardPosition ? `#${profile.weeklyLeaderboardPosition}` : '—'}
                    />
                    <TrackerTile
                      icon={BarChart3}
                      iconClass="text-neon-purple"
                      label="All-Time Rank"
                      value={profile.totalLeaderboardPosition ? `#${profile.totalLeaderboardPosition}` : '—'}
                    />
                  </div>
                  {progress && (
                    <div className="mt-3 grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
                      <TrackerTile
                        icon={Flame}
                        iconClass="text-neon-amber"
                        label="Current Streak"
                        value={`${progress.currentStreak}d`}
                        valueClass="text-neon-amber"
                      />
                      <TrackerTile
                        icon={Flame}
                        iconClass="text-neon-green"
                        label="Longest Streak"
                        value={`${progress.longestStreak}d`}
                        valueClass="text-neon-green"
                      />
                      <TrackerTile
                        icon={Star}
                        iconClass="text-neon-amber"
                        label="Achievements"
                        value={`${progress.achievementsUnlocked}/${progress.totalAchievements}`}
                        valueClass="text-neon-amber"
                        progress={(progress.achievementsUnlocked / Math.max(1, progress.totalAchievements)) * 100}
                      />
                      <TrackerTile
                        icon={Target}
                        iconClass="text-neon-cyan"
                        label="Days Played"
                        value={String(progress.daysPlayed)}
                      />
                    </div>
                  )}
                  <div className="mt-4 grid gap-3 sm:grid-cols-2">
                    <button
                      type="button"
                      onClick={() => { window.location.hash = 'stats'; }}
                      className="inline-flex min-h-10 items-center justify-center gap-2 rounded-lg border border-neon-cyan/[0.08] bg-ink px-4 text-sm font-medium text-zinc-200 transition hover:border-neon-cyan/30"
                    >
                      <TrendingUp className="h-4 w-4 text-neon-cyan" />
                      Full statistics
                    </button>
                    <button
                      type="button"
                      onClick={() => { window.location.hash = 'achievements'; }}
                      className="inline-flex min-h-10 items-center justify-center gap-2 rounded-lg border border-neon-cyan/[0.08] bg-ink px-4 text-sm font-medium text-zinc-200 transition hover:border-neon-cyan/30"
                    >
                      <Medal className="h-4 w-4 text-neon-amber" />
                      View achievements
                    </button>
                  </div>
                </>
              ) : joinRequest ? (
                <div className="space-y-2">
                  <div className="flex items-center gap-2">
                    {joinRequest.status === 'Pending' && (
                      <span className="inline-flex items-center gap-1.5 rounded-md border border-amber-400/30 bg-amber-400/10 px-2 py-1 text-[10px] font-semibold uppercase tracking-wider text-amber-300">
                        Request pending
                      </span>
                    )}
                    {joinRequest.status === 'Approved' && (
                      <span className="inline-flex items-center gap-1.5 rounded-md border border-emerald-400/30 bg-emerald-400/10 px-2 py-1 text-[10px] font-semibold uppercase tracking-wider text-emerald-300">
                        Approved — linking soon
                      </span>
                    )}
                    {joinRequest.status === 'Rejected' && (
                      <span className="inline-flex items-center gap-1.5 rounded-md border border-red-400/30 bg-red-400/10 px-2 py-1 text-[10px] font-semibold uppercase tracking-wider text-red-300">
                        Declined — you can reapply
                      </span>
                    )}
                  </div>
                  <div className="rounded-lg border border-line bg-ink p-3 text-sm text-mist">
                    <div>Roblox: <span className="text-zinc-200">{joinRequest.robloxUsername}</span> (ID {joinRequest.robloxUserId})</div>
                    <div>Club: <span className="text-zinc-200">{joinRequest.club || 'None'}</span></div>
                    <div>Submitted: <span className="text-zinc-200">{formatDateTime(joinRequest.createdAt)}</span></div>
                    {joinRequest.reviewedAt && (
                      <div>Reviewed: <span className="text-zinc-200">{formatDateTime(joinRequest.reviewedAt)}{joinRequest.reviewedBy ? ` by ${joinRequest.reviewedBy}` : ''}</span></div>
                    )}
                  </div>
                </div>
              ) : (
                <p className="text-sm text-mist">
                  You are not in the tracker yet. Use "Request to Join" on the main page to submit a request.
                </p>
              )}
            </Card>

            {/* ─── RECENT ACTIVITY ─── */}
            {player && (
              <Card icon={Gamepad2} title="Recent Activity" accent="text-neon-cyan">
                {gameSessions.length > 0 ? (
                  <div className="space-y-2">
                    {gameSessions.slice(0, 8).map((session, index) => {
                      const live = !session.endedAt;
                      const gameIcon = session.gameIconUrl;
                      const gameInitial = (session.gameName || '?').trim().charAt(0).toUpperCase();
                      const when = session.startedAt || session.endedAt;
                      return (
                        <div
                          key={`${session.gameName}-${when}-${index}`}
                          className="flex items-center gap-3 rounded-lg border border-line bg-ink p-3"
                        >
                          {/* Game icon (real Roblox game thumbnail when available) */}
                          {gameIcon ? (
                            <img
                              src={gameIcon}
                              alt=""
                              className="h-10 w-10 shrink-0 rounded-lg border border-line object-cover"
                              onError={(e) => { e.currentTarget.style.visibility = 'hidden'; }}
                            />
                          ) : (
                            <span className="grid h-10 w-10 shrink-0 place-items-center rounded-lg border border-line bg-panelSoft text-sm font-bold text-neon-cyan/80">
                              {gameInitial}
                            </span>
                          )}

                          <div className="min-w-0 flex-1">
                            <div className="flex items-center gap-2">
                              <span className="truncate text-sm font-semibold text-zinc-100">{session.gameName}</span>
                              {live && (
                                <span className="inline-flex shrink-0 items-center gap-1 rounded-full bg-emerald-400/10 px-1.5 py-0.5 text-[10px] font-bold text-emerald-300">
                                  <span className="h-1.5 w-1.5 animate-pulse rounded-full bg-emerald-400" />
                                  LIVE
                                </span>
                              )}
                            </div>
                            <div className="mt-0.5 flex flex-wrap items-center gap-x-3 gap-y-0.5 text-[11px] text-mist">
                              <span className="inline-flex items-center gap-1">
                                <Play className="h-3 w-3 text-emerald-400" />
                                {session.startedAt ? formatDateTime(session.startedAt) : '—'}
                              </span>
                              <span className="inline-flex items-center gap-1">
                                <Square className="h-3 w-3 text-zinc-400" />
                                {session.endedAt ? formatDateTime(session.endedAt) : 'now'}
                              </span>
                              {session.startedAt && session.endedAt && (
                                <span className="text-zinc-400">
                                  {formatDuration(Math.max(0, (new Date(session.endedAt) - new Date(session.startedAt)) / 1000))}
                                </span>
                              )}
                            </div>
                          </div>
                        </div>
                      );
                    })}
                    {details?.recentActivity?.some((e) => e.eventType === 'Adjusted') && (
                      <div className="px-1 pt-1 text-[11px] text-zinc-500">
                        Manual playtime adjustments are listed in the tracker event log on the dashboard.
                      </div>
                    )}
                  </div>
                ) : details?.recentActivity?.length > 0 ? (
                  <p className="text-sm text-mist">No game sessions recorded yet — only manual adjustments.</p>
                ) : (
                  <p className="text-sm text-mist">No activity recorded yet.</p>
                )}
              </Card>
            )}
          </div>

          {/* ══════════ RIGHT COLUMN ══════════ */}
          <div className="space-y-5">
            {/* ─── QUICK INFO ─── */}
            <Card
              icon={ShieldCheck}
              title="Quick Info"
              accent="text-neon-cyan"
              action={
                !player ? (
                  <button
                    type="button"
                    onClick={startGameEditing}
                    className="text-zinc-500 transition hover:text-neon-cyan"
                    title="Edit game info"
                  >
                    <Pencil className="h-4 w-4" />
                  </button>
                ) : null
              }
            >
              {gameEditing ? (
                <form onSubmit={handleGameInfoSave} className="space-y-3">
                  <div>
                    <label htmlFor="gi-username" className="mb-1 block text-xs font-medium text-zinc-400">Roblox username</label>
                    <input
                      id="gi-username"
                      type="text"
                      value={gameForm.robloxUsername}
                      onChange={(e) => setGameForm((c) => ({ ...c, robloxUsername: e.target.value }))}
                      placeholder="Your Roblox username"
                      className="w-full min-h-10 rounded-lg border border-line bg-ink px-3 text-sm text-zinc-50 placeholder:text-zinc-500"
                      maxLength={100}
                    />
                  </div>
                  <div>
                    <label htmlFor="gi-userid" className="mb-1 block text-xs font-medium text-zinc-400">Roblox user ID</label>
                    <input
                      id="gi-userid"
                      type="text"
                      inputMode="numeric"
                      value={gameForm.robloxUserId}
                      onChange={(e) => setGameForm((c) => ({ ...c, robloxUserId: e.target.value.replace(/[^0-9]/g, '') }))}
                      placeholder="e.g. 2243793833"
                      className="w-full min-h-10 rounded-lg border border-line bg-ink px-3 text-sm text-zinc-50 placeholder:text-zinc-500"
                    />
                    <p className="mt-1 text-[11px] text-zinc-500">Find it on your Roblox profile page URL: roblox.com/users/&lt;id&gt;/profile</p>
                    {gameForm.robloxUserId ? (
                      <a
                        href={`https://www.roblox.com/users/${gameForm.robloxUserId}/profile`}
                        target="_blank"
                        rel="noreferrer"
                        className="mt-1 inline-flex items-center gap-1 text-[11px] text-neon-cyan hover:text-neon-cyan/80"
                      >
                        <ExternalLink className="h-3 w-3" />
                        Preview your profile
                      </a>
                    ) : null}
                  </div>
                  <div>
                    <label htmlFor="gi-discord" className="mb-1 block text-xs font-medium text-zinc-400">Discord User ID (optional)</label>
                    <input
                      id="gi-discord"
                      type="text"
                      inputMode="numeric"
                      value={gameForm.discordUserId}
                      onChange={(e) => setGameForm((c) => ({ ...c, discordUserId: e.target.value.replace(/\s/g, '') }))}
                      placeholder="123456789012345678"
                      className="w-full min-h-10 rounded-lg border border-line bg-ink px-3 text-sm text-zinc-50 placeholder:text-zinc-500"
                      maxLength={20}
                    />
                    <p className="mt-1 text-[11px] text-zinc-500">{DISCORD_COPY_HINT}</p>
                  </div>
                  <div className="flex gap-2">
                    <button
                      type="submit"
                      disabled={gameBusy}
                      className="inline-flex min-h-9 items-center gap-1.5 rounded-lg bg-neon-cyan px-4 text-sm font-semibold text-zinc-950 transition hover:bg-neon-cyan/80 disabled:opacity-60"
                    >
                      <Save className="h-4 w-4" />
                      {gameBusy ? 'Saving...' : 'Save'}
                    </button>
                    <button
                      type="button"
                      onClick={() => setGameEditing(false)}
                      className="inline-flex min-h-9 items-center rounded-lg border border-line px-4 text-sm font-medium text-zinc-300 transition hover:bg-zinc-800"
                    >
                      Cancel
                    </button>
                  </div>
                </form>
              ) : (
                <div className="divide-y divide-line/60">
                  <QuickInfoRow
                    label="Roblox Username"
                    value={profile.robloxUsername || null}
                    editable={!player}
                    onEdit={startGameEditing}
                  />
                  <QuickInfoRow
                    label="Roblox User ID"
                    value={displayRobloxId != null ? String(displayRobloxId) : null}
                    mono
                    editable={!player}
                    onEdit={startGameEditing}
                  />
                  <QuickInfoRow
                    label="Discord ID"
                    value={profile.discordUserId || null}
                    mono
                    editable
                    onEdit={startGameEditing}
                  />
                  {!profile.discordUserId && (
                    <p className="pt-2 text-[11px] text-mist">
                      Link Discord so the bot's <span className="font-mono text-zinc-300">/playtime</span> can find you.
                    </p>
                  )}
                </div>
              )}
            </Card>

            {/* ─── ROBLOX ─── */}
            {player && (
              <Card icon={Gamepad2} title="Roblox" accent="text-neon-cyan">
                <div className="flex items-center gap-3">
                  {player.avatarUrl ? (
                    <img src={player.avatarUrl} alt="" className="h-14 w-14 rounded-xl border border-neon-cyan/[0.12] object-cover" />
                  ) : (
                    <div className="grid h-14 w-14 place-items-center rounded-xl border border-neon-cyan/[0.12] bg-panelSoft text-lg font-bold text-neon-cyan/80">
                      {player.username.slice(0, 1).toUpperCase()}
                    </div>
                  )}
                  <div className="min-w-0">
                    <div className="truncate font-semibold text-zinc-50">{player.username}</div>
                    <div className="flex items-center gap-1.5 font-mono text-xs text-mist">
                      ID: {player.robloxUserId}
                      <CopyButton value={player.robloxUserId} title="Copy Roblox ID" />
                    </div>
                  </div>
                </div>
                <a
                  href={player.profileUrl}
                  target="_blank"
                  rel="noreferrer"
                  className="mt-3 inline-flex items-center gap-1.5 text-sm text-neon-cyan transition hover:text-neon-cyan/80"
                >
                  Open Roblox profile
                  <ExternalLink className="h-3.5 w-3.5" />
                </a>
                <div className="mt-3 rounded-lg border border-line bg-ink p-3">
                  <div className="flex items-center gap-2 text-sm">
                    <span className={`h-2 w-2 rounded-full ${presenceStyle.dot}`} />
                    <span className={`font-medium ${presenceStyle.text}`}>{presenceStyle.label}</span>
                  </div>
                  {presence === 'game' && details?.currentGame && (
                    <div className="mt-1 truncate text-xs text-mist">Playing {details.currentGame}</div>
                  )}
                  {presence === 'offline' && details?.lastSeenPlaying && (
                    <div className="mt-1 text-xs text-mist">Last seen in game {formatDateTime(details.lastSeenPlaying)}</div>
                  )}
                </div>
              </Card>
            )}

            {/* ─── PLAYER STATS ─── */}
            {player && (
              <Card icon={BarChart3} title="Player Stats" accent="text-neon-purple">
                {weekDonutTotal > 0 ? (
                  <div className="flex items-center gap-4">
                    <div className="relative h-28 w-28 shrink-0">
                      <ResponsiveContainer width="100%" height="100%">
                        <PieChart>
                          <Pie
                            data={weekDonut}
                            dataKey="value"
                            nameKey="name"
                            innerRadius={34}
                            outerRadius={54}
                            paddingAngle={3}
                            stroke="none"
                          >
                            {weekDonut.map((entry, index) => (
                              <Cell key={entry.name} fill={DONUT_COLORS[index % DONUT_COLORS.length]} />
                            ))}
                          </Pie>
                        </PieChart>
                      </ResponsiveContainer>
                      <div className="pointer-events-none absolute inset-0 grid place-items-center">
                        <div className="text-center">
                          <div className="text-[9px] font-semibold uppercase tracking-wider text-zinc-500">7 days</div>
                          <div className="text-xs font-bold text-zinc-50">{formatDuration(weekDonutTotal)}</div>
                        </div>
                      </div>
                    </div>
                    <div className="min-w-0 flex-1 space-y-1.5">
                      {weekDonut.map((entry, index) => (
                        <div key={entry.name} className="flex items-center gap-2 text-xs">
                          <span className="h-2 w-2 shrink-0 rounded-full" style={{ background: DONUT_COLORS[index % DONUT_COLORS.length] }} />
                          <span className="flex-1 truncate text-mist">{entry.name}</span>
                          <span className="font-medium text-zinc-300">
                            {Math.round((entry.value / weekDonutTotal) * 100)}%
                          </span>
                        </div>
                      ))}
                    </div>
                  </div>
                ) : (
                  <p className="text-xs text-mist">No playtime in the last 7 days.</p>
                )}
                <div className="mt-4 space-y-2 border-t border-line/60 pt-3">
                  <div className="flex items-center justify-between text-sm">
                    <span className="text-mist">Today</span>
                    <span className="font-semibold text-zinc-100">{formatDuration(player.todayPlaySeconds)}</span>
                  </div>
                  <div className="flex items-center justify-between text-sm">
                    <span className="text-mist">This week</span>
                    <span className="font-semibold text-zinc-100">{formatDuration(player.weeklyPlaySeconds)}</span>
                  </div>
                  <div className="flex items-center justify-between text-sm">
                    <span className="text-mist">This month</span>
                    <span className="font-semibold text-zinc-100">{formatDuration(player.monthlyPlaySeconds)}</span>
                  </div>
                  {progress && progress.daysPlayed > 0 && (
                    <div className="flex items-center justify-between text-sm">
                      <span className="text-mist">Avg / active day</span>
                      <span className="font-semibold text-zinc-100">
                        {formatDuration(Math.round(player.totalPlaySeconds / progress.daysPlayed))}
                      </span>
                    </div>
                  )}
                </div>
              </Card>
            )}

            {/* ─── DISCORD (only for users without a linked tracker player — linked users manage Discord in Quick Info) ─── */}
            {!player && (
              <Card icon={Link2} title="Discord" accent="text-neon-purple">
                <div className="mb-3 flex items-center gap-2">
                  {discordLinked ? (
                    <span className="inline-flex items-center gap-1.5 rounded-md border border-emerald-400/30 bg-emerald-400/10 px-2 py-1 text-[10px] font-semibold uppercase tracking-wider text-emerald-300">
                      <CheckCircle className="h-3 w-3" />
                      Linked
                    </span>
                  ) : (
                    <span className="inline-flex items-center gap-1.5 rounded-md border border-zinc-500/30 bg-zinc-500/10 px-2 py-1 text-[10px] font-semibold uppercase tracking-wider text-zinc-300">
                      <Unlink className="h-3 w-3" />
                      Not linked
                    </span>
                  )}
                  {discordLinked && <span className="font-mono text-xs text-zinc-200">{profile.discordUserId}</span>}
                </div>

                {discordEditing ? (
                  <div className="space-y-2">
                    <input
                      type="text"
                      inputMode="numeric"
                      value={discordInput}
                      onChange={(e) => setDiscordInput(e.target.value.replace(/\s/g, ''))}
                      placeholder="123456789012345678"
                      className="w-full min-h-10 rounded-lg border border-line bg-ink px-3 text-sm text-zinc-50 placeholder:text-zinc-500"
                      maxLength={20}
                      minLength={17}
                    />
                    <p className="text-xs text-mist">{DISCORD_COPY_HINT}</p>
                    <div className="flex gap-2">
                      <button
                        type="button"
                        onClick={handleDiscordSave}
                        disabled={discordBusy}
                        className="inline-flex min-h-9 items-center gap-1.5 rounded-lg bg-neon-cyan px-4 text-sm font-semibold text-zinc-950 transition hover:bg-neon-cyan/80 disabled:opacity-60"
                      >
                        <Link2 className="h-4 w-4" />
                        {discordBusy ? 'Saving...' : 'Save'}
                      </button>
                      <button
                        type="button"
                        onClick={() => { setDiscordEditing(false); setDiscordInput(profile.discordUserId ?? ''); }}
                        className="inline-flex min-h-9 items-center rounded-lg border border-line px-4 text-sm font-medium text-zinc-300 transition hover:bg-zinc-800"
                      >
                        Cancel
                      </button>
                    </div>
                  </div>
                ) : (
                  <div className="flex flex-wrap gap-2">
                    <button
                      type="button"
                      onClick={() => setDiscordEditing(true)}
                      className="inline-flex min-h-9 items-center gap-1.5 rounded-lg border border-neon-cyan/[0.08] px-4 text-sm font-medium text-zinc-200 transition hover:bg-neon-cyan/[0.06]"
                    >
                      <Link2 className="h-4 w-4" />
                      {discordLinked ? 'Change Discord ID' : 'Link Discord ID'}
                    </button>
                    {discordLinked && (
                      <button
                        type="button"
                        onClick={handleDiscordUnlink}
                        disabled={discordBusy}
                        className="inline-flex min-h-9 items-center gap-1.5 rounded-lg border border-red-400/30 bg-red-400/10 px-4 text-sm font-semibold text-red-200 transition hover:bg-red-400/20 disabled:opacity-60"
                      >
                        <Trash2 className="h-4 w-4" />
                        Unlink
                      </button>
                    )}
                  </div>
                )}
                <p className="mt-3 text-xs text-mist">
                  Linking Discord lets the bot's <span className="font-mono text-zinc-300">/playtime</span> command find your stats.
                </p>
              </Card>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}
