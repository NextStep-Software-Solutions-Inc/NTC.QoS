"use client";

import { CheckCircle2, Database, Loader2 } from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Checkbox } from "@/components/ui/checkbox";
import { cn } from "@/lib/utils";

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
  const startItem = (currentPage - 1) * pageSize + 1;
  const endItem = Math.min(currentPage * pageSize, totalCount);

  return (
    <div className="flex items-center justify-between text-sm mt-4 py-2 px-3 rounded-lg bg-gradient-to-r from-gray-50 to-gray-100/50 dark:from-gray-800/50 dark:to-gray-900/50 border border-gray-200/50 dark:border-gray-700/50">
      <div className="flex items-center gap-3">
        <div className="flex items-center gap-2">
          <div className="flex items-center justify-center h-6 w-6 rounded-md bg-gradient-to-br from-blue-500 to-blue-600 shadow-sm">
            <Database className="h-3 w-3 text-white" />
          </div>
          <span className="text-gray-600 dark:text-gray-400">
            Showing{" "}
            <span className="font-bold text-gray-900 dark:text-white">
              {startItem.toLocaleString()}-{endItem.toLocaleString()}
            </span>{" "}
            of{" "}
            <span className="font-bold bg-gradient-to-r from-blue-600 to-purple-600 bg-clip-text text-transparent">
              {totalCount.toLocaleString()}
            </span>{" "}
            NAPs
          </span>
        </div>

        {selectedCount > 0 && (
          <Badge
            variant="secondary"
            className="gap-1.5 bg-blue-100 text-blue-700 dark:bg-blue-900/30 dark:text-blue-300 border-0">
            <CheckCircle2 className="h-3 w-3" />
            {selectedCount} selected
          </Badge>
        )}

        {isLoading && (
          <Badge
            variant="secondary"
            className="gap-1.5 bg-amber-100 text-amber-700 dark:bg-amber-900/30 dark:text-amber-300 border-0 animate-pulse">
            <Loader2 className="h-3 w-3 animate-spin" />
            Loading...
          </Badge>
        )}
      </div>

      <div className="flex items-center gap-2.5">
        <label
          htmlFor="select-all-checkbox"
          className={cn(
            "text-xs font-medium cursor-pointer transition-colors",
            selectedCount === dataLength && dataLength > 0
              ? "text-blue-600 dark:text-blue-400"
              : "text-gray-500 hover:text-gray-700 dark:hover:text-gray-300",
          )}>
          Select all
        </label>
        <Checkbox
          id="select-all-checkbox"
          checked={selectedCount === dataLength && dataLength > 0}
          onCheckedChange={onToggleSelectAll}
          className="h-4 w-4 rounded-[4px]"
        />
      </div>
    </div>
  );
}
