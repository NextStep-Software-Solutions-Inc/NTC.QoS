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
import { useCellGeoJSONWorker, type CellGeoJSONFeatureCollection } from "@/hooks/useCellGeoJSONWorker";
import { useMap } from "@/hooks/useMap";
import { useMapViewport } from "@/hooks/useMapViewport";
import { PHILIPPINES_BOUNDS } from "@/lib/constants/map-bounds";
import type { PaginationResponse } from "@/types/reusable/api";
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
 * Base map layers that should render BELOW cell layers.
 * Used to find the insertion anchor when re-ordering layers after style changes.
 * Must be in order of typical z-order in MapTiler styles.
 */
const LAYERS_BELOW_CELLS = [
  // Base map elements that commonly exist in most styles
  "background", "water", "landuse", "landcover", "terrain", "building", "road", "bridge", "tunnel",
] as const;

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

  const lastToastUpdateRef = useRef(0);
  const lastProgressPercentRef = useRef(0);
  const updateTimeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const previousViewportRef = useRef<typeof viewportBounds>(null);
  const previousOptionsRef = useRef<string>("");

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
  useLayoutEffect(() => {
    normalizedConfigRef.current = normalizedConfig;
  }, [normalizedConfig]);

  // Track viewport for spatial loading
  const { bounds: viewportBounds, isStable: isViewportStable } = useMapViewport({
    debounceMs: 800,
    enabled: isMapLoaded && Object.values(normalizedConfig).some((v) => v),
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
    enabled: isMapLoaded && Object.values(normalizedConfig).some((v) => v) && isViewportStable && !!viewportBounds,
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
    if (hasNextPage && !isFetchingNextPage && isMapLoaded && isViewportStable && loadedPageCount < maxAutoPages) {
      void fetchNextPage();
    }
  }, [hasNextPage, isFetchingNextPage, fetchNextPage, isMapLoaded, isViewportStable, loadedPageCount, maxAutoPages]);

  // Show loading progress toast
  useEffect(() => {
    const now = Date.now();
    const { current, total } = loadingProgress;

    if (total === 0) return;

    const progressPercent = Math.floor((current / total) * 100);
    const timeSinceLastUpdate = now - lastToastUpdateRef.current;
    const progressChanged = Math.abs(progressPercent - lastProgressPercentRef.current) >= 10;

    if (isLoading || isFetchingNextPage) {
      if (timeSinceLastUpdate >= 1000 || progressChanged) {
        toast.loading(`Loading cells... ${progressPercent}%`, {
          id: "loading-cells",
          description: `${current.toLocaleString()} of ${total.toLocaleString()} cells loaded`,
        });
        lastToastUpdateRef.current = now;
        lastProgressPercentRef.current = progressPercent;
      }
    } else if (current > 0) {
      toast.success("Cells loaded successfully!", {
        id: "loading-cells",
        description: `${current.toLocaleString()} cells loaded`,
      });
    }
  }, [isLoading, isFetchingNextPage, loadingProgress]);

  // Load signal coverage data (admin-only)
  useEffect(() => {
    if (normalizedConfig.signalCoverage && !signalCoverageData && options?.isAdminMode) {
      // Load from static file
      fetch("/data/all_telco_band_data_flat.json")
        .then((res) => res.json())
        .then((data: Array<{ lat: number; lng: number; strength: number; barangay?: string; azimuth?: string }>) => {
          const geojson: GeoJSON.FeatureCollection = {
            type: "FeatureCollection",
            features: data.map((point) => ({
              type: "Feature" as const,
              geometry: { type: "Point" as const, coordinates: [point.lng, point.lat] },
              properties: { strength: point.strength, barangay: point.barangay, azimuth: point.azimuth },
            })),
          };
          setSignalCoverageData(geojson);
        })
        .catch((err) => {
          console.error("Failed to load signal coverage data:", err);
        });
    }
  }, [normalizedConfig.signalCoverage, signalCoverageData, options?.isAdminMode]);

  // Helper to set layer visibility
  const setLayerVisible = useCallback(
    (layerId: string, visible: boolean) => {
      try {
        if (map && map.getLayer(layerId)) {
          map.setLayoutProperty(layerId, "visibility", visible ? "visible" : "none");
        }
      } catch {
        // Style may be reloading, silently ignore
      }
    },
    [map],
  );

  /**
   * Enforces proper layer ordering after style changes or concurrent layer additions.
   * Uses map.moveLayer() to ensure cell layers are positioned consistently,
   * regardless of the map style's base layer structure.
   *
   * Strategy:
   * 1. Find the highest-z "floor" layer from LAYERS_BELOW_CELLS that exists in the current style
   * 2. Find the first non-cell layer above that floor (this is our insertion anchor)
   * 3. Move all cell layers to just above the floor, before the anchor
   *
   * This ensures: [...floor layers..., signal-coverage, cell-layers, <anchor/labels>, ...]
   */
  const enforceCellLayerOrder = useCallback(() => {
    if (!map || !map.isStyleLoaded()) return;

    const style = map.getStyle();
    if (!style?.layers) return;

    // Skip if no cell layers exist yet
    const cellLayerIds = [
      "signal-coverage-circles",
      "cells-heatmap",
      "cells-point-view",
      "cells-clusters",
      "cells-cluster-count",
      "cells-circles",
      "cells-symbols",
    ];

    if (!cellLayerIds.some((id) => map.getLayer(id))) return;

    const layerIds = style.layers.map((l) => l.id).filter((id): id is string => id !== undefined);

    // Phase 1: Find the highest-z "floor" layer from known base map layers
    let floorIdx = -1;
    for (const id of LAYERS_BELOW_CELLS) {
      const idx = layerIds.indexOf(id as string);
      if (idx > floorIdx) floorIdx = idx;
    }

    let anchorId: string | undefined;

    if (floorIdx !== -1) {
      // Walk forward from just above the floor layer to find the first non-cell layer.
      // This is the slot where cell layers will be inserted.
      for (let i = floorIdx + 1; i < layerIds.length; i++) {
        const id = layerIds[i];
        if (id && !cellLayerIds.includes(id)) {
          anchorId = id;
          break;
        }
      }
    }

    // Phase 2 fallback: find the topmost non-cell symbol layer (typically place labels)
    if (!anchorId) {
      for (let i = style.layers.length - 1; i >= 0; i--) {
        const layer = style.layers[i];
        if (layer && layer.type === "symbol" && !cellLayerIds.includes(layer.id)) {
          anchorId = layer.id;
          break;
        }
      }
    }

    if (!anchorId) return; // No suitable anchor found

    if (process.env.NODE_ENV === "development") {
      console.log("🗺️ [Cells] Enforcing layer order before", anchorId);
    }

    // Move cell layers in order (signal coverage first, then others)
    // This ensures proper z-order: signal-coverage below all other cells
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
        // Layer may not be ready yet; will be retried on the next scheduled call
        if (process.env.NODE_ENV === "development") {
          console.debug(`[useMapCells] moveLayer(${layerId}) failed, will retry:`, err);
        }
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
      // Data updates are handled by a dedicated effect to avoid layer rebuilding
    };

    // Add non-clustered source for heatmap and point view (admin-only)
    const addCellsHeatmapSource = () => {
      if (options?.isAdminMode) {
        if (!map.getSource("cells-heatmap-source")) {
          map.addSource("cells-heatmap-source", {
            type: "geojson",
            // Use ref so initial data is current without needing accumulatedGeoJSON in effect deps
            data: accumulatedGeoJSONRef.current,
            cluster: false,
          });
        }
        // Data updates are handled by a dedicated effect to avoid layer rebuilding
      }
    };

    // Add signal coverage source (admin-only)
    const addSignalCoverageSource = () => {
      if (options?.isAdminMode && normalizedConfig.signalCoverage && signalCoverageData) {
        if (!map.getSource("signal-coverage-source")) {
          map.addSource("signal-coverage-source", { type: "geojson", data: signalCoverageData });
        } else {
          const source = map.getSource<GeoJSONSource>("signal-coverage-source");
          if (source) {
            source.setData(signalCoverageData);
          }
        }
      }
    };

    // Load provider icons
    const loadIcons = async () => {
      await loadProviderIcons(map, iconsLoadedRef.current);
    };

    // Add cell layers
    // Find the appropriate layer to insert custom layers before it
    // We want custom layers ABOVE base map layers but BELOW place labels
    // Strategy: Look for place/settlement/city labels or fallback to any label layer
    const findCellBeforeLayer = () => {
      const layers = map.getStyle().layers;
      if (!layers) return undefined;

      // First, try to find a place name or settlement label layer
      // These typically come after roads/buildings in MapTiler styles (including outdoor)
      for (const layer of layers) {
        if (layer.type === "symbol") {
          const layerId = layer.id.toLowerCase();
          if (layerId.includes("place") || layerId.includes("settlement") || layerId.includes("city") || layerId.includes("town")) {
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

      return undefined; // Will render on top if no suitable layer found
    };

    const addCellLayers = () => {
      const beforeId = findCellBeforeLayer();

      if (process.env.NODE_ENV === "development") {
        console.log("🗺️ [Cells] Adding layers", beforeId ? `before "${beforeId}"` : "on top");
      }

      // Cluster circles
      if (!map.getLayer("cells-clusters")) {
        map.addLayer({
          id: "cells-clusters",
          type: "circle",
          source: "cells",
          filter: ["has", "point_count"],
          paint: getClusterLayerPaint(),
          layout: {
            visibility: "none", // Start hidden, controlled by visibility effect
          },
        }, beforeId);
      }

      // Cluster count labels
      if (!map.getLayer("cells-cluster-count")) {
        map.addLayer({
          id: "cells-cluster-count",
          type: "symbol",
          source: "cells",
          filter: ["has", "point_count"],
          layout: {
            "text-field": "{point_count_abbreviated}",
            "text-font": ["Open Sans Bold"],
            "text-size": 11,
            visibility: "none", // Start hidden, controlled by visibility effect
          },
          paint: { "text-color": "#ffffff" },
        }, beforeId);
      }

      // Individual cell circles (fallback)
      if (!map.getLayer("cells-circles")) {
        map.addLayer({
          id: "cells-circles",
          type: "circle",
          source: "cells",
          filter: ["!has", "point_count"],
          paint: getCellCircleLayerPaint(),
          layout: {
            visibility: "none", // Start hidden, controlled by visibility effect
          },
        }, beforeId);
      }

      // Cell symbols with provider icons
      if (!map.getLayer("cells-symbols")) {
        map.addLayer({
          id: "cells-symbols",
          type: "symbol",
          source: "cells",
          filter: ["!has", "point_count"],
          layout: {
            "icon-image": ["get", "providerIcon"],
            "icon-size": CELL_ICON_SIZE,
            "icon-allow-overlap": true,
            "icon-ignore-placement": true,
            visibility: "none", // Start hidden, controlled by visibility effect
            "icon-anchor": "bottom",
          },
        }, beforeId);
      }
    };

    // Add admin-only layers
    const addAdminLayers = () => {
      if (!options?.isAdminMode) return;

      const beforeId = findCellBeforeLayer();

      // Cell site heatmap - always add if in admin mode, control visibility separately
      if (!map.getLayer("cells-heatmap")) {
        map.addLayer({
          id: "cells-heatmap",
          type: "heatmap",
          source: "cells-heatmap-source",
          maxzoom: 15,
          paint: getCellSiteHeatmapPaint(0.7),
          layout: {
            visibility: "none", // Start hidden, controlled by applyVisibility
          },
        }, beforeId);
      }

      // Point view layer - always add if in admin mode, control visibility separately
      if (!map.getLayer("cells-point-view")) {
        map.addLayer({
          id: "cells-point-view",
          type: "circle",
          source: "cells-heatmap-source",
          paint: getPointViewCirclePaint(),
          layout: {
            visibility: "none", // Start hidden, controlled by applyVisibility
          },
        }, beforeId);
      }

      // Signal coverage layer - always add if in admin mode, control visibility separately
      // Insert BELOW all cellsite layers (before cells-clusters, the lowest cellsite layer)
      // so that radio-coverage circles render underneath cell tower icons and heatmap.
      if (!map.getLayer("signal-coverage-circles")) {
        // addCellLayers() always runs synchronously before addAdminLayers() in the
        // same effect body, so cells-clusters should reliably be present here.
        // Fallback keeps the layer below any currently-available cell visual layer.
        const signalBeforeId = map.getLayer("cells-clusters")
          ? "cells-clusters"
          : map.getLayer("cells-circles")
            ? "cells-circles"
            : map.getLayer("cells-symbols")
              ? "cells-symbols"
              : beforeId;
        map.addLayer(
          {
            id: "signal-coverage-circles",
            type: "circle",
            source: "signal-coverage-source",
            paint: getSignalCoverageCirclePaint(0.8),
            layout: {
              visibility: "none", // Start hidden, controlled by applyVisibility
            },
          },
          signalBeforeId,
        );
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

    // Click handler for signal coverage points (admin-only)
    const handleSignalCoverageClick = (e: any) => {
      if (!options?.isAdminMode) return;
      const features = map.queryRenderedFeatures(e.point, { layers: ["signal-coverage-circles"] });
      if (features && features.length > 0) {
        const feature = features[0];
        if (!feature) return;
        const properties = feature.properties as { strength?: number; barangay?: string; azimuth?: string };

        if (properties) {
          const coordinates = (feature.geometry as any).coordinates.slice();

          // Ensure the popup appears over the correct feature
          while (Math.abs(e.lngLat.lng - coordinates[0]) > 180) {
            coordinates[0] += e.lngLat.lng > coordinates[0] ? 360 : -360;
          }

          popup.setLngLat(coordinates).setHTML(createSignalCoveragePopupHTML(properties as any)).addTo(map);
        }
      }
    };

    // Mouse enter/leave handlers for signal coverage (admin-only)
    const handleSignalCoverageEnter = () => {
      if (options?.isAdminMode) {
        map.getCanvas().style.cursor = "pointer";
      }
    };

    const handleSignalCoverageLeave = () => {
      if (options?.isAdminMode) {
        map.getCanvas().style.cursor = "";
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

    // Initialize
    void loadIcons();
    addCellsSource();
    addCellsHeatmapSource();
    addSignalCoverageSource();
    addCellLayers();
    addAdminLayers();

    // Enforce proper layer ordering after all layers are added
    // Use deferred setTimeout to ensure all concurrent layer additions complete first
    setTimeout(() => enforceCellLayerOrder(), 0);

    // Apply initial visibility immediately after adding layers
    const applyInitialVisibility = () => {
      const cellSitesVisible = normalizedConfig.cellSites && !normalizedConfig.pointView;

      setLayerVisible("cells-clusters", cellSitesVisible);
      setLayerVisible("cells-cluster-count", cellSitesVisible);
      setLayerVisible("cells-circles", cellSitesVisible);
      setLayerVisible("cells-symbols", cellSitesVisible);

      if (options?.isAdminMode) {
        setLayerVisible("cells-heatmap", normalizedConfig.cellSiteHeatmap);

        if (normalizedConfig.pointView) {
          setLayerVisible("cells-point-view", true);
          setLayerVisible("cells-clusters", false);
          setLayerVisible("cells-cluster-count", false);
          setLayerVisible("cells-circles", false);
          setLayerVisible("cells-symbols", false);
        } else {
          setLayerVisible("cells-point-view", false);
        }

        setLayerVisible("signal-coverage-circles", normalizedConfig.signalCoverage);
      }
    };

    // Apply visibility after layers are added
    applyInitialVisibility();

    // Attach click handlers
    map.on("click", "cells-circles", handleCellClick);
    map.on("click", "cells-symbols", handleCellClick);

    // Attach signal coverage handlers (admin-only)
    if (options?.isAdminMode) {
      map.on("click", "signal-coverage-circles", handleSignalCoverageClick);
      map.on("mouseenter", "signal-coverage-circles", handleSignalCoverageEnter);
      map.on("mouseleave", "signal-coverage-circles", handleSignalCoverageLeave);
    }

    // Handle style changes
    const handleStyleData = () => {
      if (isCleaningUpRef.current || !map.isStyleLoaded()) return;
      void loadIcons();
      addCellsSource();
      addCellsHeatmapSource();
      addSignalCoverageSource();
    };

    const handleSourceData = () => {
      if (isCleaningUpRef.current || !map.isStyleLoaded()) return;
      // Detect whether layers are about to be newly added (style-change recovery).
      // cells-clusters is the first cell layer added; if it is absent the whole
      // layer set is being re-created and we must re-apply the current visibility
      // after the add, because the layers start with visibility:"none".
      const layersWereMissing = !map.getLayer("cells-clusters");
      addCellLayers();
      addAdminLayers();
      if (layersWereMissing && map.getLayer("cells-clusters")) {
        // Re-apply the current visibility from the always-current ref so that
        // switching map styles doesn't leave every cell layer hidden.
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

        // Enforce proper layer ordering after re-adding layers on style change
        setTimeout(() => enforceCellLayerOrder(), 0);
      }
    };

    map.on("styledata", handleStyleData);
    map.on("sourcedata", handleSourceData);

    // Store handler references for cleanup
    handleStyleDataRef.current = handleStyleData;
    handleSourceDataRef.current = handleSourceData;

    return () => {
      // Prevent handlers from running during teardown
      isCleaningUpRef.current = true;

      // Remove event listeners first
      map.off("click", "cells-circles", handleCellClick);
      map.off("click", "cells-symbols", handleCellClick);

      if (options?.isAdminMode) {
        map.off("click", "signal-coverage-circles", handleSignalCoverageClick);
        map.off("mouseenter", "signal-coverage-circles", handleSignalCoverageEnter);
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
  }, [map, isMapLoaded, signalCoverageData, options?.isAdminMode, setLayerVisible]);

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
    if (!map || !isMapLoaded || !map.isStyleLoaded()) return;

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
    map.setFilter("signal-coverage-circles", [
      "all",
      [">=", ["get", "strength"], signalStrengthMin],
      ["<=", ["get", "strength"], signalStrengthMax],
    ]);
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
