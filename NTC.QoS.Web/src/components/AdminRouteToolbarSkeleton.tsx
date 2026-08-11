import { Skeleton } from "@/components/ui/skeleton";

export default function AdminRouteToolbarSkeleton() {
  return (
    <div className="w-full border rounded-md shadow-sm p-4">
      <div className="grid gap-2 grid-cols-1 xl:grid-cols-4 lg:grid-cols-3 sm:grid-cols-2">
        {/* Total Municipality */}
        <div className="flex items-center gap-2">
          <Skeleton className="h-8 w-8 rounded-full" /> {/* Icon */}
          <div className="space-y-1">
            <Skeleton className="h-4 w-24 rounded-md" /> {/* Label */}
            <Skeleton className="h-4 w-12 rounded-md" /> {/* Value */}
          </div>
        </div>

        {/* Total Cell Sites */}
        <div className="flex items-center gap-2">
          <Skeleton className="h-8 w-8 rounded-full" /> {/* Icon */}
          <div className="space-y-1">
            <Skeleton className="h-4 w-24 rounded-md" /> {/* Label */}
            <Skeleton className="h-4 w-12 rounded-md" /> {/* Value */}
          </div>
        </div>

        {/* Top ISPs */}
        <div className="flex items-center gap-2">
          <Skeleton className="h-8 w-8 rounded-full" /> {/* Icon */}
          <div className="space-y-1">
            <Skeleton className="h-4 w-20 rounded-md" /> {/* Label */}
            <div className="flex flex-wrap gap-1">
              <Skeleton className="h-4 w-10 rounded-md" />
              <Skeleton className="h-4 w-10 rounded-md" />
              <Skeleton className="h-4 w-10 rounded-md" />
            </div>
          </div>
        </div>

        {/* Search Input */}
        <div className="flex items-center border rounded-md bg-gray-50 px-2">
          <Skeleton className="h-5 w-5 rounded-md" /> {/* Icon */}
          <Skeleton className="h-8 w-full ml-2 rounded-md" /> {/* Input */}
        </div>
      </div>
    </div>
  );
}
