"use client";

import React, { forwardRef } from "react";

export interface ButtonProps
  extends React.ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: "primary" | "secondary" | "ghost" | "subtle";
  size?: "sm" | "md" | "lg";
  isLoading?: boolean;
}

export const Button = forwardRef<HTMLButtonElement, ButtonProps>(
  (
    {
      children,
      className = "",
      variant = "primary",
      size = "md",
      isLoading = false,
      disabled,
      ...props
    },
    ref
  ) => {
    const baseStyles =
      "relative inline-flex items-center justify-center font-medium transition-all duration-300 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-luma-accent focus-visible:ring-offset-2 disabled:opacity-50 disabled:pointer-events-none active:scale-[0.98] select-none cursor-pointer";

    const sizeStyles = {
      sm: "text-xs px-3.5 py-1.5 rounded-full tracking-wide",
      md: "text-sm sm:text-base px-6 py-3 rounded-full tracking-wide",
      lg: "text-base sm:text-lg px-8 py-4 rounded-full tracking-wide shadow-sm",
    };

    const variantStyles = {
      primary:
        "bg-luma-primary text-luma-bg font-semibold hover:bg-opacity-95 hover:shadow-lg hover:shadow-luma-primary/20 hover:-translate-y-0.5 border border-luma-primary/10",
      secondary:
        "bg-luma-surface/60 backdrop-blur-md text-luma-primary border border-luma-primary/15 hover:border-luma-accent hover:bg-luma-surface/90 hover:shadow-md hover:shadow-luma-primary/10 hover:-translate-y-0.5",
      ghost:
        "bg-transparent text-luma-text-secondary hover:text-luma-primary hover:bg-luma-surface/40",
      subtle:
        "bg-luma-surface/50 text-luma-primary hover:bg-luma-surface/80 border border-transparent hover:border-luma-accent/30",
    };

    return (
      <button
        ref={ref}
        disabled={disabled || isLoading}
        className={`${baseStyles} ${sizeStyles[size]} ${variantStyles[variant]} ${className}`}
        {...props}
      >
        {isLoading ? (
          <span className="inline-flex items-center gap-2">
            <svg
              className="animate-spin h-4 w-4 text-current"
              xmlns="http://www.w3.org/2000/svg"
              fill="none"
              viewBox="0 0 24 24"
            >
              <circle
                className="opacity-25"
                cx="12"
                cy="12"
                r="10"
                stroke="currentColor"
                strokeWidth="4"
              />
              <path
                className="opacity-75"
                fill="currentColor"
                d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
              />
            </svg>
            <span>{children}</span>
          </span>
        ) : (
          children
        )}
      </button>
    );
  }
);

Button.displayName = "Button";
