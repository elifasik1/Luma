// =============================================================================
// LUMA — Theme Normalization
// Maps backend C# enum casing (e.g. "WarmDream") to frontend token keys
// (e.g. "warm_dream"). Uses explicit maps — never arbitrary string transforms.
// Unknown values safely fall back to defaults.
// =============================================================================

import type {
  PaletteKey,
  AtmosphereKey,
  IllustrationKey,
  MotionKey,
  ImageryKey,
  CoreAnchorKey,
} from "./tokens";

import {
  DEFAULT_PALETTE,
  DEFAULT_ATMOSPHERE,
  DEFAULT_MOTION,
  DEFAULT_ILLUSTRATION,
  coreAnchorKeys,
} from "./tokens";

// ---------------------------------------------------------------------------
// Palette normalization
// ---------------------------------------------------------------------------
const PALETTE_MAP: Record<string, PaletteKey> = {
  WarmDream: "warm_dream",
  LavenderMilk: "lavender_milk",
  SunsetCream: "sunset_cream",
  CityNight: "city_night",
  ForestMist: "forest_mist",
  OceanLight: "ocean_light",
  SocialSunrise: "social_sunrise",
  AmberSummit: "amber_summit",
  MakerWorkshop: "maker_workshop",
  // Also handle if backend sends snake_case already
  warm_dream: "warm_dream",
  lavender_milk: "lavender_milk",
  sunset_cream: "sunset_cream",
  city_night: "city_night",
  forest_mist: "forest_mist",
  ocean_light: "ocean_light",
  social_sunrise: "social_sunrise",
  amber_summit: "amber_summit",
  maker_workshop: "maker_workshop",
};

export function normalizePalette(backend: string): PaletteKey {
  return PALETTE_MAP[backend] ?? DEFAULT_PALETTE;
}

// ---------------------------------------------------------------------------
// Atmosphere normalization
// ---------------------------------------------------------------------------
const ATMOSPHERE_MAP: Record<string, AtmosphereKey> = {
  Airy: "airy",
  Cozy: "cozy",
  Energetic: "energetic",
  Calm: "calm",
  Creative: "creative",
  Focused: "focused",
  airy: "airy",
  cozy: "cozy",
  energetic: "energetic",
  calm: "calm",
  creative: "creative",
  focused: "focused",
};

export function normalizeAtmosphere(backend: string): AtmosphereKey {
  return ATMOSPHERE_MAP[backend] ?? DEFAULT_ATMOSPHERE;
}

// ---------------------------------------------------------------------------
// Illustration normalization
// ---------------------------------------------------------------------------
const ILLUSTRATION_MAP: Record<string, IllustrationKey> = {
  Dreamer: "dreamer",
  UrbanDreamer: "urban_dreamer",
  CreativeDreamer: "creative_dreamer",
  NatureDreamer: "nature_dreamer",
  ProfessionalDreamer: "professional_dreamer",
  dreamer: "dreamer",
  urban_dreamer: "urban_dreamer",
  creative_dreamer: "creative_dreamer",
  nature_dreamer: "nature_dreamer",
  professional_dreamer: "professional_dreamer",
};

export function normalizeIllustration(backend: string): IllustrationKey {
  return ILLUSTRATION_MAP[backend] ?? DEFAULT_ILLUSTRATION;
}

// ---------------------------------------------------------------------------
// Motion normalization
// ---------------------------------------------------------------------------
const MOTION_MAP: Record<string, MotionKey> = {
  Soft: "soft",
  Gentle: "gentle",
  Floating: "floating",
  Still: "still",
  soft: "soft",
  gentle: "gentle",
  floating: "floating",
  still: "still",
};

export function normalizeMotion(backend: string): MotionKey {
  return MOTION_MAP[backend] ?? DEFAULT_MOTION;
}

// ---------------------------------------------------------------------------
// Imagery normalization
// ---------------------------------------------------------------------------
const IMAGERY_MAP: Record<string, ImageryKey> = {
  City: "city",
  Nature: "nature",
  Ocean: "ocean",
  Campus: "campus",
  Workspace: "workspace",
  Bakery: "bakery",
  Travel: "travel",
  Home: "home",
  Books: "books",
  Technology: "technology",
  city: "city",
  nature: "nature",
  ocean: "ocean",
  campus: "campus",
  workspace: "workspace",
  bakery: "bakery",
  travel: "travel",
  home: "home",
  books: "books",
  technology: "technology",
};

export function normalizeImagery(backend: string): ImageryKey | null {
  return IMAGERY_MAP[backend] ?? null;
}

export function normalizeImageryList(backend: string[]): ImageryKey[] {
  return backend
    .map(normalizeImagery)
    .filter((v): v is ImageryKey => v !== null);
}

export function normalizeCoreAnchorKeys(backend: string[]): CoreAnchorKey[] {
  const allowedKeys = new Set<string>(coreAnchorKeys);
  return backend.filter((key): key is CoreAnchorKey => allowedKeys.has(key));
}
