"use client";

import { useEffect, useState, Suspense } from "react";
import { useSearchParams } from "next/navigation";
import Link from "next/link";
import type { DreamDetailsResponse } from "@/lib/types";
import {
  getCurrentUser,
  getDailyPath,
  getDreamDetails,
  getDreamGoals,
  getReflections,
  getLatestDream,
} from "@/lib/api";
import type { AuthenticatedUser, DailyPathResponse, GoalResponse, ReflectionResponse } from "@/lib/types";
import { WorldView } from "@/components/world/WorldView";
import { DreamThemeProvider } from "@/components/world/DreamThemeProvider";
import { Button } from "@/components/ui/Button";

function YourWorldContent() {
  const searchParams = useSearchParams();
  const dreamId = searchParams.get("id");

  const [dream, setDream] = useState<DreamDetailsResponse | null>(null);
  const [user, setUser] = useState<AuthenticatedUser | null>(null);
  const [dailyPath, setDailyPath] = useState<DailyPathResponse | null>(null);
  const [goals, setGoals] = useState<GoalResponse[]>([]);
  const [reflections, setReflections] = useState<ReflectionResponse[]>([]);
  const [dataWarnings, setDataWarnings] = useState<string[]>([]);
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
        }

        const results = await Promise.allSettled([
          getCurrentUser(),
          getDailyPath(targetId),
          getDreamGoals(targetId),
          getReflections(targetId),
        ]);

        if (isMounted) {
          const warnings: string[] = [];
          const [userResult, dailyResult, goalsResult, reflectionsResult] = results;
          if (userResult.status === "fulfilled") setUser(userResult.value);
          else warnings.push("Your account details are unavailable right now.");
          if (dailyResult.status === "fulfilled") setDailyPath(dailyResult.value);
          else warnings.push("Today’s path is not available yet.");
          if (goalsResult.status === "fulfilled") setGoals(goalsResult.value);
          else warnings.push("Goals are not available yet.");
          if (reflectionsResult.status === "fulfilled") setReflections(reflectionsResult.value);
          else warnings.push("Reflections are not available yet.");
          setDataWarnings(warnings);
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
      <WorldView
        dream={dream}
        user={user}
        dailyPath={dailyPath}
        goals={goals}
        reflections={reflections}
        dataWarnings={dataWarnings}
      />
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
