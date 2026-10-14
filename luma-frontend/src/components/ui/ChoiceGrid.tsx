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
            onClick={() => toggleOption(option.id)}
            className={`group relative flex flex-col items-start text-left p-4 sm:p-5 rounded-2xl sm:rounded-3xl transition-all duration-300 border focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-luma-accent focus-visible:ring-offset-2 select-none cursor-pointer ${
              isSelected
  ? "bg-luma-surface/70 shadow-md shadow-luma-primary/10 border-luma-accent translate-y-[-2px]"
  : "bg-luma-bg-secondary/50 hover:bg-luma-surface/35 border-luma-primary/10 hover:border-luma-primary/25 hover:shadow-sm"
            }`}
          >
            {/* Subtle glow border when selected */}
            {isSelected && (
              <div
className="absolute inset-0 rounded-2xl sm:rounded-3xl pointer-events-none opacity-50 bg-gradient-to-br from-luma-accent/30 via-luma-surface/30 to-transparent"                aria-hidden="true"
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
                      ? "text-luma-primary font-semibold"
                      : "text-luma-text group-hover:text-luma-primary"
                  }`}
                >
                  {option.label}
                </span>
              </div>

              {/* Indicator badge */}
              <div
                className={`w-5 h-5 rounded-full flex items-center justify-center transition-all duration-300 ${
                  isSelected
                    ? "bg-luma-primary text-luma-bg scale-100"
                    : "border border-luma-primary/20 opacity-40 group-hover:opacity-70 scale-90"
                }`}
                aria-hidden="true"
              >
                {isSelected && (
                  <svg
                    className="w-3 h-3 stroke-current stroke-[2.5]"
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
