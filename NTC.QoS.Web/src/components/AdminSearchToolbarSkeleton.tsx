import { Skeleton } from "@/components/ui/skeleton";

export default function AdminSearchToolbarSkeleton() {
  return (
    <div className="w-full flex flex-col gap-4 sm:gap-6 mb-4 mt-2">
      {/* Top Row: Search, Filter, Sort */}
      <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4">
        <div className="flex flex-1 items-center gap-2">
          {/* Search input skeleton */}
          <Skeleton className="h-10 w-full rounded-md" />

          {/* Filter button skeleton */}
          <Skeleton className="h-10 w-20 rounded-md" />

          {/* Sort button skeleton */}
          <Skeleton className="h-10 w-20 rounded-md" />
        </div>
      </div>

      <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4">
        <div className="flex flex-1 items-center gap-2">
          {/* Municipal skeleton */}
          <Skeleton className="h-10 w-[70%] rounded-md" />

          {/* Add button skeleton */}
          <Skeleton className="h-10 w-[15%] rounded-md" />

          {/* External link button skeleton */}
          <Skeleton className="h-10 w-[15%] rounded-md" />
        </div>
      </div>

      {/* Bottom Row: Info Section */}
      <div className="pl-5 flex flex-col sm:flex-row items-start sm:items-center w-full">
        {/* Left side: broadband name + providers */}
        <div className="w-1/2 space-y-2">
          <Skeleton className="h-5 w-40 rounded-md" /> {/* broadbandName */}
          <Skeleton className="h-4 w-52 rounded-md" /> {/* dataLength */}
        </div>

        {/* Right side: total sites info */}
        <div className="flex gap-2 items-center mt-4 sm:mt-0">
          <Skeleton className="h-10 w-10 rounded-full" /> {/* icon circle */}
          <div className="space-y-2">
            <Skeleton className="h-4 w-28 rounded-md" /> {/* statusLabel */}
            <Skeleton className="h-4 w-20 rounded-md" /> {/* status */}
          </div>
        </div>
      </div>
    </div>
  );
}
