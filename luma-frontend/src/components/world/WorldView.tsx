"use client";

import React from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import type {
  AuthenticatedUser,
  DailyPathResponse,
  DreamDetailsResponse,
  GoalResponse,
  ReflectionResponse,
} from "@/lib/types";
import { useLumaTheme } from "@/theme/ThemeProvider";
import { normalizeCoreAnchorKeys } from "@/theme/normalize";
import {
  coreAnchors,
  illustrationLabels,
  imageryLabels,
  type IllustrationKey,
  type PaletteKey,
} from "@/theme/tokens";
import { Button } from "@/components/ui/Button";
import { updateDreamTargetDate } from "@/lib/api";

interface WorldViewProps {
  dream: DreamDetailsResponse;
  user: AuthenticatedUser | null;
  dailyPath: DailyPathResponse | null;
  goals: GoalResponse[];
  reflections: ReflectionResponse[];
  dataWarnings: string[];
}

const THEME_OPTIONS: Array<{
  key: PaletteKey;
  label: string;
  swatch: string;
  description: string;
}> = [
  { key: "warm_dream", label: "Warm Dream", swatch: "#E88062", description: "Soft peach, warm cream & gentle light" },
  { key: "forest_mist", label: "Forest Mist", swatch: "#3C6E5A", description: "Deep pine, moss greens & calm earth" },
  { key: "city_night", label: "City Night", swatch: "#24324A", description: "Midnight blue, graphite & focused quiet" },
  { key: "lavender_milk", label: "Lavender Milk", swatch: "#9683B5", description: "Silky lavender, lilac & ethereal calm" },
  { key: "ocean_light", label: "Ocean Light", swatch: "#3E8C9E", description: "Coastal cyan, sea salt & airy clarity" },
  { key: "social_sunrise", label: "Social Sunrise", swatch: "#D96B43", description: "Warm coral, dawn sunlight & vibrant energy" },
  { key: "amber_summit", label: "Amber Summit", swatch: "#C9862C", description: "Golden amber, terracotta & warm altitude" },
  { key: "sunset_cream", label: "Sunset Cream", swatch: "#D47A65", description: "Dusk glow, vanilla cream & cozy reflection" },
  { key: "maker_workshop", label: "Maker Workshop", swatch: "#7C5E48", description: "Warm wood, craft copper & focused build" },
];

const archetypeDetails: Record<IllustrationKey, { symbol: string; scene: string }> = {
  dreamer: { symbol: "✦", scene: "A quiet horizon opens around a small guiding light." },
  urban_dreamer: { symbol: "▴", scene: "City windows glow softly beyond your own horizon." },
  creative_dreamer: { symbol: "✿", scene: "Ideas gather like color and begin to bloom." },
  nature_dreamer: { symbol: "❋", scene: "New roots take hold beneath a wide, open sky." },
  professional_dreamer: { symbol: "△", scene: "A bright workspace waits for what you will make next." },
};

function readable(value: string) {
  return value.replace(/_/g, " ");
}

function initials(name?: string | null) {
  const words = name?.trim().split(/\s+/).filter(Boolean) ?? [];
  return words.length ? words.slice(0, 2).map((word) => word[0].toUpperCase()).join("") : "L";
}

function formatReflectionDate(value: string) {
  return new Intl.DateTimeFormat("en", { month: "short", day: "numeric" }).format(new Date(value));
}

function EmptyValue({ children }: { children: React.ReactNode }) {
  return <span className="text-luma-text-secondary/80">{children}</span>;
}

export const WorldView: React.FC<WorldViewProps> = ({
  dream,
  user,
  dailyPath,
  goals,
  reflections,
  dataWarnings,
}) => {
  const router = useRouter();
  const profile = dream.futureProfile;
  const themeConfig = profile?.themeConfig;
  const {
    palette,
    atmosphere,
    illustration,
    motion,
    imagery,
    setTheme,
  } = useLumaTheme();

  const [menuOpen, setMenuOpen] = React.useState(false);
  const [profileOpen, setProfileOpen] = React.useState(false);
  const [themeOpen, setThemeOpen] = React.useState(false);
  const [pendingPalette, setPendingPalette] = React.useState<PaletteKey>(palette);
  const originalPalette = React.useRef<PaletteKey>(palette);
  const [themeSaving, setThemeSaving] = React.useState(false);
  const [themeFeedback, setThemeFeedback] = React.useState<string | null>(null);
  const [targetDateDisplay, setTargetDateDisplay] = React.useState(() => {
    const td = profile?.targetDate;
    if (td) {
      // Backend returns YYYY-MM-DD; convert to DD/MM/YYYY
      const m = String(td).match(/^(\d{4})-(\d{2})-(\d{2})/);
      if (m) return `${m[3]}/${m[2]}/${m[1]}`;
    }
    return profile?.targetYear ? `01/01/${profile.targetYear}` : "";
  });
  const [targetDateDraft, setTargetDateDraft] = React.useState(targetDateDisplay);
  const [targetDateSaving, setTargetDateSaving] = React.useState(false);
  const [targetDateFeedback, setTargetDateFeedback] = React.useState<string | null>(null);

  // Keep pendingPalette in sync with the actual applied palette when selector is closed
  React.useEffect(() => {
    if (!themeOpen) {
      setPendingPalette(palette);
    }
  }, [palette, themeOpen]);

  const openTheme = () => {
    originalPalette.current = palette;
    setPendingPalette(palette);
    setMenuOpen(false);
    setThemeOpen(true);
    setThemeFeedback(null);
  };

  const cancelTheme = () => {
    void setTheme({ palette: originalPalette.current }, { persist: false });
    setThemeOpen(false);
    setThemeFeedback(null);
  };

  const saveTheme = async () => {
    setThemeSaving(true);
    try {
      await setTheme({ palette: pendingPalette }, { persist: true });
      setThemeFeedback("Theme saved to your account ✓");
      window.setTimeout(() => {
        setThemeOpen(false);
        setThemeFeedback(null);
      }, 700);
    } catch {
      setThemeFeedback("Could not save the theme. Please try again.");
    } finally {
      setThemeSaving(false);
    }
  };

  const logout = async () => {
    await fetch("/api/auth/logout", { method: "POST" });
    router.replace("/login");
  };

  const saveTargetDate = async () => {
    // Validate DD/MM/YYYY
    const match = targetDateDraft.match(/^(\d{1,2})\/(\d{1,2})\/(\d{4})$/);
    if (!match) {
      setTargetDateFeedback("Enter a valid date in DD/MM/YYYY format.");
      return;
    }
    const day = Number(match[1]);
    const month = Number(match[2]);
    const year = Number(match[3]);
    if (month < 1 || month > 12 || day < 1) {
      setTargetDateFeedback("Enter a valid date in DD/MM/YYYY format.");
      return;
    }
    const maxDay = new Date(year, month, 0).getDate();
    if (day > maxDay) {
      setTargetDateFeedback(`Invalid date: ${String(month).padStart(2, "0")} only has ${maxDay} days.`);
      return;
    }
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    if (new Date(year, month - 1, day) < today) {
      setTargetDateFeedback("The date must be today or in the future.");
      return;
    }

    const isoDate = `${year}-${String(month).padStart(2, "0")}-${String(day).padStart(2, "0")}`;
    setTargetDateSaving(true);
    setTargetDateFeedback(null);
    try {
      const result = await updateDreamTargetDate(dream.id, isoDate);
      // Backend returns YYYY-MM-DD, convert to DD/MM/YYYY
      const rm = String(result.targetDate).match(/^(\d{4})-(\d{2})-(\d{2})/);
      const display = rm ? `${rm[3]}/${rm[2]}/${rm[1]}` : targetDateDraft;
      setTargetDateDisplay(display);
      setTargetDateDraft(display);
      setTargetDateFeedback("Target date saved.");
    } catch (error) {
      setTargetDateFeedback(error instanceof Error ? error.message : "Could not save the target date.");
    } finally {
      setTargetDateSaving(false);
    }
  };

  const selectedAnchors = normalizeCoreAnchorKeys(themeConfig?.coreAnchorKeys || []);
  const archetype = archetypeDetails[illustration];
  const environment = profile?.environment ? readable(profile.environment) : null;
  const lifestyle = profile?.lifestyle ?? [];
  const values = profile?.values ?? [];
  const aspirations = profile?.aspirations ?? [];
  const pendingActions = dailyPath?.actions.filter((action) => action.status === "Pending") ?? [];
  const completedActions = dailyPath?.actions.filter((action) => action.status === "Completed") ?? [];
  const restedActions = dailyPath?.actions.filter((action) => action.status === "Skipped") ?? [];
  const nextAction = pendingActions[0];
  const completionRatio = dailyPath && dailyPath.totalActions > 0
    ? Math.round((dailyPath.completedActions / dailyPath.totalActions) * 100)
    : 0;

  const goalStatusCounts = goals.reduce<Record<string, number>>((counts, goal) => {
    const status = goal.status || "Unspecified";
    counts[status] = (counts[status] ?? 0) + 1;
    return counts;
  }, {});

  return (
    <main className="mx-auto w-full max-w-7xl px-4 pb-12 pt-5 sm:px-8 sm:pt-8">
      <nav className="mb-12 flex items-center justify-between border-b border-luma-primary/10 pb-4" aria-label="Your world navigation">
        <div className="flex items-center gap-8">
          <Link href={`/your-world?id=${encodeURIComponent(dream.id)}`} className="font-serif text-lg tracking-[0.18em] text-luma-primary">
            LUMA
          </Link>
          <div className="hidden items-center gap-6 text-sm text-luma-text-secondary md:flex" aria-label="Primary navigation">
            <Link href={`/daily-path?id=${encodeURIComponent(dream.id)}`} className="transition-colors hover:text-luma-primary focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-luma-primary/40">
              Today
            </Link>
            <Link href={`/journey?id=${encodeURIComponent(dream.id)}`} className="transition-colors hover:text-luma-primary focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-luma-primary/40">
              My Journey
            </Link>
            <Link href={`/goals?id=${encodeURIComponent(dream.id)}`} className="transition-colors hover:text-luma-primary focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-luma-primary/40">
              Goals
            </Link>
          </div>
        </div>
        <div className="relative">
          <button
            type="button"
            aria-haspopup="menu"
            aria-expanded={menuOpen}
            onClick={() => setMenuOpen((open) => !open)}
            className="flex min-h-11 items-center gap-2 rounded-full border border-luma-primary/15 bg-luma-bg-secondary/60 px-2 py-1.5 text-sm text-luma-primary transition hover:border-luma-primary/35"
          >
            <span className="flex h-8 w-8 items-center justify-center rounded-full bg-luma-primary text-xs font-semibold text-luma-bg" aria-hidden="true">
              {initials(user?.name)}
            </span>
            <span className="hidden max-w-36 truncate sm:inline">{user?.name?.trim() || "Your account"}</span>
            <span aria-hidden="true">⌄</span>
          </button>
          {menuOpen && (
            <div role="menu" className="absolute right-0 z-30 mt-2 w-56 rounded-2xl border border-luma-primary/15 bg-luma-bg p-2 shadow-[var(--luma-shadow)]">
              <button type="button" role="menuitem" onClick={() => { setMenuOpen(false); setProfileOpen(true); }} className="min-h-11 w-full rounded-xl px-3 py-2 text-left text-sm text-luma-primary hover:bg-luma-surface/50">Profile &amp; Settings</button>
              <button type="button" role="menuitem" onClick={openTheme} className="min-h-11 w-full rounded-xl px-3 py-2 text-left text-sm text-luma-primary hover:bg-luma-surface/50">Theme</button>
              <button type="button" role="menuitem" onClick={logout} className="min-h-11 w-full rounded-xl px-3 py-2 text-left text-sm text-luma-primary hover:bg-luma-surface/50">Log out</button>
            </div>
          )}
        </div>
      </nav>

      <header className="mb-14 max-w-3xl">
        <p className="mb-4 text-xs font-semibold uppercase tracking-[0.24em] text-luma-text-secondary">
          {user?.name?.trim() ? `Welcome back, ${user.name.trim()}` : "Welcome back"}
        </p>
        <h1 className="font-serif text-5xl leading-[0.98] tracking-tight text-luma-primary sm:text-7xl">
          Your world, taking shape.
        </h1>
        <p className="mt-6 max-w-xl text-base leading-8 text-luma-text-secondary sm:text-lg">
          Keep shaping the life you want, one considered step at a time.
        </p>
      </header>

      {dataWarnings.length > 0 && (
        <div role="status" className="mb-8 rounded-2xl border border-luma-accent/25 bg-luma-surface/40 px-4 py-3 text-sm text-luma-primary">
          {dataWarnings.join(" ")}
        </div>
      )}

      <section className="mb-16" aria-labelledby="future-world-title">
        <div className="mb-5 flex items-end justify-between gap-4">
          <div>
            <p className="text-xs font-semibold uppercase tracking-[0.24em] text-luma-text-secondary">The future you imagined</p>
            <h2 id="future-world-title" className="mt-2 font-serif text-3xl text-luma-primary sm:text-4xl">My future world</h2>
            <p className="mt-2 text-sm text-luma-text-secondary">A vision for {targetDateDisplay || profile?.targetYear}</p>
          </div>
          <Link href="/discover" className="shrink-0 text-sm text-luma-primary underline decoration-luma-primary/30 underline-offset-4 hover:decoration-luma-primary">Edit your vision</Link>
        </div>

        <div className="relative overflow-hidden rounded-[2.5rem] border border-luma-primary/10 bg-luma-bg-secondary/50 shadow-[var(--luma-shadow)]">
          <div className={`world-scene world-scene--${atmosphere} relative min-h-[420px] sm:min-h-[560px] lg:min-h-[620px]`} aria-label={`${illustrationLabels[illustration]} future scene. ${archetype.scene}`}>
            <div className="world-scene__glow" aria-hidden="true" />
            <div className="world-scene__sun" aria-hidden="true" />
            <div className="world-scene__horizon" aria-hidden="true" />
            {selectedAnchors.slice(0, 2).map((anchorKey) => <span key={anchorKey} className={`world-scene__anchor-motif world-scene__anchor-motif--${anchorKey}`} aria-hidden="true">{coreAnchors[anchorKey].motif}</span>)}
            <div className="world-scene__landscape" aria-hidden="true"><span className="world-scene__hill world-scene__hill--back" /><span className="world-scene__hill world-scene__hill--front" /></div>
            <div className="world-scene__figure" aria-hidden="true"><span className="world-scene__figure-head" /><span className="world-scene__figure-body" /><span className="world-scene__figure-arm" /><span className="world-scene__figure-light">{archetype.symbol}</span></div>
            <div className="world-scene__caption max-w-xl"><span className="world-scene__eyebrow">{environment ? `${environment} future` : "Future profile"}</span><p className="font-serif text-2xl leading-snug text-luma-primary sm:text-4xl">{profile?.summary || <EmptyValue>No future summary added yet.</EmptyValue>}</p></div>
            <p className="world-scene__note">{archetype.scene}</p>
          </div>

            <div className="grid gap-8 p-6 sm:p-10 lg:grid-cols-[1.1fr_0.9fr]">
            <div>
              <p className="mb-2 text-xs font-semibold uppercase tracking-[0.18em] text-luma-text-secondary">Becoming</p>
              <h3 className="font-serif text-3xl text-luma-primary">{profile?.identity || <EmptyValue>Your future identity is still unfolding.</EmptyValue>}</h3>
              <p className="mt-3 max-w-lg leading-7 text-luma-text-secondary">{profile ? `${profile.mood || "A meaningful"} life at a ${profile.pace || "steady"} pace.` : "Complete your Life Canvas to give this world more shape."}</p>
            </div>
            <div className="space-y-5 text-sm text-luma-text-secondary">
              <InsightLine label="Life feels like" values={lifestyle} empty="No lifestyle choices recorded yet." />
              <InsightLine label="Rooted in" values={values} empty="No values recorded yet." />
              <InsightLine label="Reaching toward" values={aspirations} empty="No aspirations recorded yet." />
              <div className="flex flex-wrap gap-2 pt-1"><span className="rounded-full bg-luma-surface/50 px-3 py-1 capitalize">{atmosphere} atmosphere</span><span className="rounded-full bg-luma-surface/50 px-3 py-1 capitalize">{motion} motion</span><span className="rounded-full bg-luma-surface/50 px-3 py-1 capitalize">{palette.replace(/_/g, " ")}</span>{imagery.slice(0, 2).map((key) => <span key={key} className="rounded-full bg-luma-surface/50 px-3 py-1">{imageryLabels[key].emoji} {imageryLabels[key].label}</span>)}</div>
            </div>
          </div>
        </div>

        <div className="mt-5 flex flex-wrap items-end gap-3 rounded-2xl border border-luma-primary/10 bg-luma-surface/30 p-4">
          <label className="block">
            <span className="mb-1 block text-xs font-semibold uppercase tracking-[0.14em] text-luma-text-secondary">Target date (DD/MM/YYYY)</span>
            <input type="text" placeholder="DD/MM/YYYY" value={targetDateDraft} onChange={(event) => setTargetDateDraft(event.target.value)} className="min-h-10 w-40 rounded-xl border border-luma-primary/20 bg-luma-bg px-3 text-sm text-luma-primary outline-none focus:border-luma-primary focus:ring-2 focus:ring-luma-primary/20" />
          </label>
          <Button size="sm" onClick={saveTargetDate} disabled={targetDateSaving}>{targetDateSaving ? "Saving…" : "Save date"}</Button>
          {targetDateFeedback && <p role="status" className="text-xs text-luma-text-secondary">{targetDateFeedback}</p>}
        </div>
      </section>

      <section className="grid gap-12 lg:grid-cols-[1.2fr_0.8fr]" aria-label="Personal progress and journey">
        <div>
          <div className="mb-5 flex items-end justify-between gap-4"><div><p className="text-xs font-semibold uppercase tracking-[0.24em] text-luma-text-secondary">The next gentle step</p><h2 className="mt-2 font-serif text-3xl text-luma-primary sm:text-4xl">Your path forward</h2></div><Link href={`/daily-path?id=${encodeURIComponent(dream.id)}`} className="shrink-0 text-sm text-luma-primary underline decoration-luma-primary/30 underline-offset-4">Open today&apos;s path</Link></div>
          {dailyPath ? <div className="rounded-[2rem] bg-luma-primary p-6 text-luma-bg shadow-[var(--luma-shadow)] sm:p-8"><div className="flex flex-wrap items-end justify-between gap-4"><div><p className="text-xs font-semibold uppercase tracking-[0.2em] text-luma-bg/65">Today&apos;s progress</p><p className="mt-2 font-serif text-5xl">{dailyPath.completedActions}<span className="text-2xl text-luma-bg/60"> / {dailyPath.totalActions}</span></p></div><p className="text-sm text-luma-bg/75">{completionRatio}% complete</p></div><div className="mt-6 h-1.5 overflow-hidden rounded-full bg-luma-bg/20"><div className="h-full rounded-full bg-luma-bg transition-all" style={{ width: `${completionRatio}%` }} /></div><p className="mt-3 text-xs text-luma-bg/65">{completedActions.length} completed · {pendingActions.length} pending · {restedActions.length} rested</p>{nextAction ? <div className="mt-8 border-t border-luma-bg/20 pt-6"><p className="text-xs uppercase tracking-[0.16em] text-luma-bg/65">Next up</p><h3 className="mt-2 font-serif text-2xl">{nextAction.title}</h3><p className="mt-2 text-sm text-luma-bg/75">{nextAction.estimatedMinutes ? `${nextAction.estimatedMinutes} minutes` : "Time not recorded"}{nextAction.goalTitle ? ` · ${nextAction.goalTitle}` : ""}</p></div> : <div className="mt-8 border-t border-luma-bg/20 pt-6"><p className="font-serif text-2xl">Your recorded path is clear for now.</p><p className="mt-2 text-sm text-luma-bg/75">No pending action is currently recorded.</p></div>}<div className="mt-8 flex flex-wrap gap-3"><Link href={`/daily-path?id=${encodeURIComponent(dream.id)}`} className="rounded-full bg-luma-bg px-4 py-2 text-sm font-semibold text-luma-primary">Open today&apos;s path →</Link><Link href={`/journey?id=${encodeURIComponent(dream.id)}`} className="rounded-full border border-luma-bg/30 px-4 py-2 text-sm text-luma-bg">My Journey</Link></div></div> : <EmptyState text="No daily path is available yet." href={`/daily-path?id=${encodeURIComponent(dream.id)}`} label="Open Today" />}
        </div>

        <div className="space-y-12"><section aria-labelledby="goals-title"><div className="mb-5 flex items-end justify-between gap-3"><div><p className="text-xs font-semibold uppercase tracking-[0.24em] text-luma-text-secondary">What matters next</p><h2 id="goals-title" className="mt-2 font-serif text-3xl text-luma-primary">Goals that matter</h2></div><Link href={`/goals?id=${encodeURIComponent(dream.id)}`} className="text-sm text-luma-primary underline decoration-luma-primary/30 underline-offset-4">Goals &amp; Gaps</Link></div>{goals.length ? <div className="space-y-5">{goals.slice(0, 3).map((goal, index) => <article key={goal.id} className={`border-l-2 pl-4 ${index === 0 ? "border-luma-primary" : "border-luma-primary/20"}`}><p className="font-serif text-xl text-luma-primary">{goal.title}</p><p className="mt-1 text-xs capitalize text-luma-text-secondary">{goal.status ? readable(goal.status) : "Status not recorded"}</p>{goal.desiredOutcome && <p className="mt-2 text-sm leading-6 text-luma-text-secondary">{goal.desiredOutcome}</p>}</article>)}<p className="text-xs text-luma-text-secondary">{Object.entries(goalStatusCounts).map(([status, count]) => `${count} ${readable(status).toLowerCase()}`).join(" · ")}</p></div> : <EmptyState text="No goals have been recorded yet." href={`/goals?id=${encodeURIComponent(dream.id)}`} label="Open Goals & Gaps" />}</section><section aria-labelledby="reflection-title"><div className="mb-5 flex items-end justify-between gap-3"><div><p className="text-xs font-semibold uppercase tracking-[0.24em] text-luma-text-secondary">A place to notice</p><h2 id="reflection-title" className="mt-2 font-serif text-3xl text-luma-primary">Reflection &amp; journey</h2></div><Link href={`/journey?id=${encodeURIComponent(dream.id)}`} className="text-sm text-luma-primary underline decoration-luma-primary/30 underline-offset-4">My Journey</Link></div>{reflections.length ? <div className="rounded-[1.5rem] bg-luma-surface/40 p-5"><p className="text-xs uppercase tracking-[0.16em] text-luma-text-secondary">{formatReflectionDate(reflections[0].createdAt)} · {reflections[0].mood}</p><p className="mt-3 font-serif text-xl leading-relaxed text-luma-primary">{reflections[0].insight || reflections[0].notes}</p></div> : <EmptyState text="Your reflections will live here as you move through your path." href={`/daily-path?id=${encodeURIComponent(dream.id)}`} label="Open Today" />}</section></div>
      </section>

      <div className="mt-14 flex flex-wrap gap-4 border-t border-luma-primary/10 pt-6 text-sm"><Link href={`/talk?id=${encodeURIComponent(dream.id)}`} className="text-luma-primary underline decoration-luma-primary/30 underline-offset-4">Talk to LUMA →</Link><button type="button" onClick={openTheme} className="text-luma-primary underline decoration-luma-primary/30 underline-offset-4">Change theme · {palette.replace(/_/g, " ")}</button></div>

      {profileOpen && <div className="fixed inset-0 z-40 flex items-center justify-center bg-black/30 p-4" role="dialog" aria-modal="true" aria-labelledby="profile-panel-title"><div className="w-full max-w-md rounded-[2rem] border border-luma-primary/15 bg-luma-bg p-6 shadow-2xl"><div className="flex items-start justify-between gap-4"><div><p className="text-xs font-semibold uppercase tracking-[0.2em] text-luma-text-secondary">Account</p><h2 id="profile-panel-title" className="mt-1 font-serif text-3xl text-luma-primary">Profile &amp; Settings</h2></div><button type="button" onClick={() => setProfileOpen(false)} aria-label="Close profile settings" className="rounded-full px-2 py-1 text-luma-text-secondary hover:bg-luma-surface/50">✕</button></div><div className="mt-6 space-y-3 text-sm"><InfoField label="Name" value={user?.name} /><InfoField label="Email" value={user?.email} /><p className="pt-2 text-xs leading-5 text-luma-text-secondary">Profile editing is not available in the current account flow. Your saved visual theme is managed from Theme.</p></div></div></div>}
      {themeOpen && <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4" role="dialog" aria-modal="true" aria-labelledby="theme-picker-title"><div className="w-full max-w-xl rounded-[2rem] border border-luma-primary/20 bg-luma-bg p-6 shadow-2xl sm:p-8"><div className="flex items-start justify-between gap-4"><div><h2 id="theme-picker-title" className="font-serif text-2xl text-luma-primary">Choose your atmosphere</h2><p className="mt-1 text-xs text-luma-text-secondary">Your saved theme follows you across LUMA.</p></div><button type="button" onClick={cancelTheme} disabled={themeSaving} aria-label="Close theme selector" className="rounded-full px-2 py-1 text-luma-text-secondary hover:bg-luma-surface/50">✕</button></div>{themeFeedback && <p role="status" className="mt-4 rounded-xl bg-luma-surface/50 px-3 py-2 text-xs text-luma-primary">{themeFeedback}</p>}<div className="mt-6 grid max-h-[55vh] grid-cols-1 gap-3 overflow-y-auto sm:grid-cols-2">{THEME_OPTIONS.map(({ key, label, swatch, description }) => <button key={key} type="button" onClick={() => { setPendingPalette(key); void setTheme({ palette: key }, { persist: false }); }} aria-pressed={pendingPalette === key} className={`rounded-2xl border p-3 text-left transition ${pendingPalette === key ? "border-luma-primary bg-luma-surface/60" : "border-luma-primary/10 hover:border-luma-primary/30"}`}><span className="mb-2 block h-8 w-8 rounded-full border border-white/70" style={{ backgroundColor: swatch }} aria-hidden="true" /><span className="block text-sm font-semibold capitalize text-luma-primary">{label}{pendingPalette === key ? " ✓" : ""}</span><span className="mt-1 block text-xs text-luma-text-secondary">{description}</span></button>)}</div><div className="mt-6 flex justify-end gap-3"><Button variant="secondary" size="sm" onClick={cancelTheme} disabled={themeSaving}>Cancel</Button><Button size="sm" onClick={saveTheme} disabled={themeSaving}>{themeSaving ? "Saving…" : "Save theme"}</Button></div></div></div>}
    </main>
  );
};

function InsightLine({ label, values, empty }: { label: string; values: string[]; empty: string }) {
  return <div><p className="text-xs uppercase tracking-[0.14em] text-luma-text-secondary">{label}</p><p className="mt-1 leading-6 capitalize text-luma-primary">{values.length ? values.slice(0, 4).map(readable).join(" · ") : <EmptyValue>{empty}</EmptyValue>}</p></div>;
}

function InfoField({ label, value }: { label: string; value?: string | null }) {
  return <div className="border-b border-luma-primary/10 pb-3"><p className="text-[0.65rem] font-semibold uppercase tracking-[0.16em] text-luma-text-secondary">{label}</p><p className="mt-1 text-sm text-luma-primary">{value || <EmptyValue>Not recorded yet</EmptyValue>}</p></div>;
}

function EmptyState({ text, href, label }: { text: string; href: string; label: string }) {
  return <div className="rounded-[1.5rem] border border-dashed border-luma-primary/20 px-5 py-6 text-sm leading-6 text-luma-text-secondary"><p>{text}</p><Link href={href} className="mt-3 inline-block font-semibold text-luma-primary underline underline-offset-4">{label} →</Link></div>;
}
