const API_BASE = import.meta.env.VITE_API_BASE_URL ?? '';

function getToken() {
  return localStorage.getItem('token');
}

function getRole() {
  return localStorage.getItem('role');
}

function getUsername() {
  return localStorage.getItem('username');
}

function getDiscordUserId() {
  return localStorage.getItem('discordUserId');
}

function isLoggedIn() {
  return !!getToken();
}

function isAdmin() {
  return getRole() === 'Admin';
}

function setAuth(token, role, username, discordUserId = null) {
  localStorage.setItem('token', token);
  localStorage.setItem('role', role);
  localStorage.setItem('username', username);
  if (discordUserId) localStorage.setItem('discordUserId', discordUserId);
  else localStorage.removeItem('discordUserId');
}

function clearAuth() {
  localStorage.removeItem('token');
  localStorage.removeItem('role');
  localStorage.removeItem('username');
  localStorage.removeItem('discordUserId');
}

let onAuthExpired = null;
export function setOnAuthExpired(callback) {
  onAuthExpired = callback;
}

async function request(path, options = {}) {
  const token = getToken();
  const headers = {
    'Content-Type': 'application/json',
    ...options.headers
  };

  if (token) {
    headers['Authorization'] = `Bearer ${token}`;
  }

  const response = await fetch(`${API_BASE}${path}`, {
    ...options,
    headers
  });

  if (response.status === 401) {
    clearAuth();
    if (onAuthExpired) onAuthExpired();
    throw new Error('Session expired. Please log in again.');
  }

  if (!response.ok) {
    let message = `Request failed with ${response.status}`;
    try {
      const problem = await response.json();
      message = problem.message ?? problem.title ?? message;
    } catch {
      // Keep the fallback message.
    }

    throw new Error(message);
  }

  if (response.status === 204) {
    return null;
  }

  return response.json();
}

async function downloadCsv(path) {
  const token = getToken();
  const headers = {};
  if (token) {
    headers['Authorization'] = `Bearer ${token}`;
  }

  const response = await fetch(`${API_BASE}${path}`, { headers });

  if (!response.ok) {
    throw new Error(`Export failed with ${response.status}`);
  }

  const blob = await response.blob();
  const disposition = response.headers.get('Content-Disposition');
  const filename = disposition?.match(/filename=(.+)/)?.[1] ?? 'export.csv';

  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = filename;
  document.body.appendChild(a);
  a.click();
  a.remove();
  URL.revokeObjectURL(url);
}

export const api = {
  // Auth
  login: (username, password) =>
    request('/api/auth/login', {
      method: 'POST',
      body: JSON.stringify({ username, password })
    }),

  register: (body) =>
    request('/api/auth/register', {
      method: 'POST',
      body: JSON.stringify(body)
    }),

  me: () => request('/api/auth/me'),

  // Phase 8: profile page
  myProfile: () => request('/api/profile/me'),
  updateDiscord: (discordUserId) =>
    request('/api/profile/discord', {
      method: 'POST',
      body: JSON.stringify({ discordUserId })
    }),

  // Admin: user management (Phase 8)
  getUserDetail: (id) => request(`/api/admin/users/${id}`),
  updateUser: (id, body) => request(`/api/admin/users/${id}`, { method: 'PUT', body: JSON.stringify(body) }),

  changePassword: (body) =>
    request('/api/auth/change-password', {
      method: 'POST',
      body: JSON.stringify(body)
    }),

  // Roblox account ownership verification (claim existing tracker player)
  verifyStart: (robloxUserId) =>
    request('/api/auth/verify/start', {
      method: 'POST',
      body: JSON.stringify({ robloxUserId })
    }),

  verifyCheck: (verificationId, code) =>
    request('/api/auth/verify/check', {
      method: 'POST',
      body: JSON.stringify({ verificationId, code })
    }),

  playerSearch: (q) => request(`/api/players/search?q=${encodeURIComponent(q ?? '')}`),

  logout: () => {
    clearAuth();
  },

  // Public tracker data
  dashboard: () => request('/api/dashboard'),
  weeklyLeaderboard: () => request('/api/dashboard/leaderboard/weekly'),
  player: (id) => request(`/api/players/${id}`),

  // Admin-only player management
  addPlayer: (body) => request('/api/players', { method: 'POST', body: JSON.stringify(body) }),
  deletePlayer: (id) => request(`/api/players/${id}`, { method: 'DELETE' }),
  adjustPlaytime: (id, body) => request(`/api/players/${id}/adjust-playtime`, { method: 'POST', body: JSON.stringify(body) }),
  updateClub: (id, club) => request(`/api/players/${id}/club`, { method: 'PUT', body: JSON.stringify({ club }) }),
  checkNow: () => request('/api/monitor/check-now', { method: 'POST' }),

  // Join requests (public)
  submitJoinRequest: (body) => request('/api/joinrequest', { method: 'POST', body: JSON.stringify(body) }),
  getMyJoinRequest: (robloxUserId) => request(`/api/joinrequest/mine?robloxUserId=${encodeURIComponent(robloxUserId)}`),
  getMyJoinRequestAuthenticated: () => request('/api/joinrequest/mine-auth'),
  updateJoinRequest: (id, body) => request(`/api/joinrequest/${id}`, { method: 'PUT', body: JSON.stringify(body) }),

  // Join requests (admin)
  getJoinRequests: (status) => request(`/api/joinrequest${status ? `?status=${status}` : ''}`),
  reviewJoinRequest: (id, status) => request(`/api/joinrequest/${id}/review`, { method: 'PUT', body: JSON.stringify({ status }) }),
  deleteJoinRequest: (id) => request(`/api/joinrequest/${id}`, { method: 'DELETE' }),

  // CSV
  downloadCsv: () => downloadCsv('/api/export/playtime.csv'),

  // Helpers
  isLoggedIn,
  isAdmin,
  getRole,
  getUsername,
  getDiscordUserId,
  setAuth,
  clearAuth,
};
