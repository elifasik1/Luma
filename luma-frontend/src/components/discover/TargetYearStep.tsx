"use client";

import React, { useState, useCallback, useMemo } from "react";

interface TargetYearStepProps {
  /** DD/MM/YYYY string or empty */
  value: string;
  onChange: (value: string) => void;
}

/** Parse a DD/MM/YYYY string into { day, month, year } or null on invalid. */
function parseDDMMYYYY(raw: string): { day: number; month: number; year: number } | null {
  const match = raw.match(/^(\d{1,2})\/(\d{1,2})\/(\d{4})$/);
  if (!match) return null;
  const day = Number(match[1]);
  const month = Number(match[2]);
  const year = Number(match[3]);
  if (month < 1 || month > 12 || day < 1 || year < 1) return null;
  // Validate day count for the given month/year (handles leap years)
  const maxDay = new Date(year, month, 0).getDate();
  if (day > maxDay) return null;
  return { day, month, year };
}

/** Format day/month/year into DD/MM/YYYY */
function formatDDMMYYYY(day: number, month: number, year: number): string {
  return `${String(day).padStart(2, "0")}/${String(month).padStart(2, "0")}/${year}`;
}

/** Get today at midnight local as { day, month, year } */
function todayParts() {
  const now = new Date();
  return { day: now.getDate(), month: now.getMonth() + 1, year: now.getFullYear() };
}

/** Check if a date is today or in the future */
function isNotPast(day: number, month: number, year: number): boolean {
  const t = todayParts();
  const input = new Date(year, month - 1, day);
  const today = new Date(t.year, t.month - 1, t.day);
  return input >= today;
}

const MONTHS = [
  "January", "February", "March", "April", "May", "June",
  "July", "August", "September", "October", "November", "December",
];

export const TargetYearStep: React.FC<TargetYearStepProps> = ({ value, onChange }) => {
  const today = useMemo(todayParts, []);

  // Parse the incoming value, defaulting to today
  const parsed = useMemo(() => parseDDMMYYYY(value), [value]);
  const selectedDay = parsed?.day ?? today.day;
  const selectedMonth = parsed?.month ?? today.month;
  const selectedYear = parsed?.year ?? today.year;

  const [error, setError] = useState<string | null>(null);

  // Commit changes from dropdowns
  const commit = useCallback(
    (day: number, month: number, year: number) => {
      // Clamp day to max for the given month/year
      const maxDay = new Date(year, month, 0).getDate();
      const clampedDay = Math.min(day, maxDay);

      if (!isNotPast(clampedDay, month, year)) {
        setError("The date must be today or in the future.");
        return;
      }
      setError(null);
      onChange(formatDDMMYYYY(clampedDay, month, year));
    },
    [onChange],
  );

  const handleDayChange = (e: React.ChangeEvent<HTMLSelectElement>) =>
    commit(Number(e.target.value), selectedMonth, selectedYear);

  const handleMonthChange = (e: React.ChangeEvent<HTMLSelectElement>) =>
    commit(selectedDay, Number(e.target.value), selectedYear);

  const handleYearChange = (e: React.ChangeEvent<HTMLSelectElement>) =>
    commit(selectedDay, selectedMonth, Number(e.target.value));

  // Max day for current selection
  const maxDay = new Date(selectedYear, selectedMonth, 0).getDate();

  // Year range: current year → current year + 30
  const yearOptions = useMemo(() => {
    const years: number[] = [];
    for (let y = today.year; y <= today.year + 30; y++) years.push(y);
    return years;
  }, [today.year]);

  return (
    <section className="mx-auto max-w-xl text-center" aria-labelledby="target-year-title">
      <p className="mb-3 text-xs font-semibold uppercase tracking-[0.24em] text-luma-text-secondary">Your horizon</p>
      <h1 id="target-year-title" className="font-serif text-4xl leading-tight text-luma-primary sm:text-5xl">When are you imagining this future?</h1>
      <p className="mx-auto mt-5 max-w-md text-base leading-7 text-luma-text-secondary">
        Choose the date that gives your vision a sense of direction. It must be today or any day ahead.
      </p>

      <div className="mx-auto mt-10 max-w-sm">
        <span className="mb-2 block text-sm font-semibold text-luma-primary">Target date</span>
        <div className="flex items-center gap-2">
          {/* Day */}
          <label className="flex-1">
            <span className="mb-1 block text-[10px] font-semibold uppercase tracking-wide text-luma-text-secondary">Day</span>
            <select
              value={selectedDay}
              onChange={handleDayChange}
              className="min-h-12 w-full cursor-pointer appearance-none rounded-2xl border border-luma-primary/20 bg-luma-bg px-3 text-center text-lg text-luma-primary outline-none transition focus:border-luma-primary focus:ring-2 focus:ring-luma-primary/20"
              aria-label="Day"
            >
              {Array.from({ length: maxDay }, (_, i) => i + 1).map((d) => (
                <option key={d} value={d}>{String(d).padStart(2, "0")}</option>
              ))}
            </select>
          </label>

          <span className="mt-5 text-lg text-luma-text-secondary/50">/</span>

          {/* Month */}
          <label className="flex-[1.4]">
            <span className="mb-1 block text-[10px] font-semibold uppercase tracking-wide text-luma-text-secondary">Month</span>
            <select
              value={selectedMonth}
              onChange={handleMonthChange}
              className="min-h-12 w-full cursor-pointer appearance-none rounded-2xl border border-luma-primary/20 bg-luma-bg px-3 text-center text-lg text-luma-primary outline-none transition focus:border-luma-primary focus:ring-2 focus:ring-luma-primary/20"
              aria-label="Month"
            >
              {MONTHS.map((name, i) => (
                <option key={i + 1} value={i + 1}>{name}</option>
              ))}
            </select>
          </label>

          <span className="mt-5 text-lg text-luma-text-secondary/50">/</span>

          {/* Year */}
          <label className="flex-1">
            <span className="mb-1 block text-[10px] font-semibold uppercase tracking-wide text-luma-text-secondary">Year</span>
            <select
              value={selectedYear}
              onChange={handleYearChange}
              className="min-h-12 w-full cursor-pointer appearance-none rounded-2xl border border-luma-primary/20 bg-luma-bg px-3 text-center text-lg text-luma-primary outline-none transition focus:border-luma-primary focus:ring-2 focus:ring-luma-primary/20"
              aria-label="Year"
            >
              {yearOptions.map((y) => (
                <option key={y} value={y}>{y}</option>
              ))}
            </select>
          </label>
        </div>

        {/* Formatted preview */}
        <p className="mt-3 text-sm font-medium text-luma-primary" aria-live="polite">
          {formatDDMMYYYY(Math.min(selectedDay, maxDay), selectedMonth, selectedYear)}
        </p>

        {error && (
          <p className="mt-2 text-xs text-rose-600" role="alert">{error}</p>
        )}

        <span id="target-year-help" className="mt-2 block text-xs text-luma-text-secondary">
          The date must be today ({formatDDMMYYYY(today.day, today.month, today.year)}) or later.
        </span>
      </div>
    </section>
  );
};
