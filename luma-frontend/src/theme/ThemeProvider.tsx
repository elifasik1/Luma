"use client";

// =============================================================================
// LUMA — Unified Theme Provider
// Manages the single source of truth for active theme preferences across all pages.
// Applies CSS custom properties based on Theme Registry tokens.
// Automatically hydrates from cache and synchronizes with PostgreSQL backend.
// =============================================================================

import {
  createContext,
  useContext,
  useState,
  useEffect,
  useCallback,
  useRef,
  useMemo,
  type ReactNode,
} from "react";
import type { CSSProperties } from "react";

import type {
  PaletteKey,
  AtmosphereKey,
  MotionKey,
  IllustrationKey,
  ImageryKey,
  DailyRhythmKey,
  CoreAnchorKey,
  AmbientElementDefinition,
} from "./tokens";

import {
  palettes,
  atmospheres,
  motions,
  worldAmbients,
  dailyRhythms,
  coreAnchors,
  DEFAULT_PALETTE,
  DEFAULT_ATMOSPHERE,
  DEFAULT_MOTION,
  DEFAULT_ILLUSTRATION,
} from "./tokens";

import {
  normalizePalette,
  normalizeAtmosphere,
  normalizeMotion,
  normalizeIllustration,
} from "./normalize";
import { getUserTheme, updateUserTheme } from "@/lib/api";
import type { ThemeConfigDetailsResponse } from "@/lib/types";

// ---------------------------------------------------------------------------
// Theme Shape & Context Value
// ---------------------------------------------------------------------------
export interface LumaTheme {
  palette: PaletteKey;
  atmosphere: AtmosphereKey;
  motion: MotionKey;
  illustration: IllustrationKey;
  imagery: ImageryKey[];
}

export interface ThemeContextValue extends LumaTheme {
  theme: LumaTheme;
  setTheme: (
    newTheme: Partial<LumaTheme>,
    options?: { persist?: boolean; markExplicit?: boolean }
  ) => Promise<void>;
  syncWithDreamTheme: (
    themeConfig: ThemeConfigDetailsResponse | null | undefined
  ) => void;
  isLoading: boolean;
}

const defaultTheme: LumaTheme = {
  palette: DEFAULT_PALETTE,
  atmosphere: DEFAULT_ATMOSPHERE,
  motion: DEFAULT_MOTION,
  illustration: DEFAULT_ILLUSTRATION,
  imagery: [],
};

const defaultContextValue: ThemeContextValue = {
  ...defaultTheme,
  theme: defaultTheme,
  setTheme: async () => {},
  syncWithDreamTheme: () => {},
  isLoading: false,
};

const ThemeContext = createContext<ThemeContextValue>(defaultContextValue);

export function useLumaTheme(): ThemeContextValue {
  return useContext(ThemeContext);
}

// ---------------------------------------------------------------------------
// LocalStorage Keys
// ---------------------------------------------------------------------------
const STORAGE_KEY_PALETTE = "luma_theme_palette";
const STORAGE_KEY_EXPLICIT = "luma_theme_is_explicit";

function isUserExplicitChoiceInStorage(): boolean {
  if (typeof window === "undefined") return false;
  try {
    return localStorage.getItem(STORAGE_KEY_EXPLICIT) === "true";
  } catch {
    return false;
  }
}

// ---------------------------------------------------------------------------
// Provider Component
// ---------------------------------------------------------------------------
interface ThemeProviderProps {
  theme?: Partial<LumaTheme>;
  /** When true, renders the ambient world atmosphere layer (Discovery) */
  ambient?: boolean;
  /** Controlled Discovery rhythms that softly shape the ambient world layer */
  ambientRhythms?: readonly DailyRhythmKey[];
  /** Controlled Discovery anchors that add semantic ambient details */
  ambientAnchors?: readonly CoreAnchorKey[];
  children: ReactNode;
}

export function ThemeProvider({
  theme: initialPropTheme,
  ambient = false,
  ambientRhythms = [],
  ambientAnchors = [],
  children,
}: ThemeProviderProps) {
  const parentContext = useContext(ThemeContext);
  const isRootProvider = parentContext === defaultContextValue;

  const [activeTheme, setActiveTheme] = useState<LumaTheme>(() => {
    if (initialPropTheme) {
      return { ...defaultTheme, ...initialPropTheme };
    }
    return defaultTheme;
  });
  const [isLoading, setIsLoading] = useState<boolean>(isRootProvider);
  const hasUserExplicitlyChosenRef = useRef<boolean>(isUserExplicitChoiceInStorage());
  const hasResolvedThemeRef = useRef<boolean>(false);

  const effectiveTheme: LumaTheme = useMemo(() => {
    if (initialPropTheme) {
      return { ...activeTheme, ...initialPropTheme };
    }
    return activeTheme;
  }, [activeTheme, initialPropTheme]);

  // On Root mount: hydrate cached palette from localStorage, then load persisted user theme from backend (PostgreSQL)
  useEffect(() => {
    if (!isRootProvider) return;

    let isMounted = true;

    async function loadBackendTheme() {
      // Hydrate local cache without SSR hydration mismatch
      try {
        const cachedPalette = localStorage.getItem(STORAGE_KEY_PALETTE);
        if (cachedPalette && isMounted) {
          setActiveTheme((prev) => ({
            ...prev,
            palette: normalizePalette(cachedPalette),
          }));
        }
      } catch {
        // ignore localStorage errors
      }

      try {
        const savedTheme = await getUserTheme();
        if (!isMounted) return;

        if (savedTheme?.palette) {
          const resolvedTheme: LumaTheme = {
            palette: normalizePalette(savedTheme.palette),
            atmosphere: normalizeAtmosphere(savedTheme.atmosphere || "calm"),
            illustration: normalizeIllustration(savedTheme.illustration || "dreamer"),
            motion: normalizeMotion(savedTheme.motion || "soft"),
            imagery: [],
          };
          setActiveTheme(resolvedTheme);
          const isExplicitChoice = savedTheme.isExplicit ?? false;
          hasUserExplicitlyChosenRef.current = isExplicitChoice;
          hasResolvedThemeRef.current = true;

          try {
            localStorage.setItem(STORAGE_KEY_PALETTE, resolvedTheme.palette);
            localStorage.setItem(STORAGE_KEY_EXPLICIT, String(isExplicitChoice));
          } catch {
            // ignore
          }
        }
      } catch (err) {
        // Safe fallback: do not delete or overwrite saved preference on network/load failure
        console.warn("[LUMA Theme] Could not load saved theme, keeping active fallback.", err);
      } finally {
        if (isMounted) {
          setIsLoading(false);
        }
      }
    }

    void loadBackendTheme();

    return () => {
      isMounted = false;
    };
  }, [isRootProvider]);

  // Explicit user theme changer (persisted to PostgreSQL)
  const setTheme = useCallback(
    async (
      newTheme: Partial<LumaTheme>,
      options?: { persist?: boolean; markExplicit?: boolean }
    ) => {
      const persist = options?.persist ?? true;
      const markExplicit = options?.markExplicit ?? persist;
      const normalizedPalette = newTheme.palette
        ? normalizePalette(newTheme.palette)
        : activeTheme.palette;
      const normalizedAtmosphere = newTheme.atmosphere
        ? normalizeAtmosphere(newTheme.atmosphere)
        : activeTheme.atmosphere;
      const normalizedIllustration = newTheme.illustration
        ? normalizeIllustration(newTheme.illustration)
        : activeTheme.illustration;
      const normalizedMotion = newTheme.motion
        ? normalizeMotion(newTheme.motion)
        : activeTheme.motion;

      const merged: LumaTheme = {
        palette: normalizedPalette,
        atmosphere: normalizedAtmosphere,
        illustration: normalizedIllustration,
        motion: normalizedMotion,
        imagery: newTheme.imagery ?? activeTheme.imagery,
      };

      if (markExplicit) {
        hasUserExplicitlyChosenRef.current = true;
        hasResolvedThemeRef.current = true;
      }
      setActiveTheme(merged);

      if (markExplicit) {
        try {
          localStorage.setItem(STORAGE_KEY_PALETTE, merged.palette);
          localStorage.setItem(STORAGE_KEY_EXPLICIT, "true");
        } catch {
          // ignore
        }
      }

      if (persist) {
        try {
          await updateUserTheme({
            palette: merged.palette,
            atmosphere: merged.atmosphere,
            illustration: merged.illustration,
            motion: merged.motion,
          });
        } catch (err) {
          console.error("[LUMA Theme] Failed to persist theme change to backend:", err);
          throw err;
        }
      }
    },
    [activeTheme]
  );

  // Sync with dream theme unless explicitly overridden by user
  const syncWithDreamTheme = useCallback(
    (themeConfig: ThemeConfigDetailsResponse | null | undefined) => {
      if (!themeConfig) return;
      if (isLoading) return;
      if (hasUserExplicitlyChosenRef.current) {
        // User has already explicitly chosen or loaded an account preference; do not overwrite!
        return;
      }

      const dreamTheme: LumaTheme = {
        palette: normalizePalette(themeConfig.palette || "WarmDream"),
        atmosphere: normalizeAtmosphere(themeConfig.atmosphere || "Airy"),
        illustration: normalizeIllustration(themeConfig.illustration || "Dreamer"),
        motion: normalizeMotion(themeConfig.motion || "Soft"),
        imagery: [],
      };
      setActiveTheme(dreamTheme);
      try {
        localStorage.setItem(STORAGE_KEY_PALETTE, dreamTheme.palette);
      } catch {
        // ignore
      }
    },
    [isLoading]
  );

  // Apply CSS custom properties to html element for universal consistency
  useEffect(() => {
    if (typeof document === "undefined") return;
    if (!isRootProvider) return; // Prevent nested/local theme providers from overriding root HTML attributes
    const root = document.documentElement;
    root.setAttribute("data-theme", effectiveTheme.palette);

    const tokenProperties = {
      ...palettes[effectiveTheme.palette],
      ...atmospheres[effectiveTheme.atmosphere],
      ...motions[effectiveTheme.motion],
    } as Record<string, string>;

    for (const [key, value] of Object.entries(tokenProperties)) {
      if (key.startsWith("--")) {
        root.style.setProperty(key, value);
      }
    }
  }, [effectiveTheme, isRootProvider]);

  const themeTokenStyle = useMemo(
    () =>
      ({
        ...palettes[effectiveTheme.palette],
        ...atmospheres[effectiveTheme.atmosphere],
        ...motions[effectiveTheme.motion],
      } as CSSProperties),
    [effectiveTheme]
  );

  const ambientConfig = ambient ? worldAmbients[effectiveTheme.palette] : null;
  const ambientElements = ambient
    ? [
        ...selectRhythmAmbientElements(
          ambientRhythms,
          ambientAnchors.length > 0 ? 5 : 7
        ),
        ...selectAnchorAmbientElements(ambientAnchors),
      ]
    : [];

  const contextValue: ThemeContextValue = useMemo(
    () => ({
      ...effectiveTheme,
      theme: effectiveTheme,
      setTheme,
      syncWithDreamTheme,
      isLoading,
    }),
    [effectiveTheme, setTheme, syncWithDreamTheme, isLoading]
  );

  return (
    <ThemeContext.Provider value={contextValue}>
      <div
        className="min-h-screen"
        data-theme={effectiveTheme.palette}
        suppressHydrationWarning
        style={{
          ...themeTokenStyle,
          backgroundColor: "var(--luma-bg)",
          color: "var(--luma-text)",
          transition:
            "background-color var(--luma-transition-speed) ease, color var(--luma-transition-speed) ease",
        }}
      >
        {/* Ambient world atmosphere layer (Discovery & previews) */}
        {ambientConfig && (
          <div
            className="luma-world-ambient"
            aria-hidden="true"
            style={{
              position: "fixed",
              inset: 0,
              pointerEvents: "none",
              zIndex: 0,
              background: ambientConfig.gradient,
              transition:
                "background var(--luma-transition-speed, 0.4s) ease, opacity var(--luma-transition-speed, 0.4s) ease",
            }}
          >
            <div className="luma-rhythm-ambient-field" aria-hidden="true">
              {ambientElements.map(({ key, element }, index) => (
                <div
                  key={`${key}-${element.type}-${index}`}
                  className={`luma-ambient-element luma-ambient-element--${element.type} luma-ambient-element--${element.motion}`}
                  style={
                    {
                      ...element.position,
                      "--ambient-size": `${element.size}px`,
                      "--ambient-color": element.color,
                      "--ambient-opacity": element.opacity,
                      "--ambient-blur": `${element.blur}px`,
                      "--ambient-delay": `${element.delay}s`,
                      "--ambient-duration": `${element.duration}s`,
                      "--ambient-rotation": `${element.rotation ?? 0}deg`,
                    } as CSSProperties
                  }
                />
              ))}
            </div>
            {/* Primary glow */}
            <div
              className="luma-world-glow"
              style={{
                position: "absolute",
                width: "clamp(12rem, 40vw, 28rem)",
                aspectRatio: "1",
                borderRadius: "50%",
                background: ambientConfig.glowColor,
                filter: "blur(80px)",
                opacity: 0.7,
                transition: "all var(--luma-transition-speed, 0.4s) ease",
                ...(ambientConfig.glowPosition === "top-right" && {
                  top: "5%",
                  right: "8%",
                }),
                ...(ambientConfig.glowPosition === "top-left" && {
                  top: "8%",
                  left: "5%",
                }),
                ...(ambientConfig.glowPosition === "bottom-left" && {
                  bottom: "10%",
                  left: "8%",
                }),
                ...(ambientConfig.glowPosition === "center" && {
                  top: "30%",
                  left: "35%",
                }),
              }}
            />
            {/* Secondary glow */}
            {ambientConfig.secondaryGlow && (
              <div
                style={{
                  position: "absolute",
                  width: "clamp(8rem, 25vw, 18rem)",
                  aspectRatio: "1",
                  borderRadius: "50%",
                  background: ambientConfig.secondaryGlow,
                  filter: "blur(60px)",
                  opacity: 0.5,
                  bottom: "15%",
                  right: "20%",
                  transition: "all var(--luma-transition-speed, 0.4s) ease",
                }}
              />
            )}
            {!ambientElements.length && ambientConfig.motifEmoji && (
              <div
                className="luma-world-motif"
                style={{
                  position: "absolute",
                  top: "12%",
                  right: "12%",
                  fontSize: "clamp(2rem, 5vw, 3.5rem)",
                  opacity: 0.08,
                  animation:
                    "luma-float var(--luma-animation-speed, 4s) ease-in-out infinite",
                  transition: "opacity var(--luma-transition-speed, 0.4s) ease",
                }}
              >
                {ambientConfig.motifEmoji}
              </div>
            )}
          </div>
        )}
        <div className="relative z-10" style={{ position: "relative", zIndex: 10 }}>{children}</div>
      </div>
    </ThemeContext.Provider>
  );
}

function selectRhythmAmbientElements(
  rhythmKeys: readonly DailyRhythmKey[],
  maxElements: number
) {
  const rhythmElements = rhythmKeys.map((rhythmKey) => ({
    rhythmKey,
    elements: dailyRhythms[rhythmKey].ambientElements,
  }));
  const selected: Array<{
    key: DailyRhythmKey;
    element: AmbientElementDefinition;
  }> = [];

  const maxElementsPerRhythm = rhythmElements.length === 1 ? maxElements : 3;
  for (
    let elementIndex = 0;
    elementIndex < maxElementsPerRhythm && selected.length < maxElements;
    elementIndex++
  ) {
    for (const rhythm of rhythmElements) {
      const element = rhythm.elements[elementIndex];
      if (!element || selected.length >= maxElements) continue;
      selected.push({ key: rhythm.rhythmKey, element });
    }
  }

  return selected;
}

function selectAnchorAmbientElements(anchorKeys: readonly CoreAnchorKey[]) {
  return anchorKeys.slice(0, 2).flatMap((key) =>
    coreAnchors[key].ambientElements.slice(0, 1).map((element) => ({
      key,
      element,
    }))
  );
}
