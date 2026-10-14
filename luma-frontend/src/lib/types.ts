// =============================================================================
// LUMA — TypeScript Types
// Mirrors the verified ASP.NET Core backend contracts exactly.
// =============================================================================

import type { CoreAnchorKey, DailyRhythmKey } from "@/theme/tokens";

// ---------------------------------------------------------------------------
// POST /dreams — Request
// ---------------------------------------------------------------------------
export interface CreateDreamRequest {
  title: string;
  description: string;
  visualWorldKey?: VisualWorldKey;
  dailyRhythmKeys?: DailyRhythmKey[];
  coreAnchorKeys?: CoreAnchorKey[];
}

export type VisualWorldKey =
  | "calm_free"
  | "big_city"
  | "nature"
  | "creative"
  | "social"
  | "ambitious"
  | "learning"
  | "exploring"
  | "independent"
  | "building_something"
  | "ocean"
  | "not_sure";

// ---------------------------------------------------------------------------
// POST /dreams — Response (201 Created)
// ---------------------------------------------------------------------------
export interface DreamAnalysisResult {
  dreamId: string;
  futureProfileId: string;
  themeConfigId: string;
  title: string;
  summary: string;
  environment: string;
  mood: string;
  palette: string;
  atmosphere: string;
  illustration: string;
  motion: string;
}

// ---------------------------------------------------------------------------
// GET /dreams/{dreamId} — Response (200 OK)
// ---------------------------------------------------------------------------
export interface DreamDetailsResponse {
  id: string;
  title: string;
  rawInput: string;
  createdAt: string;
  userId: string;
  futureProfile: FutureProfileDetailsResponse | null;
}

export interface FutureProfileDetailsResponse {
  id: string;
  identity: string;
  environment: string;
  lifestyle: string[];
  aspirations: string[];
  values: string[];
  mood: string;
  pace: string;
  summary: string | null;
  themeConfig: ThemeConfigDetailsResponse | null;
}

export interface ThemeConfigDetailsResponse {
  id: string;
  palette: string;
  atmosphere: string;
  imagery: string[];
  illustration: string;
  motion: string;
  coreAnchorKeys: string[];
}

// ---------------------------------------------------------------------------
// GET /dreams/{dreamId}/future-scenario — Response (200 OK)
// ---------------------------------------------------------------------------
export interface FutureScenarioResponse {
  id: string;
  dreamId: string;
  title: string;
  intro: string;
  morning: string;
  daytime: string;
  evening: string;
  emotionalTone: string;
  closingReflection: string;
  createdAt: string;
}

// ---------------------------------------------------------------------------
// GET /dreams/{dreamId}/goals — Response (200 OK)
// ---------------------------------------------------------------------------
export interface GoalResponse {
  id: string;
  dreamId: string;
  title: string;
  description: string;
  category: string;
  whyItMatters: string;
  desiredOutcome: string;
  currentState: string;
  gap: string;
  status: string;
  order: number;
  createdAt: string;
}

// ---------------------------------------------------------------------------
// GET /dreams/{dreamId}/daily-path — Response (200 OK)
// ---------------------------------------------------------------------------
export type DailyActionStatus = "Pending" | "Completed" | "Skipped";

export interface DailyPathResponse {
  actions: DailyActionResponse[];
  generatedAt: string;
  totalActions: number;
  completedActions: number;
  skippedActions: number;
  momentumMessage: string;
}

export interface DailyActionResponse {
  id: string;
  dreamId: string;
  goalId: string | null;
  goalTitle: string;
  title: string;
  description: string;
  timeOfDay: string;
  estimatedMinutes: number;
  intention: string;
  order: number;
  status: DailyActionStatus;
  completedAt: string | null;
}

export interface UpdateDailyActionStatusRequest {
  status: DailyActionStatus;
}

// ---------------------------------------------------------------------------
// GET /dreams/{dreamId}/journey-calendar â€” My Journey snapshots
// ---------------------------------------------------------------------------
export interface JourneyCalendarMonthResponse {
  year: number;
  month: number;
  days: JourneyCalendarDaySummaryResponse[];
}

export interface JourneyCalendarDaySummaryResponse {
  planDate: string;
  hasSnapshot: boolean;
  actionCount: number;
  completedActionCount: number;
  reflections: JourneyReflectionSummaryResponse[];
}

export interface JourneyCalendarDayResponse {
  snapshotId: string;
  planDate: string;
  generatedAt: string;
  actions: JourneySnapshotActionResponse[];
  reflections: JourneyReflectionSummaryResponse[];
}

export interface JourneySnapshotActionResponse {
  id: string;
  goalId: string | null;
  goalTitle: string;
  title: string;
  description: string;
  timeOfDay: string;
  estimatedMinutes: number;
  intention: string;
  order: number;
  status: DailyActionStatus;
  completedAt: string | null;
}

export interface JourneyReflectionSummaryResponse {
  id: string;
  mood: string;
  notes: string;
  insight: string;
  createdAt: string;
}

// ---------------------------------------------------------------------------
// POST & GET /dreams/{dreamId}/reflections — Types
// ---------------------------------------------------------------------------
export interface CreateReflectionRequest {
  mood: string;
  notes: string;
}

export interface ReflectionResponse {
  id: string;
  dreamId: string;
  mood: string;
  notes: string;
  insight: string;
  createdAt: string;
}

// ---------------------------------------------------------------------------
// POST /dreams/{dreamId}/conversation — Types
// ---------------------------------------------------------------------------
export interface LumaConversationTurn {
  role: "user" | "assistant";
  content: string;
}

export interface TalkToLumaResponse {
  reply: string;
  proposal: DailyPathChangeProposal | null;
}

export interface AgentMessageResponse {
  message: string;
  usedTools: string[];
  actionProposal: AgentActionProposal | null;
}

export interface AgentActionProposal {
  actionName: "update_daily_plan";
  reason: string;
  requiresApproval: boolean;
  proposal: DailyPathChangeProposal | null;
  message: string;
}

export interface DailyPathChangeProposal {
  type: "suggest_plan_change";
  keepActionId: string;
  keepActionTitle: string;
  keepActionEstimatedMinutes: number;
  restActionIds: string[];
}

export interface ApplyDailyPathChangeRequest {
  keepActionId: string;
  restActionIds: string[];
}

// ---------------------------------------------------------------------------
// Backend error shape
// ---------------------------------------------------------------------------
export interface ApiError {
  error: string;
}

// ---------------------------------------------------------------------------
// Discovery flow (frontend-only, not sent to backend directly)
// ---------------------------------------------------------------------------
export interface DiscoveryAnswers {
  lifestyle: string[];
  dayFeel: DailyRhythmKey[];
  values: CoreAnchorKey[];
  aboutYou: string;
}
