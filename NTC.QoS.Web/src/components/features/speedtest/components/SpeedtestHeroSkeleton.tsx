"use client";

import { Skeleton } from "@/components/ui/skeleton";

/**
 * Skeleton loading state for SpeedtestHero component
 * Matches the layout and structure of the actual hero section
 */
export function SpeedtestHeroSkeleton() {
  return (
    <div 
      className="overflow-hidden text-white shadow-lg sticky top-0 z-40 h-[180px]"
      style={{ background: "linear-gradient(to right, rgb(6 182 212), rgb(59 130 246), rgb(124 58 237))" }}
    >
      <div className="relative h-full">
        {/* Background glow effects */}
        <div
          className="absolute top-0 right-0 h-48 w-48 rounded-full opacity-15"
          style={{
            background: "radial-gradient(circle, rgba(34, 211, 238, 0.4) 0%, transparent 70%)",
            transform: "translate(30%, -30%)",
            filter: "blur(40px)",
          }}
        />
        <div
          className="absolute bottom-0 left-0 h-48 w-48 rounded-full opacity-15"
          style={{
            background: "radial-gradient(circle, rgba(139, 92, 246, 0.4) 0%, transparent 70%)",
            transform: "translate(-30%, 30%)",
            filter: "blur(40px)",
          }}
        />

        {/* Content */}
        <div className="relative px-6 py-4 h-full flex flex-col">
          {/* Header row */}
          <div className="flex items-center justify-between mb-4">
            <div className="flex items-center gap-3">
              <Skeleton className="h-10 w-10 rounded-xl bg-white/20" />
              <div>
                <Skeleton className="h-6 w-48 mb-1 bg-white/30" />
                <Skeleton className="h-4 w-32 bg-white/20" />
              </div>
            </div>
            <div className="flex items-center gap-2">
              <Skeleton className="h-8 w-24 rounded-md bg-white/20" />
              <Skeleton className="h-8 w-8 rounded-md bg-white/20" />
            </div>
          </div>

          {/* Stats grid */}
          <div className="grid grid-cols-2 md:grid-cols-4 gap-3 flex-1">
            {/* Stat card 1 */}
            <div className="bg-white/10 backdrop-blur-sm rounded-lg p-3 border border-white/20">
              <div className="flex items-center gap-2 mb-2">
                <Skeleton className="h-4 w-4 rounded bg-white/30" />
                <Skeleton className="h-3 w-20 bg-white/30" />
              </div>
              <Skeleton className="h-7 w-24 bg-white/40" />
            </div>

            {/* Stat card 2 */}
            <div className="bg-white/10 backdrop-blur-sm rounded-lg p-3 border border-white/20">
              <div className="flex items-center gap-2 mb-2">
                <Skeleton className="h-4 w-4 rounded bg-white/30" />
                <Skeleton className="h-3 w-20 bg-white/30" />
              </div>
              <Skeleton className="h-7 w-24 bg-white/40" />
            </div>

            {/* Stat card 3 */}
            <div className="bg-white/10 backdrop-blur-sm rounded-lg p-3 border border-white/20">
              <div className="flex items-center gap-2 mb-2">
                <Skeleton className="h-4 w-4 rounded bg-white/30" />
                <Skeleton className="h-3 w-20 bg-white/30" />
              </div>
              <Skeleton className="h-7 w-24 bg-white/40" />
            </div>

            {/* Stat card 4 */}
            <div className="bg-white/10 backdrop-blur-sm rounded-lg p-3 border border-white/20">
              <div className="flex items-center gap-2 mb-2">
                <Skeleton className="h-4 w-4 rounded bg-white/30" />
                <Skeleton className="h-3 w-20 bg-white/30" />
              </div>
              <Skeleton className="h-7 w-24 bg-white/40" />
            </div>
          </div>

          {/* Signal quality badges */}
          <div className="flex gap-2 mt-4">
            <Skeleton className="h-7 w-24 rounded-full bg-white/20" />
            <Skeleton className="h-7 w-20 rounded-full bg-white/20" />
            <Skeleton className="h-7 w-24 rounded-full bg-white/20" />
            <Skeleton className="h-7 w-20 rounded-full bg-white/20" />
          </div>
        </div>
      </div>
    </div>
  );
}
