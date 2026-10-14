"use client";

import { useEffect, useState, Suspense } from "react";
import { useSearchParams } from "next/navigation";
import Link from "next/link";
import { getDreamDetails, getDreamGoals, getDailyPath, getLatestDream } from "@/lib/api";
import type { DreamDetailsResponse, GoalResponse, DailyPathResponse } from "@/lib/types";
import { GoalsView } from "@/components/world/GoalsView";
import { DreamThemeProvider } from "@/components/world/DreamThemeProvider";
import { Button } from "@/components/ui/Button";

function GoalsContent() {
  const searchParams = useSearchParams();
  const dreamId = searchParams.get("id");

  const [dream, setDream] = useState<DreamDetailsResponse | null>(null);
  const [goals, setGoals] = useState<GoalResponse[] | null>(null);
  const [dailyPath, setDailyPath] = useState<DailyPathResponse | null>(null);
  const [isLoading, setIsLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let isMounted = true;

    async function loadGoals() {
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

        const [goalsData, dailyPathData] = await Promise.all([
          getDreamGoals(currentDreamId),
          getDailyPath(currentDreamId).catch(() => null),
        ]);

        if (isMounted) {
          setGoals(goalsData);
          setDailyPath(dailyPathData);
        }
      } catch (err: unknown) {
        if (isMounted) {
          setError(
            err instanceof Error
              ? err.message
              : "Could not generate your paths forward."
          );
        }
      } finally {
        if (isMounted) setIsLoading(false);
      }
    }

    void loadGoals();

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
          Mapping your paths forward...
        </h2>
        <p className="text-sm text-luma-text-secondary max-w-sm">
          Bridging where you stand today with the future you imagined.
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
          Paths Not Available
        </h2>
        <p className="text-sm text-luma-text-secondary mb-8 leading-relaxed">
          {error || "We couldn't generate the paths toward this future."}
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
            Mapping your paths forward...
          </h2>
          <p className="text-sm text-luma-text-secondary max-w-sm">
            Bridging where you stand today with the future you imagined.
          </p>
        </div>
      ) : error || !goals || goals.length === 0 ? (
        <div className="min-h-[70vh] flex flex-col items-center justify-center text-center px-4 max-w-md mx-auto">
          <div className="w-16 h-16 rounded-full bg-luma-surface/40 flex items-center justify-center mb-6 text-2xl text-luma-primary">
            ✨
          </div>
          <h2 className="font-serif text-2xl sm:text-3xl text-luma-primary tracking-tight mb-3">
            Paths Not Available
          </h2>
          <p className="text-sm text-luma-text-secondary mb-8 leading-relaxed">
            {error || "We couldn't generate the paths toward this future."}
          </p>
          <Link href={`/your-world?id=${dreamId}`} passHref>
            <Button size="md">
              <span>Back to Your World</span>
            </Button>
          </Link>
        </div>
      ) : (
        <GoalsView dream={dream} goals={goals} dailyPath={dailyPath} />
      )}
    </DreamThemeProvider>
  );
}

export default function GoalsPage() {
  return (
    <main className="flex-1 flex flex-col justify-center">
      <Suspense
        fallback={
          <div className="min-h-[70vh] flex flex-col items-center justify-center text-center px-4">
            <div className="w-16 h-16 rounded-full bg-luma-surface/40 animate-pulse mb-4" />
            <p className="text-sm text-luma-text-secondary">Loading paths forward...</p>
          </div>
        }
      >
        <GoalsContent />
      </Suspense>
    </main>
  );
}
