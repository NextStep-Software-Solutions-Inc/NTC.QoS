"use client";

import { Card, CardContent } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";

export function ProviderDetailsSkeleton() {
  // Fixed count for skeleton rendering
  const serviceCount = 3;

  return (
    <div className="p-6 w-full mx-auto space-y-4">
      {/* Header */}
      <div className="w-full flex justify-between items-center space-x-4">
        <div className="flex items-center space-x-2">
          <div className="flex items-center gap-x-1 pl-10">
            <Skeleton className="w-10 h-10 rounded-full" /> {/* Avatar */}
            <Skeleton className="h-6 w-32 rounded-md" /> {/* Provider Name */}
          </div>
        </div>
      </div>

      <Card>
        <CardContent className="w-full p-6 space-y-6">
          {/* Basic Information Header */}
          <div className="flex items-center justify-between">
            <Skeleton className="h-6 w-40 rounded-md" /> {/* Title */}
            <div className="flex gap-2">
              <Skeleton className="h-10 w-20 rounded-md" />
              <Skeleton className="h-10 w-20 rounded-md" />
            </div>
          </div>

          {/* Provider Basic Info */}
          <div className="w-full grid sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4">
            {Array.from({ length: 4 }).map((_, index) => (
              <div key={index} className="flex items-center space-x-4">
                <Skeleton className="p-[.5rem] rounded-full w-10 h-10" />
                <section className="flex-1 space-y-1">
                  <Skeleton className="h-3 w-24 rounded-md" /> {/* Label */}
                  <Skeleton className="h-4 w-32 rounded-md" /> {/* Value */}
                </section>
              </div>
            ))}
          </div>

          <hr />

          {/* Service Details */}
          <div>
            <div className="flex items-center justify-between mb-2">
              <Skeleton className="h-6 w-40 rounded-md" /> {/* Title */}
              <Skeleton className="h-10 w-40 rounded-md" /> {/* Add button */}
            </div>

            <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
              {Array.from({ length: serviceCount }).map((_, i) => (
                <Card key={i} className="flex items-center justify-between border p-4">
                  <Skeleton className="h-5 w-24 rounded-md" /> {/* Service Name */}
                  <Skeleton className="h-4 w-20 rounded-md" /> {/* Type Badge */}
                </Card>
              ))}
            </div>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}

// Usage in your component
/*
<WithPlaceholder isLoading={!provider || !services} placeholder={<ProviderDetailsSkeleton />}>
  <YourActualProviderDetailsComponent />
</WithPlaceholder>
*/
