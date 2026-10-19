"use client";

import React from "react";
import Link from "next/link";
import type { DreamDetailsResponse, FutureScenarioResponse } from "@/lib/types";
import { Button } from "@/components/ui/Button";

interface FutureScenarioViewProps {
  dream: DreamDetailsResponse;
  scenario: FutureScenarioResponse;
}

export const FutureScenarioView: React.FC<FutureScenarioViewProps> = ({
  dream,
  scenario,
}) => {
  return (
      <main className="future-scenario mx-auto w-full max-w-4xl px-4 py-8 sm:px-8 sm:py-12 animate-luma-fade-in">
        {/* Navigation Bar */}
        <nav
          className="mb-8 flex flex-wrap items-center justify-between gap-3 border-b border-luma-primary/10 pb-4"
          aria-label="Future scenario navigation"
        >
          <div className="flex flex-wrap items-center gap-4 text-sm">
            <Link
              href={`/your-world?id=${dream.id}`}
              className="inline-flex items-center gap-2 font-medium text-luma-primary transition-opacity hover:opacity-70"
            >
              <span aria-hidden="true">←</span>
              <span>Back to Your World</span>
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
            LUMA · FUTURE SCENARIO
          </span>
        </nav>

        {/* Experience Header */}
        <header className="mx-auto mb-10 max-w-2xl text-center sm:mb-14">
          <div className="mb-4 inline-flex items-center gap-2 rounded-full border border-luma-primary/10 bg-white/60 px-4 py-1.5 backdrop-blur-sm">
            <span className="text-xs text-luma-accent" aria-hidden="true">✦</span>
            <p className="text-xs font-semibold uppercase tracking-[0.22em] text-luma-text-secondary">
              A possible future, shaped by what you told me
            </p>
          </div>
          <h1 className="font-serif text-4xl leading-tight tracking-tight text-luma-primary sm:text-5xl">
            {scenario.title}
          </h1>
          <p className="mt-4 font-serif text-lg italic text-luma-text-secondary sm:text-xl leading-relaxed">
            “{scenario.intro}”
          </p>
        </header>

        {/* Storyline Flow: Morning / Daytime / Evening */}
        <div className="space-y-6 sm:space-y-8 mb-12">
          {/* Morning */}
          <section
            className="rounded-[1.75rem] border border-luma-primary/10 bg-white/45 p-6 sm:p-8 shadow-[var(--luma-shadow)] transition-all duration-300 hover:bg-white/60 backdrop-blur-sm"
            aria-label="Morning"
          >
            <div className="flex items-center gap-3 mb-3">
              <span className="flex h-8 w-8 items-center justify-center rounded-full bg-luma-accent/25 text-base">
                🌅
              </span>
              <h2 className="font-serif text-xs font-semibold uppercase tracking-[0.2em] text-luma-text-secondary">
                Morning Rhythm
              </h2>
            </div>
            <p className="font-serif text-lg leading-relaxed text-luma-primary sm:text-xl">
              {scenario.morning}
            </p>
          </section>

          {/* Daytime */}
          <section
            className="rounded-[1.75rem] border border-luma-primary/10 bg-white/45 p-6 sm:p-8 shadow-[var(--luma-shadow)] transition-all duration-300 hover:bg-white/60 backdrop-blur-sm"
            aria-label="Daytime"
          >
            <div className="flex items-center gap-3 mb-3">
              <span className="flex h-8 w-8 items-center justify-center rounded-full bg-luma-muted/40 text-base">
                ☀️
              </span>
              <h2 className="font-serif text-xs font-semibold uppercase tracking-[0.2em] text-luma-text-secondary">
                Daytime Flow
              </h2>
            </div>
            <p className="font-serif text-lg leading-relaxed text-luma-primary sm:text-xl">
              {scenario.daytime}
            </p>
          </section>

          {/* Evening */}
          <section
            className="rounded-[1.75rem] border border-luma-primary/10 bg-white/45 p-6 sm:p-8 shadow-[var(--luma-shadow)] transition-all duration-300 hover:bg-white/60 backdrop-blur-sm"
            aria-label="Evening"
          >
            <div className="flex items-center gap-3 mb-3">
              <span className="flex h-8 w-8 items-center justify-center rounded-full bg-luma-surface/60 text-base">
                🌙
              </span>
              <h2 className="font-serif text-xs font-semibold uppercase tracking-[0.2em] text-luma-text-secondary">
                Evening Wind Down
              </h2>
            </div>
            <p className="font-serif text-lg leading-relaxed text-luma-primary sm:text-xl">
              {scenario.evening}
            </p>
          </section>
        </div>

        {/* Emotional Tone & Closing Reflection */}
        <section
          className="mx-auto mb-12 max-w-3xl rounded-[2rem] border border-luma-primary/15 bg-gradient-to-br from-white/70 via-white/50 to-white/30 p-8 sm:p-10 text-center shadow-[var(--luma-shadow)] backdrop-blur-md"
          aria-label="Emotional Resonance & Reflection"
        >
          <p className="mb-2 text-xs font-semibold uppercase tracking-[0.22em] text-luma-text-secondary">
            Emotional Resonance
          </p>
          <h3 className="font-serif text-2xl text-luma-primary sm:text-3xl mb-6">
            {scenario.emotionalTone}
          </h3>

          <div className="mx-auto w-12 h-px bg-luma-primary/15 mb-6" aria-hidden="true" />

          <p className="font-serif text-base italic leading-relaxed text-luma-text-secondary sm:text-lg">
            “{scenario.closingReflection}”
          </p>
        </section>

        {/* Next Steps / Journey Continuation */}
        <footer className="py-8 text-center space-y-5">
          <div className="flex flex-col sm:flex-row items-center justify-center gap-4">
            <Link href={`/your-world?id=${dream.id}`}>
              <Button size="lg" variant="secondary">
                ← Return to Your World
              </Button>
            </Link>
            <Link href={`/goals?id=${dream.id}`}>
              <Button size="lg" className="shadow-lg">
                <span>See the paths forward →</span>
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
