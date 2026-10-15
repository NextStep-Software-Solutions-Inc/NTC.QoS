"use client";

import "@maptiler/sdk/dist/maptiler-sdk.css";
import "@/styles/maptiler.css";

import React, { useEffect, useMemo, useRef } from "react";

import MapType from "@/components/Map/MapType";
import { useMap } from "@/hooks/useMap";
import { useMapSpeedtests } from "@/hooks/useMapSpeedtests";

interface SpeedtestMapViewProps {
  showHeatmap?: boolean;
  showCircles?: boolean;
}

/**
 * SpeedtestMapView - Internal map view for speedtest visualization
 * Renders speedtest data as heatmap and/or circles on the map
 */
export default function SpeedtestMapView({ showHeatmap = true, showCircles = true }: SpeedtestMapViewProps) {
  const mapContainerRef = useRef<HTMLDivElement | null>(null);
  const { setMapContainer, isMapLoaded } = useMap();

  // Configure speedtest layer visibility
  const speedtestsVisibilities = useMemo(
    () => ({ "speedtests-circles": showCircles, "speedtests-heatmap": showHeatmap }),
    [showCircles, showHeatmap],
  );

  // Use the speedtest map hook
  useMapSpeedtests(speedtestsVisibilities);

  // Set up map container
  useEffect(() => {
    if (mapContainerRef.current) {
      setMapContainer(mapContainerRef.current);
    }
  }, [setMapContainer]);

  return (
    <div className="relative h-full w-full">
      <div ref={mapContainerRef} className="relative h-full w-full" />
      <div className="absolute bottom-3 right-3">
        <MapType />
      </div>
    </div>
  );
}
