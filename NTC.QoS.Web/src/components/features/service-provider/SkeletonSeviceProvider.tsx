import React from "react";

import { Skeleton } from "@/components/ui/skeleton";

const SkeletonServiceProvider = () => {
  return (
    <div className="p-5">
      <section className="flex items-center justify-between">
        <section className="flex items-center space-x-1">
          <Skeleton className="h-6 w-6 rounded-full" />
          <Skeleton className="w-11 h-11 rounded-full" />
          <Skeleton className="w-[300px] h-12" />
        </section>
        <div>
          <Skeleton className="w-6 h-6 rounded-full" />
        </div>
      </section>

      <section className="mt-6">
        <Skeleton className="h-8 w-[200px] mb-4" />
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
          {Array.from({ length: 4 }).map((_, index) => (
            <div key={index} className="flex items-center space-x-1">
              <Skeleton className="h-10 w-10 rounded-full" />
              <div>
                <Skeleton className="h-4 w-[100px]" />
                <Skeleton className="h-7 w-[150px] mt-1" />
              </div>
            </div>
          ))}
        </div>
      </section>
    </div>
  );
};

export default SkeletonServiceProvider;
