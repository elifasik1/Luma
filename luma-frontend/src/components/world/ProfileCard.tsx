"use client";

import React from "react";

export interface ProfileCardProps {
  title: string;
  subtitle?: string;
  icon?: string;
  children: React.ReactNode;
  className?: string;
}

export const ProfileCard: React.FC<ProfileCardProps> = ({
  title,
  subtitle,
  icon,
  children,
  className = "",
}) => {
  return (
    <div
      className={`relative p-5 sm:p-7 rounded-3xl bg-white/70 backdrop-blur-md border border-luma-plum/10 shadow-sm transition-all duration-300 hover:shadow-md hover:bg-white/90 hover:border-luma-plum/20 ${className}`}
    >
      <div className="flex items-center justify-between mb-3.5">
        <div className="flex items-center gap-2.5">
          {icon && (
            <span className="text-xl sm:text-2xl" aria-hidden="true">
              {icon}
            </span>
          )}
          <div>
            <h3 className="font-serif text-lg sm:text-xl text-luma-primary font-medium">
              {title}
            </h3>
            {subtitle && (
              <p className="text-xs text-luma-text-secondary">{subtitle}</p>
            )}
          </div>
        </div>
      </div>

      <div className="text-sm sm:text-base text-luma-text leading-relaxed">
        {children}
      </div>
    </div>
  );
};
