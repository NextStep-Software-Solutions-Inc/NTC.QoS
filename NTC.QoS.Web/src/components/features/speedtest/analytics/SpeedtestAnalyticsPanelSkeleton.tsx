"use client";

import { Card, CardContent, CardHeader } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";

/**
 * Skeleton loading state for SpeedtestAnalyticsPanel
 * Shows animated placeholders for charts and analytics
 */
export function SpeedtestAnalyticsPanelSkeleton() {
  return (
    <div className="space-y-4 p-4">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <Skeleton className="h-10 w-10 rounded-xl" />
          <div>
            <Skeleton className="h-6 w-48 mb-1" />
            <Skeleton className="h-4 w-64" />
          </div>
        </div>
        <div className="flex items-center gap-2">
          <Skeleton className="h-8 w-24 rounded-md" />
          <Skeleton className="h-8 w-8 rounded-md" />
        </div>
      </div>

      {/* Charts Grid */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
        {/* Chart Card 1 - Provider Performance */}
        <Card>
          <CardHeader className="pb-3">
            <div className="flex items-center justify-between">
              <div>
                <Skeleton className="h-5 w-48 mb-1" />
                <Skeleton className="h-4 w-64" />
              </div>
              <Skeleton className="h-8 w-24 rounded-md" />
            </div>
          </CardHeader>
          <CardContent>
            {/* Bar chart skeleton */}
            <div className="h-64 flex items-end justify-around gap-2 px-4">
              <div className="flex flex-col items-center gap-2 flex-1">
                <Skeleton className="w-full h-32 rounded-t-md" />
                <Skeleton className="h-3 w-16" />
              </div>
              <div className="flex flex-col items-center gap-2 flex-1">
                <Skeleton className="w-full h-48 rounded-t-md" />
                <Skeleton className="h-3 w-16" />
              </div>
              <div className="flex flex-col items-center gap-2 flex-1">
                <Skeleton className="w-full h-40 rounded-t-md" />
                <Skeleton className="h-3 w-16" />
              </div>
              <div className="flex flex-col items-center gap-2 flex-1">
                <Skeleton className="w-full h-24 rounded-t-md" />
                <Skeleton className="h-3 w-16" />
              </div>
            </div>
          </CardContent>
        </Card>

        {/* Chart Card 2 - Signal Quality Distribution */}
        <Card>
          <CardHeader className="pb-3">
            <div className="flex items-center justify-between">
              <div>
                <Skeleton className="h-5 w-48 mb-1" />
                <Skeleton className="h-4 w-64" />
              </div>
              <Skeleton className="h-8 w-24 rounded-md" />
            </div>
          </CardHeader>
          <CardContent>
            {/* Pie/Donut chart skeleton */}
            <div className="h-64 flex items-center justify-center">
              <div className="relative">
                <Skeleton className="h-40 w-40 rounded-full" />
                <div className="absolute inset-8">
                  <Skeleton className="h-24 w-24 rounded-full bg-background" />
                </div>
              </div>
              <div className="ml-8 space-y-3">
                <div className="flex items-center gap-2">
                  <Skeleton className="h-3 w-3 rounded-sm" />
                  <Skeleton className="h-4 w-24" />
                  <Skeleton className="h-4 w-12" />
                </div>
                <div className="flex items-center gap-2">
                  <Skeleton className="h-3 w-3 rounded-sm" />
                  <Skeleton className="h-4 w-24" />
                  <Skeleton className="h-4 w-12" />
                </div>
                <div className="flex items-center gap-2">
                  <Skeleton className="h-3 w-3 rounded-sm" />
                  <Skeleton className="h-4 w-24" />
                  <Skeleton className="h-4 w-12" />
                </div>
                <div className="flex items-center gap-2">
                  <Skeleton className="h-3 w-3 rounded-sm" />
                  <Skeleton className="h-4 w-24" />
                  <Skeleton className="h-4 w-12" />
                </div>
              </div>
            </div>
          </CardContent>
        </Card>

        {/* Chart Card 3 - Speed Distribution */}
        <Card>
          <CardHeader className="pb-3">
            <div className="flex items-center justify-between">
              <div>
                <Skeleton className="h-5 w-48 mb-1" />
                <Skeleton className="h-4 w-64" />
              </div>
              <Skeleton className="h-8 w-24 rounded-md" />
            </div>
          </CardHeader>
          <CardContent>
            {/* Line chart skeleton */}
            <div className="h-64 flex items-end justify-around px-4">
              <Skeleton className="w-1 h-20 rounded-full" />
              <Skeleton className="w-1 h-32 rounded-full" />
              <Skeleton className="w-1 h-48 rounded-full" />
              <Skeleton className="w-1 h-40 rounded-full" />
              <Skeleton className="w-1 h-56 rounded-full" />
              <Skeleton className="w-1 h-44 rounded-full" />
              <Skeleton className="w-1 h-36 rounded-full" />
              <Skeleton className="w-1 h-52 rounded-full" />
            </div>
          </CardContent>
        </Card>

        {/* Chart Card 4 - Coverage Map */}
        <Card>
          <CardHeader className="pb-3">
            <div className="flex items-center justify-between">
              <div>
                <Skeleton className="h-5 w-48 mb-1" />
                <Skeleton className="h-4 w-64" />
              </div>
              <Skeleton className="h-8 w-24 rounded-md" />
            </div>
          </CardHeader>
          <CardContent>
            {/* Map skeleton */}
            <Skeleton className="w-full h-64 rounded-lg" />
          </CardContent>
        </Card>
      </div>

      {/* Stats Summary */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        <Card>
          <CardContent className="p-4">
            <Skeleton className="h-4 w-24 mb-2" />
            <Skeleton className="h-8 w-32" />
          </CardContent>
        </Card>
        <Card>
          <CardContent className="p-4">
            <Skeleton className="h-4 w-24 mb-2" />
            <Skeleton className="h-8 w-32" />
          </CardContent>
        </Card>
        <Card>
          <CardContent className="p-4">
            <Skeleton className="h-4 w-24 mb-2" />
            <Skeleton className="h-8 w-32" />
          </CardContent>
        </Card>
        <Card>
          <CardContent className="p-4">
            <Skeleton className="h-4 w-24 mb-2" />
            <Skeleton className="h-8 w-32" />
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
