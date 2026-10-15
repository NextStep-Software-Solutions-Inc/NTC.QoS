"use client";

import { useEffect, useMemo, useRef, useState } from "react";
import * as maptilersdk from "@maptiler/sdk";
import { Map as MapIcon, Maximize2, Minimize2, X } from "lucide-react";

import { type Cell } from "@/components/features/cell/types/Cell";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";

interface PhilippinesCellsiteMapProps {
  cells: Cell[];
  isExpanded?: boolean;
  onToggleExpand?: () => void;
  onClose?: () => void;
  className?: string;
}

// Get provider color from cell data (data-driven, not hardcoded)
function getProviderColor(cell: Cell): string {
  return cell.serviceProvider.provider.color;
}

// Philippine geographic bounds
const PH_CENTER: [number, number] = [122.0, 12.0]; // [lng, lat]
const PH_ZOOM = 4.5; // Fixed zoom to show entire Philippines

export function PhilippinesCellsiteMap({
  cells,
  isExpanded = false,
  onToggleExpand,
  onClose,
  className = "",
}: PhilippinesCellsiteMapProps) {
  const mapContainerRef = useRef<HTMLDivElement>(null);
  const mapRef = useRef<maptilersdk.Map | null>(null);
  const markersRef = useRef<HTMLDivElement[]>([]);
  const [mapLoaded, setMapLoaded] = useState(false);
  const [mapError, setMapError] = useState<string | null>(null);

  // Count cells by provider for legend (data-driven colors)
  const providerCounts = useMemo(() => {
    const counts: Record<string, { count: number; color: string }> = {};
    cells.forEach((cell) => {
      const providerName = cell.serviceProvider.provider.name || "Unknown";
      const color = getProviderColor(cell);
      if (!counts[providerName]) {
        counts[providerName] = { count: 0, color };
      }
      counts[providerName].count++;
    });
    return counts;
  }, [cells]);

  // Convert cells to GeoJSON for efficient rendering
  const geoJsonData = useMemo(() => {
    return {
      type: "FeatureCollection" as const,
      features: cells
        .filter((cell) => typeof cell.longitude === "number" && typeof cell.latitude === "number")
        .map((cell, index) => ({
          type: "Feature" as const,
          geometry: { type: "Point" as const, coordinates: [cell.longitude, cell.latitude] as [number, number] },
          properties: {
            provider: cell.serviceProvider.provider.name || "Unknown",
            color: getProviderColor(cell),
            id: index, // Use index as ID since CellViewport may not have id
          },
        })),
    };
  }, [cells]);

  // Initialize MapTiler map
  useEffect(() => {
    if (!mapContainerRef.current || mapRef.current) return;

    // Dynamically import MapTiler SDK
    import("@maptiler/sdk")
      .then((maptilersdk) => {
        if (!mapContainerRef.current) return;

        try {
          // Set API key - should be from environment variable
          maptilersdk.config.apiKey = process.env.NEXT_PUBLIC_MAPTILER_API_KEY || "YdGJjWFVpmrHqz1FNIwk";

          // Create map with fixed zoom showing entire Philippines
          const map = new maptilersdk.Map({
            container: mapContainerRef.current,
            style: maptilersdk.MapStyle.TOPO,
            center: PH_CENTER,
            zoom: PH_ZOOM,
            maxZoom: PH_ZOOM + 1,
            minZoom: PH_ZOOM - 1,
            interactive: false, // No zoom or pan - static view
            attributionControl: false,
            // Required for canvas.toDataURL() to return non-blank screenshots
            canvasContextAttributes: { preserveDrawingBuffer: true },
          });

          map.on("load", () => {
            setMapLoaded(true);

            // Add GeoJSON source for cells
            map.addSource("cells", { type: "geojson", data: geoJsonData });

            // Add circle layer for cell sites
            map.addLayer({
              id: "cells-layer",
              type: "circle",
              source: "cells",
              paint: {
                "circle-radius": 2,
                "circle-color": ["get", "color"],
                "circle-stroke-color": "#ffffff",
                "circle-stroke-width": 0.5,
                "circle-stroke-opacity": 0.5,
              },
            });
          });

          mapRef.current = map;
        } catch (err) {
          console.error("MapTiler initialization error:", err);
          setMapError("Failed to load map");
        }
      })
      .catch((err) => {
        console.error("Failed to load MapTiler SDK:", err);
        setMapError("Failed to load map library");
      });

    return () => {
      if (mapRef.current) {
        mapRef.current.remove();
        mapRef.current = null;
      }
      markersRef.current = [];
    };
  }, []);

  // Update markers when cells change
  useEffect(() => {
    if (!mapRef.current || !mapLoaded) return;

    // Clear existing markers
    markersRef.current.forEach((el) => el.remove());
    markersRef.current = [];

    // Re-import and add new markers
    import("@maptiler/sdk").then((maptilersdk) => {
      if (!mapRef.current) return;

      cells.forEach((cell) => {
        if (typeof cell.longitude !== "number" || typeof cell.latitude !== "number") return;

        const color = getProviderColor(cell);

        const el = document.createElement("div");
        el.style.width = "8px";
        el.style.height = "8px";
        el.style.borderRadius = "50%";
        el.style.backgroundColor = color;
        el.style.border = "1px solid rgba(255,255,255,0.5)";
        el.style.boxShadow = "0 1px 2px rgba(0,0,0,0.3)";

        if (!mapRef.current) return;
        new maptilersdk.Marker({ element: el }).setLngLat([cell.longitude, cell.latitude]).addTo(mapRef.current);

        markersRef.current.push(el);
      });
    });
  }, [cells, mapLoaded]);

  // Resize map when expanded state changes
  useEffect(() => {
    if (mapRef.current && mapLoaded) {
      setTimeout(() => {
        mapRef.current?.resize();
      }, 100);
    }
  }, [isExpanded, mapLoaded]);

  return (
    <Card className={`overflow-hidden flex flex-col h-full ${className}`}>
      <CardHeader className="pb-2 flex flex-row items-center justify-between flex-shrink-0">
        <div className="flex items-center gap-2">
          <MapIcon className="h-5 w-5 text-blue-500" />
          <CardTitle className="text-base">PH Cell Site GIS</CardTitle>
          <Badge variant="outline" className="ml-1 text-xs">
            {cells.length}
          </Badge>
        </div>
        <div className="flex items-center gap-1">
          {onToggleExpand && (
            <Button
              variant="ghost"
              size="sm"
              onClick={onToggleExpand}
              className="h-7 w-7 p-0"
              title={isExpanded ? "Minimize" : "Maximize"}>
              {isExpanded ? <Minimize2 className="h-3.5 w-3.5" /> : <Maximize2 className="h-3.5 w-3.5" />}
            </Button>
          )}
          {onClose && (
            <Button variant="ghost" size="sm" onClick={onClose} className="h-7 w-7 p-0" title="Close map">
              <X className="h-3.5 w-3.5" />
            </Button>
          )}
        </div>
      </CardHeader>
      <CardContent className="p-3 flex-1 flex flex-col min-h-0">
        {/* MapTiler Map Container */}
        <div
          ref={mapContainerRef}
          className={`relative flex-1 rounded-lg overflow-hidden ${isExpanded ? "min-h-[600px]" : "min-h-[350px]"}`}
          style={{ minHeight: isExpanded ? "600px" : "350px" }}>
          {mapError && (
            <div className="absolute inset-0 flex items-center justify-center bg-gray-100 dark:bg-gray-800">
              <p className="text-sm text-gray-500">{mapError}</p>
            </div>
          )}
          {!mapLoaded && !mapError && (
            <div className="absolute inset-0 flex items-center justify-center bg-gray-100 dark:bg-gray-800">
              <p className="text-sm text-gray-500">Loading map...</p>
            </div>
          )}
        </div>

        {/* Provider Legend - Data-driven colors */}
        <div className="mt-2 flex flex-wrap gap-x-3 gap-y-1 text-xs flex-shrink-0">
          {Object.entries(providerCounts)
            .sort((a, b) => b[1].count - a[1].count)
            .slice(0, 6) // Show top 6 providers
            .map(([provider, { count, color }]) => (
              <div key={provider} className="flex items-center gap-1">
                <div className="w-2.5 h-2.5 rounded-full" style={{ backgroundColor: color }} />
                <span className="text-gray-600 dark:text-gray-400 truncate max-w-[80px]">
                  {provider} ({count})
                </span>
              </div>
            ))}
        </div>
      </CardContent>
    </Card>
  );
}

export default PhilippinesCellsiteMap;
