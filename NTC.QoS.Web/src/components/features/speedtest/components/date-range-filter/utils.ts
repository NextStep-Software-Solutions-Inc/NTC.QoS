import {
  endOfDay,
  endOfMonth,
  endOfYear,
  format,
  isSameDay,
  isSameMonth,
  isSameYear,
  startOfDay,
  startOfMonth,
  startOfYear,
  subDays,
  subMonths,
  subYears,
} from "date-fns";
import { type DateRange } from "react-day-picker";
import { type MonthItem, type PresetItem } from "./types";

export const MONTHS: MonthItem[] = [
  { index: 0, short: "Jan", full: "January" },
  { index: 1, short: "Feb", full: "February" },
  { index: 2, short: "Mar", full: "March" },
  { index: 3, short: "Apr", full: "April" },
  { index: 4, short: "May", full: "May" },
  { index: 5, short: "Jun", full: "June" },
  { index: 6, short: "Jul", full: "July" },
  { index: 7, short: "Aug", full: "August" },
  { index: 8, short: "Sep", full: "September" },
  { index: 9, short: "Oct", full: "October" },
  { index: 10, short: "Nov", full: "November" },
  { index: 11, short: "Dec", full: "December" },
];

export function getPresets(): PresetItem[] {
  return [
    {
      label: "Today",
      getRange: () => {
        const now = new Date();
        return { from: startOfDay(now), to: endOfDay(now) };
      },
    },
    {
      label: "Yesterday",
      getRange: () => {
        const yesterday = subDays(new Date(), 1);
        return { from: startOfDay(yesterday), to: endOfDay(yesterday) };
      },
    },
    {
      label: "Last 7 Days",
      getRange: () => {
        const now = new Date();
        return { from: startOfDay(subDays(now, 6)), to: endOfDay(now) };
      },
    },
    {
      label: "Last 30 Days",
      getRange: () => {
        const now = new Date();
        return { from: startOfDay(subDays(now, 29)), to: endOfDay(now) };
      },
    },
    {
      label: "This Month",
      getRange: () => {
        const now = new Date();
        return { from: startOfMonth(now), to: endOfDay(now) };
      },
    },
    {
      label: "Last Month",
      getRange: () => {
        const lastMonth = subMonths(new Date(), 1);
        return { from: startOfMonth(lastMonth), to: endOfMonth(lastMonth) };
      },
    },
    {
      label: "This Year",
      getRange: () => {
        const now = new Date();
        return { from: startOfYear(now), to: endOfDay(now) };
      },
    },
    {
      label: "Last Year",
      getRange: () => {
        const lastYear = subYears(new Date(), 1);
        return { from: startOfYear(lastYear), to: endOfYear(lastYear) };
      },
    },
  ];
}

export function getAvailableYears(currentYear: number, parsedFromYear?: number): number[] {
  const list: number[] = [];
  const maxYear = Math.max(currentYear, parsedFromYear || currentYear);
  const minYear = Math.min(2020, parsedFromYear || 2020);
  for (let y = maxYear; y >= minYear; y--) {
    list.push(y);
  }
  return list;
}

export function formatTriggerLabel(parsedRange?: DateRange, compact = false): string {
  if (!parsedRange?.from) return "Date Range";

  const from = parsedRange.from;
  const to = parsedRange.to || from;

  // 1. Full Year (Jan 1 to Dec 31 of same year)
  if (
    from.getMonth() === 0 &&
    from.getDate() === 1 &&
    to.getMonth() === 11 &&
    to.getDate() === 31 &&
    from.getFullYear() === to.getFullYear()
  ) {
    return compact ? `${from.getFullYear()}` : `Year ${from.getFullYear()}`;
  }

  // 2. Full Month (1st of month to end of same month)
  if (
    from.getDate() === 1 &&
    isSameMonth(from, to) &&
    isSameDay(to, endOfMonth(to))
  ) {
    return compact ? format(from, "MMM yyyy") : format(from, "MMMM yyyy");
  }

  // 3. Same Day
  if (isSameDay(from, to)) {
    return format(from, compact ? "MMM d, yyyy" : "MMMM d, yyyy");
  }

  // 4. Same Month Range (e.g. Sep 1 – 15, 2026)
  if (isSameMonth(from, to)) {
    return `${format(from, "MMM d")} – ${format(to, "d, yyyy")}`;
  }

  // 5. Same Year Range (e.g. Aug 15 – Sep 20, 2026)
  if (isSameYear(from, to)) {
    return `${format(from, "MMM d")} – ${format(to, "MMM d, yyyy")}`;
  }

  // 6. Cross Year Range (e.g. Dec 15, 2025 – Jan 20, 2026)
  return `${format(from, "MMM d, yyyy")} – ${format(to, "MMM d, yyyy")}`;
}

export function convertRangeToUtcIso(range?: DateRange): { fromIso?: string; toIso?: string } {
  if (!range?.from) return {};
  const fromLocal = new Date(
    range.from.getFullYear(),
    range.from.getMonth(),
    range.from.getDate(),
    0,
    0,
    0,
    0,
  );
  const toDate = range.to || range.from;
  const toLocal = new Date(
    toDate.getFullYear(),
    toDate.getMonth(),
    toDate.getDate(),
    23,
    59,
    59,
    999,
  );
  return {
    fromIso: fromLocal.toISOString(),
    toIso: toLocal.toISOString(),
  };
}
