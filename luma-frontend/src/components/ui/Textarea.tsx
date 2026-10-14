"use client";

import React, { forwardRef } from "react";

export interface TextareaProps
  extends React.TextareaHTMLAttributes<HTMLTextAreaElement> {
  label?: string;
  helperText?: string;
  error?: string;
}

export const Textarea = forwardRef<HTMLTextAreaElement, TextareaProps>(
  ({ label, helperText, error, className = "", id, ...props }, ref) => {
    const inputId = id || (label ? label.toLowerCase().replace(/\s+/g, "-") : undefined);

    return (
      <div className="w-full flex flex-col gap-2">
        {label && (
          <label
            htmlFor={inputId}
            className="text-sm font-medium text-luma-primary flex items-center justify-between"
          >
            <span>{label}</span>
          </label>
        )}
        <div className="relative">
          <textarea
            ref={ref}
            id={inputId}
            className={`w-full rounded-2xl sm:rounded-3xl p-5 sm:p-6 bg-white/60 backdrop-blur-sm border border-luma-plum/15 text-luma-text placeholder:text-luma-text-secondary/50 focus-visible:outline-none focus-visible:border-luma-accent focus-visible:ring-2 focus-visible:ring-luma-accent/30 focus-visible:bg-white/90 transition-all duration-300 resize-none text-base sm:text-lg leading-relaxed shadow-sm ${
              error ? "border-rose-400 focus-visible:ring-rose-200" : ""
            } ${className}`}
            {...props}
          />
        </div>
        {helperText && !error && (
          <p className="text-xs sm:text-sm text-luma-text-secondary px-1">
            {helperText}
          </p>
        )}
        {error && (
          <p className="text-xs sm:text-sm text-rose-500 font-medium px-1">
            {error}
          </p>
        )}
      </div>
    );
  }
);

Textarea.displayName = "Textarea";
