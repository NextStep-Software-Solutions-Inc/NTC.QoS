"use client";

/**
 * useMapCells Hook - Revamped Version
 *
 * Manages cell site layers on the map with support for:
 * - Clustered cell sites with provider-specific icons
 * - Cell site heatmap (admin-only)
 * - Signal coverage visualization (admin-only)
 * - Point view mode (admin-only)
 * - Viewport-based loading with pagination
 * - Provider filtering
 *
 * Public map: Only shows cells-clusters, cells-symbols, cells-circles
 * Admin map: Shows all layers with full control
 */
import { useCallback, useEffect, useLayoutEffect, useMemo, useRef, useState } from "react";
import { Popup, type GeoJSONSource } from "@maptiler/sdk";
import type { InfiniteData } from "@tanstack/react-query";
import { toast } from "sonner";

import { getPublicCellsInfiniteRequest } from "@/components/features/cell/handlers/getPublicCellsInfiniteRequest";
import { type CellViewport } from "@/components/features/cell/types/Cell";
import { type CellularCoverageTileset } from "@/components/features/cellular-coverage/types/CellularCoverageTileset";
import { useCellGeoJSONWorker, type CellGeoJSONFeatureCollection } from "@/hooks/useCellGeoJSONWorker";
import { useMap } from "@/hooks/useMap";
import { useMapViewport } from "@/hooks/useMapViewport";
import { PHILIPPINES_BOUNDS } from "@/lib/constants/map-bounds";
import { request } from "@/services/fetch";
import type { APIResponse, PaginationResponse } from "@/types/reusable/api";
import { createInfiniteQueryHookFromTemplate } from "./createInfiniteQueryHookFromTemplate";
import {
  createSignalCoveragePopupHTML,
  getCellCircleLayerPaint,
  getCellSiteHeatmapPaint,
  getClusterLayerPaint,
  getPointViewCirclePaint,
  getProviderIconId,
  getSignalCoverageCirclePaint,
  loadProviderIcons,
} from "./map-cells-utils";

// Constants
const PAGE_SIZE = 2000;
const CELL_RADIUS_KM = 2;
const CLUSTER_RADIUS = 25;
const CLUSTER_MAX_ZOOM = 14;
// Slightly larger icons improve tower legibility on dense/imagery-heavy styles (e.g., Streets/Satellite).
const CELL_ICON_SIZE = 0.5;

// Layer configuration interface (matches CellsiteMapView)
export interface LayerConfig {
  cellSites: boolean;
  cellSiteHeatmap: boolean;
  signalCoverage: boolean;
  pointView: boolean;
}

// Options for cell filtering
export interface CellOptions {
  regionIds?: string[];
  serviceProviderIds?: string[];
  generationIds?: string[];
  isAdminMode?: boolean; // New: determines if admin-only layers are available
  /** Signal strength range filter (dBm) applied to the Radio Network Coverage layer */
  signalStrengthFilter?: { min: number; max: number };
}

const useGetCellsInfinite = createInfiniteQueryHookFromTemplate(getPublicCellsInfiniteRequest);

/**
 * Finds the insertion anchor layer for cell layers so they render ABOVE base map layers
 * (terrain, landcover, water, roads, buildings, fiber) and BELOW higher data layers (NAPs, Speedtests)
 * and base-map place-name labels.
 */
function findCellAnchorLayer(map: any): string | undefined {
  try {
    if (!map.isStyleLoaded()) return undefined;
    const style = map.getStyle();
    if (!style?.layers) return undefined;

    // Check if higher-priority custom data layers exist (NAPs or Speedtests)
    const higherDataLayers = [
      "NAPs-cluster",
      "NAPs-cluster-count",
      "NAPs-unclustered",
      "speedtests-heatmap",
      "speedtests-circles",
    ];
    for (const layerId of higherDataLayers) {
      if (map.getLayer(layerId)) {
        return layerId;
      }
    }

    // Custom cell & higher layer IDs to exclude when looking for place labels
    const customLayerIds = new Set([
      "cells-clusters",
      "cells-cluster-count",
      "cells-circles",
      "cells-symbols",
      "cells-heatmap",
      "cells-point-view",
      "signal-coverage-circles",
      ...higherDataLayers,
    ]);

    // Priority 1: place / settlement / city / town labels (so cell layers are on top of terrain, water, roads, buildings)
    for (const layer of style.layers) {
      if (layer.type === "symbol" && !customLayerIds.has(layer.id)) {
        const id = layer.id.toLowerCase();
        if (
          id.includes("place") ||
          id.includes("settlement") ||
          id.includes("city") ||
          id.includes("town")
        ) {
          return layer.id;
        }
      }
    }

    // Priority 2 (fallback): any symbol layer whose id contains "label"
    for (const layer of style.layers) {
      if (layer.type === "symbol" && !customLayerIds.has(layer.id)) {
        const id = layer.id.toLowerCase();
        if (id.includes("label")) {
          return layer.id;
        }
      }
    }

    return undefined;
  } catch (err) {
    if (process.env.NODE_ENV === "development") {
      console.debug("[useMapCells] findCellAnchorLayer error:", err);
    }
    return undefined;
  }
}

/**
 * Main hook for managing cell site layers
 */
export const useMapCells = (layerConfig: LayerConfig | Record<string, boolean>, options?: CellOptions) => {
  const { map, isMapLoaded } = useMap();
  const iconsLoadedRef = useRef<Set<string>>(new Set());

  // Refs for event handlers and cleanup flag
  const isCleaningUpRef = useRef(false);
  const handleStyleDataRef = useRef<(() => void) | null>(null);
  const handleSourceDataRef = useRef<(() => void) | null>(null);
  // Normalize layer config (support both new and legacy formats)
  const normalizedConfig: LayerConfig = useMemo(() => {
    const input = layerConfig as Record<string, boolean>;

    // Legacy format detection (has "cellular" key)
    if ("cellular" in input) {
      return {
        cellSites: input.cellular ?? false,
        cellSiteHeatmap: false, // Admin only
        signalCoverage: false, // Admin only
        pointView: false, // Admin only
      };
    }

    // New format - restrict admin layers in public mode
    const isAdminMode = options?.isAdminMode ?? false;
    return {
      cellSites: input.cellSites ?? false,
      cellSiteHeatmap: isAdminMode ? (input.cellSiteHeatmap ?? false) : false,
      signalCoverage: isAdminMode ? (input.signalCoverage ?? false) : false,
      pointView: isAdminMode ? (input.pointView ?? false) : false,
    };
  }, [layerConfig, options?.isAdminMode]);

  // State
  const [accumulatedGeoJSON, setAccumulatedGeoJSON] = useState<CellGeoJSONFeatureCollection>({
    type: "FeatureCollection",
    features: [],
  });
  const [totalLoadedCount, setTotalLoadedCount] = useState(0);
  const [signalCoverageData, setSignalCoverageData] = useState<GeoJSON.FeatureCollection | null>(null);
  const [coverageSourceType, setCoverageSourceType] = useState<"maptiler" | "geojson" | null>(null);
  const [dynamicCoverageUrl, setDynamicCoverageUrl] = useState<string | null>(null);

  const lastToastUpdateRef = useRef(0);
  const lastProgressPercentRef = useRef(0);
  const updateTimeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const enforceTimeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const previousViewportRef = useRef<typeof viewportBounds>(null);
  const previousOptionsRef = useRef<string>("");

  // Clean up all pending timeouts on unmount
  useEffect(() => {
    return () => {
      if (updateTimeoutRef.current) {
        clearTimeout(updateTimeoutRef.current);
        updateTimeoutRef.current = null;
      }
      if (enforceTimeoutRef.current) {
        clearTimeout(enforceTimeoutRef.current);
        enforceTimeoutRef.current = null;
      }
    };
  }, []);

  // Keep a ref to the latest accumulated GeoJSON so the main layer setup effect can access it
  // without needing it as a dependency (avoids layer teardown on every data update)
  const accumulatedGeoJSONRef = useRef(accumulatedGeoJSON);
  useLayoutEffect(() => {
    accumulatedGeoJSONRef.current = accumulatedGeoJSON;
  }, [accumulatedGeoJSON]);

  // Always-current ref for normalizedConfig so event handlers (e.g. handleSourceData)
  // can apply the correct visibility after a style change without needing normalizedConfig
  // in the main effect's dependency array (which would cause unnecessary layer teardown).
  const normalizedConfigRef = useRef(normalizedConfig);
  normalizedConfigRef.current = normalizedConfig;
  useLayoutEffect(() => {
    normalizedConfigRef.current = normalizedConfig;
  }, [normalizedConfig]);

  // Cell sites API fetching is only needed when cell sites, heatmap, or pointView layers are enabled.
  // Radio Network Coverage uses separate coverage data and must NOT trigger cell site viewport fetching.
  const isCellSitesActive = normalizedConfig.cellSites || normalizedConfig.cellSiteHeatmap || normalizedConfig.pointView;

  // Track viewport for spatial loading
  const { bounds: viewportBounds, isStable: isViewportStable } = useMapViewport({
    debounceMs: 800,
    enabled: isMapLoaded && isCellSitesActive,
    constraint: PHILIPPINES_BOUNDS,
    showDebugBox: process.env.NODE_ENV === "development",
  });

  // Fetch cells with infinite query
  const {
    data: cellsData,
    fetchNextPage,
    hasNextPage,
    isFetchingNextPage,
    isLoading,
  } = useGetCellsInfinite({
    enabled: isMapLoaded && isCellSitesActive && isViewportStable && !!viewportBounds,
    pathParams: {},
    queryParams: {
      regionIds: options?.regionIds?.length ? options.regionIds : undefined,
      serviceProviderIds: options?.serviceProviderIds,
      generationIds: options?.generationIds,
      pageSize: PAGE_SIZE,
      ...(viewportBounds && {
        minLat: viewportBounds.minLat,
        maxLat: viewportBounds.maxLat,
        minLng: viewportBounds.minLng,
        maxLng: viewportBounds.maxLng,
        zoom: viewportBounds.zoom,
      }),
    },
  });

  // Accumulate cells from all pages
  const accumulatedCells = useMemo(() => {
    if (cellsData) {
      const infiniteData = cellsData as unknown as InfiniteData<PaginationResponse<CellViewport>>;
      return infiniteData.pages.flatMap((page) => page.data);
    }
    return [];
  }, [cellsData]);

  // Loading progress
  const loadingProgress = useMemo(() => {
    if (cellsData) {
      const infiniteData = cellsData as unknown as InfiniteData<PaginationResponse<CellViewport>>;
      const firstPage = infiniteData.pages[0];
      if (firstPage) {
        return { current: accumulatedCells.length, total: firstPage.totalRows };
      }
    }
    return { current: 0, total: 0 };
  }, [cellsData, accumulatedCells]);

  // Process cells with worker
  const handleCellsProcessed = useCallback((geojson: CellGeoJSONFeatureCollection, count: number) => {
    setAccumulatedGeoJSON(geojson);
    setTotalLoadedCount((prev) => prev + count);

    if (updateTimeoutRef.current) {
      clearTimeout(updateTimeoutRef.current);
    }

    updateTimeoutRef.current = setTimeout(() => {
      // Map will update via effect
    }, 250);
  }, []);

  const { processCells } = useCellGeoJSONWorker(handleCellsProcessed);

  // Reset accumulated data when filter options change
  useEffect(() => {
    const currentSerialized = JSON.stringify({
      generationIds: options?.generationIds,
      serviceProviderIds: options?.serviceProviderIds,
      regionIds: options?.regionIds,
    });
    if (previousOptionsRef.current !== currentSerialized) {
      setAccumulatedGeoJSON({ type: "FeatureCollection", features: [] });
      setTotalLoadedCount(0);
      previousOptionsRef.current = currentSerialized;
    }
  }, [options?.generationIds, options?.serviceProviderIds, options?.regionIds]);

  // Track viewport changes
  useEffect(() => {
    if (viewportBounds && isViewportStable) {
      const viewportChanged =
        !previousViewportRef.current ||
        previousViewportRef.current.minLat !== viewportBounds.minLat ||
        previousViewportRef.current.maxLat !== viewportBounds.maxLat ||
        previousViewportRef.current.minLng !== viewportBounds.minLng ||
        previousViewportRef.current.maxLng !== viewportBounds.maxLng;

      if (viewportChanged) {
        setTotalLoadedCount(() => 0);
        previousViewportRef.current = viewportBounds;
      }
    }
  }, [viewportBounds, isViewportStable]);

  // Process cells when new data arrives
  useEffect(() => {
    if (accumulatedCells.length > 0 && accumulatedCells.length > totalLoadedCount) {
      const newCells = accumulatedCells.slice(totalLoadedCount);
      processCells(newCells, CELL_RADIUS_KM, 6, accumulatedGeoJSON.features);
    }
  }, [accumulatedCells, processCells, totalLoadedCount, accumulatedGeoJSON.features]);

  // Max pages to auto-fetch per viewport to prevent network flooding (zoom < 10: 2 pages, zoom >= 10: 4 pages)
  const maxAutoPages = (viewportBounds?.zoom ?? 5) < 10 ? 2 : 4;
  const loadedPageCount = (cellsData as any)?.pages?.length ?? 0;

  // Auto-fetch next page up to maxAutoPages limit
  useEffect(() => {
    if (
      hasNextPage &&
      !isFetchingNextPage &&
      isMapLoaded &&
      isViewportStable &&
      isCellSitesActive &&
      loadedPageCount < maxAutoPages
    ) {
      void fetchNextPage();
    }
  }, [
    hasNextPage,
    isFetchingNextPage,
    fetchNextPage,
    isMapLoaded,
    isViewportStable,
    isCellSitesActive,
    loadedPageCount,
    maxAutoPages,
  ]);

  // Show cell sites loading progress toast
  useEffect(() => {
    if (!isCellSitesActive) {
      toast.dismiss("loading-cells");
      return;
    }

    const now = Date.now();
    const { current, total } = loadingProgress;

    if (total === 0) return;

    const progressPercent = Math.floor((current / total) * 100);
    const timeSinceLastUpdate = now - lastToastUpdateRef.current;
    const progressChanged = Math.abs(progressPercent - lastProgressPercentRef.current) >= 10;

    if (isLoading || isFetchingNextPage) {
      if (timeSinceLastUpdate >= 1000 || progressChanged) {
        toast.loading(`Loading cell sites... ${progressPercent}%`, {
          id: "loading-cells",
          description: `${current.toLocaleString()} of ${total.toLocaleString()} cell sites loaded`,
        });
        lastToastUpdateRef.current = now;
        lastProgressPercentRef.current = progressPercent;
      }
    } else if (current > 0) {
      toast.success("Cell sites loaded successfully!", {
        id: "loading-cells",
        description: `${current.toLocaleString()} cell sites loaded`,
      });
    }
  }, [isLoading, isFetchingNextPage, loadingProgress, isCellSitesActive]);

  // Load signal coverage data (admin-only) - Priority: Active MapTiler backend tileset -> env fallback -> JSON assets fallback
  useEffect(() => {
    if (!normalizedConfig.signalCoverage || !options?.isAdminMode) return;
    if (signalCoverageData || coverageSourceType) return;

    const loadCoverage = async () => {
      // 1. Try active MapTiler tileset from backend API first
      let resolvedCoverageUrl: string | undefined = process.env.NEXT_PUBLIC_MAPTILER_COVERAGE_URL;

      try {
        const regionParam = options?.regionIds?.[0] ? `?regionId=${encodeURIComponent(options.regionIds[0])}` : "";
        const res = await request.get<APIResponse<CellularCoverageTileset[]>>(
          `/cellular-coverages/tilesets/active${regionParam}`
        );

        if (res.succeeded && res.data && res.data.length > 0) {
          const readyTileset = res.data.find((t) => t.tileJsonUrl);
          if (readyTileset?.tileJsonUrl) {
            resolvedCoverageUrl = readyTileset.tileJsonUrl;
          }
        }
      } catch (err) {
        if (process.env.NODE_ENV === "development") {
          console.debug("[useMapCells] Failed to fetch active tilesets from API:", err);
        }
      }

      // If MapTiler vector tiles source is configured (from API or env)
      if (resolvedCoverageUrl) {
        toast.loading("Loading radio network coverage from MapTiler...", {
          id: "loading-coverage",
          description: "Connecting to MapTiler coverage source",
        });

        setDynamicCoverageUrl(resolvedCoverageUrl);
        setCoverageSourceType("maptiler");
        toast.success("Radio network coverage loaded successfully!", {
          id: "loading-coverage",
          description: "Loaded from MapTiler source",
        });
        return;
      }

      // 2. Fallback: Load from public JSON assets (all_telco_band_data_flat.json or cell-signal.json)
      toast.loading("Loading radio network coverage...", {
        id: "loading-coverage",
        description: "Fetching coverage data points",
      });

      const jsonPaths = ["/data/all_telco_band_data_flat.json", "/data/cell-signal.json"];

      for (const path of jsonPaths) {
        try {
          const res = await fetch(path);
          if (!res.ok) continue;
          const data: Array<{ lat: number; lng: number; strength: number; barangay?: string; azimuth?: string }> =
            await res.json();
          if (!Array.isArray(data) || data.length === 0) continue;

          const geojson: GeoJSON.FeatureCollection = {
            type: "FeatureCollection",
            features: data.map((point) => ({
              type: "Feature" as const,
              geometry: { type: "Point" as const, coordinates: [point.lng, point.lat] },
              properties: { strength: point.strength, barangay: point.barangay, azimuth: point.azimuth },
            })),
          };
          setSignalCoverageData(geojson);
          setCoverageSourceType("geojson");
          toast.success("Radio network coverage loaded successfully!", {
            id: "loading-coverage",
            description: `${data.length.toLocaleString()} coverage points loaded`,
          });
          return;
        } catch (err) {
          console.warn(`Failed to load coverage fallback from ${path}:`, err);
        }
      }

      toast.error("Failed to load radio network coverage", {
        id: "loading-coverage",
      });
    };

    void loadCoverage();
  }, [normalizedConfig.signalCoverage, signalCoverageData, coverageSourceType, options?.isAdminMode, options?.regionIds]);

  // Helper to set layer visibility
  const setLayerVisible = useCallback(
    (layerId: string, visible: boolean) => {
      try {
        if (map && map.isStyleLoaded() && map.getLayer(layerId)) {
          map.setLayoutProperty(layerId, "visibility", visible ? "visible" : "none");
        }
      } catch (err) {
        if (process.env.NODE_ENV === "development") {
          console.debug("[useMapCells] setLayerVisible error:", err);
        }
      }
    },
    [map],
  );

  /**
   * Enforces proper layer ordering after style changes or concurrent layer additions.
   * Uses map.moveLayer() to ensure cell layers are positioned consistently:
   * Above base map layers (and fiber) and below NAPs/Speedtests and place labels.
   */
  const enforceCellLayerOrder = useCallback(() => {
    if (!map || !map.isStyleLoaded()) return;

    try {
      const anchorId = findCellAnchorLayer(map);

      // Move cell layers in bottom-to-top order within the cell group
      const moveOrder = [
        "signal-coverage-circles",
        "cells-heatmap",
        "cells-point-view",
        "cells-clusters",
        "cells-cluster-count",
        "cells-circles",
        "cells-symbols",
      ] as const;

      for (const layerId of moveOrder) {
        if (!map.getLayer(layerId)) continue;
        try {
          map.moveLayer(layerId, anchorId);
        } catch (err) {
          if (process.env.NODE_ENV === "development") {
            console.debug(`[useMapCells] moveLayer(${layerId}) failed, will retry:`, err);
          }
        }
      }
    } catch (err) {
      if (process.env.NODE_ENV === "development") {
        console.debug("[useMapCells] enforceCellLayerOrder error:", err);
      }
    }
  }, [map]);

  // Main effect: Create and manage map layers
  useEffect(() => {
    if (!map || !isMapLoaded) return;

    // Reset cleanup flag at the start of the effect lifecycle
    isCleaningUpRef.current = false;

    // Create popup instance for signal coverage
    const popup = new Popup({ closeButton: true, closeOnClick: true, maxWidth: "320px" });

    // Add or update cells source (clustered)
    const addCellsSource = () => {
      try {
        if (!map.getSource("cells")) {
          map.addSource("cells", {
            type: "geojson",
            // Use ref so initial data is current without needing accumulatedGeoJSON in effect deps
            data: accumulatedGeoJSONRef.current,
            cluster: true,
            clusterMaxZoom: CLUSTER_MAX_ZOOM,
            clusterRadius: CLUSTER_RADIUS,
          });
        }
      } catch (err) {
        if (process.env.NODE_ENV === "development") {
          console.debug("[useMapCells] addCellsSource error:", err);
        }
      }
    };

    // Add non-clustered source for heatmap and point view (admin-only)
    const addCellsHeatmapSource = () => {
      if (options?.isAdminMode) {
        try {
          if (!map.getSource("cells-heatmap-source")) {
            map.addSource("cells-heatmap-source", {
              type: "geojson",
              // Use ref so initial data is current without needing accumulatedGeoJSON in effect deps
              data: accumulatedGeoJSONRef.current,
              cluster: false,
            });
          }
        } catch (err) {
          if (process.env.NODE_ENV === "development") {
            console.debug("[useMapCells] addCellsHeatmapSource error:", err);
          }
        }
      }
    };

    // Add signal coverage source (admin-only) - Priority: MapTiler source first, then GeoJSON fallback
    const addSignalCoverageSource = () => {
      if (!options?.isAdminMode) return;

      try {
        const maptilerCoverageUrl = dynamicCoverageUrl || process.env.NEXT_PUBLIC_MAPTILER_COVERAGE_URL;
        if (coverageSourceType === "maptiler" && maptilerCoverageUrl) {
          if (!map.getSource("signal-coverage-source")) {
            map.addSource("signal-coverage-source", {
              type: "vector",
              url: maptilerCoverageUrl,
            });
          }
        } else if (signalCoverageData) {
          if (!map.getSource("signal-coverage-source")) {
            map.addSource("signal-coverage-source", {
              type: "geojson",
              data: signalCoverageData,
            });
          } else {
            const source = map.getSource<GeoJSONSource>("signal-coverage-source");
            if (source && "setData" in source) {
              source.setData(signalCoverageData);
            }
          }
        }
      } catch (err) {
        if (process.env.NODE_ENV === "development") {
          console.debug("[useMapCells] addSignalCoverageSource error:", err);
        }
      }
    };

    // Load provider icons
    const loadIcons = async () => {
      await loadProviderIcons(map, iconsLoadedRef.current);
    };

    // Add cell layers
    const addCellLayers = () => {
      try {
        if (!map.isStyleLoaded()) return;
        const beforeId = findCellAnchorLayer(map);
        const initialVis =
          normalizedConfigRef.current.cellSites && !normalizedConfigRef.current.pointView ? "visible" : "none";

        // Cluster circles
        if (!map.getLayer("cells-clusters")) {
          map.addLayer(
            {
              id: "cells-clusters",
              type: "circle",
              source: "cells",
              filter: ["has", "point_count"],
              paint: getClusterLayerPaint(),
              layout: {
                visibility: initialVis,
              },
            },
            beforeId,
          );
        }

        // Cluster count labels
        if (!map.getLayer("cells-cluster-count")) {
          map.addLayer(
            {
              id: "cells-cluster-count",
              type: "symbol",
              source: "cells",
              filter: ["has", "point_count"],
              layout: {
                "text-field": "{point_count_abbreviated}",
                "text-font": ["Open Sans Bold"],
                "text-size": 11,
                visibility: initialVis,
              },
              paint: { "text-color": "#ffffff" },
            },
            beforeId,
          );
        }

        // Individual cell circles (fallback)
        if (!map.getLayer("cells-circles")) {
          map.addLayer(
            {
              id: "cells-circles",
              type: "circle",
              source: "cells",
              filter: ["!has", "point_count"],
              paint: getCellCircleLayerPaint(),
              layout: {
                visibility: initialVis,
              },
            },
            beforeId,
          );
        }

        // Cell symbols with provider icons
        if (!map.getLayer("cells-symbols")) {
          map.addLayer(
            {
              id: "cells-symbols",
              type: "symbol",
              source: "cells",
              filter: ["!has", "point_count"],
              layout: {
                "icon-image": ["get", "providerIcon"],
                "icon-size": CELL_ICON_SIZE,
                "icon-allow-overlap": true,
                "icon-ignore-placement": true,
                visibility: initialVis,
                "icon-anchor": "bottom",
              },
            },
            beforeId,
          );
        }
      } catch (err) {
        if (process.env.NODE_ENV === "development") {
          console.debug("[useMapCells] addCellLayers error:", err);
        }
      }
    };

    // Add admin-only layers
    const addAdminLayers = () => {
      if (!options?.isAdminMode) return;
      try {
        const beforeId = findCellAnchorLayer(map);

        // Cell site heatmap - always add if in admin mode, control visibility separately
        if (!map.getLayer("cells-heatmap")) {
          map.addLayer(
            {
              id: "cells-heatmap",
              type: "heatmap",
              source: "cells-heatmap-source",
              maxzoom: 15,
              paint: getCellSiteHeatmapPaint(0.7),
              layout: {
                visibility: "none", // Start hidden, controlled by applyVisibility
              },
            },
            beforeId,
          );
        }

        // Point view layer - always add if in admin mode, control visibility separately
        if (!map.getLayer("cells-point-view")) {
          map.addLayer(
            {
              id: "cells-point-view",
              type: "circle",
              source: "cells-heatmap-source",
              paint: getPointViewCirclePaint(),
              layout: {
                visibility: "none", // Start hidden, controlled by applyVisibility
              },
            },
            beforeId,
          );
        }

        // Signal coverage layer - always add if in admin mode and source is available
        if (map.getSource("signal-coverage-source") && !map.getLayer("signal-coverage-circles")) {
          const signalBeforeId = map.getLayer("cells-clusters")
            ? "cells-clusters"
            : map.getLayer("cells-circles")
              ? "cells-circles"
              : map.getLayer("cells-symbols")
                ? "cells-symbols"
                : beforeId;

          const coverageLayerSpec: any = {
            id: "signal-coverage-circles",
            type: "circle",
            source: "signal-coverage-source",
            paint: getSignalCoverageCirclePaint(0.8),
            layout: {
              visibility: normalizedConfigRef.current.signalCoverage ? "visible" : "none",
            },
          };

          if (coverageSourceType === "maptiler") {
            coverageLayerSpec["source-layer"] =
              process.env.NEXT_PUBLIC_MAPTILER_COVERAGE_SOURCE_LAYER_ID ?? "coverage";
          }

          map.addLayer(coverageLayerSpec, signalBeforeId);
        }
      } catch (err) {
        if (process.env.NODE_ENV === "development") {
          console.debug("[useMapCells] addAdminLayers error:", err);
        }
      }
    };

    // Click handler for cell ID selection (for public and admin)
    const handleCellClick = (e: any) => {
      const features = map.queryRenderedFeatures(e.point, { layers: ["cells-symbols", "cells-circles"] });
      if (features && features.length > 0) {
        const feature = features.find((f: any) => f.layer.id === "cells-symbols" || f.layer.id === "cells-circles");
        if (!feature) return;

        const id = feature.properties?.id as string;
        if (!id) return;

        // Update URL with cellId
        const searchParams = new URLSearchParams(window.location.search);
        searchParams.set("cellId", id);
        window.history.replaceState({}, "", `?${searchParams.toString()}`);
      }
    };

    // Hover & click handlers for signal coverage points (admin-only)
    const handleSignalCoverageMouseMove = (e: any) => {
      if (!options?.isAdminMode) return;
      const features = map.queryRenderedFeatures(e.point, { layers: ["signal-coverage-circles"] });
      if (features && features.length > 0) {
        const feature = features[0];
        if (!feature) return;
        map.getCanvas().style.cursor = "pointer";
        const properties = feature.properties as any;

        if (properties) {
          const coordinates = (feature.geometry as any).coordinates.slice();

          // Ensure the popup appears over the correct feature
          while (Math.abs(e.lngLat.lng - coordinates[0]) > 180) {
            coordinates[0] += e.lngLat.lng > coordinates[0] ? 360 : -360;
          }

          popup.setLngLat(coordinates).setHTML(createSignalCoveragePopupHTML(properties)).addTo(map);
        }
      }
    };

    const handleSignalCoverageLeave = () => {
      if (options?.isAdminMode) {
        map.getCanvas().style.cursor = "";
        popup.remove();
      }
    };

    // Helper to remove all cell-related layers safely
    const removeCellLayers = () => {
      const layersToRemove = [
        "cells-clusters",
        "cells-cluster-count",
        "cells-circles",
        "cells-symbols",
        "cells-heatmap",
        "cells-point-view",
        "signal-coverage-circles",
      ];

      for (const layerId of layersToRemove) {
        if (map.getLayer(layerId)) {
          map.removeLayer(layerId);
        }
      }
    };

    // Helper to remove all cell-related sources safely
    const removeCellSources = () => {
      const sourcesToRemove = ["cells", "cells-heatmap-source", "signal-coverage-source"];

      for (const sourceId of sourcesToRemove) {
        if (map.getSource(sourceId)) {
          map.removeSource(sourceId);
        }
      }
    };

    // Apply initial visibility immediately after adding layers
    const applyInitialVisibility = () => {
      const cfg = normalizedConfigRef.current;
      const cellSitesVisible = cfg.cellSites && !cfg.pointView;

      setLayerVisible("cells-clusters", cellSitesVisible);
      setLayerVisible("cells-cluster-count", cellSitesVisible);
      setLayerVisible("cells-circles", cellSitesVisible);
      setLayerVisible("cells-symbols", cellSitesVisible);

      if (options?.isAdminMode) {
        setLayerVisible("cells-heatmap", cfg.cellSiteHeatmap);

        if (cfg.pointView) {
          setLayerVisible("cells-point-view", true);
          setLayerVisible("cells-clusters", false);
          setLayerVisible("cells-cluster-count", false);
          setLayerVisible("cells-circles", false);
          setLayerVisible("cells-symbols", false);
        } else {
          setLayerVisible("cells-point-view", false);
        }

        setLayerVisible("signal-coverage-circles", cfg.signalCoverage);
      }
    };

    const setupCellLayers = () => {
      if (!map.isStyleLoaded()) return;
      void loadIcons();
      addCellsSource();
      addCellsHeatmapSource();
      addSignalCoverageSource();
      addCellLayers();
      addAdminLayers();
      applyInitialVisibility();
      if (enforceTimeoutRef.current) clearTimeout(enforceTimeoutRef.current);
      enforceTimeoutRef.current = setTimeout(() => enforceCellLayerOrder(), 0);
    };

    // Initialize once style is ready
    let cleanupStyleListener: (() => void) | undefined;
    if (map.isStyleLoaded()) {
      setupCellLayers();
    } else {
      const onStyleReady = () => {
        if (map.isStyleLoaded()) {
          map.off("styledata", onStyleReady);
          map.off("idle", onStyleReady);
          setupCellLayers();
        }
      };
      map.on("styledata", onStyleReady);
      map.on("idle", onStyleReady);
      const timer = setTimeout(onStyleReady, 100);
      cleanupStyleListener = () => {
        map.off("styledata", onStyleReady);
        map.off("idle", onStyleReady);
        clearTimeout(timer);
      };
    }

    // Attach click handlers
    map.on("click", "cells-circles", handleCellClick);
    map.on("click", "cells-symbols", handleCellClick);

    // Attach signal coverage handlers (admin-only)
    if (options?.isAdminMode) {
      map.on("click", "signal-coverage-circles", handleSignalCoverageMouseMove);
      map.on("mousemove", "signal-coverage-circles", handleSignalCoverageMouseMove);
      map.on("mouseleave", "signal-coverage-circles", handleSignalCoverageLeave);
    }

    // Handle style changes (e.g. map.setStyle() wipes sources and layers)
    const handleStyleData = () => {
      if (isCleaningUpRef.current || !map.isStyleLoaded()) return;
      if (!map.getSource("cells") || !map.getLayer("cells-clusters")) {
        setupCellLayers();
      }
    };

    const handleSourceData = () => {
      if (isCleaningUpRef.current || !map.isStyleLoaded()) return;
      // Detect whether layers are about to be newly added (style-change recovery).
      // cells-clusters is the first cell layer added; if it is absent the whole
      // layer set is being re-created and we must re-apply the current visibility
      // after the add, because the layers start with visibility:"none".
      const layersWereMissing = !map.getLayer("cells-clusters");
      addCellsSource();
      addCellLayers();
      addAdminLayers();
      if (layersWereMissing && map.getLayer("cells-clusters")) {
        applyInitialVisibility();
        if (enforceTimeoutRef.current) clearTimeout(enforceTimeoutRef.current);
        enforceTimeoutRef.current = setTimeout(() => enforceCellLayerOrder(), 0);
      }
    };

    map.on("styledata", handleStyleData);
    map.on("sourcedata", handleSourceData);

    // Store handler references for cleanup
    handleStyleDataRef.current = handleStyleData;
    handleSourceDataRef.current = handleSourceData;

    return () => {
      cleanupStyleListener?.();
      if (enforceTimeoutRef.current) {
        clearTimeout(enforceTimeoutRef.current);
        enforceTimeoutRef.current = null;
      }
      // Prevent handlers from running during teardown
      isCleaningUpRef.current = true;

      // Remove event listeners first
      map.off("click", "cells-circles", handleCellClick);
      map.off("click", "cells-symbols", handleCellClick);

      if (options?.isAdminMode) {
        map.off("click", "signal-coverage-circles", handleSignalCoverageMouseMove);
        map.off("mousemove", "signal-coverage-circles", handleSignalCoverageMouseMove);
        map.off("mouseleave", "signal-coverage-circles", handleSignalCoverageLeave);
      }

      if (handleStyleDataRef.current) {
        map.off("styledata", handleStyleDataRef.current);
      }
      if (handleSourceDataRef.current) {
        map.off("sourcedata", handleSourceDataRef.current);
      }

      // Clean up popup
      popup.remove();

      // Remove layers BEFORE removing sources to avoid "source in use" errors
      removeCellLayers();
      removeCellSources();
    };
    // accumulatedGeoJSON intentionally omitted from deps; updates handled by dedicated effect below
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [map, isMapLoaded, signalCoverageData, coverageSourceType, options?.isAdminMode, setLayerVisible]);

  // Dedicated effect to update source data whenever accumulatedGeoJSON changes.
  // This calls source.setData() directly WITHOUT removing/re-adding layers, preventing
  // the visible blank flash that would occur if accumulatedGeoJSON were in the main effect's deps.
  useEffect(() => {
    if (!map || !isMapLoaded || !map.isStyleLoaded()) return;

    const cellsSource = map.getSource<GeoJSONSource>("cells");
    if (cellsSource) {
      cellsSource.setData(accumulatedGeoJSON);
    }

    if (options?.isAdminMode) {
      const heatmapSource = map.getSource<GeoJSONSource>("cells-heatmap-source");
      if (heatmapSource) {
        heatmapSource.setData(accumulatedGeoJSON);
      }
    }
  }, [map, isMapLoaded, accumulatedGeoJSON, options?.isAdminMode]);

  // Separate effect for visibility control - only runs when config changes
  useEffect(() => {
    if (!map || !isMapLoaded) return;

    const applyVisibility = () => {
      const cellSitesVisible = normalizedConfig.cellSites && !normalizedConfig.pointView;

      setLayerVisible("cells-clusters", cellSitesVisible);
      setLayerVisible("cells-cluster-count", cellSitesVisible);
      setLayerVisible("cells-circles", cellSitesVisible);
      setLayerVisible("cells-symbols", cellSitesVisible);

      if (options?.isAdminMode) {
        // Admin-only layer visibility - just toggle, don't remove/add
        setLayerVisible("cells-heatmap", normalizedConfig.cellSiteHeatmap);

        if (normalizedConfig.pointView) {
          setLayerVisible("cells-point-view", true);
          // Hide clusters in point view mode
          setLayerVisible("cells-clusters", false);
          setLayerVisible("cells-cluster-count", false);
          setLayerVisible("cells-circles", false);
          setLayerVisible("cells-symbols", false);
        } else {
          setLayerVisible("cells-point-view", false);
        }

        // Signal coverage - toggle independently
        setLayerVisible("signal-coverage-circles", normalizedConfig.signalCoverage);
      }
    };

    if (!map.isStyleLoaded()) {
      const onStyleReady = () => {
        if (map.isStyleLoaded()) {
          map.off("styledata", onStyleReady);
          map.off("idle", onStyleReady);
          applyVisibility();
        }
      };
      map.on("styledata", onStyleReady);
      map.on("idle", onStyleReady);
      const timer = setTimeout(onStyleReady, 100);
      return () => {
        map.off("styledata", onStyleReady);
        map.off("idle", onStyleReady);
        clearTimeout(timer);
      };
    }

    applyVisibility();
  }, [map, isMapLoaded, normalizedConfig, options?.isAdminMode, setLayerVisible]);

  // Apply signal strength filter to the Radio Network Coverage layer (admin-only)
  // Uses MapTiler's GPU-accelerated filter expressions for efficient large-dataset filtering
  const signalStrengthMin = options?.signalStrengthFilter?.min;
  const signalStrengthMax = options?.signalStrengthFilter?.max;

  useEffect(() => {
    if (!map || !isMapLoaded || !map.isStyleLoaded() || !options?.isAdminMode) return;
    if (!map.getLayer("signal-coverage-circles")) return;

    // No filter when values are absent or cover the full default range (-120 to -40)
    if (signalStrengthMin === undefined || signalStrengthMax === undefined ||
        (signalStrengthMin <= -120 && signalStrengthMax >= -40)) {
      map.setFilter("signal-coverage-circles", null);
      return;
    }

    // Apply inclusive range filter as a MapTiler expression
    const strengthExpr: any = ["coalesce", ["get", "strength"], ["get", "signal_strength_dbm"]];
    map.setFilter("signal-coverage-circles", [
      "all",
      [">=", strengthExpr, signalStrengthMin],
      ["<=", strengthExpr, signalStrengthMax],
    ] as any);
  }, [map, isMapLoaded, options?.isAdminMode, signalStrengthMin, signalStrengthMax]);

  // Re-enforce cell layer ordering after any map style or source change.
  // This ensures cell layers stay in the correct z-order across all map styles,
  // even when switching between styles with different base layer structures.
  useEffect(() => {
    if (!map || !isMapLoaded) return;

    let pendingEnforce: ReturnType<typeof setTimeout> | null = null;

    const scheduleEnforcement = () => {
      // Cancel any already-scheduled enforcement to debounce rapid-fire events
      if (pendingEnforce !== null) clearTimeout(pendingEnforce);
      pendingEnforce = setTimeout(enforceCellLayerOrder, 0);
    };

    map.on("styledata", scheduleEnforcement);
    map.on("sourcedata", scheduleEnforcement);

    return () => {
      if (pendingEnforce !== null) clearTimeout(pendingEnforce);
      map.off("styledata", scheduleEnforcement);
      map.off("sourcedata", scheduleEnforcement);
    };
  }, [map, isMapLoaded, enforceCellLayerOrder]);

  return { cells: accumulatedCells, setLayerVisible };
};
