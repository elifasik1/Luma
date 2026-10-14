"use client";

import React from "react";
import { Textarea } from "@/components/ui/Textarea";

interface AboutYouStepProps {
  value: string;
  onChange: (value: string) => void;
}

export const AboutYouStep: React.FC<AboutYouStepProps> = ({
  value,
  onChange,
}) => {
  return (
    <div className="space-y-6 sm:space-y-8 animate-luma-fade-in max-w-2xl mx-auto">
      <div className="text-center space-y-3">
        <span className="text-xs uppercase tracking-widest text-luma-accent font-semibold">
          Step 4 • Personal Voice
        </span>
        <h2 className="font-serif text-3xl sm:text-4xl text-luma-primary tracking-tight">
          Tell me a little about yourself.
        </h2>
        <p className="text-sm sm:text-base text-luma-text-secondary leading-relaxed">
          Share your dreams, where you are now, or any details about the life you
          want to create. LUMA will listen and begin shaping your world.
        </p>
      </div>

      <div className="pt-2">
        <Textarea
          rows={6}
          placeholder="For example: I want to transition into creative technology, live near the coast, and build a calm morning routine with books and coffee..."
          value={value}
          onChange={(e) => onChange(e.target.value)}
          helperText="Write as freely as you like. There are no right or wrong answers."
        />
      </div>
    </div>
  );
};
