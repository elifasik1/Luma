"use client";

import React from "react";
import Link from "next/link";
import type { DreamDetailsResponse } from "@/lib/types";
import { useLumaTheme } from "@/theme/ThemeProvider";
import { normalizeCoreAnchorKeys } from "@/theme/normalize";
import {
  coreAnchors,
  illustrationLabels,
  imageryLabels,
  type IllustrationKey,
} from "@/theme/tokens";
import { Button } from "@/components/ui/Button";

interface WorldViewProps {
  dream: DreamDetailsResponse;
}

const archetypeDetails: Record<
  IllustrationKey,
  { symbol: string; scene: string; companion: string }
> = {
  dreamer: {
    symbol: "✦",
    scene: "A quiet horizon opens around a small guiding light.",
    companion: "dreamer",
  },
  urban_dreamer: {
    symbol: "▥",
    scene: "City windows glow softly beyond your own horizon.",
    companion: "urban dreamer",
  },
  creative_dreamer: {
    symbol: "✿",
    scene: "Ideas gather like color and begin to bloom.",
    companion: "creative dreamer",
  },
  nature_dreamer: {
    symbol: "❋",
    scene: "New roots take hold beneath a wide, open sky.",
    companion: "nature dreamer",
  },
  professional_dreamer: {
    symbol: "▱",
    scene: "A bright workspace waits for what you will make next.",
    companion: "professional dreamer",
  },
};

export const WorldView: React.FC<WorldViewProps> = ({ dream }) => {
  const profile = dream.futureProfile;
  const themeConfig = profile?.themeConfig;

  const {
    palette: normalizedPalette,
    atmosphere: normalizedAtmosphere,
    illustration: normalizedIllustration,
    motion: normalizedMotion,
    imagery: normalizedImagery,
  } = useLumaTheme();
  const selectedCoreAnchors = normalizeCoreAnchorKeys(
    themeConfig?.coreAnchorKeys || []
  );
  const archetype = archetypeDetails[normalizedIllustration];

  const environment =
    (profile?.environment || "home").replace(/_/g, " ");
  const values = profile?.values || [];
  const lifestyle = profile?.lifestyle || [];
  const aspirations = profile?.aspirations || [];

  return (
      <main className="your-world mx-auto w-full max-w-6xl px-4 py-8 sm:px-8 sm:py-12 animate-luma-fade-in">
        <nav
          className="mb-8 flex items-center justify-between border-b border-luma-primary/10 pb-4"
          aria-label="Your world navigation"
        >
          <Link
            href="/discover"
            className="inline-flex items-center gap-2 text-sm font-medium text-luma-primary transition-opacity hover:opacity-70"
          >
            <span aria-hidden="true">←</span>
            <span>Imagine another future</span>
          </Link>
          <span className="font-serif text-xs tracking-[0.2em] text-luma-primary/55">
            LUMA · YOUR WORLD
          </span>
          <Link
            href={`/talk?id=${encodeURIComponent(dream.id)}`}
            className="inline-flex items-center gap-2 text-sm font-medium text-luma-primary transition-opacity hover:opacity-70"
          >
            Talk to LUMA <span aria-hidden="true">✦</span>
          </Link>
        </nav>

        <header className="mx-auto mb-9 max-w-3xl text-center sm:mb-12">
          <p className="mb-3 text-xs font-semibold uppercase tracking-[0.24em] text-luma-text-secondary">
            A glimpse of what could be
          </p>
          <h1 className="font-serif text-4xl leading-tight tracking-tight text-luma-primary sm:text-6xl">
            Your life, taking shape.
          </h1>
          <p className="mt-4 font-serif text-xl italic text-luma-text-secondary sm:text-2xl">
            “{dream.title}”
          </p>
        </header>

        <section
          className={`world-scene world-scene--${normalizedAtmosphere} relative mb-10 min-h-[340px] overflow-hidden rounded-[2rem] border border-luma-primary/10 shadow-[var(--luma-shadow)] sm:min-h-[430px]`}
          aria-label={`${illustrationLabels[normalizedIllustration]} future scene. ${archetype.scene}`}
        >
          <div className="world-scene__glow" aria-hidden="true" />
          <div className="world-scene__sun" aria-hidden="true" />
          <div className="world-scene__horizon" aria-hidden="true" />
          {selectedCoreAnchors.length > 0 && (
            <div className="world-scene__anchor-motifs" aria-hidden="true">
              {selectedCoreAnchors.slice(0, 2).map((anchorKey) => (
                <span
                  key={anchorKey}
                  className={`world-scene__anchor-motif world-scene__anchor-motif--${anchorKey}`}
                >
                  {coreAnchors[anchorKey].motif}
                </span>
              ))}
            </div>
          )}
          <div className="world-scene__landscape" aria-hidden="true">
            <span className="world-scene__hill world-scene__hill--back" />
            <span className="world-scene__hill world-scene__hill--front" />
          </div>

          <div className="world-scene__architecture" aria-hidden="true">
            {normalizedIllustration === "urban_dreamer" && (
              <div className="world-scene__city">
                <i /><i /><i /><i /><i />
              </div>
            )}
            {normalizedIllustration === "nature_dreamer" && (
              <div className="world-scene__tree">
                <i /><i /><i />
              </div>
            )}
            {normalizedIllustration === "creative_dreamer" && (
              <div className="world-scene__creative-orbit">
                <i /><i /><i />
              </div>
            )}
            {normalizedIllustration === "professional_dreamer" && (
              <div className="world-scene__window">
                <i /><i /><i /><i />
              </div>
            )}
          </div>

          <div className="world-scene__figure" aria-hidden="true">
            <span className="world-scene__figure-head" />
            <span className="world-scene__figure-body" />
            <span className="world-scene__figure-arm" />
            <span className="world-scene__figure-light">{archetype.symbol}</span>
          </div>

          <div className="world-scene__caption">
            <span className="world-scene__eyebrow">
              YOUR {environment.toUpperCase()} FUTURE
            </span>
            <p className="font-serif text-xl leading-snug text-luma-primary sm:text-2xl">
              {profile?.summary ||
                "A possibility shaped by what matters most to you."}
            </p>
            <div className="mt-4 flex flex-wrap items-center gap-2">
              <span className="rounded-full border border-luma-primary/10 bg-white/50 px-3 py-1 text-xs font-medium text-luma-primary">
                {illustrationLabels[normalizedIllustration]}
              </span>
              {normalizedImagery.slice(0, 3).map((imagery) => (
                <span
                  key={imagery}
                  className="rounded-full border border-luma-primary/10 bg-white/50 px-3 py-1 text-xs text-luma-primary"
                >
                  {imageryLabels[imagery].emoji} {imageryLabels[imagery].label}
                </span>
              ))}
              {selectedCoreAnchors.slice(0, 2).map((anchorKey) => (
                <span
                  key={anchorKey}
                  className="rounded-full border border-luma-accent/25 bg-luma-surface/45 px-3 py-1 text-xs text-luma-primary"
                >
                  {coreAnchors[anchorKey].icon} {coreAnchors[anchorKey].label}
                </span>
              ))}
            </div>
          </div>
          <p className="world-scene__note">{archetype.scene}</p>
        </section>

        <section
          className="mx-auto grid max-w-5xl gap-8 rounded-[1.75rem] border border-luma-primary/10 bg-white/35 p-5 sm:grid-cols-[1fr_auto] sm:gap-12 sm:p-8"
          aria-label="The shape of your future"
        >
          <div>
            <p className="mb-2 text-xs font-semibold uppercase tracking-[0.2em] text-luma-text-secondary">
              The person you’re becoming
            </p>
            <h2 className="font-serif text-2xl text-luma-primary sm:text-3xl">
              {profile?.identity || "Dreamer"}
            </h2>
            <p className="mt-2 text-sm text-luma-text-secondary">
              {profile?.mood || "Hopeful"} at your own{" "}
              {profile?.pace || "steady"} pace, in a life rooted in{" "}
              {values.length > 0
                ? values.map((value) => value.replace(/_/g, " ")).join(" and ")
                : "what matters to you"}
              .
            </p>

            <div className="mt-6 space-y-4">
              <DetailGroup label="A life that feels like">
                {lifestyle}
              </DetailGroup>
              <DetailGroup label="What you’re reaching toward">
                {aspirations}
              </DetailGroup>
            </div>
          </div>

          <aside className="flex min-w-44 flex-row gap-6 border-t border-luma-primary/10 pt-5 sm:flex-col sm:border-l sm:border-t-0 sm:pl-7 sm:pt-0">
            <div>
              <p className="text-xs uppercase tracking-[0.16em] text-luma-text-secondary">
                Color story
              </p>
              <div className="mt-2 flex items-center gap-2">
                <span className="world-theme-swatch" aria-hidden="true" />
                <span className="font-serif text-base text-luma-primary">
                  {normalizedPalette.replace(/_/g, " ")}
                </span>
              </div>
            </div>
            <div>
              <p className="text-xs uppercase tracking-[0.16em] text-luma-text-secondary">
                Atmosphere
              </p>
              <p className="mt-2 font-serif text-base capitalize text-luma-primary">
                {normalizedAtmosphere} · {normalizedMotion}
              </p>
            </div>
          </aside>
        </section>

        {/* Possible Future Scenario Experience Callout */}
        <section
          className="mx-auto mt-8 max-w-5xl rounded-[1.75rem] border border-luma-primary/15 bg-gradient-to-r from-white/60 via-white/40 to-white/60 p-6 sm:p-8 shadow-[var(--luma-shadow)] backdrop-blur-sm transition-all hover:bg-white/70"
          aria-label="Enter your possible future"
        >
          <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-6">
            <div className="max-w-xl">
              <div className="mb-2 inline-flex items-center gap-2">
                <span className="text-xs text-luma-accent" aria-hidden="true">✦</span>
                <span className="text-xs font-semibold uppercase tracking-[0.2em] text-luma-text-secondary">
                  Possible Future Scenario
                </span>
              </div>
              <h3 className="font-serif text-2xl sm:text-3xl text-luma-primary">
                Step into a day in this possible future.
              </h3>
              <p className="mt-2 text-sm text-luma-text-secondary leading-relaxed">
                Experience how your morning, daytime focus, and evening rhythm take shape in this possible life.
              </p>
            </div>
            <div className="flex-shrink-0">
              <Link href={`/future-scenario?id=${dream.id}`}>
                <Button size="lg" className="w-full sm:w-auto shadow-md shadow-luma-peach/20">
                  <span>Enter This Day ✦</span>
                </Button>
              </Link>
            </div>
          </div>
        </section>

        <footer className="py-10 text-center">
          <Link href="/discover">
            <Button size="lg" variant="secondary">
              Imagine another path <span aria-hidden="true">✦</span>
            </Button>
          </Link>
          <p className="mt-4 text-xs text-luma-text-secondary">
            A possible future, not a prediction. Keep shaping it, one step at a time.
          </p>
        </footer>
      </main>
  );
};

function DetailGroup({
  label,
  children,
}: {
  label: string;
  children: string[];
}) {
  return (
    <div>
      <p className="mb-2 text-xs uppercase tracking-[0.16em] text-luma-text-secondary">
        {label}
      </p>
      {children.length > 0 ? (
        <ul className="flex flex-wrap gap-x-4 gap-y-1">
          {children.map((item) => (
            <li
              key={item}
              className="inline-flex items-center gap-2 text-sm font-medium capitalize text-luma-primary"
            >
              <span className="h-1.5 w-1.5 rounded-full bg-luma-accent" />
              {item.replace(/_/g, " ")}
            </li>
          ))}
        </ul>
      ) : (
        <p className="text-sm italic text-luma-text-secondary">
          Still taking shape
        </p>
      )}
    </div>
  );
}
