"use client";

import React, { useState, useEffect } from "react";
import Link from "next/link";
import type {
  DreamDetailsResponse,
  DailyPathResponse,
  DailyActionStatus,
  DailyActionResponse,
  ReflectionResponse,
} from "@/lib/types";
import { getDailyPath, updateDailyActionStatus, createReflection, getReflections } from "@/lib/api";
import { Button } from "@/components/ui/Button";

interface DailyPathViewProps {
  dream: DreamDetailsResponse;
  dailyPath: DailyPathResponse;
}

const timeIcons: Record<string, string> = {
  Morning: "🌅",
  Afternoon: "☀️",
  Evening: "🌙",
  Anytime: "✦",
};

const timeColors: Record<string, string> = {
  Morning: "from-amber-100/60 to-orange-50/40",
  Afternoon: "from-sky-100/60 to-blue-50/40",
  Evening: "from-indigo-100/60 to-violet-50/40",
  Anytime: "from-white/60 to-white/40",
};

const moodOptions = [
  { label: "Grounded", icon: "🌿" },
  { label: "Energized", icon: "⚡" },
  { label: "Peaceful", icon: "🕊️" },
  { label: "Reflective", icon: "🌙" },
  { label: "Challenging", icon: "🌊" },
];

export const DailyPathView: React.FC<DailyPathViewProps> = ({
  dream,
  dailyPath: initialDailyPath,
}) => {
  const [dailyPath, setDailyPath] = useState(initialDailyPath);
  const [isRefreshing, setIsRefreshing] = useState(false);
  const [updatingActionId, setUpdatingActionId] = useState<string | null>(null);

  // Reflection state
  const [reflections, setReflections] = useState<ReflectionResponse[]>([]);
  const [selectedMood, setSelectedMood] = useState<string>("Grounded");
  const [reflectionNotes, setReflectionNotes] = useState<string>("");
  const [isSubmittingReflection, setIsSubmittingReflection] = useState(false);
  const [reflectionError, setReflectionError] = useState<string | null>(null);

  const total = dailyPath.actions.length;
  const completed = dailyPath.actions.filter(
    (a) => a.status === "Completed"
  ).length;
  const skipped = dailyPath.actions.filter((a) => a.status === "Skipped").length;
  const progressPercent = total > 0 ? Math.round((completed / total) * 100) : 0;

  useEffect(() => {
    getReflections(dream.id)
      .then((data) => setReflections(data))
      .catch((err) => console.error("Could not load reflections:", err));
  }, [dream.id]);

  const handleRefresh = async () => {
    setIsRefreshing(true);
    try {
      const refreshed = await getDailyPath(dream.id, true);
      setDailyPath(refreshed);
    } catch (err) {
      console.error("Failed to refresh daily path:", err);
    } finally {
      setIsRefreshing(false);
    }
  };

  const handleStatusChange = async (
    action: DailyActionResponse,
    newStatus: DailyActionStatus
  ) => {
    const targetStatus: DailyActionStatus =
      action.status === newStatus ? "Pending" : newStatus;

    const previousActions = dailyPath.actions;
    const updatedActions = dailyPath.actions.map((a) =>
      a.id === action.id
        ? {
            ...a,
            status: targetStatus,
            completedAt:
              targetStatus === "Completed" ? new Date().toISOString() : null,
          }
        : a
    );

    const newCompleted = updatedActions.filter(
      (a) => a.status === "Completed"
    ).length;
    const newSkipped = updatedActions.filter(
      (a) => a.status === "Skipped"
    ).length;

    setDailyPath((prev) => ({
      ...prev,
      actions: updatedActions,
      completedActions: newCompleted,
      skippedActions: newSkipped,
    }));

    setUpdatingActionId(action.id);
    try {
      await updateDailyActionStatus(dream.id, action.id, targetStatus);
    } catch (err) {
      console.error("Failed to update action status:", err);
      setDailyPath((prev) => ({
        ...prev,
        actions: previousActions,
      }));
    } finally {
      setUpdatingActionId(null);
    }
  };

  const handleReflectionSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsSubmittingReflection(true);
    setReflectionError(null);

    try {
      const result = await createReflection(dream.id, {
        mood: selectedMood,
        notes: reflectionNotes,
      });

      setReflections((prev) => [result, ...prev]);
      setReflectionNotes("");
    } catch (err: unknown) {
      setReflectionError(
        err instanceof Error ? err.message : "Failed to record reflection."
      );
    } finally {
      setIsSubmittingReflection(false);
    }
  };

  // Group actions by time of day
  const grouped = dailyPath.actions.reduce<
    Record<string, typeof dailyPath.actions>
  >((acc, action) => {
    const key = action.timeOfDay || "Anytime";
    if (!acc[key]) acc[key] = [];
    acc[key].push(action);
    return acc;
  }, {});

  const timeOrder = ["Morning", "Afternoon", "Evening", "Anytime"];
  const sortedGroups = timeOrder.filter((t) => grouped[t]);

  return (
      <main className="daily-path-view mx-auto w-full max-w-5xl px-4 py-8 sm:px-8 sm:py-12 animate-luma-fade-in">
        {/* Navigation Bar */}
        <nav
          className="mb-8 flex items-center justify-between border-b border-luma-primary/10 pb-4"
          aria-label="Daily path navigation"
        >
          <div className="flex items-center gap-4 text-sm">
            <Link
              href={`/goals?id=${dream.id}`}
              className="inline-flex items-center gap-2 font-medium text-luma-primary transition-opacity hover:opacity-70"
            >
              <span aria-hidden="true">←</span>
              <span>Goals &amp; Gaps</span>
            </Link>
            <span className="text-luma-primary/30">|</span>
            <Link
              href={`/your-world?id=${dream.id}`}
              className="font-medium text-luma-text-secondary transition-opacity hover:opacity-70"
            >
              Your World
            </Link>
            <span className="text-luma-primary/30">|</span>
            <Link
              href={`/journey?id=${dream.id}`}
              className="font-medium text-luma-text-secondary transition-opacity hover:opacity-70"
            >
              My Journey
            </Link>
          </div>
          <span className="font-serif text-xs tracking-[0.2em] text-luma-primary/55">
            LUMA · TODAY&apos;S PATH &amp; PROGRESS
          </span>
        </nav>

        {/* Header */}
        <header className="mx-auto mb-8 max-w-3xl text-center sm:mb-10">
          <div className="mb-4 inline-flex items-center gap-2 rounded-full border border-luma-primary/10 bg-white/60 px-4 py-1.5 backdrop-blur-sm">
            <span className="text-xs text-luma-accent" aria-hidden="true">✦</span>
            <p className="text-xs font-semibold uppercase tracking-[0.22em] text-luma-text-secondary">
              Your future starts today
            </p>
          </div>
          <h1 className="font-serif text-4xl leading-tight tracking-tight text-luma-primary sm:text-5xl">
            Small steps toward your possible tomorrow.
          </h1>
          <p className="mt-4 font-serif text-lg italic text-luma-text-secondary sm:text-xl leading-relaxed">
            Each action is a gentle bridge between where you stand and the future you imagined.
          </p>
        </header>

        {/* Momentum & Progress Summary Card */}
        <section
          className="mx-auto mb-10 max-w-3xl rounded-[2rem] border border-luma-primary/15 bg-white/60 p-6 sm:p-8 shadow-[var(--luma-shadow)] backdrop-blur-md"
          aria-label="Today's Progress & Momentum"
        >
          <div className="flex flex-wrap items-center justify-between gap-4 mb-4">
            <div>
              <p className="text-xs font-semibold uppercase tracking-[0.2em] text-luma-text-secondary">
                Today&apos;s Momentum
              </p>
              <h2 className="font-serif text-xl sm:text-2xl text-luma-primary font-medium mt-0.5">
                {completed === total && total > 0
                  ? "Day Honored ✦"
                  : completed > 0
                  ? `${completed} of ${total} Steps Completed`
                  : "Ready for Today"}
              </h2>
            </div>
            <div className="flex items-center gap-3">
              <span className="inline-flex items-center gap-1.5 rounded-full bg-luma-accent/25 border border-luma-primary/10 px-3.5 py-1 text-xs font-semibold text-luma-primary">
                ✓ {completed} Done
              </span>
              {skipped > 0 && (
                <span className="inline-flex items-center gap-1.5 rounded-full bg-luma-surface/60 border border-luma-primary/10 px-3 py-1 text-xs font-medium text-luma-text-secondary">
                  ☕ {skipped} Rested
                </span>
              )}
              <span className="font-serif text-sm font-semibold text-luma-primary">
                {progressPercent}%
              </span>
            </div>
          </div>

          {/* Progress Bar */}
          <div className="w-full bg-luma-primary/10 h-2.5 rounded-full overflow-hidden p-0.5 mb-4">
            <div
              className="bg-gradient-to-r from-luma-accent via-luma-surface to-luma-primary h-full rounded-full transition-all duration-500 ease-out"
              style={{ width: `${progressPercent}%` }}
            />
          </div>

          {/* Reflection quote */}
          <p className="font-serif text-sm text-luma-text-secondary italic leading-relaxed">
            &ldquo;{dailyPath.momentumMessage || "Every journey begins with a single intentional breath."}&rdquo;
          </p>
        </section>

        {/* Refresh Actions Link */}
        <div className="mb-8 flex justify-end">
          <button
            type="button"
            onClick={handleRefresh}
            disabled={isRefreshing}
            className="inline-flex items-center gap-2 rounded-full border border-luma-primary/15 bg-white/50 px-4 py-1.5 text-xs font-medium text-luma-primary backdrop-blur-sm transition-all hover:bg-white/70 disabled:opacity-50 disabled:cursor-not-allowed"
          >
            <span className={isRefreshing ? "animate-spin" : ""}>↻</span>
            {isRefreshing ? "Refreshing..." : "Regenerate daily path"}
          </button>
        </div>

        {/* Daily Actions by Time of Day */}
        <div className="space-y-10 mb-14">
          {sortedGroups.map((timeOfDay) => {
            const actions = grouped[timeOfDay];
            const icon = timeIcons[timeOfDay] || "✦";
            const gradient = timeColors[timeOfDay] || timeColors.Anytime;

            return (
              <section key={timeOfDay} aria-labelledby={`time-${timeOfDay}`}>
                {/* Time of Day Header */}
                <div className="flex items-center gap-3 mb-5">
                  <span className="text-2xl" aria-hidden="true">{icon}</span>
                  <h2
                    id={`time-${timeOfDay}`}
                    className="font-serif text-2xl text-luma-primary sm:text-3xl"
                  >
                    {timeOfDay}
                  </h2>
                  <div className="flex-1 h-px bg-luma-primary/10" />
                </div>

                {/* Actions Grid */}
                <div className="space-y-4">
                  {actions.map((action, index) => {
                    const isCompleted = action.status === "Completed";
                    const isSkipped = action.status === "Skipped";
                    const isUpdating = updatingActionId === action.id;

                    return (
                      <article
                        key={action.id || index}
                        className={`rounded-[1.5rem] border p-5 sm:p-7 shadow-[var(--luma-shadow)] backdrop-blur-md transition-all duration-300 ${
                          isCompleted
                            ? "border-emerald-500/30 bg-emerald-50/40 shadow-emerald-500/5"
                            : isSkipped
                            ? "border-luma-primary/10 bg-white/30 opacity-75"
                            : `border-luma-primary/15 bg-gradient-to-br ${gradient} hover:scale-[1.005]`
                        }`}
                        aria-labelledby={`action-title-${action.id}`}
                      >
                        <div className="flex flex-wrap items-start justify-between gap-3 mb-3">
                          <div className="flex-1 min-w-0">
                            <div className="flex items-center gap-2 mb-1 flex-wrap">
                              <h3
                                id={`action-title-${action.id}`}
                                className={`font-serif text-xl sm:text-2xl leading-snug transition-colors ${
                                  isCompleted
                                    ? "line-through text-luma-primary/70"
                                    : isSkipped
                                    ? "italic text-luma-text-secondary"
                                    : "text-luma-primary"
                                }`}
                              >
                                {action.title}
                              </h3>
                              {isCompleted && (
                                <span className="inline-flex items-center gap-1 rounded-full bg-emerald-100 text-emerald-800 text-[11px] font-semibold px-2.5 py-0.5">
                                  ✓ Completed
                                </span>
                              )}
                              {isSkipped && (
                                <span className="inline-flex items-center gap-1 rounded-full bg-stone-100 text-stone-700 text-[11px] font-medium px-2.5 py-0.5">
                                  ☕ Taking rest
                                </span>
                              )}
                            </div>
                            {action.goalTitle?.trim() && (
                              <div className="mt-1 flex items-center gap-1.5 text-xs text-luma-text-secondary flex-wrap">
                                <span className="text-luma-accent" aria-hidden="true">✦</span>
                                <span>İlişkili Hedef:</span>
                                {action.goalId ? (
                                  <Link
                                    href={`/goals?id=${encodeURIComponent(dream.id)}#goal-card-${action.goalId}`}
                                    className="font-medium text-luma-primary underline underline-offset-2 hover:text-luma-accent transition-colors"
                                  >
                                    {action.goalTitle}
                                  </Link>
                                ) : (
                                  <span className="font-medium text-luma-primary">{action.goalTitle}</span>
                                )}
                              </div>
                            )}
                          </div>

                          <span className="shrink-0 inline-flex items-center gap-1.5 rounded-full border border-luma-primary/10 bg-white/70 px-3 py-1 text-xs font-semibold text-luma-primary">
                            ⏱ {action.estimatedMinutes} min
                          </span>
                        </div>

                        <p
                          className={`font-serif text-sm sm:text-base leading-relaxed mb-4 ${
                            isCompleted || isSkipped
                              ? "text-luma-text-secondary/80"
                              : "text-luma-text-secondary"
                          }`}
                        >
                          {action.description}
                        </p>

                        {/* Goal Connection Bridge */}
                        {action.goalTitle?.trim() && (
                          <div className="rounded-xl border border-luma-primary/10 bg-white/40 px-3.5 py-2.5 backdrop-blur-sm mb-3 text-xs text-luma-text-secondary flex items-start gap-2">
                            <span className="text-luma-accent text-sm leading-none shrink-0" aria-hidden="true">✦</span>
                            <p className="leading-relaxed">
                              Bu adım, geleceğindeki <strong className="font-medium text-luma-primary">&ldquo;{action.goalTitle}&rdquo;</strong> hedefine giden yolu bugünün sakin ritminde destekler.
                            </p>
                          </div>
                        )}

                        {/* Intention */}
                        <div className="rounded-xl border border-luma-primary/10 bg-white/50 p-3 backdrop-blur-sm mb-4">
                          <p className="text-[11px] font-semibold uppercase tracking-[0.18em] text-luma-text-secondary mb-1 flex items-center gap-1.5">
                            <span className="text-luma-accent">✦</span> Intention
                          </p>
                          <p className="text-xs sm:text-sm text-luma-primary leading-relaxed italic">
                            {action.intention}
                          </p>
                        </div>

                        {/* Action Status Controls */}
                        <div className="flex flex-wrap items-center justify-between gap-3 pt-3 border-t border-luma-primary/10">
                          <div className="flex items-center gap-2">
                            {/* Complete Button */}
                            <button
                              type="button"
                              onClick={() => handleStatusChange(action, "Completed")}
                              disabled={isUpdating}
                              className={`inline-flex items-center gap-1.5 rounded-full px-4 py-1.5 text-xs font-medium transition-all ${
                                isCompleted
                                  ? "bg-emerald-600 text-white shadow-sm hover:bg-emerald-700"
                                  : "border border-luma-primary/20 bg-white/80 text-luma-primary hover:bg-white hover:border-emerald-500 hover:text-emerald-700"
                              } disabled:opacity-50`}
                            >
                              {isCompleted ? "✓ Completed" : "Mark as Done"}
                            </button>

                            {/* Skip / Rest Button */}
                            <button
                              type="button"
                              onClick={() => handleStatusChange(action, "Skipped")}
                              disabled={isUpdating}
                              className={`inline-flex items-center gap-1.5 rounded-full px-3.5 py-1.5 text-xs font-medium transition-all ${
                                isSkipped
                                  ? "bg-stone-600 text-white shadow-sm hover:bg-stone-700"
                                  : "border border-luma-primary/15 bg-white/40 text-luma-text-secondary hover:bg-white/70"
                              } disabled:opacity-50`}
                            >
                              {isSkipped ? "Rested" : "Skip for today"}
                            </button>
                          </div>

                          {(isCompleted || isSkipped) && (
                            <button
                              type="button"
                              onClick={() => handleStatusChange(action, "Pending")}
                              disabled={isUpdating}
                              className="text-[11px] font-medium text-luma-text-secondary/70 hover:text-luma-primary transition-colors underline underline-offset-2"
                            >
                              Reset
                            </button>
                          )}
                        </div>
                      </article>
                    );
                  })}
                </div>
              </section>
            );
          })}
        </div>

        {/* Future Evolution / Evening Reflection Section */}
        <section
          className="mx-auto mb-14 max-w-3xl rounded-[2rem] border border-luma-primary/15 bg-white/60 p-6 sm:p-10 shadow-[var(--luma-shadow)] backdrop-blur-md"
          aria-label="Evening Reflection & Future Evolution"
        >
          <div className="text-center mb-8">
            <div className="mb-3 inline-flex items-center gap-2 rounded-full border border-luma-primary/10 bg-white/80 px-3.5 py-1 backdrop-blur-sm">
              <span className="text-xs text-luma-accent">✦</span>
              <span className="text-[11px] font-semibold uppercase tracking-[0.2em] text-luma-text-secondary">
                Evening Reflection · Future Evolution
              </span>
            </div>
            <h2 className="font-serif text-2xl sm:text-3xl text-luma-primary font-medium">
              How did today shape who you are becoming?
            </h2>
            <p className="mt-2 font-serif text-sm text-luma-text-secondary sm:text-base max-w-lg mx-auto leading-relaxed">
              Take a quiet moment to reflect on your day. LUMA will connect your lived experience back to your evolving future possibility.
            </p>
          </div>

          <form onSubmit={handleReflectionSubmit} className="space-y-6">
            {/* Mood selector */}
            <div>
              <label className="block text-xs font-semibold uppercase tracking-[0.18em] text-luma-text-secondary mb-3">
                How did your day feel overall?
              </label>
              <div className="flex flex-wrap gap-2.5">
                {moodOptions.map((mood) => {
                  const isSelected = selectedMood === mood.label;
                  return (
                    <button
                      key={mood.label}
                      type="button"
                      onClick={() => setSelectedMood(mood.label)}
                      className={`inline-flex items-center gap-1.5 rounded-full px-4 py-2 text-xs font-medium transition-all ${
                        isSelected
                          ? "bg-luma-primary text-white shadow-sm scale-[1.03]"
                          : "border border-luma-primary/15 bg-white/60 text-luma-primary hover:bg-white hover:border-luma-primary/30"
                      }`}
                    >
                      <span>{mood.icon}</span>
                      <span>{mood.label}</span>
                    </button>
                  );
                })}
              </div>
            </div>

            {/* Reflection Notes Textarea */}
            <div>
              <label
                htmlFor="reflectionNotes"
                className="block text-xs font-semibold uppercase tracking-[0.18em] text-luma-text-secondary mb-2"
              >
                What felt aligned, or what friction did you notice? <span className="font-normal text-luma-text-secondary/70">(Optional)</span>
              </label>
              <textarea
                id="reflectionNotes"
                rows={3}
                value={reflectionNotes}
                onChange={(e) => setReflectionNotes(e.target.value)}
                placeholder="e.g. Taking 20 minutes in the morning gave me clarity. I felt unhurried..."
                className="w-full rounded-2xl border border-luma-primary/20 bg-white/70 p-4 text-sm text-luma-primary placeholder:text-luma-text-secondary/50 focus:border-luma-accent focus:bg-white focus:outline-none focus:ring-1 focus:ring-luma-accent/50 transition-all font-serif"
              />
            </div>

            {reflectionError && (
              <p className="text-xs text-rose-600 italic">{reflectionError}</p>
            )}

            <div className="flex justify-end">
              <Button
                type="submit"
                size="md"
                disabled={isSubmittingReflection}
                className="shadow-md"
              >
                {isSubmittingReflection ? (
                  <span>Listening &amp; Reflecting...</span>
                ) : (
                  <span>Reflect &amp; Evolve Future ✦</span>
                )}
              </Button>
            </div>
          </form>

          {/* Reflections List & Insights */}
          {reflections.length > 0 && (
            <div className="mt-10 pt-8 border-t border-luma-primary/10 space-y-6">
              <h3 className="text-xs font-semibold uppercase tracking-[0.2em] text-luma-text-secondary">
                Recent Future Reflections &amp; Insights
              </h3>

              <div className="space-y-4">
                {reflections.map((ref, idx) => (
                  <article
                    key={ref.id || idx}
                    className="rounded-2xl border border-luma-primary/10 bg-gradient-to-br from-white/80 via-luma-surface/40 to-white/60 p-5 sm:p-6 backdrop-blur-sm space-y-3"
                  >
                    <div className="flex items-center justify-between gap-2 text-xs">
                      <span className="inline-flex items-center gap-1 rounded-full bg-white/80 border border-luma-primary/10 px-3 py-0.5 font-medium text-luma-primary">
                        ✦ Felt {ref.mood}
                      </span>
                      <time className="text-luma-text-secondary text-[11px]">
                        {new Date(ref.createdAt).toLocaleDateString(undefined, {
                          month: "short",
                          day: "numeric",
                        })}
                      </time>
                    </div>

                    {ref.notes && (
                      <p className="font-serif text-sm text-luma-text-secondary italic">
                        &ldquo;{ref.notes}&rdquo;
                      </p>
                    )}

                    <div className="rounded-xl border border-luma-accent/30 bg-luma-accent/15 p-4">
                      <p className="text-[11px] font-semibold uppercase tracking-[0.18em] text-luma-primary mb-1 flex items-center gap-1.5">
                        <span className="text-luma-accent">✦</span> How this evolves your future path
                      </p>
                      <p className="font-serif text-xs sm:text-sm text-luma-primary leading-relaxed">
                        {ref.insight}
                      </p>
                    </div>
                  </article>
                ))}
              </div>
            </div>
          )}
        </section>

        {/* LUMA Full-Circle Reflection */}
        <section
          className="mx-auto mb-10 max-w-3xl rounded-[2rem] border border-luma-primary/15 bg-gradient-to-br from-white/70 via-white/50 to-white/30 p-8 sm:p-10 text-center shadow-[var(--luma-shadow)] backdrop-blur-md"
          aria-label="The LUMA Loop"
        >
          <div className="mb-3 inline-flex items-center gap-2 rounded-full border border-luma-primary/10 bg-white/60 px-3 py-1">
            <span className="text-xs text-luma-accent">✦</span>
            <span className="text-[11px] font-semibold uppercase tracking-[0.2em] text-luma-text-secondary">
              The LUMA Flow
            </span>
          </div>
          <h3 className="font-serif text-2xl text-luma-primary sm:text-3xl mb-3">
            Future → Reality → Goals → Today → Progress → Reflection → Future
          </h3>
          <p className="font-serif text-sm text-luma-text-secondary sm:text-base mb-6 max-w-xl mx-auto leading-relaxed">
            By acting and reflecting today, you subtly transform who you are becoming. Every choice and quiet effort brings your possible future into the present.
          </p>

          <div className="flex flex-wrap items-center justify-center gap-4">
            <Link href={`/your-world?id=${dream.id}`}>
              <Button size="md" variant="secondary">
                View Your World with Fresh Eyes →
              </Button>
            </Link>
          </div>
        </section>

        {/* Bottom Navigation */}
        <footer className="py-8 text-center space-y-4">
          <div className="flex flex-col sm:flex-row items-center justify-center gap-4">
            <Link href={`/goals?id=${dream.id}`}>
              <Button size="lg" variant="secondary">
                ← Back to Goals &amp; Gaps
              </Button>
            </Link>
            <Link href={`/your-world?id=${dream.id}`}>
              <Button size="lg" variant="secondary">
                ← Return to Your World
              </Button>
            </Link>
            <Link href="/discover">
              <Button size="lg" className="shadow-lg">
                Imagine another possibility <span aria-hidden="true">✦</span>
              </Button>
            </Link>
          </div>
          <p className="text-xs text-luma-text-secondary">
            Small consistent steps shape big futures. Be gentle with yourself.
          </p>
        </footer>
      </main>
  );
};
