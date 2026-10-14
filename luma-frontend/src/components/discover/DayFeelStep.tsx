"use client";

import React from "react";
import { ChoiceGrid, type ChoiceOption } from "@/components/ui/ChoiceGrid";
import type { DailyRhythmKey } from "@/theme/tokens";
import { dailyRhythms, dailyRhythmKeys } from "@/theme/tokens";

interface DayFeelStepProps {
  selected: DailyRhythmKey[];
  onChange: (selected: DailyRhythmKey[]) => void;
}

const DAY_FEEL_OPTIONS: ChoiceOption<DailyRhythmKey>[] = dailyRhythmKeys.map(
  (id) => ({
    id,
    label: dailyRhythms[id].label,
    description: dailyRhythms[id].description,
    icon: dailyRhythms[id].icon,
  })
);

export const DayFeelStep: React.FC<DayFeelStepProps> = ({
  selected,
  onChange,
}) => {
  return (
    <div className="space-y-6 sm:space-y-8 animate-luma-fade-in">
      <div className="text-center max-w-2xl mx-auto space-y-3">
        <span className="text-xs uppercase tracking-widest text-luma-accent font-semibold">
          Step 2 • Daily Rhythm
        </span>
        <h2 className="font-serif text-3xl sm:text-4xl text-luma-primary tracking-tight">
          How would you like your days to feel?
        </h2>
        <p className="text-sm sm:text-base text-luma-text-secondary">
          Choose the rhythm of your days. Your world stays yours; its atmosphere will follow.
        </p>
      </div>

      <ChoiceGrid
        options={DAY_FEEL_OPTIONS}
        selectedIds={selected}
        onChange={onChange}
        multiSelect={true}
        columns={3}
      />
    </div>
  );
};
