"use client";

import { Card, CardContent } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";

export function BoundaryDetailsSpeedtestsSkeleton() {
  // Fixed count for skeleton rendering
  const providerCount = 2;
  return (
    <div className="space-y-4 w-full">
      {/* Header Skeleton */}
      <div className="flex items-center justify-between gap-2 mx-2">
        <Skeleton className="h-8 w-64 rounded-md" /> {/* Title */}
        <div className="flex items-center gap-2">
          <Skeleton className="h-10 w-20 rounded-md" /> {/* Add Button */}
          <Skeleton className="h-10 w-10 rounded-md" /> {/* External link */}
        </div>
      </div>

      {/* AdminSearchToolbar Skeleton */}
      <div className="w-full">
        <div className="w-full flex flex-col gap-4 sm:gap-6 mb-4 mt-2">
          <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4">
            <div className="flex flex-1 items-center gap-2">
              <Skeleton className="h-10 w-full rounded-md" />
              <Skeleton className="h-10 w-20 rounded-md" />
              <Skeleton className="h-10 w-20 rounded-md" />
            </div>
          </div>

          <div className="pl-5 flex flex-col sm:flex-row items-start sm:items-center w-full">
            <div className="w-1/2 space-y-2">
              <Skeleton className="h-5 w-40 rounded-md" />
              <Skeleton className="h-4 w-52 rounded-md" />
            </div>
            <div className="flex gap-2 items-center mt-4 sm:mt-0">
              <Skeleton className="h-10 w-10 rounded-full" />
              <div className="space-y-2">
                <Skeleton className="h-4 w-28 rounded-md" />
                <Skeleton className="h-4 w-20 rounded-md" />
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* Provider Cards Skeleton */}
      <div className="w-full space-y-4">
        {Array.from({ length: providerCount }).map((_, providerIndex) => (
          <Card key={providerIndex} className="w-full p-3">
            {/* Card Header */}
            <div className="flex justify-between items-center mb-4">
              <div className="flex items-center gap-2">
                <Skeleton className="h-8 w-8 rounded-full" /> {/* Avatar */}
                <Skeleton className="h-5 w-32 rounded-md" /> {/* Provider Name */}
              </div>
              <Skeleton className="h-8 w-20 rounded-md" /> {/* Dropdown/Edit */}
            </div>

            {/* Card Content */}
            <CardContent className="space-y-4 p-2">
              {/* Metrics Grid */}
              <div className="grid grid-cols-1 sm:grid-cols-4 gap-4">
                {Array.from({ length: 4 }).map((_, metricIndex) => (
                  <div key={metricIndex} className="space-y-1">
                    <Skeleton className="h-4 w-24 rounded-md" /> {/* Label */}
                    <Skeleton className="h-6 w-16 rounded-md" /> {/* Value */}
                  </div>
                ))}
              </div>

              {/* Info Section */}
              <div className="flex flex-col sm:flex-row justify-between text-sm gap-2">
                <div className="space-y-2 mt-2">
                  <Skeleton className="h-4 w-48 rounded-md" />
                  <Skeleton className="h-4 w-32 rounded-md" />
                </div>
                <div className="space-y-2 mt-2">
                  <Skeleton className="h-4 w-48 rounded-md" />
                  <Skeleton className="h-4 w-32 rounded-md" />
                  <Skeleton className="h-4 w-40 rounded-md" />
                </div>
                <div className="mt-2">
                  <Skeleton className="h-6 w-24 rounded-md" /> {/* Extra info */}
                </div>
              </div>

              {/* Show Chart Button */}
              <Skeleton className="h-10 w-full rounded-md mt-4" />
            </CardContent>
          </Card>
        ))}
      </div>
    </div>
  );
}
