"use client";

// =============================================================================
// LUMA — Theme Provider
// Sets CSS custom properties on a wrapper element based on theme token values.
// Components consume via var(--luma-*) — zero knowledge of active palette.
// =============================================================================

import {
  createContext,
  useContext,
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

// ---------------------------------------------------------------------------
// Theme shape
// ---------------------------------------------------------------------------
export interface LumaTheme {
  palette: PaletteKey;
  atmosphere: AtmosphereKey;
  motion: MotionKey;
  illustration: IllustrationKey;
  imagery: ImageryKey[];
}

const defaultTheme: LumaTheme = {
  palette: DEFAULT_PALETTE,
  atmosphere: DEFAULT_ATMOSPHERE,
  motion: DEFAULT_MOTION,
  illustration: DEFAULT_ILLUSTRATION,
  imagery: [],
};

// ---------------------------------------------------------------------------
// Context
// ---------------------------------------------------------------------------
const ThemeContext = createContext<LumaTheme>(defaultTheme);

export function useLumaTheme() {
  return useContext(ThemeContext);
}

// ---------------------------------------------------------------------------
// Provider
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
  theme,
  ambient = false,
  ambientRhythms = [],
  ambientAnchors = [],
  children,
}: ThemeProviderProps) {
  const merged: LumaTheme = { ...defaultTheme, ...theme };
  const themeTokenStyle = {
    ...palettes[merged.palette],
    ...atmospheres[merged.atmosphere],
    ...motions[merged.motion],
  } as CSSProperties;

  const ambientConfig = ambient ? worldAmbients[merged.palette] : null;
  const ambientElements = ambient
    ? [
        ...selectRhythmAmbientElements(
          ambientRhythms,
          ambientAnchors.length > 0 ? 5 : 7
        ),
        ...selectAnchorAmbientElements(ambientAnchors),
      ]
    : [];

  return (
  <ThemeContext.Provider value={merged}>
  <div
    className="min-h-screen"
    style={{
      ...themeTokenStyle,
      backgroundColor: "var(--luma-bg)",
      color: "var(--luma-text)",
      transition:
        "background-color var(--luma-transition-speed) ease, color var(--luma-transition-speed) ease",
    }}
  >
    {/* Ambient world atmosphere layer */}
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
          transition: "background var(--luma-transition-speed, 0.4s) ease, opacity var(--luma-transition-speed, 0.4s) ease",
        }}
      >
        <div className="luma-rhythm-ambient-field" aria-hidden="true">
          {ambientElements.map(({ key, element }, index) => (
            <div
              key={`${key}-${element.type}-${index}`}
              className={`luma-ambient-element luma-ambient-element--${element.type} luma-ambient-element--${element.motion}`}
              style={{
                ...element.position,
                "--ambient-size": `${element.size}px`,
                "--ambient-color": element.color,
                "--ambient-opacity": element.opacity,
                "--ambient-blur": `${element.blur}px`,
                "--ambient-delay": `${element.delay}s`,
                "--ambient-duration": `${element.duration}s`,
                "--ambient-rotation": `${element.rotation ?? 0}deg`,
              } as CSSProperties}
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
            ...(ambientConfig.glowPosition === "top-right" && { top: "5%", right: "8%" }),
            ...(ambientConfig.glowPosition === "top-left" && { top: "8%", left: "5%" }),
            ...(ambientConfig.glowPosition === "bottom-left" && { bottom: "10%", left: "8%" }),
            ...(ambientConfig.glowPosition === "center" && { top: "30%", left: "35%" }),
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
              animation: "luma-float var(--luma-animation-speed, 4s) ease-in-out infinite",
              transition: "opacity var(--luma-transition-speed, 0.4s) ease",
            }}
          >
            {ambientConfig.motifEmoji}
          </div>
        )}
      </div>
    )}
    <div style={{ position: "relative", zIndex: 1 }}>
      {children}
    </div>
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
  for (let elementIndex = 0; elementIndex < maxElementsPerRhythm && selected.length < maxElements; elementIndex++) {
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
