"use client";

import { Suspense, useEffect, useMemo, useState } from "react";
import Link from "next/link";
import { useSearchParams } from "next/navigation";
import { Button } from "@/components/ui/Button";
import { DreamThemeProvider } from "@/components/world/DreamThemeProvider";
import {
  getDreamDetails,
  getDreamGoals,
  getJourneyCalendarDay,
  getJourneyCalendarMonth,
  getLatestDream,
} from "@/lib/api";
import type {
  DreamDetailsResponse,
  GoalResponse,
  JourneyCalendarDayResponse,
  JourneyCalendarMonthResponse,
} from "@/lib/types";

const weekdayLabels = ["Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat"];

function dateKey(year: number, monthIndex: number, day: number) {
  return `${year}-${String(monthIndex + 1).padStart(2, "0")}-${String(day).padStart(2, "0")}`;
}

function localTodayKey() {
  const today = new Date();
  return dateKey(today.getFullYear(), today.getMonth(), today.getDate());
}

function JourneyContent() {
  const paramDreamId = useSearchParams().get("id");
  const [dreamId, setDreamId] = useState<string | null>(paramDreamId);
  const initialDate = useMemo(() => new Date(), []);
  const [dream, setDream] = useState<DreamDetailsResponse | null>(null);
  const [goals, setGoals] = useState<GoalResponse[]>([]);
  const [visibleMonth, setVisibleMonth] = useState(
    () => new Date(initialDate.getFullYear(), initialDate.getMonth(), 1)
  );
  const [calendar, setCalendar] = useState<JourneyCalendarMonthResponse | null>(null);
  const [selectedDate, setSelectedDate] = useState(localTodayKey);
  const [selectedDay, setSelectedDay] = useState<JourneyCalendarDayResponse | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isLoadingDay, setIsLoadingDay] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (paramDreamId || dreamId) {
      return;
    }

    let active = true;
    getLatestDream()
      .then((latest) => {
        if (active && latest?.id) {
          setDreamId(latest.id);
        } else if (active) {
          setError("No dream found. Please complete the discovery flow first.");
          setIsLoading(false);
          setIsLoadingDay(false);
        }
      })
      .catch((err: unknown) => {
        if (active) {
          setError(err instanceof Error ? err.message : "Could not load your journey.");
          setIsLoading(false);
          setIsLoadingDay(false);
        }
      });

    return () => {
      active = false;
    };
  }, [paramDreamId]);

  useEffect(() => {
    if (!dreamId) return;
    let active = true;

    getDreamDetails(dreamId)
      .then((result) => active && setDream(result))
      .catch((err: unknown) =>
        active && setError(err instanceof Error ? err.message : "Could not load your journey.")
      );

    getDreamGoals(dreamId)
      .then((result) => active && setGoals(result))
      .catch(() => {
        // Goals are supplementary for journey milestone badges
      });

    return () => {
      active = false;
    };
  }, [dreamId]);

  useEffect(() => {
    if (!dreamId) return;
    let active = true;

    getJourneyCalendarMonth(
      dreamId,
      visibleMonth.getFullYear(),
      visibleMonth.getMonth() + 1
    )
      .then((result) => active && setCalendar(result))
      .catch((err: unknown) =>
        active && setError(err instanceof Error ? err.message : "Could not load this month.")
      )
      .finally(() => active && setIsLoading(false));

    return () => {
      active = false;
    };
  }, [dreamId, visibleMonth]);

  useEffect(() => {
    if (!dreamId || !calendar) return;
    let active = true;

    getJourneyCalendarDay(dreamId, selectedDate)
      .then((result) => active && setSelectedDay(result))
      .catch((err: unknown) =>
        active && setError(err instanceof Error ? err.message : "Could not load this day.")
      )
      .finally(() => active && setIsLoadingDay(false));

    return () => {
      active = false;
    };
  }, [calendar, dreamId, selectedDate]);

  const selectMonth = (offset: number) => {
    const next = new Date(visibleMonth.getFullYear(), visibleMonth.getMonth() + offset, 1);
    setIsLoading(true);
    setIsLoadingDay(true);
    setSelectedDay(null);
    setError(null);
    setVisibleMonth(next);
    const today = new Date();
    setSelectedDate(
      next.getFullYear() === today.getFullYear() && next.getMonth() === today.getMonth()
        ? localTodayKey()
        : dateKey(next.getFullYear(), next.getMonth(), 1)
    );
  };

  const selectDate = (value: string) => {
    setIsLoadingDay(true);
    setSelectedDay(null);
    setSelectedDate(value);
  };

  if (!dream && isLoading) {
    return <LoadingState label="Gathering the moments of your journey..." />;
  }

  if (!dream || error) {
    return (
      <main className="mx-auto flex min-h-[70vh] w-full max-w-md flex-col items-center justify-center px-4 text-center">
        <span className="mb-5 text-3xl" aria-hidden="true">✦</span>
        <h1 className="font-serif text-3xl text-luma-primary">Journey not available</h1>
        <p className="mt-3 text-sm leading-relaxed text-luma-text-secondary">{error || "We couldn't find this future."}</p>
        <Link className="mt-7" href="/discover"><Button>Start imagining</Button></Link>
      </main>
    );
  }

  const year = visibleMonth.getFullYear();
  const monthIndex = visibleMonth.getMonth();
  const firstWeekday = new Date(year, monthIndex, 1).getDay();
  const daysInMonth = new Date(year, monthIndex + 1, 0).getDate();
  const dayByDate = new Map(calendar?.days.map((day) => [day.planDate, day]));
  const todayKey = localTodayKey();
  const monthLabel = visibleMonth.toLocaleDateString(undefined, { month: "long", year: "numeric" });

  const goalCategoryById = new Map<string, string>(goals.map((g) => [g.id, g.category]));

  const isDayHonored = Boolean(
    selectedDay &&
    selectedDay.actions.length > 0 &&
    selectedDay.actions.every((action) => action.status === "Completed")
  );

  return (
    <DreamThemeProvider dream={dream}>
      <main className="mx-auto w-full max-w-6xl px-4 py-8 sm:px-8 sm:py-12 animate-luma-fade-in">
        <nav className="mb-8 flex flex-wrap items-center justify-between gap-3 border-b border-luma-primary/10 pb-4" aria-label="My Journey navigation">
          <div className="flex items-center gap-4 text-sm">
            <Link href={`/daily-path?id=${dream.id}`} className="font-medium text-luma-primary transition-opacity hover:opacity-70">Today</Link>
            <Link href={`/your-world?id=${dream.id}`} className="font-medium text-luma-text-secondary transition-opacity hover:opacity-70">My Future</Link>
            <Link href={`/goals?id=${dream.id}`} className="font-medium text-luma-text-secondary transition-opacity hover:opacity-70">Goals &amp; Gaps</Link>
          </div>
          <span className="font-serif text-xs tracking-[0.2em] text-luma-primary/55">LUMA · MY JOURNEY</span>
          <Link href={`/talk?id=${dream.id}`} className="text-sm font-medium text-luma-primary transition-opacity hover:opacity-70">Talk to LUMA ✦</Link>
        </nav>

        <header className="mx-auto mb-8 max-w-3xl text-center sm:mb-10">
          <p className="mb-3 text-xs font-semibold uppercase tracking-[0.22em] text-luma-text-secondary">A record of your gentle progress</p>
          <h1 className="font-serif text-4xl tracking-tight text-luma-primary sm:text-5xl">My Journey</h1>
          <p className="mt-3 font-serif text-lg italic leading-relaxed text-luma-text-secondary">The steps you chose, held in their own time.</p>
        </header>

        <section className="rounded-[2rem] border border-luma-primary/15 bg-white/55 p-4 shadow-[var(--luma-shadow)] backdrop-blur-md sm:p-7" aria-label="Journey calendar">
          <div className="mb-6 flex items-center justify-between gap-3">
            <button type="button" onClick={() => selectMonth(-1)} className="rounded-full border border-luma-primary/15 bg-white/70 px-4 py-2 text-sm font-medium text-luma-primary transition-colors hover:bg-white" aria-label="Previous month">←</button>
            <h2 className="font-serif text-2xl text-luma-primary sm:text-3xl">{monthLabel}</h2>
            <button type="button" onClick={() => selectMonth(1)} className="rounded-full border border-luma-primary/15 bg-white/70 px-4 py-2 text-sm font-medium text-luma-primary transition-colors hover:bg-white" aria-label="Next month">→</button>
          </div>

          {isLoading ? <LoadingState label="Opening this month..." compact /> : (
            <div className="grid grid-cols-7 gap-1.5 sm:gap-2">
              {weekdayLabels.map((label) => <div key={label} className="pb-1 text-center text-[10px] font-semibold uppercase tracking-[0.12em] text-luma-text-secondary sm:text-xs">{label}</div>)}
              {Array.from({ length: firstWeekday }).map((_, index) => <div key={`blank-${index}`} />)}
              {Array.from({ length: daysInMonth }, (_, index) => {
                const day = index + 1;
                const key = dateKey(year, monthIndex, day);
                const summary = dayByDate.get(key);
                const selected = key === selectedDate;
                const isToday = key === todayKey;
                return (
                  <button key={key} type="button" onClick={() => selectDate(key)} className={`min-h-20 rounded-2xl border p-2 text-left transition-all sm:min-h-24 sm:p-3 ${selected ? "border-luma-accent bg-luma-accent/25 shadow-sm" : "border-luma-primary/10 bg-white/35 hover:bg-white/65"} ${isToday ? "ring-1 ring-luma-primary/35" : ""}`} aria-pressed={selected}>
                    <span className={`inline-flex h-6 w-6 items-center justify-center rounded-full text-xs font-semibold ${isToday ? "bg-luma-primary text-white" : "text-luma-primary"}`}>{day}</span>
                    {summary?.hasSnapshot ? (
                      <span className="mt-2 block text-[10px] font-medium leading-snug text-luma-primary sm:text-xs">{summary.completedActionCount}/{summary.actionCount} done{summary.reflections.length > 0 ? " · ✦" : ""}</span>
                    ) : <span className="mt-2 block text-[10px] text-luma-text-secondary/55 sm:text-xs">No snapshot</span>}
                  </button>
                );
              })}
            </div>
          )}
        </section>

        <section className="mt-8 rounded-[2rem] border border-luma-primary/15 bg-white/50 p-6 shadow-[var(--luma-shadow)] backdrop-blur-md sm:p-8" aria-live="polite">
          <div className="flex flex-wrap items-center justify-between gap-3 border-b border-luma-primary/10 pb-3">
            <p className="text-xs font-semibold uppercase tracking-[0.2em] text-luma-text-secondary">
              {new Date(`${selectedDate}T12:00:00`).toLocaleDateString(undefined, { weekday: "long", month: "long", day: "numeric", year: "numeric" })}
            </p>
            {selectedDay && (
              <div className="flex items-center gap-3 text-xs">
                <span className="font-semibold text-emerald-800">
                  {selectedDay.actions.filter((a) => a.status === "Completed").length} Done
                </span>
                {selectedDay.actions.some((a) => a.status === "Skipped") && (
                  <span className="text-luma-text-secondary">
                    {selectedDay.actions.filter((a) => a.status === "Skipped").length} Rested
                  </span>
                )}
                <span className="text-luma-primary/30">·</span>
                <span className="text-luma-text-secondary font-medium">
                  {selectedDay.actions.length} {selectedDay.actions.length === 1 ? "step" : "steps"}
                </span>
              </div>
            )}
          </div>

          {isLoadingDay ? <LoadingState label="Opening this day..." compact /> : selectedDay ? (
            <div className="mt-5 space-y-4">
              {isDayHonored && (
                <div
                  className="rounded-2xl border border-emerald-500/25 bg-emerald-500/10 p-4 sm:p-5 text-emerald-950 flex items-center gap-3 animate-luma-fade-in shadow-sm"
                  role="status"
                >
                  <span
                    className="flex h-8 w-8 shrink-0 items-center justify-center rounded-full bg-emerald-600 text-white text-sm font-bold shadow-sm"
                    aria-hidden="true"
                  >
                    ✓
                  </span>
                  <div>
                    <p className="font-serif text-lg font-medium text-emerald-950">
                      Day Honored ✦
                    </p>
                    <p className="text-xs text-emerald-800 leading-relaxed">
                      Every intentional step chosen for this day was fulfilled. Your future took root in quiet, steady ways.
                    </p>
                  </div>
                </div>
              )}

              {selectedDay.actions.map((action) => {
                const category = action.goalId ? goalCategoryById.get(action.goalId) : null;
                const isMilestone =
                  category?.toLowerCase() === "milestone" ||
                  action.goalTitle?.toLowerCase().includes("milestone");

                return (
                  <article key={action.id} className="rounded-2xl border border-luma-primary/10 bg-white/60 p-4 sm:p-5 shadow-sm">
                    <div className="flex flex-wrap items-start justify-between gap-3">
                      <div>
                        <div className="flex flex-wrap items-center gap-2">
                          <h3 className={`font-serif text-xl text-luma-primary ${action.status === "Completed" ? "line-through opacity-70" : ""}`}>
                            {action.title}
                          </h3>
                          {isMilestone && (
                            <span className="inline-flex items-center gap-1 rounded-full border border-amber-500/30 bg-amber-500/10 px-2.5 py-0.5 text-[11px] font-semibold text-amber-900">
                              <span aria-hidden="true">✦</span> Milestone
                            </span>
                          )}
                        </div>
                        <p className="mt-1 text-xs text-luma-text-secondary">
                          {action.goalTitle ? `${action.goalTitle} · ` : ""}
                          {action.timeOfDay}
                          {action.estimatedMinutes > 0 ? ` · ${action.estimatedMinutes} min` : ""}
                        </p>
                      </div>
                      <div>
                        {action.status === "Completed" ? (
                          <span className="rounded-full border border-emerald-500/20 bg-emerald-50 px-3 py-1 text-xs font-semibold text-emerald-800">
                            ✓ Completed
                          </span>
                        ) : action.status === "Skipped" ? (
                          <span className="rounded-full border border-luma-primary/10 bg-luma-surface/60 px-3 py-1 text-xs font-medium text-luma-text-secondary">
                            ☕ Rested
                          </span>
                        ) : (
                          <span className="rounded-full border border-luma-primary/10 bg-white px-3 py-1 text-xs font-medium text-luma-primary">
                            Pending
                          </span>
                        )}
                      </div>
                    </div>
                    {action.status === "Completed" && action.completedAt && (
                      <p className="mt-2 text-[11px] font-medium text-emerald-800/80">
                        Completed at {new Date(action.completedAt).toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" })}
                      </p>
                    )}
                    <p className="mt-3 text-sm leading-relaxed text-luma-text-secondary">{action.description}</p>
                    {action.intention && <p className="mt-3 border-t border-luma-primary/10 pt-3 text-xs italic text-luma-primary">✦ {action.intention}</p>}
                  </article>
                );
              })}

              {selectedDay.reflections.length > 0 && (
                <div className="space-y-3 pt-3">
                  <p className="text-xs font-semibold uppercase tracking-[0.16em] text-luma-text-secondary">
                    Daily Reflections
                  </p>
                  {selectedDay.reflections.map((reflection) => (
                    <article key={reflection.id} className="rounded-2xl border border-luma-accent/25 bg-luma-accent/15 p-4 sm:p-5 shadow-sm">
                      <div className="flex items-center justify-between gap-2">
                        <p className="text-xs font-semibold text-luma-primary">✦ {reflection.mood}</p>
                        <span className="text-[11px] text-luma-text-secondary">
                          {new Date(reflection.createdAt).toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" })}
                        </span>
                      </div>
                      {reflection.notes && <p className="mt-2 font-serif text-sm italic text-luma-text-secondary leading-relaxed">“{reflection.notes}”</p>}
                      {reflection.insight && <p className="mt-2 text-sm text-luma-primary leading-relaxed">{reflection.insight}</p>}
                    </article>
                  ))}
                </div>
              )}
            </div>
          ) : (
            <div className="mt-5 rounded-2xl border border-dashed border-luma-primary/15 bg-white/30 p-6 text-center">
              <p className="font-serif text-xl text-luma-primary">An open day</p>
              <p className="mt-2 text-sm text-luma-text-secondary">No saved plan exists for this date. Nothing has been reconstructed from your current path.</p>
            </div>
          )}
        </section>
      </main>
    </DreamThemeProvider>
  );
}

function LoadingState({ label, compact = false }: { label: string; compact?: boolean }) {
  return <div className={`flex flex-col items-center justify-center text-center ${compact ? "py-8" : "min-h-[70vh] px-4"}`}><span className="animate-pulse text-2xl text-luma-accent">✦</span><p className="mt-3 text-sm text-luma-text-secondary">{label}</p></div>;
}

export default function JourneyPage() {
  return <Suspense fallback={<LoadingState label="Opening your journey..." />}><JourneyContent /></Suspense>;
}
