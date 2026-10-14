// =============================================================================
// LUMA — Theme Tokens
// Defines all palette, atmosphere, motion, illustration, and imagery tokens.
// The AI returns controlled values; this file maps them to concrete CSS values.
// AI must NEVER generate CSS directly.
// =============================================================================

// ---------------------------------------------------------------------------
// Token key types
// ---------------------------------------------------------------------------
export type PaletteKey =
  | "warm_dream"
  | "lavender_milk"
  | "sunset_cream"
  | "city_night"
  | "forest_mist"
  | "ocean_light"
  | "social_sunrise"
  | "amber_summit"
  | "maker_workshop";

export type AtmosphereKey =
  | "airy"
  | "cozy"
  | "energetic"
  | "calm"
  | "creative"
  | "focused";

export type IllustrationKey =
  | "dreamer"
  | "urban_dreamer"
  | "creative_dreamer"
  | "nature_dreamer"
  | "professional_dreamer";

export type MotionKey = "soft" | "gentle" | "floating" | "still";

export const dailyRhythmKeys = [
  "creating",
  "achieving",
  "exploring",
  "connecting",
  "learning",
  "slowing_down",
  "energizing",
  "reflecting",
  "building",
  "balancing",
] as const;

export type DailyRhythmKey = (typeof dailyRhythmKeys)[number];

export const coreAnchorKeys = [
  "career",
  "education",
  "travel",
  "financial_independence",
  "health",
  "family",
  "building_something",
  "creativity",
  "calmer_life",
] as const;

export type CoreAnchorKey = (typeof coreAnchorKeys)[number];

export type ImageryKey =
  | "city"
  | "nature"
  | "ocean"
  | "campus"
  | "workspace"
  | "bakery"
  | "travel"
  | "home"
  | "books"
  | "technology";

export interface DailyRhythmDefinition {
  label: string;
  description: string;
  icon: string;
  atmosphere: AtmosphereKey;
  motion: MotionKey;
  ambientElements: AmbientElementDefinition[];
}

export type AmbientElementType =
  | "spark"
  | "particle"
  | "glow"
  | "line"
  | "orbit"
  | "page"
  | "organic";

export type AmbientMotion = "float" | "drift" | "breathe" | "rise" | "still";

export interface AmbientElementDefinition {
  type: AmbientElementType;
  position: {
    top?: string;
    right?: string;
    bottom?: string;
    left?: string;
  };
  size: number;
  opacity: number;
  blur: number;
  delay: number;
  duration: number;
  rotation?: number;
  color: string;
  motion: AmbientMotion;
}

export const dailyRhythms: Record<DailyRhythmKey, DailyRhythmDefinition> = {
  creating: {
    label: "Creating",
    description: "Bring ideas to life and find your flow.",
    icon: "✨",
    atmosphere: "creative",
    motion: "floating",
    ambientElements: [
      { type: "spark", position: { top: "13%", left: "9%" }, size: 15, opacity: 0.3, blur: 0, delay: 0, duration: 27, color: "#DCC7EB", motion: "drift" },
      { type: "organic", position: { top: "24%", right: "7%" }, size: 42, opacity: 0.12, blur: 5, delay: 4, duration: 34, rotation: 24, color: "#DCC7EB", motion: "breathe" },
      { type: "particle", position: { top: "48%", left: "4%" }, size: 7, opacity: 0.34, blur: 1, delay: 2, duration: 23, color: "#F7A695", motion: "float" },
      { type: "glow", position: { bottom: "20%", right: "8%" }, size: 68, opacity: 0.16, blur: 12, delay: 7, duration: 38, color: "#C7DBC9", motion: "breathe" },
      { type: "spark", position: { bottom: "13%", left: "13%" }, size: 11, opacity: 0.24, blur: 0, delay: 9, duration: 29, color: "#F7A695", motion: "float" },
      { type: "particle", position: { top: "9%", right: "22%" }, size: 4, opacity: 0.28, blur: 0, delay: 5, duration: 31, color: "#DCC7EB", motion: "rise" },
    ],
  },
  achieving: {
    label: "Achieving",
    description: "Make steady progress toward meaningful goals.",
    icon: "🎯",
    atmosphere: "energetic",
    motion: "soft",
    ambientElements: [
      { type: "line", position: { top: "18%", left: "6%" }, size: 52, opacity: 0.15, blur: 0, delay: 0, duration: 32, rotation: -34, color: "#E8B866", motion: "rise" },
      { type: "particle", position: { top: "35%", right: "8%" }, size: 6, opacity: 0.34, blur: 1, delay: 3, duration: 24, color: "#E8B866", motion: "rise" },
      { type: "glow", position: { top: "11%", right: "18%" }, size: 54, opacity: 0.14, blur: 10, delay: 6, duration: 36, color: "#F0D9A0", motion: "breathe" },
      { type: "line", position: { bottom: "18%", right: "6%" }, size: 38, opacity: 0.12, blur: 0, delay: 5, duration: 30, rotation: 28, color: "#E8B866", motion: "rise" },
      { type: "particle", position: { bottom: "12%", left: "16%" }, size: 4, opacity: 0.3, blur: 0, delay: 8, duration: 22, color: "#F0D9A0", motion: "rise" },
    ],
  },
  exploring: {
    label: "Exploring",
    description: "Follow curiosity into new experiences.",
    icon: "🧭",
    atmosphere: "airy",
    motion: "floating",
    ambientElements: [
      { type: "spark", position: { top: "12%", right: "11%" }, size: 13, opacity: 0.28, blur: 0, delay: 1, duration: 30, color: "#7BC4D9", motion: "drift" },
      { type: "orbit", position: { top: "25%", left: "6%" }, size: 26, opacity: 0.17, blur: 0, delay: 5, duration: 40, rotation: -18, color: "#7BC4D9", motion: "drift" },
      { type: "particle", position: { top: "44%", right: "5%" }, size: 5, opacity: 0.3, blur: 1, delay: 4, duration: 28, color: "#7BC4D9", motion: "float" },
      { type: "line", position: { bottom: "19%", left: "8%" }, size: 44, opacity: 0.12, blur: 0, delay: 8, duration: 38, rotation: -22, color: "#2E4A5D", motion: "drift" },
      { type: "glow", position: { bottom: "11%", right: "17%" }, size: 62, opacity: 0.13, blur: 12, delay: 3, duration: 37, color: "#D0E6F0", motion: "breathe" },
      { type: "spark", position: { top: "17%", left: "23%" }, size: 9, opacity: 0.21, blur: 0, delay: 11, duration: 32, color: "#7BC4D9", motion: "float" },
    ],
  },
  connecting: {
    label: "Connecting",
    description: "Make space for presence, people, and warmth.",
    icon: "💬",
    atmosphere: "cozy",
    motion: "gentle",
    ambientElements: [
      { type: "glow", position: { top: "15%", left: "8%" }, size: 54, opacity: 0.14, blur: 13, delay: 0, duration: 38, color: "#F2917D", motion: "breathe" },
      { type: "particle", position: { top: "20%", left: "16%" }, size: 6, opacity: 0.3, blur: 1, delay: 2, duration: 29, color: "#F2917D", motion: "float" },
      { type: "particle", position: { top: "17%", left: "20%" }, size: 4, opacity: 0.26, blur: 0, delay: 5, duration: 31, color: "#FCDAD2", motion: "float" },
      { type: "orbit", position: { top: "31%", right: "7%" }, size: 22, opacity: 0.14, blur: 0, delay: 7, duration: 42, color: "#F2917D", motion: "drift" },
      { type: "glow", position: { bottom: "16%", right: "11%" }, size: 62, opacity: 0.13, blur: 15, delay: 4, duration: 40, color: "#FCDAD2", motion: "breathe" },
      { type: "particle", position: { bottom: "19%", left: "7%" }, size: 5, opacity: 0.28, blur: 1, delay: 9, duration: 27, color: "#F2917D", motion: "float" },
    ],
  },
  learning: {
    label: "Learning",
    description: "Grow your skills with curiosity and focus.",
    icon: "📚",
    atmosphere: "focused",
    motion: "soft",
    ambientElements: [
      { type: "page", position: { top: "13%", left: "7%" }, size: 17, opacity: 0.18, blur: 0, delay: 0, duration: 34, rotation: -12, color: "#BFAED7", motion: "drift" },
      { type: "line", position: { top: "20%", right: "8%" }, size: 40, opacity: 0.13, blur: 0, delay: 4, duration: 36, rotation: 18, color: "#BFAED7", motion: "still" },
      { type: "particle", position: { top: "39%", left: "5%" }, size: 5, opacity: 0.26, blur: 1, delay: 6, duration: 30, color: "#DCC7EB", motion: "float" },
      { type: "page", position: { bottom: "15%", right: "7%" }, size: 13, opacity: 0.16, blur: 0, delay: 8, duration: 39, rotation: 9, color: "#BFAED7", motion: "drift" },
      { type: "glow", position: { bottom: "11%", left: "19%" }, size: 48, opacity: 0.11, blur: 12, delay: 3, duration: 41, color: "#DCC7EB", motion: "breathe" },
    ],
  },
  slowing_down: {
    label: "Slowing Down",
    description: "Restore your energy with a gentler pace.",
    icon: "🌿",
    atmosphere: "calm",
    motion: "gentle",
    ambientElements: [
      { type: "organic", position: { top: "19%", left: "7%" }, size: 50, opacity: 0.12, blur: 8, delay: 0, duration: 48, rotation: -20, color: "#A0C2A1", motion: "breathe" },
      { type: "glow", position: { bottom: "18%", right: "8%" }, size: 70, opacity: 0.12, blur: 16, delay: 9, duration: 52, color: "#C7DBC9", motion: "breathe" },
      { type: "particle", position: { top: "13%", right: "18%" }, size: 5, opacity: 0.22, blur: 1, delay: 6, duration: 44, color: "#A0C2A1", motion: "float" },
      { type: "organic", position: { bottom: "12%", left: "13%" }, size: 32, opacity: 0.1, blur: 7, delay: 12, duration: 54, rotation: 34, color: "#C7DBC9", motion: "breathe" },
    ],
  },
  energizing: {
    label: "Energizing",
    description: "Bring movement and vitality into your days.",
    icon: "⚡",
    atmosphere: "energetic",
    motion: "floating",
    ambientElements: [
      { type: "spark", position: { top: "11%", left: "9%" }, size: 11, opacity: 0.28, blur: 0, delay: 0, duration: 21, color: "#F7A695", motion: "rise" },
      { type: "particle", position: { top: "26%", right: "6%" }, size: 6, opacity: 0.32, blur: 1, delay: 2, duration: 19, color: "#F7A695", motion: "rise" },
      { type: "glow", position: { top: "16%", right: "20%" }, size: 50, opacity: 0.15, blur: 11, delay: 4, duration: 27, color: "#F7A695", motion: "breathe" },
      { type: "particle", position: { top: "43%", left: "5%" }, size: 4, opacity: 0.32, blur: 0, delay: 6, duration: 18, color: "#DCC7EB", motion: "rise" },
      { type: "spark", position: { bottom: "14%", right: "9%" }, size: 9, opacity: 0.23, blur: 0, delay: 8, duration: 22, color: "#F7A695", motion: "float" },
      { type: "particle", position: { bottom: "19%", left: "17%" }, size: 5, opacity: 0.29, blur: 1, delay: 3, duration: 20, color: "#F7A695", motion: "rise" },
      { type: "glow", position: { bottom: "9%", right: "27%" }, size: 36, opacity: 0.1, blur: 8, delay: 10, duration: 25, color: "#DCC7EB", motion: "breathe" },
    ],
  },
  reflecting: {
    label: "Reflecting",
    description: "Notice what matters and make room to think.",
    icon: "🪞",
    atmosphere: "calm",
    motion: "still",
    ambientElements: [
      { type: "glow", position: { top: "18%", right: "9%" }, size: 54, opacity: 0.11, blur: 16, delay: 0, duration: 58, color: "#B4A4CD", motion: "breathe" },
      { type: "particle", position: { bottom: "18%", left: "8%" }, size: 4, opacity: 0.22, blur: 1, delay: 7, duration: 52, color: "#DCC7EB", motion: "still" },
      { type: "orbit", position: { top: "12%", left: "17%" }, size: 18, opacity: 0.1, blur: 0, delay: 11, duration: 60, color: "#B4A4CD", motion: "still" },
    ],
  },
  building: {
    label: "Building",
    description: "Turn small, consistent steps into something real.",
    icon: "🛠️",
    atmosphere: "focused",
    motion: "soft",
    ambientElements: [
      { type: "line", position: { top: "16%", left: "6%" }, size: 48, opacity: 0.14, blur: 0, delay: 0, duration: 40, rotation: 0, color: "#5BA4B5", motion: "drift" },
      { type: "line", position: { top: "23%", left: "9%" }, size: 29, opacity: 0.11, blur: 0, delay: 5, duration: 43, rotation: 90, color: "#8CC5D0", motion: "drift" },
      { type: "page", position: { top: "13%", right: "7%" }, size: 16, opacity: 0.17, blur: 0, delay: 7, duration: 42, rotation: 10, color: "#8CC5D0", motion: "still" },
      { type: "particle", position: { bottom: "18%", left: "5%" }, size: 5, opacity: 0.25, blur: 1, delay: 3, duration: 36, color: "#5BA4B5", motion: "float" },
      { type: "line", position: { bottom: "14%", right: "8%" }, size: 38, opacity: 0.12, blur: 0, delay: 9, duration: 44, rotation: -28, color: "#5BA4B5", motion: "drift" },
      { type: "glow", position: { bottom: "10%", right: "22%" }, size: 48, opacity: 0.1, blur: 12, delay: 4, duration: 46, color: "#8CC5D0", motion: "breathe" },
    ],
  },
  balancing: {
    label: "Balancing",
    description: "Make room for work, rest, people, and yourself.",
    icon: "⚖️",
    atmosphere: "airy",
    motion: "gentle",
    ambientElements: [
      { type: "glow", position: { top: "16%", left: "8%" }, size: 48, opacity: 0.12, blur: 13, delay: 0, duration: 42, color: "#C7DBC9", motion: "breathe" },
      { type: "orbit", position: { top: "22%", right: "8%" }, size: 24, opacity: 0.14, blur: 0, delay: 4, duration: 44, color: "#C7DBC9", motion: "drift" },
      { type: "particle", position: { top: "47%", left: "5%" }, size: 5, opacity: 0.26, blur: 1, delay: 7, duration: 38, color: "#F7A695", motion: "float" },
      { type: "particle", position: { top: "47%", right: "5%" }, size: 5, opacity: 0.26, blur: 1, delay: 7, duration: 38, color: "#DCC7EB", motion: "float" },
      { type: "glow", position: { bottom: "14%", right: "9%" }, size: 48, opacity: 0.12, blur: 13, delay: 2, duration: 43, color: "#DCC7EB", motion: "breathe" },
      { type: "orbit", position: { bottom: "17%", left: "8%" }, size: 24, opacity: 0.14, blur: 0, delay: 6, duration: 44, color: "#C7DBC9", motion: "drift" },
    ],
  },
};

export interface CoreAnchorDefinition {
  label: string;
  description: string;
  icon: string;
  motif: string;
  ambientElements: AmbientElementDefinition[];
}

export const coreAnchors: Record<CoreAnchorKey, CoreAnchorDefinition> = {
  career: {
    label: "Career",
    description: "Professional excellence and purposeful work.",
    icon: "💼",
    motif: "↗",
    ambientElements: [
      { type: "line", position: { top: "14%", right: "9%" }, size: 40, opacity: 0.12, blur: 0, delay: 1, duration: 40, rotation: -28, color: "#E8B866", motion: "rise" },
      { type: "glow", position: { bottom: "17%", left: "8%" }, size: 46, opacity: 0.1, blur: 12, delay: 6, duration: 46, color: "#F0D9A0", motion: "breathe" },
    ],
  },
  education: {
    label: "Education",
    description: "Academic growth and lifelong intellect.",
    icon: "🎓",
    motif: "▤",
    ambientElements: [
      { type: "page", position: { top: "15%", left: "8%" }, size: 15, opacity: 0.14, blur: 0, delay: 2, duration: 42, rotation: -8, color: "#BFAED7", motion: "drift" },
      { type: "line", position: { bottom: "16%", right: "8%" }, size: 34, opacity: 0.11, blur: 0, delay: 7, duration: 45, rotation: 12, color: "#DCC7EB", motion: "still" },
    ],
  },
  travel: {
    label: "Travel",
    description: "Seeing the world and diverse cultural horizons.",
    icon: "✈️",
    motif: "⌁",
    ambientElements: [
      { type: "orbit", position: { top: "16%", right: "7%" }, size: 22, opacity: 0.13, blur: 0, delay: 2, duration: 46, rotation: -16, color: "#7BC4D9", motion: "drift" },
      { type: "particle", position: { bottom: "18%", left: "9%" }, size: 5, opacity: 0.22, blur: 1, delay: 8, duration: 40, color: "#7BC4D9", motion: "float" },
    ],
  },
  financial_independence: {
    label: "Financial independence",
    description: "Autonomy, peace of mind, and financial security.",
    icon: "🌱",
    motif: "⌃",
    ambientElements: [
      { type: "glow", position: { top: "14%", left: "10%" }, size: 48, opacity: 0.1, blur: 13, delay: 3, duration: 50, color: "#C7DBC9", motion: "breathe" },
      { type: "line", position: { bottom: "14%", right: "8%" }, size: 30, opacity: 0.1, blur: 0, delay: 9, duration: 44, rotation: -35, color: "#C7DBC9", motion: "rise" },
    ],
  },
  health: {
    label: "Health",
    description: "Physical vitality, mental clarity, and longevity.",
    icon: "🧘",
    motif: "◌",
    ambientElements: [
      { type: "organic", position: { top: "17%", right: "8%" }, size: 42, opacity: 0.11, blur: 8, delay: 2, duration: 52, rotation: 14, color: "#C7DBC9", motion: "breathe" },
      { type: "glow", position: { bottom: "14%", left: "9%" }, size: 54, opacity: 0.1, blur: 15, delay: 7, duration: 50, color: "#F7A695", motion: "breathe" },
    ],
  },
  family: {
    label: "Family",
    description: "Nurturing home, loved ones, and generational warmth.",
    icon: "🏡",
    motif: "⌂",
    ambientElements: [
      { type: "particle", position: { top: "16%", left: "9%" }, size: 6, opacity: 0.2, blur: 1, delay: 1, duration: 42, color: "#F2917D", motion: "float" },
      { type: "particle", position: { top: "19%", left: "12%" }, size: 4, opacity: 0.18, blur: 0, delay: 5, duration: 44, color: "#FCDAD2", motion: "float" },
      { type: "glow", position: { bottom: "14%", right: "8%" }, size: 52, opacity: 0.11, blur: 14, delay: 8, duration: 52, color: "#FCDAD2", motion: "breathe" },
    ],
  },
  building_something: {
    label: "Building something",
    description: "Ventures, products, and enduring creations.",
    icon: "🛠️",
    motif: "▦",
    ambientElements: [
      { type: "line", position: { top: "15%", left: "8%" }, size: 38, opacity: 0.12, blur: 0, delay: 2, duration: 44, color: "#5BA4B5", motion: "still" },
      { type: "page", position: { bottom: "15%", right: "8%" }, size: 14, opacity: 0.14, blur: 0, delay: 7, duration: 46, rotation: 7, color: "#8CC5D0", motion: "drift" },
    ],
  },
  creativity: {
    label: "Creativity",
    description: "Artistic expression and unrestricted imagination.",
    icon: "🎨",
    motif: "✳",
    ambientElements: [
      { type: "organic", position: { top: "14%", left: "7%" }, size: 40, opacity: 0.11, blur: 6, delay: 2, duration: 43, rotation: -16, color: "#DCC7EB", motion: "breathe" },
      { type: "spark", position: { bottom: "15%", right: "9%" }, size: 10, opacity: 0.19, blur: 0, delay: 8, duration: 36, color: "#F7A695", motion: "drift" },
    ],
  },
  calmer_life: {
    label: "A calmer life",
    description: "Simplicity, less noise, and emotional balance.",
    icon: "☁️",
    motif: "◡",
    ambientElements: [
      { type: "organic", position: { top: "18%", left: "8%" }, size: 48, opacity: 0.09, blur: 12, delay: 0, duration: 58, color: "#C7DBC9", motion: "breathe" },
      { type: "glow", position: { bottom: "14%", right: "9%" }, size: 56, opacity: 0.09, blur: 16, delay: 9, duration: 58, color: "#DCC7EB", motion: "breathe" },
    ],
  },
};

// ---------------------------------------------------------------------------
// CSS custom property sets per palette
// ---------------------------------------------------------------------------
export interface PaletteTokens {
  "--luma-bg": string;
  "--luma-bg-secondary": string;
  "--luma-primary": string;
  "--luma-accent": string;
  "--luma-surface": string;
  "--luma-muted": string;
  "--luma-text": string;
  "--luma-text-secondary": string;
}

export const palettes: Record<PaletteKey, PaletteTokens> = {
  warm_dream: {
    "--luma-bg": "#FFF6EE",
    "--luma-bg-secondary": "#FFF0E3",
    "--luma-primary": "#4B2E4D",
    "--luma-accent": "#F7A695",
    "--luma-surface": "#DCC7EB",
    "--luma-muted": "#C7DBC9",
    "--luma-text": "#4B2E4D",
    "--luma-text-secondary": "#7A5A7E",
  },
  lavender_milk: {
    "--luma-bg": "#F5F0FA",
    "--luma-bg-secondary": "#EDE5F5",
    "--luma-primary": "#4B2E4D",
    "--luma-accent": "#DCC7EB",
    "--luma-surface": "#E8DCF2",
    "--luma-muted": "#C7DBC9",
    "--luma-text": "#3D2440",
    "--luma-text-secondary": "#6E4F73",
  },
  sunset_cream: {
    "--luma-bg": "#FFF6EE",
    "--luma-bg-secondary": "#FFEDE0",
    "--luma-primary": "#4B2E4D",
    "--luma-accent": "#F7A695",
    "--luma-surface": "#FDDCD3",
    "--luma-muted": "#DCC7EB",
    "--luma-text": "#4B2E4D",
    "--luma-text-secondary": "#8A5E5A",
  },
  city_night: {
    "--luma-bg": "#1E1B2E",
    "--luma-bg-secondary": "#2A2640",
    "--luma-primary": "#DCC7EB",
    "--luma-accent": "#F7A695",
    "--luma-surface": "#352F50",
    "--luma-muted": "#4A4466",
    "--luma-text": "#F0EBF5",
    "--luma-text-secondary": "#B8A8CC",
  },
  forest_mist: {
    "--luma-bg": "#F0F5F1",
    "--luma-bg-secondary": "#E5EDE6",
    "--luma-primary": "#3A5A3E",
    "--luma-accent": "#C7DBC9",
    "--luma-surface": "#D5E5D7",
    "--luma-muted": "#DCC7EB",
    "--luma-text": "#2E442F",
    "--luma-text-secondary": "#5A7A5D",
  },
  ocean_light: {
    "--luma-bg": "#F0F6FA",
    "--luma-bg-secondary": "#E3EFF7",
    "--luma-primary": "#2E4A5D",
    "--luma-accent": "#7BC4D9",
    "--luma-surface": "#D0E6F0",
    "--luma-muted": "#C7DBC9",
    "--luma-text": "#1E3A4E",
    "--luma-text-secondary": "#4E7A8E",
  },
  // Social — warm coral/rose, human, conversational
  social_sunrise: {
    "--luma-bg": "#FFF5F2",
    "--luma-bg-secondary": "#FFE8E2",
    "--luma-primary": "#6B3A3A",
    "--luma-accent": "#F2917D",
    "--luma-surface": "#FCDAD2",
    "--luma-muted": "#E8C5BD",
    "--luma-text": "#5A2D2D",
    "--luma-text-secondary": "#9A6B60",
  },
  // Ambitious — confident gold/charcoal, professional sunrise
  amber_summit: {
    "--luma-bg": "#1C1A28",
    "--luma-bg-secondary": "#272438",
    "--luma-primary": "#F0D9A0",
    "--luma-accent": "#E8B866",
    "--luma-surface": "#332F4A",
    "--luma-muted": "#494362",
    "--luma-text": "#F5EFDF",
    "--luma-text-secondary": "#C4B898",
  },
  // Building Something — workshop warmth, blueprint teal on deep slate
  maker_workshop: {
    "--luma-bg": "#1A1F2A",
    "--luma-bg-secondary": "#232836",
    "--luma-primary": "#8CC5D0",
    "--luma-accent": "#5BA4B5",
    "--luma-surface": "#2C3242",
    "--luma-muted": "#3D4555",
    "--luma-text": "#E0ECF0",
    "--luma-text-secondary": "#90ADB8",
  },
};

// ---------------------------------------------------------------------------
// Atmosphere tokens — affect border radius, spacing feel, shadow intensity
// ---------------------------------------------------------------------------
export interface AtmosphereTokens {
  "--luma-radius": string;
  "--luma-shadow": string;
  "--luma-spacing-feel": string; // multiplier-like hint
}

export const atmospheres: Record<AtmosphereKey, AtmosphereTokens> = {
  airy: {
    "--luma-radius": "1.5rem",
    "--luma-shadow": "0 4px 24px rgba(75, 46, 77, 0.06)",
    "--luma-spacing-feel": "generous",
  },
  cozy: {
    "--luma-radius": "1.25rem",
    "--luma-shadow": "0 2px 16px rgba(75, 46, 77, 0.1)",
    "--luma-spacing-feel": "comfortable",
  },
  energetic: {
    "--luma-radius": "1rem",
    "--luma-shadow": "0 4px 20px rgba(75, 46, 77, 0.08)",
    "--luma-spacing-feel": "dynamic",
  },
  calm: {
    "--luma-radius": "1.5rem",
    "--luma-shadow": "0 2px 20px rgba(75, 46, 77, 0.05)",
    "--luma-spacing-feel": "spacious",
  },
  creative: {
    "--luma-radius": "1.25rem",
    "--luma-shadow": "0 4px 24px rgba(75, 46, 77, 0.07)",
    "--luma-spacing-feel": "expressive",
  },
  focused: {
    "--luma-radius": "0.75rem",
    "--luma-shadow": "0 2px 12px rgba(75, 46, 77, 0.08)",
    "--luma-spacing-feel": "structured",
  },
};

// ---------------------------------------------------------------------------
// Motion tokens — CSS transition/animation durations
// ---------------------------------------------------------------------------
export interface MotionTokens {
  "--luma-transition-speed": string;
  "--luma-animation-speed": string;
}

export const motions: Record<MotionKey, MotionTokens> = {
  soft: {
    "--luma-transition-speed": "0.4s",
    "--luma-animation-speed": "3s",
  },
  gentle: {
    "--luma-transition-speed": "0.6s",
    "--luma-animation-speed": "4s",
  },
  floating: {
    "--luma-transition-speed": "0.8s",
    "--luma-animation-speed": "6s",
  },
  still: {
    "--luma-transition-speed": "0.2s",
    "--luma-animation-speed": "0s",
  },
};

// ---------------------------------------------------------------------------
// Illustration labels (for display; actual assets come later)
// ---------------------------------------------------------------------------
export const illustrationLabels: Record<IllustrationKey, string> = {
  dreamer: "Dreamer",
  urban_dreamer: "Urban Dreamer",
  creative_dreamer: "Creative Dreamer",
  nature_dreamer: "Nature Dreamer",
  professional_dreamer: "Professional Dreamer",
};

// ---------------------------------------------------------------------------
// Imagery labels and emoji hints (for display in Your World)
// ---------------------------------------------------------------------------
export const imageryLabels: Record<ImageryKey, { label: string; emoji: string }> = {
  city: { label: "City", emoji: "🏙️" },
  nature: { label: "Nature", emoji: "🌿" },
  ocean: { label: "Ocean", emoji: "🌊" },
  campus: { label: "Campus", emoji: "🎓" },
  workspace: { label: "Workspace", emoji: "💼" },
  bakery: { label: "Bakery", emoji: "🧁" },
  travel: { label: "Travel", emoji: "✈️" },
  home: { label: "Home", emoji: "🏡" },
  books: { label: "Books", emoji: "📚" },
  technology: { label: "Technology", emoji: "💻" },
};

// ---------------------------------------------------------------------------
// World ambient configuration — per-palette visual atmosphere for Discovery
// ---------------------------------------------------------------------------
export interface WorldAmbient {
  /** CSS gradient used as ambient background overlay */
  gradient: string;
  /** Decorative glow color (CSS value) */
  glowColor: string;
  /** Glow position: "top-right" | "bottom-left" | "center" | "top-left" */
  glowPosition: string;
  /** Optional secondary glow */
  secondaryGlow?: string;
  /** Emoji motif hint rendered as subtle floating decoration */
  motifEmoji?: string;
}

export const worldAmbients: Record<PaletteKey, WorldAmbient> = {
  warm_dream: {
    gradient: "radial-gradient(ellipse at 70% 20%, rgba(247,166,149,0.12), transparent 55%), linear-gradient(170deg, #FFF6EE 0%, #FFF0E3 50%, #DCC7EB20 100%)",
    glowColor: "rgba(247,166,149,0.18)",
    glowPosition: "top-right",
    motifEmoji: "☀️",
  },
  lavender_milk: {
    gradient: "radial-gradient(ellipse at 25% 30%, rgba(220,199,235,0.25), transparent 50%), radial-gradient(ellipse at 80% 70%, rgba(199,219,201,0.15), transparent 45%), linear-gradient(150deg, #F5F0FA 0%, #EDE5F5 60%, #E8DCF225 100%)",
    glowColor: "rgba(220,199,235,0.22)",
    glowPosition: "top-left",
    secondaryGlow: "rgba(199,219,201,0.12)",
    motifEmoji: "🎨",
  },
  sunset_cream: {
    gradient: "radial-gradient(ellipse at 60% 25%, rgba(247,166,149,0.18), transparent 50%), linear-gradient(165deg, #FFF6EE 0%, #FFEDE0 55%, #FDDCD320 100%)",
    glowColor: "rgba(247,166,149,0.20)",
    glowPosition: "top-right",
    motifEmoji: "🌅",
  },
  city_night: {
    gradient: "radial-gradient(ellipse at 50% 80%, rgba(247,166,149,0.08), transparent 50%), radial-gradient(ellipse at 80% 10%, rgba(220,199,235,0.10), transparent 40%), linear-gradient(175deg, #1E1B2E 0%, #2A2640 50%, #352F5030 100%)",
    glowColor: "rgba(247,166,149,0.12)",
    glowPosition: "bottom-left",
    secondaryGlow: "rgba(220,199,235,0.08)",
    motifEmoji: "🏙️",
  },
  forest_mist: {
    gradient: "radial-gradient(ellipse at 30% 60%, rgba(199,219,201,0.25), transparent 50%), radial-gradient(ellipse at 75% 20%, rgba(213,229,215,0.15), transparent 45%), linear-gradient(160deg, #F0F5F1 0%, #E5EDE6 60%, #D5E5D720 100%)",
    glowColor: "rgba(199,219,201,0.25)",
    glowPosition: "center",
    motifEmoji: "🌲",
  },
  ocean_light: {
    gradient: "radial-gradient(ellipse at 65% 15%, rgba(123,196,217,0.18), transparent 50%), radial-gradient(ellipse at 20% 80%, rgba(208,230,240,0.15), transparent 40%), linear-gradient(155deg, #F0F6FA 0%, #E3EFF7 55%, #D0E6F020 100%)",
    glowColor: "rgba(123,196,217,0.20)",
    glowPosition: "top-right",
    motifEmoji: "🌊",
  },
  social_sunrise: {
    gradient: "radial-gradient(ellipse at 55% 30%, rgba(242,145,125,0.16), transparent 48%), radial-gradient(ellipse at 30% 75%, rgba(252,218,210,0.18), transparent 45%), linear-gradient(160deg, #FFF5F2 0%, #FFE8E2 55%, #FCDAD220 100%)",
    glowColor: "rgba(242,145,125,0.18)",
    glowPosition: "center",
    secondaryGlow: "rgba(252,218,210,0.14)",
    motifEmoji: "☕",
  },
  amber_summit: {
    gradient: "radial-gradient(ellipse at 70% 15%, rgba(232,184,102,0.12), transparent 45%), radial-gradient(ellipse at 25% 85%, rgba(240,217,160,0.06), transparent 40%), linear-gradient(170deg, #1C1A28 0%, #272438 50%, #332F4A30 100%)",
    glowColor: "rgba(232,184,102,0.14)",
    glowPosition: "top-right",
    motifEmoji: "⚡",
  },
  maker_workshop: {
    gradient: "radial-gradient(ellipse at 45% 70%, rgba(91,164,181,0.10), transparent 45%), radial-gradient(ellipse at 80% 20%, rgba(140,197,208,0.08), transparent 40%), linear-gradient(165deg, #1A1F2A 0%, #232836 55%, #2C324230 100%)",
    glowColor: "rgba(91,164,181,0.12)",
    glowPosition: "bottom-left",
    secondaryGlow: "rgba(140,197,208,0.06)",
    motifEmoji: "🛠️",
  },
};

// ---------------------------------------------------------------------------
// Default theme
// ---------------------------------------------------------------------------
export const DEFAULT_PALETTE: PaletteKey = "warm_dream";
export const DEFAULT_ATMOSPHERE: AtmosphereKey = "airy";
export const DEFAULT_MOTION: MotionKey = "soft";
export const DEFAULT_ILLUSTRATION: IllustrationKey = "dreamer";
