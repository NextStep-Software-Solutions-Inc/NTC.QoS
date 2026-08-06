"use client";

import { useCallback, useEffect, useLayoutEffect, useMemo, useRef, useState } from "react";
import {
  type GeoJSONSource,
  type MapLibreEvent,
  type MapSourceDataType,
  type SourceSpecification,
} from "@maptiler/sdk";
import type { InfiniteData } from "@tanstack/react-query";
import { toast } from "sonner";

import { getPublicSpeedtestsInfiniteRequest } from "@/components/features/speedtest/handlers/getPublicSpeedtestsInfiniteRequest";
import { useMap } from "@/hooks/useMap";
import { useMapViewport } from "@/hooks/useMapViewport";
import { useSpeedTestGeoJSONWorker, type SpeedTestGeoJSONFeatureCollection } from "@/hooks/useSpeedTestGeoJSONWorker";
import { PHILIPPINES_BOUNDS } from "@/lib/constants/map-bounds";
import type { PaginationResponse } from "@/types/reusable/api";
import { type SpeedTestViewport } from "@/workers/speedtest-processor.worker";
import { createInfiniteQueryHookFromTemplate } from "./createInfiniteQueryHookFromTemplate";

const PAGE_SIZE = 2000; // Items per page

type LayerId = "speedtests-circles" | "speedtests-heatmap";

const useGetSpeedtestsInfinite = createInfiniteQueryHookFromTemplate(getPublicSpeedtestsInfiniteRequest);

/**
 * Custom data layer IDs (from other hooks) that must always render BELOW speedtest.
 *
 * Desired z-order (bottom → top):
 *   fiber (useMapFibers)
 *     → signal-coverage / radio coverage (useMapCells admin)
 *     → cellsite clusters / circles / symbols (useMapCells)
 *     → cell heatmap / point-view (useMapCells admin)
 *     → NAPs (useMapNAPs)
 *     → speedtest  ← enforced here
 *     → base-map labels (place names, country names, …)
 *
 * Enforcement scans this list to find the highest-z "floor" layer, then moves both
 * speedtest layers to just above it — regardless of where "Tunnel" (the fiber anchor
 * in MapTiler TOPO style) sits relative to base-map symbol layers.
 *
 * NOTE: keep this list in sync with the layer IDs added by useMapFibers,
 * useMapCells, and useMapNAPs.
 */
const LAYERS_BELOW_SPEEDTEST = [
  // Fiber (useMapFibers — inserted before "Tunnel" in TOPO style)
  "fiber", "fiber-line", "highlighted-fiber",
  // Radio network coverage (useMapCells admin — inserted before cells-clusters)
  "signal-coverage-circles",
  // Cellsite cluster / circle / symbol layers (useMapCells)
  "cells-clusters", "cells-cluster-count", "cells-circles", "cells-symbols",
  // Cell heatmap / point view (useMapCells admin — inserted before first place label)
  "cells-heatmap", "cells-point-view",
  // NAP layers (useMapNAPs)
  "NAPs-cluster", "NAPs-cluster-count", "NAPs-unclustered",
] as const;

/**
 * Custom symbol-type layer IDs excluded when falling back to a base-map symbol anchor.
 * Prevents our own data-label layers from being mistaken for place-name labels.
 */
const CUSTOM_SYMBOL_LAYER_IDS = new Set([
  "cells-cluster-count", "cells-symbols", "NAPs-cluster-count",
]);

/**
 * Map source IDs whose data events can affect speedtest layer positioning.
 * Kept in sync with the hooks that add the layers above.
 */
const ORDERING_RELEVANT_SOURCES = new Set([
  "speedtests",
  "cells",
  "cells-heatmap-source",
  "NAPs",
  "map-tiles",
  "highlighted-fiber",
  "signal-coverage-source",
]);

/**
 * Options for Speedtest filtering
 */
type SpeedtestOptions = {
  serviceProviderIds?: string[]; // Provider IDs for filtering
  minDownloadSpeed?: number;
  maxDownloadSpeed?: number;
  minUploadSpeed?: number;
  maxUploadSpeed?: number;
  startDate?: string;
  endDate?: string;
};

export const useMapSpeedtests = (visibleLayers: Record<LayerId, boolean>, options?: SpeedtestOptions) => {
  const { map, isMapLoaded } = useMap();

  // Track viewport bounds for viewport-based loading
  // Note: bounds are already rounded in useMapViewport for better cache hit rates
  const { bounds: viewportBounds, isStable: isViewportStable } = useMapViewport({
    debounceMs: 800,
    enabled: isMapLoaded && Object.values(visibleLayers).some((v) => v),
    constraint: PHILIPPINES_BOUNDS, // Constrain to Philippines bounds to avoid fetching data from empty ocean areas
    showDebugBox: process.env.NODE_ENV === "development", // Show viewport bounding box in dev mode
  });

  const [accumulatedGeoJSON, setAccumulatedGeoJSON] = useState<SpeedTestGeoJSONFeatureCollection>({
    type: "FeatureCollection",
    features: [],
  });
  const [totalLoadedCount, setTotalLoadedCount] = useState(0);
  const lastToastUpdateRef = useRef(0);
  const lastProgressPercentRef = useRef(0);
  const updateTimeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  // Keep a ref to the latest accumulated GeoJSON so the setup effect stays stable
  // without needing accumulatedGeoJSON as a dependency on every data update
  const accumulatedGeoJSONRef = useRef(accumulatedGeoJSON);
  useLayoutEffect(() => {
    accumulatedGeoJSONRef.current = accumulatedGeoJSON;
  }, [accumulatedGeoJSON]);

  // Infinite query for viewport-based SpeedTest loading using v2 API
  const {
    data: speedtestsData,
    fetchNextPage,
    hasNextPage,
    isFetchingNextPage,
    isLoading,
  } = useGetSpeedtestsInfinite({
    enabled: isMapLoaded && Object.values(visibleLayers).some((v) => v) && isViewportStable && !!viewportBounds,
    pathParams: {},
    queryParams: {
      pageSize: PAGE_SIZE,
      serviceProviderIds: options?.serviceProviderIds, // Provider filtering
      minDownloadSpeed: options?.minDownloadSpeed,
      maxDownloadSpeed: options?.maxDownloadSpeed,
      minUploadSpeed: options?.minUploadSpeed,
      maxUploadSpeed: options?.maxUploadSpeed,
      startDate: options?.startDate,
      endDate: options?.endDate,
      // Viewport bounds for spatial filtering (v2 API) - bounds already rounded in useMapViewport
      ...(viewportBounds && {
        minLat: viewportBounds.minLat,
        maxLat: viewportBounds.maxLat,
        minLng: viewportBounds.minLng,
        maxLng: viewportBounds.maxLng,
        zoom: viewportBounds.zoom, // For level-of-detail optimization
      }),
    },
  });

  // Callback when worker finishes processing SpeedTests
  // Worker handles deduplication, so we just update the state directly
  const handleSpeedTestsProcessed = useCallback((geojson: SpeedTestGeoJSONFeatureCollection, count: number) => {
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
  const { processSpeedTests } = useSpeedTestGeoJSONWorker(handleSpeedTestsProcessed);

  // Accumulate speedtests from all pages
  const accumulatedSpeedtests = useMemo(() => {
    if (speedtestsData) {
      const infiniteData = speedtestsData as unknown as InfiniteData<PaginationResponse<SpeedTestViewport>>;
      return infiniteData.pages.flatMap((page) => page.data);
    }
    return [];
  }, [speedtestsData]);

  const loadingProgress = useMemo(() => {
    if (speedtestsData) {
      const infiniteData = speedtestsData as unknown as InfiniteData<PaginationResponse<SpeedTestViewport>>;
      const allSpeedtests = accumulatedSpeedtests;
      const firstPage = infiniteData.pages[0];
      if (firstPage) {
        return { current: allSpeedtests.length, total: firstPage.totalRows };
      }
    }
    return { current: 0, total: 0 };
  }, [speedtestsData, accumulatedSpeedtests]);

  // Track viewport changes but DON'T clear data
  // Instead, keep existing data and merge new data from new viewport
  // This allows seamless panning without losing already-loaded SpeedTests
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
        // New data will be merged with deduplication
        setTotalLoadedCount(() => 0); // eslint-disable-line react-hooks/set-state-in-effect
        previousViewportRef.current = viewportBounds;
      }
    }
  }, [
    viewportBounds?.minLat,
    viewportBounds?.maxLat,
    viewportBounds?.minLng,
    viewportBounds?.maxLng,
    viewportBounds,
    isViewportStable,
  ]);

  // Clear accumulated data when filter options change (speed ranges, dates, providers)
  // This ensures fresh data is loaded for new filter criteria
  // Use JSON serialization to properly compare objects and arrays
  useEffect(() => {
    const currentSerialized = JSON.stringify({
      minDownloadSpeed: options?.minDownloadSpeed,
      maxDownloadSpeed: options?.maxDownloadSpeed,
      minUploadSpeed: options?.minUploadSpeed,
      maxUploadSpeed: options?.maxUploadSpeed,
      startDate: options?.startDate,
      endDate: options?.endDate,
      serviceProviderIds: options?.serviceProviderIds,
    });

    const previousSerialized = previousOptionsRef.current;

    if (previousSerialized !== currentSerialized) {
      // Filter options changed - clear accumulated data to load fresh results
      setAccumulatedGeoJSON({ type: "FeatureCollection", features: [] });
      setTotalLoadedCount(0);
      previousOptionsRef.current = currentSerialized;
    }
  }, [options]);

  // Process speedtests whenever new data is loaded
  // Pass existing features to worker for intelligent deduplication
  useEffect(() => {
    if (accumulatedSpeedtests.length > 0 && accumulatedSpeedtests.length > totalLoadedCount) {
      // Get only the newly loaded speedtests
      const newSpeedtests = accumulatedSpeedtests.slice(totalLoadedCount);
      // Pass existing features to worker for deduplication
      processSpeedTests(newSpeedtests, accumulatedGeoJSON.features);
    }
  }, [accumulatedSpeedtests, processSpeedTests, totalLoadedCount, accumulatedGeoJSON.features]);

  // Max pages to auto-fetch per viewport to prevent network flooding (zoom < 10: 2 pages, zoom >= 10: 4 pages)
  const maxAutoPages = (viewportBounds?.zoom ?? 5) < 10 ? 2 : 4;
  const loadedPageCount = (speedtestsData as any)?.pages?.length ?? 0;

  // Auto-fetch next page up to maxAutoPages limit
  useEffect(() => {
    if (hasNextPage && !isFetchingNextPage && isMapLoaded && isViewportStable && loadedPageCount < maxAutoPages) {
      void fetchNextPage();
    }
  }, [hasNextPage, isFetchingNextPage, fetchNextPage, isMapLoaded, isViewportStable, loadedPageCount, maxAutoPages]);

  // Show loading toast with progress
  useEffect(() => {
    const now = Date.now();
    const { current, total } = loadingProgress;

    if (total === 0) return;

    const progressPercent = Math.floor((current / total) * 100);
    const timeSinceLastUpdate = now - lastToastUpdateRef.current;
    const progressChanged = Math.abs(progressPercent - lastProgressPercentRef.current) >= 10;

    if (isLoading || isFetchingNextPage) {
      if (timeSinceLastUpdate >= 1000 || progressChanged) {
        toast.loading(`Loading speedtests... ${progressPercent}%`, {
          id: "loading-speedtests",
          description: `${current.toLocaleString()} of ${total.toLocaleString()} speedtests loaded`,
        });
        lastToastUpdateRef.current = now;
        lastProgressPercentRef.current = progressPercent;
      }
    } else if (current > 0) {
      toast.success("Speedtests loaded successfully!", {
        id: "loading-speedtests",
        description: `${current.toLocaleString()} speedtests loaded`,
      });
    }
  }, [isLoading, isFetchingNextPage, loadingProgress]);

  /**
   * Ensures speedtest layers render above all custom data layers (fiber, cells, NAPs)
   * but below base-map place-name labels.
   *
   * Strategy (two-phase):
   *
   * Phase 1 — explicit floor anchor (preferred):
   *   Scan LAYERS_BELOW_SPEEDTEST to find the highest-z layer that currently exists
   *   in the style.  Use the first non-speedtest layer immediately above it as the
   *   insertion anchor.  This is robust even when fiber's "Tunnel" anchor in the
   *   MapTiler TOPO style sits above the base-map symbol layers, which would make
   *   the symbol-scan approach (Phase 2) place speedtest below fiber.
   *
   * Phase 2 — base-map symbol fallback:
   *   If no "floor" reference layers are present yet (very early in load), fall back
   *   to scanning from the END for the topmost non-custom base-map symbol layer.
   *
   * Moving heatmap first then circles before the same anchor produces:
   *   [...floor layer..., speedtests-heatmap, speedtests-circles, <anchor>, ...]
   */
  const enforceSpeedtestLayerOrder = useCallback(() => {
    if (!map || !map.isStyleLoaded()) return;

    const style = map.getStyle();
    if (!style?.layers) return;

    // Skip if neither speedtest layer exists yet
    if (!map.getLayer("speedtests-circles") && !map.getLayer("speedtests-heatmap")) return;

    const layerIds = style.layers.map((l) => l.id);

    // Phase 1: find the highest-z layer from LAYERS_BELOW_SPEEDTEST.
    let floorIdx = -1;
    for (const id of LAYERS_BELOW_SPEEDTEST) {
      const idx = layerIds.indexOf(id);
      if (idx > floorIdx) floorIdx = idx;
    }

    let anchorId: string | undefined;

    if (floorIdx !== -1) {
      // Walk forward from just above the floor layer to find the first non-speedtest
      // layer.  This is the slot where speedtest layers will be inserted.
      for (let i = floorIdx + 1; i < layerIds.length; i++) {
        const id = layerIds[i];
        if (id !== "speedtests-heatmap" && id !== "speedtests-circles") {
          anchorId = id;
          break;
        }
      }
    }

    // Phase 2 fallback: topmost non-custom base-map symbol layer.
    if (!anchorId) {
      for (let i = style.layers.length - 1; i >= 0; i--) {
        const layer = style.layers[i];
        if (layer && layer.type === "symbol" && !CUSTOM_SYMBOL_LAYER_IDS.has(layer.id)) {
          anchorId = layer.id;
          break;
        }
      }
    }

    if (!anchorId) return;

    // Move heatmap first (renders below circles), then circles.
    // Both are moved before the same anchor, so the final order is:
    //   [..., speedtests-heatmap, speedtests-circles, <anchorId>, ...]
    for (const id of ["speedtests-heatmap", "speedtests-circles"] as LayerId[]) {
      if (!map.getLayer(id)) continue;
      try {
        map.moveLayer(id, anchorId);
      } catch (err) {
        // Layer may not be ready yet; will be retried on the next scheduled call
        if (process.env.NODE_ENV === "development") {
          console.debug(`[useMapSpeedtests] moveLayer(${id}) failed, will retry:`, err);
        }
      }
    }
  }, [map]);

  useEffect(() => {
    if (!map || !isMapLoaded) return;

    const addSpeedtestSourceToMap = () => {
      if (!map || !map.isStyleLoaded()) return;
      if (!map.getSource("speedtests")) {
        // Use ref so the handler stays stable without accumulatedGeoJSON as a dep
        map.addSource("speedtests", { type: "geojson", data: accumulatedGeoJSONRef.current });
      }
      // Data updates are handled by a dedicated effect to avoid listener re-registration
    };

    // Find the best insertion point for speedtest layers.
    // Uses LAYERS_BELOW_SPEEDTEST to find the highest custom data layer and inserts
    // speedtest just above it. Falls back to the topmost base-map symbol if no
    // reference layers exist yet (e.g. first load before other hooks have run).
    const findBeforeLayer = () => {
      const layers = map.getStyle()?.layers;
      if (!layers) return undefined;

      const layerIds = layers.map((l) => l.id);

      // Phase 1: find the first non-speedtest layer just above the highest
      // known "below speedtest" layer.
      let floorIdx = -1;
      for (const id of LAYERS_BELOW_SPEEDTEST) {
        const idx = layerIds.indexOf(id);
        if (idx > floorIdx) floorIdx = idx;
      }
      if (floorIdx !== -1) {
        for (let i = floorIdx + 1; i < layerIds.length; i++) {
          const id = layerIds[i];
          if (id !== "speedtests-heatmap" && id !== "speedtests-circles") return id;
        }
      }

      // Phase 2 fallback: topmost non-custom base-map symbol layer.
      for (let i = layers.length - 1; i >= 0; i--) {
        const layer = layers[i];
        if (
          layer &&
          layer.type === "symbol" &&
          layer.id !== "speedtests-circles" &&
          layer.id !== "speedtests-heatmap" &&
          !CUSTOM_SYMBOL_LAYER_IDS.has(layer.id)
        ) {
          return layer.id;
        }
      }
      return undefined;
    };

    const addSpeedtestsLayers = (
      e: MapLibreEvent<unknown> & {
        dataType: "source";
        isSourceLoaded: boolean;
        source: SourceSpecification;
        sourceId: string;
        sourceDataType: MapSourceDataType;
        sourceDataChanged?: boolean;
        tile: unknown;
      } & {},
    ) => {
      if (e.sourceId !== "speedtests" || !e.isSourceLoaded) return;

      const beforeId = findBeforeLayer();

      if (!map.getLayer("speedtests-circles")) {
        console.info("Adding speedtests circles layer to map");
        map.addLayer(
          {
            id: "speedtests-circles",
            type: "circle",
            source: "speedtests",
            // minzoom: 9,
            filter: ["all", [">", ["get", "downloadSpeed"], 0]],
            paint: {
              // Use the ispColor directly for the circle color
              "circle-color": ["get", "ispColor"],
              // Adjust the radius of the circles based on downloadSpeed
              "circle-radius": [
                "interpolate",
                ["linear"],
                ["zoom"],
                6, // Zoom level 6
                ["interpolate", ["linear"], ["get", "downloadSpeed"], 50, 1, 100, 2, 150, 4], // Smaller circles at low zoom
                10, // Zoom level 10
                ["interpolate", ["linear"], ["get", "downloadSpeed"], 50, 4, 100, 8, 150, 10], // Larger circles at higher zoom
              ],
              // Adjust the opacity of the circles for better visibility
              "circle-opacity": 0.3,
            },
            layout: { visibility: visibleLayers["speedtests-circles"] ? "visible" : "none" },
          },
          beforeId,
        );
      }

      if (!map.getLayer("speedtests-heatmap")) {
        console.info("Adding speedtests heatmap layer to map");
        map.addLayer(
          {
            id: "speedtests-heatmap",
            type: "heatmap",
            source: "speedtests",
            // minzoom: 9,
            paint: {
              "heatmap-weight": ["interpolate", ["linear"], ["get", "downloadSpeed"], 0, 0, 100, 1],
              "heatmap-intensity": ["interpolate", ["linear"], ["zoom"], 0, 1, 22, 3],
              "heatmap-color": [
                "interpolate",
                ["linear"],
                ["heatmap-density"],
                0,
                "rgba(255,255,255,0.1)", // Light gray for low density
                0.2,
                "rgba(135,206,250,0.9)", // Light blue
                0.4,
                "rgba(30,144,255,0.8)", // Dodger blue
                0.6,
                "rgba(0,0,255,0.9)", // Blue
                0.8,
                "rgba(0,0,139,0.9)", // Dark blue
                1,
                "rgba(0,0,0,0.9)", // Black for high density
              ],
              "heatmap-radius": ["interpolate", ["linear"], ["zoom"], 6, 2, 22, 10],
              "heatmap-opacity": ["interpolate", ["linear"], ["zoom"], 6, 1, 22, 0],
            },
            layout: { visibility: visibleLayers["speedtests-heatmap"] ? "visible" : "none" },
          },
          "speedtests-circles",
        );
      }

      // After adding layers, schedule deferred enforcement so any concurrent
      // addLayer calls (fiber, cells, NAPs) finish before we re-order.
      setTimeout(enforceSpeedtestLayerOrder, 0);
    };
    addSpeedtestSourceToMap();
    map.on("styledata", addSpeedtestSourceToMap);
    map.on("sourcedata", addSpeedtestsLayers);

    return () => {
      map.off("styledata", addSpeedtestSourceToMap);
      map.off("sourcedata", addSpeedtestsLayers);
    };
  }, [map, isMapLoaded, visibleLayers, enforceSpeedtestLayerOrder]);

  // Dedicated effect to update speedtests source data whenever accumulatedGeoJSON changes.
  // Calls source.setData() directly without re-registering event listeners, keeping
  // the layers visible during data updates (no blank flash).
  useEffect(() => {
    if (!map || !isMapLoaded || !map.isStyleLoaded()) return;

    const source = map.getSource<GeoJSONSource>("speedtests");
    if (source) {
      source.setData(accumulatedGeoJSON);
    }
  }, [map, isMapLoaded, accumulatedGeoJSON]);

  // Re-enforce speedtest layer ordering after any map style or source change.
  //
  // WHY deferred (setTimeout 0)?
  // Both this handler and fiber/cell/NAP hooks' addLayer calls may be triggered
  // by the *same* sourcedata or styledata event.  If enforcement runs before those
  // addLayer calls complete, it won't see the newly-added layers and will skip the
  // move.  Deferring to the next event-loop tick ensures all synchronous addLayer
  // calls in the current event cycle finish before we re-order.
  //
  // WHY styledata?
  // map.addLayer() fires a styledata event. Listening here catches the moment when
  // fiber/cell/NAP hooks insert their layers so we can immediately re-position
  // speedtest on top of them.
  //
  // WHY sourcedata filter?
  // sourcedata fires for every tile load which is very frequent; filter to only
  // the sources that own the layers we need to sort against.
  useEffect(() => {
    if (!map || !isMapLoaded) return;

    let pendingEnforce: ReturnType<typeof setTimeout> | null = null;

    const scheduleEnforcement = () => {
      // Cancel any already-scheduled enforcement to debounce rapid-fire events
      if (pendingEnforce !== null) clearTimeout(pendingEnforce);
      pendingEnforce = setTimeout(enforceSpeedtestLayerOrder, 0);
    };

    const handleSourcedata = (e: { sourceId: string }) => {
      if (ORDERING_RELEVANT_SOURCES.has(e.sourceId)) {
        scheduleEnforcement();
      }
    };

    map.on("sourcedata", handleSourcedata);
    map.on("styledata", scheduleEnforcement);

    return () => {
      if (pendingEnforce !== null) clearTimeout(pendingEnforce);
      map.off("sourcedata", handleSourcedata);
      map.off("styledata", scheduleEnforcement);
    };
  }, [map, isMapLoaded, enforceSpeedtestLayerOrder]);

  const setLayerVisible = useCallback(
    (layer: LayerId, visible: boolean) => {
      try {
        if (map && map.getLayer(layer)) {
          map.setLayoutProperty(layer, "visibility", visible ? "visible" : "none");
        }
      } catch {
        // Style may be reloading, silently ignore
      }
    },
    [map],
  );

  useEffect(() => {
    Object.entries(visibleLayers).forEach(([layer, visible]) => {
      setLayerVisible(layer as LayerId, visible);
    });
  }, [map, setLayerVisible, visibleLayers]);

  return null;
};
