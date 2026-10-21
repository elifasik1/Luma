export function localDateIso(date = new Date()): string {
  return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, "0")}-${String(date.getDate()).padStart(2, "0")}`;
}

export function isValidIsoDate(value: string): boolean {
  const match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(value);
  if (!match) return false;

  const year = Number(match[1]);
  const month = Number(match[2]);
  const day = Number(match[3]);
  const date = new Date(year, month - 1, day);
  return date.getFullYear() === year && date.getMonth() === month - 1 && date.getDate() === day;
}

export function isTodayOrLater(value: string): boolean {
  return isValidIsoDate(value) && value >= localDateIso();
}

export function formatDateDisplay(value: string): string {
  if (!isValidIsoDate(value)) return "—";
  const [year, month, day] = value.split("-");
  return `${day}/${month}/${year}`;
}
