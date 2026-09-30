"use client";

import React, { useCallback, useEffect, useMemo, useRef, useState } from "react";
import * as maptilersdk from "@maptiler/sdk";
import type { GeoJSONSource, Map, MapMouseEvent } from "@maptiler/sdk";
import { toast } from "sonner";

import "@maptiler/sdk/dist/maptiler-sdk.css";
import "@/styles/maptiler.css";

import { Card, CardContent } from "@/components/ui/card";
import { cn } from "@/lib/utils";
import { copyMapScreenshotToClipboard, downloadMapScreenshot, exportMapScreenshotAsPDF } from "@/utils/map-screenshot";
import { LoadingOverlay, MapControls, MapHeader, ProviderLegend, SearchBar, SignalQualityLegend } from "./components";
import { MAP_STYLES, PH_BOUNDS, PH_CENTER, PH_ZOOM, SIGNAL_QUALITY_ORDER, VISUALIZATION_CONFIG } from "./constants";
import {
  useFilteredSpeedtests,
  useProviderCounts,
  useSignalQualityCounts,
  useSpeedtestsGeoJSON,
  useSpeedtestStats,
} from "./hooks";
import { type SpeedtestMapViewProps, type SpeedtestWithMeta, type VisualizationMode } from "./types";
import {
  createSpeedtestPopupHTML,
  getCoverageCirclePaint,
  getHeatmapLayerPaint,
  getPointsPaint,
  getSignalQualityCirclePaint,
  getSpeedCirclesPaint,
} from "./utils";

// MapTiler API Key Configuration
const MAPTILER_API_KEY = process.env.NEXT_PUBLIC_MAPTILER_API_KEY || maptilersdk.config.apiKey;

// Source and Layer IDs
const SOURCE_ID = "speedtests";
const HEATMAP_LAYER_ID = "speedtests-heatmap";
const SPEED_CIRCLES_LAYER_ID = "speedtests-speed-circles";
const POINTS_LAYER_ID = "speedtests-points";
const SIGNAL_QUALITY_LAYER_ID = "speedtests-signal-quality";
const COVERAGE_LAYER_ID = "speedtests-coverage";

// All interactive layer IDs (excluding heatmap)
const INTERACTIVE_LAYERS = [
  SPEED_CIRCLES_LAYER_ID,
  POINTS_LAYER_ID,
  SIGNAL_QUALITY_LAYER_ID,
  COVERAGE_LAYER_ID,
] as const;

// Layer configuration by visualization mode
const LAYER_BY_MODE: Record<VisualizationMode, string> = {
  heatmap: HEATMAP_LAYER_ID,
  "speed-circles": SPEED_CIRCLES_LAYER_ID,
  points: POINTS_LAYER_ID,
  "signal-quality": SIGNAL_QUALITY_LAYER_ID,
  coverage: COVERAGE_LAYER_ID,
};

export function SpeedtestMapViewAdvanced({
  speedtests = [],
  regionId,
  focusedLocation,
  className,
  isExpanded = false,
  onToggleExpand,
  onAddSpeedtest,
  onEditSpeedtest,
  onDeleteSpeedtest,
  onSelectSpeedtest,
  selectedSpeedtestId,
  isLoading = false,
  loadingProgress,
  onBackToList,
  regionName,
}: SpeedtestMapViewProps) {
  const mapContainerRef = useRef<HTMLDivElement>(null);
  const mapRef = useRef<Map | null>(null);
  const popupRef = useRef<maptilersdk.Popup | null>(null);
  const hasAutoCenteredRef = useRef(false);
  const hasLocationFocusedRef = useRef(false);

  // State
  const [isMapLoaded, setIsMapLoaded] = useState(false);
  const [isMapIdle, setIsMapIdle] = useState(false);
  const [currentMapStyle, setCurrentMapStyle] = useState("topo");
  const [showSearch, setShowSearch] = useState(false);
  const [searchQuery, setSearchQuery] = useState("");
  const [zoomLevel, setZoomLevel] = useState(PH_ZOOM);
  const [visualizationMode, setVisualizationMode] = useState<VisualizationMode>("heatmap");
  const [showProviderLegend, setShowProviderLegend] = useState(true);
  const [showSignalLegend, setShowSignalLegend] = useState(true);

  // Derive all available providers from speedtests
  const allProviders = useMemo(() => {
    const providers = new Set<string>();
    speedtests.forEach((st) => {
      const provider = st.serviceProvider?.provider?.name || "Unknown";
      providers.add(provider);
    });
    return providers;
  }, [speedtests]);

  // Provider and signal quality visibility state
  // Initialize with all providers visible
  const [visibleProviders, setVisibleProviders] = useState<Set<string>>(() => new Set<string>());
  const [visibleSignalQualities, setVisibleSignalQualities] = useState<Set<string>>(new Set(SIGNAL_QUALITY_ORDER));

  // Keep visible providers in sync with available providers
  // When new providers appear, add them to visible. When providers are removed, remove them.
  const effectiveVisibleProviders = useMemo(() => {
    // If no providers are visible yet, show all
    if (visibleProviders.size === 0 && allProviders.size > 0) {
      return allProviders;
    }
    // Filter to only providers that exist in the data
    const filtered = new Set<string>();
    visibleProviders.forEach((p) => {
      if (allProviders.has(p)) {
        filtered.add(p);
      }
    });
    // If filtering removed all providers, show all
    if (filtered.size === 0 && allProviders.size > 0) {
      return allProviders;
    }
    return filtered;
  }, [visibleProviders, allProviders]);

  // Convert speedtests to SpeedtestWithMeta
  const speedtestsWithMeta: SpeedtestWithMeta[] = useMemo(() => {
    return speedtests.map((st) => ({ ...st, isPending: false, hasError: false }));
  }, [speedtests]);

  // Use hooks
  const filteredSpeedtests = useFilteredSpeedtests(
    speedtestsWithMeta,
    effectiveVisibleProviders,
    visibleSignalQualities,
    searchQuery,
  );
  const geojsonData = useSpeedtestsGeoJSON(filteredSpeedtests, selectedSpeedtestId);
  const providerCounts = useProviderCounts(speedtestsWithMeta, effectiveVisibleProviders);
  const signalQualityCounts = useSignalQualityCounts(speedtestsWithMeta, visibleSignalQualities);
  const stats = useSpeedtestStats(filteredSpeedtests);

  useEffect(() => {
    hasAutoCenteredRef.current = false;
    hasLocationFocusedRef.current = false;
  }, [regionId]);

  useEffect(() => {
    hasLocationFocusedRef.current = false;
  }, [focusedLocation?.lat, focusedLocation?.lng]);

  // Update layer visibility based on visualization mode
  const updateLayerVisibility = useCallback((map: Map, mode: VisualizationMode) => {
    Object.entries(LAYER_BY_MODE).forEach(([vizMode, layerId]) => {
      if (map.getLayer(layerId)) {
        map.setLayoutProperty(layerId, "visibility", vizMode === mode ? "visible" : "none");
      }
    });
  }, []);

  // Add visualization layers
  const addVisualizationLayers = useCallback((map: Map) => {
    // Heatmap layer
    if (!map.getLayer(HEATMAP_LAYER_ID)) {
      map.addLayer({
        id: HEATMAP_LAYER_ID,
        type: "heatmap",
        source: SOURCE_ID,
        paint: getHeatmapLayerPaint() as maptilersdk.HeatmapLayerSpecification["paint"],
        layout: { visibility: "visible" },
      });
    }

    // Speed circles layer (radius based on download speed)
    if (!map.getLayer(SPEED_CIRCLES_LAYER_ID)) {
      map.addLayer({
        id: SPEED_CIRCLES_LAYER_ID,
        type: "circle",
        source: SOURCE_ID,
        paint: getSpeedCirclesPaint() as maptilersdk.CircleLayerSpecification["paint"],
        layout: { visibility: "none" },
      });
    }

    // Points layer (simple colored points)
    if (!map.getLayer(POINTS_LAYER_ID)) {
      map.addLayer({
        id: POINTS_LAYER_ID,
        type: "circle",
        source: SOURCE_ID,
        paint: getPointsPaint() as maptilersdk.CircleLayerSpecification["paint"],
        layout: { visibility: "none" },
      });
    }

    // Signal quality layer
    if (!map.getLayer(SIGNAL_QUALITY_LAYER_ID)) {
      map.addLayer({
        id: SIGNAL_QUALITY_LAYER_ID,
        type: "circle",
        source: SOURCE_ID,
        paint: getSignalQualityCirclePaint() as maptilersdk.CircleLayerSpecification["paint"],
        layout: { visibility: "none" },
      });
    }

    // Coverage layer
    if (!map.getLayer(COVERAGE_LAYER_ID)) {
      map.addLayer({
        id: COVERAGE_LAYER_ID,
        type: "circle",
        source: SOURCE_ID,
        paint: getCoverageCirclePaint() as maptilersdk.CircleLayerSpecification["paint"],
        layout: { visibility: "none" },
      });
    }
  }, []);

  // Handle point click
  const handlePointClick = useCallback(
    (e: MapMouseEvent) => {
      const map = mapRef.current;
      if (!map || visualizationMode === "heatmap") return;

      const layerId = LAYER_BY_MODE[visualizationMode];
      if (!map.getLayer(layerId)) return;

      const features = map.queryRenderedFeatures(e.point, { layers: [layerId] });
      if (features.length === 0) {
        if (popupRef.current) {
          popupRef.current.remove();
        }
        if (onSelectSpeedtest) {
          onSelectSpeedtest(null);
        }
        return;
      }

      const feature = features[0];
      if (!feature) return;

      const coords = (feature.geometry as GeoJSON.Point).coordinates.slice() as [number, number];
      const props = feature.properties;

      // Find the full speedtest object
      const speedtest = speedtests.find((st) => st.id === props?.id);

      // Create popup
      if (popupRef.current) {
        popupRef.current.remove();
      }

      if (speedtest) {
        popupRef.current = new maptilersdk.Popup({
          closeButton: true,
          closeOnClick: true,
          maxWidth: "300px",
          className: "speedtest-popup",
        })
          .setLngLat(coords)
          .setHTML(createSpeedtestPopupHTML(speedtest))
          .addTo(map);

        if (onSelectSpeedtest) {
          onSelectSpeedtest(speedtest);
        }
      }
    },
    [visualizationMode, speedtests, onSelectSpeedtest],
  );

  // Cursor handlers
  const handleMouseEnter = useCallback(() => {
    const map = mapRef.current;
    if (map && visualizationMode !== "heatmap") {
      map.getCanvas().style.cursor = "pointer";
    }
  }, [visualizationMode]);

  const handleMouseLeave = useCallback(() => {
    const map = mapRef.current;
    if (map) {
      map.getCanvas().style.cursor = "";
    }
  }, []);

  // Initialize map
  useEffect(() => {
    if (!mapContainerRef.current || !MAPTILER_API_KEY) {
      console.error("Map container or API key not available");
      return;
    }

    maptilersdk.config.apiKey = MAPTILER_API_KEY;

    const styleConfig = MAP_STYLES.find((s) => s.id === currentMapStyle);
    const map = new maptilersdk.Map({
      container: mapContainerRef.current,
      style: styleConfig?.style || maptilersdk.MapStyle.TOPO,
      center: PH_CENTER,
      zoom: PH_ZOOM,
      maxBounds: PH_BOUNDS,
      navigationControl: false,
      geolocateControl: false,
      scaleControl: false,
      terrainControl: false,
      fullscreenControl: false,
      // Required for canvas.toDataURL() to return non-blank screenshots
      canvasContextAttributes: { preserveDrawingBuffer: true },
    });

    mapRef.current = map;

    map.on("load", () => {
      setIsMapLoaded(true);

      // Add source
      if (!map.getSource(SOURCE_ID)) {
        map.addSource(SOURCE_ID, { type: "geojson", data: { type: "FeatureCollection", features: [] } });
      }

      // Add visualization layers
      addVisualizationLayers(map);
    });

    // Re-add layers when style changes (style change removes all layers)
    map.on("style.load", () => {
      // Re-add source and layers after style change
      // The data will be updated by the separate data useEffect
      if (!map.getSource(SOURCE_ID)) {
        map.addSource(SOURCE_ID, { type: "geojson", data: { type: "FeatureCollection", features: [] } });
      }
      // Re-add layers
      addVisualizationLayers(map);
      // Re-apply data to the source so the layers render immediately after style change
      const src = map.getSource(SOURCE_ID) as GeoJSONSource | undefined;
      if (src && (geojsonData as any)?.features) {
        try {
          // setData may not exist on some source typings; cast to any
          (src as any).setData(geojsonData);
        } catch (e) {
          console.warn("Failed to set source data after style change:", e);
        }
      }

      // Re-apply visibility for the current visualization mode
      try {
        updateLayerVisibility(map, visualizationMode);
      } catch (e) {
        // ignore
      }

      // Reattach interactive handlers for clickable layers
      try {
        INTERACTIVE_LAYERS.forEach((layerId) => {
          if (map.getLayer(layerId)) {
            map.off("click", layerId, handlePointClick);
            map.off("mouseenter", layerId, handleMouseEnter);
            map.off("mouseleave", layerId, handleMouseLeave);
          }
        });

        const currentLayerId = LAYER_BY_MODE[visualizationMode];
        if (visualizationMode !== "heatmap" && map.getLayer(currentLayerId)) {
          map.on("click", currentLayerId, handlePointClick);
          map.on("mouseenter", currentLayerId, handleMouseEnter);
          map.on("mouseleave", currentLayerId, handleMouseLeave);
        }
      } catch (e) {
        // ignore
      }
      // Mark as loaded so the data effect can update
      setIsMapLoaded(true);
    });

    map.on("zoom", () => {
      setZoomLevel(Math.round(map.getZoom() * 10) / 10);
    });

    return () => {
      if (popupRef.current) {
        popupRef.current.remove();
      }
      map.remove();
      mapRef.current = null;
      // Reset state so the new map instance starts fresh: data effects re-run
      // and the auto-zoom triggers again once the new map's data is ready.
      setIsMapLoaded(false);
      hasAutoCenteredRef.current = false;
    };
  }, [currentMapStyle, addVisualizationLayers]); // Re-initialize when style changes

  // Update data
  useEffect(() => {
    const map = mapRef.current;
    if (!map || !isMapLoaded) return;

    const source = map.getSource(SOURCE_ID) as GeoJSONSource;
    if (source) {
      source.setData(geojsonData);
    }
  }, [geojsonData, isMapLoaded]);

  // Focus map to an explicit location passed from query params (e.g., "View on Map" action).
  useEffect(() => {
    const map = mapRef.current;
    if (!map || !isMapLoaded || !focusedLocation || hasLocationFocusedRef.current) return;

    hasLocationFocusedRef.current = true;
    map.flyTo({ center: [focusedLocation.lng, focusedLocation.lat], zoom: 16, duration: 900 });
  }, [focusedLocation, isMapLoaded]);

  // Auto-center map to loaded speedtests for the active region/location once data is available.
  useEffect(() => {
    const map = mapRef.current;
    if (
      !map ||
      !isMapLoaded ||
      hasAutoCenteredRef.current ||
      filteredSpeedtests.length === 0 ||
      selectedSpeedtestId ||
      (focusedLocation && !hasLocationFocusedRef.current)
    ) {
      return;
    }

    hasAutoCenteredRef.current = true;

    if (filteredSpeedtests.length === 1) {
      const onlySpeedtest = filteredSpeedtests[0];
      if (!onlySpeedtest) return;
      map.flyTo({ center: [onlySpeedtest.longitude, onlySpeedtest.latitude], zoom: 14, duration: 900 });
      return;
    }

    const bounds = new maptilersdk.LngLatBounds();
    filteredSpeedtests.forEach((speedtest) => {
      bounds.extend([speedtest.longitude, speedtest.latitude]);
    });

    map.fitBounds(bounds, {
      padding: { top: 80, right: 80, bottom: 80, left: 80 },
      duration: 900,
      maxZoom: 13,
    });
  }, [filteredSpeedtests, isMapLoaded, selectedSpeedtestId, focusedLocation]);

  // Zoom to selected speedtest location when a specific point is selected.
  useEffect(() => {
    const map = mapRef.current;
    if (!map || !isMapLoaded || !selectedSpeedtestId) return;

    const selected = speedtests.find((st) => st.id === selectedSpeedtestId);
    if (!selected) return;

    map.flyTo({ center: [selected.longitude, selected.latitude], zoom: 16, duration: 900 });
  }, [selectedSpeedtestId, speedtests, isMapLoaded]);

  // Update visibility when mode changes
  useEffect(() => {
    const map = mapRef.current;
    if (!map || !isMapLoaded) return;

    updateLayerVisibility(map, visualizationMode);

    // Update click handlers
    INTERACTIVE_LAYERS.forEach((layerId) => {
      if (map.getLayer(layerId)) {
        map.off("click", layerId, handlePointClick);
        map.off("mouseenter", layerId, handleMouseEnter);
        map.off("mouseleave", layerId, handleMouseLeave);
      }
    });

    const currentLayerId = LAYER_BY_MODE[visualizationMode];
    if (visualizationMode !== "heatmap" && map.getLayer(currentLayerId)) {
      map.on("click", currentLayerId, handlePointClick);
      map.on("mouseenter", currentLayerId, handleMouseEnter);
      map.on("mouseleave", currentLayerId, handleMouseLeave);
    }
  }, [visualizationMode, isMapLoaded, updateLayerVisibility, handlePointClick, handleMouseEnter, handleMouseLeave]);

  // Map controls
  const handleZoomIn = useCallback(() => {
    mapRef.current?.zoomIn();
  }, []);

  const handleZoomOut = useCallback(() => {
    mapRef.current?.zoomOut();
  }, []);

  const handleResetView = useCallback(() => {
    mapRef.current?.flyTo({ center: PH_CENTER, zoom: PH_ZOOM });
  }, []);

  const handleLocateUser = useCallback(() => {
    if (navigator.geolocation) {
      navigator.geolocation.getCurrentPosition(
        (position) => {
          mapRef.current?.flyTo({ center: [position.coords.longitude, position.coords.latitude], zoom: 12 });
        },
        (error) => console.error("Geolocation error:", error),
      );
    }
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

  // Provider visibility toggle
  const handleToggleProvider = useCallback((provider: string) => {
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

  const handleShowAllProviders = useCallback(() => {
    const allProviders = new Set<string>();
    speedtests.forEach((st) => {
      allProviders.add(st.serviceProvider?.provider?.name || "Unknown");
    });
    setVisibleProviders(allProviders);
  }, [speedtests]);

  const handleHideAllProviders = useCallback(() => {
    setVisibleProviders(new Set());
  }, []);

  // Signal quality visibility toggle
  const handleToggleSignalQuality = useCallback((quality: string) => {
    setVisibleSignalQualities((prev) => {
      const next = new Set(prev);
      if (next.has(quality)) {
        next.delete(quality);
      } else {
        next.add(quality);
      }
      return next;
    });
  }, []);

  // Style change
  const handleChangeStyle = useCallback((styleId: string) => {
    const map = mapRef.current;
    if (!map) return;

    const styleConfig = MAP_STYLES.find((s) => s.id === styleId);
    if (styleConfig) {
      setCurrentMapStyle(styleId);
      map.setStyle(styleConfig.style);
    }
  }, []);

  const handleDownloadPNG = useCallback(() => {
    const m = mapRef.current;
    if (!m) return;
    m.once("idle", () => { downloadMapScreenshot(m.getCanvas(), "speedtest"); });
    m.triggerRepaint();
  }, []);

  const handleDownloadPDF = useCallback(() => {
    const m = mapRef.current;
    if (!m) return;
    m.once("idle", () => { exportMapScreenshotAsPDF(m.getCanvas(), "speedtest"); });
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
    if (!isMapLoaded || !m) return;
    const onIdle = () => setIsMapIdle(true);
    const onMoveStart = () => setIsMapIdle(false);
    m.on("idle", onIdle);
    m.on("movestart", onMoveStart);
    return () => {
      m.off("idle", onIdle);
      m.off("movestart", onMoveStart);
    };
  }, [isMapLoaded]);

  return (
    <Card className={cn("flex h-full w-full flex-col overflow-hidden border-0 rounded-none", className)}>
      <MapHeader
        filteredCount={filteredSpeedtests.length}
        totalCount={speedtests.length}
        showSearch={showSearch}
        onToggleSearch={() => setShowSearch((prev) => !prev)}
        currentMapStyle={currentMapStyle}
        onChangeStyle={handleChangeStyle}
        isExpanded={isExpanded}
        onToggleExpand={onToggleExpand}
        visualizationMode={visualizationMode}
        onChangeVisualizationMode={setVisualizationMode}
        avgDownload={stats.avgDownload}
        avgUpload={stats.avgUpload}
        avgPing={stats.avgPing}
        isLoading={isLoading}
        loadingProgress={loadingProgress}
        onBackToList={onBackToList}
        regionName={regionName}
      />

      <SearchBar show={showSearch} value={searchQuery} onChange={setSearchQuery} onSearch={handleCoordinateSearch} />

      <CardContent className="relative flex-1 p-0" style={{ minHeight: 0 }}>
        {/* Map Container */}
        <div ref={mapContainerRef} className="absolute inset-0 h-full w-full" />

        {/* Map Controls */}
        <MapControls
          onZoomIn={handleZoomIn}
          onZoomOut={handleZoomOut}
          onResetView={handleResetView}
          onLocateMe={handleLocateUser}
          onDownloadPNG={handleDownloadPNG}
          onDownloadPDF={handleDownloadPDF}
          onCopyClipboard={handleCopyClipboard}
          isScreenshotReady={isMapIdle}
        />

        {/* Provider Legend (for points mode) */}
        {visualizationMode === "points" && (
          <div className="absolute bottom-4 left-4 z-10 max-h-[40%] w-56">
            <ProviderLegend
              show={showProviderLegend}
              providerCounts={providerCounts}
              onToggle={() => setShowProviderLegend((prev) => !prev)}
              onToggleProvider={handleToggleProvider}
              onToggleAll={(show) => (show ? handleShowAllProviders() : handleHideAllProviders())}
            />
          </div>
        )}

        {/* Signal Quality Legend (for signal-quality mode) */}
        {visualizationMode === "signal-quality" && (
          <div className="absolute bottom-4 left-4 z-10 max-h-[40%] w-56">
            <SignalQualityLegend
              show={showSignalLegend}
              signalQualityCounts={signalQualityCounts}
              onToggle={() => setShowSignalLegend((prev) => !prev)}
              onToggleQuality={handleToggleSignalQuality}
              onToggleAll={(show) => {
                if (show) {
                  setVisibleSignalQualities(new Set(SIGNAL_QUALITY_ORDER));
                } else {
                  setVisibleSignalQualities(new Set());
                }
              }}
            />
          </div>
        )}

        {/* Speed Legend (for speed-circles, heatmap, coverage) */}
        {(visualizationMode === "speed-circles" ||
          visualizationMode === "heatmap" ||
          visualizationMode === "coverage") && (
          <div className="absolute bottom-4 left-4 z-10 rounded-lg bg-background/95 p-3 shadow-lg backdrop-blur-sm">
            <p className="mb-2 text-xs font-medium">
              {visualizationMode === "speed-circles" && "Speed (circle size)"}
              {visualizationMode === "heatmap" && "Density & Speed"}
              {visualizationMode === "coverage" && "Coverage Quality"}
            </p>
            <div className="h-2 w-40 rounded-full bg-gradient-to-r from-red-500 via-amber-500 to-emerald-500" />
            <div className="mt-1 flex justify-between text-[10px] text-muted-foreground">
              <span>0 Mbps</span>
              <span>50 Mbps</span>
              <span>100+ Mbps</span>
            </div>
          </div>
        )}

        {/* Loading Overlay */}
        <LoadingOverlay show={isLoading} />

        {/* Data count badge */}
        {filteredSpeedtests.length > 10000 && (
          <div className="absolute right-4 top-4 z-10">
            <div className="rounded-md bg-amber-100 px-2 py-1 text-xs text-amber-800 shadow-sm dark:bg-amber-900/30 dark:text-amber-400">
              {filteredSpeedtests.length.toLocaleString()} points
            </div>
          </div>
        )}

        {/* No API key warning */}
        {!MAPTILER_API_KEY && (
          <div className="absolute inset-0 z-30 flex items-center justify-center bg-background">
            <div className="text-center">
              <p className="text-lg font-medium text-destructive">MapTiler API Key Missing</p>
              <p className="text-sm text-muted-foreground">
                Please set NEXT_PUBLIC_MAPTILER_API_KEY in your environment
              </p>
            </div>
          </div>
        )}
      </CardContent>
    </Card>
  );
}

export default SpeedtestMapViewAdvanced;

// Re-export types for external use
export type { SpeedtestMapViewProps, MapInteractionMode, VisualizationMode } from "./types";
