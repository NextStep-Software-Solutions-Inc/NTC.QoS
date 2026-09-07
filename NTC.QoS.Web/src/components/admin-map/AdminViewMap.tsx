/**
 * Redesigned Admin View Map Component
 *
 * A modern, Google Maps-inspired admin map with glassmorphic UI elements,
 * refactored architecture following SOLID and DRY principles.
 *
 * Architecture Overview:
 * ---------------------
 * 1. AdminMapProvider - Centralized state management (Context pattern)
 * 2. LayerControls - Modular layer toggling with opacity (Single Responsibility)
 * 3. FilterPanel - Redesigned sidebar with functional filters (Interface Segregation)
 * 4. MapStyleSelector - Map style switching (Single Responsibility)
 * 5. NavigationControls - Zoom, compass, locate controls (Single Responsibility)
 *
 * Extending Layers:
 * ----------------
 * To add new layers, update the LAYER_CONFIGS array in LayerControls.tsx
 * and create corresponding hooks if needed. The system is Open/Closed -
 * you can add new layers without modifying existing code.
 *
 * Performance Optimizations:
 * -------------------------
 * - React.memo on all control components
 * - useMemo/useCallback for expensive operations
 * - Centralized state to prevent prop drilling
 * - URL-based visibility persistence
 * - Debounced map updates
 */

"use client";

import "@maptiler/sdk/dist/maptiler-sdk.css";
import "@/styles/maptiler.css";

import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { useParams, useSearchParams } from "next/navigation";
import { useUser } from "@clerk/nextjs";
import type { MapMouseEvent } from "@maptiler/sdk";
import { BarChart3, Camera, ClipboardCopy, FileText, Pencil } from "lucide-react";
import { toast } from "sonner";

import { getAuthUserRequest } from "@/components/features/users/handlers/getAuthUserRequest";
import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";
import { useMapFly } from "@/hooks/use-map-fly";
import { useMap } from "@/hooks/useMap";
import { useMapCells } from "@/hooks/useMapCells";
import { useMapFibers } from "@/hooks/useMapFibers";
import { useMapMove } from "@/hooks/useMapMove";

const useAuthUser = createQueryHookFromTemplate(getAuthUserRequest);
import { useMapNAPs } from "@/hooks/useMapNAPs";
import { useMapSpeedtests } from "@/hooks/useMapSpeedtests";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { copyMapScreenshotToClipboard, downloadMapScreenshot, exportMapScreenshotAsPDF } from "@/utils/map-screenshot";
import { AdminBroadbandPanel } from "./components/AdminBroadbandPanel";
import { CellSiteDetails } from "./components/CellSiteDetails";
import { AdminMapProvider, useFilterState, useLayerState } from "./context/AdminMapContext";
import { FilterPanel } from "./controls/FilterPanel";
import { LayerControls } from "./controls/LayerControls";
import { MapStyleSelector } from "./controls/MapStyleSelector";
import { NavigationControls } from "./controls/NavigationControls";
import { MAP_CONTROL_STACK_Z_INDEX, RIGHT_DRAWER_OPEN_OFFSET_PX } from "./layout-constants";
import { GlassPanel } from "./ui/GlassPanel";
import { MapControlButton } from "./ui/MapControlButton";

/**
 * Helper function to safely set paint property on a map layer
 * Catches errors when layer doesn't exist or property is invalid
 */
function safeSetPaintProperty(map: ReturnType<typeof useMap>["map"], layerId: string, property: string, value: number) {
  try {
    if (map && map.isStyleLoaded() && map.getLayer(layerId)) {
      map.setPaintProperty(layerId, property, value);
    }
  } catch (error) {
    // Ignore when style is not ready yet
  }
}

/**
 * Hook to apply opacity to map layers
 * Uses safe property setters to prevent crashes when layers aren't ready
 */
function useLayerOpacity() {
  const { map, isMapLoaded } = useMap();
  const { layerState } = useLayerState();

  useEffect(() => {
    if (!map || !isMapLoaded || !map.isStyleLoaded()) return;

    // Apply opacity to new cell layers
    const cellOpacity = (layerState.opacity.cellular ?? 100) / 100;
    safeSetPaintProperty(map, "cells-symbols", "icon-opacity", cellOpacity);
    safeSetPaintProperty(map, "cells-circles", "circle-opacity", cellOpacity);
    safeSetPaintProperty(map, "cells-clusters", "circle-opacity", cellOpacity * 0.9);

    // Apply opacity to cell heatmap (admin-only)
    const cellHeatmapOpacity = (layerState.opacity["cellular-heatmap"] ?? 70) / 100;
    safeSetPaintProperty(map, "cells-heatmap", "heatmap-opacity", cellHeatmapOpacity);

    // Apply opacity to signal coverage (admin-only)
    const signalOpacity = (layerState.opacity["cellular-signal"] ?? 80) / 100;
    safeSetPaintProperty(map, "signal-coverage-circles", "circle-opacity", signalOpacity);

    // Apply opacity to fiber layers
    const fiberOpacity = (layerState.opacity.fiber ?? 50) / 100;
    safeSetPaintProperty(map, "fiber", "fill-opacity", fiberOpacity);
    safeSetPaintProperty(map, "fiber-line", "line-opacity", fiberOpacity);

    // Apply opacity to NAP layers
    const napsOpacity = (layerState.opacity.naps ?? 100) / 100;
    safeSetPaintProperty(map, "NAPs-unclustered", "circle-opacity", napsOpacity);
    safeSetPaintProperty(map, "NAPs-cluster", "circle-opacity", napsOpacity);

    // Apply opacity to speedtest layers
    const speedtestOpacity = (layerState.opacity["speedtests-cellular"] ?? 70) / 100;
    safeSetPaintProperty(map, "speedtests-circles", "circle-opacity", speedtestOpacity);
    safeSetPaintProperty(map, "speedtests-heatmap", "heatmap-opacity", speedtestOpacity);
  }, [map, isMapLoaded, layerState.opacity]);
}

/**
 * Inner map component that uses the context
 * Separated to allow context initialization first
 */
function AdminMapContent() {
  const mapContainerRef = useRef<HTMLDivElement | null>(null);
  const searchParams = useSearchParams();
  const { setMapContainer, map, isMapLoaded } = useMap();
  const { layerState } = useLayerState();
  const { filters } = useFilterState();

  // Broadband Analytics Panel state
  const [isAnalyticsPanelOpen, setIsAnalyticsPanelOpen] = useState(false);
  const [isMapStyleOpen, setIsMapStyleOpen] = useState(false);
  const [isLayerControlsOpen, setIsLayerControlsOpen] = useState(false);
  const [isExportMenuOpen, setIsExportMenuOpen] = useState(false);
  const [selectedPoint, setSelectedPoint] = useState<{ lat: number; lng: number } | null>(null);
  const [isPointSelectionMode, setIsPointSelectionMode] = useState(false);

  // Envelope drawing state for custom area analysis
  const [isEnvelopeDrawingMode, setIsEnvelopeDrawingMode] = useState(false);
  const [customEnvelope, setCustomEnvelope] = useState<{
    minLat: number;
    maxLat: number;
    minLng: number;
    maxLng: number;
  } | null>(null);
  const [drawStartPoint, setDrawStartPoint] = useState<{ lat: number; lng: number } | null>(null);
  const [currentDrawRect, setCurrentDrawRect] = useState<{
    minLat: number;
    maxLat: number;
    minLng: number;
    maxLng: number;
  } | null>(null);

  const closeFloatingPanels = useCallback(() => {
    setIsMapStyleOpen(false);
    setIsLayerControlsOpen(false);
    setIsExportMenuOpen(false);
  }, []);

  // Initialize map container
  useEffect(() => {
    setMapContainer(mapContainerRef.current);
  }, [setMapContainer]);

  // Apply opacity when it changes
  useLayerOpacity();

  // Use layer visibility directly from context (synced with URL)
  const cellsLayerConfig = useMemo(
    () => ({
      cellSites: layerState.visibility.cellular ?? false,
      cellSiteHeatmap: layerState.visibility["cellular-heatmap"] ?? false,
      signalCoverage: layerState.visibility["cellular-signal"] ?? false,
      pointView: false, // Not yet implemented in admin map UI
    }),
    [layerState.visibility],
  );

  const NAPsVisibilities = useMemo(() => {
    const napsActive = layerState.visibility.naps ?? false;
    return {
      "NAPs-cluster": napsActive && (layerState.visibility["NAPs-cluster"] ?? true),
      "NAPs-cluster-count": napsActive && (layerState.visibility["NAPs-cluster-count"] ?? true),
      "NAPs-unclustered": napsActive && (layerState.visibility["NAPs-unclustered"] ?? true),
    };
  }, [layerState.visibility]);

  const fibersVisibilities = useMemo(() => {
    const fiberActive = layerState.visibility.fiber ?? false;
    return {
      fiber: fiberActive,
      "fiber-line": fiberActive && (layerState.visibility["fiber-line"] ?? true),
      "highlighted-fiber": layerState.visibility["highlighted-fiber"] ?? false,
    };
  }, [layerState.visibility]);

  const speedtestsVisibilities = useMemo(() => {
    const speedtestsActive = layerState.visibility["speedtests-cellular"] ?? false;
    return {
      "speedtests-circles": speedtestsActive && (layerState.visibility["speedtests-circles"] ?? true),
      "speedtests-heatmap": speedtestsActive && (layerState.visibility["speedtests-heatmap"] ?? true),
    };
  }, [layerState.visibility]);

  const { user } = useUser();
  const { data: authUser } = useAuthUser({
    pathParams: { id: user?.id ?? "" },
    queryParams: {},
  });
  const isSuperAdmin = authUser?.data?.role?.key === "super_admin";
  const routeParams = useParams();
  const regionParam = routeParams?.region as string | undefined;

  // Determine effective region filtering based on user role:
  // - Super Admin: nationwide access (regionIds = undefined by default, unless explicitly selected in filter panel)
  // - Regional Admin / Admin: scoped to their region (regionIds = filters.regionIds or [regionParam])
  const effectiveRegionIds = useMemo(() => {
    if (filters.regionIds.length > 0) {
      return filters.regionIds;
    }
    if (isSuperAdmin) {
      return undefined; // Superadmin sees all regions nationwide
    }
    if (regionParam && regionParam !== "all" && regionParam !== "select-region") {
      return [regionParam];
    }
    return undefined;
  }, [filters.regionIds, isSuperAdmin, regionParam]);

  // Build cell options from filters - includes provider filtering and admin mode
  const cellOptions = useMemo(
    () => ({
      regionIds: effectiveRegionIds,
      serviceProviderIds: filters.providers.length > 0 ? filters.providers : undefined,
      generationIds: filters.generationIds.length > 0 ? filters.generationIds : undefined,
      isAdminMode: true,
      signalStrengthFilter: filters.signalStrength,
    }),
    [effectiveRegionIds, filters.providers, filters.generationIds, filters.signalStrength],
  );

  // Build NAP options from filters - includes wired service provider filtering
  const napOptions = useMemo(
    () => ({
      serviceProviderIds: filters.wiredServiceProviderIds.length > 0 ? filters.wiredServiceProviderIds : undefined,
      regionIds: effectiveRegionIds,
    }),
    [filters.wiredServiceProviderIds, effectiveRegionIds],
  );

  // Build speedtest options from filters - includes wireless service provider filtering
  const speedtestOptions = useMemo(
    () => ({
      serviceProviderIds:
        filters.wirelessServiceProviderIds.length > 0 ? filters.wirelessServiceProviderIds : undefined,
      regionIds: effectiveRegionIds,
      minDownloadSpeed: filters.speedTest.downloadMin,
      maxDownloadSpeed: filters.speedTest.downloadMax,
      minUploadSpeed: filters.speedTest.uploadMin,
      maxUploadSpeed: filters.speedTest.uploadMax,
      startDate: filters.dateRange.startDate?.toISOString(),
      endDate: filters.dateRange.endDate?.toISOString(),
    }),
    [
      filters.wirelessServiceProviderIds,
      effectiveRegionIds,
      filters.speedTest.downloadMin,
      filters.speedTest.downloadMax,
      filters.speedTest.uploadMin,
      filters.speedTest.uploadMax,
      filters.dateRange.startDate,
      filters.dateRange.endDate,
    ],
  );

  // Build fiber filter from selected wired provider names (used to filter the vector tile layer)
  const fiberProviderNames = useMemo(() => {
    const names = filters.fiberProviderNames ?? [];
    return names.length > 0 ? names : undefined;
  }, [filters.fiberProviderNames]);

  useMapCells(cellsLayerConfig, cellOptions);
  useMapNAPs(NAPsVisibilities, napOptions);
  useMapFibers(fibersVisibilities, true, fiberProviderNames); // Admin mode with provider filter
  useMapSpeedtests(speedtestsVisibilities, speedtestOptions);
  useMapFly();
  useMapMove();

  // Check if cell details should be shown
  const cellId = searchParams.get("cellId");

  // Handle map click for point selection
  const handleMapClick = useCallback(
    (e: { lngLat: { lat: number; lng: number } }) => {
      if (isPointSelectionMode) {
        setSelectedPoint({ lat: e.lngLat.lat, lng: e.lngLat.lng });
        setIsPointSelectionMode(false);
        setIsAnalyticsPanelOpen(true);
        closeFloatingPanels();
      }
    },
    [closeFloatingPanels, isPointSelectionMode],
  );

  // Handle mousedown for envelope drawing start
  const handleMouseDown = useCallback(
    (e: { lngLat: { lat: number; lng: number } }) => {
      if (isEnvelopeDrawingMode) {
        setDrawStartPoint({ lat: e.lngLat.lat, lng: e.lngLat.lng });
      }
    },
    [isEnvelopeDrawingMode],
  );

  // Handle mousemove for envelope drawing preview
  const handleMouseMove = useCallback(
    (e: { lngLat: { lat: number; lng: number } }) => {
      if (isEnvelopeDrawingMode && drawStartPoint) {
        const minLat = Math.min(drawStartPoint.lat, e.lngLat.lat);
        const maxLat = Math.max(drawStartPoint.lat, e.lngLat.lat);
        const minLng = Math.min(drawStartPoint.lng, e.lngLat.lng);
        const maxLng = Math.max(drawStartPoint.lng, e.lngLat.lng);
        setCurrentDrawRect({ minLat, maxLat, minLng, maxLng });
      }
    },
    [isEnvelopeDrawingMode, drawStartPoint],
  );

  // Handle mouseup for envelope drawing complete
  const handleMouseUp = useCallback(
    (e: { lngLat: { lat: number; lng: number } }) => {
      if (isEnvelopeDrawingMode && drawStartPoint) {
        const minLat = Math.min(drawStartPoint.lat, e.lngLat.lat);
        const maxLat = Math.max(drawStartPoint.lat, e.lngLat.lat);
        const minLng = Math.min(drawStartPoint.lng, e.lngLat.lng);
        const maxLng = Math.max(drawStartPoint.lng, e.lngLat.lng);

        // Only set if the area is meaningful (not just a click)
        if (Math.abs(maxLat - minLat) > 0.0001 && Math.abs(maxLng - minLng) > 0.0001) {
          setCustomEnvelope({ minLat, maxLat, minLng, maxLng });
        }

        setDrawStartPoint(null);
        setCurrentDrawRect(null);
        setIsEnvelopeDrawingMode(false);
      }
    },
    [isEnvelopeDrawingMode, drawStartPoint],
  );

  // Register map click handler
  useEffect(() => {
    if (!map || !isMapLoaded) return;

    const clickHandler = (e: MapMouseEvent) => {
      handleMapClick({ lngLat: { lat: e.lngLat.lat, lng: e.lngLat.lng } });
    };

    const mouseDownHandler = (e: MapMouseEvent) => {
      handleMouseDown({ lngLat: { lat: e.lngLat.lat, lng: e.lngLat.lng } });
    };

    const mouseMoveHandler = (e: MapMouseEvent) => {
      handleMouseMove({ lngLat: { lat: e.lngLat.lat, lng: e.lngLat.lng } });
    };

    const mouseUpHandler = (e: MapMouseEvent) => {
      handleMouseUp({ lngLat: { lat: e.lngLat.lat, lng: e.lngLat.lng } });
    };

    map.on("click", clickHandler);
    map.on("mousedown", mouseDownHandler);
    map.on("mousemove", mouseMoveHandler);
    map.on("mouseup", mouseUpHandler);

    return () => {
      map.off("click", clickHandler);
      map.off("mousedown", mouseDownHandler);
      map.off("mousemove", mouseMoveHandler);
      map.off("mouseup", mouseUpHandler);
    };
  }, [map, isMapLoaded, handleMapClick, handleMouseDown, handleMouseMove, handleMouseUp]);

  // Update cursor and disable map drag when in point selection or envelope drawing mode
  useEffect(() => {
    if (!map) return;

    const canvas = map.getCanvas();
    const container = map.getContainer();

    if (isPointSelectionMode || isEnvelopeDrawingMode) {
      // Add CSS class and inline style to ensure crosshair shows
      canvas.style.cursor = "crosshair";
      container.style.cursor = "crosshair";

      if (isEnvelopeDrawingMode) {
        // Disable map drag interaction during envelope drawing
        map.dragPan.disable();
      }
    } else {
      canvas.style.cursor = "";
      container.style.cursor = "";
      // Re-enable map drag when not in drawing mode
      map.dragPan.enable();
    }

    return () => {
      // Ensure drag is re-enabled and styles are removed on cleanup
      canvas.style.cursor = "";
      container.style.cursor = "";
      if (map.dragPan) {
        map.dragPan.enable();
      }
    };
  }, [map, isPointSelectionMode, isEnvelopeDrawingMode]);

  // Draw preview rectangle during envelope drawing - update in realtime
  useEffect(() => {
    if (!map || !isMapLoaded || !map.isStyleLoaded()) return;

    const previewLayerId = "envelope-drawing-preview";
    const previewSourceId = "envelope-drawing-preview-source";

    if (currentDrawRect) {
      const coordinates = [
        [
          [currentDrawRect.minLng, currentDrawRect.minLat],
          [currentDrawRect.maxLng, currentDrawRect.minLat],
          [currentDrawRect.maxLng, currentDrawRect.maxLat],
          [currentDrawRect.minLng, currentDrawRect.maxLat],
          [currentDrawRect.minLng, currentDrawRect.minLat],
        ],
      ];

      const geojsonData = {
        type: "Feature" as const,
        properties: {},
        geometry: { type: "Polygon" as const, coordinates },
      };

      // Check if source already exists - if so, just update data
      const existingSource = map.isStyleLoaded() ? (map.getSource(previewSourceId) as any) : null;
      if (existingSource) {
        existingSource.setData(geojsonData);
      } else {
        // Create source and layer if they don't exist
        map.addSource(previewSourceId, { type: "geojson", data: geojsonData });

        map.addLayer({
          id: previewLayerId,
          type: "line",
          source: previewSourceId,
          paint: { "line-color": "#3b82f6", "line-width": 2, "line-dasharray": [4, 2] },
        });
      }
    } else {
      // Clean up when no currentDrawRect
      if (map.isStyleLoaded()) {
        if (map.getLayer(previewLayerId)) map.removeLayer(previewLayerId);
        if (map.getSource(previewSourceId)) map.removeSource(previewSourceId);
      }
    }

    return () => {
      if (map.isStyleLoaded()) {
        if (map.getLayer(previewLayerId)) map.removeLayer(previewLayerId);
        if (map.getSource(previewSourceId)) map.removeSource(previewSourceId);
      }
    };
  }, [map, isMapLoaded, currentDrawRect]);

  // Toggle analytics panel and optionally enter point selection mode
  const toggleAnalyticsPanel = useCallback(() => {
    setIsAnalyticsPanelOpen((prev) => {
      const next = !prev;
      if (next) {
        closeFloatingPanels();
      }
      return next;
    });
  }, [closeFloatingPanels]);

  // Enter point selection mode
  const startPointSelection = useCallback(() => {
    setIsPointSelectionMode(true);
    setIsEnvelopeDrawingMode(false);
    setIsAnalyticsPanelOpen(true);
    closeFloatingPanels();
  }, [closeFloatingPanels]);

  // Enter envelope drawing mode
  const startEnvelopeDrawing = useCallback(() => {
    setIsEnvelopeDrawingMode(true);
    setIsPointSelectionMode(false);
  }, []);

  // Clear custom envelope
  const clearCustomEnvelope = useCallback(() => {
    setCustomEnvelope(null);
  }, []);

  // Close analytics panel
  const closeAnalyticsPanel = useCallback(() => {
    setIsAnalyticsPanelOpen(false);
    setIsPointSelectionMode(false);
    setIsEnvelopeDrawingMode(false);
  }, []);

  // Map export handlers
  const handleDownloadPNG = useCallback(() => {
    if (!map) return;
    if (map.isMoving()) {
      // Map is moving, wait for idle
      map.once("idle", () => {
        downloadMapScreenshot(map.getCanvas(), "admin");
      });
      map.triggerRepaint();
    } else {
      // Map is already idle, execute immediately
      downloadMapScreenshot(map.getCanvas(), "admin");
    }
  }, [map]);

  const handleDownloadPDF = useCallback(() => {
    if (!map) return;
    if (map.isMoving()) {
      // Map is moving, wait for idle
      map.once("idle", () => {
        exportMapScreenshotAsPDF(map.getCanvas(), "admin");
      });
      map.triggerRepaint();
    } else {
      // Map is already idle, execute immediately
      exportMapScreenshotAsPDF(map.getCanvas(), "admin");
    }
  }, [map]);

  const handleCopyClipboard = useCallback(() => {
    if (!map) return;
    if (map.isMoving()) {
      // Map is moving, wait for idle
      map.once("idle", () => {
        copyMapScreenshotToClipboard(map.getCanvas())
          .then(() => toast.success("Map copied to clipboard"))
          .catch(() => toast.error("Could not copy to clipboard"));
      });
      map.triggerRepaint();
    } else {
      // Map is already idle, execute immediately
      copyMapScreenshotToClipboard(map.getCanvas())
        .then(() => toast.success("Map copied to clipboard"))
        .catch(() => toast.error("Could not copy to clipboard"));
    }
  }, [map]);

  // Disable screenshot button while the map is panning.
  // Note: The screenshot handlers (handleDownloadPNG, handleDownloadPDF, handleCopyClipboard)
  // already internally wait for the map to reach idle state via map.once("idle") before
  // capturing. So we don't need to gate the button with isMapIdle state.
  // This avoids the issue of the button staying disabled when layers are still loading.
  // The map's own idle event ensures screenshots are captured at the right time.

  return (
    <div className="relative h-full w-full overflow-hidden">
      {/* Map container */}
      <div ref={mapContainerRef} className="absolute inset-0 w-full h-full" style={{ borderRadius: "8px" }} />

      {/* Filter Panel (Left sidebar) */}
      <FilterPanel />

      {/* Cell Site Details (Right sidebar) */}
      {cellId && <CellSiteDetails />}

      {/* Broadband Analytics Panel */}
      <AdminBroadbandPanel
        isOpen={isAnalyticsPanelOpen}
        onClose={closeAnalyticsPanel}
        selectedPoint={selectedPoint}
        onStartPointSelection={startPointSelection}
        isPointSelectionMode={isPointSelectionMode}
        customEnvelope={customEnvelope}
        onStartEnvelopeDrawing={startEnvelopeDrawing}
        isEnvelopeDrawingMode={isEnvelopeDrawingMode}
        onClearCustomEnvelope={clearCustomEnvelope}
      />

      {/* Right-side controls */}
      <div
        className="absolute top-4 flex flex-col gap-3 items-end overflow-visible transition-all duration-300"
        style={{ zIndex: MAP_CONTROL_STACK_Z_INDEX, right: isAnalyticsPanelOpen ? `${RIGHT_DRAWER_OPEN_OFFSET_PX}px` : "1rem" }}>
        <NavigationControls />
        <GlassPanel padding="sm" className="flex flex-col gap-1 overflow-visible">
          <MapStyleSelector
            position="top-right"
            compact
            dropdownAlign="right"
            open={isMapStyleOpen}
            onOpenChange={(open) => {
              setIsMapStyleOpen(open);
              if (open) {
                setIsLayerControlsOpen(false);
                setIsExportMenuOpen(false);
              }
            }}
          />
          <MapControlButton
            tooltip="Analyze location"
            onClick={toggleAnalyticsPanel}
            active={isAnalyticsPanelOpen}
            tooltipSide="left"
            size="sm">
            <BarChart3 className="h-5 w-5" />
          </MapControlButton>
          <DropdownMenu
            open={isExportMenuOpen}
            onOpenChange={(open) => {
              setIsExportMenuOpen(open);
              if (open) {
                setIsMapStyleOpen(false);
                setIsLayerControlsOpen(false);
              }
            }}>
            <DropdownMenuTrigger asChild>
              <MapControlButton
                tooltip="Export map"
                tooltipSide="left"
                size="sm"
                aria-label="Export map">
                <Camera className="h-5 w-5" />
              </MapControlButton>
            </DropdownMenuTrigger>
            <DropdownMenuContent side="left" align="center" className="mr-1 text-xs">
              <DropdownMenuItem onClick={handleDownloadPNG}>
                <Camera className="h-4 w-4 mr-2" />
                Download PNG
              </DropdownMenuItem>
              <DropdownMenuItem onClick={handleDownloadPDF}>
                <FileText className="h-4 w-4 mr-2" />
                Download PDF
              </DropdownMenuItem>
              <DropdownMenuSeparator />
              <DropdownMenuItem onClick={handleCopyClipboard}>
                <ClipboardCopy className="h-4 w-4 mr-2" />
                Copy to clipboard
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
          <LayerControls
            position="right"
            compact
            open={isLayerControlsOpen}
            onOpenChange={(open) => {
              setIsLayerControlsOpen(open);
              if (open) {
                setIsMapStyleOpen(false);
                setIsExportMenuOpen(false);
              }
            }}
          />
        </GlassPanel>
      </div>

      {/* Point selection mode indicator */}
      {isPointSelectionMode && (
        <div className="absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 z-50 pointer-events-none">
          <div className="bg-blue-600 text-white px-4 py-2 rounded-full shadow-lg text-sm font-medium animate-pulse">
            Click on the map to select a point
          </div>
        </div>
      )}

      {/* Envelope drawing mode indicator */}
      {isEnvelopeDrawingMode && !drawStartPoint && (
        <div className="absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 z-50 pointer-events-none">
          <div className="bg-purple-600 text-white px-4 py-2 rounded-full shadow-lg text-sm font-medium animate-pulse flex items-center gap-2">
            <Pencil className="h-4 w-4" />
            Click and drag to draw a custom area
          </div>
        </div>
      )}

      {/* Attribution */}
      <div className="absolute bottom-1 right-1 z-30 text-xs text-gray-400 bg-white/50 px-1 rounded">
        © MapTiler © OpenStreetMap
      </div>
    </div>
  );
}

/**
 * Main Admin View Map Component
 * Wraps content with the AdminMapProvider for state management
 */
export default function AdminViewMap() {
  return (
    <AdminMapProvider>
      <AdminMapContent />
    </AdminMapProvider>
  );
}
