"use client";

import { Suspense, useEffect, useState } from "react";
import { useSearchParams } from "next/navigation";
import { TalkToLumaView } from "@/components/world/TalkToLumaView";
import { getLatestDream } from "@/lib/api";

function TalkPageContent() {
  const paramDreamId = useSearchParams().get("id");
  const [dreamId, setDreamId] = useState<string | null>(paramDreamId);
  const [loading, setLoading] = useState(!paramDreamId);

  useEffect(() => {
    if (paramDreamId || dreamId) {
      return;
    }

    let active = true;
    getLatestDream()
      .then((latest) => {
        if (active) {
          if (latest?.id) setDreamId(latest.id);
          setLoading(false);
        }
      })
      .catch(() => {
        if (active) setLoading(false);
      });

    return () => {
      active = false;
    };
  }, [paramDreamId, dreamId]);

  if (loading) {
    return (
      <main className="mx-auto flex min-h-[70vh] w-full max-w-3xl items-center justify-center px-5 text-center">
        <span className="animate-pulse text-2xl text-luma-accent">✦</span>
      </main>
    );
  }

  if (!dreamId) {
    return (
      <main className="mx-auto flex min-h-[70vh] w-full max-w-xl flex-col items-center justify-center px-5 text-center">
        <div className="flex h-14 w-14 items-center justify-center rounded-2xl bg-luma-surface/60 text-2xl mb-4">
          🎙️
        </div>
        <h1 className="font-serif text-3xl text-luma-primary sm:text-4xl">
          Connect to Alexa+ Companion
        </h1>
        <p className="mt-3 text-sm text-luma-text-secondary leading-relaxed max-w-md">
          To speak with LUMA, start with an imagined future or jump straight in with a pre-configured demo world.
        </p>
        <div className="mt-8 flex flex-wrap items-center justify-center gap-3">
          <a
            href="/login?returnUrl=/talk"
            className="rounded-full bg-luma-primary px-6 py-3 text-sm font-semibold text-luma-bg shadow-md hover:bg-opacity-95"
          >
            Launch with Demo World ✦
          </a>
          <a
            href="/discover"
            className="rounded-full border border-luma-primary/20 bg-white/70 px-6 py-3 text-sm font-medium text-luma-primary hover:bg-white"
          >
            Start Imagining →
          </a>
        </div>
      </main>
    );
  }

  return <TalkToLumaView dreamId={dreamId} />;
}

export default function TalkPage() {
  return (
    <Suspense fallback={<main className="min-h-[70vh]" />}>
      <TalkPageContent />
    </Suspense>
  );
}
