"use client";

import { Card, CardContent, CardHeader } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import { SpeedtestCardsSkeleton } from "./tables/SpeedtestTable/components/SpeedtestCardsSkeleton";
import { SpeedtestHeroSkeleton } from "./components/SpeedtestHeroSkeleton";

/**
 * Full page skeleton loading state for speedtest management page
 * Matches the current SpeedtestDashboard layout:
 * - Hero (expanded)
 * - Action bar (Refresh + Show Map | Export + Add Speedtest)
 * - Analytics panel collapsed header bar (default-collapsed)
 * - Table card (header + SpeedtestCardsSkeleton content, no footer — footer only shows when data loads)
 */
export function SpeedtestPageSkeleton() {
  return (
    <div className="flex flex-col h-full w-full overflow-hidden">
      {/* Hero Skeleton */}
      <SpeedtestHeroSkeleton />

      {/* Main Content Area */}
      <div className="flex-1 flex flex-col overflow-hidden p-4 gap-4">
        {/* Action Bar Skeleton */}
        <div className="flex flex-wrap items-center justify-between gap-3">
          <div className="flex items-center gap-2">
            {/* Refresh */}
            <Skeleton className="h-8 w-24 rounded-md" />
            {/* Show Map */}
            <Skeleton className="h-8 w-28 rounded-md" />
          </div>
          <div className="flex items-center gap-2">
            {/* Export */}
            <Skeleton className="h-8 w-24 rounded-md" />
            {/* Add Speedtest */}
            <Skeleton className="h-8 w-32 rounded-md" />
          </div>
        </div>

        {/* Analytics Panel Skeleton — collapsed by default (gradient header bar only) */}
        <div
          className="flex items-center justify-between px-5 py-4 rounded-xl"
          style={{ background: "linear-gradient(to right, rgb(8 145 178), rgb(37 99 235), rgb(124 58 237))" }}>
          <div className="flex items-center gap-3">
            <Skeleton className="h-9 w-9 rounded-lg bg-white/20" />
            <div>
              <Skeleton className="h-5 w-52 mb-1 bg-white/30" />
              <Skeleton className="h-3 w-40 bg-white/20" />
            </div>
          </div>
          <div className="flex items-center gap-2">
            <Skeleton className="h-6 w-28 rounded-full bg-white/20" />
            <Skeleton className="h-8 w-8 rounded-full bg-white/20" />
          </div>
        </div>

        {/* Table Skeleton — fills remaining space */}
        <div className="flex-1 min-h-0">
          <Card className="flex flex-col h-full overflow-hidden">
            {/* Header */}
            <CardHeader className="flex-shrink-0 px-4 py-3 border-b bg-gradient-to-r from-slate-50 to-gray-50 dark:from-gray-900 dark:to-gray-800">
              <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
                {/* Left: Icon + Title + subtitle */}
                <div className="flex items-center gap-3">
                  <Skeleton className="h-10 w-10 rounded-xl" />
                  <div>
                    <Skeleton className="h-6 w-40 mb-1" />
                    <Skeleton className="h-4 w-56" />
                  </div>
                </div>

                {/* Right: Search + Filters + View Toggle (3 btns) + Refresh (icon) + Export */}
                <div className="flex items-center gap-2">
                  <Skeleton className="h-9 w-64 rounded-md" />
                  <Skeleton className="h-9 w-24 rounded-md" />
                  <div className="flex items-center rounded-lg border bg-white dark:bg-gray-800 p-0.5">
                    <Skeleton className="h-8 w-8 rounded" />
                    <Skeleton className="h-8 w-8 rounded" />
                    <Skeleton className="h-8 w-8 rounded" />
                  </div>
                  <Skeleton className="h-9 w-9 rounded-md" />
                  <Skeleton className="h-9 w-20 rounded-md" />
                </div>
              </div>
            </CardHeader>

            {/* Content — reuse SpeedtestCardsSkeleton (matches table's isLoading state) */}
            <CardContent className="flex-1 overflow-auto p-4">
              <SpeedtestCardsSkeleton />
            </CardContent>
          </Card>
        </div>
      </div>
    </div>
  );
}
