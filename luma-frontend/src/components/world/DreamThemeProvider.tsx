"use client";

import type { ReactNode } from "react";
import type { DreamDetailsResponse } from "@/lib/types";
import { normalizePalette, normalizeAtmosphere, normalizeIllustration, normalizeMotion, normalizeImageryList } from "@/theme/normalize";
import { ThemeProvider } from "@/theme/ThemeProvider";

interface DreamThemeProviderProps {
  dream: DreamDetailsResponse;
  children: ReactNode;
}

export function DreamThemeProvider({ dream, children }: DreamThemeProviderProps) {
  const themeConfig = dream.futureProfile?.themeConfig;

  return (
    <ThemeProvider
      theme={{
        palette: normalizePalette(themeConfig?.palette || "WarmDream"),
        atmosphere: normalizeAtmosphere(themeConfig?.atmosphere || "Airy"),
        illustration: normalizeIllustration(themeConfig?.illustration || "Dreamer"),
        motion: normalizeMotion(themeConfig?.motion || "Soft"),
        imagery: normalizeImageryList(themeConfig?.imagery || []),
      }}
    >
      {children}
    </ThemeProvider>
  );
}
