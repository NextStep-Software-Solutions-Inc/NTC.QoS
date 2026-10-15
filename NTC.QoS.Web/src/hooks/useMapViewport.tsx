"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import type { GeoJSONSource } from "@maptiler/sdk";

import { useMap } from "./useMap";

export type ViewportBounds = { minLat: number; maxLat: number; minLng: number; maxLng: number; zoom: number };

type BoundsConstraint = { minLat: number; maxLat: number; minLng: number; maxLng: number };

/**
 * Round viewport bounds to reduce cache fragmentation
 * Rounds to ~1km precision (2 decimal places) for better cache hit rates
 * This allows nearby viewport positions to share the same cache key
 */
const roundViewportBounds = (bounds: ViewportBounds): ViewportBounds => {
  // At country/region zoom levels (< 10), snap to 1 decimal place (~10km) to maximize cache hits
  // At local zoom levels (>= 10), snap to 2 decimal places (~1km)
  const precision = bounds.zoom < 10 ? 10 : 100;
  return {
    minLat: Math.floor(bounds.minLat * precision) / precision,
    maxLat: Math.ceil(bounds.maxLat * precision) / precision,
    minLng: Math.floor(bounds.minLng * precision) / precision,
    maxLng: Math.ceil(bounds.maxLng * precision) / precision,
    zoom: Math.round(bounds.zoom), // Round zoom to nearest integer
  };
};

type UseMapViewportOptions = {
  /** Debounce delay in milliseconds (default: 1000ms) */
  debounceMs?: number;
  /** Enable viewport tracking (default: true) */
  enabled?: boolean;
  /** Callback when viewport changes */
  onChange?: (bounds: ViewportBounds) => void;
  /** Constrain viewport bounds to a specific geographic region (e.g., Philippines bounds) */
  constraint?: BoundsConstraint;
  /** Show viewport bounding box on map as dashed lines (default: false, useful for debugging) */
  showDebugBox?: boolean;
};

/**
 * Hook to track the current map viewport bounds
 * Useful for viewport-based data loading to handle millions of features
 *
 * The `constraint` option is particularly useful on wide screens where the viewport
 * may extend far beyond the actual content area (e.g., the Philippines on a widescreen
 * monitor will show a lot of empty ocean on the sides). This helps avoid fetching
 * data from regions where no content exists.
 *
 * @example
 * ```tsx
 * import { PHILIPPINES_BOUNDS } from '@/lib/constants/map-bounds';
 *
 * const { bounds, isStable } = useMapViewport({
 *   debounceMs: 500,
 *   constraint: PHILIPPINES_BOUNDS, // Constrain to Philippines to avoid empty ocean areas
 *   onChange: (bounds) => {
 *     console.log('Viewport changed:', bounds);
 *   }
 * });
 *
 * // Use bounds in your infinite query
 * const { data, fetchNextPage, hasNextPage } = useInfiniteQuery({
 *   queryKey: ['naps', bounds],
 *   queryFn: ({ pageParam }) => fetchNAPsInViewport(bounds, pageParam),
 *   initialPageParam: 1,
 *   getNextPageParam: (lastPage) => lastPage.nextPage,
 *   enabled: isStable, // Only fetch when viewport is stable
 * });
 * ```
 */
export const useMapViewport = (options: UseMapViewportOptions = {}) => {
  const { debounceMs = 500, enabled = true, onChange, constraint, showDebugBox = false } = options;
  const { map, isMapLoaded } = useMap();
  const [bounds, setBounds] = useState<ViewportBounds | null>(null);
  const [isStable, setIsStable] = useState(false);
  const debounceTimeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const onChangeRef = useRef(onChange);

  // Keep onChange ref updated
  useEffect(() => {
    onChangeRef.current = onChange;
  }, [onChange]);

  const getBoundsFromMap = useCallback(() => {
    if (!map) return null;

    try {
      const mapBounds = map.getBounds();
      const zoom = map.getZoom();

      let minLat = mapBounds.getSouth();
      let maxLat = mapBounds.getNorth();
      let minLng = mapBounds.getWest();
      let maxLng = mapBounds.getEast();

      // Apply constraint if provided (intersect viewport with constraint bounds)
      if (constraint) {
        minLat = Math.max(minLat, constraint.minLat);
        maxLat = Math.min(maxLat, constraint.maxLat);
        minLng = Math.max(minLng, constraint.minLng);
        maxLng = Math.min(maxLng, constraint.maxLng);
      }

      const rawBounds = { minLat, maxLat, minLng, maxLng, zoom: zoom } satisfies ViewportBounds;

      // Round bounds to improve cache hit rate (reduces cache fragmentation)
      return roundViewportBounds(rawBounds);
    } catch (error) {
      if (process.env.NODE_ENV === "development") {
        console.warn("⚠️ [useMapViewport] Failed to get bounds:", error);
      }
      return null;
    }
  }, [map, constraint]);

  const updateBounds = useCallback(() => {
    const newBounds = getBoundsFromMap();
    if (newBounds) {
      setBounds(newBounds);
      setIsStable(true);
      onChangeRef.current?.(newBounds);
    }
  }, [getBoundsFromMap]);

  useEffect(() => {
    if (!map || !isMapLoaded || !enabled) return;

    // Set initial bounds
    const initialBounds = getBoundsFromMap();
    if (initialBounds) {
      setBounds(() => initialBounds); // eslint-disable-line react-hooks/set-state-in-effect
      setIsStable(() => true);
    }

    const handleMoveStart = () => {
      setIsStable(false);
    };

    const handleMoveEnd = () => {
      // Clear any pending updates
      if (debounceTimeoutRef.current) {
        clearTimeout(debounceTimeoutRef.current);
      }

      // Debounce the update
      debounceTimeoutRef.current = setTimeout(() => {
        updateBounds();
      }, debounceMs);
    };

    // Listen to map events
    map.on("movestart", handleMoveStart);
    map.on("moveend", handleMoveEnd);
    map.on("zoomend", handleMoveEnd);

    // Cleanup
    return () => {
      if (debounceTimeoutRef.current) {
        clearTimeout(debounceTimeoutRef.current);
      }
      map.off("movestart", handleMoveStart);
      map.off("moveend", handleMoveEnd);
      map.off("zoomend", handleMoveEnd);
    };
  }, [map, isMapLoaded, enabled, debounceMs, getBoundsFromMap, updateBounds]);

  // Draw debug box showing viewport bounds
  useEffect(() => {
    if (!map || !isMapLoaded || !showDebugBox || !bounds) return;

    try {
      if (!map.isStyleLoaded()) return;

      const sourceId = "viewport-debug-box";
      const layerId = "viewport-debug-box-layer";

      // Create GeoJSON for the bounding box with slight margin for visibility
      const marginPercent = 0.005; // 0.5% margin
      const lngRange = bounds.maxLng - bounds.minLng;
      const latRange = bounds.maxLat - bounds.minLat;
      const lngMargin = lngRange * marginPercent;
      const latMargin = latRange * marginPercent;

      const boxGeoJSON = {
        type: "FeatureCollection" as const,
        features: [
          {
            type: "Feature" as const,
            geometry: {
              type: "Polygon" as const,
              coordinates: [
                [
                  [bounds.minLng + lngMargin, bounds.minLat + latMargin], // SW corner (inset)
                  [bounds.maxLng - lngMargin, bounds.minLat + latMargin], // SE corner (inset)
                  [bounds.maxLng - lngMargin, bounds.maxLat - latMargin], // NE corner (inset)
                  [bounds.minLng + lngMargin, bounds.maxLat - latMargin], // NW corner (inset)
                  [bounds.minLng + lngMargin, bounds.minLat + latMargin], // Close the loop
                ],
              ],
            },
            properties: {},
          },
        ],
      };

      // Add or update source
      if (map.getSource(sourceId)) {
        const source = map.getSource<GeoJSONSource>(sourceId)!;
        if (source.type === "geojson") {
          // Update existing source data
          source.setData(boxGeoJSON);
        }
      } else {
        map.addSource(sourceId, { type: "geojson", data: boxGeoJSON });

        // Add layer with dashed line style
        map.addLayer({
          id: layerId,
          type: "line",
          source: sourceId,
          paint: { "line-color": "#FF0000", "line-width": 2, "line-opacity": 0.4, "line-dasharray": [4, 4] },
        });
      }

      // Cleanup
      return () => {
        try {
          if (map.isStyleLoaded()) {
            if (map.getLayer(layerId)) {
              map.removeLayer(layerId);
            }
            if (map.getSource(sourceId)) {
              map.removeSource(sourceId);
            }
          }
        } catch (err) {
          if (process.env.NODE_ENV === "development") {
            console.debug("[useMapViewport] cleanup error:", err);
          }
        }
      };
    } catch (err) {
      if (process.env.NODE_ENV === "development") {
        console.debug("[useMapViewport] debugBox error:", err);
      }
    }
  }, [map, isMapLoaded, showDebugBox, bounds]);

  return {
    /** Current viewport bounds (null if map not ready) */
    bounds,
    /** Whether the viewport is stable (not moving/zooming) */
    isStable,
    /** Manually trigger bounds update */
    refreshBounds: updateBounds,
  };
};
