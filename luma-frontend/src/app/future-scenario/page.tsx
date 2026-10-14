"use client";

import { useEffect, useState, Suspense } from "react";
import { useSearchParams } from "next/navigation";
import Link from "next/link";
import type { DreamDetailsResponse, FutureScenarioResponse } from "@/lib/types";
import { getDreamDetails, getFutureScenario, getLatestDream } from "@/lib/api";
import { FutureScenarioView } from "@/components/world/FutureScenarioView";
import { DreamThemeProvider } from "@/components/world/DreamThemeProvider";
import { Button } from "@/components/ui/Button";

function FutureScenarioContent() {
  const searchParams = useSearchParams();
  const dreamId = searchParams.get("id");

  const [dream, setDream] = useState<DreamDetailsResponse | null>(null);
  const [scenario, setScenario] = useState<FutureScenarioResponse | null>(null);
  const [isLoading, setIsLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let isMounted = true;

    async function loadScenario() {
      try {
        let currentDreamId = dreamId;
        if (!currentDreamId) {
          const latest = await getLatestDream();
          if (latest?.id) {
            currentDreamId = latest.id;
          }
        }

        if (!currentDreamId) {
          if (isMounted) {
            setError("No dream found. Please complete the discovery flow first.");
            setIsLoading(false);
          }
          return;
        }

        const dreamData = await getDreamDetails(currentDreamId);
        if (!isMounted) return;
        setDream(dreamData);

        const scenarioData = await getFutureScenario(currentDreamId);
        if (isMounted) setScenario(scenarioData);
      } catch (err: unknown) {
        if (isMounted) {
          setError(
            err instanceof Error
              ? err.message
              : "Could not generate your future scenario."
          );
        }
      } finally {
        if (isMounted) setIsLoading(false);
      }
    }

    void loadScenario();

    return () => {
      isMounted = false;
    };
  }, [dreamId]);

  if (!dream && isLoading) {
    return (
      <div className="min-h-[70vh] flex flex-col items-center justify-center text-center px-4">
        <div className="relative mb-8">
          <div className="w-24 h-24 sm:w-32 sm:h-32 rounded-full bg-gradient-to-tr from-luma-peach via-luma-lilac to-luma-sage opacity-70 blur-xl animate-luma-glow pointer-events-none" />
          <div className="absolute inset-0 flex items-center justify-center">
            <span className="text-3xl sm:text-4xl animate-pulse">✦</span>
          </div>
        </div>
        <h2 className="font-serif text-3xl text-luma-primary tracking-tight mb-2">
          Stepping into a possible future...
        </h2>
        <p className="text-sm text-luma-text-secondary max-w-sm">
          Shaping a grounded day in your life based on what you shared.
        </p>
      </div>
    );
  }

  if (!dream) {
    return (
      <div className="min-h-[70vh] flex flex-col items-center justify-center text-center px-4 max-w-md mx-auto">
        <div className="w-16 h-16 rounded-full bg-luma-surface/40 flex items-center justify-center mb-6 text-2xl text-luma-primary">
          ✨
        </div>
        <h2 className="font-serif text-2xl sm:text-3xl text-luma-primary tracking-tight mb-3">
          Scenario Not Available
        </h2>
        <p className="text-sm text-luma-text-secondary mb-8 leading-relaxed">
          {error || "We couldn't generate this possible future."}
        </p>
        <Link href={dreamId ? `/your-world?id=${dreamId}` : "/discover"} passHref>
          <Button size="md">
            <span>{dreamId ? "Back to Your World" : "Start Imagining"}</span>
          </Button>
        </Link>
      </div>
    );
  }

  return (
    <DreamThemeProvider dream={dream}>
      {isLoading ? (
        <div className="min-h-[70vh] flex flex-col items-center justify-center text-center px-4">
          <div className="relative mb-8">
            <div className="w-24 h-24 sm:w-32 sm:h-32 rounded-full bg-gradient-to-tr from-luma-accent via-luma-surface to-luma-muted opacity-70 blur-xl animate-luma-glow pointer-events-none" />
            <div className="absolute inset-0 flex items-center justify-center">
              <span className="text-3xl sm:text-4xl animate-pulse">✦</span>
            </div>
          </div>
          <h2 className="font-serif text-3xl text-luma-primary tracking-tight mb-2">
            Stepping into a possible future...
          </h2>
          <p className="text-sm text-luma-text-secondary max-w-sm">
            Shaping a grounded day in your life based on what you shared.
          </p>
        </div>
      ) : error || !scenario ? (
        <div className="min-h-[70vh] flex flex-col items-center justify-center text-center px-4 max-w-md mx-auto">
          <div className="w-16 h-16 rounded-full bg-luma-surface/40 flex items-center justify-center mb-6 text-2xl text-luma-primary">
            ✨
          </div>
          <h2 className="font-serif text-2xl sm:text-3xl text-luma-primary tracking-tight mb-3">
            Scenario Not Available
          </h2>
          <p className="text-sm text-luma-text-secondary mb-8 leading-relaxed">
            {error || "We couldn't generate this possible future."}
          </p>
          <Link href={`/your-world?id=${dreamId}`} passHref>
            <Button size="md">
              <span>Back to Your World</span>
            </Button>
          </Link>
        </div>
      ) : (
        <FutureScenarioView dream={dream} scenario={scenario} />
      )}
    </DreamThemeProvider>
  );
}

export default function FutureScenarioPage() {
  return (
    <main className="flex-1 flex flex-col justify-center">
      <Suspense
        fallback={
          <div className="min-h-[70vh] flex flex-col items-center justify-center text-center px-4">
            <div className="w-16 h-16 rounded-full bg-luma-surface/40 animate-pulse mb-4" />
            <p className="text-sm text-luma-text-secondary">Loading scenario...</p>
          </div>
        }
      >
        <FutureScenarioContent />
      </Suspense>
    </main>
  );
}
