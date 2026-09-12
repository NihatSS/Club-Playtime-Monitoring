import { useCallback, useEffect, useState } from 'react';
import {
  AlertCircle,
  ArrowLeft,
  Ban,
  CheckCircle,
  ChevronRight,
  Clock,
  Crown,
  RefreshCw,
  Save,
  Shield,
  Shuffle,
  Swords,
  Trash2,
  Trophy,
  UserPlus,
  UserMinus,
  Users,
  X
} from 'lucide-react';
import { api } from '../lib/api';
import { formatDateTime } from '../lib/format';
import Header from './Header';
import { StatusBadge } from './TournamentsPage';

const TEAM_MODE_LABELS = {
  Solo: '1v1',
  DuoRandom: '2v2 Random Teams',
  DuoPredefined: '2v2 Predefined Teams'
};

const ROUND_NAMES = {
  1: 'Round 1',
  2: 'Quarter Finals',
  3: 'Semi Finals',
  4: 'Final'
};

// Derive round names from the top: last round = Final, then Semi, then Quarter.
function roundLabel(round, maxRound) {
  const fromTop = maxRound - round;
  if (fromTop === 0) return 'Final';
  if (fromTop === 1) return 'Semi Finals';
  if (fromTop === 2) return 'Quarter Finals';
  return `Round ${round}`;
}

function Avatar({ url, name, size = 'h-8 w-8' }) {
  const initial = name?.slice(0, 1)?.toUpperCase() ?? '?';
  if (url) {
    return <img src={url} alt="" className={`${size} rounded-lg border border-neon-cyan/[0.12] object-cover shrink-0`} />;
  }
  return (
    <div className={`${size} grid shrink-0 place-items-center rounded-lg border border-neon-cyan/[0.12] bg-panelSoft text-xs font-semibold text-neon-cyan/80`}>
      {initial}
    </div>
  );
}

// ─── Bracket rendering ─────────────────────────────────────────

// Bracket column: match cards are laid out in equal "bands" (flex-1 wrappers,
// card centered inside) so every card sits exactly midway between its two
// feeder matches. Connector lines are drawn against those bands: a horizontal
// stub from each card to the column gap, and a vertical spine on the upper
// card of each pair spanning down to its sibling's center (COL_GAP / 2 = 12px).
const BRACKET_COL_GAP = 24; // keep in sync with the gap-6 on the bracket row

function BracketColumn({ title, matches, isLastRound, showChampionStub, isAdmin, onSetWinner, busyMatchId }) {
  return (
    <div className="flex min-w-[240px] flex-1 flex-col">
      <div className="mb-3 text-center text-[11px] font-semibold uppercase tracking-wider text-zinc-500">{title}</div>
      <div className="flex flex-1 flex-col">
        {matches.map((match, index) => {
          const isUpperOfPair = !isLastRound && index % 2 === 0 && index + 1 < matches.length;
          return (
            <div key={match.id} className="relative flex flex-1 flex-col justify-center py-1.5">
              <BracketMatchCard
                match={match}
                isAdmin={isAdmin}
                onSetWinner={onSetWinner}
                busy={busyMatchId === match.id}
              />
              {!isLastRound && (
                <>
                  {/* Horizontal stub: card → column gap midpoint */}
                  <div
                    className="pointer-events-none absolute h-px bg-zinc-600/60"
                    style={{ right: -(BRACKET_COL_GAP / 2), top: '50%', width: BRACKET_COL_GAP / 2 }}
                  />
                  {/* Vertical spine: upper card's center → sibling card's center */}
                  {isUpperOfPair && (
                    <div
                      className="pointer-events-none absolute w-px bg-zinc-600/60"
                      style={{ right: -(BRACKET_COL_GAP / 2), top: '50%', height: '100%' }}
                    />
                  )}
                </>
              )}
              {isLastRound && showChampionStub && (
                /* Champion path: the final keeps a short line pointing right */
                <div
                  className="pointer-events-none absolute h-px bg-zinc-600/60"
                  style={{ right: -(BRACKET_COL_GAP / 2), top: '50%', width: BRACKET_COL_GAP / 2 }}
                />
              )}
            </div>
          );
        })}
        {matches.length === 0 && <div className="text-center text-xs text-zinc-600">—</div>}
      </div>
    </div>
  );
}

function BracketMatchCard({ match, isAdmin, onSetWinner, busy }) {
  const slots = [
    { id: match.participant1Id, name: match.participant1Name, avatar: match.participant1Avatar },
    { id: match.participant2Id, name: match.participant2Name, avatar: match.participant2Avatar }
  ];

  const winnerId = match.winnerId;
  const isBye = match.status === 'COMPLETED' && match.note?.startsWith('BYE');

  return (
    <div
      className={`rounded-lg border p-2 transition ${
        isBye
          ? 'border-dashed border-zinc-700 bg-transparent opacity-70'
          : match.status === 'COMPLETED'
            ? 'border-neon-green/25 bg-neon-green/[0.03]'
            : 'border-neon-cyan/[0.12] bg-[#08081a]'
      }`}
    >
      {slots.map((slot, index) => {
        const isWinner = slot.id != null && winnerId === slot.id;
        const isLoser = winnerId != null && slot.id != null && !isWinner;
        return (
          <div
            key={index}
            className={`flex items-center gap-2 rounded-md px-2 py-1.5 ${
              index === 0 ? 'mb-1' : ''
            } ${
              isWinner
                ? 'bg-emerald-400/10'
                : isLoser
                  ? 'opacity-40'
                  : ''
            }`}
          >
            {slot.id != null ? (
              <>
                <Avatar url={slot.avatar} name={slot.name} size="h-6 w-6" />
                <span className={`min-w-0 flex-1 truncate text-xs font-medium ${isWinner ? 'text-emerald-300' : 'text-zinc-200'}`}>
                  {slot.name}
                </span>
                {isAdmin && match.status !== 'COMPLETED' && slot.id != null && (
                  <button
                    type="button"
                    disabled={busy}
                    onClick={() => onSetWinner(match, slot.id)}
                    className="grid h-5 w-5 shrink-0 place-items-center rounded border border-neon-cyan/20 text-[9px] font-bold text-neon-cyan transition hover:bg-neon-cyan/10 disabled:opacity-40"
                    title="Set as winner"
                  >
                    ✓
                  </button>
                )}
                {isAdmin && match.status === 'COMPLETED' && isWinner && (
                  <span className="shrink-0 text-[10px] font-bold text-emerald-400">WIN</span>
                )}
              </>
            ) : (
              <span className="flex-1 px-2 text-xs italic text-zinc-600">{isBye ? 'BYE' : 'TBD'}</span>
            )}
          </div>
        );
      })}
      {match.note && !isBye && (
        <div className="mt-1 border-t border-neon-cyan/[0.06] px-2 pt-1 text-[10px] text-zinc-500">{match.note}</div>
      )}
    </div>
  );
}


// ─── Info tabs (Rules / Prizes) ───────────────────────────────

function InfoTabs({ tournament }) {
  const [tab, setTab] = useState('rules');

  const rules = (tournament.rulesText ?? tournament.description ?? '')
    .split('\n')
    .map((line) => line.trim())
    .filter(Boolean);
  const description = tournament.description?.trim();

  const prizes = [...tournament.prizes].sort((a, b) => a.placement - b.placement);
  const prizeInfo = tournament.prizeInfo?.trim();
  const placeMedals = { 1: '🥇 1st place', 2: '🥈 2nd place', 3: '🥉 3rd place' };

  return (
    <div className="rounded-xl border border-neon-cyan/[0.08] bg-[#08081a]">
      <div className="flex border-b border-neon-cyan/[0.08]">
        {[
          { id: 'rules', label: 'Rules' },
          { id: 'prizes', label: 'Prizes' }
        ].map((t) => (
          <button
            key={t.id}
            type="button"
            onClick={() => setTab(t.id)}
            className={`px-4 py-2.5 text-sm font-semibold transition ${
              tab === t.id
                ? 'border-b-2 border-neon-cyan text-zinc-50'
                : 'border-b-2 border-transparent text-zinc-500 hover:text-zinc-300'
            }`}
          >
            {t.label}
          </button>
        ))}
      </div>

      <div className="p-4">
        {tab === 'rules' ? (
          rules.length > 0 || description ? (
            <div className="space-y-3">
              {description && <p className="whitespace-pre-line text-sm leading-relaxed text-zinc-300">{description}</p>}
              {rules.length > 0 && (
                <ul className="space-y-1.5">
                  {rules.map((rule, i) => (
                    <li key={i} className="flex items-start gap-2 text-sm text-zinc-300">
                      <span className="mt-[7px] h-1 w-1 shrink-0 rounded-full bg-zinc-500" />
                      <span>{rule.replace(/^[•\-*]\s*/, '')}</span>
                    </li>
                  ))}
                </ul>
              )}
            </div>
          ) : (
            <div className="text-sm text-zinc-500">No rules have been published for this tournament.</div>
          )
        ) : (
          <div className="space-y-2">
            {prizes.map((prize) => (
              <div
                key={prize.placement}
                className="flex items-center gap-3 rounded-lg border border-neon-cyan/[0.08] bg-ink px-3 py-2.5"
              >
                <span className="w-24 shrink-0 text-xs font-semibold uppercase tracking-wider text-zinc-400">
                  {placeMedals[prize.placement] ?? `#${prize.placement}`}
                </span>
                <span className="min-w-0 flex-1 text-sm text-zinc-100">{prize.description}</span>
              </div>
            ))}
            {prizes.length === 0 && prizeInfo && (
              <div className="text-sm text-zinc-300">{prizeInfo}</div>
            )}
            {prizes.length === 0 && !prizeInfo && (
              <div className="text-sm text-zinc-500">No prizes have been announced for this tournament.</div>
            )}
          </div>
        )}
      </div>
    </div>
  );
}

// ─── Admin panel components ────────────────────────────────────

function AdminSection({ title, icon: Icon, children, defaultOpen = false }) {
  const [open, setOpen] = useState(defaultOpen);
  return (
    <div className="rounded-xl border border-neon-cyan/[0.12] bg-[#08081a]">
      <button
        type="button"
        onClick={() => setOpen((o) => !o)}
        className="flex w-full items-center justify-between px-4 py-3 text-left"
      >
        <span className="flex items-center gap-2 text-sm font-semibold text-zinc-100">
          <Icon className="h-4 w-4 text-neon-amber" />
          {title}
        </span>
        <ChevronRight className={`h-4 w-4 text-zinc-500 transition ${open ? 'rotate-90' : ''}`} />
      </button>
      {open && <div className="border-t border-neon-cyan/[0.08] p-4">{children}</div>}
    </div>
  );
}

function StatusControls({ tournament, onAction, busy }) {
  const transitions = [];
  if (['DRAFT', 'REGISTRATION_CLOSED'].includes(tournament.status)) {
    transitions.push({ status: 'REGISTRATION_OPEN', label: 'Open registration', class: 'bg-emerald-400/10 border-emerald-400/30 text-emerald-300' });
  }
  if (tournament.status === 'REGISTRATION_OPEN') {
    transitions.push({ status: 'REGISTRATION_CLOSED', label: 'Close registration', class: 'bg-amber-400/10 border-amber-400/30 text-amber-300' });
  }
  if (['REGISTRATION_CLOSED', 'DRAFT'].includes(tournament.status)) {
    transitions.push({ status: 'UPCOMING', label: 'Mark upcoming', class: 'bg-neon-cyan/10 border-neon-cyan/30 text-neon-cyan' });
  }

  return (
    <div className="flex flex-wrap items-center gap-2">
      <StatusBadge status={tournament.status} />
      {transitions.map((t) => (
        <button
          key={t.status}
          type="button"
          disabled={busy}
          onClick={() => onAction(() => api.setTournamentStatus(tournament.id, t.status), `${t.label} ✓`)}
          className={`inline-flex min-h-8 items-center rounded-lg border px-3 text-xs font-semibold transition disabled:opacity-50 ${t.class}`}
        >
          {t.label}
        </button>
      ))}
      {tournament.status !== 'CANCELLED' && tournament.status !== 'COMPLETED' && (
        <button
          type="button"
          disabled={busy}
          onClick={() => {
            if (window.confirm('Cancel this tournament? It cannot be reopened.')) {
              onAction(() => api.cancelTournament(tournament.id), 'Tournament cancelled');
            }
          }}
          className="inline-flex min-h-8 items-center rounded-lg border border-red-400/30 bg-red-400/10 px-3 text-xs font-semibold text-red-300 transition disabled:opacity-50"
        >
          Cancel tournament
        </button>
      )}
    </div>
  );
}

function ParticipantAdmin({ tournament, players, onAction, busy }) {
  const [selectedPlayer, setSelectedPlayer] = useState('');
  const [teamName, setTeamName] = useState('');
  const activeParticipants = tournament.participants.filter((p) => !p.isDisqualified);
  const unassigned = tournament.teamMode !== 'Solo'
    ? activeParticipants.filter((p) => p.teamId == null)
    : [];

  async function addPlayer() {
    if (!selectedPlayer) return;
    await onAction(() => api.adminAddParticipant(tournament.id, Number(selectedPlayer)), 'Player added');
    setSelectedPlayer('');
  }

  async function createTeam() {
    if (!teamName.trim()) return;
    await onAction(() => api.createTeam(tournament.id, teamName.trim()), 'Team created');
    setTeamName('');
  }

  return (
    <div className="space-y-4">
      {/* Add participant */}
      <div>
        <div className="mb-2 text-xs font-semibold uppercase tracking-wider text-zinc-500">Add participant</div>
        <div className="flex gap-2">
          <select
            value={selectedPlayer}
            onChange={(e) => setSelectedPlayer(e.target.value)}
            className="min-h-9 flex-1 rounded-lg border border-neon-cyan/[0.08] bg-ink px-3 text-sm text-zinc-50"
          >
            <option value="">Select a tracker player...</option>
            {players.map((p) => (
              <option key={p.id} value={p.id}>
                {p.username} (#{p.id})
              </option>
            ))}
          </select>
          <button
            type="button"
            onClick={addPlayer}
            disabled={busy || !selectedPlayer}
            className="inline-flex min-h-9 items-center gap-1.5 rounded-lg bg-neon-cyan px-3 text-xs font-semibold text-zinc-950 transition hover:bg-neon-cyan/80 disabled:opacity-50"
          >
            <UserPlus className="h-3.5 w-3.5" />
            Add
          </button>
        </div>
      </div>

      {/* Participant list */}
      <div className="space-y-1.5">
        {tournament.participants.map((p) => (
          <div key={p.id} className="flex items-center gap-2 rounded-lg border border-neon-cyan/[0.08] bg-ink px-3 py-2">
            <Avatar url={p.avatarUrl} name={p.username} size="h-7 w-7" />
            <span className={`min-w-0 flex-1 truncate text-sm ${p.isDisqualified ? 'text-zinc-500 line-through' : 'text-zinc-100'}`}>
              {p.username}
              {p.teamId != null && (
                <span className="ml-2 text-[10px] text-neon-cyan">
                  {tournament.teams.find((t) => t.id === p.teamId)?.name ?? 'team'}
                </span>
              )}
            </span>
            {p.isDisqualified && (
              <span className="shrink-0 text-[10px] font-semibold uppercase text-red-300" title={p.disqualifiedReason ?? ''}>
                DQ
              </span>
            )}
            {tournament.teamMode !== 'Solo' && p.teamId != null && tournament.status !== 'IN_PROGRESS' && (
              <button
                type="button"
                disabled={busy}
                onClick={() => onAction(() => api.removeTeamMember(tournament.id, p.teamId, p.id), 'Removed from team')}
                className="grid h-6 w-6 place-items-center rounded border border-neon-cyan/[0.12] text-zinc-400 transition hover:bg-neon-cyan/[0.06]"
                title="Remove from team"
              >
                <UserMinus className="h-3 w-3" />
              </button>
            )}
            {p.isDisqualified ? (
              <button
                type="button"
                disabled={busy}
                onClick={() => onAction(() => api.requalifyParticipant(tournament.id, p.id), 'Reinstated')}
                className="inline-flex min-h-6 items-center rounded border border-emerald-400/30 px-2 text-[10px] font-semibold text-emerald-300 transition disabled:opacity-50"
              >
                Reinstate
              </button>
            ) : (
              <>
                <button
                  type="button"
                  disabled={busy}
                  onClick={() => {
                    const reason = window.prompt(`Disqualify ${p.username}? Reason (optional):`);
                    if (reason === null) return;
                    onAction(() => api.disqualifyParticipant(tournament.id, p.id, reason), 'Participant disqualified');
                  }}
                  className="grid h-6 w-6 place-items-center rounded border border-red-400/20 text-red-300 transition hover:bg-red-400/10 disabled:opacity-50"
                  title="Disqualify"
                >
                  <Ban className="h-3 w-3" />
                </button>
                <button
                  type="button"
                  disabled={busy}
                  onClick={() => {
                    if (window.confirm(`Remove ${p.username} from the tournament?`)) {
                      onAction(() => api.removeTournamentParticipant(tournament.id, p.id), 'Participant removed');
                    }
                  }}
                  className="grid h-6 w-6 place-items-center rounded border border-red-400/20 text-red-300 transition hover:bg-red-400/10 disabled:opacity-50"
                  title="Remove from tournament"
                >
                  <Trash2 className="h-3 w-3" />
                </button>
              </>
            )}
          </div>
        ))}
        {tournament.participants.length === 0 && (
          <div className="rounded-lg border border-dashed border-zinc-700 px-3 py-4 text-center text-xs text-zinc-500">
            No participants yet.
          </div>
        )}
      </div>

      {/* 2v2 team controls */}
      {tournament.teamMode !== 'Solo' && (
        <div className="space-y-3 border-t border-neon-cyan/[0.08] pt-3">
          <div className="flex items-center justify-between">
            <div className="text-xs font-semibold uppercase tracking-wider text-zinc-500">Teams</div>
            {tournament.teamMode === 'DuoRandom' && (
              <button
                type="button"
                disabled={busy}
                onClick={() => {
                  if (window.confirm('Randomly pair all unassigned players into teams of 2?')) {
                    onAction(() => api.createRandomTeams(tournament.id), 'Random teams created');
                  }
                }}
                className="inline-flex min-h-8 items-center gap-1.5 rounded-lg border border-neon-purple/30 bg-neon-purple/10 px-3 text-xs font-semibold text-neon-purple transition disabled:opacity-50"
              >
                <Shuffle className="h-3.5 w-3.5" />
                Generate random teams
              </button>
            )}
          </div>

          {tournament.teamMode === 'DuoPredefined' && (
            <div className="flex gap-2">
              <input
                value={teamName}
                onChange={(e) => setTeamName(e.target.value)}
                placeholder="New team name"
                maxLength={100}
                className="min-h-9 flex-1 rounded-lg border border-neon-cyan/[0.08] bg-ink px-3 text-sm text-zinc-50 placeholder:text-zinc-500"
              />
              <button
                type="button"
                onClick={createTeam}
                disabled={busy || !teamName.trim()}
                className="inline-flex min-h-9 items-center rounded-lg bg-neon-cyan px-3 text-xs font-semibold text-zinc-950 transition hover:bg-neon-cyan/80 disabled:opacity-50"
              >
                Create
              </button>
            </div>
          )}

          {/* Unassigned players */}
          {unassigned.length > 0 && (
            <div className="rounded-lg border border-amber-400/20 bg-amber-400/[0.04] p-2">
              <div className="mb-1 text-[10px] font-semibold uppercase tracking-wider text-amber-300">
                Unassigned players ({unassigned.length})
              </div>
              <div className="flex flex-wrap gap-1.5">
                {unassigned.map((p) => (
                  <span key={p.id} className="inline-flex items-center gap-1 rounded-md border border-line bg-ink px-2 py-1 text-xs text-zinc-200">
                    {p.username}
                  </span>
                ))}
              </div>
            </div>
          )}

          <div className="grid gap-2 sm:grid-cols-2">
            {tournament.teams.map((team) => (
              <div key={team.id} className="rounded-lg border border-neon-cyan/[0.12] bg-ink p-2.5">
                <div className="flex items-center justify-between gap-2">
                  <span className="truncate text-sm font-semibold text-zinc-100">{team.name}</span>
                  {tournament.status !== 'IN_PROGRESS' && (
                    <button
                      type="button"
                      disabled={busy}
                      onClick={() => {
                        if (window.confirm(`Delete team '${team.name}'?`)) {
                          onAction(() => api.deleteTeam(tournament.id, team.id), 'Team deleted');
                        }
                      }}
                      className="grid h-6 w-6 shrink-0 place-items-center rounded border border-red-400/20 text-red-300 transition hover:bg-red-400/10 disabled:opacity-50"
                    >
                      <Trash2 className="h-3 w-3" />
                    </button>
                  )}
                </div>
                <div className="mt-2 space-y-1">
                  {team.members.map((m) => (
                    <div key={m.id} className="flex items-center gap-1.5 text-xs text-zinc-300">
                      <Avatar url={m.avatarUrl} name={m.username} size="h-5 w-5" />
                      <span className="flex-1 truncate">{m.username}</span>
                    </div>
                  ))}
                  {team.members.length < 2 && unassigned.length > 0 && tournament.status !== 'IN_PROGRESS' && (
                    <select
                      value=""
                      onChange={(e) => {
                        if (e.target.value) {
                          onAction(() => api.assignTeamMember(tournament.id, team.id, Number(e.target.value)), 'Player assigned');
                        }
                      }}
                      disabled={busy}
                      className="mt-1 w-full rounded-md border border-neon-cyan/[0.12] bg-panel px-2 py-1 text-xs text-zinc-200"
                    >
                      <option value="">+ Add player...</option>
                      {unassigned.map((p) => (
                        <option key={p.id} value={p.id}>{p.username}</option>
                      ))}
                    </select>
                  )}
                </div>
              </div>
            ))}
          </div>
          {tournament.teams.length === 0 && (
            <div className="rounded-lg border border-dashed border-zinc-700 px-3 py-3 text-center text-xs text-zinc-500">
              {tournament.teamMode === 'DuoRandom'
                ? 'Use "Generate random teams" to pair registered players.'
                : 'Create teams above, then assign players.'}
            </div>
          )}
        </div>
      )}
    </div>
  );
}

function BracketAdmin({ tournament, onAction, busy }) {
  const hasBracket = tournament.matches.length > 0;
  const canGenerate = ['DRAFT', 'REGISTRATION_OPEN', 'REGISTRATION_CLOSED', 'UPCOMING'].includes(tournament.status);

  if (!hasBracket) {
    return (
      <div className="space-y-3">
        <p className="text-sm text-mist">
          Generate the bracket to start the tournament. {tournament.teamMode === 'Solo'
            ? 'All active participants'
            : 'All teams'} will be shuffled into a single-elimination bracket (BYEs are added automatically for non-power-of-two counts).
        </p>
        <button
          type="button"
          disabled={busy || !canGenerate}
          onClick={() => {
            if (window.confirm('Generate the bracket now? This starts the tournament.')) {
              onAction(() => api.generateBracket(tournament.id), 'Bracket generated — tournament started');
            }
          }}
          className="inline-flex min-h-10 items-center gap-2 rounded-lg bg-neon-cyan px-4 text-sm font-semibold text-zinc-950 transition hover:bg-neon-cyan/80 disabled:opacity-50"
        >
          <Swords className="h-4 w-4" />
          Generate bracket & start
        </button>
      </div>
    );
  }

  return (
    <div className="space-y-2">
      <p className="text-sm text-mist">Bracket exists ({tournament.matches.length} matches). Click ✓ next to a participant in the bracket to record winners.</p>
      {tournament.status === 'IN_PROGRESS' && (
        <button
          type="button"
          disabled={busy}
          onClick={() => {
            if (window.confirm('Delete the bracket? All results will be lost and the tournament returns to pre-start state.')) {
              onAction(() => api.deleteBracket(tournament.id), 'Bracket deleted');
            }
          }}
          className="inline-flex min-h-8 items-center gap-1.5 rounded-lg border border-red-400/30 bg-red-400/10 px-3 text-xs font-semibold text-red-300 transition disabled:opacity-50"
        >
          <Trash2 className="h-3 w-3" />
          Delete bracket (reset)
        </button>
      )}
    </div>
  );
}

function EditDetailsForm({ tournament, onAction, busy }) {
  const [form, setForm] = useState({
    name: tournament.name,      description: tournament.description ?? '',
      rulesText: tournament.rulesText ?? '',
    registrationStartsAt: toLocalInput(tournament.registrationStartsAt),
    registrationDeadline: toLocalInput(tournament.registrationDeadline),
    startsAt: toLocalInput(tournament.startsAt),
    maxParticipants: tournament.maxParticipants,
    prizeInfo: tournament.prizeInfo ?? '',
    prize1: tournament.prizes.find((p) => p.placement === 1)?.description ?? '',
    prize2: tournament.prizes.find((p) => p.placement === 2)?.description ?? '',
    prize3: tournament.prizes.find((p) => p.placement === 3)?.description ?? ''
  });

  async function submit(event) {
    event.preventDefault();
    const prizes = [];
    if (form.prize1.trim()) prizes.push({ placement: 1, description: form.prize1.trim() });
    if (form.prize2.trim()) prizes.push({ placement: 2, description: form.prize2.trim() });
    if (form.prize3.trim()) prizes.push({ placement: 3, description: form.prize3.trim() });

    await onAction(
      () =>
        api.updateTournament(tournament.id, {
          name: form.name.trim(),
          description: form.description.trim(),
          rulesText: form.rulesText.trim(),
          registrationStartsAt: form.registrationStartsAt ? new Date(form.registrationStartsAt).toISOString() : null,
          registrationDeadline: form.registrationDeadline ? new Date(form.registrationDeadline).toISOString() : null,
          startsAt: form.startsAt ? new Date(form.startsAt).toISOString() : null,
          maxParticipants: Number(form.maxParticipants) || 0,
          prizeInfo: form.prizeInfo.trim(),
          prizes
        }),
      'Tournament updated'
    );
  }

  const inputClass = 'w-full min-h-9 rounded-lg border border-neon-cyan/[0.08] bg-ink px-3 text-sm text-zinc-50 placeholder:text-zinc-500';

  return (
    <form onSubmit={submit} className="space-y-3">
      <input
        value={form.name}
        onChange={(e) => setForm((c) => ({ ...c, name: e.target.value }))}
        className={inputClass}
        required
        maxLength={120}
        placeholder="Tournament name"
      />
      <textarea
        value={form.description}
        onChange={(e) => setForm((c) => ({ ...c, description: e.target.value }))}
        className={`${inputClass} min-h-16`}
        maxLength={4000}
        placeholder="Description"
      />
      <textarea
        value={form.rulesText}
        onChange={(e) => setForm((c) => ({ ...c, rulesText: e.target.value }))}
        className={`${inputClass} min-h-20`}
        maxLength={4000}
        placeholder={'Rules (one per line)\nAwakenings r not allowed\nCheating is not allowed'}
      />
      <div className="grid gap-2 sm:grid-cols-3">
        <label className="text-xs text-zinc-400">
          Registration opens
          <input
            type="datetime-local"
            value={form.registrationStartsAt}
            onChange={(e) => setForm((c) => ({ ...c, registrationStartsAt: e.target.value }))}
            className={`${inputClass} mt-1`}
          />
        </label>
        <label className="text-xs text-zinc-400">
          Registration deadline
          <input
            type="datetime-local"
            value={form.registrationDeadline}
            onChange={(e) => setForm((c) => ({ ...c, registrationDeadline: e.target.value }))}
            className={`${inputClass} mt-1`}
          />
        </label>
        <label className="text-xs text-zinc-400">
          Tournament start
          <input
            type="datetime-local"
            value={form.startsAt}
            onChange={(e) => setForm((c) => ({ ...c, startsAt: e.target.value }))}
            className={`${inputClass} mt-1`}
          />
        </label>
      </div>
      <div className="grid gap-2 sm:grid-cols-2">
        <label className="text-xs text-zinc-400">
          Max participants (0 = unlimited)
          <input
            type="number"
            min={0}
            value={form.maxParticipants}
            onChange={(e) => setForm((c) => ({ ...c, maxParticipants: e.target.value }))}
            className={`${inputClass} mt-1`}
          />
        </label>
        <label className="text-xs text-zinc-400">
          Prize summary
          <input
            value={form.prizeInfo}
            onChange={(e) => setForm((c) => ({ ...c, prizeInfo: e.target.value }))}
            className={`${inputClass} mt-1`}
            maxLength={500}
          />
        </label>
      </div>
      <div className="grid gap-2 sm:grid-cols-3">
        <input value={form.prize1} onChange={(e) => setForm((c) => ({ ...c, prize1: e.target.value }))} placeholder="🥇 1st place prize" className={inputClass} maxLength={300} />
        <input value={form.prize2} onChange={(e) => setForm((c) => ({ ...c, prize2: e.target.value }))} placeholder="🥈 2nd place prize" className={inputClass} maxLength={300} />
        <input value={form.prize3} onChange={(e) => setForm((c) => ({ ...c, prize3: e.target.value }))} placeholder="🥉 3rd place prize" className={inputClass} maxLength={300} />
      </div>
      <button
        type="submit"
        disabled={busy}
        className="inline-flex min-h-9 items-center gap-2 rounded-lg bg-neon-cyan px-4 text-sm font-semibold text-zinc-950 transition hover:bg-neon-cyan/80 disabled:opacity-60"
      >
        <Save className="h-4 w-4" />
        {busy ? 'Saving...' : 'Save changes'}
      </button>
    </form>
  );
}

function toLocalInput(iso) {
  if (!iso) return '';
  const d = new Date(iso);
  const pad = (n) => String(n).padStart(2, '0');
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`;
}

// ─── Main page ─────────────────────────────────────────────────

export default function TournamentDetailPage({ tournamentId, user, avatarUrl, isAdmin, onLogout, onSignIn }) {
  const [tournament, setTournament] = useState(null);
  const [players, setPlayers] = useState([]);
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');
  const [registering, setRegistering] = useState(false);

  const load = useCallback(async () => {
    try {
      const data = await api.tournament(tournamentId);
      setTournament(data);
      setError('');
    } catch (err) {
      setError(err.message);
      setTournament(null);
    } finally {
      setLoading(false);
    }
  }, [tournamentId]);

  useEffect(() => {
    load();
  }, [load]);

  useEffect(() => {
    if (!isAdmin) return;
    api
      .dashboard()
      .then((data) => setPlayers(data.map((p) => ({ id: p.id, username: p.username }))))
      .catch(() => {});
  }, [isAdmin]);

  const runAction = useCallback(
    async (action, successMessage) => {
      setBusy(true);
      setError('');
      setNotice('');
      try {
        const result = await action();
        setNotice(result?.message ?? successMessage ?? 'Done.');
        await load();
        return true;
      } catch (err) {
        setError(err.message);
        return false;
      } finally {
        setBusy(false);
      }
    },
    [load]
  );

  async function handleRegister() {
    setRegistering(true);
    setError('');
    setNotice('');
    try {
      const profile = await api.myProfile();
      const playerId = profile?.player?.id;
      if (!playerId) {
        setError('Your account is not linked to a tracker player yet. Link one on your profile first.');
        return;
      }
      await api.registerForTournament(tournamentId, playerId);
      setNotice('Registered successfully!');
      await load();
    } catch (err) {
      setError(err.message);
    } finally {
      setRegistering(false);
    }
  }

  async function handleSetWinner(match, winnerParticipantId) {
    setBusy(true);
    setError('');
    try {
      await api.setMatchWinner(tournamentId, match.id, winnerParticipantId);
      await load();
    } catch (err) {
      // 409 = cascade confirmation required (match already completed & advanced).
      setError(err.message);
    } finally {
      setBusy(false);
    }
  }

  if (loading) {
    return (
      <div className="min-h-screen bg-[#050510] text-zinc-50">
        <Header user={user} avatarUrl={avatarUrl} isAdmin={isAdmin} onSignIn={onSignIn} onLogout={onLogout} />
        <div className="mx-auto max-w-6xl px-5 py-8 text-sm text-mist">Loading tournament...</div>
      </div>
    );
  }

  if (!tournament) {
    return (
      <div className="min-h-screen bg-[#050510] text-zinc-50">
        <Header user={user} avatarUrl={avatarUrl} isAdmin={isAdmin} onSignIn={onSignIn} onLogout={onLogout} />
        <div className="mx-auto max-w-6xl space-y-4 px-5 py-8">
          <button
            type="button"
            onClick={() => { window.location.hash = 'tournaments'; }}
            className="inline-flex items-center gap-1.5 text-sm text-mist hover:text-zinc-200"
          >
            <ArrowLeft className="h-4 w-4" /> All tournaments
          </button>
          <div className="rounded-lg border border-red-400/30 bg-red-400/10 px-4 py-3 text-sm text-red-100">{error || 'Tournament not found.'}</div>
        </div>
      </div>
    );
  }

  const canRegister =
    user &&
    !tournament.isRegistered &&
    ['REGISTRATION_OPEN', 'REGISTRATION_CLOSED', 'DRAFT', 'UPCOMING'].includes(tournament.status) &&
    new Date(tournament.registrationStartsAt).getTime() <= Date.now() &&
    Date.now() <= new Date(tournament.registrationDeadline).getTime();

  const maxRound = Math.max(0, ...tournament.matches.map((m) => m.round));
  const rounds = [];
  for (let r = 1; r <= maxRound; r++) {
    rounds.push(tournament.matches.filter((m) => m.round === r).sort((a, b) => a.slot - b.slot));
  }

  return (
    <div className="min-h-screen bg-[#050510] text-zinc-50">
      <Header user={user} avatarUrl={avatarUrl} isAdmin={isAdmin} onSignIn={onSignIn} onLogout={onLogout} />
      <div className="mx-auto max-w-6xl space-y-5 px-5 py-6">
        {/* Header */}
        <div className="flex flex-wrap items-center justify-between gap-3">
          <button
            type="button"
            onClick={() => { window.location.hash = 'tournaments'; }}
            className="inline-flex items-center gap-1.5 rounded-lg border border-neon-cyan/[0.08] px-3 py-2 text-sm font-medium text-zinc-300 transition hover:bg-neon-cyan/[0.06]"
          >
            <ArrowLeft className="h-4 w-4" />
            All tournaments
          </button>
          <div className="flex flex-wrap items-center gap-2">
            <StatusBadge status={tournament.status} />
            <span className="inline-flex items-center rounded-md border border-line bg-ink px-2 py-0.5 text-[10px] font-semibold uppercase tracking-wider text-zinc-300">
              {TEAM_MODE_LABELS[tournament.teamMode] ?? tournament.teamMode}
            </span>
          </div>
        </div>

        <div>
          <h1 className="text-2xl font-bold text-zinc-50">{tournament.name}</h1>
          {tournament.description && <p className="mt-1 max-w-3xl whitespace-pre-line text-sm text-mist">{tournament.description}</p>}
        </div>

        {error && (
          <div className="flex items-start gap-2 rounded-lg border border-red-400/30 bg-red-400/10 px-4 py-3 text-sm text-red-100">
            <AlertCircle className="mt-0.5 h-4 w-4 shrink-0" />
            <span>{error}</span>
            <button type="button" onClick={() => setError('')} className="ml-auto text-zinc-400 hover:text-zinc-200"><X className="h-4 w-4" /></button>
          </div>
        )}
        {notice && (
          <div className="flex items-start gap-2 rounded-lg border border-emerald-400/30 bg-emerald-400/10 px-4 py-3 text-sm text-emerald-100">
            <CheckCircle className="mt-0.5 h-4 w-4 shrink-0" />
            <span>{notice}</span>
            <button type="button" onClick={() => setNotice('')} className="ml-auto text-zinc-400 hover:text-zinc-200"><X className="h-4 w-4" /></button>
          </div>
        )}

        {/* Schedule summary (prizes moved into the Rules/Prizes tabs below) */}
        <div className="grid gap-3 sm:grid-cols-3">
          <div className="rounded-xl border border-neon-cyan/[0.08] bg-[#08081a] p-4">
            <div className="flex items-center gap-1.5 text-[10px] font-semibold uppercase tracking-wider text-zinc-500"><Clock className="h-3 w-3" /> Starts</div>
            <div className="mt-1 text-sm font-semibold text-zinc-50">{formatDateTime(tournament.startsAt)}</div>
          </div>
          <div className="rounded-xl border border-neon-cyan/[0.08] bg-[#08081a] p-4">
            <div className="flex items-center gap-1.5 text-[10px] font-semibold uppercase tracking-wider text-zinc-500"><Clock className="h-3 w-3" /> Registration deadline</div>
            <div className="mt-1 text-sm font-semibold text-zinc-50">{formatDateTime(tournament.registrationDeadline)}</div>
          </div>
          <div className="rounded-xl border border-neon-cyan/[0.08] bg-[#08081a] p-4">
            <div className="flex items-center gap-1.5 text-[10px] font-semibold uppercase tracking-wider text-zinc-500"><Users className="h-3 w-3" /> Participants</div>
            <div className="mt-1 text-sm font-semibold text-zinc-50">
              {tournament.participants.filter((p) => !p.isDisqualified).length}
              {tournament.maxParticipants > 0 ? ` / ${tournament.maxParticipants}` : ''}
            </div>
          </div>
        </div>

        {/* Rules & prizes tabs */}
        <InfoTabs tournament={tournament} />

        {/* Registration CTA */}
        {user && ['REGISTRATION_OPEN', 'DRAFT', 'UPCOMING', 'REGISTRATION_CLOSED'].includes(tournament.status) && (
          <div className="flex flex-wrap items-center justify-between gap-3 rounded-xl border border-neon-cyan/[0.12] bg-[#08081a] p-4">
            <div className="text-sm text-mist">
              {tournament.isRegistered
                ? '✅ You are registered for this tournament.'
                : canRegister
                  ? 'Registration is open — secure your spot!'
                  : 'Registration is not open right now.'}
            </div>
            {tournament.isRegistered ? (
              <span className="inline-flex items-center gap-1.5 rounded-lg border border-emerald-400/30 bg-emerald-400/10 px-4 py-2 text-sm font-semibold text-emerald-300">
                <CheckCircle className="h-4 w-4" /> Registered
              </span>
            ) : (
              <button
                type="button"
                onClick={handleRegister}
                disabled={!canRegister || registering}
                className="inline-flex min-h-10 items-center gap-2 rounded-lg bg-neon-green px-4 text-sm font-semibold text-zinc-950 transition hover:bg-neon-green/80 disabled:cursor-not-allowed disabled:opacity-40"
              >
                <UserPlus className="h-4 w-4" />
                {registering ? 'Registering...' : 'Register'}
              </button>
            )}
          </div>
        )}
        {!user && ['REGISTRATION_OPEN'].includes(tournament.status) && (
          <div className="rounded-xl border border-neon-cyan/[0.12] bg-[#08081a] p-4 text-sm text-mist">
            <button type="button" onClick={onSignIn} className="font-semibold text-neon-cyan hover:text-neon-cyan/80">
              Sign in
            </button>{' '}
            to register for this tournament.
          </div>
        )}

        {/* Results banner for completed tournaments */}
        {tournament.results && (
          <div className="rounded-xl border border-amber-400/30 bg-amber-400/[0.06] p-5">
            <div className="mb-3 flex items-center gap-2 text-sm font-semibold text-amber-300">
              <Crown className="h-4 w-4" />
              Final results
            </div>
            <div className="grid gap-2 sm:grid-cols-3">
              <div className="rounded-lg border border-amber-400/30 bg-ink p-3">
                <div className="text-[10px] font-semibold uppercase tracking-wider text-amber-300">🥇 1st place</div>
                <div className="mt-1 text-base font-bold text-zinc-50">{tournament.results.firstPlace ?? '—'}</div>
                <div className="text-xs text-mist">{tournament.prizes.find((p) => p.placement === 1)?.description ?? ''}</div>
              </div>
              <div className="rounded-lg border border-zinc-500/30 bg-ink p-3">
                <div className="text-[10px] font-semibold uppercase tracking-wider text-zinc-300">🥈 2nd place</div>
                <div className="mt-1 text-base font-bold text-zinc-50">{tournament.results.secondPlace ?? '—'}</div>
                <div className="text-xs text-mist">{tournament.prizes.find((p) => p.placement === 2)?.description ?? ''}</div>
              </div>
              <div className="rounded-lg border border-amber-700/30 bg-ink p-3">
                <div className="text-[10px] font-semibold uppercase tracking-wider text-amber-600">🥉 3rd place</div>
                <div className="mt-1 text-base font-bold text-zinc-50">{tournament.results.thirdPlace ?? '—'}</div>
                <div className="text-xs text-mist">{tournament.prizes.find((p) => p.placement === 3)?.description ?? ''}</div>
              </div>
            </div>
          </div>
        )}

        {/* Admin controls */}
        {isAdmin && (
          <div className="space-y-3">
            <div className="flex items-center gap-2 text-xs font-semibold uppercase tracking-wider text-zinc-500">
              <Shield className="h-3.5 w-3.5 text-neon-green" />
              Admin controls
            </div>
            <AdminSection title="Status & lifecycle" icon={Shield}>
              <StatusControls tournament={tournament} onAction={runAction} busy={busy} />
            </AdminSection>
            <AdminSection title="Participants & teams" icon={Users}>
              <ParticipantAdmin tournament={tournament} players={players} onAction={runAction} busy={busy} />
            </AdminSection>
            <AdminSection title="Bracket" icon={Swords}>
              <BracketAdmin tournament={tournament} onAction={runAction} busy={busy} />
            </AdminSection>
            <AdminSection title="Edit details & prizes" icon={Trophy}>
              <EditDetailsForm tournament={tournament} onAction={runAction} busy={busy} />
            </AdminSection>
          </div>
        )}

        {/* Teams */}
        {tournament.teamMode !== 'Solo' && tournament.teams.length > 0 && (
          <div>
            <h2 className="mb-3 flex items-center gap-2 text-lg font-semibold text-zinc-50">
              <Users className="h-5 w-5 text-neon-cyan" />
              Teams
            </h2>
            <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
              {tournament.teams.map((team) => (
                <div key={team.id} className="rounded-xl border border-neon-cyan/[0.08] bg-[#08081a] p-4">
                  <div className="flex items-center justify-between gap-2">
                    <span className="font-semibold text-zinc-50">{team.name}</span>
                    {team.isRandomTeam && (
                      <span className="rounded-md border border-neon-purple/30 bg-neon-purple/10 px-1.5 py-0.5 text-[9px] font-semibold uppercase tracking-wider text-neon-purple">
                        Random
                      </span>
                    )}
                  </div>
                  <div className="mt-2 space-y-1.5">
                    {team.members.map((m) => (
                      <div key={m.id} className="flex items-center gap-2">
                        <Avatar url={m.avatarUrl} name={m.username} size="h-7 w-7" />
                        <span className="truncate text-sm text-zinc-200">{m.username}</span>
                      </div>
                    ))}
                  </div>
                </div>
              ))}
            </div>
          </div>
        )}

        {/* Bracket */}
        {tournament.matches.length > 0 && (
          <div>
            <h2 className="mb-3 flex items-center gap-2 text-lg font-semibold text-zinc-50">
              <Swords className="h-5 w-5 text-neon-purple" />
              Bracket
            </h2>
            <div className="overflow-x-auto thin-scrollbar rounded-xl border border-neon-cyan/[0.08] bg-[#0a0a1a] p-4 pr-7">
              <div className="flex min-w-max gap-6">
                {rounds.map((roundMatches, index) => (
                  <BracketColumn
                    key={index}
                    title={roundLabel(index + 1, maxRound)}
                    matches={roundMatches}
                    isLastRound={index === rounds.length - 1}
                    showChampionStub={rounds.length > 1}
                    isAdmin={isAdmin && tournament.status === 'IN_PROGRESS'}
                    onSetWinner={handleSetWinner}
                    busyMatchId={busy ? busy : null}
                  />
                ))}
              </div>
            </div>
          </div>
        )}

        {/* Participants list (1v1 or no teams) */}
        {tournament.teamMode === 'Solo' && tournament.participants.length > 0 && (
          <div>
            <h2 className="mb-3 flex items-center gap-2 text-lg font-semibold text-zinc-50">
              <Users className="h-5 w-5 text-neon-cyan" />
              Participants ({tournament.participants.filter((p) => !p.isDisqualified).length})
            </h2>
            <div className="grid gap-2 sm:grid-cols-2 lg:grid-cols-3">
              {tournament.participants.map((p) => (
                <div key={p.id} className={`flex items-center gap-2 rounded-lg border border-neon-cyan/[0.08] bg-[#08081a] px-3 py-2 ${p.isDisqualified ? 'opacity-50' : ''}`}>
                  <Avatar url={p.avatarUrl} name={p.username} />
                  <span className={`min-w-0 flex-1 truncate text-sm ${p.isDisqualified ? 'text-zinc-500 line-through' : 'text-zinc-100'}`}>
                    {p.username}
                  </span>
                  {p.isDisqualified && <span className="text-[10px] font-bold uppercase text-red-300">DQ</span>}
                </div>
              ))}
            </div>
          </div>
        )}

        {/* Refresh */}
        <div className="flex justify-end">
          <button
            type="button"
            onClick={load}
            className="inline-flex items-center gap-1.5 rounded-lg border border-neon-cyan/[0.08] px-3 py-2 text-xs font-medium text-zinc-400 transition hover:bg-neon-cyan/[0.06] hover:text-zinc-200"
          >
            <RefreshCw className="h-3.5 w-3.5" />
            Refresh
          </button>
        </div>
      </div>
    </div>
  );
}
