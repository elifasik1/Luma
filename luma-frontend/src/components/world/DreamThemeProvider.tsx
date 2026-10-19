"use client";

import { useEffect, type ReactNode } from "react";
import type { DreamDetailsResponse } from "@/lib/types";
import { useLumaTheme } from "@/theme/ThemeProvider";

interface DreamThemeProviderProps {
  dream: DreamDetailsResponse;
  children: ReactNode;
}

export function DreamThemeProvider({ dream, children }: DreamThemeProviderProps) {
  const { syncWithDreamTheme } = useLumaTheme();
  const themeConfig = dream.futureProfile?.themeConfig;

  useEffect(() => {
    if (themeConfig) {
      syncWithDreamTheme(themeConfig);
    }
  }, [themeConfig, syncWithDreamTheme]);

  return <>{children}</>;
}
