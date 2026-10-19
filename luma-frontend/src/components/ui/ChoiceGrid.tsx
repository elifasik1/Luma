"use client";

export interface ChoiceOption<T extends string = string> {
  id: T;
  label: string;
  description?: string;
  icon?: string;
}

export interface ChoiceGridProps<T extends string = string> {
  options: ChoiceOption<T>[];
  selectedIds: T[];
  onChange: (selectedIds: T[]) => void;
  multiSelect?: boolean;
  columns?: 2 | 3 | 4;
}

export function ChoiceGrid<T extends string = string>({
  options,
  selectedIds,
  onChange,
  multiSelect = true,
  columns = 3,
}: ChoiceGridProps<T>) {
  const toggleOption = (id: T) => {
    if (multiSelect) {
      if (selectedIds.includes(id)) {
        onChange(selectedIds.filter((item) => item !== id));
      } else {
        onChange([...selectedIds, id]);
      }
    } else {
      onChange(selectedIds.includes(id) ? [] : [id]);
    }
  };

  const colClasses = {
    2: "grid-cols-1 sm:grid-cols-2",
    3: "grid-cols-1 sm:grid-cols-2 md:grid-cols-3",
    4: "grid-cols-2 sm:grid-cols-3 md:grid-cols-4",
  };

  return (
    <div
      role={multiSelect ? "group" : "radiogroup"}
      className={`grid gap-3.5 sm:gap-4 w-full ${colClasses[columns]}`}
    >
      {options.map((option) => {
        const isSelected = selectedIds.includes(option.id);
        return (
          <button
            key={option.id}
            type="button"
            role={multiSelect ? "checkbox" : "radio"}
            aria-checked={isSelected}
            tabIndex={0}
            onClick={() => toggleOption(option.id)}
            onKeyDown={(e) => {
              if (e.key === " " || e.key === "Enter") {
                e.preventDefault();
                toggleOption(option.id);
              }
            }}
            className={`group relative flex flex-col items-start text-left p-4 sm:p-5 rounded-2xl sm:rounded-3xl transition-all duration-200 border focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-luma-accent focus-visible:ring-offset-2 select-none cursor-pointer touch-manipulation active:scale-[0.99] ${
              isSelected
                ? "bg-luma-surface/90 shadow-md shadow-luma-primary/15 border-luma-primary ring-2 ring-luma-primary/30 -translate-y-0.5"
                : "bg-luma-bg-secondary/60 hover:bg-luma-surface/40 border-luma-primary/15 hover:border-luma-primary/30 hover:shadow-sm"
            }`}
          >
            {/* Subtle glow border when selected */}
            {isSelected && (
              <div
                className="absolute inset-0 rounded-2xl sm:rounded-3xl pointer-events-none opacity-40 bg-gradient-to-br from-luma-primary/10 via-luma-accent/20 to-transparent"
                aria-hidden="true"
              />
            )}

            <div className="flex items-center justify-between w-full mb-1">
              <div className="flex items-center gap-2">
                {option.icon && (
                  <span className="text-xl sm:text-2xl" aria-hidden="true">
                    {option.icon}
                  </span>
                )}
                <span
                  className={`font-medium text-base sm:text-lg transition-colors ${
                    isSelected
                      ? "text-luma-primary font-bold"
                      : "text-luma-text group-hover:text-luma-primary"
                  }`}
                >
                  {option.label}
                </span>
              </div>

              {/* Indicator badge */}
              <div
                className={`w-5 h-5 rounded-full flex items-center justify-center transition-all duration-200 shrink-0 ${
                  isSelected
                    ? "bg-luma-primary text-luma-bg ring-2 ring-luma-primary/20 scale-100 shadow-sm"
                    : "border-2 border-luma-primary/30 group-hover:border-luma-primary/50 group-hover:scale-95"
                }`}
                aria-hidden="true"
              >
                {isSelected && (
                  <svg
                    className="w-3.5 h-3.5 stroke-current stroke-[3]"
                    fill="none"
                    viewBox="0 0 24 24"
                  >
                    <path
                      strokeLinecap="round"
                      strokeLinejoin="round"
                      d="M5 13l4 4L19 7"
                    />
                  </svg>
                )}
              </div>
            </div>

            {option.description && (
              <p className="text-xs sm:text-sm text-luma-text-secondary mt-1 line-clamp-2 leading-relaxed">
                {option.description}
              </p>
            )}
          </button>
        );
      })}
    </div>
  );
}
