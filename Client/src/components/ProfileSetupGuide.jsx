import { CheckCircle, Circle, ClipboardList, Clock, UserPlus } from 'lucide-react';

/**
 * Phase 9: onboarding checklist for signed-in users who aren't fully set up.
 * Progress is derived from the profile data — no separate state to maintain.
 */
export default function ProfileSetupGuide({ profile, onRequestJoin }) {
  if (!profile) return null;

  const hasAccount = true; // The guide only renders for signed-in users.
  const hasRoblox = !!profile.player;
  const hasDiscord = !!profile.discordUserId;
  const hasRequest = !!profile.joinRequest;
  const isTrackerMember = !!profile.player;

  const steps = [
    { label: 'Account', done: hasAccount, hint: 'Create your website account.' },
    { label: 'Roblox', done: hasRoblox, hint: 'Add your Roblox username and Roblox ID by claiming or joining.' },
    { label: 'Discord', done: hasDiscord, hint: 'Add your Discord ID in your profile to use the bot.' },
    { label: 'Tracker Request', done: isTrackerMember, hint: 'Request to join the tracker.' }
  ];

  const completed = steps.filter((s) => s.done).length;

  // Fully set up members don't need the guide.
  if (isTrackerMember && hasDiscord) return null;

  const nextStep = steps.find((s) => !s.done);

  return (
    <section className="rounded-xl border border-neon-cyan/[0.08] bg-[#08081a] p-4">
      <div className="mb-3 flex items-center justify-between">
        <div className="flex items-center gap-2 text-sm font-semibold text-zinc-100">
          <ClipboardList className="h-4 w-4 text-neon-cyan" />
          Profile Setup
          <span className="text-xs font-normal text-mist">
            {completed}/{steps.length} complete
          </span>
        </div>
        {nextStep && !isTrackerMember && (
          <button
            type="button"
            onClick={onRequestJoin}
            className="inline-flex h-8 items-center gap-1.5 rounded-lg border border-neon-green/30 bg-neon-green/10 px-3 text-xs font-semibold text-neon-green transition hover:bg-neon-green/20"
          >
            <UserPlus className="h-3.5 w-3.5" />
            {nextStep.label === 'Tracker Request' ? 'Request to Join' : 'Continue Setup'}
          </button>
        )}
      </div>

      <div className="mb-3 h-1.5 overflow-hidden rounded-full bg-zinc-800">
        <div
          className="h-full rounded-full bg-gradient-to-r from-neon-cyan to-neon-green transition-all duration-500"
          style={{ width: `${(completed / steps.length) * 100}%` }}
        />
      </div>

      <ul className="space-y-1.5">
        {steps.map((step) => (
          <li key={step.label} className="flex items-center gap-2 text-sm">
            {step.done ? (
              <CheckCircle className="h-4 w-4 shrink-0 text-emerald-400" />
            ) : (
              <Circle className="h-4 w-4 shrink-0 text-zinc-600" />
            )}
            <span className={step.done ? 'text-zinc-400 line-through decoration-zinc-600' : 'text-zinc-100'}>
              {step.label}
            </span>
            {!step.done && (
              <span className="flex items-center gap-1 text-xs text-mist">
                — {step.hint}
                {step.label === 'Discord' && (
                  <Clock className="h-3 w-3 text-zinc-600" />
                )}
              </span>
            )}
          </li>
        ))}
      </ul>
    </section>
  );
}
