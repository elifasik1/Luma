"use client";

import { useEffect, useState, Suspense } from "react";
import { useSearchParams } from "next/navigation";
import Link from "next/link";
import type { DreamDetailsResponse } from "@/lib/types";
import { getDreamDetails, getLatestDream } from "@/lib/api";
import { WorldView } from "@/components/world/WorldView";
import { DreamThemeProvider } from "@/components/world/DreamThemeProvider";
import { Button } from "@/components/ui/Button";

function YourWorldContent() {
  const searchParams = useSearchParams();
  const dreamId = searchParams.get("id");

  const [dream, setDream] = useState<DreamDetailsResponse | null>(null);
  const [isLoading, setIsLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let isMounted = true;

    async function loadDream() {
      try {
        let targetId = dreamId;
        if (!targetId) {
          const latest = await getLatestDream();
          if (latest?.id) {
            targetId = latest.id;
          }
        }

        if (!targetId) {
          if (isMounted) {
            setError("No dream found. Please complete the discovery flow first.");
            setIsLoading(false);
          }
          return;
        }

        const data = await getDreamDetails(targetId);
        if (isMounted) {
          setDream(data);
          setIsLoading(false);
        }
      } catch (err: unknown) {
        if (isMounted) {
          setError(
            err instanceof Error
              ? err.message
              : "Could not retrieve your world."
          );
          setIsLoading(false);
        }
      }
    }

    void loadDream();

    return () => {
      isMounted = false;
    };
  }, [dreamId]);

  if (isLoading) {
    return (
      <div className="min-h-[70vh] flex flex-col items-center justify-center text-center px-4">
        <div className="relative mb-8">
          <div className="w-24 h-24 sm:w-32 sm:h-32 rounded-full bg-gradient-to-tr from-luma-peach via-luma-lilac to-luma-sage opacity-70 blur-xl animate-luma-glow pointer-events-none" />
          <div className="absolute inset-0 flex items-center justify-center">
            <span className="text-3xl sm:text-4xl animate-pulse">✦</span>
          </div>
        </div>
        <h2 className="font-serif text-3xl text-luma-primary tracking-tight mb-2">
          Illuminating your world...
        </h2>
        <p className="text-sm text-luma-text-secondary">
          Loading your future profile and adaptive atmosphere.
        </p>
      </div>
    );
  }

  if (error || !dream) {
    return (
      <div className="min-h-[70vh] flex flex-col items-center justify-center text-center px-4 max-w-md mx-auto">
        <div className="w-16 h-16 rounded-full bg-luma-surface/40 flex items-center justify-center mb-6 text-2xl text-luma-primary">
          ✨
        </div>
        <h2 className="font-serif text-2xl sm:text-3xl text-luma-primary tracking-tight mb-3">
          World Not Found
        </h2>
        <p className="text-sm text-luma-text-secondary mb-8 leading-relaxed">
          {error || "We couldn't locate this future blueprint."}
        </p>
        <Link href="/discover" passHref>
          <Button size="md">
            <span>Start Imagining</span>
          </Button>
        </Link>
      </div>
    );
  }

  return (
    <DreamThemeProvider dream={dream}>
      <WorldView dream={dream} />
    </DreamThemeProvider>
  );
}

export default function YourWorldPage() {
  return (
    <main className="flex-1 flex flex-col justify-center">
      <Suspense
        fallback={
          <div className="min-h-[70vh] flex flex-col items-center justify-center text-center px-4">
            <div className="w-16 h-16 rounded-full bg-luma-surface/40 animate-pulse mb-4" />
            <p className="text-sm text-luma-text-secondary">Loading your world...</p>
          </div>
        }
      >
        <YourWorldContent />
      </Suspense>
    </main>
  );
}
