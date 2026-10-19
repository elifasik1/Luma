"use client";

import React, { useState } from "react";
import Link from "next/link";
import type { DreamDetailsResponse, GoalResponse, DailyPathResponse } from "@/lib/types";
import { Button } from "@/components/ui/Button";

interface GoalsViewProps {
  dream: DreamDetailsResponse;
  goals: GoalResponse[];
  dailyPath?: DailyPathResponse | null;
}

const categoryIcons: Record<string, string> = {
  Career: "💼",
  Lifestyle: "🌱",
  Creativity: "🎨",
  PersonalGrowth: "✨",
  Financial: "🪙",
  Health: "🌿",
  Travel: "🧭",
  Relationships: "🤝",
};

function getInitialGoalIdFromHash(): string | null {
  if (typeof window !== "undefined" && window.location.hash.startsWith("#goal-card-")) {
    return window.location.hash.replace("#goal-card-", "") || null;
  }
  return null;
}

export const GoalsView: React.FC<GoalsViewProps> = ({ dream, goals, dailyPath }) => {
  const [selectedGoalId, setSelectedGoalId] = useState<string | null>(getInitialGoalIdFromHash);
  return (
      <main className="goals-view mx-auto w-full max-w-5xl px-4 py-8 sm:px-8 sm:py-12 animate-luma-fade-in">
        {/* Navigation Bar */}
        <nav
          className="mb-8 flex flex-wrap items-center justify-between gap-3 border-b border-luma-primary/10 pb-4"
          aria-label="Goals navigation"
        >
          <div className="flex flex-wrap items-center gap-4 text-sm">
            <Link
              href={`/future-scenario?id=${dream.id}`}
              className="inline-flex items-center gap-2 font-medium text-luma-primary transition-opacity hover:opacity-70"
            >
              <span aria-hidden="true">←</span>
              <span>Future Scenario</span>
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
              href={`/daily-path?id=${dream.id}`}
              className="font-medium text-luma-primary transition-opacity hover:opacity-70"
            >
              Today
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
            LUMA · PATHS &amp; GAPS
          </span>
        </nav>

        {/* Header */}
        <header className="mx-auto mb-10 max-w-3xl text-center sm:mb-14">
          <div className="mb-4 inline-flex items-center gap-2 rounded-full border border-luma-primary/10 bg-white/60 px-4 py-1.5 backdrop-blur-sm">
            <span className="text-xs text-luma-accent" aria-hidden="true">✦</span>
            <p className="text-xs font-semibold uppercase tracking-[0.22em] text-luma-text-secondary">
              Your future is becoming clearer
            </p>
          </div>
          <h1 className="font-serif text-4xl leading-tight tracking-tight text-luma-primary sm:text-5xl">
            Here are the paths that could bring you closer.
          </h1>
          <p className="mt-4 font-serif text-lg italic text-luma-text-secondary sm:text-xl leading-relaxed">
            Meaningful life bridges to move from where you stand today toward your possible future.
          </p>
        </header>

        {/* Goals & Gaps Cards */}
        <div className="space-y-8 sm:space-y-10 mb-14">
          {goals.map((goal, index) => {
            const icon = categoryIcons[goal.category] || "✦";
            const isSelected = selectedGoalId === goal.id;
            const matchingActions = (dailyPath?.actions ?? []).filter((action) => {
              if (action.goalId && action.goalId === goal.id) return true;
              if (
                action.goalTitle &&
                goal.title &&
                action.goalTitle.trim().toLowerCase() === goal.title.trim().toLowerCase()
              ) {
                return true;
              }
              return false;
            });

            return (
              <article
                key={goal.id || index}
                id={`goal-card-${goal.id}`}
                className={`rounded-[2rem] border transition-all duration-300 p-6 sm:p-10 shadow-[var(--luma-shadow)] backdrop-blur-md ${
                  isSelected
                    ? "border-luma-accent/60 bg-white/80 ring-2 ring-luma-accent/20"
                    : "border-luma-primary/15 bg-white/50 hover:bg-white/65"
                }`}
                aria-labelledby={`goal-title-${goal.id}`}
              >
                {/* Goal Top Banner */}
                <div className="flex flex-wrap items-center justify-between gap-3 mb-4">
                  <div className="flex items-center gap-2">
                    <span className="flex h-7 w-7 items-center justify-center rounded-full bg-white/80 border border-luma-primary/10 text-sm">
                      {icon}
                    </span>
                    <span className="rounded-full border border-luma-primary/10 bg-white/50 px-3 py-0.5 text-xs font-semibold uppercase tracking-wider text-luma-primary">
                      {goal.category}
                    </span>
                  </div>
                  <div className="flex items-center gap-3">
                    <button
                      type="button"
                      onClick={() => setSelectedGoalId(isSelected ? null : goal.id)}
                      className={`inline-flex items-center gap-1.5 rounded-full px-3 py-1 text-xs font-medium transition-all ${
                        isSelected
                          ? "border border-luma-accent bg-luma-accent/20 text-luma-primary shadow-sm"
                          : "border border-luma-primary/15 bg-white/70 text-luma-text-secondary hover:border-luma-primary/30 hover:text-luma-primary"
                      }`}
                      aria-expanded={isSelected}
                    >
                      <span aria-hidden="true">{matchingActions.length > 0 ? "🌱" : "✦"}</span>
                      <span>
                        {matchingActions.length > 0
                          ? `Bugünkü Adımlar (${matchingActions.length})`
                          : "Bugünkü Adımları Gör"}
                      </span>
                      <span
                        aria-hidden="true"
                        className={`text-[10px] transform transition-transform duration-200 ${
                          isSelected ? "rotate-180" : ""
                        }`}
                      >
                        ▼
                      </span>
                    </button>
                    <span className="font-serif text-xs font-medium tracking-[0.16em] text-luma-text-secondary">
                      PATH 0{goal.order || index + 1}
                    </span>
                  </div>
                </div>

                <h2
                  id={`goal-title-${goal.id}`}
                  className="font-serif text-2xl text-luma-primary sm:text-3xl leading-snug mb-3"
                >
                  {goal.title}
                </h2>

                <p className="font-serif text-base text-luma-text-secondary sm:text-lg leading-relaxed mb-8">
                  {goal.description}
                </p>

                {/* Structured Breakdown: Why It Matters / Starting Point / The Bridge / Desired Outcome */}
                <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
                  {/* Why It Matters */}
                  <div className="rounded-2xl border border-luma-primary/10 bg-white/40 p-4 backdrop-blur-sm flex flex-col justify-between">
                    <div>
                      <p className="text-[11px] font-semibold uppercase tracking-[0.18em] text-luma-text-secondary mb-1.5 flex items-center gap-1.5">
                        <span className="text-luma-accent">✦</span> Why It Matters
                      </p>
                      <p className="text-xs sm:text-sm text-luma-primary leading-relaxed">
                        {goal.whyItMatters}
                      </p>
                    </div>
                  </div>

                  {/* Starting Point */}
                  <div className="rounded-2xl border border-luma-primary/10 bg-white/40 p-4 backdrop-blur-sm flex flex-col justify-between">
                    <div>
                      <p className="text-[11px] font-semibold uppercase tracking-[0.18em] text-luma-text-secondary mb-1.5 flex items-center gap-1.5">
                        <span>📍</span> Current Starting Point
                      </p>
                      <p className="text-xs sm:text-sm text-luma-primary leading-relaxed">
                        {goal.currentState}
                      </p>
                    </div>
                  </div>

                  {/* The Bridge / Gap */}
                  <div className="rounded-2xl border border-luma-accent/30 bg-luma-accent/15 p-4 backdrop-blur-sm flex flex-col justify-between">
                    <div>
                      <p className="text-[11px] font-semibold uppercase tracking-[0.18em] text-luma-primary mb-1.5 flex items-center gap-1.5">
                        <span>🌉</span> The Bridge
                      </p>
                      <p className="text-xs sm:text-sm text-luma-primary leading-relaxed">
                        {goal.gap}
                      </p>
                    </div>
                  </div>

                  {/* Desired Outcome */}
                  <div className="rounded-2xl border border-luma-primary/10 bg-white/40 p-4 backdrop-blur-sm flex flex-col justify-between">
                    <div>
                      <p className="text-[11px] font-semibold uppercase tracking-[0.18em] text-luma-text-secondary mb-1.5 flex items-center gap-1.5">
                        <span>🎯</span> Desired Outcome
                      </p>
                      <p className="text-xs sm:text-sm text-luma-primary leading-relaxed">
                        {goal.desiredOutcome}
                      </p>
                    </div>
                  </div>
                </div>

                {/* Today's Connected Actions Section */}
                {isSelected && (
                  <section
                    className="mt-6 rounded-2xl border border-luma-accent/25 bg-white/70 p-5 shadow-sm backdrop-blur-md animate-luma-fade-in"
                    aria-label={`${goal.title} ile ilişkili bugünkü adımlar`}
                  >
                    <div className="flex flex-wrap items-center justify-between gap-3 mb-4 border-b border-luma-primary/10 pb-3">
                      <div className="flex items-center gap-2">
                        <span className="text-sm text-luma-accent" aria-hidden="true">✦</span>
                        <h3 className="font-serif text-sm font-semibold text-luma-primary">
                          Bu Hedefi Destekleyen Bugünkü Adımlar
                        </h3>
                      </div>
                      <Link
                        href={`/daily-path?id=${dream.id}`}
                        className="text-xs font-medium text-luma-primary underline underline-offset-4 hover:opacity-75"
                      >
                        Today ekranında görüntüle →
                      </Link>
                    </div>

                    {matchingActions.length > 0 ? (
                      <div className="space-y-3">
                        {matchingActions.map((action) => {
                          const isCompleted = action.status === "Completed";
                          const isSkipped = action.status === "Skipped";

                          return (
                            <div
                              key={action.id}
                              className={`flex flex-col sm:flex-row sm:items-center justify-between gap-3 rounded-xl border p-3.5 transition-colors ${
                                isCompleted
                                  ? "border-emerald-200/60 bg-emerald-50/50"
                                  : isSkipped
                                  ? "border-luma-primary/10 bg-luma-bg/40 opacity-75"
                                  : "border-luma-primary/10 bg-white/85"
                              }`}
                            >
                              <div className="space-y-1">
                                <div className="flex items-center gap-2">
                                  <span className="text-[10px] font-semibold uppercase tracking-wider text-luma-text-secondary">
                                    {action.timeOfDay} · {action.estimatedMinutes} dk
                                  </span>
                                  <span
                                    className={`inline-flex items-center rounded-full px-2 py-0.5 text-[10px] font-semibold ${
                                      isCompleted
                                        ? "bg-emerald-100 text-emerald-800"
                                        : isSkipped
                                        ? "bg-amber-100 text-amber-800"
                                        : "bg-luma-accent/25 text-luma-primary"
                                    }`}
                                  >
                                    {isCompleted ? "Tamamlandı" : isSkipped ? "Ertelendi" : "Bekliyor"}
                                  </span>
                                </div>
                                <h4
                                  className={`text-sm font-medium ${
                                    isCompleted ? "line-through text-luma-text-secondary" : "text-luma-primary"
                                  }`}
                                >
                                  {action.title}
                                </h4>
                                {action.description && (
                                  <p className="text-xs text-luma-text-secondary leading-relaxed">
                                    {action.description}
                                  </p>
                                )}
                              </div>

                              <Link
                                href={`/daily-path?id=${dream.id}`}
                                className="shrink-0 text-xs font-medium text-luma-primary underline underline-offset-2 hover:opacity-80"
                              >
                                Today&apos;e git
                              </Link>
                            </div>
                          );
                        })}
                      </div>
                    ) : (
                      <div className="rounded-xl border border-dashed border-luma-primary/15 bg-luma-bg/40 p-4 text-center">
                        <p className="text-xs text-luma-text-secondary leading-relaxed">
                          Bu hedef için bugüne özel henüz bir aksiyon planlanmamış.
                        </p>
                        <div className="mt-3 flex items-center justify-center gap-3 text-xs">
                          <Link
                            href={`/talk?id=${dream.id}`}
                            className="font-medium text-luma-primary underline underline-offset-2"
                          >
                            LUMA ile planla
                          </Link>
                          <span className="text-luma-text-secondary">·</span>
                          <Link
                            href={`/daily-path?id=${dream.id}`}
                            className="font-medium text-luma-primary underline underline-offset-2"
                          >
                            Today planını yenile
                          </Link>
                        </div>
                      </div>
                    )}
                  </section>
                )}
              </article>
            );
          })}
        </div>

        {/* Next Step / Journey CTA */}
        <section
          className="mx-auto mb-10 max-w-3xl rounded-[2rem] border border-luma-primary/15 bg-gradient-to-br from-white/70 via-white/50 to-white/30 p-8 sm:p-10 text-center shadow-[var(--luma-shadow)] backdrop-blur-md"
          aria-label="Next Horizon"
        >
          <p className="mb-2 text-xs font-semibold uppercase tracking-[0.22em] text-luma-text-secondary">
            The Next Step in Your Journey
          </p>
          <h3 className="font-serif text-2xl text-luma-primary sm:text-3xl mb-3">
            From broad life paths to concrete daily action.
          </h3>
          <p className="font-serif text-sm text-luma-text-secondary sm:text-base mb-6 max-w-lg mx-auto leading-relaxed">
            LUMA can translate these life directions into gentle, focused daily steps.
          </p>

          <div className="inline-flex flex-col sm:flex-row items-center justify-center gap-4">
            <Link href={`/daily-path?id=${dream.id}`}>
              <Button size="lg" className="shadow-lg">
                See today&apos;s path <span aria-hidden="true">→</span>
              </Button>
            </Link>
          </div>
        </section>

        {/* Bottom Navigation */}
        <footer className="py-8 text-center space-y-4">
          <div className="flex flex-col sm:flex-row items-center justify-center gap-4">
            <Link href={`/future-scenario?id=${dream.id}`}>
              <Button size="lg" variant="secondary">
                ← Return to Future Scenario
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
            A possible future, not a prediction. Every choice today shapes tomorrow.
          </p>
        </footer>
      </main>
  );
};
