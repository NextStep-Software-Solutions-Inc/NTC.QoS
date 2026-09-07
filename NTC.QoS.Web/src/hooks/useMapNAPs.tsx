"use client";

import { memo, useCallback, useEffect, useLayoutEffect, useMemo, useRef, useState } from "react";
import type * as maptilersdk from "@maptiler/sdk";
import { Popup, type GeoJSONSource, type MapLibreEvent } from "@maptiler/sdk";
import type { InfiniteData } from "@tanstack/react-query";
import { createRoot } from "react-dom/client";
import { toast } from "sonner";

import { getPublicNAPsInfiniteRequest } from "@/components/features/NAP/handlers/getPublicNAPsInfiniteRequest";
import type { NAPViewport } from "@/components/features/NAP/types/NAP";
import { createInfiniteQueryHookFromTemplate } from "@/hooks/createInfiniteQueryHookFromTemplate";
import { useMap } from "@/hooks/useMap";
import { useMapViewport } from "@/hooks/useMapViewport";
import { useNAPGeoJSONWorker, type GeoJSONFeatureCollection } from "@/hooks/useNAPGeoJSONWorker";
import { PHILIPPINES_BOUNDS } from "@/lib/constants/map-bounds";
import type { PaginationResponse } from "@/types/reusable/api";

const useGetNAPsInfinite = createInfiniteQueryHookFromTemplate(getPublicNAPsInfiniteRequest);

type LayerId = "NAPs-cluster" | "NAPs-cluster-count" | "NAPs-unclustered";

const PAGE_SIZE = 10000; // Items per page
const clusterRadiusStep = ["step", ["get", "point_count"], 10, 10, 13, 30, 16] satisfies
  | maptilersdk.DataDrivenPropertyValueSpecification<number>
  | undefined;

/**
 * Options for NAP filtering
 */
type NAPOptions = {
  serviceProviderIds?: string[]; // Provider IDs for filtering
  regionIds?: string[]; // Region IDs for filtering
};

export const useMapNAPs = (visibleLayers: Record<LayerId, boolean>, options?: NAPOptions) => {
  const { map, isMapLoaded } = useMap();

  // Track viewport bounds for viewport-based loading
  // Note: bounds are already rounded in useMapViewport for better cache hit rates
  const { bounds: viewportBounds, isStable: isViewportStable } = useMapViewport({
    debounceMs: 800, // Wait 800ms after user stops moving map
    enabled: isMapLoaded && Object.values(visibleLayers).some((v) => v),
    constraint: PHILIPPINES_BOUNDS, // Constrain to Philippines bounds to avoid fetching data from empty ocean areas
    showDebugBox: process.env.NODE_ENV === "development", // Show viewport bounding box in dev mode
  });

  const [accumulatedGeoJSON, setAccumulatedGeoJSON] = useState<GeoJSONFeatureCollection>({
    type: "FeatureCollection",
    features: [],
  });
  const [totalLoadedCount, setTotalLoadedCount] = useState(0);
  const [isInitialLoad, setIsInitialLoad] = useState(true);
  const updateTimeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  // Keep a ref to the latest accumulated GeoJSON so event handlers stay stable
  // without needing accumulatedGeoJSON as a dependency on every data update
  const accumulatedGeoJSONRef = useRef(accumulatedGeoJSON);
  useLayoutEffect(() => {
    accumulatedGeoJSONRef.current = accumulatedGeoJSON;
  }, [accumulatedGeoJSON]);

  // Infinite query for viewport-based NAP loading using v2 API
  const { data, fetchNextPage, hasNextPage, isFetchingNextPage, isLoading } = useGetNAPsInfinite({
    enabled: isMapLoaded && Object.values(visibleLayers).some((v) => v) && isViewportStable && !!viewportBounds,
    pathParams: {},
    queryParams: {
      pageSize: PAGE_SIZE,
      serviceProviderIds: options?.serviceProviderIds, // Provider filtering
      regionIds: options?.regionIds, // Region filtering
      // Viewport bounds for spatial filtering (v2 API) - already rounded for better caching
      ...(viewportBounds && {
        minLat: viewportBounds.minLat,

        maxLat: viewportBounds.maxLat,
        minLng: viewportBounds.minLng,
        maxLng: viewportBounds.maxLng,
        zoom: viewportBounds.zoom, // For level-of-detail optimization
      }),
    },
  });

  // Callback when worker finishes processing NAPs
  // Worker handles deduplication, so we just update the state directly
  const handleNAPsProcessed = useCallback((geojson: GeoJSONFeatureCollection, count: number) => {
    // Worker already merged and deduplicated, just update the state
    setAccumulatedGeoJSON(geojson);
    setTotalLoadedCount((prev) => prev + count);

    // Debounce map updates to batch DOM operations
    if (updateTimeoutRef.current) {
      clearTimeout(updateTimeoutRef.current);
    }

    updateTimeoutRef.current = setTimeout(() => {
      // Map update will happen via the effect
    }, 250); // 250ms debounce
  }, []);

  // Initialize Web Worker
  const { processNAPs } = useNAPGeoJSONWorker(handleNAPsProcessed);

  // Extract all NAPs from all loaded pages
  const infiniteData = data as InfiniteData<PaginationResponse<NAPViewport>> | undefined;

  const allLoadedNAPs = useMemo<NAPViewport[]>(() => {
    if (!infiniteData?.pages) return [];
    return infiniteData.pages.flatMap((page) => page.data);
  }, [infiniteData]);

  // Get total expected count
  const totalExpectedCount = useMemo(() => {
    if (!infiniteData?.pages || infiniteData.pages.length === 0) return 0;
    return infiniteData.pages[0]?.totalRows ?? 0;
  }, [infiniteData]);

  // Reset accumulated NAPs when filter options change
  useEffect(() => {
    const currentSerialized = JSON.stringify({
      serviceProviderIds: options?.serviceProviderIds,
      regionIds: options?.regionIds,
    });
    if (previousOptionsRef.current !== currentSerialized) {
      setAccumulatedGeoJSON({ type: "FeatureCollection", features: [] });
      setTotalLoadedCount(0);
      setIsInitialLoad(true);
      previousOptionsRef.current = currentSerialized;
    }
  }, [options?.serviceProviderIds, options?.regionIds]);

  // Track viewport changes but DON'T clear data
  // Instead, keep existing data and merge new data from new viewport
  // This allows seamless panning without losing already-loaded NAPs
  const previousViewportRef = useRef<typeof viewportBounds>(null);
  const previousOptionsRef = useRef<string>("");

  useEffect(() => {
    if (viewportBounds && isViewportStable) {
      const viewportChanged =
        !previousViewportRef.current ||
        previousViewportRef.current.minLat !== viewportBounds.minLat ||
        previousViewportRef.current.maxLat !== viewportBounds.maxLat ||
        previousViewportRef.current.minLng !== viewportBounds.minLng ||
        previousViewportRef.current.maxLng !== viewportBounds.maxLng;

      if (viewportChanged) {
        // Viewport changed - reset counters but KEEP existing GeoJSON data
        // New data will be merged by the worker with deduplication
        setTotalLoadedCount(() => 0); // eslint-disable-line react-hooks/set-state-in-effect
        setIsInitialLoad(() => true);
        previousViewportRef.current = viewportBounds;
      }
    }
  }, [
    viewportBounds?.minLat,
    viewportBounds?.maxLat,
    viewportBounds?.minLng,
    viewportBounds?.maxLng,
    isViewportStable,
    viewportBounds,
  ]);

  // Process NAPs whenever new data is loaded
  // Pass existing features to worker for intelligent deduplication
  useEffect(() => {
    if (allLoadedNAPs.length > 0 && allLoadedNAPs.length > totalLoadedCount) {
      // Get only the newly loaded NAPs
      const newNAPs = allLoadedNAPs.slice(totalLoadedCount);
      // Pass existing features to worker for deduplication
      processNAPs(newNAPs, accumulatedGeoJSON.features);
    }
  }, [allLoadedNAPs, processNAPs, totalLoadedCount, accumulatedGeoJSON.features]);

  // Max pages to auto-fetch per viewport to prevent network flooding (zoom < 10: 2 pages, zoom >= 10: 4 pages)
  const maxAutoPages = (viewportBounds?.zoom ?? 5) < 10 ? 2 : 4;
  const loadedPageCount = (data as any)?.pages?.length ?? 0;

  // Auto-fetch next page up to maxAutoPages limit
  useEffect(() => {
    if (hasNextPage && !isFetchingNextPage && isViewportStable && loadedPageCount < maxAutoPages) {
      void fetchNextPage();
    }
  }, [hasNextPage, isFetchingNextPage, isViewportStable, fetchNextPage, loadedPageCount, maxAutoPages]);

  // Handle loading indicators with aggressive rate limiting
  const lastToastUpdateRef = useRef<number>(0);
  const toastUpdateTimeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const lastProgressRef = useRef<number>(0);

  // Clean up all pending timeouts on unmount to prevent memory leaks
  useEffect(() => {
    return () => {
      if (updateTimeoutRef.current) {
        clearTimeout(updateTimeoutRef.current);
        updateTimeoutRef.current = null;
      }
      if (toastUpdateTimeoutRef.current) {
        clearTimeout(toastUpdateTimeoutRef.current);
        toastUpdateTimeoutRef.current = null;
      }
    };
  }, []);

  const isAnyNAPLayerVisible = Object.values(visibleLayers).some(Boolean);

  useEffect(() => {
    // If layer is toggled off, immediately dismiss any active loading toast and cancel pending updates
    if (!isAnyNAPLayerVisible) {
      if (toastUpdateTimeoutRef.current) {
        clearTimeout(toastUpdateTimeoutRef.current);
        toastUpdateTimeoutRef.current = null;
      }
      toast.dismiss("loading-NAPs");
      return;
    }

    const now = Date.now();
    const timeSinceLastUpdate = now - lastToastUpdateRef.current;
    const progress = totalExpectedCount > 0 ? Math.round((totalLoadedCount / totalExpectedCount) * 100) : 0;

    // Clear any pending toast updates
    if (toastUpdateTimeoutRef.current) {
      clearTimeout(toastUpdateTimeoutRef.current);
      toastUpdateTimeoutRef.current = null;
    }

    // Aggressive debouncing: minimum 1 second between updates
    if (timeSinceLastUpdate < 1000 && isFetchingNextPage) {
      // Only update if progress changed by at least 10%
      if (Math.abs(progress - lastProgressRef.current) < 10) {
        return;
      }
    }

    // Debounce all toast updates
    toastUpdateTimeoutRef.current = setTimeout(() => {
      if (isInitialLoad && isLoading) {
        toast.loading("Loading NAPs...", { id: "loading-NAPs", description: `Starting to load Network Access Points` });
        lastToastUpdateRef.current = Date.now();
      } else if (isFetchingNextPage) {
        toast.loading(`Loading NAPs... ${progress}%`, {
          id: "loading-NAPs",
          description: `Loaded ${totalLoadedCount.toLocaleString()} of ${totalExpectedCount.toLocaleString()} NAPs`,
        });
        lastToastUpdateRef.current = Date.now();
        lastProgressRef.current = progress;
      } else if (!hasNextPage && totalLoadedCount > 0 && !isInitialLoad) {
        toast.success("All NAPs loaded!", {
          id: "loading-NAPs",
          description: `Successfully loaded ${totalLoadedCount.toLocaleString()} Network Access Points`,
          duration: 3000,
        });
        setIsInitialLoad(false);
        lastToastUpdateRef.current = Date.now();
      } else if (!isLoading && !isFetchingNextPage && !hasNextPage) {
        toast.dismiss("loading-NAPs");
      }
    }, 300); // 300ms debounce

    return () => {
      if (toastUpdateTimeoutRef.current) {
        clearTimeout(toastUpdateTimeoutRef.current);
        toastUpdateTimeoutRef.current = null;
      }
    };
  }, [isLoading, isFetchingNextPage, hasNextPage, totalLoadedCount, totalExpectedCount, isInitialLoad, isAnyNAPLayerVisible]);

  const addNAPSourceToMap = useCallback(() => {
    if (!map || !isMapLoaded || !map.isStyleLoaded()) return;

    try {
      if (!map.isStyleLoaded()) return;

      if (!map.getSource("NAPs")) {
        if (process.env.NODE_ENV === "development") {
          console.log("🗺️ [NAPs Debug] Adding NAPs source with", accumulatedGeoJSONRef.current.features.length, "features");
        }

        map.addSource("NAPs", {
          type: "geojson",
          // Use ref so the handler stays stable without accumulatedGeoJSON as a dep
          data: accumulatedGeoJSONRef.current,
          cluster: true,
          clusterMaxZoom: 16,
          clusterRadius: 30,
          clusterProperties: {
            clusterColor: ["coalesce", ["get", "ispColor"], "#808080"], // Default to gray
          },
          generateId: true,
        });
      }
    } catch {
      // Style not ready yet
    }
  }, [map, isMapLoaded]);

  const findBeforeLayer = useCallback(() => {
    try {
      if (!map || !map.isStyleLoaded()) return undefined;
      const layers = map.getStyle()?.layers;
      if (!layers) return undefined;

      // Look for place labels specifically (not road labels)
      for (const layer of layers) {
        if (layer.type === "symbol") {
          const layerId = layer.id.toLowerCase();
          if (
            layerId.includes("place") ||
            layerId.includes("settlement") ||
            layerId.includes("city") ||
            layerId.includes("town")
          ) {
            return layer.id;
          }
        }
      }

      // Fallback: find any label layer
      for (const layer of layers) {
        if (layer.type === "symbol" && layer.id.toLowerCase().includes("label")) {
          return layer.id;
        }
      }

      return undefined;
    } catch {
      return undefined;
    }
  }, [map]);

  const ensureNAPSLayers = useCallback(() => {
    if (!map || !isMapLoaded || !map.isStyleLoaded()) return;

    try {
      addNAPSourceToMap();
      if (!map.getSource("NAPs")) return;

      const beforeId = findBeforeLayer();

      const layers: Array<{ id: LayerId; config: maptilersdk.LayerSpecification }> = [
        {
          id: "NAPs-cluster",
          config: {
            id: "NAPs-cluster",
            type: "circle",
            source: "NAPs",
            filter: ["has", "point_count"],
            paint: {
              "circle-color": ["get", "clusterColor"],
              "circle-radius": clusterRadiusStep,
              "circle-opacity": 0.5,
            },
            layout: { visibility: visibleLayers["NAPs-cluster"] ? "visible" : "none" },
          },
        },
        {
          id: "NAPs-cluster-count",
          config: {
            id: "NAPs-cluster-count",
            type: "symbol",
            source: "NAPs",
            filter: ["has", "point_count"],
            layout: {
              "text-field": "{point_count_abbreviated}",
              "text-font": ["Open Sans Bold", "Arial Unicode MS Bold"],
              "text-size": 12,
              visibility: visibleLayers["NAPs-cluster-count"] ? "visible" : "none",
            },
            paint: { "text-color": "#fff", "text-opacity": 1 },
          },
        },
        {
          id: "NAPs-unclustered",
          config: {
            id: "NAPs-unclustered",
            type: "circle",
            source: "NAPs",
            filter: ["!has", "point_count"],
            paint: { "circle-radius": 8, "circle-color": ["get", "ispColor"] },
            layout: { visibility: visibleLayers["NAPs-unclustered"] ? "visible" : "none" },
          },
        },
      ];

      layers.forEach(({ id, config }) => {
        if (!map.getLayer(id)) {
          map.addLayer(config, beforeId);
        }
      });
    } catch {
      // Style not ready yet
    }
  }, [map, isMapLoaded, addNAPSourceToMap, findBeforeLayer, visibleLayers]);

  const setLayerVisible = useCallback(
    (layer: LayerId, visible: boolean) => {
      try {
        if (map && map.isStyleLoaded()) {
          if (!map.getLayer(layer)) {
            ensureNAPSLayers();
          }
          if (map.getLayer(layer)) {
            map.setLayoutProperty(layer, "visibility", visible ? "visible" : "none");
          }
        }
      } catch {
        // Style not ready yet
      }
    },
    [map, ensureNAPSLayers],
  );

  useEffect(() => {
    Object.entries(visibleLayers).forEach(([layer, visible]) => {
      setLayerVisible(layer as LayerId, visible);
    });
  }, [map, setLayerVisible, visibleLayers]);

  useEffect(() => {
    if (!map || !isMapLoaded) return;

    let timer: ReturnType<typeof setTimeout> | null = null;

    const setupLayers = () => {
      if (map.isStyleLoaded()) {
        ensureNAPSLayers();
      }
    };

    if (map.isStyleLoaded()) {
      setupLayers();
    } else {
      map.once("styledata", setupLayers);
      map.once("idle", setupLayers);
      timer = setTimeout(setupLayers, 100);
    }

    const handleStyleData = () => {
      setupLayers();
    };

    const handleSourceData = (e: { sourceId?: string }) => {
      if (e?.sourceId === "NAPs") {
        setupLayers();
      }
    };

    map.on("styledata", handleStyleData);
    map.on("sourcedata", handleSourceData);

    return () => {
      if (timer) clearTimeout(timer);
      map.off("styledata", setupLayers);
      map.off("idle", setupLayers);
      map.off("styledata", handleStyleData);
      map.off("sourcedata", handleSourceData);
    };
  }, [ensureNAPSLayers, isMapLoaded, map]);

  // Dedicated effect to update NAPs source data whenever accumulatedGeoJSON changes.
  // Calls source.setData() directly without re-registering event listeners, keeping
  // the layers visible during data updates (no blank flash).
  useEffect(() => {
    if (!map || !isMapLoaded || !map.isStyleLoaded()) return;

    try {
      if (!map.isStyleLoaded()) return;
      const source = map.getSource<GeoJSONSource>("NAPs");
      if (source) {
        if (process.env.NODE_ENV === "development") {
          console.log("🗺️ [NAPs Debug] Updating NAPs source with", accumulatedGeoJSON.features.length, "total features");
        }
        source.setData(accumulatedGeoJSON);
      }
    } catch {
      // Style not ready yet
    }
  }, [map, isMapLoaded, accumulatedGeoJSON]);

  const handleHoverEffect = useCallback(() => {
    if (!map || !isMapLoaded || !map.isStyleLoaded()) return;

    const popup = new Popup({
      closeButton: false,
      closeOnClick: false,
      offset: 10,
      className: "bg-transparent m-0 p-0",
    });

    let hoveredFeatureId: string | number | undefined;

    const handleMouseMove = (e: maptilersdk.MapMouseEvent) => {
      const features = map.queryRenderedFeatures(e.point, { layers: ["NAPs-unclustered"] });

      if (!features.length) {
        popup.remove();
        hoveredFeatureId = undefined;
        return;
      }

      const feature = features[0] as GeoJSON.Feature<GeoJSON.Point, { isp?: string; ispColor?: string }>;
      if (hoveredFeatureId === feature?.id) return;

      hoveredFeatureId = feature?.id;

      const coordinates = feature.geometry.type === "Point" ? feature.geometry.coordinates.slice() : [0, 0];
      const provider = feature.properties.isp ?? "Unknown Provider";
      const ispColor = feature.properties.ispColor ?? "#000";

      const div = document.createElement("div");
      createRoot(div).render(<NAPInformation provider={provider} color={ispColor} popup={popup} />);
      popup
        .setLngLat(coordinates as [number, number])
        .setDOMContent(div)
        .addTo(map);
    };

    const handleMouseLeave = () => {
      popup.remove();
      hoveredFeatureId = undefined;
    };

    map.on("mousemove", "NAPs-unclustered", handleMouseMove);
    map.on("mouseleave", "NAPs-unclustered", handleMouseLeave);

    return () => {
      map.off("mousemove", "NAPs-unclustered", handleMouseMove);
      map.off("mouseleave", "NAPs-unclustered", handleMouseLeave);
      popup.remove();
    };
  }, [map, isMapLoaded]);

  useEffect(() => {
    handleHoverEffect();
  }, [handleHoverEffect]);

  return null;
};

const NAPInformation = memo(({ color, provider }: { color?: string; provider?: string; popup: Popup }) => {
  return (
    <div className="rounded-xl shadow-3xl bg-white p-4 text-base">
      <span style={{ color: color ?? "#000" }} className="font-semibold">
        {provider}
      </span>
      <br />
      <span className="text-sm text-gray-500">Network Access Point</span>
    </div>
  );
});
NAPInformation.displayName = "NAPInformation";
