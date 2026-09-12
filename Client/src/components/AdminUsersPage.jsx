import { useCallback, useEffect, useState } from 'react';
import {
  AlertCircle,
  ArrowLeft,
  CheckCircle,
  Gamepad2,
  KeyRound,
  Link2,
  RefreshCw,
  Save,
  Search,
  Shield,
  Trash2,
  User as UserIcon
} from 'lucide-react';
import { api } from '../lib/api';
import { PasswordChecklist } from './ProfilePage';
import { passwordIssues } from '../lib/password';
import { formatDateTime } from '../lib/format';
import Header from './Header';

function RoleBadge({ role }) {
  const isAdmin = role === 'Admin';
  return (
    <span
      className={`inline-flex items-center gap-1 rounded-md border px-1.5 py-0.5 text-[10px] font-semibold uppercase tracking-wider ${
        isAdmin
          ? 'border-neon-green/30 bg-neon-green/10 text-neon-green'
          : 'border-zinc-500/30 bg-zinc-500/10 text-zinc-300'
      }`}
    >
      {isAdmin && <Shield className="h-3 w-3" />}
      {role}
    </span>
  );
}

function LinkBadge({ linked }) {
  return linked ? (
    <span className="inline-flex items-center gap-1 rounded-md border border-neon-purple/30 bg-neon-purple/10 px-1.5 py-0.5 text-[10px] font-semibold uppercase tracking-wider text-neon-purple">
      <Link2 className="h-3 w-3" />
      Discord
    </span>
  ) : (
    <span className="inline-flex items-center rounded-md border border-zinc-500/30 bg-zinc-500/10 px-1.5 py-0.5 text-[10px] font-semibold uppercase tracking-wider text-zinc-500">
      No Discord
    </span>
  );
}

function EditUserForm({ user, onSaved, onDone }) {
  const [form, setForm] = useState({
    username: user.username,
    role: user.role,
    discordUserId: user.discordUserId ?? '',
    playerId: user.playerId != null ? String(user.playerId) : ''
  });
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [pwForm, setPwForm] = useState({ next: '', confirm: '' });
  const [pwBusy, setPwBusy] = useState(false);

  async function handleSave(event) {
    event.preventDefault();
    setBusy(true);
    setError('');
    setNotice('');
    try {
      await api.updateUser(user.id, {
        username: form.username.trim(),
        role: form.role,
        discordUserId: form.discordUserId.trim(),
        playerId: form.playerId.trim() ? Number(form.playerId.trim()) : null
      });
      setNotice('User updated.');
      onSaved();
    } catch (err) {
      setError(err.message);
    } finally {
      setBusy(false);
    }
  }

  async function handlePasswordChange(event) {
    event.preventDefault();
    setError('');
    setNotice('');
    if (pwForm.next !== pwForm.confirm) {
      setError('Passwords do not match.');
      return;
    }
    const issues = passwordIssues(pwForm.next);
    if (issues.length > 0) {
      setError('Password does not meet the requirements: ' + issues.join(', ') + '.');
      return;
    }
    setPwBusy(true);
    try {
      await api.adminChangeUserPassword(user.id, { newPassword: pwForm.next });
      setNotice('Password updated.');
      setPwForm({ next: '', confirm: '' });
      setShowPassword(false);
    } catch (err) {
      setError(err.message);
    } finally {
      setPwBusy(false);
    }
  }

  async function handleDelete() {
    const confirmed = window.confirm(
      `Delete account '${user.username}'? This cannot be undone and does not delete the linked tracker player.`
    );
    if (!confirmed) return;
    setBusy(true);
    setError('');
    try {
      await api.deleteUser(user.id);
      onSaved();
      onDone();
    } catch (err) {
      setError(err.message);
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="rounded-xl border border-neon-cyan/[0.12] bg-[#08081a] p-5 space-y-4">
      <div className="flex items-center justify-between">
        <h2 className="flex items-center gap-2 text-lg font-semibold text-zinc-50">
          <UserIcon className="h-5 w-5 text-neon-cyan" />
          Edit user #{user.id}
        </h2>
        <button
          type="button"
          onClick={onDone}
          className="grid h-8 w-8 place-items-center rounded-lg border border-neon-cyan/[0.08] text-mist transition hover:bg-neon-cyan/[0.06] hover:text-zinc-100"
          title="Close editor"
        >
          ✕
        </button>
      </div>

      {error && (
        <div className="flex items-center gap-2 rounded-lg border border-red-400/30 bg-red-400/10 px-4 py-3 text-sm text-red-100">
          <AlertCircle className="h-4 w-4 shrink-0" />
          {error}
        </div>
      )}
      {notice && (
        <div className="flex items-center gap-2 rounded-lg border border-emerald-400/30 bg-emerald-400/10 px-4 py-3 text-sm text-emerald-100">
          <CheckCircle className="h-4 w-4 shrink-0" />
          {notice}
        </div>
      )}

      <form onSubmit={handleSave} className="space-y-3">
        <div className="grid gap-3 sm:grid-cols-2">
          <div>
            <label htmlFor="adm-username" className="mb-1 block text-xs font-medium text-zinc-400">Website username</label>
            <input
              id="adm-username"
              type="text"
              value={form.username}
              onChange={(e) => setForm((c) => ({ ...c, username: e.target.value }))}
              className="w-full min-h-10 rounded-lg border border-neon-cyan/[0.08] bg-ink px-3 text-sm text-zinc-50 placeholder:text-zinc-500"
              required
              minLength={3}
              maxLength={50}
            />
          </div>
          <div>
            <label htmlFor="adm-role" className="mb-1 block text-xs font-medium text-zinc-400">Role</label>
            <select
              id="adm-role"
              value={form.role}
              onChange={(e) => setForm((c) => ({ ...c, role: e.target.value }))}
              className="w-full min-h-10 rounded-lg border border-neon-cyan/[0.08] bg-ink px-3 text-sm text-zinc-50"
            >
              <option value="User">User</option>
              <option value="Admin">Admin</option>
            </select>
          </div>
        </div>

        <div>
          <label htmlFor="adm-discord" className="mb-1 block text-xs font-medium text-zinc-400">
            Discord User ID (17–20 digits, empty to unlink)
          </label>
          <input
            id="adm-discord"
            type="text"
            inputMode="numeric"
            value={form.discordUserId}
            onChange={(e) => setForm((c) => ({ ...c, discordUserId: e.target.value.replace(/\s/g, '') }))}
            placeholder="123456789012345678"
            className="w-full min-h-10 rounded-lg border border-neon-cyan/[0.08] bg-ink px-3 text-sm text-zinc-50 placeholder:text-zinc-500"
            maxLength={20}
          />
        </div>

        <div>
          <label htmlFor="adm-player" className="mb-1 block text-xs font-medium text-zinc-400">
            Linked tracker player ID (optional — links the website account to a tracker player)
          </label>
          <input
            id="adm-user-player-id"
            type="number"
            min={1}
            value={form.playerId}
            onChange={(e) => setForm((c) => ({ ...c, playerId: e.target.value }))}
            placeholder="e.g. 27"
            className="w-full min-h-10 rounded-lg border border-neon-cyan/[0.08] bg-ink px-3 text-sm text-zinc-50 placeholder:text-zinc-500"
          />
          {user.playerUsername && (
            <p className="mt-1 text-[11px] text-zinc-500">
              Currently linked to: {user.playerUsername} (Roblox ID {user.playerRobloxUserId})
            </p>
          )}
        </div>

        <div className="flex flex-wrap gap-2">
          <button
            type="submit"
            disabled={busy}
            className="inline-flex min-h-10 items-center gap-2 rounded-lg bg-neon-cyan px-4 text-sm font-semibold text-zinc-950 transition hover:bg-neon-cyan/80 disabled:opacity-60"
          >
            <Save className="h-4 w-4" />
            {busy ? 'Saving...' : 'Save changes'}
          </button>
          {user.id !== api.getUserId?.() && (
            <button
              type="button"
              onClick={handleDelete}
              disabled={busy}
              className="inline-flex min-h-10 items-center gap-2 rounded-lg border border-red-400/30 bg-red-400/10 px-4 text-sm font-semibold text-red-200 transition hover:bg-red-400/20 disabled:opacity-60"
            >
              <Trash2 className="h-4 w-4" />
              Delete account
            </button>
          )}
        </div>
      </form>

      {/* Password reset */}
      <form onSubmit={handlePasswordChange} className="space-y-2 rounded-lg border border-neon-cyan/[0.08] bg-ink p-3">
        <div className="flex items-center gap-2 text-sm font-semibold text-zinc-200">
          <KeyRound className="h-4 w-4 text-neon-amber" />
          Reset password
        </div>
        {showPassword ? (
          <>
            <div className="grid gap-2 sm:grid-cols-2">
              <input
                type="password"
                value={pwForm.next}
                onChange={(e) => setPwForm((c) => ({ ...c, next: e.target.value }))}
                placeholder="New password"
                className="w-full min-h-9 rounded-md border border-neon-cyan/[0.08] bg-panel px-3 text-sm text-zinc-50 placeholder:text-zinc-500"
                required
                minLength={8}
                autoComplete="new-password"
              />
              <PasswordChecklist password={pwForm.next} />
              <input
                type="password"
                value={pwForm.confirm}
                onChange={(e) => setPwForm((c) => ({ ...c, confirm: e.target.value }))}
                placeholder="Repeat new password"
                className="w-full min-h-9 rounded-md border border-neon-cyan/[0.08] bg-panel px-3 text-sm text-zinc-50 placeholder:text-zinc-500"
                required
                minLength={8}
                autoComplete="new-password"
              />
            </div>
            <div className="flex gap-2">
              <button
                type="submit"
                disabled={pwBusy}
                className="inline-flex min-h-9 items-center rounded-lg bg-neon-amber px-4 text-sm font-semibold text-zinc-950 transition hover:bg-neon-amber/80 disabled:opacity-60"
              >
                {pwBusy ? 'Setting...' : 'Set password'}
              </button>
              <button
                type="button"
                onClick={() => { setShowPassword(false); setPwForm({ next: '', confirm: '' }); }}
                className="inline-flex min-h-9 items-center rounded-lg border border-neon-cyan/[0.08] px-4 text-sm font-medium text-zinc-300 transition hover:bg-neon-cyan/[0.06]"
              >
                Cancel
              </button>
            </div>
          </>
        ) : (
          <button
            type="button"
            onClick={() => setShowPassword(true)}
            className="inline-flex min-h-9 items-center gap-1.5 rounded-lg border border-neon-cyan/[0.08] px-4 text-sm font-medium text-zinc-200 transition hover:bg-neon-cyan/[0.06]"
          >
            <KeyRound className="h-4 w-4" />
            Set a new password
          </button>
        )}
      </form>
    </div>
  );
}

export default function AdminUsersPage({ onBack, user, avatarUrl, isAdmin, onLogout }) {
  const [users, setUsers] = useState([]);
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');
  const [search, setSearch] = useState('');
  const [editingId, setEditingId] = useState(null);
  const [detail, setDetail] = useState(null);

  const loadUsers = useCallback(async (silent = false) => {
    if (!silent) setLoading(true);
    try {
      const data = await api.getUsers();
      setUsers(data);
      setError('');
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    loadUsers();
  }, [loadUsers]);

  async function openEditor(id) {
    setEditingId(id);
    setDetail(null);
    setError('');
    try {
      const data = await api.getUserDetail(id);
      setDetail(data);
    } catch (err) {
      setError(err.message);
      setEditingId(null);
    }
  }

  const filtered = users.filter((u) => {
    const q = search.trim().toLowerCase();
    if (!q) return true;
    return u.username.toLowerCase().includes(q) || String(u.id) === q;
  });

  const editingUser = detail ?? users.find((u) => u.id === editingId) ?? null;

  return (
    <div className="min-h-screen bg-[#050510] text-zinc-50">
      <Header user={user} avatarUrl={avatarUrl} isAdmin={isAdmin} onLogout={onLogout} />
      <div className="mx-auto max-w-4xl space-y-5 px-5 py-6">
        {/* Header */}
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
            <Shield className="h-5 w-5 text-neon-green" />
            User Management
          </h1>
        </div>

        {error && (
          <div className="flex items-center gap-2 rounded-lg border border-red-400/30 bg-red-400/10 px-4 py-3 text-sm text-red-100">
            <AlertCircle className="h-4 w-4 shrink-0" />
            {error}
          </div>
        )}

        {/* Search + refresh */}
        <div className="flex items-center gap-2">
          <div className="relative flex-1 max-w-md">
            <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-zinc-500" />
            <input
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              placeholder="Search username or #id..."
              className="h-10 w-full rounded-lg border border-neon-cyan/[0.08] bg-[#08081a] py-2 pl-10 pr-3 text-sm text-zinc-50 placeholder:text-zinc-500 focus:border-zinc-500 transition"
            />
          </div>
          <button
            type="button"
            onClick={() => loadUsers(true)}
            className="grid h-10 w-10 place-items-center rounded-lg border border-neon-cyan/[0.08] text-mist transition hover:bg-neon-cyan/[0.06] hover:text-zinc-100"
            title="Refresh"
          >
            <RefreshCw className={`h-4 w-4 ${loading ? 'animate-spin' : ''}`} />
          </button>
        </div>

        {/* Editor */}
        {editingId && editingUser && (
          <EditUserForm
            user={editingUser}
            onSaved={() => loadUsers(true)}
            onDone={() => { setEditingId(null); setDetail(null); }}
          />
        )}

        {/* User list */}
        <div className="overflow-hidden rounded-xl border border-neon-cyan/[0.08] bg-[#08081a]">
          <div className="overflow-x-auto thin-scrollbar">
            <table className="min-w-[640px] w-full text-left text-sm">
              <thead className="border-b border-neon-cyan/[0.08] bg-[#0a0a20] text-[11px] uppercase tracking-wider text-zinc-500">
                <tr>
                  <th className="px-4 py-3">User</th>
                  <th className="px-4 py-3">Role</th>
                  <th className="px-4 py-3">Links</th>
                  <th className="px-4 py-3">Created</th>
                  <th className="px-4 py-3 text-right">Actions</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-neon-cyan/10">
                {loading && users.length === 0 ? (
                  <tr><td colSpan={5} className="px-4 py-8 text-center text-sm text-mist">Loading users...</td></tr>
                ) : filtered.length === 0 ? (
                  <tr><td colSpan={5} className="px-4 py-8 text-center text-sm text-mist">No users match your search.</td></tr>
                ) : (
                  filtered.map((u) => (
                    <tr key={u.id} className="transition hover:bg-neon-cyan/[0.03]">
                      <td className="px-4 py-3">
                        <div className="flex items-center gap-2.5">
                          <div className="grid h-8 w-8 place-items-center rounded-lg border border-neon-cyan/[0.12] bg-panelSoft font-semibold text-neon-cyan/80">
                            {u.username.slice(0, 1).toUpperCase()}
                          </div>
                          <div>
                            <div className="font-medium text-zinc-50">{u.username}</div>
                            <div className="text-[11px] text-zinc-500">#{u.id}</div>
                          </div>
                        </div>
                      </td>
                      <td className="px-4 py-3"><RoleBadge role={u.role} /></td>
                      <td className="px-4 py-3"><LinkBadge linked={!!u.discordUserId} /></td>
                      <td className="px-4 py-3 text-xs text-mist">{formatDateTime(u.createdAt)}</td>
                      <td className="px-4 py-3">
                        <div className="flex justify-end">
                          <button
                            type="button"
                            onClick={() => openEditor(u.id)}
                            className="inline-flex min-h-8 items-center gap-1.5 rounded-lg border border-neon-cyan/[0.08] px-3 text-xs font-medium text-zinc-200 transition hover:bg-neon-cyan/[0.06]"
                          >
                            <UserIcon className="h-3.5 w-3.5" />
                            Edit
                          </button>
                        </div>
                      </td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>
        </div>
      </div>
    </div>
  );
}
