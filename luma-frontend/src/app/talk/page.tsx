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
  }, [paramDreamId]);

  if (loading) {
    return (
      <main className="mx-auto flex min-h-[70vh] w-full max-w-3xl items-center justify-center px-5 text-center">
        <span className="animate-pulse text-2xl text-luma-accent">✦</span>
      </main>
    );
  }

  if (!dreamId) {
    return (
      <main className="mx-auto flex min-h-[70vh] w-full max-w-3xl items-center justify-center px-5 text-center">
        <p className="font-serif text-2xl text-luma-primary">Open your world to talk with LUMA.</p>
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
