const API_BASE = import.meta.env.VITE_API_BASE_URL ?? '';

function getToken() {
  return localStorage.getItem('token');
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
    localStorage.removeItem('token');
    localStorage.removeItem('role');
    localStorage.removeItem('username');
    window.location.reload();
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
  dashboard: () => request('/api/dashboard'),
  weeklyLeaderboard: () => request('/api/dashboard/leaderboard/weekly'),
  player: (id) => request(`/api/players/${id}`),
  addPlayer: (body) => request('/api/players', { method: 'POST', body: JSON.stringify(body) }),
  deletePlayer: (id) => request(`/api/players/${id}`, { method: 'DELETE' }),
  adjustPlaytime: (id, body) => request(`/api/players/${id}/adjust-playtime`, { method: 'POST', body: JSON.stringify(body) }),
  checkNow: () => request('/api/monitor/check-now', { method: 'POST' }),
  downloadCsv: () => downloadCsv('/api/export/playtime.csv')
};
