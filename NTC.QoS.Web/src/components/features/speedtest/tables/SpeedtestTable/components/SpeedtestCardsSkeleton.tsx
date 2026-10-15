import { Card } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";

/**
 * Skeleton loading state for SpeedtestCard grid view
 * Shows 15 placeholder cards in responsive grid layout
 */
export function SpeedtestCardsSkeleton() {
  return (
    <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 2xl:grid-cols-5 gap-4 p-1">
      {Array.from({ length: 15 }).map((_, i) => (
        <Card key={i} className="relative overflow-hidden border border-gray-200 dark:border-gray-700">
          <div className="p-4 space-y-4">
            {/* Header with checkbox and provider */}
            <div className="flex items-start justify-between gap-2">
              <div className="flex items-center gap-2">
                <Skeleton className="h-4 w-4 rounded" />
                <Skeleton className="h-10 w-10 rounded-full" />
                <div className="space-y-1">
                  <Skeleton className="h-4 w-24" />
                  <Skeleton className="h-3 w-20" />
                </div>
              </div>
              <Skeleton className="h-8 w-8 rounded" />
            </div>

            {/* Signal quality badge */}
            <div className="flex items-center justify-between">
              <Skeleton className="h-5 w-16 rounded-full" />
              <Skeleton className="h-4 w-32" />
            </div>

            {/* Speed metrics */}
            <div className="space-y-3">
              {/* Download */}
              <div className="space-y-1.5">
                <div className="flex items-center justify-between">
                  <Skeleton className="h-4 w-20" />
                  <Skeleton className="h-5 w-16" />
                </div>
                <Skeleton className="h-2 w-full rounded-full" />
              </div>

              {/* Upload */}
              <div className="space-y-1.5">
                <div className="flex items-center justify-between">
                  <Skeleton className="h-4 w-20" />
                  <Skeleton className="h-5 w-16" />
                </div>
                <Skeleton className="h-2 w-full rounded-full" />
              </div>
            </div>

            {/* Additional metrics */}
            <div className="grid grid-cols-2 gap-3">
              <div className="space-y-1">
                <Skeleton className="h-3 w-12" />
                <Skeleton className="h-5 w-16" />
              </div>
              <div className="space-y-1">
                <Skeleton className="h-3 w-12" />
                <Skeleton className="h-5 w-16" />
              </div>
            </div>

            {/* Footer with location and actions */}
            <div className="flex items-center justify-between pt-2 border-t border-gray-100 dark:border-gray-800">
              <div className="flex items-center gap-1.5">
                <Skeleton className="h-3 w-3 rounded" />
                <Skeleton className="h-3 w-32" />
              </div>
              <div className="flex gap-1">
                <Skeleton className="h-7 w-7 rounded" />
                <Skeleton className="h-7 w-7 rounded" />
              </div>
            </div>
          </div>
        </Card>
      ))}
    </div>
  );
}
