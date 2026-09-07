"use client";

import "@maptiler/sdk/dist/maptiler-sdk.css";
import "@/styles/maptiler.css";

import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { useSearchParams } from "next/navigation";
import { useUser } from "@clerk/nextjs";
import { Popup, type MapGeoJSONFeature, type MapMouseEvent } from "@maptiler/sdk";
import { Camera, ClipboardCopy, FileText, MapPin } from "lucide-react";
import { createRoot, type Root } from "react-dom/client";
import { toast } from "sonner";

import {
  DetailsPanel,
  FloatingSearch,
  GlassPanel,
  LayerOpacityProvider,
  LayerToggles,
  MapControlButton,
  MapStyleSwitcher,
  NavigationControls,
  QuickStatsBanner,
  useDetailsPanel,
  useLayerOpacity,
} from "@/components/public-map/controls";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { useMapFly } from "@/hooks/use-map-fly";
import { useMap } from "@/hooks/useMap";
import { useMapCells } from "@/hooks/useMapCells";
import { useMapMove } from "@/hooks/useMapMove";
import { copyMapScreenshotToClipboard, downloadMapScreenshot, exportMapScreenshotAsPDF } from "@/utils/map-screenshot";

// All properties popup - shows all available properties from the QoS feature
function QoSPopupContent({ properties, onClose }: { properties: Record<string, unknown>; onClose?: () => void }) {
  // Extract known properties and remaining ones
  const knownKeys = [
    "zipcode",
    "Zipcode",
    "barangay",
    "Barangay",
    "municipality",
    "Municipality",
    "province",
    "Province",
    "region",
    "Region",
    "providers",
    "Providers",
    "Provider",
    "provider",
  ];

  // Parse providers
  const rawProviders = (properties.providers ??
    properties.Providers ??
    properties.Provider ??
    properties.provider ??
    "") as string;
  const providersList = rawProviders
    .split(/[,\/]/)
    .map((p) => p.trim())
    .filter((p) => p.length > 0);

  // Get all other properties not in the known list
  const otherProperties = Object.entries(properties).filter(([key]) => !knownKeys.includes(key) && key !== "id");

  // Normalize known properties
  const zipcode = (properties.zipcode ?? properties.Zipcode ?? "") as string;
  const barangay = (properties.barangay ?? properties.Barangay ?? "") as string;
  const municipality = (properties.municipality ?? properties.Municipality ?? "") as string;
  const province = (properties.province ?? properties.Province ?? "") as string;
  const region = (properties.region ?? properties.Region ?? "") as string;

  return (
    <div className="bg-white/95 backdrop-blur-md rounded-2xl shadow-2xl p-5 min-w-[300px] max-w-[400px] border border-white/20 max-h-[70vh] overflow-y-auto">
      <div className="flex items-center justify-between border-b border-gray-100 pb-3 mb-4 sticky top-0 bg-white/95">
        <h3 className="text-lg font-bold text-gray-800">Fiber Coverage Details</h3>
        {onClose && (
          <button onClick={onClose} className="p-1 hover:bg-gray-100 rounded-full transition-colors">
            <svg className="w-4 h-4 text-gray-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        )}
      </div>

      {/* Location info section */}
      <div className="space-y-2.5 text-sm">
        {region && (
          <div className="flex items-start gap-3">
            <span className="text-gray-400 w-28 shrink-0 text-xs uppercase tracking-wide">Region</span>
            <span className="text-gray-800 font-medium">{region}</span>
          </div>
        )}
        {province && (
          <div className="flex items-start gap-3">
            <span className="text-gray-400 w-28 shrink-0 text-xs uppercase tracking-wide">Province</span>
            <span className="text-gray-800 font-medium">{province}</span>
          </div>
        )}
        {municipality && (
          <div className="flex items-start gap-3">
            <span className="text-gray-400 w-28 shrink-0 text-xs uppercase tracking-wide">Municipality</span>
            <span className="text-gray-800 font-medium">{municipality}</span>
          </div>
        )}
        {barangay && (
          <div className="flex items-start gap-3">
            <span className="text-gray-400 w-28 shrink-0 text-xs uppercase tracking-wide">Barangay</span>
            <span className="text-gray-800 font-medium">{barangay}</span>
          </div>
        )}
        {zipcode && (
          <div className="flex items-start gap-3">
            <span className="text-gray-400 w-28 shrink-0 text-xs uppercase tracking-wide">Zip Code</span>
            <span className="text-gray-800 font-medium">{zipcode}</span>
          </div>
        )}
      </div>

      {/* Service Providers section */}
      {providersList.length > 0 && (
        <div className="mt-5 pt-4 border-t border-gray-100">
          <div className="flex items-center gap-2 mb-3">
            <svg className="w-4 h-4 text-primary" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M8.111 16.404a5.5 5.5 0 017.778 0M12 20h.01m-7.08-7.071c3.904-3.905 10.236-3.905 14.141 0M1.394 9.393c5.857-5.857 15.355-5.857 21.213 0"
              />
            </svg>
            <p className="text-xs font-semibold text-gray-600 uppercase tracking-wide">
              Service Providers ({providersList.length})
            </p>
          </div>
          <div className="flex flex-wrap gap-2">
            {providersList.map((provider, idx) => (
              <span
                key={idx}
                className="inline-flex items-center px-3 py-1.5 rounded-full text-xs font-medium bg-gradient-to-r from-primary/10 to-primary/5 text-primary border border-primary/20">
                {provider}
              </span>
            ))}
          </div>
        </div>
      )}

      {otherProperties.length > 0 && (
        <div className="mt-5 pt-4 border-t border-gray-100">
          <p className="text-xs font-semibold text-gray-500 uppercase tracking-wide mb-3">Additional Properties</p>
          <div className="space-y-2 text-sm">
            {otherProperties.map(([key, value]) => {
              // Safely convert value to string and sanitize for display
              const displayValue =
                typeof value === "object" ? JSON.stringify(value) : String(value ?? "").slice(0, 500); // Limit length
              return (
                <div key={key} className="flex items-start gap-3">
                  <span className="text-gray-400 w-28 shrink-0 text-xs uppercase tracking-wide">{key}</span>
                  <span className="text-gray-800 font-medium break-all">{displayValue}</span>
                </div>
              );
            })}
          </div>
        </div>
      )}

      {providersList.length === 0 && (
        <div className="mt-5 pt-4 border-t border-gray-100">
          <p className="text-sm text-gray-400 italic flex items-center gap-2">
            <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
              />
            </svg>
            No provider information available
          </p>
        </div>
      )}
    </div>
  );
}

// Provider filter component for the layer panel
interface ProviderFilterProps {
  availableProviders: string[];
  selectedProviders: Set<string>;
  onToggleProvider: (provider: string) => void;
  onClearAll: () => void;
  onSelectAll: () => void;
}

function _ProviderFilter({
  availableProviders,
  selectedProviders,
  onToggleProvider,
  onClearAll,
  onSelectAll,
}: ProviderFilterProps) {
  if (availableProviders.length === 0) return null;

  return (
    <div className="mt-3 pt-3 border-t border-gray-100">
      <div className="flex items-center justify-between mb-2">
        <p className="text-xs font-semibold text-gray-500 uppercase tracking-wide">Filter by Provider</p>
        <div className="flex gap-2">
          <button onClick={onSelectAll} className="text-xs text-primary hover:underline">
            All
          </button>
          <button onClick={onClearAll} className="text-xs text-gray-400 hover:underline">
            Clear
          </button>
        </div>
      </div>
      <div className="flex flex-wrap gap-1.5 max-h-32 overflow-y-auto">
        {availableProviders.map((provider) => {
          const isSelected = selectedProviders.has(provider);
          return (
            <button
              key={provider}
              onClick={() => onToggleProvider(provider)}
              className={`px-2 py-1 rounded-full text-xs font-medium transition-colors ${isSelected ? "bg-primary text-white" : "bg-gray-100 text-gray-600 hover:bg-gray-200"
                }`}>
              {provider}
            </button>
          );
        })}
      </div>
    </div>
  );
}

// Hook for QoS Layer with opacity and provider filtering
function useQoSLayer(options?: { onFeatureSelect?: (props: Record<string, unknown> | null) => void }) {
  const { map, isMapLoaded } = useMap();
  const searchParams = useSearchParams();
  const qosParam = searchParams.get("qos");
  const qosEnabled = qosParam === null ? true : qosParam === "true";
  const { opacities } = useLayerOpacity();

  const popupRef = useRef<Popup | null>(null);
  const popupRootRef = useRef<Root | null>(null);
  const popupContainerRef = useRef<HTMLDivElement | null>(null);
  const hoveredFeatureIdRef = useRef<string | number | undefined>(undefined);

  const SOURCE_ID = "qos-vector-source";
  const SOURCE_LAYER_ID = process.env.NEXT_PUBLIC_MAPTILER_FIBER_SOURCE_LAYER_ID ?? "qos";
  const FILL_LAYER_ID = "qos-fill-layer";
  const LINE_LAYER_ID = "qos-line-layer";

  // Get opacity value (default to 70%)
  const qosOpacity = (opacities.qos ?? 70) / 100;

  // Create popup content - now passes all properties
  const createPopupContent = useCallback((properties: Record<string, unknown>, popup: Popup) => {
    if (!popupContainerRef.current) {
      popupContainerRef.current = document.createElement("div");
    }

    if (popupRootRef.current) {
      popupRootRef.current.unmount();
    }

    popupRootRef.current = createRoot(popupContainerRef.current);
    popupRootRef.current.render(<QoSPopupContent properties={properties} onClose={() => popup.remove()} />);

    return popupContainerRef.current;
  }, []);

  // Update opacity when it changes
  useEffect(() => {
    if (!map || !isMapLoaded || !map.isStyleLoaded()) return;

    if (map.getLayer(FILL_LAYER_ID)) {
      map.setPaintProperty(FILL_LAYER_ID, "fill-opacity", qosOpacity);
    }
    if (map.getLayer(LINE_LAYER_ID)) {
      map.setPaintProperty(LINE_LAYER_ID, "line-opacity", qosOpacity);
    }
  }, [map, isMapLoaded, qosOpacity]);

  // Add QoS source and layers
  useEffect(() => {
    if (!map || !isMapLoaded) return;

    const sourceUrl = process.env.NEXT_PUBLIC_MAPTILER_URL;
    if (!sourceUrl) return;

    const addQoSSource = () => {
      try {
        if (!map || !map.isStyleLoaded()) return;
        if (!map.getSource(SOURCE_ID)) {
          map.addSource(SOURCE_ID, { type: "vector", url: sourceUrl });
        }
      } catch {
        // Source may already exist or style not ready; styledata will retry
      }
    };

    /**
     * Finds the first place-name / symbol label layer so QoS layers can be
     * inserted below it, keeping map location labels legible at all times.
     */
    const findFirstLabelLayer = (): string | undefined => {
      try {
        if (!map || !map.isStyleLoaded()) return undefined;
        const layers = map.getStyle()?.layers;
        if (!layers) return undefined;

        // Prefer place / settlement / city / town labels
        for (const layer of layers) {
          if (layer.type === "symbol") {
            const id = layer.id.toLowerCase();
            if (id.includes("place") || id.includes("settlement") || id.includes("city") || id.includes("town")) {
              return layer.id;
            }
          }
        }

        // Fallback: any symbol layer whose id contains "label"
        for (const layer of layers) {
          if (layer.type === "symbol" && layer.id.toLowerCase().includes("label")) {
            return layer.id;
          }
        }

        return undefined; // Render on top if no label layer is found
      } catch {
        return undefined;
      }
    };

    const addQoSLayers = () => {
      if (!map.getSource(SOURCE_ID)) return;

      // Insert QoS layers below the first label layer so place-name labels
      // are always rendered above the coverage overlay.
      const beforeLabelLayer = findFirstLabelLayer();

      try {
        // Add fill layer
        if (!map.getLayer(FILL_LAYER_ID)) {
          map.addLayer(
            {
              id: FILL_LAYER_ID,
              type: "fill",
              source: SOURCE_ID,
              "source-layer": SOURCE_LAYER_ID,
              paint: {
                "fill-color": [
                  "case",
                  ["boolean", ["feature-state", "hover"], false],
                  "rgba(59, 130, 246, 0.5)",
                  "rgba(139, 92, 246, 0.25)",
                ],
                "fill-opacity": qosOpacity,
              },
              layout: { visibility: qosEnabled ? "visible" : "none" },
            },
            beforeLabelLayer,
          );
        }

        // Add line layer
        if (!map.getLayer(LINE_LAYER_ID)) {
          map.addLayer(
            {
              id: LINE_LAYER_ID,
              type: "line",
              source: SOURCE_ID,
              "source-layer": SOURCE_LAYER_ID,
              paint: {
                "line-color": ["case", ["boolean", ["feature-state", "hover"], false], "#3b82f6", "#8b5cf6"],
                "line-width": ["case", ["boolean", ["feature-state", "hover"], false], 2.5, 1],
                "line-opacity": qosOpacity,
              },
              layout: { visibility: qosEnabled ? "visible" : "none" },
            },
            // Render QoS line directly below the QoS fill to preserve the
            // original visual stacking (fill on top of outline).
            FILL_LAYER_ID,
          );
        }
      } catch {
        // Layer add may fail if beforeId layer doesn't exist; sourcedata will retry
      }
    };

    // Mouse move handler for hover effect
    const handleMouseMove = (e: MapMouseEvent & { features?: MapGeoJSONFeature[] }) => {
      const features = map.queryRenderedFeatures(e.point, { layers: [FILL_LAYER_ID] });

      if (features.length === 0) {
        if (hoveredFeatureIdRef.current !== undefined) {
          map.setFeatureState(
            { source: SOURCE_ID, sourceLayer: SOURCE_LAYER_ID, id: hoveredFeatureIdRef.current },
            { hover: false },
          );
          hoveredFeatureIdRef.current = undefined;
        }
        map.getCanvas().style.cursor = "";
        return;
      }

      map.getCanvas().style.cursor = "pointer";
      const feature = features[0]!;
      const featureId = feature.id;

      if (featureId !== hoveredFeatureIdRef.current) {
        if (hoveredFeatureIdRef.current !== undefined) {
          map.setFeatureState(
            { source: SOURCE_ID, sourceLayer: SOURCE_LAYER_ID, id: hoveredFeatureIdRef.current },
            { hover: false },
          );
        }
        hoveredFeatureIdRef.current = featureId;
        map.setFeatureState({ source: SOURCE_ID, sourceLayer: SOURCE_LAYER_ID, id: featureId }, { hover: true });
      }
    };

    // Mouse leave handler
    const handleMouseLeave = () => {
      if (hoveredFeatureIdRef.current !== undefined) {
        map.setFeatureState(
          { source: SOURCE_ID, sourceLayer: SOURCE_LAYER_ID, id: hoveredFeatureIdRef.current },
          { hover: false },
        );
        hoveredFeatureIdRef.current = undefined;
      }
      map.getCanvas().style.cursor = "";
    };

    // Click handler for popup - now passes all properties
    const handleClick = (e: MapMouseEvent & { features?: MapGeoJSONFeature[] }) => {
      const features = map.queryRenderedFeatures(e.point, { layers: [FILL_LAYER_ID] });
      if (features.length === 0) {
        popupRef.current?.remove();
        return;
      }

      map.flyTo({ center: [e.lngLat.lng, e.lngLat.lat], zoom: Math.max(map.getZoom(), 13), speed: 0.3 });

      const feature = features[0]!;
      // Pass all properties to popup for display
      const props = feature.properties as Record<string, unknown>;

      // Notify parent that a fiber feature was selected
      options?.onFeatureSelect?.(props);

      // Create popup - Popup is already imported at top of file
      popupRef.current?.remove();
      popupRef.current = new Popup({
        closeButton: false,
        closeOnClick: true,
        maxWidth: "420px",
        className: "qos-popup-container",
        offset: 15,
      });

      const content = createPopupContent(props, popupRef.current);
      popupRef.current
        .setLngLat([e.lngLat.lng, e.lngLat.lat])
        .setDOMContent(content)
        .addTo(map);
      // Clear selection when popup is closed
      popupRef.current.on("close", () => options?.onFeatureSelect?.(null));
    };

    const setupQoS = () => {
      if (!map.isStyleLoaded()) return;
      addQoSSource();
      addQoSLayers();
    };

    // Bootstrap: add source and layers once style is ready
    let cleanupStyleListener: (() => void) | undefined;
    if (map.isStyleLoaded()) {
      setupQoS();
    } else {
      const onStyleReady = () => {
        if (map.isStyleLoaded()) {
          map.off("styledata", onStyleReady);
          map.off("idle", onStyleReady);
          setupQoS();
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

    // Re-add source and layers after style changes (style change removes all sources/layers)
    const handleStyleData = () => {
      if (!map.isStyleLoaded()) return;
      if (!map.getSource(SOURCE_ID) || !map.getLayer(FILL_LAYER_ID)) {
        setupQoS();
      }
    };

    // Add layers once source metadata has been fetched
    const handleSourceData = (e: { sourceId: string; isSourceLoaded?: boolean }) => {
      if (map && map.isStyleLoaded() && e.sourceId === SOURCE_ID) {
        addQoSLayers();
      }
    };

    map.on("styledata", handleStyleData);
    map.on("sourcedata", handleSourceData);

    map.on("mousemove", FILL_LAYER_ID, handleMouseMove);
    map.on("mouseleave", FILL_LAYER_ID, handleMouseLeave);
    map.on("click", FILL_LAYER_ID, handleClick);

    return () => {
      cleanupStyleListener?.();
      map.off("styledata", handleStyleData);
      map.off("sourcedata", handleSourceData);
      map.off("mousemove", FILL_LAYER_ID, handleMouseMove);
      map.off("mouseleave", FILL_LAYER_ID, handleMouseLeave);
      map.off("click", FILL_LAYER_ID, handleClick);
      popupRef.current?.remove();
    };
  }, [map, isMapLoaded, qosEnabled, qosOpacity, createPopupContent]);

  // Update visibility when qos param changes
  useEffect(() => {
    if (!map || !isMapLoaded || !map.isStyleLoaded()) return;

    if (map.getLayer(FILL_LAYER_ID)) {
      map.setLayoutProperty(FILL_LAYER_ID, "visibility", qosEnabled ? "visible" : "none");
    }
    if (map.getLayer(LINE_LAYER_ID)) {
      map.setLayoutProperty(LINE_LAYER_ID, "visibility", qosEnabled ? "visible" : "none");
    }
  }, [map, isMapLoaded, qosEnabled]);

  // Cleanup
  useEffect(() => {
    return () => {
      if (popupRootRef.current) {
        popupRootRef.current.unmount();
      }
    };
  }, []);
}

function PublicViewMapContent() {
  const mapContainerRef = useRef<HTMLDivElement | null>(null);
  const searchParams = useSearchParams();
  const { setMapContainer, map } = useMap();
  const { isSignedIn } = useUser();
  const { isOpen, onClose, lat, lng, isPointSelectionMode, startPointSelection, cancelPointSelection } =
    useDetailsPanel();

  // Track the currently-selected QoS / fiber-coverage feature (from map click)
  const [selectedFiberFeature, setSelectedFiberFeature] = useState<Record<string, unknown> | null>(null);

  useMapMove();
  const params = useMemo(() => Object.fromEntries(searchParams.entries()), [searchParams]);

  // Cell Site layer configuration - Public map only shows clustered cells and symbols
  const cellsLayerConfig = useMemo(
    () => ({
      cellSites: params.cellular === undefined ? true : params.cellular === "true",
      cellSiteHeatmap: false, // Admin only
      signalCoverage: false, // Admin only
      pointView: false, // Admin only
    }),
    [params],
  );

  // Use QoS/Fiber Coverage layer instead of the old fiber layer
  useMapCells(cellsLayerConfig, { isAdminMode: false });
  useQoSLayer({ onFeatureSelect: setSelectedFiberFeature }); // Fiber Coverage layer (QoS)

  useMapFly();

  useEffect(() => {
    setMapContainer(mapContainerRef.current);
  }, [setMapContainer]);

  const handleDownloadPNG = useCallback(() => {
    if (!map) return;
    map.once("idle", () => { downloadMapScreenshot(map.getCanvas(), "public"); });
    map.triggerRepaint();
  }, [map]);

  const handleDownloadPDF = useCallback(() => {
    if (!map) return;
    map.once("idle", () => { exportMapScreenshotAsPDF(map.getCanvas(), "public"); });
    map.triggerRepaint();
  }, [map]);

  const handleCopyClipboard = useCallback(() => {
    if (!map) return;
    map.once("idle", () => {
      copyMapScreenshotToClipboard(map.getCanvas())
        .then(() => toast.success("Map copied to clipboard"))
        .catch(() => toast.error("Could not copy to clipboard"));
    });
    map.triggerRepaint();
  }, [map]);

  // Disable screenshot button while the map is panning/loading
  const [isMapIdle, setIsMapIdle] = useState(false);
  useEffect(() => {
    if (!map) return;
    const onIdle = () => setIsMapIdle(true);
    const onMoveStart = () => setIsMapIdle(false);
    map.on("idle", onIdle);
    map.on("movestart", onMoveStart);
    return () => {
      map.off("idle", onIdle);
      map.off("movestart", onMoveStart);
    };
  }, [map]);

  return (
    <>
      {/* Full-screen map container */}
      <div className="fixed inset-0 z-0">
        <div ref={mapContainerRef} className="h-full w-full" />
      </div>

      {/* Floating UI Controls */}
      <div className="fixed inset-0 pointer-events-none z-10">
        {/* Top Left - Quick Stats / Branding */}
        <div className="absolute top-4 left-4 pointer-events-auto z-20">
          <QuickStatsBanner />
        </div>

        {/* Top Center - Search Bar - moves to right on small screens to avoid overlapping logo */}
        <div className="absolute top-4 pointer-events-auto z-10 right-4 md:right-auto md:left-1/2 md:-translate-x-1/2 w-auto min-w-[200px] sm:w-full sm:max-w-md sm:px-0 transition-all duration-300 ease-in-out">
          <FloatingSearch />
        </div>

        {/* Bottom Left - Navigation Controls + Point Analysis Button */}
        <div className="absolute bottom-6 left-4 pointer-events-auto flex items-end gap-2">
          <NavigationControls />
          {/* Point Analysis Toggle Button — only available to signed-in users */}
          {isSignedIn && (
            <GlassPanel className="p-1.5">
              <MapControlButton
                tooltip={isPointSelectionMode ? "Cancel point selection" : "Analyze location"}
                onClick={isPointSelectionMode ? cancelPointSelection : startPointSelection}
                active={isPointSelectionMode}>
                <MapPin className="h-5 w-5" />
              </MapControlButton>
            </GlassPanel>
          )}
        </div>

        {/* Bottom Right - Map Style & Layers + Screenshot */}
        <div className="absolute bottom-6 right-4 pointer-events-auto flex flex-col gap-2 items-end">
          <LayerToggles />
          <div className="flex gap-2 items-center">
            <GlassPanel className="p-1.5">
              <DropdownMenu>
                <DropdownMenuTrigger asChild>
                  <MapControlButton
                    disabled={!isMapIdle}
                    aria-label="Export map">
                    {isMapIdle ? <Camera className="h-5 w-5" /> : <Camera className="h-5 w-5 opacity-40" />}
                  </MapControlButton>
                </DropdownMenuTrigger>
                <DropdownMenuContent side="top" align="end" className="mb-1">
                  <DropdownMenuItem onClick={handleDownloadPNG} disabled={!isMapIdle}>
                    <Camera className="h-4 w-4 mr-2" />
                    Download PNG
                  </DropdownMenuItem>
                  <DropdownMenuItem onClick={handleDownloadPDF} disabled={!isMapIdle}>
                    <FileText className="h-4 w-4 mr-2" />
                    Download PDF
                  </DropdownMenuItem>
                  <DropdownMenuSeparator />
                  <DropdownMenuItem onClick={handleCopyClipboard} disabled={!isMapIdle}>
                    <ClipboardCopy className="h-4 w-4 mr-2" />
                    Copy to clipboard
                  </DropdownMenuItem>
                </DropdownMenuContent>
              </DropdownMenu>
            </GlassPanel>
            <MapStyleSwitcher />
          </div>
        </div>
      </div>

      {/* Point selection mode indicator */}
      {isPointSelectionMode && (
        <div className="fixed top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 z-50 pointer-events-none">
          <div className="bg-blue-600 text-white px-4 py-2 rounded-full shadow-lg text-sm font-medium animate-pulse">
            Click on the map to analyze location
          </div>
        </div>
      )}

      {/* Details Panel (slides from right) */}
      <DetailsPanel isOpen={isOpen} onClose={onClose} lat={lat} lng={lng} selectedFiberFeature={selectedFiberFeature} />
    </>
  );
}

// Wrapper component with LayerOpacityProvider
export default function PublicViewMap() {
  return (
    <LayerOpacityProvider>
      <PublicViewMapContent />
    </LayerOpacityProvider>
  );
}
