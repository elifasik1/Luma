"use client";

import { useEffect, useState, Suspense } from "react";
import { useSearchParams } from "next/navigation";
import Link from "next/link";
import type { DreamDetailsResponse, DailyPathResponse } from "@/lib/types";
import { getDreamDetails, getDailyPath, getLatestDream } from "@/lib/api";
import { DailyPathView } from "@/components/world/DailyPathView";
import { DreamThemeProvider } from "@/components/world/DreamThemeProvider";
import { Button } from "@/components/ui/Button";

function DailyPathContent() {
  const searchParams = useSearchParams();
  const dreamId = searchParams.get("id");
  const dateParam = searchParams.get("date");

  const [dream, setDream] = useState<DreamDetailsResponse | null>(null);
  const [dailyPath, setDailyPath] = useState<DailyPathResponse | null>(null);
  const [isLoading, setIsLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let isMounted = true;

    async function loadDailyPath() {
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

        // Use date parameter if provided, otherwise default to target date or today
        const planDate = dateParam || dreamData.futureProfile?.targetDate || undefined;

        const dailyPathData = await getDailyPath(currentDreamId, false, planDate);
        if (isMounted) {
          setDailyPath(dailyPathData);
        }
      } catch (err: unknown) {
        if (isMounted) {
          setError(
            err instanceof Error
              ? err.message
              : "Could not generate your daily path."
          );
        }
      } finally {
        if (isMounted) setIsLoading(false);
      }
    }

    void loadDailyPath();

    return () => {
      isMounted = false;
    };
  }, [dreamId, dateParam]);

  const planDate = dateParam || dream?.futureProfile?.targetDate || undefined;

  if (!dream && isLoading) {
    return (
      <div className="min-h-[70vh] flex flex-col items-center justify-center text-center px-4">
        <div className="relative mb-8">
          <div className="w-24 h-24 sm:w-32 sm:h-32 rounded-full bg-gradient-to-tr from-luma-peach via-luma-lilac to-luma-sage opacity-70 blur-xl animate-luma-glow pointer-events-none" />
          <div className="absolute inset-0 flex items-center justify-center">
            <span className="text-3xl sm:text-4xl animate-pulse">☀️</span>
          </div>
        </div>
        <h2 className="font-serif text-3xl text-luma-primary tracking-tight mb-2">
          Planning your day...
        </h2>
        <p className="text-sm text-luma-text-secondary max-w-sm">
          Turning your goals into gentle, actionable steps for today.
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
          Daily Path Not Available
        </h2>
        <p className="text-sm text-luma-text-secondary mb-8 leading-relaxed">
          {error || "We couldn't generate your daily actions. Make sure goals have been created first."}
        </p>
        <Link href={dreamId ? `/goals?id=${dreamId}` : "/discover"} passHref>
          <Button size="md">
            <span>{dreamId ? "Go to Goals" : "Start Imagining"}</span>
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
              <span className="text-3xl sm:text-4xl animate-pulse">☀️</span>
            </div>
          </div>
          <h2 className="font-serif text-3xl text-luma-primary tracking-tight mb-2">
            Planning your day...
          </h2>
          <p className="text-sm text-luma-text-secondary max-w-sm">
            Turning your goals into gentle, actionable steps for today.
          </p>
        </div>
      ) : error || !dailyPath || dailyPath.actions.length === 0 ? (
        <div className="min-h-[70vh] flex flex-col items-center justify-center text-center px-4 max-w-md mx-auto">
          <div className="w-16 h-16 rounded-full bg-luma-surface/40 flex items-center justify-center mb-6 text-2xl text-luma-primary">
            ✨
          </div>
          <h2 className="font-serif text-2xl sm:text-3xl text-luma-primary tracking-tight mb-3">
            Daily Path Not Available
          </h2>
          <p className="text-sm text-luma-text-secondary mb-8 leading-relaxed">
            {error || "We couldn't generate your daily actions. Make sure goals have been created first."}
          </p>
          <Link href={`/goals?id=${dreamId}`} passHref>
            <Button size="md">
              <span>Go to Goals</span>
            </Button>
          </Link>
        </div>
      ) : (
        <DailyPathView dream={dream} dailyPath={dailyPath} planDate={planDate} />
      )}
    </DreamThemeProvider>
  );
}

export default function DailyPathPage() {
  return (
    <main className="flex-1 flex flex-col justify-center">
      <Suspense
        fallback={
          <div className="min-h-[70vh] flex flex-col items-center justify-center text-center px-4">
            <div className="w-16 h-16 rounded-full bg-luma-surface/40 animate-pulse mb-4" />
            <p className="text-sm text-luma-text-secondary">Loading today&apos;s path...</p>
          </div>
        }
      >
        <DailyPathContent />
      </Suspense>
    </main>
  );
}
