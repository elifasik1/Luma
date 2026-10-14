"use client";

import React from "react";
import { ChoiceGrid, type ChoiceOption } from "@/components/ui/ChoiceGrid";
import { coreAnchors, coreAnchorKeys, type CoreAnchorKey } from "@/theme/tokens";

interface ValuesStepProps {
  selected: CoreAnchorKey[];
  onChange: (selected: CoreAnchorKey[]) => void;
}

const VALUES_OPTIONS: ChoiceOption<CoreAnchorKey>[] = coreAnchorKeys.map(
  (id) => ({
    id,
    label: coreAnchors[id].label,
    description: coreAnchors[id].description,
    icon: coreAnchors[id].icon,
  })
);

export const ValuesStep: React.FC<ValuesStepProps> = ({
  selected,
  onChange,
}) => {
  return (
    <div className="space-y-6 sm:space-y-8 animate-luma-fade-in">
      <div className="text-center max-w-2xl mx-auto space-y-3">
        <span className="text-xs uppercase tracking-widest text-luma-accent font-semibold">
          Step 3 • Core Anchors
        </span>
        <h2 className="font-serif text-3xl sm:text-4xl text-luma-primary tracking-tight">
          What matters most to you?
        </h2>
        <p className="text-sm sm:text-base text-luma-text-secondary">
          Pick the foundational values that will shape your future priorities.
        </p>
      </div>

      <ChoiceGrid
        options={VALUES_OPTIONS}
        selectedIds={selected}
        onChange={onChange}
        multiSelect={true}
        columns={3}
      />
    </div>
  );
};
