import { useState, useEffect, useCallback } from 'react';
import {
  CheckCircle,
  XCircle,
  Clock,
  Trash2,
  Users,
  Filter,
  ExternalLink,
  RefreshCw
} from 'lucide-react';
import { api } from '../lib/api';
import { formatDateTime } from '../lib/format';

const statusStyles = {
  Pending: 'border-amber-400/30 bg-amber-400/10 text-amber-200',
  Approved: 'border-emerald-400/30 bg-emerald-400/10 text-emerald-200',
  Rejected: 'border-red-400/30 bg-red-400/10 text-red-200'
};

const statusIcons = {
  Pending: Clock,
  Approved: CheckCircle,
  Rejected: XCircle
};

function RequestCard({ request, onReview, onDelete, busy }) {
  const StatusIcon = statusIcons[request.status] || Clock;

  return (
    <div className="rounded-lg border border-line bg-panel p-4 shadow-glow transition hover:border-zinc-500">
      <div className="flex items-start justify-between gap-3">
        <div className="min-w-0">
          <div className="flex items-center gap-2 flex-wrap">
            <span className="text-base font-semibold text-zinc-50">{request.robloxUsername}</span>
            <span className={`inline-flex items-center rounded-md border px-1.5 py-0.5 text-[10px] font-semibold uppercase tracking-wider ${
              request.club === 'PIH'
                ? 'border-violet-400/30 bg-violet-400/10 text-violet-200'
                : 'border-amber-400/30 bg-amber-400/10 text-amber-200'
            }`}>
              {request.club}
            </span>
            <span className={`inline-flex items-center gap-1 rounded-md border px-1.5 py-0.5 text-[10px] font-semibold uppercase tracking-wider ${statusStyles[request.status]}`}>
              <StatusIcon className="h-3 w-3" />
              {request.status}
            </span>
          </div>
          <div className="mt-1 text-sm text-mist">
            Roblox ID: #{request.robloxUserId}
            {request.discordUserId && <span className="ml-3">Discord: {request.discordUserId}</span>}
          </div>
          <div className="mt-1 text-xs text-zinc-500">
            Submitted {formatDateTime(request.createdAt)}
          </div>
          {request.note && (
            <div className="mt-2 rounded-md border border-line bg-ink px-3 py-2 text-sm text-zinc-300">
              {request.note}
            </div>
          )}
          {request.reviewedBy && (
            <div className="mt-1 text-xs text-zinc-500">
              Reviewed by {request.reviewedBy} on {formatDateTime(request.reviewedAt)}
            </div>
          )}
        </div>
      </div>

      <div className="mt-3 flex items-center gap-2">
        <a
          href={`https://www.roblox.com/users/${request.robloxUserId}/profile`}
          target="_blank"
          rel="noreferrer"
          className="inline-flex items-center gap-1 rounded-md border border-line px-2.5 py-1.5 text-xs font-medium text-zinc-300 transition hover:bg-zinc-800 hover:text-zinc-100"
        >
          <ExternalLink className="h-3 w-3" />
          Profile
        </a>

        {request.status === 'Pending' && (
          <>
            <button
              type="button"
              onClick={() => onReview(request.id, 'Approved')}
              disabled={busy}
              className="inline-flex items-center gap-1 rounded-md bg-emerald-400/10 border border-emerald-400/30 px-2.5 py-1.5 text-xs font-semibold text-emerald-200 transition hover:bg-emerald-400/20 disabled:opacity-50"
            >
              <CheckCircle className="h-3 w-3" />
              Approve
            </button>
            <button
              type="button"
              onClick={() => onReview(request.id, 'Rejected')}
              disabled={busy}
              className="inline-flex items-center gap-1 rounded-md bg-red-400/10 border border-red-400/30 px-2.5 py-1.5 text-xs font-semibold text-red-200 transition hover:bg-red-400/20 disabled:opacity-50"
            >
              <XCircle className="h-3 w-3" />
              Reject
            </button>
          </>
        )}

        <button
          type="button"
          onClick={() => onDelete(request.id)}
          disabled={busy}
          className="ml-auto inline-flex items-center gap-1 rounded-md border border-line px-2.5 py-1.5 text-xs font-medium text-zinc-400 transition hover:bg-zinc-800 hover:text-red-300 disabled:opacity-50"
        >
          <Trash2 className="h-3 w-3" />
        </button>
      </div>
    </div>
  );
}

export default function AdminPanel() {
  const [requests, setRequests] = useState([]);
  const [filter, setFilter] = useState('Pending');
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');

  const loadRequests = useCallback(async (silent = false) => {
    if (!silent) setLoading(true);
    try {
      const data = await api.getJoinRequests(filter);
      setRequests(data);
      setError('');
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  }, [filter]);

  useEffect(() => {
    loadRequests();
  }, [loadRequests]);

  async function handleReview(id, status) {
    setBusy(true);
    try {
      await api.reviewJoinRequest(id, status);
      await loadRequests(true);
    } catch (err) {
      setError(err.message);
    } finally {
      setBusy(false);
    }
  }

  async function handleDelete(id) {
    if (!window.confirm('Delete this request?')) return;
    setBusy(true);
    try {
      await api.deleteJoinRequest(id);
      await loadRequests(true);
    } catch (err) {
      setError(err.message);
    } finally {
      setBusy(false);
    }
  }

  const pendingCount = requests.filter((r) => r.status === 'Pending').length;

  return (
    <div className="rounded-xl border border-line bg-panel p-5 shadow-glow">
      <div className="flex items-center justify-between mb-4">
        <div className="flex items-center gap-2">
          <Users className="h-5 w-5 text-sky-300" />
          <h2 className="text-lg font-semibold text-zinc-50">Join Requests</h2>
          {pendingCount > 0 && (
            <span className="inline-flex items-center justify-center h-5 min-w-5 rounded-full bg-amber-400 text-[10px] font-bold text-zinc-950 px-1">
              {pendingCount}
            </span>
          )}
        </div>
        <button
          type="button"
          onClick={() => loadRequests(true)}
          className="grid h-8 w-8 place-items-center rounded-md border border-line text-mist transition hover:bg-zinc-800 hover:text-zinc-100"
        >
          <RefreshCw className={`h-4 w-4 ${loading ? 'animate-spin' : ''}`} />
        </button>
      </div>

      {/* Filter tabs */}
      <div className="flex items-center gap-1 mb-4">
        {['Pending', 'Approved', 'Rejected', 'All'].map((status) => (
          <button
            key={status}
            type="button"
            onClick={() => setFilter(status === 'All' ? '' : status)}
            className={`inline-flex items-center gap-1 rounded-md border px-3 py-1.5 text-xs font-semibold transition ${
              (status === 'All' && filter === '') || filter === status
                ? 'border-zinc-500 bg-zinc-700 text-zinc-50'
                : 'border-line text-mist hover:text-zinc-200'
            }`}
          >
            {status}
          </button>
        ))}
      </div>

      {/* Error */}
      {error && (
        <div className="mb-3 rounded-lg border border-red-400/30 bg-red-400/10 px-4 py-2 text-sm text-red-100">
          {error}
        </div>
      )}

      {/* Request list */}
      <div className="space-y-3">
        {loading && requests.length === 0 ? (
          <div className="text-sm text-mist py-4 text-center">Loading...</div>
        ) : requests.length === 0 ? (
          <div className="text-sm text-mist py-4 text-center">No {filter ? filter.toLowerCase() : ''} requests</div>
        ) : (
          requests.map((req) => (
            <RequestCard
              key={req.id}
              request={req}
              onReview={handleReview}
              onDelete={handleDelete}
              busy={busy}
            />
          ))
        )}
      </div>
    </div>
  );
}
