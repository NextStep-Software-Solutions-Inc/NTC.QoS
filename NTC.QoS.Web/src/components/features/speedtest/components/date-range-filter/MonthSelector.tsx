"use client";

import React from "react";
import { endOfMonth, isSameDay } from "date-fns";
import { Check, ChevronLeft, ChevronRight } from "lucide-react";
import { type DateRange } from "react-day-picker";

import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";
import { MONTHS } from "./utils";

interface MonthSelectorProps {
  year: number;
  onYearChange: (year: number) => void;
  onSelectMonth: (monthIndex: number) => void;
  onSelectEntireYear: (year: number) => void;
  onClear: () => void;
  parsedRange?: DateRange;
}

export function MonthSelector({
  year,
  onYearChange,
  onSelectMonth,
  onSelectEntireYear,
  onClear,
  parsedRange,
}: MonthSelectorProps) {
  return (
    <div className="p-4 min-w-[300px]">
      {/* Year Navigation for Month Selection */}
      <div className="flex items-center justify-between mb-4 px-1">
        <Button
          variant="outline"
          size="sm"
          className="h-7 w-7 p-0"
          onClick={() => onYearChange(year - 1)}>
          <ChevronLeft className="h-4 w-4" />
        </Button>
        <span className="text-sm font-semibold text-gray-800 dark:text-gray-200">
          {year}
        </span>
        <Button
          variant="outline"
          size="sm"
          className="h-7 w-7 p-0"
          onClick={() => onYearChange(year + 1)}>
          <ChevronRight className="h-4 w-4" />
        </Button>
      </div>

      {/* 12-Month Grid */}
      <div className="grid grid-cols-3 gap-2">
        {MONTHS.map((m) => {
          const isCurrentActive =
            parsedRange?.from &&
            parsedRange.from.getFullYear() === year &&
            parsedRange.from.getMonth() === m.index &&
            parsedRange.from.getDate() === 1 &&
            parsedRange.to &&
            isSameDay(parsedRange.to, endOfMonth(parsedRange.to));

          return (
            <Button
              key={m.short}
              variant={isCurrentActive ? "default" : "outline"}
              size="sm"
              onClick={() => onSelectMonth(m.index)}
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

      {/* Entire Year Shortcut and Clear */}
      <div className="mt-4 pt-3 border-t border-gray-100 dark:border-gray-800 flex justify-between items-center">
        <Button
          variant="ghost"
          size="sm"
          onClick={onClear}
          className="h-8 text-xs text-red-600 hover:text-red-700 hover:bg-red-50 dark:hover:bg-red-950/30">
          Clear Filter
        </Button>
        <Button
          variant="secondary"
          size="sm"
          onClick={() => onSelectEntireYear(year)}
          className="h-8 text-xs text-blue-700 bg-blue-50 hover:bg-blue-100 dark:bg-blue-950/40 dark:text-blue-300 font-medium">
          All of {year}
        </Button>
      </div>
    </div>
  );
}
