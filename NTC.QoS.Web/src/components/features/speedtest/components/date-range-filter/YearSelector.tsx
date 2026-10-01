"use client";

import React from "react";
import { Check } from "lucide-react";
import { type DateRange } from "react-day-picker";

import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";

interface YearSelectorProps {
  years: number[];
  onSelectYear: (year: number) => void;
  onClear: () => void;
  parsedRange?: DateRange;
}

export function YearSelector({
  years,
  onSelectYear,
  onClear,
  parsedRange,
}: YearSelectorProps) {
  return (
    <div className="p-4 min-w-[280px]">
      <div className="mb-3 px-1 text-xs text-gray-500 font-medium">
        Select a year to view all tests conducted in that year:
      </div>

      <div className="grid grid-cols-2 gap-2 max-h-[220px] overflow-y-auto p-1">
        {years.map((yr) => {
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
              onClick={() => onSelectYear(yr)}
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
          onClick={onClear}
          className="h-8 text-xs text-red-600 hover:text-red-700 hover:bg-red-50 dark:hover:bg-red-950/30">
          Clear Filter
        </Button>
      </div>
    </div>
  );
}
