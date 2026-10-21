// =============================================================================
// LUMA — Frontend API Client
// Calls the Next.js /api/dreams proxy endpoints.
// =============================================================================

import type {
  CreateDreamRequest,
  DreamAnalysisResult,
  DreamDetailsResponse,
  FutureScenarioResponse,
  GoalResponse,
  DailyPathResponse,
  DailyActionResponse,
  CreateReflectionRequest,
  ReflectionResponse,
  DiscoveryAnswers,
  LumaConversationTurn,
  TalkToLumaResponse,
  AgentMessageResponse,
  DailyPathChangeProposal,
  JourneyCalendarMonthResponse,
  JourneyCalendarDayResponse,
  UserThemeResponse,
  UpdateThemePayload,
  AuthenticatedUser,
} from "./types";
import { coreAnchors, dailyRhythms } from "@/theme/tokens";

/**
 * Composes discovery answers into a coherent, natural-language description
 * for the backend POST /dreams endpoint.
 */
export function composeDreamDescription(answers: DiscoveryAnswers): {
  title: string;
  description: string;
} {
  const lifestyleText =
    answers.lifestyle.length > 0
      ? `I envision a ${answers.lifestyle.join(" and ").toLowerCase()} lifestyle.`
      : "I envision a meaningful and fulfilling lifestyle.";

  const dayFeelText =
    answers.dayFeel.length > 0
      ? `I want my days to feel focused on ${answers.dayFeel
          .map((key) => dailyRhythms[key].label.toLowerCase())
          .join(", ")}.`
      : "";

  const valuesText =
    answers.values.length > 0
      ? `What matters most to me is ${answers.values
          .map((key) => coreAnchors[key].label.toLowerCase())
          .join(", ")}.`
      : "";

  const aboutText = answers.aboutYou.trim()
    ? `About me: ${answers.aboutYou.trim()}`
    : "";

  const paragraphs = [lifestyleText, dayFeelText, valuesText, aboutText].filter(
    Boolean
  );

  const primaryLifestyle = answers.lifestyle[0] || "Future";
  const title = `My ${primaryLifestyle} Life`;
  const description = paragraphs.join("\n\n");

  return { title, description };
}

/**
 * Submits a new dream creation request through the Next.js API proxy.
 */
export async function createDream(
  payload: CreateDreamRequest
): Promise<DreamAnalysisResult> {
  const response = await fetch("/api/dreams", {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify(payload),
  });

  if (!response.ok) {
    let errorMessage = `Failed to create dream (${response.status})`;
    try {
      const data = await response.json();
      if (data?.error) {
        errorMessage = data.error;
      }
    } catch {
      // ignore parse error
    }
    throw new Error(errorMessage);
  }

  return response.json();
}

export async function updateDreamTargetDate(dreamId: string, targetDate: string): Promise<{ targetDate: string; targetYear: number }> {
  const response = await fetch(`/api/dreams/${encodeURIComponent(dreamId)}/target-date`, {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ targetDate }),
  });
  if (!response.ok) {
    const data = await response.json().catch(() => null);
    throw new Error(data?.error ?? `Failed to save target date (${response.status})`);
  }
  return response.json();
}

/**
 * Fetches the persisted dream details by ID through the Next.js API proxy.
 */
export async function getDreamDetails(
  dreamId: string
): Promise<DreamDetailsResponse> {
  const response = await fetch(`/api/dreams/${encodeURIComponent(dreamId)}`, {
    headers: {
      "Content-Type": "application/json",
    },
  });

  if (!response.ok) {
    let errorMessage = `Failed to load dream details (${response.status})`;
    try {
      const data = await response.json();
      if (data?.error) {
        errorMessage = data.error;
      }
    } catch {
      // ignore parse error
    }
    throw new Error(errorMessage);
  }

  return response.json();
}

/**
 * Fetches or generates the persisted future scenario by dream ID.
 */
export async function getFutureScenario(
  dreamId: string
): Promise<FutureScenarioResponse> {
  const response = await fetch(
    `/api/dreams/${encodeURIComponent(dreamId)}/future-scenario`,
    {
      headers: {
        "Content-Type": "application/json",
      },
    }
  );

  if (!response.ok) {
    let errorMessage = `Failed to load future scenario (${response.status})`;
    try {
      const data = await response.json();
      if (data?.error) {
        errorMessage = data.error;
      }
    } catch {
      // ignore parse error
    }
    throw new Error(errorMessage);
  }

  return response.json();
}

/**
 * Fetches or generates the persisted goals & gaps by dream ID.
 */
export async function getDreamGoals(
  dreamId: string
): Promise<GoalResponse[]> {
  const response = await fetch(
    `/api/dreams/${encodeURIComponent(dreamId)}/goals`,
    {
      headers: {
        "Content-Type": "application/json",
      },
    }
  );

  if (!response.ok) {
    let errorMessage = `Failed to load goals (${response.status})`;
    try {
      const data = await response.json();
      if (data?.error) {
        errorMessage = data.error;
      }
    } catch {
      // ignore parse error
    }
    throw new Error(errorMessage);
  }

  return response.json();
}

/**
 * Fetches or generates the daily path actions by dream ID.
 * Pass refresh=true to regenerate actions.
 * Pass planDate to get actions for a specific date (defaults to today if not provided).
 */
export async function getDailyPath(
  dreamId: string,
  refresh = false,
  planDate?: string
): Promise<DailyPathResponse> {
  const params = new URLSearchParams();
  if (refresh) params.set("refresh", "true");
  if (planDate) params.set("planDate", planDate);
  const queryString = params.toString();
  const response = await fetch(
    `/api/dreams/${encodeURIComponent(dreamId)}/daily-path${queryString ? `?${queryString}` : ""}`,
    {
      headers: {
        "Content-Type": "application/json",
      },
    }
  );

  if (!response.ok) {
    let errorMessage = `Failed to load daily path (${response.status})`;
    try {
      const data = await response.json();
      if (data?.error) {
        errorMessage = data.error;
      }
    } catch {
      // ignore parse error
    }
    throw new Error(errorMessage);
  }

  return response.json();
}

/** Fetches the persisted My Journey calendar for one selected month. */
export async function getJourneyCalendarMonth(
  dreamId: string,
  year: number,
  month: number
): Promise<JourneyCalendarMonthResponse> {
  const response = await fetch(
    `/api/dreams/${encodeURIComponent(dreamId)}/journey-calendar?year=${encodeURIComponent(String(year))}&month=${encodeURIComponent(String(month))}`,
    { headers: { "Content-Type": "application/json" } }
  );

  if (!response.ok) {
    let errorMessage = `Failed to load journey calendar (${response.status})`;
    try {
      const data = await response.json();
      if (data?.error) errorMessage = data.error;
    } catch {
      // ignore parse error
    }
    throw new Error(errorMessage);
  }

  return response.json();
}

/**
 * Fetches one persisted day snapshot. A 404 means that date is intentionally
 * empty; callers must not reconstruct it from today's live DailyAction rows.
 */
export async function getJourneyCalendarDay(
  dreamId: string,
  planDate: string
): Promise<JourneyCalendarDayResponse | null> {
  const response = await fetch(
    `/api/dreams/${encodeURIComponent(dreamId)}/journey-calendar/${encodeURIComponent(planDate)}`,
    { headers: { "Content-Type": "application/json" } }
  );

  if (response.status === 404) return null;
  if (!response.ok) {
    let errorMessage = `Failed to load journey day (${response.status})`;
    try {
      const data = await response.json();
      if (data?.error) errorMessage = data.error;
    } catch {
      // ignore parse error
    }
    throw new Error(errorMessage);
  }

  return response.json();
}

/**
 * Updates the status (Pending, Completed, Skipped) of a specific daily action.
 */
export async function updateDailyActionStatus(
  dreamId: string,
  actionId: string,
  status: "Pending" | "Completed" | "Skipped",
  scheduledDate?: string,
  planDate?: string
): Promise<DailyActionResponse> {
  const params = new URLSearchParams();
  if (planDate) params.set("planDate", planDate);
  const response = await fetch(
    `/api/dreams/${encodeURIComponent(dreamId)}/daily-actions/${encodeURIComponent(actionId)}${params.toString() ? `?${params.toString()}` : ""}`,
    {
      method: "PATCH",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify({ status, scheduledDate }),
    }
  );

  if (!response.ok) {
    let errorMessage = `Failed to update action status (${response.status})`;
    try {
      const data = await response.json();
      if (data?.error) {
        errorMessage = data.error;
      }
    } catch {
      // ignore parse error
    }
    throw new Error(errorMessage);
  }

  return response.json();
}

/**
 * Submits an evening reflection on how today felt and returns an AI future-evolution insight.
 */
export async function createReflection(
  dreamId: string,
  payload: CreateReflectionRequest
): Promise<ReflectionResponse> {
  const response = await fetch(
    `/api/dreams/${encodeURIComponent(dreamId)}/reflections`,
    {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify(payload),
    }
  );

  if (!response.ok) {
    let errorMessage = `Failed to submit reflection (${response.status})`;
    try {
      const data = await response.json();
      if (data?.error) {
        errorMessage = data.error;
      }
    } catch {
      // ignore parse error
    }
    throw new Error(errorMessage);
  }

  return response.json();
}

export async function updateReflection(
  dreamId: string,
  reflectionId: string,
  payload: Omit<CreateReflectionRequest, "planDate">
): Promise<ReflectionResponse> {
  const response = await fetch(
    `/api/dreams/${encodeURIComponent(dreamId)}/reflections/${encodeURIComponent(reflectionId)}`,
    {
      method: "PUT",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(payload),
    }
  );
  if (!response.ok) {
    const data = await response.json().catch(() => null);
    throw new Error(data?.error ?? `Failed to update reflection (${response.status})`);
  }
  return response.json();
}

/**
 * Fetches all past reflections for a given dream.
 */
export async function getReflections(
  dreamId: string
): Promise<ReflectionResponse[]> {
  const response = await fetch(
    `/api/dreams/${encodeURIComponent(dreamId)}/reflections`,
    {
      headers: {
        "Content-Type": "application/json",
      },
    }
  );

  if (!response.ok) {
    let errorMessage = `Failed to load reflections (${response.status})`;
    try {
      const data = await response.json();
      if (data?.error) {
        errorMessage = data.error;
      }
    } catch {
      // ignore parse error
    }
    throw new Error(errorMessage);
  }

  return response.json();
}

export async function sendLumaMessage(
  dreamId: string,
  message: string,
  history: LumaConversationTurn[]
): Promise<TalkToLumaResponse> {
  const response = await fetch(
    `/api/dreams/${encodeURIComponent(dreamId)}/conversation`,
    {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ message, history }),
    }
  );

  if (!response.ok) {
    let errorMessage = `Failed to reach LUMA (${response.status})`;
    try {
      const data = await response.json();
      if (data?.error) {
        errorMessage = data.error;
      }
    } catch {
      // ignore parse error
    }
    throw new Error(errorMessage);
  }

  return response.json();
}

export async function sendAgentMessage(
  dreamId: string,
  message: string
): Promise<AgentMessageResponse> {
  const response = await fetch(
    `/api/agent/${encodeURIComponent(dreamId)}/message`,
    {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ message }),
    }
  );

  if (!response.ok) {
    let errorMessage =
      response.status === 401
        ? "Please sign in to talk with LUMA."
        : response.status === 404
          ? "This future isn’t available."
          : response.status === 429
            ? "LUMA is taking a short pause. Please try again in a moment."
            : "LUMA couldn’t respond right now.";
    try {
      const data = await response.json();
      if (response.status < 500 && data?.error) {
        errorMessage = data.error;
      }
    } catch {
      // Keep the concise user-facing fallback.
    }
    throw new Error(errorMessage);
  }

  return response.json();
}

export async function applyDailyPathChange(
  dreamId: string,
  proposal: DailyPathChangeProposal
): Promise<DailyPathResponse> {
  const response = await fetch(
    `/api/dreams/${encodeURIComponent(dreamId)}/daily-path/adaptive-change`,
    {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        keepActionId: proposal.keepActionId,
        restActionIds: proposal.restActionIds,
      }),
    }
  );

  if (!response.ok) {
    let errorMessage =
      response.status === 401
        ? "Please sign in to update today’s path."
        : response.status === 404
          ? "This daily path is no longer available."
          : response.status === 429
            ? "LUMA is taking a short pause. Please try again in a moment."
            : "Today’s path could not be updated.";
    try {
      const data = await response.json();
      if (response.status < 500 && data?.error) {
        errorMessage = data.error;
      }
    } catch {
      // ignore parse error
    }
    throw new Error(errorMessage);
  }

  return response.json();
}

export async function getLatestDream(): Promise<{ id: string; title: string; createdAt: string } | null> {
  const response = await fetch("/api/dreams/latest");
  if (!response.ok) {
    if (response.status === 404 || response.status === 401) {
      return null;
    }
    throw new Error("Could not retrieve latest dream.");
  }
  return response.json();
}

/**
 * Retrieves the current authenticated user profile, including their saved theme preference.
 */
export async function getCurrentUser(): Promise<AuthenticatedUser | null> {
  try {
    const response = await fetch("/api/auth/me", { cache: "no-store" });
    if (!response.ok) return null;
    return response.json();
  } catch {
    return null;
  }
}

/**
 * Retrieves the persisted theme preference for the authenticated user.
 */
export async function getUserTheme(): Promise<UserThemeResponse | null> {
  try {
    const response = await fetch("/api/auth/theme", { cache: "no-store" });
    if (!response.ok) return null;
    return response.json();
  } catch {
    return null;
  }
}

/**
 * Persists an explicit theme change for the authenticated user to the backend database.
 */
export async function updateUserTheme(payload: UpdateThemePayload): Promise<UserThemeResponse> {
  const response = await fetch("/api/auth/theme", {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(payload),
  });

  if (!response.ok) {
    let errorMessage = "Failed to save theme preference.";
    try {
      const data = await response.json();
      if (data?.error) errorMessage = data.error;
    } catch {
      // ignore
    }
    throw new Error(errorMessage);
  }

  return response.json();
}
