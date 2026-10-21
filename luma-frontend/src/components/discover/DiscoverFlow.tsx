"use client";

import React from "react";
import Link from "next/link";
import { getDiscoveryPreviewTheme, useDiscoverFlow } from "@/hooks/useDiscoverFlow";
import { LifestyleStep } from "./LifestyleStep";
import { DayFeelStep } from "./DayFeelStep";
import { ValuesStep } from "./ValuesStep";
import { AboutYouStep } from "./AboutYouStep";
import { TargetYearStep } from "./TargetYearStep";
import { Button } from "@/components/ui/Button";
import { ThemeProvider } from "@/theme/ThemeProvider";

export const DiscoverFlow: React.FC = () => {
  const {
    currentStep,
    totalSteps,
    answers,
    isSubmitting,
    submissionPhase,
    error,
    updateLifestyle,
    updateDayFeel,
    updateValues,
    updateAboutYou,
    updateTargetDate,
    nextStep,
    prevStep,
    canProceed,
    submitDiscovery,
  } = useDiscoverFlow();
  const previewTheme = getDiscoveryPreviewTheme(answers);

  if (isSubmitting) {
    return (
      <ThemeProvider
        theme={previewTheme}
        ambient
        ambientRhythms={answers.dayFeel}
        ambientAnchors={currentStep === 3 ? answers.values : []}
      >
      <div className="min-h-[70vh] flex flex-col items-center justify-center text-center px-4">
        {/* Soft glowing orb */}
        <div className="relative mb-8">
          <div className="w-24 h-24 sm:w-32 sm:h-32 rounded-full bg-gradient-to-tr from-luma-peach via-luma-lilac to-luma-sage opacity-70 blur-xl animate-luma-glow pointer-events-none" />
          <div className="absolute inset-0 flex items-center justify-center">
            <span className="text-3xl sm:text-4xl animate-bounce">✦</span>
          </div>
        </div>

        <h2 className="font-serif text-3xl sm:text-4xl text-luma-primary tracking-tight mb-3">
          {submissionPhase === "analyzing" && "Listening to your thoughts..."}
          {submissionPhase === "shaping" && "Your future world is taking shape..."}
          {submissionPhase === "ready" && "Illuminating your world..."}
        </h2>

        <p className="text-base text-luma-text-secondary max-w-md font-light leading-relaxed">
          {submissionPhase === "analyzing" &&
            "Gathering the themes, desires, and rhythms of your imagined life."}
          {submissionPhase === "shaping" &&
            "Composing your environment, aspirations, mood, and personal aesthetic."}
          {submissionPhase === "ready" &&
            "Preparing your personal future canvas."}
        </p>
      </div>
      </ThemeProvider>
    );
  }

  return (
    <ThemeProvider
      theme={previewTheme}
      ambient
      ambientRhythms={answers.dayFeel}
      ambientAnchors={currentStep === 3 ? answers.values : []}
    >
    <div className="w-full max-w-4xl mx-auto px-4 sm:px-6 py-6 sm:py-10">
      {/* Top Header & Progress */}
      <div className="flex items-center justify-between mb-8 sm:mb-12 border-b border-luma-primary/10 pb-4">
        <Link
          href="/"
          className="text-xs sm:text-sm font-medium text-luma-text-secondary hover:text-luma-primary transition-colors flex items-center gap-1.5"
        >
          <span>←</span>
          <span>Back to Home</span>
        </Link>

        {/* Step Indicator */}
        <div className="flex items-center gap-2">
          {Array.from({ length: totalSteps }, (_, i) => (
            <div
              key={i}
              className={`h-1.5 rounded-full transition-all duration-300 ${
                i + 1 === currentStep
                  ? "w-8 bg-luma-primary"
                  : i + 1 < currentStep
                  ? "w-3 bg-luma-accent"
                  : "w-3 bg-luma-primary/15"
              }`}
              aria-label={`Step ${i + 1} of ${totalSteps}`}
            />
          ))}
          <span className="text-xs font-semibold text-luma-primary ml-2">
            {currentStep}/{totalSteps}
          </span>
        </div>
      </div>

      {/* Error Banner if any */}
      {error && (
        <div className="mb-6 p-4 rounded-2xl bg-rose-50 border border-rose-200 text-rose-800 text-sm flex items-center justify-between">
          <span>{error}</span>
          <Button
            size="sm"
            variant="ghost"
            onClick={() => submitDiscovery()}
            className="text-rose-800 hover:bg-rose-100"
          >
            Retry
          </Button>
        </div>
      )}

      {/* Active Step Content */}
      <div className="min-h-[420px] flex flex-col justify-between">
        <div>
          {currentStep === 1 && (
            <LifestyleStep
              selected={answers.lifestyle}
              onChange={updateLifestyle}
            />
          )}

          {currentStep === 2 && (
            <DayFeelStep
              selected={answers.dayFeel}
              onChange={updateDayFeel}
            />
          )}

          {currentStep === 3 && (
            <ValuesStep
              selected={answers.values}
              onChange={updateValues}
            />
          )}

          {currentStep === 4 && (
            <AboutYouStep
              value={answers.aboutYou}
              onChange={updateAboutYou}
            />
          )}

          {currentStep === 5 && (
            <TargetYearStep value={answers.targetDate} onChange={updateTargetDate} />
          )}
        </div>

        {/* Action Controls */}
        <div className="mt-10 sm:mt-14 pt-6 border-t border-luma-primary/10 flex items-center justify-between">
          <div>
            {currentStep > 1 && (
              <Button
                variant="ghost"
                onClick={prevStep}
                className="text-xs sm:text-sm"
              >
                ← Back
              </Button>
            )}
          </div>

          <div>
            {currentStep < totalSteps ? (
              <Button
                onClick={nextStep}
                disabled={!canProceed()}
                size="md"
                className="shadow-md shadow-luma-accent/15"
              >
                Continue →
              </Button>
            ) : (
              <Button
                onClick={submitDiscovery}
                size="lg"
                className="shadow-lg shadow-luma-accent/20"
              >
                <span>Shape My Future World ✦</span>
              </Button>
            )}
          </div>
        </div>
      </div>
    </div>
    </ThemeProvider>
  );
};
