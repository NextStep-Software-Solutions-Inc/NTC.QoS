"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import * as maptilersdk from "@maptiler/sdk";
import { toast } from "sonner";

import { Card, CardContent } from "@/components/ui/card";
import { cn } from "@/lib/utils";
import { copyMapScreenshotToClipboard, downloadMapScreenshot, exportMapScreenshotAsPDF } from "@/utils/map-screenshot";
import {
  ErrorOverlay,
  FilterPanel,
  LoadingOverlay,
  MapControls,
  MapHeader,
  ModeIndicator,
  ProviderLegend,
  SearchBar,
  ZoomLevelIndicator,
} from "./components";
import {
  CELL_ICON_SIZE,
  CLUSTER_MAX_ZOOM,
  CLUSTER_RADIUS,
  DEFAULT_LAYER_OPACITY,
  MAP_STYLES,
  MODE_CONFIG,
  PH_BOUNDS,
  PH_CENTER,
  PH_MIN_ZOOM,
  PH_ZOOM,
  POPUP_MAX_WIDTH,
  PROVIDER_ICONS,
  SIGNAL_COVERAGE_DATA_PATH,
  SIGNAL_STRENGTH_FILTERS,
  UNKNOWN_PROVIDER,
} from "./constants";
import {
  useAdvancedFilteredCells,
  useAvailableFilters,
  useCellsGeoJSON,
  useFilteredCells,
  useMergedCells,
  useProviderCounts,
} from "./hooks";
// Local imports
import {
  type CellMapDetails,
  type CellsiteMapFilters,
  type CellsiteMapViewProps,
  type LayerConfig,
  type LayerOpacity,
  type MapInteractionMode,
  type SignalStrengthFilter,
} from "./types";
import {
  createCellPopupHTML,
  createSignalCoveragePopupHTML,
  getCellCircleLayerPaint,
  getCellSiteHeatmapPaint,
  getClusterLayerPaint,
  getPointViewCirclePaint,
  getSignalCoverageCirclePaint,
} from "./utils";

import { getOrFetchIconBlob } from "@/hooks/map-cells-utils";

// Re-export types
export type { MapInteractionMode, CellsiteMapViewProps, CellMapDetails };

export default function CellsiteMapView({
  cells,
  className = "",
  isExpanded = false,
  onToggleExpand,
  onAddCell,
  onEditCell,
  onDeleteCell,
  onSelectCell,
  onViewCellDetails,
  onAdvancedFiltersChange,
  filterOptions,
  selectedCellId,
  optimisticCells = [],
  regionId,
  focusedLocation,
}: CellsiteMapViewProps) {
  // Refs
  const mapContainerRef = useRef<HTMLDivElement>(null);
  const mapRef = useRef<maptilersdk.Map | null>(null);
  const popupRef = useRef<maptilersdk.Popup | null>(null);
  const iconsLoadedRef = useRef<Set<string>>(new Set());
  const hasAutoCenteredRef = useRef(false);
  // Stores handler functions defined in map.on("load") so they can be
  // re-attached to layers after a style change clears all layers.
  const layerHandlersRef = useRef<{
    clusterClick: ((e: maptilersdk.MapMouseEvent) => void) | null;
    cellClick: ((e: maptilersdk.MapMouseEvent) => void) | null;
    setCursorPointer: (() => void) | null;
    resetCursor: (() => void) | null;
  }>({ clusterClick: null, cellClick: null, setCursorPointer: null, resetCursor: null });
  const hasLocationFocusedRef = useRef(false);
  const initializedProviderVisibilityRef = useRef(false);
  const previousProviderUniverseSizeRef = useRef(0);

  // Use refs to store current values for event handlers (avoids stale closures)
  const interactionModeRef = useRef<MapInteractionMode>("view");
  const onAddCellRef = useRef(onAddCell);
  const onEditCellRef = useRef(onEditCell);
  const onDeleteCellRef = useRef(onDeleteCell);
  const onSelectCellRef = useRef(onSelectCell);
  const onViewCellDetailsRef = useRef(onViewCellDetails);

  // Update refs when props change
  useEffect(() => {
    onAddCellRef.current = onAddCell;
    onEditCellRef.current = onEditCell;
    onDeleteCellRef.current = onDeleteCell;
    onSelectCellRef.current = onSelectCell;
    onViewCellDetailsRef.current = onViewCellDetails;
  }, [onAddCell, onEditCell, onDeleteCell, onSelectCell, onViewCellDetails]);

  // State
  const [mapLoaded, setMapLoaded] = useState(false);
  const [isMapIdle, setIsMapIdle] = useState(false);
  const [mapError, setMapError] = useState<string | null>(null);
  const [interactionMode, setInteractionMode] = useState<MapInteractionMode>("view");
  const [searchQuery, setSearchQuery] = useState("");
  const [showSearch, setShowSearch] = useState(false);
  const [currentMapStyle, setCurrentMapStyle] = useState("topo");
  const [showLegend, setShowLegend] = useState(true);
  const [visibleProviders, setVisibleProviders] = useState<Set<string>>(new Set());
  const [zoomLevel, setZoomLevel] = useState(PH_ZOOM);
  const [showFilterPanel, setShowFilterPanel] = useState(false);
  const [signalCoverageData, setSignalCoverageData] = useState<GeoJSON.FeatureCollection | null>(null);

  // Layer controls state
  const [layerConfig, setLayerConfig] = useState<LayerConfig>({
    cellSites: true,
    cellSiteHeatmap: false,
    signalCoverage: false,
    pointView: false,
  });
  const [layerOpacity, setLayerOpacity] = useState<LayerOpacity>(DEFAULT_LAYER_OPACITY);

  // Signal strength filter state
  const [signalStrengthFilter, setSignalStrengthFilter] = useState<SignalStrengthFilter>("all");

  // Terrain and 3D view state
  const [enableTerrain, setEnableTerrain] = useState(false);
  const [enable3D, setEnable3D] = useState(false);

  // Filter state
  const [filters, setFilters] = useState<CellsiteMapFilters>({
    providers: [],
    generations: [],
    bands: [],
    technologies: [],
    search: "",
  });

  // Update mode ref when state changes
  useEffect(() => {
    interactionModeRef.current = interactionMode;
    // Update cursor
    if (mapRef.current && mapLoaded) {
      mapRef.current.getCanvas().style.cursor = MODE_CONFIG[interactionMode].cursor;
    }
  }, [interactionMode, mapLoaded]);

  // Computed data using custom hooks
  const allCells = useMergedCells(cells, optimisticCells);
  const filteredByProvider = useFilteredCells(allCells, visibleProviders, searchQuery);
  const filteredCells = useAdvancedFilteredCells(filteredByProvider, filters);
  const cellsGeoJSON = useCellsGeoJSON(filteredCells, selectedCellId);
  const providerCounts = useProviderCounts(allCells, visibleProviders);
  const derivedFilters = useAvailableFilters(allCells);

  const availableGenerations = filterOptions?.generations?.length
    ? filterOptions.generations
    : derivedFilters.generations;
  const availableBands = filterOptions?.bands?.length ? filterOptions.bands : derivedFilters.bands;
  const availableTechnologies = filterOptions?.technologies?.length
    ? filterOptions.technologies
    : derivedFilters.technologies;

  // Active filter count
  const activeFilterCount = [
    derivedFilters.providers.length > 0 && visibleProviders.size !== derivedFilters.providers.length,
    filters.generations.length > 0,
    filters.bands.length > 0,
    filters.technologies.length > 0,
  ].filter(Boolean).length;

  // Keep a ref to allCells for event handlers
  const allCellsRef = useRef(allCells);
  useEffect(() => {
    allCellsRef.current = allCells;
  }, [allCells]);

  // Initialize visible providers when cells change
  useEffect(() => {
    const providerUniverse = new Set(allCells.map((cell) => cell.providerName || UNKNOWN_PROVIDER));
    const providerList = Array.from(providerUniverse);

    setVisibleProviders((prev) => {
      if (!initializedProviderVisibilityRef.current) {
        initializedProviderVisibilityRef.current = true;
        previousProviderUniverseSizeRef.current = providerUniverse.size;
        return new Set(providerList);
      }

      const wasAllSelected = prev.size === previousProviderUniverseSizeRef.current;
      previousProviderUniverseSizeRef.current = providerUniverse.size;

      if (wasAllSelected) {
        return new Set(providerList);
      }

      return new Set(Array.from(prev).filter((provider) => providerUniverse.has(provider)));
    });
  }, [allCells]);

  useEffect(() => {
    hasAutoCenteredRef.current = false;
  }, [regionId]);

  useEffect(() => {
    onAdvancedFiltersChange?.({
      generations: filters.generations,
      bands: filters.bands,
      technologies: filters.technologies,
    });
  }, [filters.generations, filters.bands, filters.technologies, onAdvancedFiltersChange]);

  // Listen for viewCellDetails custom event from popup
  useEffect(() => {
    const handleViewDetails = (e: CustomEvent<{ cellId: string }>) => {
      if (onViewCellDetailsRef.current) {
        onViewCellDetailsRef.current(e.detail.cellId);
      }
    };

    window.addEventListener("viewCellDetails", handleViewDetails as EventListener);
    return () => {
      window.removeEventListener("viewCellDetails", handleViewDetails as EventListener);
    };
  }, []);

  // Load signal coverage data
  useEffect(() => {
    if (layerConfig.signalCoverage && !signalCoverageData) {
      fetch(SIGNAL_COVERAGE_DATA_PATH)
        .then((res) => res.json())
        .then((data: Array<{ lat: number; lng: number; strength: number; barangay?: string; azimuth?: string }>) => {
          // Convert to GeoJSON
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
  }, [layerConfig.signalCoverage, signalCoverageData]);

  // Popup management
  const showCellPopup = useCallback((cell: CellMapDetails, coordinates: [number, number]) => {
    if (!mapRef.current) return;

    if (popupRef.current) {
      popupRef.current.remove();
    }

    const popup = new maptilersdk.Popup({
      closeButton: true,
      closeOnClick: false,
      maxWidth: POPUP_MAX_WIDTH,
      className: "cell-popup",
    })
      .setLngLat(coordinates)
      .setHTML(createCellPopupHTML(cell, !!onViewCellDetailsRef.current))
      .addTo(mapRef.current);

    popupRef.current = popup;
  }, []);

  // Zoom to cell
  const zoomToCell = useCallback(
    (cell: CellMapDetails) => {
      if (!mapRef.current) return;
      mapRef.current.flyTo({ center: [cell.lng, cell.lat], zoom: 16, duration: 1000 });
      showCellPopup(cell, [cell.lng, cell.lat]);
    },
    [showCellPopup],
  );

  // Zoom to selected cell when it changes (only in view mode)
  useEffect(() => {
    // Don't fly to cell in add mode - user is positioning for new cell
    if (interactionMode === "add") return;

    if (selectedCellId && mapRef.current && mapLoaded) {
      const cell = allCells.find((c) => c.id === selectedCellId);
      if (cell) {
        zoomToCell(cell);
      }
    }
  }, [selectedCellId, allCells, mapLoaded, zoomToCell, interactionMode]);

  // Load provider icons using global app-wide icon cache
  const loadProviderIcons = useCallback(async (map: maptilersdk.Map) => {
    const iconPromises = Object.entries(PROVIDER_ICONS).map(async ([key, url]) => {
      const iconId = key === "default" ? "cell-default" : `cell-${key.toLowerCase()}`;
      if (map.hasImage?.(iconId)) {
        iconsLoadedRef.current.add(iconId);
        return;
      }

      try {
        const blob = await getOrFetchIconBlob(url);
        const imageBitmap = await createImageBitmap(blob);

        if (!map.hasImage(iconId)) {
          map.addImage(iconId, imageBitmap);
          iconsLoadedRef.current.add(iconId);
        }
      } catch (err) {
        console.warn(`Failed to load icon ${iconId}:`, err);
      }
    });

    await Promise.all(iconPromises);
  }, []);

  // Initialize MapTiler map
  useEffect(() => {
    if (!mapContainerRef.current || mapRef.current) return;

    const initMap = async () => {
      try {
        const apiKey = process.env.NEXT_PUBLIC_MAPTILER_API_KEY;
        if (!apiKey) {
          setMapError("MapTiler API key not configured");
          return;
        }
        maptilersdk.config.apiKey = apiKey;

        const map = new maptilersdk.Map({
          container: mapContainerRef.current!,
          style: maptilersdk.MapStyle.TOPO,
          center: PH_CENTER,
          zoom: PH_ZOOM,
          minZoom: PH_MIN_ZOOM,
          attributionControl: false,
          maxBounds: PH_BOUNDS,
          // Disable default navigation controls
          navigationControl: false,
          geolocateControl: false,
          scaleControl: false,
          fullscreenControl: false,
          terrainControl: false,
          // Required for canvas.toDataURL() to return non-blank screenshots
          canvasContextAttributes: { preserveDrawingBuffer: true },
        });

        map.on("load", async () => {
          // Load provider icons
          await loadProviderIcons(map);

          setMapLoaded(true);

          // Add GeoJSON source for cells with clustering (for markers)
          map.addSource("cells", {
            type: "geojson",
            data: cellsGeoJSON,
            cluster: true,
            clusterMaxZoom: CLUSTER_MAX_ZOOM,
            clusterRadius: CLUSTER_RADIUS,
          });

          // Add separate non-clustered source for heatmap
          map.addSource("cells-heatmap-source", { type: "geojson", data: cellsGeoJSON, cluster: false });

          // Cluster circles layer
          map.addLayer({
            id: "cells-clusters",
            type: "circle",
            source: "cells",
            filter: ["has", "point_count"],
            paint: getClusterLayerPaint(),
          });

          // Cluster count labels
          map.addLayer({
            id: "cells-cluster-count",
            type: "symbol",
            source: "cells",
            filter: ["has", "point_count"],
            layout: { "text-field": "{point_count_abbreviated}", "text-font": ["Open Sans Bold"], "text-size": 11 },
            paint: { "text-color": "#ffffff" },
          });

          // Individual cell circles (fallback if icons don't load)
          map.addLayer({
            id: "cells-circles",
            type: "circle",
            source: "cells",
            filter: ["!has", "point_count"],
            paint: getCellCircleLayerPaint(),
          });

          // Symbol layer with provider icons (on top of circles)
          // icon-anchor: bottom positions the icon above the coordinate point
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
              "icon-anchor": "bottom",
            },
          });

          // Click handler for clusters - zoom in
          const handleClusterClick = async (e: maptilersdk.MapMouseEvent) => {
            const features = map.queryRenderedFeatures(e.point, { layers: ["cells-clusters"] });
            if (!features.length || !features[0]) return;

            const clusterId = features[0].properties?.cluster_id;
            if (!clusterId) return;

            const source = map.getSource("cells") as maptilersdk.GeoJSONSource;

            try {
              const zoom = await source.getClusterExpansionZoom(clusterId);
              const geometry = features[0].geometry as GeoJSON.Point;
              map.easeTo({ center: geometry.coordinates as [number, number], zoom: zoom || 14 });
            } catch (err) {
              console.error("Failed to get cluster expansion zoom:", err);
            }
          };
          map.on("click", "cells-clusters", handleClusterClick);

          // Click handler for individual cells - uses refs to get current values
          const handleCellClick = (e: maptilersdk.MapMouseEvent) => {
            const features = map.queryRenderedFeatures(e.point, { layers: ["cells-circles", "cells-symbols"] });
            if (!features.length || !features[0]) return;

            const props = features[0].properties as {
              id: string;
              name: string;
              providerName: string;
              providerColor: string;
              status: string;
              lat: number;
              lng: number;
              siteId?: string;
              towerType?: string;
              isVerified?: boolean;
            };

            const cell = allCellsRef.current.find((c) => c.id === props.id);
            if (!cell) return;

            const currentMode = interactionModeRef.current;
            const geometry = features[0].geometry as GeoJSON.Point;
            const coordinates = geometry.coordinates as [number, number];

            // Handle based on current mode
            switch (currentMode) {
              case "edit":
                onEditCellRef.current?.(cell);
                break;
              case "delete":
                onDeleteCellRef.current?.(cell);
                break;
              case "view":
              default:
                onSelectCellRef.current?.(cell);
                // Show popup
                if (popupRef.current) {
                  popupRef.current.remove();
                }
                const popup = new maptilersdk.Popup({
                  closeButton: true,
                  closeOnClick: false,
                  maxWidth: POPUP_MAX_WIDTH,
                  className: "cell-popup",
                })
                  .setLngLat(coordinates)
                  .setHTML(createCellPopupHTML(cell, !!onViewCellDetailsRef.current))
                  .addTo(map);
                popupRef.current = popup;
                break;
            }
          };

          map.on("click", "cells-circles", handleCellClick);
          map.on("click", "cells-symbols", handleCellClick);

          // Cursor changes
          const setCursorPointer = () => {
            map.getCanvas().style.cursor = "pointer";
          };
          const resetCursor = () => {
            map.getCanvas().style.cursor = MODE_CONFIG[interactionModeRef.current].cursor;
          };

          map.on("mouseenter", "cells-circles", setCursorPointer);
          map.on("mouseleave", "cells-circles", resetCursor);
          map.on("mouseenter", "cells-symbols", setCursorPointer);
          map.on("mouseleave", "cells-symbols", resetCursor);
          map.on("mouseenter", "cells-clusters", setCursorPointer);
          map.on("mouseleave", "cells-clusters", resetCursor);

          // Store handler references so handleChangeStyle can re-attach them after a style change.
          layerHandlersRef.current = { clusterClick: handleClusterClick, cellClick: handleCellClick, setCursorPointer, resetCursor };

          // Track zoom level
          map.on("zoom", () => {
            setZoomLevel(Math.round(map.getZoom()));
          });
        });

        // Map click for adding (only on empty space) - uses refs
        map.on("click", (e) => {
          const currentMode = interactionModeRef.current;
          if (currentMode !== "add" || !onAddCellRef.current) return;

          const features = map.queryRenderedFeatures(e.point, {
            layers: ["cells-circles", "cells-symbols", "cells-clusters"],
          });

          if (features.length === 0) {
            const { lng, lat } = e.lngLat;
            onAddCellRef.current(lat, lng);
          }
        });

        mapRef.current = map;
      } catch (err) {
        console.error("MapTiler initialization error:", err);
        setMapError("Failed to load map");
      }
    };

    initMap();

    return () => {
      if (popupRef.current) {
        popupRef.current.remove();
      }
      if (mapRef.current) {
        mapRef.current.remove();
        mapRef.current = null;
      }
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps -- Map initialization should only run once on mount. Dependencies like cellsGeoJSON are handled via refs and separate useEffect hooks to avoid reinitializing the map.
  }, []);

  // Update GeoJSON sources when data changes
  useEffect(() => {
    if (!mapRef.current || !mapLoaded) return;

    // Update clustered source for markers
    const source = mapRef.current.getSource("cells");
    if (source && "setData" in source) {
      (source as maptilersdk.GeoJSONSource).setData(cellsGeoJSON);
    }

    // Update non-clustered source for heatmap
    const heatmapSource = mapRef.current.getSource("cells-heatmap-source");
    if (heatmapSource && "setData" in heatmapSource) {
      (heatmapSource as maptilersdk.GeoJSONSource).setData(cellsGeoJSON);
    }
  }, [cellsGeoJSON, mapLoaded]);

  // Resize map when expanded
  useEffect(() => {
    if (mapRef.current && mapLoaded) {
      setTimeout(() => {
        mapRef.current?.resize();
      }, 100);
    }
  }, [isExpanded, mapLoaded]);

  // Focus map to an explicit location passed from query params (e.g., "View on Map" action).
  useEffect(() => {
    if (!mapRef.current || !mapLoaded || !focusedLocation || hasLocationFocusedRef.current) {
      return;
    }
    hasLocationFocusedRef.current = true;
    mapRef.current.flyTo({ center: [focusedLocation.lng, focusedLocation.lat], zoom: 16, duration: 900 });
  }, [focusedLocation, mapLoaded]);

  // Reset hasLocationFocusedRef when focusedLocation changes to a new value
  useEffect(() => {
    hasLocationFocusedRef.current = false;
  }, [focusedLocation]);

  // Auto-center map to loaded cells for the current region/location once data is available.
  useEffect(() => {
    if (
      !mapRef.current ||
      !mapLoaded ||
      hasAutoCenteredRef.current ||
      allCells.length === 0 ||
      selectedCellId ||
      (focusedLocation && !hasLocationFocusedRef.current)
    ) {
      return;
    }

    const map = mapRef.current;
    hasAutoCenteredRef.current = true;

    if (allCells.length === 1) {
      const onlyCell = allCells[0];
      if (!onlyCell) return;
      map.flyTo({ center: [onlyCell.lng, onlyCell.lat], zoom: 14, duration: 900 });
      return;
    }

    const bounds = new maptilersdk.LngLatBounds();
    allCells.forEach((cell) => bounds.extend([cell.lng, cell.lat]));

    map.fitBounds(bounds, {
      padding: { top: 80, right: 80, bottom: 80, left: 80 },
      duration: 900,
      maxZoom: 13,
    });
  }, [allCells, mapLoaded, selectedCellId, focusedLocation]);

  // Manage cell sites layer visibility
  useEffect(() => {
    if (!mapRef.current || !mapLoaded) return;
    const map = mapRef.current;

    const visibility = layerConfig.cellSites ? "visible" : "none";
    const opacity = layerOpacity.cellSites;

    if (map.getLayer("cells-clusters")) {
      map.setLayoutProperty("cells-clusters", "visibility", visibility);
      map.setPaintProperty("cells-clusters", "circle-opacity", opacity);
    }
    if (map.getLayer("cells-cluster-count")) {
      map.setLayoutProperty("cells-cluster-count", "visibility", visibility);
    }
    if (map.getLayer("cells-circles")) {
      map.setLayoutProperty("cells-circles", "visibility", visibility);
      map.setPaintProperty("cells-circles", "circle-opacity", opacity);
    }
    if (map.getLayer("cells-symbols")) {
      map.setLayoutProperty("cells-symbols", "visibility", visibility);
      map.setPaintProperty("cells-symbols", "icon-opacity", opacity);
    }
  }, [layerConfig.cellSites, layerOpacity.cellSites, mapLoaded]);

  // Manage cell site heatmap layer
  useEffect(() => {
    if (!mapRef.current || !mapLoaded) return;
    const map = mapRef.current;

    if (layerConfig.cellSiteHeatmap) {
      // Add heatmap layer using non-clustered source if not exists
      if (!map.getLayer("cells-heatmap")) {
        map.addLayer(
          {
            id: "cells-heatmap",
            type: "heatmap",
            source: "cells-heatmap-source",
            maxzoom: 15,
            paint: getCellSiteHeatmapPaint(layerOpacity.cellSiteHeatmap),
          },
          "cells-clusters",
        );
      } else {
        map.setPaintProperty("cells-heatmap", "heatmap-opacity", layerOpacity.cellSiteHeatmap);
      }
    } else {
      // Remove heatmap layer if exists
      if (map.getLayer("cells-heatmap")) {
        map.removeLayer("cells-heatmap");
      }
    }
  }, [layerConfig.cellSiteHeatmap, layerOpacity.cellSiteHeatmap, mapLoaded]);

  // Create filtered signal coverage data based on signal strength filter
  const filteredSignalCoverageData = useCallback(() => {
    if (!signalCoverageData) return null;
    if (signalStrengthFilter === "all") return signalCoverageData;

    // Find the filter config
    const filterConfig = SIGNAL_STRENGTH_FILTERS.find((f) => f.id === signalStrengthFilter);
    if (!filterConfig) return signalCoverageData;

    const filteredFeatures = signalCoverageData.features.filter((feature) => {
      const strength = feature.properties?.strength as number;
      if (typeof strength !== "number") return false;
      return strength >= filterConfig.minRssi && strength <= filterConfig.maxRssi;
    });

    return { type: "FeatureCollection" as const, features: filteredFeatures };
  }, [signalCoverageData, signalStrengthFilter]);

  // Manage signal coverage circle layer (color-coded by RSSI)
  useEffect(() => {
    if (!mapRef.current || !mapLoaded) return;
    const map = mapRef.current;

    const filteredData = filteredSignalCoverageData();

    if (layerConfig.signalCoverage && filteredData) {
      // Add source if not exists
      if (!map.getSource("signal-coverage-source")) {
        map.addSource("signal-coverage-source", { type: "geojson", data: filteredData });
      } else {
        (map.getSource("signal-coverage-source") as maptilersdk.GeoJSONSource).setData(filteredData);
      }

      // Find the first symbol layer to place coverage circles before it (after base layers like water)
      const layers = map.getStyle()?.layers;
      let beforeLayer: string | undefined;
      if (layers) {
        for (const layer of layers) {
          if (layer.type === "symbol" && layer.id === "Aerialway labels") {
            beforeLayer = layer.id;
            break;
          }
        }
      }

      // Add circle layer if not exists (color-coded by signal strength)
      // Place it before symbol layers so it appears above base layers but below labels
      if (!map.getLayer("signal-coverage-circles")) {
        map.addLayer(
          {
            id: "signal-coverage-circles",
            type: "circle",
            source: "signal-coverage-source",
            paint: getSignalCoverageCirclePaint(layerOpacity.signalCoverage),
          },
          beforeLayer,
        );

        // Add click handler for signal coverage popup
        map.on("click", "signal-coverage-circles", (e: any) => {
          const features = map.queryRenderedFeatures(e.point, { layers: ["signal-coverage-circles"] });
          if (!features || features.length === 0) return;
          const feature = features[0];
          if (!feature || !feature.geometry || feature.geometry.type !== "Point") return;

          const geometry = feature.geometry as GeoJSON.Point;
          const coordinates = geometry.coordinates.slice() as [number, number];
          const properties = (feature.properties ?? {}) as { strength?: number; barangay?: string; azimuth?: string };

          // Close existing popup
          if (popupRef.current) {
            popupRef.current.remove();
          }

          // Create new popup
          const popup = new maptilersdk.Popup({
            closeButton: true,
            closeOnClick: true,
            maxWidth: POPUP_MAX_WIDTH,
            className: "signal-popup",
          })
            .setLngLat(coordinates)
            .setHTML(createSignalCoveragePopupHTML(properties as any))
            .addTo(map);

          popupRef.current = popup;
        });

        // Cursor change for signal coverage
        map.on("mouseenter", "signal-coverage-circles", () => {
          map.getCanvas().style.cursor = "pointer";
        });
        map.on("mouseleave", "signal-coverage-circles", () => {
          map.getCanvas().style.cursor = MODE_CONFIG[interactionModeRef.current].cursor;
        });
      } else {
        map.setPaintProperty("signal-coverage-circles", "circle-opacity", layerOpacity.signalCoverage);
      }
    } else {
      // Remove layer if exists
      if (map.getLayer("signal-coverage-circles")) {
        map.removeLayer("signal-coverage-circles");
      }
    }
  }, [
    layerConfig.signalCoverage,
    layerConfig.cellSiteHeatmap,
    layerOpacity.signalCoverage,
    filteredSignalCoverageData,
    mapLoaded,
  ]);

  // Manage point view layer (non-clustered small dots)
  useEffect(() => {
    if (!mapRef.current || !mapLoaded) return;
    const map = mapRef.current;

    if (layerConfig.pointView) {
      // Add point view layer if not exists
      if (!map.getLayer("cells-point-view")) {
        map.addLayer({
          id: "cells-point-view",
          type: "circle",
          source: "cells-heatmap-source",
          paint: getPointViewCirclePaint(),
        });
      }
      // Set opacity
      map.setPaintProperty("cells-point-view", "circle-opacity", layerOpacity.pointView);

      // Hide cluster layers when point view is active
      if (map.getLayer("cells-clusters")) {
        map.setLayoutProperty("cells-clusters", "visibility", "none");
      }
      if (map.getLayer("cells-cluster-count")) {
        map.setLayoutProperty("cells-cluster-count", "visibility", "none");
      }
      if (map.getLayer("cells-circles")) {
        map.setLayoutProperty("cells-circles", "visibility", "none");
      }
      if (map.getLayer("cells-symbols")) {
        map.setLayoutProperty("cells-symbols", "visibility", "none");
      }
    } else {
      // Remove point view layer if exists
      if (map.getLayer("cells-point-view")) {
        map.removeLayer("cells-point-view");
      }
      // Show cluster layers (unless heatmap is hiding them or cellSites is off)
      if (layerConfig.cellSites && !layerConfig.cellSiteHeatmap) {
        if (map.getLayer("cells-clusters")) {
          map.setLayoutProperty("cells-clusters", "visibility", "visible");
        }
        if (map.getLayer("cells-cluster-count")) {
          map.setLayoutProperty("cells-cluster-count", "visibility", "visible");
        }
        if (map.getLayer("cells-circles")) {
          map.setLayoutProperty("cells-circles", "visibility", "visible");
        }
        if (map.getLayer("cells-symbols")) {
          map.setLayoutProperty("cells-symbols", "visibility", "visible");
        }
      }
    }
  }, [layerConfig.pointView, layerConfig.cellSites, layerConfig.cellSiteHeatmap, layerOpacity.pointView, mapLoaded]);

  // Manage terrain/3D view
  useEffect(() => {
    if (!mapRef.current || !mapLoaded) return;
    const map = mapRef.current;

    // Enable/disable terrain
    if (enableTerrain) {
      try {
        // Add terrain source if not exists - must be added before setTerrain
        if (!map.getSource("terrain-dem")) {
          map.addSource("terrain-dem", {
            type: "raster-dem",
            url: `https://api.maptiler.com/tiles/terrain-rgb-v2/tiles.json?key=${process.env.NEXT_PUBLIC_MAPTILER_API_KEY}`,
            tileSize: 256,
          });
        }
        // Now set the terrain
        map.setTerrain({ source: "terrain-dem", exaggeration: 1.5 });
        // Set pitch for better 3D visualization
        if (map.getPitch() < 30) {
          map.easeTo({ pitch: 45, duration: 500 });
        }
      } catch (err) {
        console.warn("Failed to enable terrain:", err);
      }
    } else {
      try {
        // @ts-ignore - MapTiler SDK types don't include null for setTerrain
        map.setTerrain(null);
        // Reset pitch when terrain is disabled
        if (map.getPitch() > 0) {
          map.easeTo({ pitch: 0, duration: 500 });
        }
      } catch {
        // Ignore errors when disabling terrain
      }
    }
  }, [enableTerrain, mapLoaded]);

  // Manage 3D buildings
  useEffect(() => {
    if (!mapRef.current || !mapLoaded) return;
    const map = mapRef.current;

    if (enable3D) {
      // Set pitch for better 3D visualization
      if (map.getPitch() < 30) {
        map.easeTo({ pitch: 45, duration: 500 });
      }
    } else {
      // Reset pitch when 3D is disabled (only if terrain is also disabled)
      if (!enableTerrain && map.getPitch() > 0) {
        map.easeTo({ pitch: 0, duration: 500 });
      }
    }
  }, [enable3D, enableTerrain, mapLoaded]);

  // Map control handlers
  const handleZoomIn = useCallback(() => mapRef.current?.zoomIn(), []);
  const handleZoomOut = useCallback(() => mapRef.current?.zoomOut(), []);

  const handleResetView = useCallback(() => {
    mapRef.current?.flyTo({ center: PH_CENTER, zoom: PH_ZOOM, duration: 1000 });
  }, []);

  const handleLocateMe = useCallback(() => {
    if (!navigator.geolocation) return;
    navigator.geolocation.getCurrentPosition(
      (pos) => {
        mapRef.current?.flyTo({ center: [pos.coords.longitude, pos.coords.latitude], zoom: 14, duration: 1000 });
      },
      (err) => console.error("Geolocation error:", err),
    );
  }, []);

  // Parse "lat, lng" from the search bar and fly the map to those coordinates
  const handleCoordinateSearch = useCallback((query: string) => {
    const match = query.match(/^(-?\d+(?:\.\d+)?)\s*,\s*(-?\d+(?:\.\d+)?)$/);
    if (!match) return;
    const lat = parseFloat(match[1]!);
    const lng = parseFloat(match[2]!);
    if (!isFinite(lat) || !isFinite(lng)) return;
    mapRef.current?.flyTo({ center: [lng, lat], zoom: 16, duration: 900 });
  }, []);

  const handleChangeStyle = useCallback(
    async (styleId: string) => {
      const style = MAP_STYLES.find((s) => s.id === styleId);
      if (style && mapRef.current) {
        const map = mapRef.current;
        map.setStyle(style.style);
        setCurrentMapStyle(styleId);

        // Reload layers after style change.
        // 'styledata' fires once the style JSON is parsed and all existing
        // sources/layers have been cleared — the correct moment to re-add them.
        map.once("styledata", async () => {
          // Reload icons
          iconsLoadedRef.current.clear();
          await loadProviderIcons(map);

          // Re-add cells clustered source if it doesn't exist
          if (!map.getSource("cells")) {
            map.addSource("cells", {
              type: "geojson",
              data: cellsGeoJSON,
              cluster: true,
              clusterMaxZoom: CLUSTER_MAX_ZOOM,
              clusterRadius: CLUSTER_RADIUS,
            });
          }

          // Re-add cells non-clustered source for heatmap if it doesn't exist
          if (!map.getSource("cells-heatmap-source")) {
            map.addSource("cells-heatmap-source", { type: "geojson", data: cellsGeoJSON, cluster: false });
          }

          // Re-add layers
          if (!map.getLayer("cells-clusters")) {
            map.addLayer({
              id: "cells-clusters",
              type: "circle",
              source: "cells",
              filter: ["has", "point_count"],
              paint: getClusterLayerPaint(),
            });
          }

          if (!map.getLayer("cells-cluster-count")) {
            map.addLayer({
              id: "cells-cluster-count",
              type: "symbol",
              source: "cells",
              filter: ["has", "point_count"],
              layout: { "text-field": "{point_count_abbreviated}", "text-font": ["Open Sans Bold"], "text-size": 11 },
              paint: { "text-color": "#ffffff" },
            });
          }

          if (!map.getLayer("cells-circles")) {
            map.addLayer({
              id: "cells-circles",
              type: "circle",
              source: "cells",
              filter: ["!has", "point_count"],
              paint: getCellCircleLayerPaint(),
            });
          }

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
                "icon-anchor": "bottom",
              },
            });
          }

          // Add heatmap layer if enabled
          if (layerConfig.cellSiteHeatmap && !map.getLayer("cells-heatmap")) {
            map.addLayer(
              {
                id: "cells-heatmap",
                type: "heatmap",
                source: "cells-heatmap-source",
                maxzoom: 15,
                paint: getCellSiteHeatmapPaint(layerOpacity.cellSiteHeatmap),
              },
              "cells-clusters",
            );
          }

          // Re-add signal coverage if enabled
          if (layerConfig.signalCoverage && signalCoverageData) {
            if (!map.getSource("signal-coverage-source")) {
              map.addSource("signal-coverage-source", { type: "geojson", data: signalCoverageData });
            }
            if (!map.getLayer("signal-coverage-circles")) {
              map.addLayer(
                {
                  id: "signal-coverage-circles",
                  type: "circle",
                  source: "signal-coverage-source",
                  paint: getSignalCoverageCirclePaint(layerOpacity.signalCoverage),
                },
                "cells-clusters",
              );
            }
          }

          // Re-add point view if enabled
          if (layerConfig.pointView && !map.getLayer("cells-point-view")) {
            map.addLayer({
              id: "cells-point-view",
              type: "circle",
              source: "cells-heatmap-source",
              paint: getPointViewCirclePaint(),
            });
          }

          // Apply visibility settings
          const visibility = layerConfig.cellSites && !layerConfig.pointView ? "visible" : "none";
          if (map.getLayer("cells-clusters")) {
            map.setLayoutProperty("cells-clusters", "visibility", visibility);
          }
          if (map.getLayer("cells-cluster-count")) {
            map.setLayoutProperty("cells-cluster-count", "visibility", visibility);
          }
          if (map.getLayer("cells-circles")) {
            map.setLayoutProperty("cells-circles", "visibility", visibility);
          }
          if (map.getLayer("cells-symbols")) {
            map.setLayoutProperty("cells-symbols", "visibility", visibility);
          }

          // Re-attach layer click/cursor handlers (cleared when style was replaced).
          const h = layerHandlersRef.current;
          if (h.clusterClick) {
            map.off("click", "cells-clusters", h.clusterClick);
            map.on("click", "cells-clusters", h.clusterClick);
          }
          if (h.cellClick) {
            map.off("click", "cells-circles", h.cellClick);
            map.off("click", "cells-symbols", h.cellClick);
            map.on("click", "cells-circles", h.cellClick);
            map.on("click", "cells-symbols", h.cellClick);
          }
          if (h.setCursorPointer && h.resetCursor) {
            map.off("mouseenter", "cells-circles", h.setCursorPointer);
            map.off("mouseleave", "cells-circles", h.resetCursor);
            map.off("mouseenter", "cells-symbols", h.setCursorPointer);
            map.off("mouseleave", "cells-symbols", h.resetCursor);
            map.off("mouseenter", "cells-clusters", h.setCursorPointer);
            map.off("mouseleave", "cells-clusters", h.resetCursor);
            map.on("mouseenter", "cells-circles", h.setCursorPointer);
            map.on("mouseleave", "cells-circles", h.resetCursor);
            map.on("mouseenter", "cells-symbols", h.setCursorPointer);
            map.on("mouseleave", "cells-symbols", h.resetCursor);
            map.on("mouseenter", "cells-clusters", h.setCursorPointer);
            map.on("mouseleave", "cells-clusters", h.resetCursor);
          }
        });
      }
    },
    [cellsGeoJSON, layerConfig, layerOpacity, signalCoverageData, loadProviderIcons],
  );

  // Provider filter handlers
  const toggleProvider = useCallback((provider: string) => {
    setVisibleProviders((prev) => {
      const next = new Set(prev);
      if (next.has(provider)) {
        next.delete(provider);
      } else {
        next.add(provider);
      }
      return next;
    });
  }, []);

  const toggleAllProviders = useCallback(
    (show: boolean) => {
      if (show) {
        setVisibleProviders(new Set(Object.keys(providerCounts)));
      } else {
        setVisibleProviders(new Set());
      }
    },
    [providerCounts],
  );

  const handleDownloadPNG = useCallback(() => {
    const m = mapRef.current;
    if (!m) return;
    m.once("idle", () => { downloadMapScreenshot(m.getCanvas(), "cells"); });
    m.triggerRepaint();
  }, []);

  const handleDownloadPDF = useCallback(() => {
    const m = mapRef.current;
    if (!m) return;
    m.once("idle", () => { exportMapScreenshotAsPDF(m.getCanvas(), "cells"); });
    m.triggerRepaint();
  }, []);

  const handleCopyClipboard = useCallback(() => {
    const m = mapRef.current;
    if (!m) return;
    m.once("idle", () => {
      copyMapScreenshotToClipboard(m.getCanvas())
        .then(() => toast.success("Map copied to clipboard"))
        .catch(() => toast.error("Could not copy to clipboard"));
    });
    m.triggerRepaint();
  }, []);

  // Track map idle state for screenshot button gating
  useEffect(() => {
    const m = mapRef.current;
    if (!mapLoaded || !m) return;
    const onIdle = () => setIsMapIdle(true);
    const onMoveStart = () => setIsMapIdle(false);
    m.on("idle", onIdle);
    m.on("movestart", onMoveStart);
    return () => {
      m.off("idle", onIdle);
      m.off("movestart", onMoveStart);
    };
  }, [mapLoaded]);

  const handleProvidersChange = useCallback((providers: string[]) => {
    setVisibleProviders(new Set(providers));
  }, []);

  return (
    <Card className={cn("overflow-hidden flex flex-col h-full", className)}>
      {/* Header with mode controls, filter, layers, and style */}
      <MapHeader
        filteredCount={filteredCells.length}
        totalCount={allCells.length}
        showSearch={showSearch}
        onToggleSearch={() => setShowSearch(!showSearch)}
        currentMapStyle={currentMapStyle}
        onChangeStyle={handleChangeStyle}
        isExpanded={isExpanded}
        onToggleExpand={onToggleExpand}
        layerConfig={layerConfig}
        layerOpacity={layerOpacity}
        onLayerConfigChange={setLayerConfig}
        onLayerOpacityChange={setLayerOpacity}
        signalStrengthFilter={signalStrengthFilter}
        onSignalStrengthFilterChange={setSignalStrengthFilter}
        currentMode={interactionMode}
        onModeChange={setInteractionMode}
        hasAddHandler={!!onAddCell}
        hasEditHandler={!!onEditCell}
        hasDeleteHandler={!!onDeleteCell}
        showFilterPanel={showFilterPanel}
        onToggleFilterPanel={() => setShowFilterPanel(!showFilterPanel)}
        activeFilterCount={activeFilterCount}
      />

      {/* Search bar */}
      <SearchBar show={showSearch} value={searchQuery} onChange={setSearchQuery} onSearch={handleCoordinateSearch} />

      {/* Map Container with optional Filter Panel */}
      <CardContent className="p-0 flex-1 flex min-h-0 relative">
        {/* Filter Panel */}
        {showFilterPanel && (
          <FilterPanel
            show={showFilterPanel}
            onToggle={() => setShowFilterPanel(false)}
            filters={filters}
            onFiltersChange={setFilters}
            selectedProviders={Array.from(visibleProviders)}
            onProvidersChange={handleProvidersChange}
            availableProviders={derivedFilters.providers}
            availableGenerations={availableGenerations}
            availableBands={availableBands}
            availableTechnologies={availableTechnologies}
          />
        )}

        {/* Map */}
        <div className="flex-1 flex flex-col min-h-0">
          <div ref={mapContainerRef} className="flex-1 relative" style={{ minHeight: isExpanded ? "500px" : "350px" }}>
            <LoadingOverlay show={!mapLoaded && !mapError} />
            <ErrorOverlay error={mapError} />

            {mapLoaded && (
              <>
                <ModeIndicator currentMode={interactionMode} />
                <MapControls
                  onZoomIn={handleZoomIn}
                  onZoomOut={handleZoomOut}
                  onResetView={handleResetView}
                  onLocateMe={handleLocateMe}
                  enableTerrain={enableTerrain}
                  onTerrainChange={setEnableTerrain}
                  enable3D={enable3D}
                  on3DChange={setEnable3D}
                  onDownloadPNG={handleDownloadPNG}
                  onDownloadPDF={handleDownloadPDF}
                  onCopyClipboard={handleCopyClipboard}
                  isScreenshotReady={isMapIdle}
                />
                <ZoomLevelIndicator level={zoomLevel} />
              </>
            )}
          </div>

          {/* Legend */}
          <ProviderLegend
            show={showLegend}
            providerCounts={providerCounts}
            onToggle={() => setShowLegend(!showLegend)}
            onToggleProvider={toggleProvider}
            onToggleAll={toggleAllProviders}
          />
        </div>
      </CardContent>

      {/* Custom popup styles */}
      <style jsx global>{`
        .cell-popup .maplibregl-popup-content {
          padding: 0;
          border-radius: 12px;
          box-shadow: 0 4px 20px rgba(0, 0, 0, 0.2);
          overflow: hidden;
        }
        .cell-popup .maplibregl-popup-close-button {
          padding: 6px 10px;
          font-size: 18px;
          color: #6b7280;
          font-weight: 500;
        }
        .cell-popup .maplibregl-popup-close-button:hover {
          color: #1f2937;
          background: rgba(0, 0, 0, 0.05);
        }
        /* Signal coverage popup styles */
        .signal-popup .maplibregl-popup-content {
          padding: 0;
          border-radius: 12px;
          box-shadow: 0 10px 30px rgba(0, 0, 0, 0.2);
          overflow: hidden;
          background: transparent;
          border: none;
        }
        .signal-popup .maplibregl-popup-close-button {
          position: absolute;
          top: 8px;
          right: 8px;
          padding: 4px 8px;
          font-size: 18px;
          color: white;
          font-weight: 600;
          background: rgba(0, 0, 0, 0.3);
          border-radius: 6px;
          z-index: 10;
        }
        .signal-popup .maplibregl-popup-close-button:hover {
          background: rgba(0, 0, 0, 0.5);
          color: white;
        }
        .signal-popup .maplibregl-popup-tip {
          border-top-color: #f8fafc;
        }
      `}</style>
    </Card>
  );
}
