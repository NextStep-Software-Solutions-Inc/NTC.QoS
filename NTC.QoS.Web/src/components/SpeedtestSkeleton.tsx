import { Skeleton } from "@/components/ui/skeleton";

export const SpeedtestSkeleton = () => (
  <div className="border p-5 rounded-lg space-y-6">
    {/* Header Section Skeleton */}
    <div className="grid items-start sm:grid-cols-2 gap-5">
      <div className="flex items-start gap-3">
        <Skeleton className="w-10 h-10 rounded-sm" /> {/* Back button */}
        <div className="space-y-2">
          <Skeleton className="h-6 w-48" /> {/* Title */}
          <Skeleton className="h-4 w-64" /> {/* Location */}
          <div className="flex gap-2">
            <Skeleton className="h-6 w-20 rounded-full" /> {/* Badge */}
            <Skeleton className="h-6 w-20 rounded-full" /> {/* Badge */}
          </div>
        </div>
      </div>
    </div>

    {/* Stats Grid Skeleton */}
    <div className="grid items-center sm:grid-cols-2 gap-5">
      <Skeleton className="h-[90px] w-full rounded-md" /> {/* Avg Download */}
      <Skeleton className="h-[90px] w-full rounded-md" /> {/* Avg Upload */}
      <Skeleton className="h-[90px] w-full rounded-md" /> {/* Average Latency */}
      <Skeleton className="h-[90px] w-full rounded-md" /> {/* Network Stability */}
    </div>

    {/* Speed Trends Chart Skeleton */}
    <div className="space-y-3">
      <Skeleton className="h-6 w-64" /> {/* Chart title */}
      <Skeleton className="h-4 w-80" /> {/* Chart description */}
      <Skeleton className="h-[300px] w-full rounded-md" /> {/* Chart */}
    </div>

    {/* Monthly Performance Summary Skeleton */}
    <div className="space-y-4">
      <Skeleton className="h-6 w-72" /> {/* Section Header */}
      <Skeleton className="h-4 w-60" /> {/* Section description */}
      <div className="space-y-3">
        <div className="flex justify-between">
          <Skeleton className="h-5 w-28" />
          <Skeleton className="h-5 w-32" />
          <Skeleton className="h-5 w-32" />
        </div>
        <div className="flex justify-between">
          <Skeleton className="h-5 w-28" />
          <Skeleton className="h-5 w-32" />
          <Skeleton className="h-5 w-32" />
        </div>
      </div>
    </div>
  </div>
);
