"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import * as maptilersdk from "@maptiler/sdk";
import { Network, Palette } from "lucide-react";
import { toast } from "sonner";

import { type NAPDetails, type NAPViewport } from "@/components/features/NAP/types/NAP";
import { Card, CardContent } from "@/components/ui/card";
import { PHILIPPINES_BOUNDS } from "@/lib/constants/map-bounds";
import { cn } from "@/lib/utils";
import { copyMapScreenshotToClipboard, downloadMapScreenshot, exportMapScreenshotAsPDF } from "@/utils/map-screenshot";
import { getBackendUrl } from "@/lib/api-utils";
// Worker types
import type {
  LineFeatureCollection,
  WorkerMessage as MapVizWorkerMessage,
  WorkerResponse as MapVizWorkerResponse,
} from "@/workers/nap-map-viz.worker";
import {
  ErrorOverlay,
  LoadingOverlay,
  MapControls,
  MapHeader,
  ModeToolbar,
  ProviderLegend,
  SearchBar,
  ZoomLevelIndicator,
} from "./components";
import {
  CLUSTER_MAX_ZOOM,
  CLUSTER_RADIUS,
  MAP_STYLES,
  MODE_CONFIG,
  PH_BOUNDS,
  PH_CENTER,
  PH_MIN_ZOOM,
  PH_ZOOM,
  UNKNOWN_PROVIDER,
} from "./constants";
import { useFilteredNAPs, useMergedNAPs, useNAPsGeoJSON, useProviderCounts } from "./hooks";
// Local imports
import { type MapInteractionMode, type NAPMapViewProps } from "./types";
import { createNAPPopupHTML, getClusterLayerPaint, getNAPCircleLayerPaint, getPointViewCirclePaint } from "./utils";

// Toast IDs for updates
const NETWORK_TOAST_ID = "network-progress";
const COLOR_TOAST_ID = "color-progress";
const VIEWPORT_CHANGE_DEBOUNCE_MS = 500;

// Re-export types
export type { MapInteractionMode, NAPMapViewProps };

export default function NAPMapView({
  naps,
  regionId,
  focusedLocation,
  className = "",
  isExpanded = false,
  onToggleExpand,
  onAddNAP,
  onEditNAP,
  onDeleteNAP,
  onSelectNAP,
  onViewportChange,
  selectedNAPId,
  optimisticNAPs = [],
}: NAPMapViewProps) {
  // Refs
  const mapContainerRef = useRef<HTMLDivElement>(null);
  const mapRef = useRef<maptilersdk.Map | null>(null);
  const popupRef = useRef<maptilersdk.Popup | null>(null);
  const workerRef = useRef<Worker | null>(null);
  const pendingNetworkRequestRef = useRef<boolean>(false);
  // Cache for network lines to avoid recalculating on style change
  const networkLinesCache = useRef<{ geojson: LineFeatureCollection; napIds: string } | null>(null);
  const hasAutoCenteredRef = useRef(false);
  const hasRegionZoomedRef = useRef(false);
  const hasLocationFocusedRef = useRef(false);
  const lastZoomedNAPIdRef = useRef<string | null>(null);
  // Track provider visibility state to preserve user selections across data updates
  const initializedProviderVisibilityRef = useRef(false);
  const previousProviderUniverseSizeRef = useRef(0);
  // Stores handler functions defined in map.on("load") so they can be
  // re-attached to layers after a style change clears all layers.
  const layerHandlersRef = useRef<{
    clusterClick: ((e: maptilersdk.MapMouseEvent) => void) | null;
    napClick: ((e: maptilersdk.MapMouseEvent) => void) | null;
    setCursorPointer: (() => void) | null;
    resetCursor: (() => void) | null;
  }>({ clusterClick: null, napClick: null, setCursorPointer: null, resetCursor: null });

  // Use refs to store current values for event handlers (avoids stale closures)
  const interactionModeRef = useRef<MapInteractionMode>("view");
  const onAddNAPRef = useRef(onAddNAP);
  const onEditNAPRef = useRef(onEditNAP);
  const onDeleteNAPRef = useRef(onDeleteNAP);
  const onSelectNAPRef = useRef(onSelectNAP);
  const onViewportChangeRef = useRef(onViewportChange);

  // Update refs when props change
  useEffect(() => {
    onAddNAPRef.current = onAddNAP;
    onEditNAPRef.current = onEditNAP;
    onDeleteNAPRef.current = onDeleteNAP;
    onSelectNAPRef.current = onSelectNAP;
    onViewportChangeRef.current = onViewportChange;
  }, [onAddNAP, onEditNAP, onDeleteNAP, onSelectNAP, onViewportChange]);

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
  const [showHeatmap, setShowHeatmap] = useState(false);
  const [showProviderNetwork, setShowProviderNetwork] = useState(false);
  const [showPointView, setShowPointView] = useState(false);
  const [isCalculatingColors, setIsCalculatingColors] = useState(false);

  // Update mode ref when state changes
  useEffect(() => {
    interactionModeRef.current = interactionMode;
    // Update cursor
    if (mapRef.current && mapLoaded) {
      mapRef.current.getCanvas().style.cursor = MODE_CONFIG[interactionMode].cursor;
    }
  }, [interactionMode, mapLoaded]);

  // Computed data using custom hooks
  const allNAPs = useMergedNAPs(naps, optimisticNAPs);
  const filteredNAPs = useFilteredNAPs(allNAPs, visibleProviders, searchQuery);
  const napsGeoJSON = useNAPsGeoJSON(filteredNAPs, selectedNAPId);
  const providerCounts = useProviderCounts(allNAPs, visibleProviders);

  // Keep a ref to allNAPs for event handlers
  const allNAPsRef = useRef(allNAPs);
  useEffect(() => {
    allNAPsRef.current = allNAPs;
  }, [allNAPs]);

  useEffect(() => {
    hasAutoCenteredRef.current = false;
    hasRegionZoomedRef.current = false;
    hasLocationFocusedRef.current = false;
  }, [regionId]);

  useEffect(() => {
    hasLocationFocusedRef.current = false;
  }, [focusedLocation?.lat, focusedLocation?.lng]);

  // Initialize visible providers when NAPs change with smart preservation of user selections
  // This prevents filter resets when NAP data updates
  useEffect(() => {
    const providerUniverse = new Set(allNAPs.map((nap) => nap.ISP || UNKNOWN_PROVIDER));
    const providerList = Array.from(providerUniverse);

    setVisibleProviders((prev) => {
      if (!initializedProviderVisibilityRef.current) {
        // First time initialization - show all providers
        initializedProviderVisibilityRef.current = true;
        previousProviderUniverseSizeRef.current = providerUniverse.size;
        return new Set(providerList);
      }

      const wasAllSelected = prev.size === previousProviderUniverseSizeRef.current;
      previousProviderUniverseSizeRef.current = providerUniverse.size;

      if (wasAllSelected) {
        // If user had ALL providers selected, keep all new providers selected too
        return new Set(providerList);
      }

      // Otherwise preserve user's manual selection
      return prev;
    });
  }, [allNAPs]);

  // Initialize and clean up web worker for heavy computations
  useEffect(() => {
    // Create worker
    workerRef.current = new Worker(new URL("@/workers/nap-map-viz.worker.ts", import.meta.url));

    // Handle worker messages
    workerRef.current.onmessage = (event: MessageEvent<MapVizWorkerResponse>) => {
      const response = event.data;

      if (response.type === "NETWORK_LINES_GENERATED") {
        pendingNetworkRequestRef.current = false;
        toast.success("Network lines generated", {
          id: NETWORK_TOAST_ID,
          description: `${response.lineCount} lines in ${response.duration.toFixed(0)}ms`,
          duration: 3000,
        });
        // Cache the network lines for reuse on style change
        const napIds = allNAPsRef.current
          .map((n) => n.id)
          .sort()
          .join(",");
        networkLinesCache.current = { geojson: response.geojson, napIds };
        // Update map source with network lines from worker
        if (mapRef.current && mapRef.current.getSource("provider-network-source")) {
          (mapRef.current.getSource("provider-network-source") as maptilersdk.GeoJSONSource).setData(response.geojson);
        }
      } else if (response.type === "NETWORK_LINES_PROGRESS") {
        toast.loading(
          <div className="flex items-center gap-3 w-full">
            <Network className="h-4 w-4 text-purple-500 animate-pulse" />
            <div className="flex-1 min-w-0">
              <div className="flex justify-between text-xs mb-1">
                <span className="truncate">Building: {response.currentProvider}</span>
              </div>
              <div className="h-1.5 bg-purple-100 rounded-full overflow-hidden">
                <div
                  className="h-full bg-purple-500 transition-all duration-300"
                  style={{ width: `${response.progress}%` }}
                />
              </div>
            </div>
          </div>,
          {
            id: NETWORK_TOAST_ID,
            description: `Processing provider ${response.processedProviders} of ${response.totalProviders} - ${response.progress}%`,
            duration: Infinity,
            cancel: {
              label: "Cancel",
              onClick: () => {
                workerRef.current?.postMessage({ type: "CANCEL" } as MapVizWorkerMessage);
              },
            },
            classNames: { content: "w-full" },
          },
        );
      } else if (response.type === "CLUSTER_COLORS_CALCULATED") {
        setIsCalculatingColors(false);
        toast.success("Dominant colors calculated", {
          id: COLOR_TOAST_ID,
          description: `${response.results.length} clusters in ${response.duration.toFixed(0)}ms`,
          duration: 3000,
        });
        // TODO: Apply colors to clusters when MapLibre supports dynamic cluster properties
      } else if (response.type === "CLUSTER_COLORS_PROGRESS") {
        toast.loading(
          <div className="flex items-center gap-3 w-full">
            <Palette className="h-4 w-4 text-amber-500 animate-pulse" />
            <div className="flex-1 min-w-0">
              <div className="flex justify-between text-xs mb-1">
                <span>
                  Colors: {response.processed}/{response.total}
                </span>
                <span className="font-medium">{response.progress}%</span>
              </div>
              <div className="h-1.5 bg-amber-100 rounded-full overflow-hidden">
                <div
                  className="h-full bg-amber-500 transition-all duration-300"
                  style={{ width: `${response.progress}%` }}
                />
              </div>
            </div>
          </div>,
          {
            id: COLOR_TOAST_ID,
            description: `Processing colors ${response.processed} of ${response.total} - ${response.progress}%`,
            duration: Infinity,
            cancel: {
              label: "Cancel",
              onClick: () => {
                workerRef.current?.postMessage({ type: "CANCEL" } as MapVizWorkerMessage);
              },
            },
            classNames: { content: "w-full" },
          },
        );
      } else if (response.type === "CANCELLED") {
        pendingNetworkRequestRef.current = false;
        setIsCalculatingColors(false);
        toast.dismiss(NETWORK_TOAST_ID);
        toast.dismiss(COLOR_TOAST_ID);
        toast.info("Operation cancelled");
      }
    };

    workerRef.current.onerror = (error) => {
      console.error("Worker error:", error);
      pendingNetworkRequestRef.current = false;
      setIsCalculatingColors(false);
      toast.dismiss(NETWORK_TOAST_ID);
      toast.dismiss(COLOR_TOAST_ID);
      toast.error("Worker error occurred");
    };

    return () => {
      workerRef.current?.terminate();
      workerRef.current = null;
    };
  }, []);

  // Popup management
  const showNAPPopup = useCallback((nap: NAPDetails, coordinates: [number, number]) => {
    if (!mapRef.current) return;

    if (popupRef.current) {
      popupRef.current.remove();
    }

    const popup = new maptilersdk.Popup({
      closeButton: true,
      closeOnClick: false,
      maxWidth: "280px",
      className: "nap-popup",
    })
      .setLngLat(coordinates)
      .setHTML(createNAPPopupHTML(nap))
      .addTo(mapRef.current);

    popupRef.current = popup;
  }, []);

  // Zoom to NAP
  const zoomToNAP = useCallback(
    (nap: NAPDetails) => {
      if (!mapRef.current) return;
      mapRef.current.flyTo({ center: [nap.lng, nap.lat], zoom: 16, duration: 1000 });
      showNAPPopup(nap, [nap.lng, nap.lat]);
    },
    [showNAPPopup],
  );

  // Zoom to selected NAP when it changes (only in view mode)
  useEffect(() => {
    // Don't fly to NAP in add mode - user is positioning for new NAP
    if (interactionMode === "add") return;
    // Only zoom/show popup when the selected NAP actually changes, not on allNAPs batch updates
    if (selectedNAPId === lastZoomedNAPIdRef.current) return;

    lastZoomedNAPIdRef.current = selectedNAPId ?? null;

    if (selectedNAPId && mapRef.current && mapLoaded) {
      const nap = allNAPs.find((n) => n.id === selectedNAPId);
      if (nap) {
        zoomToNAP(nap);
      }
    }
  }, [selectedNAPId, allNAPs, mapLoaded, zoomToNAP, interactionMode]);

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

        map.on("load", () => {
          setMapLoaded(true);

          let viewportTimer: ReturnType<typeof setTimeout> | null = null;
          const emitViewportBounds = () => {
            if (!onViewportChangeRef.current) return;
            const bounds = map.getBounds();
            onViewportChangeRef.current({
              minLat: bounds.getSouth(),
              maxLat: bounds.getNorth(),
              minLng: bounds.getWest(),
              maxLng: bounds.getEast(),
            });
          };
          const scheduleViewportBounds = () => {
            if (viewportTimer) {
              clearTimeout(viewportTimer);
            }
            viewportTimer = setTimeout(() => {
              emitViewportBounds();
            }, VIEWPORT_CHANGE_DEBOUNCE_MS);
          };

          // Initial map bounds once map is mounted and ready.
          emitViewportBounds();
          map.on("moveend", scheduleViewportBounds);
          map.on("zoomend", scheduleViewportBounds);

          // Add GeoJSON source for NAPs with clustering (for markers)
          map.addSource("naps", {
            type: "geojson",
            data: napsGeoJSON,
            cluster: true,
            clusterMaxZoom: CLUSTER_MAX_ZOOM,
            clusterRadius: CLUSTER_RADIUS,
          });

          // Add separate non-clustered source for heatmap
          map.addSource("naps-heatmap-source", { type: "geojson", data: napsGeoJSON, cluster: false });

          // Cluster circles layer
          map.addLayer({
            id: "naps-clusters",
            type: "circle",
            source: "naps",
            filter: ["has", "point_count"],
            paint: getClusterLayerPaint(),
          });

          // Cluster count labels
          map.addLayer({
            id: "naps-cluster-count",
            type: "symbol",
            source: "naps",
            filter: ["has", "point_count"],
            layout: { "text-field": "{point_count_abbreviated}", "text-font": ["Open Sans Bold"], "text-size": 12 },
            paint: { "text-color": "#ffffff" },
          });

          // Individual NAP circles
          map.addLayer({
            id: "naps-circles",
            type: "circle",
            source: "naps",
            filter: ["!has", "point_count"],
            paint: getNAPCircleLayerPaint(),
          });

          // Click handler for clusters - zoom in
          const handleNAPClusterClick = async (e: maptilersdk.MapMouseEvent) => {
            const features = map.queryRenderedFeatures(e.point, { layers: ["naps-clusters"] });
            if (!features.length || !features[0]) return;

            const clusterId = features[0].properties?.cluster_id;
            if (!clusterId) return;

            const source = map.getSource("naps") as maptilersdk.GeoJSONSource;

            try {
              const zoom = await source.getClusterExpansionZoom(clusterId);
              const geometry = features[0].geometry as GeoJSON.Point;
              map.easeTo({ center: geometry.coordinates as [number, number], zoom: zoom || 14 });
            } catch (err) {
              console.error("Failed to get cluster expansion zoom:", err);
            }
          };
          map.on("click", "naps-clusters", handleNAPClusterClick);

          // Click handler for individual NAPs - uses refs to get current values
          const handleNAPClick = (e: maptilersdk.MapMouseEvent) => {
            const features = map.queryRenderedFeatures(e.point, { layers: ["naps-circles"] });
            if (!features || !features[0]) return;

            const props = features[0].properties as {
              id: string;
              isp: string;
              ispColor: string;
              lat: number;
              lng: number;
            };

            const nap = allNAPsRef.current.find((n) => n.id === props.id);
            if (!nap) return;

            const currentMode = interactionModeRef.current;
            const geometry = features[0].geometry as GeoJSON.Point;
            const coordinates = geometry.coordinates as [number, number];

            // Handle based on current mode
            switch (currentMode) {
              case "edit":
                onEditNAPRef.current?.(nap);
                break;
              case "delete":
                onDeleteNAPRef.current?.(nap);
                break;
              case "view":
              default:
                onSelectNAPRef.current?.(nap);
                // Show popup
                if (popupRef.current) {
                  popupRef.current.remove();
                }
                const popup = new maptilersdk.Popup({
                  closeButton: true,
                  closeOnClick: false,
                  maxWidth: "280px",
                  className: "nap-popup",
                })
                  .setLngLat(coordinates)
                  .setHTML(createNAPPopupHTML(nap))
                  .addTo(map);
                popupRef.current = popup;
                break;
            }
          };
          map.on("click", "naps-circles", handleNAPClick);

          // Cursor changes
          const setCursorPointer = () => { map.getCanvas().style.cursor = "pointer"; };
          const resetCursor = () => { map.getCanvas().style.cursor = MODE_CONFIG[interactionModeRef.current].cursor; };
          map.on("mouseenter", "naps-circles", setCursorPointer);
          map.on("mouseleave", "naps-circles", resetCursor);
          map.on("mouseenter", "naps-clusters", setCursorPointer);
          map.on("mouseleave", "naps-clusters", resetCursor);

          // Store handler references so handleChangeStyle can re-attach them after a style change.
          layerHandlersRef.current = { clusterClick: handleNAPClusterClick, napClick: handleNAPClick, setCursorPointer, resetCursor };

          // Track zoom level
          map.on("zoom", () => {
            setZoomLevel(Math.round(map.getZoom()));
          });

          map.once("remove", () => {
            if (viewportTimer) {
              clearTimeout(viewportTimer);
            }
          });
        });

        // Map click for adding (only on empty space) - uses refs
        map.on("click", (e) => {
          const currentMode = interactionModeRef.current;
          if (currentMode !== "add" || !onAddNAPRef.current) return;

          const features = map.queryRenderedFeatures(e.point, { layers: ["naps-circles", "naps-clusters"] });

          if (features.length === 0) {
            const { lng, lat } = e.lngLat;
            onAddNAPRef.current(lat, lng);
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
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  // Update GeoJSON sources when data changes
  useEffect(() => {
    if (!mapRef.current || !mapLoaded) return;

    // Update clustered source for markers
    const source = mapRef.current.getSource("naps");
    if (source && "setData" in source) {
      (source as maptilersdk.GeoJSONSource).setData(napsGeoJSON);
    }

    // Update non-clustered source for heatmap
    const heatmapSource = mapRef.current.getSource("naps-heatmap-source");
    if (heatmapSource && "setData" in heatmapSource) {
      (heatmapSource as maptilersdk.GeoJSONSource).setData(napsGeoJSON);
    }
  }, [napsGeoJSON, mapLoaded]);

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

  // Preliminary region zoom while region-scoped NAP data is still loading.
  useEffect(() => {
    if (!mapRef.current || !mapLoaded || !regionId || hasRegionZoomedRef.current || allNAPs.length > 0) {
      return;
    }

    hasRegionZoomedRef.current = true;
    let cancelled = false;

    const fetchAndZoomToFirstRegionNAP = async () => {
      const params = new URLSearchParams({
        pageNumber: "1",
        pageSize: "1",
        regionIds: regionId,
        minLat: String(PHILIPPINES_BOUNDS.minLat),
        maxLat: String(PHILIPPINES_BOUNDS.maxLat),
        minLng: String(PHILIPPINES_BOUNDS.minLng),
        maxLng: String(PHILIPPINES_BOUNDS.maxLng),
      });

      try {
        const backendUrl = getBackendUrl();
        const res = await fetch(`${backendUrl}/v2/NAPs/viewport/public?${params.toString()}`, {
          headers: { Accept: "application/json" },
        });
        const json = (await res.json()) as { data?: NAPViewport[] };
        const firstNAP = json.data?.[0];

        if (cancelled || !firstNAP || !mapRef.current) return;
        mapRef.current.flyTo({ center: [firstNAP.lng, firstNAP.lat], zoom: 9, duration: 900 });
      } catch (error) {
        console.error("Failed to fetch NAP for initial region zoom:", error);
      }
    };

    void fetchAndZoomToFirstRegionNAP();

    return () => {
      cancelled = true;
    };
  }, [regionId, mapLoaded, allNAPs.length]);

  // Auto-center map to loaded NAPs for the active region/location once data is available.
  useEffect(() => {
    if (
      !mapRef.current ||
      !mapLoaded ||
      hasAutoCenteredRef.current ||
      allNAPs.length === 0 ||
      selectedNAPId ||
      (focusedLocation && !hasLocationFocusedRef.current)
    ) {
      return;
    }

    const map = mapRef.current;
    hasAutoCenteredRef.current = true;

    if (allNAPs.length === 1) {
      const onlyNAP = allNAPs[0];
      if (!onlyNAP) return;
      map.flyTo({ center: [onlyNAP.lng, onlyNAP.lat], zoom: 14, duration: 900 });
      return;
    }

    const bounds = new maptilersdk.LngLatBounds();
    allNAPs.forEach((nap) => bounds.extend([nap.lng, nap.lat]));

    map.fitBounds(bounds, {
      padding: { top: 80, right: 80, bottom: 80, left: 80 },
      duration: 900,
      maxZoom: 13,
    });
  }, [allNAPs, mapLoaded, selectedNAPId, focusedLocation]);

  // Toggle heatmap layer
  useEffect(() => {
    if (!mapRef.current || !mapLoaded) return;
    const map = mapRef.current;

    if (showHeatmap) {
      // Add heatmap layer using non-clustered source if not exists
      if (!map.getLayer("naps-heatmap")) {
        map.addLayer(
          {
            id: "naps-heatmap",
            type: "heatmap",
            source: "naps-heatmap-source",
            maxzoom: 15,
            paint: {
              "heatmap-weight": 1,
              "heatmap-intensity": ["interpolate", ["linear"], ["zoom"], 0, 1, 15, 3],
              "heatmap-color": [
                "interpolate",
                ["linear"],
                ["heatmap-density"],
                0,
                "rgba(33,102,172,0)",
                0.2,
                "rgb(103,169,207)",
                0.4,
                "rgb(209,229,240)",
                0.6,
                "rgb(253,219,199)",
                0.8,
                "rgb(239,138,98)",
                1,
                "rgb(178,24,43)",
              ],
              "heatmap-radius": ["interpolate", ["linear"], ["zoom"], 0, 2, 15, 20],
              "heatmap-opacity": 0.8,
            },
          },
          "naps-clusters",
        );
      }
      // Hide marker layers
      map.setLayoutProperty("naps-clusters", "visibility", "none");
      map.setLayoutProperty("naps-cluster-count", "visibility", "none");
      map.setLayoutProperty("naps-circles", "visibility", "none");
    } else {
      // Remove heatmap layer if exists
      if (map.getLayer("naps-heatmap")) {
        map.removeLayer("naps-heatmap");
      }
      // Show marker layers
      if (map.getLayer("naps-clusters")) {
        map.setLayoutProperty("naps-clusters", "visibility", "visible");
      }
      if (map.getLayer("naps-cluster-count")) {
        map.setLayoutProperty("naps-cluster-count", "visibility", "visible");
      }
      if (map.getLayer("naps-circles")) {
        map.setLayoutProperty("naps-circles", "visibility", "visible");
      }
    }
  }, [showHeatmap, mapLoaded]);

  // Toggle provider network lines
  useEffect(() => {
    if (!mapRef.current || !mapLoaded) return;
    const map = mapRef.current;

    if (showProviderNetwork) {
      // Add source if not exists (with empty initial data)
      if (!map.getSource("provider-network-source")) {
        map.addSource("provider-network-source", {
          type: "geojson",
          data: { type: "FeatureCollection", features: [] },
        });
      }

      // Add layer if not exists
      if (!map.getLayer("provider-network-lines")) {
        map.addLayer(
          {
            id: "provider-network-lines",
            type: "line",
            source: "provider-network-source",
            paint: { "line-color": ["get", "color"], "line-width": 1.5, "line-opacity": 0.5 },
          },
          "naps-clusters",
        ); // Add below clusters
      }

      // Check if we have cached network lines for the same NAPs
      const napIds = filteredNAPs
        .map((n) => n.id)
        .sort()
        .join(",");
      const cached = networkLinesCache.current;

      if (cached && cached.napIds === napIds) {
        // Use cached network lines - no need to recalculate
        if (mapRef.current?.getSource("provider-network-source")) {
          (mapRef.current.getSource("provider-network-source") as maptilersdk.GeoJSONSource).setData(cached.geojson);
          toast.success("Network lines restored from cache", {
            id: NETWORK_TOAST_ID,
            description: `${cached.geojson.features.length} lines`,
            duration: 2000,
          });
        }
      } else if (workerRef.current && filteredNAPs.length > 0 && !pendingNetworkRequestRef.current) {
        // Send to worker for network computation (non-blocking)
        pendingNetworkRequestRef.current = true;

        // Convert NAPs to worker format
        const napPoints = filteredNAPs.map((nap) => ({
          id: nap.id,
          lat: nap.lat,
          lng: nap.lng,
          isp: nap.ISP || UNKNOWN_PROVIDER,
          ispColor: nap.ISPColor || "#888888",
        }));

        // Post message to worker
        // Using mesh approach - each point connects to 3 nearest neighbors within maxDistance
        const message: MapVizWorkerMessage = {
          type: "GENERATE_NETWORK_LINES",
          naps: napPoints,
          maxDistance: 0.15, // ~15km max connection distance
        };
        workerRef.current.postMessage(message);
      }
    } else {
      // Cancel any pending network request when toggling off
      if (pendingNetworkRequestRef.current && workerRef.current) {
        workerRef.current.postMessage({ type: "CANCEL" } as MapVizWorkerMessage);
      }
      // Remove layer if exists
      if (map.getLayer("provider-network-lines")) {
        map.removeLayer("provider-network-lines");
      }
      toast.dismiss(NETWORK_TOAST_ID);
    }
  }, [showProviderNetwork, mapLoaded, filteredNAPs]);

  // Toggle point view (unclustered small dots)
  useEffect(() => {
    if (!mapRef.current || !mapLoaded) return;
    const map = mapRef.current;

    if (showPointView) {
      // Add point view layer if not exists
      if (!map.getLayer("naps-point-view")) {
        map.addLayer({
          id: "naps-point-view",
          type: "circle",
          source: "naps-heatmap-source", // Use non-clustered source
          paint: getPointViewCirclePaint(),
        });
      }
      // Hide cluster layers
      if (map.getLayer("naps-clusters")) {
        map.setLayoutProperty("naps-clusters", "visibility", "none");
      }
      if (map.getLayer("naps-cluster-count")) {
        map.setLayoutProperty("naps-cluster-count", "visibility", "none");
      }
      if (map.getLayer("naps-circles")) {
        map.setLayoutProperty("naps-circles", "visibility", "none");
      }
    } else {
      // Remove point view layer if exists
      if (map.getLayer("naps-point-view")) {
        map.removeLayer("naps-point-view");
      }
      // Show cluster layers (unless heatmap is active)
      if (!showHeatmap) {
        if (map.getLayer("naps-clusters")) {
          map.setLayoutProperty("naps-clusters", "visibility", "visible");
        }
        if (map.getLayer("naps-cluster-count")) {
          map.setLayoutProperty("naps-cluster-count", "visibility", "visible");
        }
        if (map.getLayer("naps-circles")) {
          map.setLayoutProperty("naps-circles", "visibility", "visible");
        }
      }
    }
  }, [showPointView, showHeatmap, mapLoaded]);

  // Update cluster colors dynamically based on dominant provider
  // Note: This is a best-effort approach due to MapLibre limitations
  // Individual cluster feature properties cannot be updated after render
  // The actual coloring uses a gradient fallback based on point_count
  useEffect(() => {
    if (!mapRef.current || !mapLoaded) return;
    // Future enhancement: Could implement custom cluster rendering
    // using HTML markers for full color control per cluster
  }, [mapLoaded]);

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

  const handleDownloadPNG = useCallback(() => {
    const m = mapRef.current;
    if (!m) return;
    m.once("idle", () => { downloadMapScreenshot(m.getCanvas(), "naps"); });
    m.triggerRepaint();
  }, []);

  const handleDownloadPDF = useCallback(() => {
    const m = mapRef.current;
    if (!m) return;
    m.once("idle", () => { exportMapScreenshotAsPDF(m.getCanvas(), "naps"); });
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

  const handleChangeStyle = useCallback(
    (styleId: string) => {
      const style = MAP_STYLES.find((s) => s.id === styleId);
      if (style && mapRef.current) {
        const map = mapRef.current;
        map.setStyle(style.style);
        setCurrentMapStyle(styleId);

        // Reload layers after style change.
        // 'styledata' fires once the style JSON is parsed and all existing
        // sources/layers have been cleared — the correct moment to re-add them.
        map.once("styledata", () => {
          // Re-add NAPs clustered source if it doesn't exist
          if (!map.getSource("naps")) {
            map.addSource("naps", {
              type: "geojson",
              data: napsGeoJSON,
              cluster: true,
              clusterMaxZoom: CLUSTER_MAX_ZOOM,
              clusterRadius: CLUSTER_RADIUS,
            });
          }

          // Re-add NAPs non-clustered source for heatmap if it doesn't exist
          if (!map.getSource("naps-heatmap-source")) {
            map.addSource("naps-heatmap-source", { type: "geojson", data: napsGeoJSON, cluster: false });
          }

          // Re-add layers
          if (!map.getLayer("naps-clusters")) {
            map.addLayer({
              id: "naps-clusters",
              type: "circle",
              source: "naps",
              filter: ["has", "point_count"],
              paint: getClusterLayerPaint(),
            });
          }

          if (!map.getLayer("naps-cluster-count")) {
            map.addLayer({
              id: "naps-cluster-count",
              type: "symbol",
              source: "naps",
              filter: ["has", "point_count"],
              layout: { "text-field": "{point_count_abbreviated}", "text-font": ["Open Sans Bold"], "text-size": 12 },
              paint: { "text-color": "#ffffff" },
            });
          }

          if (!map.getLayer("naps-circles")) {
            map.addLayer({
              id: "naps-circles",
              type: "circle",
              source: "naps",
              filter: ["!has", "point_count"],
              paint: getNAPCircleLayerPaint(),
            });
          }

          // Add heatmap layer if enabled (using non-clustered source)
          if (showHeatmap && !map.getLayer("naps-heatmap")) {
            map.addLayer(
              {
                id: "naps-heatmap",
                type: "heatmap",
                source: "naps-heatmap-source",
                maxzoom: 15,
                paint: {
                  "heatmap-weight": 1,
                  "heatmap-intensity": ["interpolate", ["linear"], ["zoom"], 0, 1, 15, 3],
                  "heatmap-color": [
                    "interpolate",
                    ["linear"],
                    ["heatmap-density"],
                    0,
                    "rgba(33,102,172,0)",
                    0.2,
                    "rgb(103,169,207)",
                    0.4,
                    "rgb(209,229,240)",
                    0.6,
                    "rgb(253,219,199)",
                    0.8,
                    "rgb(239,138,98)",
                    1,
                    "rgb(178,24,43)",
                  ],
                  "heatmap-radius": ["interpolate", ["linear"], ["zoom"], 0, 2, 15, 20],
                  "heatmap-opacity": 0.8,
                },
              },
              "naps-clusters",
            );
          }

          // Re-add provider network lines if enabled
          if (showProviderNetwork) {
            // Add source with cached or empty data
            if (!map.getSource("provider-network-source")) {
              const cachedData = networkLinesCache.current?.geojson || {
                type: "FeatureCollection" as const,
                features: [],
              };
              map.addSource("provider-network-source", { type: "geojson", data: cachedData });
            }

            // Add layer
            if (!map.getLayer("provider-network-lines")) {
              map.addLayer(
                {
                  id: "provider-network-lines",
                  type: "line",
                  source: "provider-network-source",
                  paint: { "line-color": ["get", "color"], "line-width": 1.5, "line-opacity": 0.5 },
                },
                "naps-clusters",
              );
            }
          }

          // Re-add point view if enabled
          if (showPointView && !map.getLayer("naps-point-view")) {
            map.addLayer({
              id: "naps-point-view",
              type: "circle",
              source: "naps-heatmap-source",
              paint: getPointViewCirclePaint(),
            });
            // Hide cluster layers when point view is active
            if (map.getLayer("naps-clusters")) {
              map.setLayoutProperty("naps-clusters", "visibility", "none");
            }
            if (map.getLayer("naps-cluster-count")) {
              map.setLayoutProperty("naps-cluster-count", "visibility", "none");
            }
            if (map.getLayer("naps-circles")) {
              map.setLayoutProperty("naps-circles", "visibility", "none");
            }
          }

          // Re-attach layer click/cursor handlers (cleared when style was replaced).
          const h = layerHandlersRef.current;
          if (h.clusterClick) {
            map.off("click", "naps-clusters", h.clusterClick);
            map.on("click", "naps-clusters", h.clusterClick);
          }
          if (h.napClick) {
            map.off("click", "naps-circles", h.napClick);
            map.on("click", "naps-circles", h.napClick);
          }
          if (h.setCursorPointer && h.resetCursor) {
            map.off("mouseenter", "naps-circles", h.setCursorPointer);
            map.off("mouseleave", "naps-circles", h.resetCursor);
            map.off("mouseenter", "naps-clusters", h.setCursorPointer);
            map.off("mouseleave", "naps-clusters", h.resetCursor);
            map.on("mouseenter", "naps-circles", h.setCursorPointer);
            map.on("mouseleave", "naps-circles", h.resetCursor);
            map.on("mouseenter", "naps-clusters", h.setCursorPointer);
            map.on("mouseleave", "naps-clusters", h.resetCursor);
          }
        });
      }
    },
    [napsGeoJSON, showHeatmap, showProviderNetwork, showPointView],
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

  // Calculate dominant colors for clusters
  const handleCalculateColors = useCallback(() => {
    if (!mapRef.current || !workerRef.current || isCalculatingColors) return;

    setIsCalculatingColors(true);

    // Get visible clusters from the map
    // For now, we'll use the filtered NAPs grouped by approximate regions
    // This is a placeholder - in production, you'd query actual cluster data
    const clusters = filteredNAPs.reduce(
      (acc, nap, index) => {
        const clusterId = Math.floor(index / 50); // Group every 50 NAPs
        if (!acc[clusterId]) {
          acc[clusterId] = { clusterId, points: [] };
        }
        acc[clusterId].points.push({ ispColor: nap.ISPColor || "#888888", isp: nap.ISP || UNKNOWN_PROVIDER });
        return acc;
      },
      {} as Record<number, { clusterId: number; points: { ispColor: string; isp: string }[] }>,
    );

    const clusterArray = Object.values(clusters);

    if (clusterArray.length === 0) {
      toast.info("No clusters to calculate");
      setIsCalculatingColors(false);
      return;
    }

    const message: MapVizWorkerMessage = { type: "CALCULATE_CLUSTER_COLORS", clusters: clusterArray };
    workerRef.current.postMessage(message);
  }, [filteredNAPs, isCalculatingColors]);

  return (
    <Card className={cn("overflow-hidden flex flex-col h-full", className)}>
      {/* Header */}
      <MapHeader
        filteredCount={filteredNAPs.length}
        totalCount={allNAPs.length}
        showSearch={showSearch}
        onToggleSearch={() => setShowSearch(!showSearch)}
        currentMapStyle={currentMapStyle}
        onChangeStyle={handleChangeStyle}
        isExpanded={isExpanded}
        onToggleExpand={onToggleExpand}
        showHeatmap={showHeatmap}
        onToggleHeatmap={() => setShowHeatmap(!showHeatmap)}
        showProviderNetwork={showProviderNetwork}
        onToggleProviderNetwork={() => setShowProviderNetwork(!showProviderNetwork)}
        showPointView={showPointView}
        onTogglePointView={() => setShowPointView(!showPointView)}
        isCalculatingColors={isCalculatingColors}
        onCalculateColors={handleCalculateColors}
      />

      {/* Search bar */}
      <SearchBar show={showSearch} value={searchQuery} onChange={setSearchQuery} onSearch={handleCoordinateSearch} />

      {/* Mode Toolbar */}
      <ModeToolbar
        currentMode={interactionMode}
        onModeChange={setInteractionMode}
        hasAddHandler={!!onAddNAP}
        hasEditHandler={!!onEditNAP}
        hasDeleteHandler={!!onDeleteNAP}
      />

      {/* Map Container */}
      <CardContent className="p-0 flex-1 flex flex-col min-h-0 relative">
        <div ref={mapContainerRef} className="flex-1 relative" style={{ minHeight: isExpanded ? "500px" : "350px" }}>
          <LoadingOverlay show={!mapLoaded && !mapError} />
          <ErrorOverlay error={mapError} />

          {mapLoaded && (
            <>
              <MapControls
                onZoomIn={handleZoomIn}
                onZoomOut={handleZoomOut}
                onResetView={handleResetView}
                onLocateMe={handleLocateMe}
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
      </CardContent>

      {/* Custom popup styles */}
      <style jsx global>{`
        .nap-popup .maplibregl-popup-content {
          padding: 0;
          border-radius: 8px;
          box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
        }
        .nap-popup .maplibregl-popup-close-button {
          padding: 4px 8px;
          font-size: 16px;
          color: #6b7280;
        }
        .nap-popup .maplibregl-popup-close-button:hover {
          color: #1f2937;
          background: transparent;
        }
      `}</style>
    </Card>
  );
}
