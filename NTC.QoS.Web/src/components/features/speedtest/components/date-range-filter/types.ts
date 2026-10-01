import { type DateRange } from "react-day-picker";

export interface DateRangeFilterProps {
  startDate?: string;
  endDate?: string;
  onChange?: (startDate?: string, endDate?: string) => void;
  className?: string;
  buttonClassName?: string;
  align?: "start" | "center" | "end";
  size?: "default" | "sm";
  compact?: boolean;
}

export interface PresetItem {
  label: string;
  getRange: () => DateRange;
}

export interface MonthItem {
  index: number;
  short: string;
  full: string;
}
