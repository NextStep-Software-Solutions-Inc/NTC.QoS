"use client";

import { motion } from "framer-motion";
import { Activity, CheckCircle2, Gauge, Loader2, TrendingDown, TrendingUp } from "lucide-react";

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
  // Speedtest-specific stats
  avgDownloadSpeed?: number;
  avgUploadSpeed?: number;
  avgPing?: number;
}

export function StatsBar({
  currentPage,
  pageSize,
  totalCount,
  selectedCount,
  isLoading = false,
  onToggleSelectAll,
  dataLength,
  avgDownloadSpeed = 0,
  avgUploadSpeed = 0,
  avgPing = 0,
}: StatsBarProps) {
  const startItem = (currentPage - 1) * pageSize + 1;
  const endItem = Math.min(currentPage * pageSize, totalCount);

  return (
    <motion.div
      initial={{ opacity: 0, y: 10 }}
      animate={{ opacity: 1, y: 0 }}
      className="flex flex-wrap items-center justify-between gap-4 mt-4 py-3 px-4 rounded-xl bg-gradient-to-r from-slate-50 via-gray-50 to-slate-50 dark:from-gray-800/80 dark:via-gray-850/80 dark:to-gray-800/80 border border-gray-200/60 dark:border-gray-700/60 shadow-sm">
      <div className="flex items-center gap-4">
        {/* Count info */}
        <div className="flex items-center gap-2">
          <div className="flex items-center justify-center h-7 w-7 rounded-lg bg-gradient-to-br from-cyan-500 to-blue-600 shadow-sm">
            <Activity className="h-3.5 w-3.5 text-white" />
          </div>
          <span className="text-sm text-gray-600 dark:text-gray-400">
            Showing{" "}
            <span className="font-bold text-gray-900 dark:text-white">
              {startItem.toLocaleString()}-{endItem.toLocaleString()}
            </span>{" "}
            of{" "}
            <span className="font-bold bg-gradient-to-r from-cyan-600 to-blue-600 bg-clip-text text-transparent">
              {totalCount.toLocaleString()}
            </span>{" "}
            speedtests
          </span>
        </div>

        {/* Speed stats */}
        {avgDownloadSpeed > 0 && (
          <div className="hidden sm:flex items-center gap-3 pl-4 border-l border-gray-200 dark:border-gray-700">
            <div className="flex items-center gap-1.5">
              <TrendingDown className="h-3.5 w-3.5 text-green-500" />
              <span className="text-xs text-gray-500">Avg DL:</span>
              <span className="text-xs font-bold text-green-600">{avgDownloadSpeed.toFixed(1)} Mbps</span>
            </div>
            <div className="flex items-center gap-1.5">
              <TrendingUp className="h-3.5 w-3.5 text-blue-500" />
              <span className="text-xs text-gray-500">Avg UL:</span>
              <span className="text-xs font-bold text-blue-600">{avgUploadSpeed.toFixed(1)} Mbps</span>
            </div>
            <div className="flex items-center gap-1.5">
              <Gauge className="h-3.5 w-3.5 text-orange-500" />
              <span className="text-xs text-gray-500">Avg Ping:</span>
              <span className="text-xs font-bold text-orange-600">{avgPing.toFixed(0)} ms</span>
            </div>
          </div>
        )}

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
          htmlFor="select-all-speedtest"
          className={cn(
            "text-xs font-medium cursor-pointer transition-colors",
            selectedCount === dataLength && dataLength > 0
              ? "text-blue-600 dark:text-blue-400"
              : "text-gray-500 hover:text-gray-700 dark:hover:text-gray-300",
          )}>
          Select all
        </label>
        <Checkbox
          id="select-all-speedtest"
          checked={selectedCount === dataLength && dataLength > 0}
          onCheckedChange={onToggleSelectAll}
          className="h-4 w-4 rounded-[4px]"
        />
      </div>
    </motion.div>
  );
}
