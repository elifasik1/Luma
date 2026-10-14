"use client";

import React from "react";
import { ChoiceGrid, type ChoiceOption } from "@/components/ui/ChoiceGrid";

interface LifestyleStepProps {
  selected: string[];
  onChange: (selected: string[]) => void;
}
const LIFESTYLE_OPTIONS: ChoiceOption[] = [
  {
    id: "Calm & Free",
    label: "Calm & Free",
    description: "Space to breathe, unhurried mornings, and peace of mind.",
    icon: "🌿",
  },
  {
    id: "Big City",
    label: "Big City",
    description: "Vibrant energy, culture, momentum, and endless possibilities.",
    icon: "🏙️",
  },
  {
    id: "Nature",
    label: "Nature",
    description: "Surrounded by greenery, fresh air, and organic balance.",
    icon: "🌲",
  },
  {
    id: "Creative",
    label: "Creative",
    description: "Making, expressing, designing, and bringing ideas to life.",
    icon: "🎨",
  },
  {
    id: "Social",
    label: "Social",
    description: "Rich communities, shared moments, and meaningful connections.",
    icon: "🤝",
  },
  {
    id: "Ambitious",
    label: "Ambitious",
    description: "Building, achieving milestones, and creating meaningful impact.",
    icon: "🚀",
  },
  {
    id: "Learning",
    label: "Learning",
    description: "Curiosity, growth, new skills, and always discovering more.",
    icon: "📚",
  },
  {
    id: "Exploring",
    label: "Exploring",
    description: "New places, new experiences, movement, and possibility.",
    icon: "✈️",
  },
  {
    id: "Independent",
    label: "Independent",
    description: "Freedom, self-direction, and a life shaped on your own terms.",
    icon: "🏡",
  },
  {
    id: "Building Something",
    label: "Building Something",
    description: "Turning an idea into something real, useful, and yours.",
    icon: "🛠️",
  },
  {
    id: "Ocean",
    label: "Ocean",
    description: "Open horizons, sunlight, water, and a sense of freedom.",
    icon: "🌊",
  },
  {
    id: "Not sure yet",
    label: "Not sure yet",
    description: "You don't have to know yet. We can discover it together.",
    icon: "✨",
  },
];

export const LifestyleStep: React.FC<LifestyleStepProps> = ({
  selected,
  onChange,
}) => {
  return (
    <div className="space-y-6 sm:space-y-8 animate-luma-fade-in">
      <div className="text-center max-w-2xl mx-auto space-y-3">
        <span className="text-xs uppercase tracking-widest text-luma-accent font-semibold">
          Step 1 • Life Canvas
        </span>
        <h2 className="font-serif text-3xl sm:text-4xl text-luma-primary tracking-tight">
          What kind of life feels closer to you?
        </h2>
        <p className="text-sm sm:text-base text-luma-text-secondary">
          Choose one or more environments that resonate with your inner vision.
        </p>
      </div>

      <ChoiceGrid
        options={LIFESTYLE_OPTIONS}
        selectedIds={selected}
        onChange={onChange}
        multiSelect={true}
        columns={3}
      />
    </div>
  );
};
