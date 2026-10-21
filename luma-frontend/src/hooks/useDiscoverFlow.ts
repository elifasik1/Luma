"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import type { DiscoveryAnswers, VisualWorldKey } from "@/lib/types";
import { composeDreamDescription, createDream } from "@/lib/api";
import type { LumaTheme } from "@/theme/ThemeProvider";
import { dailyRhythms, type CoreAnchorKey, type DailyRhythmKey, type MotionKey } from "@/theme/tokens";

/** Parse DD/MM/YYYY into parts or null */
function parseDDMMYYYY(raw: string): { day: number; month: number; year: number } | null {
  const match = raw.match(/^(\d{1,2})\/(\d{1,2})\/(\d{4})$/);
  if (!match) return null;
  const day = Number(match[1]);
  const month = Number(match[2]);
  const year = Number(match[3]);
  if (month < 1 || month > 12 || day < 1 || year < 1) return null;
  const maxDay = new Date(year, month, 0).getDate();
  if (day > maxDay) return null;
  const today = new Date();
  today.setHours(0, 0, 0, 0);
  const input = new Date(year, month - 1, day);
  if (input < today) return null;
  return { day, month, year };
}

/** Return today as DD/MM/YYYY */
function formatTodayDDMMYYYY(): string {
  const now = new Date();
  return `${String(now.getDate()).padStart(2, "0")}/${String(now.getMonth() + 1).padStart(2, "0")}/${now.getFullYear()}`;
}

export type DiscoverStep = 1 | 2 | 3 | 4 | 5;

// ---------------------------------------------------------------------------
// Visual World definitions — each lifestyle option has its own world
// ---------------------------------------------------------------------------
const lifestyleWorlds: Record<string, { key: VisualWorldKey; theme: LumaTheme }> = {
  // 1. Calm & Free — soft warm cream, spacious, peaceful, breathing space
  "Calm & Free": { key: "calm_free", theme: {
    palette: "warm_dream",
    atmosphere: "calm",
    motion: "gentle",
    illustration: "dreamer",
    imagery: ["home", "ocean"],
  } },

  // 2. Big City — city at night, deep plum/indigo, focused/dynamic
  "Big City": { key: "big_city", theme: {
    palette: "city_night",
    atmosphere: "focused",
    motion: "soft",
    illustration: "urban_dreamer",
    imagery: ["city", "workspace", "technology"],
  } },

  // 3. Nature — forest mist, organic shapes, calm
  Nature: { key: "nature", theme: {
    palette: "forest_mist",
    atmosphere: "calm",
    motion: "gentle",
    illustration: "nature_dreamer",
    imagery: ["nature", "home"],
  } },

  // 4. Creative — lavender/lilac, artistic warmth, floating
  Creative: { key: "creative", theme: {
    palette: "lavender_milk",
    atmosphere: "creative",
    motion: "floating",
    illustration: "creative_dreamer",
    imagery: ["books", "home"],
  } },

  // 5. Social — warm coral/rose, human, conversational, welcoming
  Social: { key: "social", theme: {
    palette: "social_sunrise",
    atmosphere: "cozy",
    motion: "floating",
    illustration: "dreamer",
    imagery: ["bakery", "home", "travel"],
  } },

  // 6. Ambitious — confident gold/charcoal, professional, energetic, sunrise
  Ambitious: { key: "ambitious", theme: {
    palette: "amber_summit",
    atmosphere: "energetic",
    motion: "soft",
    illustration: "professional_dreamer",
    imagery: ["workspace", "technology"],
  } },

  // 7. Learning — academic lavender + focused structured feel
  Learning: { key: "learning", theme: {
    palette: "lavender_milk",
    atmosphere: "focused",
    motion: "soft",
    illustration: "dreamer",
    imagery: ["books", "campus", "technology"],
  } },

  // 8. Exploring — open sky, travel, movement, airy blue
  Exploring: { key: "exploring", theme: {
    palette: "ocean_light",
    atmosphere: "airy",
    motion: "floating",
    illustration: "dreamer",
    imagery: ["travel", "ocean"],
  } },

  // 9. Independent — warm but minimal, personal space, quiet confidence
  Independent: { key: "independent", theme: {
    palette: "sunset_cream",
    atmosphere: "cozy",
    motion: "gentle",
    illustration: "dreamer",
    imagery: ["home", "books"],
  } },

  // 10. Building Something — workshop/blueprint, maker space, structured but warm
  "Building Something": { key: "building_something", theme: {
    palette: "maker_workshop",
    atmosphere: "focused",
    motion: "soft",
    illustration: "professional_dreamer",
    imagery: ["workspace", "technology", "books"],
  } },

  // 11. Ocean — open horizons, sunlight, water, freedom
  Ocean: { key: "ocean", theme: {
    palette: "ocean_light",
    atmosphere: "calm",
    motion: "floating",
    illustration: "dreamer",
    imagery: ["ocean", "travel"],
  } },
};

// "Not sure yet" default — neutral/warm with possibility feel
const notSureYetWorld = {
  key: "not_sure" as const,
  theme: {
  palette: "warm_dream",
  atmosphere: "calm",
  motion: "floating",
  illustration: "dreamer",
  imagery: [],
  } satisfies LumaTheme,
};

export function getDiscoveryVisualWorldKey(answers: DiscoveryAnswers): VisualWorldKey {
  const selectedLifestyle = [...answers.lifestyle]
    .reverse()
    .find((value) => value !== "Not sure yet");
  return selectedLifestyle
    ? lifestyleWorlds[selectedLifestyle]?.key ?? notSureYetWorld.key
    : notSureYetWorld.key;
}

export function getDiscoveryPreviewTheme(answers: DiscoveryAnswers): LumaTheme {
  const selectedLifestyle = [...answers.lifestyle]
    .reverse()
    .find((value) => value !== "Not sure yet");

  if (!selectedLifestyle && answers.lifestyle.includes("Not sure yet")) {
    return notSureYetWorld.theme;
  }

  const preview = lifestyleWorlds[selectedLifestyle ?? ""]?.theme ?? {
    palette: "warm_dream",
    atmosphere: "airy",
    motion: "soft",
    illustration: "dreamer",
    imagery: [],
  };

  const selectedRhythms = answers.dayFeel;
  if (selectedRhythms.length > 0) {
    const dominantRhythm = dailyRhythms[selectedRhythms[0]];
    const motionCalmness: Record<MotionKey, number> = {
      still: 0,
      gentle: 1,
      soft: 2,
      floating: 3,
    };
    const calmestMotion = selectedRhythms
      .map((key) => dailyRhythms[key].motion)
      .sort((left, right) => motionCalmness[left] - motionCalmness[right])[0];

    return {
      ...preview,
      atmosphere: dominantRhythm.atmosphere,
      motion: calmestMotion,
    };
  }

  return preview;
}

export function useDiscoverFlow() {
  const router = useRouter();

  const [currentStep, setCurrentStep] = useState<DiscoverStep>(1);
  const [answers, setAnswers] = useState<DiscoveryAnswers>({
    lifestyle: [],
    dayFeel: [],
    values: [],
    aboutYou: "",
    targetDate: formatTodayDDMMYYYY(),
  });

  const [isSubmitting, setIsSubmitting] = useState(false);
  const [submissionPhase, setSubmissionPhase] = useState<
    "idle" | "analyzing" | "shaping" | "ready"
  >("idle");
  const [error, setError] = useState<string | null>(null);

  const totalSteps = 5;

  const updateLifestyle = (lifestyle: string[]) => {
    setAnswers((prev) => ({ ...prev, lifestyle }));
  };

  const updateDayFeel = (dayFeel: DailyRhythmKey[]) => {
    setAnswers((prev) => ({ ...prev, dayFeel }));
  };

  const updateValues = (values: CoreAnchorKey[]) => {
    setAnswers((prev) => ({ ...prev, values }));
  };

  const updateAboutYou = (aboutYou: string) => {
    setAnswers((prev) => ({ ...prev, aboutYou }));
  };

  const updateTargetDate = (targetDate: string) => {
    setAnswers((prev) => ({ ...prev, targetDate }));
  };

  const nextStep = () => {
    if (currentStep < totalSteps) {
      setCurrentStep((prev) => (prev + 1) as DiscoverStep);
    }
  };

  const prevStep = () => {
    if (currentStep > 1) {
      setCurrentStep((prev) => (prev - 1) as DiscoverStep);
    }
  };

  const canProceed = () => {
    switch (currentStep) {
      case 1:
        return answers.lifestyle.length > 0;
      case 2:
        return answers.dayFeel.length > 0;
      case 3:
        return answers.values.length > 0;
      case 4:
        return true; // About you can be optional or short
      case 5:
        return parseDDMMYYYY(answers.targetDate) !== null;
      default:
        return false;
    }
  };

  const submitDiscovery = async () => {
    setIsSubmitting(true);
    setError(null);
    setSubmissionPhase("analyzing");

    try {
      const { title, description } = composeDreamDescription(answers);

      // Phase 1: Analyzing
      await new Promise((res) => setTimeout(res, 600));
      setSubmissionPhase("shaping");

      const parsedDate = parseDDMMYYYY(answers.targetDate);
      const targetYear = parsedDate ? parsedDate.year : new Date().getFullYear();
      const targetDateISO = parsedDate
        ? `${parsedDate.year}-${String(parsedDate.month).padStart(2, "0")}-${String(parsedDate.day).padStart(2, "0")}`
        : undefined;

      // API call to Next.js proxy
      const result = await createDream({
        title,
        description,
        visualWorldKey: getDiscoveryVisualWorldKey(answers),
        dailyRhythmKeys: answers.dayFeel,
        coreAnchorKeys: answers.values,
        targetYear,
        targetDate: targetDateISO,
      });

      // Phase 2: Shaping
      await new Promise((res) => setTimeout(res, 500));
      setSubmissionPhase("ready");

      // Navigate to Your World with dream ID
      router.push(`/your-world?id=${encodeURIComponent(result.dreamId)}`);
    } catch (err: unknown) {
      console.error("Discovery submission error:", err);
      setError(
        err instanceof Error
          ? err.message
          : "We encountered an issue creating your world. Please try again."
      );
      setIsSubmitting(false);
      setSubmissionPhase("idle");
    }
  };

  return {
    currentStep,
    totalSteps,
    answers,
    isSubmitting,
    submissionPhase,
    error,
    updateLifestyle,
    updateDayFeel,
    updateValues,
    updateAboutYou,
    updateTargetDate,
    nextStep,
    prevStep,
    canProceed,
    submitDiscovery,
  };
}
