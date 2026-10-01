"use client";

import React, { useCallback, useEffect, useMemo, useState } from "react";
import { endOfMonth, isSameDay, isSameMonth, startOfMonth, startOfYear, endOfYear } from "date-fns";
import { Calendar as CalendarIcon, X } from "lucide-react";
import { type DateRange } from "react-day-picker";

import { Button } from "@/components/ui/button";
import { Calendar } from "@/components/ui/calendar";
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { cn } from "@/lib/utils";

import { MonthSelector } from "./MonthSelector";
import { PresetList } from "./PresetList";
import { type DateRangeFilterProps } from "./types";
import {
  convertRangeToUtcIso,
  formatTriggerLabel,
  getAvailableYears,
  getPresets,
} from "./utils";
import { YearSelector } from "./YearSelector";

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

  // Quick preset ranges
  const presets = useMemo(() => getPresets(), []);

  // Dynamic available years list
  const currentYear = new Date().getFullYear();
  const availableYears = useMemo(() => {
    return getAvailableYears(currentYear, parsedRange?.from?.getFullYear());
  }, [currentYear, parsedRange]);

  const applyRange = useCallback(
    (range?: DateRange) => {
      if (!range?.from) {
        onChange?.(undefined, undefined);
        setIsOpen(false);
        return;
      }

      const { fromIso, toIso } = convertRangeToUtcIso(range);
      onChange?.(fromIso, toIso);
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

  const handleSelectMonth = useCallback(
    (monthIndex: number) => {
      const from = startOfMonth(new Date(monthViewYear, monthIndex, 1));
      const to = endOfMonth(new Date(monthViewYear, monthIndex, 1));
      applyRange({ from, to });
    },
    [monthViewYear, applyRange],
  );

  const handleSelectYear = useCallback(
    (year: number) => {
      const from = startOfYear(new Date(year, 0, 1));
      const to = endOfYear(new Date(year, 11, 31));
      applyRange({ from, to });
    },
    [applyRange],
  );

  const isFilterActive = Boolean(startDate || endDate);
  const triggerLabel = useMemo(() => formatTriggerLabel(parsedRange, compact), [parsedRange, compact]);
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
          <Tabs value={activeTab} onValueChange={setActiveTab} className="w-full">
            {/* Header Mode Switcher Tabs */}
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
                <PresetList
                  presets={presets}
                  onSelectPreset={(r) => {
                    setStagedRange(r);
                    applyRange(r);
                  }}
                />

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
            <TabsContent value="month" className="m-0 outline-none">
              <MonthSelector
                year={monthViewYear}
                onYearChange={setMonthViewYear}
                onSelectMonth={handleSelectMonth}
                onSelectEntireYear={handleSelectYear}
                onClear={handleClear}
                parsedRange={parsedRange}
              />
            </TabsContent>

            {/* TAB 3: Yearly Filter */}
            <TabsContent value="year" className="m-0 outline-none">
              <YearSelector
                years={availableYears}
                onSelectYear={handleSelectYear}
                onClear={handleClear}
                parsedRange={parsedRange}
              />
            </TabsContent>
          </Tabs>
        </PopoverContent>
      </Popover>
    </div>
  );
}

export default DateRangeFilter;
