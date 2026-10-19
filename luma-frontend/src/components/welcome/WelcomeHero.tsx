"use client";

import React, { useState } from "react";
import Link from "next/link";

type PreviewMode = "momentum" | "adaptive" | "horizon";
type LifeCanvasTheme = "forest_mist" | "ocean_light" | "city_night";

export function WelcomeHero() {
  const [activePreview, setActivePreview] = useState<PreviewMode>("adaptive");
  const [isAdaptedPlan, setIsAdaptedPlan] = useState(false);
  const [lifeCanvasTheme, setLifeCanvasTheme] = useState<LifeCanvasTheme>("forest_mist");

  const previewModes: PreviewMode[] = ["adaptive", "momentum", "horizon"];
  const handlePreviewKeyDown = (event: React.KeyboardEvent<HTMLButtonElement>) => {
    const currentIndex = previewModes.indexOf(activePreview);
    const nextIndex =
      event.key === "ArrowRight"
        ? (currentIndex + 1) % previewModes.length
        : event.key === "ArrowLeft"
          ? (currentIndex - 1 + previewModes.length) % previewModes.length
          : -1;

    if (nextIndex >= 0) {
      event.preventDefault();
      setActivePreview(previewModes[nextIndex]);
    }
  };

  return (
    <div className="relative w-full overflow-hidden">
      {/* Background Ambient Glows (strictly pointer-events-none) */}
      <div
        aria-hidden="true"
        className="pointer-events-none absolute left-1/2 top-0 -z-10 h-[38rem] w-[90vw] max-w-6xl -translate-x-1/2 opacity-60 blur-3xl"
        style={{
          background:
            "radial-gradient(circle at 50% 25%, color-mix(in srgb, var(--luma-accent, #F7A695) 28%, transparent), color-mix(in srgb, var(--luma-surface, #DCC7EB) 22%, transparent) 55%, transparent 75%)",
        }}
      />
      <div
        aria-hidden="true"
        className="pointer-events-none absolute right-[-10%] top-[45%] -z-10 h-96 w-96 rounded-full bg-luma-muted/25 blur-3xl"
      />
      <div
        aria-hidden="true"
        className="pointer-events-none absolute -left-20 top-[65%] -z-10 h-96 w-96 rounded-full bg-luma-surface/25 blur-3xl"
      />

      {/* =====================================================================
          1. NAVIGATION
          ===================================================================== */}
      <header className="sticky top-0 z-40 w-full border-b border-luma-primary/10 bg-luma-bg/85 backdrop-blur-md transition-colors">
        <div className="mx-auto flex h-18 max-w-7xl items-center justify-between px-5 sm:px-8 lg:px-12">
          {/* Logo */}
          <Link
            href="/"
            className="group flex items-center gap-2 font-serif text-2xl tracking-[0.18em] text-luma-primary transition-opacity hover:opacity-85"
            aria-label="LUMA home"
          >
            <span>LUMA</span>
            <span
              aria-hidden="true"
              className="text-sm text-luma-accent transition-transform duration-300 group-hover:rotate-45"
            >
              ✦
            </span>
          </Link>

          {/* Product Navigation */}
          <nav
            aria-label="Product navigation"
            className="hidden items-center gap-8 text-sm font-medium text-luma-text-secondary md:flex"
          >
            <a
              href="#preview"
              className="transition-colors hover:text-luma-primary"
            >
              Live Preview
            </a>
            <a
              href="#features"
              className="transition-colors hover:text-luma-primary"
            >
              How It Works
            </a>
            <a
              href="#alexa-showcase"
              className="inline-flex items-center gap-1.5 transition-colors hover:text-luma-primary"
            >
              <span>Alexa+ Simulator</span>
              <span className="rounded-full bg-luma-accent/20 px-2 py-0.5 text-[10px] font-semibold uppercase tracking-wider text-luma-primary">
                Demo
              </span>
            </a>
            <Link
              href="/discover"
              className="transition-colors hover:text-luma-primary"
            >
              Life Canvas
            </Link>
          </nav>

          {/* Right Action Buttons */}
          <div className="flex items-center gap-3 sm:gap-4">
            <Link
              href="/login"
              className="rounded-full border border-luma-primary/15 bg-white/50 px-4 py-2 text-xs font-medium text-luma-primary shadow-sm backdrop-blur-sm transition-all duration-200 hover:border-luma-primary/30 hover:bg-white/80 sm:px-5 sm:text-sm"
            >
              Log in / Demo
            </Link>
            <Link
              href="/discover"
              className="inline-flex items-center gap-1.5 rounded-full bg-luma-primary px-4 py-2 text-xs font-semibold text-luma-bg shadow-md shadow-luma-primary/15 transition-all duration-200 hover:-translate-y-0.5 hover:bg-opacity-95 sm:px-5 sm:text-sm"
            >
              <span>Start imagining</span>
              <span aria-hidden="true">→</span>
            </Link>
          </div>
        </div>
      </header>

      {/* =====================================================================
          2. HERO SECTION
          ===================================================================== */}
      <section
        className="relative mx-auto max-w-7xl px-5 pt-8 pb-16 sm:px-8 sm:pt-14 sm:pb-24 lg:px-12"
        aria-labelledby="hero-heading"
      >
        {/* Hackathon Eyebrow */}
        <div className="mb-6 flex justify-center lg:justify-start">
          <div className="inline-flex items-center gap-2 rounded-full border border-luma-primary/15 bg-luma-bg-secondary/70 px-4 py-1.5 shadow-sm backdrop-blur-md">
            <span className="text-xs text-luma-accent" aria-hidden="true">
              ✦
            </span>
            <span className="text-[11px] font-semibold uppercase tracking-[0.2em] text-luma-text-secondary sm:text-xs">
              Amazon Developer Hackathon 2026 · Adaptive AI Life Companion
            </span>
          </div>
        </div>

        {/* Hero Grid: Editorial Typography + Rich Product Preview */}
        <div className="grid items-center gap-12 lg:grid-cols-[1fr_1.1fr] lg:gap-14 xl:gap-20">
          {/* Left Column: Headlines & Calls to Action */}
          <div className="text-center lg:text-left">
            <h1
              id="hero-heading"
              className="font-serif text-4xl leading-[1.08] tracking-tight text-luma-primary sm:text-6xl lg:text-[4rem] xl:text-[4.5rem]"
            >
              Your future is{" "}
              <span className="relative whitespace-nowrap italic text-luma-accent underline decoration-luma-accent/40 decoration-wavy decoration-2 underline-offset-8">
                more than a dream.
              </span>
            </h1>

            <p className="mt-6 text-base leading-relaxed text-luma-text-secondary sm:text-lg lg:text-xl lg:leading-relaxed font-normal max-w-2xl mx-auto lg:mx-0">
              Imagine the life you want to live. Turn what matters to you into
              small, meaningful steps. And when life changes, let your plan
              change with you.
            </p>

            {/* CTAs */}
            <div className="mt-8 flex flex-wrap items-center justify-center gap-4 sm:mt-10 lg:justify-start">
              <Link
                href="/discover"
                className="group inline-flex items-center gap-3 rounded-full bg-luma-primary px-7 py-4 text-base font-semibold text-luma-bg shadow-lg shadow-luma-primary/20 transition-all duration-300 hover:-translate-y-0.5 hover:shadow-xl sm:px-8 sm:text-lg"
              >
                <span>Start imagining</span>
                <span
                  aria-hidden="true"
                  className="transition-transform duration-300 group-hover:translate-x-1"
                >
                  →
                </span>
              </Link>

              <Link
                href="/login?returnUrl=/talk"
                className="group inline-flex items-center gap-2.5 rounded-full border border-luma-primary/20 bg-white/70 px-6 py-4 text-base font-medium text-luma-primary shadow-sm backdrop-blur-md transition-all duration-300 hover:-translate-y-0.5 hover:border-luma-accent hover:bg-white hover:shadow-md sm:text-lg"
              >
                <span className="text-base text-luma-accent" aria-hidden="true">
                  🎙️
                </span>
                <span>Explore the Alexa+ demo</span>
              </Link>
            </div>

            {/* Trust Badges */}
            <div className="mt-8 flex flex-wrap items-center justify-center gap-y-2 gap-x-6 text-xs text-luma-text-secondary sm:mt-10 lg:justify-start">
              <div className="flex items-center gap-1.5">
                <span className="text-emerald-700">✓</span>
                <span>No punitive streaks</span>
              </div>
              <div className="flex items-center gap-1.5">
                <span className="text-emerald-700">✓</span>
                <span>Live Alexa+ voice simulator</span>
              </div>
              <div className="flex items-center gap-1.5">
                <span className="text-emerald-700">✓</span>
                <span>Model Context Protocol (MCP) agent</span>
              </div>
            </div>
          </div>

          {/* Right Column: Layered Live Product Preview Card */}
          <div id="preview" className="relative mx-auto w-full max-w-xl">
            {/* Interactive Preview Mode Tabs */}
            <div className="mb-3 flex items-center justify-between px-2">
              <span className="text-[11px] font-semibold uppercase tracking-[0.18em] text-luma-text-secondary">
                Product Experience
              </span>
              <div
                role="tablist"
                aria-label="Product experience previews"
                className="flex max-w-full items-center gap-1 overflow-x-auto rounded-full border border-luma-primary/15 bg-white/60 p-1 shadow-sm backdrop-blur-sm"
              >
                <button
                  type="button"
                  role="tab"
                  aria-selected={activePreview === "adaptive"}
                  tabIndex={activePreview === "adaptive" ? 0 : -1}
                  onKeyDown={handlePreviewKeyDown}
                  onClick={() => setActivePreview("adaptive")}
                  className={`rounded-full px-3 py-1 text-xs font-semibold transition-all ${
                    activePreview === "adaptive"
                      ? "bg-luma-primary text-luma-bg shadow-sm"
                      : "text-luma-text-secondary hover:text-luma-primary"
                  }`}
                >
                  Adaptive Assist ✦
                </button>
                <button
                  type="button"
                  role="tab"
                  aria-selected={activePreview === "momentum"}
                  tabIndex={activePreview === "momentum" ? 0 : -1}
                  onKeyDown={handlePreviewKeyDown}
                  onClick={() => setActivePreview("momentum")}
                  className={`rounded-full px-3 py-1 text-xs font-semibold transition-all ${
                    activePreview === "momentum"
                      ? "bg-luma-primary text-luma-bg shadow-sm"
                      : "text-luma-text-secondary hover:text-luma-primary"
                  }`}
                >
                  Daily Path
                </button>
                <button
                  type="button"
                  role="tab"
                  aria-selected={activePreview === "horizon"}
                  tabIndex={activePreview === "horizon" ? 0 : -1}
                  onKeyDown={handlePreviewKeyDown}
                  onClick={() => setActivePreview("horizon")}
                  className={`rounded-full px-3 py-1 text-xs font-semibold transition-all ${
                    activePreview === "horizon"
                      ? "bg-luma-primary text-luma-bg shadow-sm"
                      : "text-luma-text-secondary hover:text-luma-primary"
                  }`}
                >
                  Life Canvas
                </button>
              </div>
            </div>

            {/* The Main Preview Glassmorphic Card */}
            <div
              data-theme={activePreview === "horizon" ? lifeCanvasTheme : undefined}
              className="overflow-hidden rounded-[2rem] border border-luma-primary/20 bg-white/80 p-6 shadow-2xl shadow-luma-primary/10 backdrop-blur-xl sm:p-7 transition-all"
            >
              {/* Header: Visual World Anchor */}
              <div className="flex items-center justify-between border-b border-luma-primary/10 pb-4">
                <div className="flex items-center gap-3">
                  <div className="flex h-10 w-10 items-center justify-center rounded-2xl bg-luma-surface/80 text-xl shadow-sm">
                    🌿
                  </div>
                  <div>
                    <span className="text-[10px] font-bold uppercase tracking-widest text-luma-accent">
                      {activePreview === "adaptive"
                        ? "Adaptive Assist Preview"
                        : activePreview === "momentum"
                          ? "Daily Path Preview"
                          : "Life Canvas Preview"}
                    </span>
                    <h2 className="font-serif text-lg font-medium text-luma-primary">
                      Calm &amp; Free · Coastal Studio Life
                    </h2>
                  </div>
                </div>
                <span className="rounded-full bg-emerald-100/80 px-2.5 py-1 text-[11px] font-semibold text-emerald-800">
                  ● Today Active
                </span>
              </div>

              {activePreview === "adaptive" && (
                <div className="mt-5 space-y-4">
                  <div className="rounded-2xl border border-luma-primary/10 bg-luma-bg-secondary/50 p-4">
                    <div className="flex items-center justify-between text-xs">
                      <span className="font-semibold text-luma-primary">Adaptive recommendation</span>
                      <span className="font-serif font-bold text-luma-primary">68%</span>
                    </div>
                    <p className="mt-2 text-sm font-semibold text-luma-primary">Keep your creative momentum without spending your energy.</p>
                    <div className="mt-3 h-2 w-full overflow-hidden rounded-full bg-luma-primary/10">
                      <div className="h-full w-[68%] rounded-full bg-gradient-to-r from-luma-accent via-luma-surface to-luma-primary" />
                    </div>
                    <p className="mt-2 text-[11px] text-luma-text-secondary">Demo signal: a late meeting was detected, so the evening review moves to tomorrow.</p>
                  </div>
                  <div className="rounded-xl border border-amber-400/40 bg-amber-50/70 p-3.5">
                    <div className="flex items-center justify-between gap-3">
                      <span className="text-xs font-bold text-amber-950">Suggested adjustment</span>
                      <span className="text-[10px] font-semibold uppercase tracking-wider text-amber-900">Demo data</span>
                    </div>
                    <p className="mt-1.5 text-xs text-amber-950">15-min evening sunset walk · Rest protected</p>
                  </div>
                </div>
              )}

              {activePreview === "adaptive" && (
                <div className="mt-4 rounded-xl border border-luma-primary/10 bg-white/90 p-3.5 shadow-sm">
                  <div className="flex items-center justify-between text-xs font-semibold text-luma-primary">
                    <span>Today&apos;s flexible plan</span>
                    <span className="text-[10px] font-normal uppercase tracking-wider text-luma-text-secondary">Demo</span>
                  </div>
                  <p className="mt-1.5 text-xs text-luma-text-secondary">08:00 AM tea · 02:00 PM portfolio outline · 07:30 PM sunset walk</p>
                </div>
              )}

              {activePreview === "horizon" && (
                <div className="mt-5 space-y-4">
                  <div className="rounded-xl border border-luma-primary/10 bg-luma-bg-secondary/50 p-3">
                    <div className="flex items-center justify-between gap-3">
                      <span className="text-[10px] font-bold uppercase tracking-wider text-luma-text-secondary">Canvas theme</span>
                      <span className="text-[10px] text-luma-text-secondary">Demo preference</span>
                    </div>
                    <div className="mt-2 flex flex-wrap gap-2" role="radiogroup" aria-label="Life Canvas theme">
                      {([
                        ["forest_mist", "Forest Mist", "bg-luma-muted"],
                        ["ocean_light", "Ocean Light", "bg-luma-accent"],
                        ["city_night", "City Night", "bg-luma-primary"],
                      ] as const).map(([theme, label, swatch]) => (
                        <button
                          key={theme}
                          type="button"
                          role="radio"
                          aria-checked={lifeCanvasTheme === theme}
                          onClick={() => setLifeCanvasTheme(theme)}
                          className={`inline-flex items-center gap-1.5 rounded-full border px-2.5 py-1 text-[11px] font-semibold transition-all ${
                            lifeCanvasTheme === theme
                              ? "border-luma-primary bg-luma-primary text-luma-bg"
                              : "border-luma-primary/15 bg-white/70 text-luma-text-secondary hover:border-luma-primary/40 hover:text-luma-primary"
                          }`}
                        >
                          <span className={`h-2.5 w-2.5 rounded-full ${swatch}`} aria-hidden="true" />
                          {label}
                        </button>
                      ))}
                    </div>
                  </div>
                  <div className="rounded-2xl border border-luma-primary/10 bg-luma-bg-secondary/50 p-4">
                    <span className="text-[10px] font-bold uppercase tracking-wider text-luma-text-secondary">Selected lifestyle</span>
                    <h3 className="mt-2 font-serif text-2xl text-luma-primary">Calm &amp; Free · Coastal Studio Life</h3>
                    <p className="mt-2 text-sm leading-relaxed text-luma-text-secondary">A sunlit creative studio near the water, with room for deep work, nature, and unhurried mornings.</p>
                  </div>
                  <div className="grid grid-cols-2 gap-3 text-xs">
                    <div className="rounded-xl border border-luma-primary/10 bg-luma-surface/30 p-3"><span className="block text-[10px] uppercase tracking-wider text-luma-text-secondary">Environment</span><span className="mt-1 block font-semibold text-luma-primary">Coastal / Nature</span></div>
                    <div className="rounded-xl border border-luma-primary/10 bg-luma-surface/30 p-3"><span className="block text-[10px] uppercase tracking-wider text-luma-text-secondary">Future profile</span><span className="mt-1 block font-semibold text-luma-primary">Creative Maker</span></div>
                  </div>
                  <p className="text-[11px] italic text-luma-text-secondary">Illustrative demo profile — not real user data.</p>
                </div>
              )}

              {activePreview === "momentum" && <>
              {/* Goal Progress Section */}
              <div className="mt-5 rounded-2xl border border-luma-primary/10 bg-luma-bg-secondary/50 p-4">
                <div className="flex items-center justify-between text-xs">
                  <span className="font-semibold text-luma-primary">
                    Core Goal: Transition to Creative Studio Practice
                  </span>
                  <span className="font-serif font-bold text-luma-primary">
                    68%
                  </span>
                </div>
                {/* Progress Bar */}
                <div className="mt-2 h-2 w-full overflow-hidden rounded-full bg-luma-primary/10">
                  <div
                    className="h-full rounded-full bg-gradient-to-r from-luma-accent via-luma-surface to-luma-primary transition-all duration-500"
                    style={{ width: "68%" }}
                  />
                </div>
                <div className="mt-2.5 flex items-center justify-between text-[11px] text-luma-text-secondary">
                  <span>Milestone 2 of 3</span>
                  <span className="italic">
                    &ldquo;Protected morning ritual preserves creative flow&rdquo;
                  </span>
                </div>
              </div>

              {/* Today's Daily Actions */}
              <div className="mt-5 space-y-3">
                <div className="flex items-center justify-between text-xs font-semibold uppercase tracking-wider text-luma-text-secondary">
                  <span>Today&apos;s Path · 3 Small Actions</span>
                  <span className="text-[11px] font-normal normal-case text-emerald-800">
                    2 of 3 Honored
                  </span>
                </div>

                {/* Action 1: Morning Focus (Completed) */}
                <div className="flex items-start gap-3 rounded-xl border border-emerald-500/25 bg-emerald-50/50 p-3.5 transition-all">
                  <div className="mt-0.5 flex h-5 w-5 shrink-0 items-center justify-center rounded-full bg-emerald-600 text-white text-xs font-bold">
                    ✓
                  </div>
                  <div className="flex-1 min-w-0">
                    <div className="flex items-center justify-between gap-2">
                      <p className="text-sm font-semibold text-emerald-950 line-through opacity-85">
                        08:00 AM · Unhurried morning tea &amp; sketchbook
                      </p>
                      <span className="text-[10px] uppercase font-bold text-emerald-800">
                        Morning
                      </span>
                    </div>
                    <p className="text-xs text-emerald-800/80 mt-0.5">
                      Ground yourself before digital inputs.
                    </p>
                  </div>
                </div>

                {/* Action 2: Daytime Deep Work */}
                <div className="flex items-start gap-3 rounded-xl border border-luma-primary/15 bg-white/90 p-3.5 shadow-sm transition-all">
                  <div className="mt-0.5 flex h-5 w-5 shrink-0 items-center justify-center rounded-full border-2 border-luma-primary/40 text-[10px] text-luma-primary">
                    ●
                  </div>
                  <div className="flex-1 min-w-0">
                    <div className="flex items-center justify-between gap-2">
                      <p className="text-sm font-semibold text-luma-primary">
                        02:00 PM · 45-min studio portfolio outline
                      </p>
                      <span className="text-[10px] uppercase font-bold text-luma-text-secondary">
                        Afternoon
                      </span>
                    </div>
                    <p className="text-xs text-luma-text-secondary mt-0.5">
                      Focused session tied to creative milestone.
                    </p>
                  </div>
                </div>

                {/* Action 3: Adaptive Callout (Highlight for Hackathon) */}
                <div
                  className={`rounded-xl border p-3.5 transition-all duration-300 ${
                    isAdaptedPlan
                      ? "border-amber-400/40 bg-amber-50/70"
                      : "border-luma-accent/40 bg-luma-surface/30"
                  }`}
                >
                  <div className="flex items-start gap-3">
                    <span className="text-base" aria-hidden="true">
                      {isAdaptedPlan ? "☕" : "⚡"}
                    </span>
                    <div className="flex-1 min-w-0">
                      <div className="flex items-center justify-between">
                        <span className="inline-flex items-center gap-1 rounded-full bg-luma-primary/10 px-2 py-0.5 text-[10px] font-bold text-luma-primary">
                          Alexa+ Adaptive Recommendation
                        </span>
                        <button
                          type="button"
                          onClick={() => setIsAdaptedPlan(!isAdaptedPlan)}
                          className="text-[11px] font-semibold text-luma-accent underline hover:text-luma-primary"
                        >
                          {isAdaptedPlan ? "Undo adaptation" : "Simulate adapt"}
                        </button>
                      </div>

                      {isAdaptedPlan ? (
                        <div className="mt-1.5 text-xs text-amber-950">
                          <p className="font-semibold">
                            Adapted: 15-min evening sunset walk
                          </p>
                          <p className="text-[11px] text-amber-900/80 mt-0.5">
                            Late meetings detected. Evening review deferred to tomorrow;
                            tonight honors rest.
                          </p>
                        </div>
                      ) : (
                        <div className="mt-1.5 text-xs text-luma-primary">
                          <p className="font-semibold">
                            Energy dipped after 5 PM?
                          </p>
                          <p className="text-[11px] text-luma-text-secondary mt-0.5">
                            Ask Alexa+: &ldquo;I&apos;m tired today.&rdquo; LUMA adapts
                            the evening to preserve your rest.
                          </p>
                        </div>
                      )}
                    </div>
                  </div>
                </div>
              </div>

              {/* Bottom Card Footer */}
              <div className="mt-5 flex items-center justify-between border-t border-luma-primary/10 pt-3.5 text-xs text-luma-text-secondary">
                <span className="inline-flex items-center gap-1.5">
                  <span className="h-2 w-2 rounded-full bg-emerald-500 animate-pulse" />
                  Synced with Alexa+ Simulator
                </span>
                <Link
                  href="/login?returnUrl=/daily-path"
                  className="font-semibold text-luma-primary underline underline-offset-2 hover:text-luma-accent"
                >
                  Open Full Daily Path →
                </Link>
              </div>
              </>}
            </div>
          </div>
        </div>
      </section>

      {/* =====================================================================
          3. THREE FEATURE BLOCKS: IMAGINE · PLAN · ADAPT
          ===================================================================== */}
      <section
        id="features"
        className="relative border-t border-luma-primary/10 bg-luma-bg-secondary/40 py-20 sm:py-28"
        aria-labelledby="features-heading"
      >
        <div className="mx-auto max-w-7xl px-5 sm:px-8 lg:px-12">
          {/* Section Header */}
          <div className="mx-auto max-w-3xl text-center">
            <span className="text-xs font-semibold uppercase tracking-[0.25em] text-luma-accent">
              Core Architecture
            </span>
            <h2
              id="features-heading"
              className="mt-3 font-serif text-3xl tracking-tight text-luma-primary sm:text-5xl"
            >
              How LUMA builds your future.
            </h2>
            <p className="mt-4 text-base leading-relaxed text-luma-text-secondary sm:text-lg">
              Three connected layers designed to transform vague desires into tangible,
              forgiving, daily momentum.
            </p>
          </div>

          {/* Three Feature Cards Grid */}
          <div className="mt-14 grid gap-8 md:grid-cols-3 lg:gap-10">
            {/* Card 1: Imagine */}
            <div className="group relative flex flex-col justify-between rounded-[2rem] border border-luma-primary/15 bg-white/80 p-8 shadow-sm backdrop-blur-md transition-all duration-300 hover:-translate-y-1 hover:border-luma-accent/60 hover:shadow-xl">
              <div>
                <div className="flex h-12 w-12 items-center justify-center rounded-2xl bg-luma-surface/60 text-2xl shadow-sm">
                  🎨
                </div>
                <span className="mt-6 inline-block text-[11px] font-bold uppercase tracking-[0.2em] text-luma-accent">
                  Step 01 · Imagine
                </span>
                <h3 className="mt-2 font-serif text-2xl font-medium text-luma-primary">
                  Explore your future identity and aspirations.
                </h3>
                <p className="mt-3.5 text-sm leading-relaxed text-luma-text-secondary">
                  Before rigid task lists, step into the feeling of where you want to be.
                  LUMA’s Life Canvas shapes your aesthetic atmosphere, core anchors,
                  and a descriptive narrative of your imagined world.
                </p>
              </div>

              {/* Visual preview snippet */}
              <div className="mt-8 rounded-xl border border-luma-primary/10 bg-luma-bg/70 p-3.5">
                <span className="text-[10px] font-bold uppercase tracking-wider text-luma-text-secondary">
                  Life Canvas Environments
                </span>
                <div className="mt-2 flex flex-wrap gap-1.5">
                  <span className="rounded-full bg-luma-surface/80 px-2.5 py-0.5 text-xs text-luma-primary font-medium">
                    🌿 Calm &amp; Free
                  </span>
                  <span className="rounded-full bg-luma-bg-secondary px-2.5 py-0.5 text-xs text-luma-text-secondary">
                    🌲 Nature
                  </span>
                  <span className="rounded-full bg-luma-bg-secondary px-2.5 py-0.5 text-xs text-luma-text-secondary">
                    🏙️ Big City
                  </span>
                  <span className="rounded-full bg-luma-bg-secondary px-2.5 py-0.5 text-xs text-luma-text-secondary">
                    🛠️ Maker
                  </span>
                </div>
              </div>
            </div>

            {/* Card 2: Plan */}
            <div className="group relative flex flex-col justify-between rounded-[2rem] border border-luma-primary/15 bg-white/80 p-8 shadow-sm backdrop-blur-md transition-all duration-300 hover:-translate-y-1 hover:border-luma-accent/60 hover:shadow-xl">
              <div>
                <div className="flex h-12 w-12 items-center justify-center rounded-2xl bg-luma-accent/20 text-2xl shadow-sm">
                  🌅
                </div>
                <span className="mt-6 inline-block text-[11px] font-bold uppercase tracking-[0.2em] text-luma-accent">
                  Step 02 · Plan
                </span>
                <h3 className="mt-2 font-serif text-2xl font-medium text-luma-primary">
                  Translate goals into daily actions.
                </h3>
                <p className="mt-3.5 text-sm leading-relaxed text-luma-text-secondary">
                  Sweeping 5-year visions become manageable daily rhythms. LUMA creates
                  3 to 4 focused actions grouped by morning, afternoon, and evening—calibrated
                  to move major milestones forward without cognitive overload.
                </p>
              </div>

              {/* Visual preview snippet */}
              <div className="mt-8 rounded-xl border border-luma-primary/10 bg-luma-bg/70 p-3.5">
                <span className="text-[10px] font-bold uppercase tracking-wider text-luma-text-secondary">
                  Daily Rhythm Structure
                </span>
                <div className="mt-2 space-y-1.5 text-xs text-luma-primary font-medium">
                  <div className="flex items-center gap-2">
                    <span>🌅 Morning:</span>
                    <span className="text-luma-text-secondary font-normal">Intentional anchor</span>
                  </div>
                  <div className="flex items-center gap-2">
                    <span>☀️ Afternoon:</span>
                    <span className="text-luma-text-secondary font-normal">Deep work milestone</span>
                  </div>
                  <div className="flex items-center gap-2">
                    <span>🌙 Evening:</span>
                    <span className="text-luma-text-secondary font-normal">Rest &amp; reflection</span>
                  </div>
                </div>
              </div>
            </div>

            {/* Card 3: Adapt */}
            <div className="group relative flex flex-col justify-between rounded-[2rem] border border-luma-primary/15 bg-white/80 p-8 shadow-sm backdrop-blur-md transition-all duration-300 hover:-translate-y-1 hover:border-luma-accent/60 hover:shadow-xl">
              <div>
                <div className="flex h-12 w-12 items-center justify-center rounded-2xl bg-amber-100/70 text-2xl shadow-sm">
                  ⚡
                </div>
                <span className="mt-6 inline-block text-[11px] font-bold uppercase tracking-[0.2em] text-luma-accent">
                  Step 03 · Adapt
                </span>
                <h3 className="mt-2 font-serif text-2xl font-medium text-luma-primary">
                  Adjust the plan when time or energy changes.
                </h3>
                <p className="mt-3.5 text-sm leading-relaxed text-luma-text-secondary">
                  Life doesn&apos;t follow a static spreadsheet. When overtime strikes or
                  fatigue sets in, LUMA’s adaptive agent recalculates your schedule, protects
                  rest, and preserves streak-free forward momentum.
                </p>
              </div>

              {/* Visual preview snippet */}
              <div className="mt-8 rounded-xl border border-amber-300/40 bg-amber-50/60 p-3.5">
                <div className="flex items-center justify-between text-[10px] font-bold uppercase tracking-wider text-amber-900">
                  <span>Zero-Guilt Adaptation</span>
                  <span>MCP Agent Powered</span>
                </div>
                <p className="mt-1.5 text-xs text-amber-950 font-medium">
                  &ldquo;Postponed 1 task · Scheduled 20-min restorative tea break&rdquo;
                </p>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* =====================================================================
          4. ALEXA+ SHOWCASE (Amazon Developer Hackathon Simulator)
          ===================================================================== */}
      <section
        id="alexa-showcase"
        className="relative overflow-hidden py-20 sm:py-28"
        aria-labelledby="alexa-heading"
      >
        <div className="mx-auto max-w-7xl px-5 sm:px-8 lg:px-12">
          {/* Hackathon Simulator Tag */}
          <div className="mb-4 inline-flex items-center gap-2 rounded-full border border-luma-accent/30 bg-luma-surface/40 px-3.5 py-1 text-xs font-semibold uppercase tracking-wider text-luma-primary">
            <span>🎙️</span>
            <span>Web-Based Hackathon Simulator · Amazon Developer Showcase</span>
          </div>

          <div className="grid items-center gap-12 lg:grid-cols-2 lg:gap-16">
            {/* Left Column: Narrative & Agent Explanation */}
            <div>
              <h2
                id="alexa-heading"
                className="font-serif text-3xl leading-tight tracking-tight text-luma-primary sm:text-5xl"
              >
                Talk to your future companion with{" "}
                <span className="italic text-luma-accent">Alexa+.</span>
              </h2>

              <p className="mt-5 text-base leading-relaxed text-luma-text-secondary sm:text-lg">
                Experience the voice-enabled AI companion simulator. LUMA integrates
                with an MCP agent pipeline to inspect your goals, read today’s daily
                path, and rebalance actions through natural voice conversations.
              </p>

              {/* Simulator Capabilities List */}
              <div className="mt-8 space-y-4">
                <div className="flex items-start gap-3.5">
                  <div className="mt-1 flex h-6 w-6 shrink-0 items-center justify-center rounded-full bg-luma-primary text-xs text-luma-bg font-bold">
                    1
                  </div>
                  <div>
                    <h4 className="text-sm font-semibold text-luma-primary">
                      In-Browser Voice &amp; Speech Synthesis
                    </h4>
                    <p className="text-xs text-luma-text-secondary mt-0.5">
                      Interact using simulated voice recognition and synthesized audio playback
                      directly in your web browser.
                    </p>
                  </div>
                </div>

                <div className="flex items-start gap-3.5">
                  <div className="mt-1 flex h-6 w-6 shrink-0 items-center justify-center rounded-full bg-luma-primary text-xs text-luma-bg font-bold">
                    2
                  </div>
                  <div>
                    <h4 className="text-sm font-semibold text-luma-primary">
                      Real Model Context Protocol (MCP) Tools
                    </h4>
                    <p className="text-xs text-luma-text-secondary mt-0.5">
                      The agent calls real tools like <code className="bg-luma-primary/10 px-1 py-0.5 rounded text-[11px]">adapt_daily_path</code> and <code className="bg-luma-primary/10 px-1 py-0.5 rounded text-[11px]">inspect_goals</code> backed by PostgreSQL.
                    </p>
                  </div>
                </div>

                <div className="flex items-start gap-3.5">
                  <div className="mt-1 flex h-6 w-6 shrink-0 items-center justify-center rounded-full bg-luma-primary text-xs text-luma-bg font-bold">
                    3
                  </div>
                  <div>
                    <h4 className="text-sm font-semibold text-luma-primary">
                      Human-in-the-Loop Confirmation
                    </h4>
                    <p className="text-xs text-luma-text-secondary mt-0.5">
                      The Alexa+ agent never unilaterally rewrites your schedule without your explicit
                      spoken or clicked approval.
                    </p>
                  </div>
                </div>
              </div>

              {/* Launch Simulator CTA */}
              <div className="mt-10">
                <Link
                  href="/login?returnUrl=/talk"
                  className="group inline-flex items-center gap-2.5 rounded-full bg-luma-primary px-7 py-3.5 text-base font-semibold text-luma-bg shadow-md shadow-luma-primary/20 transition-all duration-200 hover:-translate-y-0.5 hover:shadow-lg"
                >
                  <span>Launch Alexa+ Simulator</span>
                  <span aria-hidden="true" className="transition-transform duration-200 group-hover:translate-x-1">
                    →
                  </span>
                </Link>
                <p className="mt-3 text-[11px] text-luma-text-secondary">
                  * Web simulator for hackathon evaluation. No physical Alexa hardware required.
                </p>
              </div>
            </div>

            {/* Right Column: Simulated Dialogue Preview Card */}
            <div className="rounded-[2rem] border border-luma-primary/20 bg-white/90 p-6 shadow-2xl backdrop-blur-xl sm:p-8">
              <div className="flex items-center justify-between border-b border-luma-primary/10 pb-4">
                <div className="flex items-center gap-2.5">
                  <span className="flex h-3 w-3 rounded-full bg-emerald-500 animate-ping" />
                  <span className="font-serif text-sm font-medium text-luma-primary">
                    Alexa+ Companion Simulator (Active Session)
                  </span>
                </div>
                <span className="text-[10px] font-bold uppercase tracking-wider text-luma-accent">
                  Live MCP
                </span>
              </div>

              {/* Simulated Chat Dialogue */}
              <div className="mt-6 space-y-4 text-xs sm:text-sm">
                {/* User Message */}
                <div className="flex items-start gap-3 justify-end">
                  <div className="max-w-[85%] rounded-2xl rounded-tr-sm bg-luma-primary px-4 py-3 text-luma-bg shadow-sm">
                    <p className="font-medium">
                      &ldquo;Alexa, I got pulled into late design reviews today and I&apos;m exhausted. What should I do about my plan?&rdquo;
                    </p>
                    <span className="mt-1 block text-right text-[10px] text-luma-bg/70">
                      Spoken Voice · 06:15 PM
                    </span>
                  </div>
                  <div className="flex h-8 w-8 shrink-0 items-center justify-center rounded-full bg-luma-surface/80 text-sm">
                    👤
                  </div>
                </div>

                {/* Tool Orchestration Notice */}
                <div className="flex items-center justify-center">
                  <span className="rounded-full bg-luma-bg-secondary px-3 py-1 text-[11px] font-medium text-luma-text-secondary border border-luma-primary/10">
                    ⚙️ MCP tool executed: <code className="font-semibold text-luma-primary">adapt_daily_path(energy: low)</code>
                  </span>
                </div>

                {/* Alexa+ Assistant Message */}
                <div className="flex items-start gap-3">
                  <div className="flex h-8 w-8 shrink-0 items-center justify-center rounded-full bg-luma-primary text-sm text-luma-bg font-serif">
                    ✦
                  </div>
                  <div className="max-w-[88%] rounded-2xl rounded-tl-sm border border-luma-primary/15 bg-luma-bg/80 px-4 py-3.5 shadow-sm">
                    <span className="block text-[10px] font-bold uppercase tracking-wider text-luma-accent mb-1">
                      Alexa+ (LUMA Companion)
                    </span>
                    <p className="text-luma-primary leading-relaxed">
                      &ldquo;I hear you. Protecting your rest tonight is more valuable than pushing through.
                      I suggest keeping your 10-minute evening tea ritual and deferring the portfolio review
                      to tomorrow at 10 AM. Shall I update your path?&rdquo;
                    </p>

                    {/* Proposed Action Interactive Confirmation */}
                    <div className="mt-3.5 rounded-xl border border-emerald-500/25 bg-emerald-50/70 p-2.5 text-xs text-emerald-950">
                      <div className="flex items-center justify-between font-semibold">
                        <span>Proposed Adaptive Plan:</span>
                        <span className="text-[10px] text-emerald-800">Requires confirmation</span>
                      </div>
                      <div className="mt-2 flex gap-2">
                        <Link
                          href="/login?returnUrl=/talk"
                          className="rounded-lg bg-emerald-700 px-3 py-1.5 text-xs font-semibold text-white shadow-sm hover:bg-emerald-800 transition-colors"
                        >
                          Confirm via Alexa+ ✓
                        </Link>
                        <Link
                          href="/login?returnUrl=/talk"
                          className="rounded-lg border border-emerald-700/20 px-3 py-1.5 text-xs font-medium text-emerald-900 hover:bg-emerald-100 transition-colors"
                        >
                          Keep Original
                        </Link>
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* =====================================================================
          5. FINAL CALL TO ACTION & FOOTER
          ===================================================================== */}
      <section className="relative border-t border-luma-primary/10 bg-gradient-to-b from-luma-bg to-luma-bg-secondary py-20 sm:py-28">
        <div className="mx-auto max-w-5xl px-5 text-center sm:px-8">
          <div className="mb-4 inline-flex items-center gap-2 rounded-full border border-luma-primary/15 bg-white/70 px-4 py-1.5 text-xs font-medium text-luma-text-secondary backdrop-blur-sm">
            <span className="text-luma-accent" aria-hidden="true">
              ✦
            </span>
            <span>A little space to begin</span>
          </div>

          <h2 className="font-serif text-4xl leading-tight tracking-tight text-luma-primary sm:text-6xl">
            See the life you want.{" "}
            <span className="block italic text-luma-accent">Build it.</span>
          </h2>

          <p className="mx-auto mt-6 max-w-xl text-base leading-relaxed text-luma-text-secondary sm:text-lg">
            No pressure, no rigid checklists. Just a calm, inspiring companion
            that helps you shape what matters and adapt when life happens.
          </p>

          <div className="mt-10 flex flex-wrap items-center justify-center gap-4">
            <Link
              href="/discover"
              className="inline-flex items-center gap-2.5 rounded-full bg-luma-primary px-8 py-4 text-base font-semibold text-luma-bg shadow-xl shadow-luma-primary/20 transition-all duration-300 hover:-translate-y-0.5 hover:shadow-2xl sm:text-lg"
            >
              <span>Start your Life Canvas</span>
              <span aria-hidden="true">→</span>
            </Link>

            <Link
              href="/login"
              className="inline-flex items-center gap-2 rounded-full border border-luma-primary/20 bg-white/80 px-7 py-4 text-base font-medium text-luma-primary shadow-sm backdrop-blur-md transition-all duration-300 hover:-translate-y-0.5 hover:bg-white sm:text-lg"
            >
              <span>Try Demo Session</span>
              <span className="text-luma-accent">✦</span>
            </Link>
          </div>
        </div>

        {/* Footer */}
        <footer className="mt-20 border-t border-luma-primary/10 pt-8 pb-10">
          <div className="mx-auto flex max-w-7xl flex-col items-center justify-between gap-4 px-5 text-xs text-luma-text-secondary sm:flex-row sm:px-8 lg:px-12">
            <div className="flex items-center gap-2">
              <span className="font-serif font-bold text-luma-primary text-sm tracking-wider">
                LUMA
              </span>
              <span>·</span>
              <span>Amazon Developer Hackathon 2026 Submission</span>
            </div>

            <div className="flex flex-wrap items-center justify-center gap-6">
              <Link href="/discover" className="hover:text-luma-primary transition-colors">
                Life Canvas
              </Link>
              <Link href="/login?returnUrl=/daily-path" className="hover:text-luma-primary transition-colors">
                Daily Path
              </Link>
              <Link href="/login?returnUrl=/goals" className="hover:text-luma-primary transition-colors">
                Goals &amp; Gaps
              </Link>
              <Link href="/login?returnUrl=/journey" className="hover:text-luma-primary transition-colors">
                My Journey
              </Link>
              <Link href="/login?returnUrl=/talk" className="hover:text-luma-primary transition-colors">
                Alexa+ Simulator
              </Link>
            </div>
          </div>
        </footer>
      </section>
    </div>
  );
}
