"use client";

import React, { useCallback, useEffect, useMemo, useState } from "react";
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
import {
  Calendar as CalendarIcon,
  CalendarDays,
  Check,
  ChevronLeft,
  ChevronRight,
  Clock,
  X,
} from "lucide-react";
import { type DateRange } from "react-day-picker";

import { Button } from "@/components/ui/button";
import { Calendar } from "@/components/ui/calendar";
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { cn } from "@/lib/utils";

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

const MONTHS = [
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

export function DateRangeFilter({
  startDate,
  endDate,
  onChange,
  className,
  buttonClassName,
  align = "start",
  size = "default",
  compact = false,
}: DateRangeFilterProps) {
  const [isOpen, setIsOpen] = useState(false);

  // Parse incoming ISO strings into local DateRange
  const parsedRange = useMemo<DateRange | undefined>(() => {
    if (!startDate && !endDate) return undefined;
    const from = startDate ? new Date(startDate) : undefined;
    const to = endDate ? new Date(endDate) : undefined;
    if (from && isNaN(from.getTime())) return undefined;
    if (to && isNaN(to.getTime())) return undefined;
    return { from, to: to || from };
  }, [startDate, endDate]);

  // Staged date range for popover selection in custom range mode
  const [stagedRange, setStagedRange] = useState<DateRange | undefined>(parsedRange);

  // Active tab state ("range" | "month" | "year")
  const [activeTab, setActiveTab] = useState<string>("range");

  // State for Month picker navigation
  const [monthViewYear, setMonthViewYear] = useState<number>(() => {
    return parsedRange?.from?.getFullYear() || new Date().getFullYear();
  });

  // Automatically determine best initial tab when popover opens
  useEffect(() => {
    setStagedRange(parsedRange);

    if (isOpen) {
      if (parsedRange?.from) {
        const from = parsedRange.from;
        const to = parsedRange.to || from;

        // Is full year?
        if (
          from.getMonth() === 0 &&
          from.getDate() === 1 &&
          to.getMonth() === 11 &&
          to.getDate() === 31 &&
          from.getFullYear() === to.getFullYear()
        ) {
          setActiveTab("year");
          setMonthViewYear(from.getFullYear());
          return;
        }

        // Is full month?
        if (
          from.getDate() === 1 &&
          isSameMonth(from, to) &&
          isSameDay(to, endOfMonth(to))
        ) {
          setActiveTab("month");
          setMonthViewYear(from.getFullYear());
          return;
        }
      }
      setMonthViewYear(parsedRange?.from?.getFullYear() || new Date().getFullYear());
    }
  }, [parsedRange, isOpen]);

  // Preset ranges for quick picking in range mode
  const presets = useMemo(
    () => [
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
    ],
    [],
  );

  const applyRange = useCallback(
    (range?: DateRange) => {
      if (!range?.from) {
        onChange?.(undefined, undefined);
        setIsOpen(false);
        return;
      }

      // Convert local date bounds to UTC ISO strings
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

      onChange?.(fromLocal.toISOString(), toLocal.toISOString());
      setIsOpen(false);
    },
    [onChange],
  );

  const handleClear = useCallback(
    (e?: React.MouseEvent) => {
      e?.stopPropagation();
      setStagedRange(undefined);
      onChange?.(undefined, undefined);
      setIsOpen(false);
    },
    [onChange],
  );

  // Month selection handler
  const handleSelectMonth = useCallback(
    (monthIndex: number) => {
      const from = startOfMonth(new Date(monthViewYear, monthIndex, 1));
      const to = endOfMonth(new Date(monthViewYear, monthIndex, 1));
      applyRange({ from, to });
    },
    [monthViewYear, applyRange],
  );

  // Year selection handler
  const handleSelectYear = useCallback(
    (year: number) => {
      const from = startOfYear(new Date(year, 0, 1));
      const to = endOfYear(new Date(year, 11, 31));
      applyRange({ from, to });
    },
    [applyRange],
  );

  const isFilterActive = Boolean(startDate || endDate);

  // Dynamic available years list
  const currentYear = new Date().getFullYear();
  const availableYears = useMemo(() => {
    const list: number[] = [];
    const maxYear = Math.max(currentYear, parsedRange?.from?.getFullYear() || currentYear);
    const minYear = Math.min(2020, parsedRange?.from?.getFullYear() || 2020);
    for (let y = maxYear; y >= minYear; y--) {
      list.push(y);
    }
    return list;
  }, [currentYear, parsedRange]);

  // Clean, professional formatting for trigger button label
  const triggerLabel = useMemo(() => {
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
  }, [parsedRange, compact]);

  const isSmall = size === "sm";

  return (
    <div className={cn("inline-flex items-center", className)}>
      <Popover open={isOpen} onOpenChange={setIsOpen}>
        <PopoverTrigger asChild>
          <Button
            variant={isFilterActive ? "secondary" : "outline"}
            size={isSmall ? "sm" : "default"}
            title={
              isFilterActive
                ? `Filter: ${triggerLabel} (Click to change, X to clear)`
                : "Filter by date, month, or year"
            }
            className={cn(
              "font-medium text-left transition-all flex items-center gap-1.5 shadow-sm",
              isSmall ? "h-7 text-xs px-2.5" : "h-9 text-sm px-3",
              isFilterActive
                ? "bg-blue-50 text-blue-700 border-blue-300 hover:bg-blue-100 hover:text-blue-800 dark:bg-blue-950/50 dark:text-blue-300 dark:border-blue-700"
                : "border-gray-200 dark:border-gray-700 hover:bg-gray-100 dark:hover:bg-gray-800 text-gray-700 dark:text-gray-200",
              buttonClassName,
            )}>
            <CalendarIcon
              className={cn(
                "flex-shrink-0",
                isSmall ? "h-3.5 w-3.5 text-blue-600 dark:text-blue-400" : "h-4 w-4 text-blue-600 dark:text-blue-400",
              )}
            />
            <span className="truncate max-w-[200px]">{triggerLabel}</span>
            {isFilterActive && (
              <span
                role="button"
                tabIndex={0}
                aria-label="Clear date range"
                onClick={handleClear}
                onKeyDown={(e) => {
                  if (e.key === "Enter" || e.key === " ") {
                    handleClear();
                  }
                }}
                className="ml-1 p-0.5 rounded-full hover:bg-blue-200/70 dark:hover:bg-blue-800/70 transition-colors">
                <X className={cn("text-blue-600 dark:text-blue-300", isSmall ? "h-3 w-3" : "h-3.5 w-3.5")} />
              </span>
            )}
          </Button>
        </PopoverTrigger>

        <PopoverContent
          align={align}
          className="w-auto p-0 shadow-2xl border border-gray-200 dark:border-gray-700 rounded-xl bg-white dark:bg-gray-900 overflow-hidden"
          sideOffset={8}>
          {/* Header Mode Switcher Tabs */}
          <Tabs value={activeTab} onValueChange={setActiveTab} className="w-full">
            <div className="border-b border-gray-200 dark:border-gray-800 bg-gray-50/70 dark:bg-gray-900/70 p-2">
              <TabsList className="grid grid-cols-3 w-full h-8 text-xs bg-gray-200/70 dark:bg-gray-800">
                <TabsTrigger value="range" className="text-xs py-1 font-medium">
                  Custom Range
                </TabsTrigger>
                <TabsTrigger value="month" className="text-xs py-1 font-medium">
                  Monthly
                </TabsTrigger>
                <TabsTrigger value="year" className="text-xs py-1 font-medium">
                  Yearly
                </TabsTrigger>
              </TabsList>
            </div>

            {/* TAB 1: Custom Range with Calendar and Presets */}
            <TabsContent value="range" className="m-0 p-0 outline-none">
              <div className="flex flex-col sm:flex-row">
                {/* Presets Sidebar */}
                <div className="border-b sm:border-b-0 sm:border-r border-gray-200 dark:border-gray-800 p-2.5 flex flex-row sm:flex-col gap-1 overflow-x-auto sm:overflow-visible min-w-[130px] bg-gray-50/40 dark:bg-gray-900/40">
                  <span className="text-[10px] font-semibold text-gray-400 uppercase tracking-wider px-2 py-1 hidden sm:block">
                    Presets
                  </span>
                  {presets.map((preset) => (
                    <Button
                      key={preset.label}
                      variant="ghost"
                      size="sm"
                      className="justify-start text-xs font-normal h-7 px-2 hover:bg-blue-50 hover:text-blue-600 dark:hover:bg-blue-950/30 dark:hover:text-blue-400"
                      onClick={() => {
                        const r = preset.getRange();
                        setStagedRange(r);
                        applyRange(r);
                      }}>
                      {preset.label}
                    </Button>
                  ))}
                </div>

                {/* Calendar View */}
                <div className="p-3">
                  <Calendar
                    initialFocus
                    mode="range"
                    defaultMonth={stagedRange?.from || new Date()}
                    selected={stagedRange}
                    onSelect={setStagedRange}
                    numberOfMonths={1}
                    className="rounded-md"
                  />

                  {/* Actions Footer */}
                  <div className="flex items-center justify-between pt-3 mt-2 border-t border-gray-100 dark:border-gray-800">
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={() => handleClear()}
                      className="h-8 text-xs text-red-600 hover:text-red-700 hover:bg-red-50 dark:hover:bg-red-950/30">
                      Reset
                    </Button>
                    <div className="flex items-center gap-2">
                      <Button
                        variant="outline"
                        size="sm"
                        className="h-8 text-xs"
                        onClick={() => setIsOpen(false)}>
                        Cancel
                      </Button>
                      <Button
                        size="sm"
                        className="h-8 text-xs bg-blue-600 text-white hover:bg-blue-700 shadow-sm"
                        disabled={!stagedRange?.from}
                        onClick={() => applyRange(stagedRange)}>
                        Apply
                      </Button>
                    </div>
                  </div>
                </div>
              </div>
            </TabsContent>

            {/* TAB 2: Monthly Filter */}
            <TabsContent value="month" className="m-0 p-4 outline-none min-w-[300px]">
              {/* Year Navigation for Month Selection */}
              <div className="flex items-center justify-between mb-4 px-1">
                <Button
                  variant="outline"
                  size="sm"
                  className="h-7 w-7 p-0"
                  onClick={() => setMonthViewYear((y) => y - 1)}>
                  <ChevronLeft className="h-4 w-4" />
                </Button>
                <span className="text-sm font-semibold text-gray-800 dark:text-gray-200">
                  {monthViewYear}
                </span>
                <Button
                  variant="outline"
                  size="sm"
                  className="h-7 w-7 p-0"
                  onClick={() => setMonthViewYear((y) => y + 1)}>
                  <ChevronRight className="h-4 w-4" />
                </Button>
              </div>

              {/* 12-Month Grid */}
              <div className="grid grid-cols-3 gap-2">
                {MONTHS.map((m) => {
                  const isCurrentActive =
                    parsedRange?.from &&
                    parsedRange.from.getFullYear() === monthViewYear &&
                    parsedRange.from.getMonth() === m.index &&
                    parsedRange.from.getDate() === 1 &&
                    parsedRange.to &&
                    isSameDay(parsedRange.to, endOfMonth(parsedRange.to));

                  return (
                    <Button
                      key={m.short}
                      variant={isCurrentActive ? "default" : "outline"}
                      size="sm"
                      onClick={() => handleSelectMonth(m.index)}
                      className={cn(
                        "h-10 text-xs font-medium justify-center transition-all",
                        isCurrentActive
                          ? "bg-blue-600 text-white shadow-sm"
                          : "hover:bg-blue-50 hover:text-blue-600 dark:hover:bg-blue-950/30 dark:hover:text-blue-300",
                      )}>
                      {m.short}
                      {isCurrentActive && <Check className="ml-1 h-3.5 w-3.5" />}
                    </Button>
                  );
                })}
              </div>

              {/* Entire Year Shortcut */}
              <div className="mt-4 pt-3 border-t border-gray-100 dark:border-gray-800 flex justify-between items-center">
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={() => handleClear()}
                  className="h-8 text-xs text-red-600 hover:text-red-700 hover:bg-red-50 dark:hover:bg-red-950/30">
                  Clear Filter
                </Button>
                <Button
                  variant="secondary"
                  size="sm"
                  onClick={() => handleSelectYear(monthViewYear)}
                  className="h-8 text-xs text-blue-700 bg-blue-50 hover:bg-blue-100 dark:bg-blue-950/40 dark:text-blue-300 font-medium">
                  All of {monthViewYear}
                </Button>
              </div>
            </TabsContent>

            {/* TAB 3: Yearly Filter */}
            <TabsContent value="year" className="m-0 p-4 outline-none min-w-[280px]">
              <div className="mb-3 px-1 text-xs text-gray-500 font-medium">
                Select a year to view all tests conducted in that year:
              </div>

              <div className="grid grid-cols-2 gap-2 max-h-[220px] overflow-y-auto p-1">
                {availableYears.map((yr) => {
                  const isCurrentActive =
                    parsedRange?.from &&
                    parsedRange.from.getFullYear() === yr &&
                    parsedRange.from.getMonth() === 0 &&
                    parsedRange.from.getDate() === 1 &&
                    parsedRange.to &&
                    parsedRange.to.getFullYear() === yr &&
                    parsedRange.to.getMonth() === 11 &&
                    parsedRange.to.getDate() === 31;

                  return (
                    <Button
                      key={yr}
                      variant={isCurrentActive ? "default" : "outline"}
                      size="sm"
                      onClick={() => handleSelectYear(yr)}
                      className={cn(
                        "h-10 text-xs font-semibold justify-center transition-all",
                        isCurrentActive
                          ? "bg-blue-600 text-white shadow-sm"
                          : "hover:bg-blue-50 hover:text-blue-600 dark:hover:bg-blue-950/30 dark:hover:text-blue-300",
                      )}>
                      {yr}
                      {isCurrentActive && <Check className="ml-1.5 h-3.5 w-3.5" />}
                    </Button>
                  );
                })}
              </div>

              <div className="mt-3 pt-3 border-t border-gray-100 dark:border-gray-800 flex justify-end">
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={() => handleClear()}
                  className="h-8 text-xs text-red-600 hover:text-red-700 hover:bg-red-50 dark:hover:bg-red-950/30">
                  Clear Filter
                </Button>
              </div>
            </TabsContent>
          </Tabs>
        </PopoverContent>
      </Popover>
    </div>
  );
}

export default DateRangeFilter;
