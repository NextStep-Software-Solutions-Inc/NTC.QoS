"use client";

import { Loader2 } from "lucide-react";

import { Checkbox } from "@/components/ui/checkbox";

interface StatsBarProps {
  currentPage: number;
  pageSize: number;
  totalCount: number;
  selectedCount: number;
  isLoading?: boolean;
  onToggleSelectAll: () => void;
  dataLength: number;
}

export function StatsBar({
  currentPage,
  pageSize,
  totalCount,
  selectedCount,
  isLoading = false,
  onToggleSelectAll,
  dataLength,
}: StatsBarProps) {
  return (
    <div className="flex items-center justify-between text-sm text-gray-600 dark:text-gray-400 mt-4">
      <div className="flex items-center gap-4">
        <span>
          Showing{" "}
          <span className="font-semibold text-gray-900 dark:text-white">
            {(currentPage - 1) * pageSize + 1}-{Math.min(currentPage * pageSize, totalCount)}
          </span>{" "}
          of <span className="font-semibold text-gray-900 dark:text-white">{totalCount.toLocaleString()}</span> cell
          sites
        </span>
        {selectedCount > 0 && (
          <span className="text-blue-600 dark:text-blue-400 font-medium">{selectedCount} selected</span>
        )}
        {isLoading && (
          <span className="flex items-center gap-2 text-blue-600 dark:text-blue-400">
            <Loader2 className="h-4 w-4 animate-spin" />
            Loading...
          </span>
        )}
      </div>
      <Checkbox
        checked={selectedCount === dataLength && dataLength > 0}
        onCheckedChange={onToggleSelectAll}
        className="mr-2"
      />
    </div>
  );
}
