"use client";

import { Card, CardContent, CardHeader } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";

export function BoundaryDetailsCellsSkeleton() {
  // Fixed counts for skeleton rendering
  const providerCount = 2;
  const siteCount = 2;
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
      <div className="w-full space-y-2">
        {Array.from({ length: providerCount }).map((_, providerIndex) => (
          <Card key={providerIndex} className="w-full max-w-screen-lg">
            <CardHeader className="flex justify-between p-4">
              <div className="flex items-center gap-2">
                <Skeleton className="h-8 w-8 rounded-full" />
                <Skeleton className="h-5 w-32 rounded-lg" />
              </div>
              <div className="flex items-end gap-3">
                <Skeleton className="h-8 w-8 rounded-full" />
                <div className="flex flex-col gap-1">
                  <Skeleton className="h-3 w-16 rounded" />
                  <Skeleton className="h-3 w-12 rounded" />
                </div>
              </div>
            </CardHeader>

            <CardContent className="space-y-2">
              {Array.from({ length: siteCount }).map((_, siteIndex) => (
                <div key={siteIndex} className="flex items-center justify-between">
                  <div className="flex items-center gap-3">
                    <Skeleton className="h-7 w-7 rounded-full" />
                    <div className="flex-1 space-y-1">
                      <Skeleton className="h-4 w-32 rounded" />
                      <Skeleton className="h-3 w-20 rounded" />
                      <Skeleton className="h-3 w-40 rounded" />
                    </div>
                  </div>
                  <div className="flex items-center gap-2">
                    <Skeleton className="h-6 w-16 rounded-full" />
                    <Skeleton className="h-8 w-20 rounded" />
                  </div>
                </div>
              ))}
            </CardContent>
          </Card>
        ))}
      </div>
    </div>
  );
}
