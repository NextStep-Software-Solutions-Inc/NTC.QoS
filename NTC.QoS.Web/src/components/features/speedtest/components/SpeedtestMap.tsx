"use client";

import React, { memo, Suspense } from "react";
import { Eye, EyeOff, Layers, Loader2 } from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";

// Lazy load the map view
const SpeedtestMapView = React.lazy(() => import("./SpeedtestMapView"));

// Loading component for the map
function MapLoader() {
  return (
    <div className="w-full h-full flex items-center justify-center bg-gradient-to-br from-slate-100 to-slate-200 dark:from-gray-800 dark:to-gray-900">
      <div className="flex flex-col items-center gap-3">
        <Loader2 className="h-8 w-8 text-cyan-500 animate-spin" />
        <p className="text-sm text-gray-500 dark:text-gray-400">Loading map...</p>
      </div>
    </div>
  );
}

interface SpeedtestMapProps {
  className?: string;
  height?: string | number;
  showControls?: boolean;
}

/**
 * SpeedtestMap - Embedded map component for viewing speedtest locations
 * Uses the existing map infrastructure with speedtest-specific visualization
 */
function SpeedtestMapComponent({ className, height = "400px", showControls = true }: SpeedtestMapProps) {
  const [showHeatmap, setShowHeatmap] = React.useState(true);
  const [showCircles, setShowCircles] = React.useState(true);

  return (
    <div className={cn("relative w-full rounded-xl overflow-hidden border shadow-sm", className)} style={{ height }}>
      {/* Map View */}
      <Suspense fallback={<MapLoader />}>
        <SpeedtestMapView showHeatmap={showHeatmap} showCircles={showCircles} />
      </Suspense>

      {/* Map Controls Overlay */}
      {showControls && (
        <div className="absolute top-3 right-3 z-10">
          <div className="flex flex-col gap-2 p-2 rounded-lg bg-white/90 dark:bg-gray-900/90 backdrop-blur-sm shadow-lg border">
            <Button
              variant={showHeatmap ? "default" : "ghost"}
              size="sm"
              onClick={() => setShowHeatmap(!showHeatmap)}
              className={cn("gap-2 text-xs", showHeatmap && "bg-gradient-to-r from-cyan-500 to-blue-600 text-white")}>
              {showHeatmap ? <Eye className="h-3.5 w-3.5" /> : <EyeOff className="h-3.5 w-3.5" />}
              Heatmap
            </Button>
            <Button
              variant={showCircles ? "default" : "ghost"}
              size="sm"
              onClick={() => setShowCircles(!showCircles)}
              className={cn("gap-2 text-xs", showCircles && "bg-gradient-to-r from-cyan-500 to-blue-600 text-white")}>
              {showCircles ? <Eye className="h-3.5 w-3.5" /> : <EyeOff className="h-3.5 w-3.5" />}
              Points
            </Button>
          </div>
        </div>
      )}

      {/* Legend */}
      <div className="absolute bottom-3 left-3 z-10">
        <div className="flex items-center gap-2 px-3 py-1.5 rounded-lg bg-white/90 dark:bg-gray-900/90 backdrop-blur-sm shadow-lg border text-xs">
          <Layers className="h-3.5 w-3.5 text-gray-500" />
          <span className="text-gray-600 dark:text-gray-300">Speedtest Locations</span>
          <Badge variant="secondary" className="text-[10px] px-1.5 py-0">
            Live Data
          </Badge>
        </div>
      </div>
    </div>
  );
}

export const SpeedtestMap = memo(SpeedtestMapComponent);
export default SpeedtestMap;
