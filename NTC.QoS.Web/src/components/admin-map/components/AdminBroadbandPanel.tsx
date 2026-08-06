/**
 * Admin Broadband Analytics Panel
 *
 * A comprehensive panel for displaying broadband infrastructure analytics in the admin map.
 * Supports both point-based (click on map) and area-based (viewport) analytics.
 *
 * Features:
 * - Point broadband info with configurable radius
 * - Area broadband info based on map viewport
 * - Summary statistics with animated cards
 * - Service provider breakdown
 * - Municipality coverage details
 */

"use client";

import { memo, useCallback, useEffect, useRef, useState } from "react";
import bboxPolygon from "@turf/bbox-polygon";
import circle from "@turf/circle";
import { point } from "@turf/helpers";
import { AnimatePresence, motion } from "framer-motion";
import {
  Antenna,
  BarChart3,
  Building2,
  Check,
  ChevronDown,
  ChevronUp,
  FileDown,
  Layers,
  Loader2,
  MapPin,
  MapPinned,
  Network,
  Pencil,
  RotateCcw,
  Square,
  Target,
  Wifi,
  X,
} from "lucide-react";
import { toast } from "sonner";

import { getAreaBroadbandInfoRequest } from "@/components/features/analytics/handlers/getAreaBroadbandInfoRequest";
import { getPointBroadbandInfoRequest } from "@/components/features/analytics/handlers/getPointBroadbandInfoRequest";
import type {
  BroadbandInfoData,
  MunicipalityInfo,
  ServiceProviderInfo,
} from "@/components/features/analytics/types/BroadbandInfo";
import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
import { Label } from "@/components/ui/label";
import { ScrollArea } from "@/components/ui/scroll-area";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { createMutationHookFromTemplate } from "@/hooks/createMutationHookFromTemplate";
import { useMap } from "@/hooks/useMap";
import useReverseGeoCoding from "@/hooks/useReverseGeoCoding";
import { cn } from "@/lib/utils";
import { formatLabel } from "@/utils/formatLabel";
import { generateLocationReport, type AnalysisBounds, type GeocodingResult } from "@/utils/map-report";
import { ANALYTICS_PANEL_WIDTH_PX, NAVBAR_HEIGHT_PX } from "../layout-constants";
import { GlassPanel } from "../ui/GlassPanel";
import { MapControlButton } from "../ui/MapControlButton";

const usePointBroadbandInfo = createMutationHookFromTemplate(getPointBroadbandInfoRequest);
const useAreaBroadbandInfo = createMutationHookFromTemplate(getAreaBroadbandInfoRequest);

type AnalyticsHeaderActions = { onGenerateReport: () => void; isReportDisabled: boolean };

const DEFAULT_ANALYTICS_HEADER_ACTIONS: AnalyticsHeaderActions = {
  onGenerateReport: () => undefined,
  isReportDisabled: true,
};

interface AdminBroadbandPanelProps {
  isOpen: boolean;
  onClose: () => void;
  /** Selected point coordinates for point-based analysis */
  selectedPoint?: { lat: number; lng: number } | null;
  /** Callback to enter point selection mode on the map */
  onStartPointSelection?: () => void;
  /** Whether point selection mode is active */
  isPointSelectionMode?: boolean;
  /** Custom envelope bounds for area analysis */
  customEnvelope?: { minLat: number; maxLat: number; minLng: number; maxLng: number } | null;
  /** Callback to enter envelope drawing mode */
  onStartEnvelopeDrawing?: () => void;
  /** Whether envelope drawing mode is active */
  isEnvelopeDrawingMode?: boolean;
  /** Callback to clear custom envelope */
  onClearCustomEnvelope?: () => void;
}

/**
 * Summary Card Component
 */
const SummaryCard = memo(function SummaryCard({
  icon: Icon,
  label,
  value,
  gradient,
  borderColor,
  iconColor,
  textColor,
}: {
  icon: typeof Antenna;
  label: string;
  value: number;
  gradient: string;
  borderColor: string;
  iconColor: string;
  textColor: string;
}) {
  return (
    <div className={cn("rounded-xl p-3 border", gradient, borderColor)}>
      <div className="flex items-center gap-2 mb-1">
        <Icon className={cn("h-4 w-4", iconColor)} />
        <span className={cn("text-xs font-medium", iconColor)}>{label}</span>
      </div>
      <p className={cn("text-2xl font-bold", textColor)}>{value.toLocaleString()}</p>
    </div>
  );
});

/**
 * Service Provider Card Component
 */
const ServiceProviderCard = memo(function ServiceProviderCard({ provider }: { provider: ServiceProviderInfo }) {
  return (
    <div className="bg-white rounded-lg border border-gray-200 p-3 hover:shadow-md transition-shadow">
      <div className="flex items-center justify-between mb-2">
        <div className="flex items-center gap-2">
          <div className="w-3 h-3 rounded-full" style={{ backgroundColor: provider.color || "#6B7280" }} />
          <span className="font-semibold text-gray-800 text-sm">{provider.name}</span>
        </div>
        <span className="px-2 py-0.5 bg-blue-100 text-blue-700 text-xs rounded-full font-medium">
          {provider.serviceType}
        </span>
      </div>
      <div className="grid grid-cols-3 gap-2 text-xs">
        <div>
          <span className="text-gray-500">Cells:</span>
          <p className="font-semibold text-gray-800">{provider.cellCount}</p>
        </div>
        <div>
          <span className="text-gray-500">NAPs:</span>
          <p className="font-semibold text-gray-800">{provider.napCount}</p>
        </div>
        <div>
          <span className="text-gray-500">Total:</span>
          <p className="font-semibold text-gray-800">{provider.totalAssets}</p>
        </div>
      </div>
    </div>
  );
});

/**
 * Municipality Card Component
 */
const MunicipalityCard = memo(function MunicipalityCard({ municipality }: { municipality: MunicipalityInfo }) {
  return (
    <div className="bg-white rounded-lg border border-gray-200 p-2.5 text-xs">
      <div className="flex items-center justify-between mb-1.5">
        <span className="font-semibold text-gray-800">{municipality.displayName}</span>
        <span
          className={cn(
            "px-2 py-0.5 rounded-full text-xs font-medium",
            municipality.hasAssets ? "bg-green-100 text-green-700" : "bg-gray-100 text-gray-600",
          )}>
          {formatLabel(municipality.coverageStatus)}
        </span>
      </div>
      {municipality.hasAssets && (
        <div className="grid grid-cols-3 gap-2 text-xs">
          <div>
            <span className="text-gray-500">Cells:</span>
            <p className="font-medium text-gray-700">{municipality.cellCount}</p>
          </div>
          <div>
            <span className="text-gray-500">NAPs:</span>
            <p className="font-medium text-gray-700">{municipality.napCount}</p>
          </div>
          <div>
            <span className="text-gray-500">Coverage:</span>
            <p className="font-medium text-gray-700">{municipality.assetPercentage.toFixed(1)}%</p>
          </div>
        </div>
      )}
    </div>
  );
});

const AnalyticsActionBar = memo(function AnalyticsActionBar({
  onGenerateReport,
  isReportDisabled,
  className,
}: {
  onGenerateReport: () => void;
  isReportDisabled: boolean;
  className?: string;
}) {
  return (
    <div className={cn("flex items-center gap-1", className)}>
      <MapControlButton
        size="sm"
        variant="square"
        tooltip="Generate & download location report"
        tooltipSide="top"
        onClick={onGenerateReport}
        disabled={isReportDisabled}
        aria-label="Generate report">
        <FileDown className={cn("h-4 w-4", isReportDisabled && "opacity-40")} />
      </MapControlButton>
    </div>
  );
});

/**
 * Broadband Data Display Component
 */
const BroadbandDataDisplay = memo(function BroadbandDataDisplay({
  data,
  isLoading,
  radiusInfo,
}: {
  data: BroadbandInfoData | null | undefined;
  isLoading: boolean;
  radiusInfo?: string;
}) {
  const [showProviders, setShowProviders] = useState(true);
  const [showMunicipalities, setShowMunicipalities] = useState(true);

  if (isLoading) {
    return (
      <div className="flex flex-col items-center justify-center py-8 gap-3">
        <Loader2 className="h-8 w-8 animate-spin text-blue-500" />
        <span className="text-sm text-gray-500">Analyzing broadband coverage...</span>
      </div>
    );
  }

  if (!data) {
    return (
      <div className="text-sm text-gray-500 bg-gray-50 rounded-lg p-4 text-center">
        No broadband data available for this area.
      </div>
    );
  }

  const { summary, serviceProviders, envelopeInfo, municipalities } = data;

  return (
    <div className="space-y-4">
      {/* Summary Cards */}
      <div className="grid grid-cols-2 gap-3">
        <SummaryCard
          icon={Antenna}
          label="Cell Sites"
          value={summary.totalCells}
          gradient="bg-gradient-to-br from-blue-50 to-blue-100"
          borderColor="border-blue-200"
          iconColor="text-blue-600"
          textColor="text-blue-900"
        />
        <SummaryCard
          icon={Network}
          label="NAPs"
          value={summary.totalNaps}
          gradient="bg-gradient-to-br from-green-50 to-green-100"
          borderColor="border-green-200"
          iconColor="text-green-600"
          textColor="text-green-900"
        />
        <SummaryCard
          icon={Building2}
          label="Providers"
          value={summary.serviceProviderCount}
          gradient="bg-gradient-to-br from-purple-50 to-purple-100"
          borderColor="border-purple-200"
          iconColor="text-purple-600"
          textColor="text-purple-900"
        />
        <SummaryCard
          icon={Wifi}
          label="Total Assets"
          value={summary.totalAssets}
          gradient="bg-gradient-to-br from-orange-50 to-orange-100"
          borderColor="border-orange-200"
          iconColor="text-orange-600"
          textColor="text-orange-900"
        />
      </div>

      {/* Coverage Info */}
      <div className="bg-gray-50 rounded-xl p-3 border border-gray-200">
        <div className="flex items-center gap-2 mb-2">
          <MapPinned className="h-4 w-4 text-gray-600" />
          <span className="text-sm font-semibold text-gray-700">Coverage Area</span>
        </div>
        <div className="grid grid-cols-2 gap-2 text-xs">
          <div>
            <span className="text-gray-500">Area Size:</span>
            <p className="font-semibold text-gray-800">{summary.coverageAreaKm2.toFixed(2)} km²</p>
          </div>
          <div>
            <span className="text-gray-500">Density:</span>
            <p className="font-semibold text-gray-800">{summary.assetDensityPerKm2.toFixed(1)} assets/km²</p>
          </div>
          <div>
            <span className="text-gray-500">Municipalities:</span>
            <p className="font-semibold text-gray-800">{summary.municipalityCount}</p>
          </div>
          <div>
            <span className="text-gray-500">With Assets:</span>
            <p className="font-semibold text-gray-800">{summary.municipalitiesWithAssets}</p>
          </div>
        </div>
      </div>

      {/* Service Providers */}
      {serviceProviders && serviceProviders.length > 0 && (
        <div className="space-y-2">
          <button
            onClick={() => setShowProviders(!showProviders)}
            className="flex items-center justify-between w-full text-left">
            <span className="text-xs font-semibold text-gray-500 uppercase tracking-wide">
              Service Providers ({serviceProviders.length})
            </span>
            {showProviders ? (
              <ChevronUp className="h-4 w-4 text-gray-400" />
            ) : (
              <ChevronDown className="h-4 w-4 text-gray-400" />
            )}
          </button>
          <AnimatePresence>
            {showProviders && (
              <motion.div
                initial={{ height: 0, opacity: 0 }}
                animate={{ height: "auto", opacity: 1 }}
                exit={{ height: 0, opacity: 0 }}
                className="space-y-2 overflow-hidden">
                {serviceProviders.map((provider) => (
                  <ServiceProviderCard key={provider.id} provider={provider} />
                ))}
              </motion.div>
            )}
          </AnimatePresence>
        </div>
      )}

      {/* Municipalities */}
      {municipalities && municipalities.length > 0 && (
        <div className="space-y-2">
          <button
            onClick={() => setShowMunicipalities(!showMunicipalities)}
            className="flex items-center justify-between w-full text-left">
            <span className="text-xs font-semibold text-gray-500 uppercase tracking-wide">
              Municipalities ({municipalities.length})
            </span>
            {showMunicipalities ? (
              <ChevronUp className="h-4 w-4 text-gray-400" />
            ) : (
              <ChevronDown className="h-4 w-4 text-gray-400" />
            )}
          </button>
          <AnimatePresence>
            {showMunicipalities && (
              <motion.div
                initial={{ height: 0, opacity: 0 }}
                animate={{ height: "auto", opacity: 1 }}
                exit={{ height: 0, opacity: 0 }}
                className="space-y-1.5 max-h-48 overflow-y-auto">
                {municipalities.map((municipality) => (
                  <MunicipalityCard key={municipality.id} municipality={municipality} />
                ))}
              </motion.div>
            )}
          </AnimatePresence>
        </div>
      )}

      {/* Radius/Area Info */}
      {radiusInfo && (
        <div className="bg-blue-50 border border-blue-200 rounded-lg p-2.5 text-xs text-blue-700">
          <div className="flex items-center gap-2">
            <Target className="h-3.5 w-3.5" />
            <span>{radiusInfo}</span>
          </div>
        </div>
      )}
    </div>
  );
});

/**
 * Hook to draw radar pulse effect on the map for point analysis
 */
function usePointRadarEffect(
  selectedPoint: { lat: number; lng: number } | null | undefined,
  radius: number,
  isActive: boolean,
) {
  const { map, isMapLoaded } = useMap();

  useEffect(() => {
    if (!map || !isMapLoaded || !selectedPoint || !isActive) return;

    const pulseLayerId1 = "admin-point-pulse-1";
    const pulseLayerId2 = "admin-point-pulse-2";
    const pulseLayerId3 = "admin-point-pulse-3";
    const pulseLayerIds = [pulseLayerId1, pulseLayerId2, pulseLayerId3];

    let animationFrame: number;

    const removeLayers = () => {
      if (!map.isStyleLoaded()) return;
      [pulseLayerId3, pulseLayerId2, pulseLayerId1].forEach((id) => {
        if (map.getLayer(id)) map.removeLayer(id);
        if (map.getSource(id + "-source")) map.removeSource(id + "-source");
      });
    };

    const startAnimation = () => {
      if (!map || !map.isStyleLoaded()) return;
      // Cancel any running animation and remove old layers/sources first
      if (animationFrame) cancelAnimationFrame(animationFrame);
      removeLayers();

      // Create circle using Turf.js
      const center = point([selectedPoint.lng, selectedPoint.lat]);
      const radiusInKm = radius / 1000;
      const options = { steps: 64, units: "kilometers" as const };
      const circlePolygon = circle(center, radiusInKm, options);

      // Add three radar pulse layers
      const colors = ["#06b6d4", "#a855f7", "#10b981"]; // Cyan, Purple, Green
      pulseLayerIds.forEach((id, idx) => {
        map.addSource(id + "-source", { type: "geojson", data: circlePolygon });
        map.addLayer({
          id,
          type: "line",
          source: id + "-source",
          paint: { "line-color": colors[idx], "line-width": 3, "line-opacity": 0 },
        });
      });

      // Animate radar pulses
      const startTime = Date.now();
      const pulseDuration = 2000;
      const pulseDelay = 666;

      const animate = () => {
        const elapsed = Date.now() - startTime;

        pulseLayerIds.forEach((id, idx) => {
          const progress = ((elapsed - pulseDelay * idx) % pulseDuration) / pulseDuration;
          const scale = Math.max(0, progress);
          const opacity = Math.max(0, 1 - progress);

          if (map.getLayer(id) && progress >= 0) {
            const pulseRadius = radius * (0.3 + scale * 0.7);
            const pulseCircle = circle(center, pulseRadius / 1000, options);
            const source = map.getSource(id + "-source") as any;
            if (source) source.setData(pulseCircle);
            map.setPaintProperty(id, "line-opacity", opacity * 0.6);
            map.setPaintProperty(id, "line-width", 3 - progress * 2);
          }
        });

        animationFrame = requestAnimationFrame(animate);
      };

      animate();
    };

    startAnimation();
    // Re-draw after a map style change (setStyle() removes all custom layers/sources)
    map.on("styledata", startAnimation);

    return () => {
      map.off("styledata", startAnimation);
      if (animationFrame) cancelAnimationFrame(animationFrame);
      removeLayers();
    };
  }, [map, isMapLoaded, selectedPoint, radius, isActive]);
}

/**
 * Point Analysis Tab Component
 */
const PointAnalysisTab = memo(function PointAnalysisTab({
  selectedPoint,
  onStartPointSelection,
  isPointSelectionMode,
  isActive,
  onHeaderActionsChange,
}: {
  selectedPoint?: { lat: number; lng: number } | null;
  onStartPointSelection?: () => void;
  isPointSelectionMode?: boolean;
  isActive: boolean;
  onHeaderActionsChange?: (actions: AnalyticsHeaderActions) => void;
}) {
  const [radius, setRadius] = useState(500);
  const sliderRef = useRef<HTMLInputElement>(null);
  const { map } = useMap();
  const { mutate, data: broadbandInfo, isPending, reset, abort } = usePointBroadbandInfo();
  const { results: geoResults } = useReverseGeoCoding({ lat: selectedPoint?.lat, lng: selectedPoint?.lng });

  // Draw radar effect on the map
  usePointRadarEffect(selectedPoint, radius, !!selectedPoint);

  // Cancel request handler
  const handleCancelRequest = useCallback(() => {
    abort();
    reset();
  }, [abort, reset]);

  const handleGenerateReport = useCallback(() => {
    if (!selectedPoint || !broadbandInfo?.data || !map) return;

    map.triggerRepaint();
    generateLocationReport({
      mapCanvas: map.getCanvas(),
      lat: selectedPoint.lat,
      lng: selectedPoint.lng,
      geocoding: geoResults as GeocodingResult | undefined,
      broadbandData: broadbandInfo.data,
      searchRadius: radius,
      reportTitle: "Broadband Point Analysis Report",
      analysisTitle: "Point Analysis",
    });
  }, [selectedPoint, broadbandInfo?.data, map, geoResults, radius]);

  // Fetch when point or radius changes
  useEffect(() => {
    if (selectedPoint) {
      mutate({
        pathParams: {},
        payload: { latitude: selectedPoint.lat, longitude: selectedPoint.lng, radiusMeters: radius },
      });
    }
  }, [selectedPoint, radius, mutate]);

  const isActionReady = !!selectedPoint && !!map && !isPending;
  const isReportReady = isActionReady && !!broadbandInfo?.data;

  useEffect(() => {
    if (!isActive) return;

    onHeaderActionsChange?.({ onGenerateReport: handleGenerateReport, isReportDisabled: !isReportReady });

    return () => {
      onHeaderActionsChange?.(DEFAULT_ANALYTICS_HEADER_ACTIONS);
    };
  }, [isActive, onHeaderActionsChange, handleGenerateReport, isReportReady]);

  const handleSliderRelease = useCallback(() => {
    const currentValue = sliderRef.current?.value;
    if (currentValue) {
      setRadius(Number(currentValue));
    }
  }, []);

  if (!selectedPoint) {
    return (
      <div className="flex flex-col items-center justify-center py-12 gap-4 text-center">
        <div className="w-16 h-16 rounded-full bg-blue-50 flex items-center justify-center">
          <MapPin className="h-8 w-8 text-blue-500" />
        </div>
        <div>
          <p className="text-sm font-medium text-gray-700">No Location Selected</p>
          <p className="text-xs text-gray-500 mt-1">Click the button below to select a point on the map</p>
        </div>
        <Button
          variant="default"
          size="sm"
          onClick={onStartPointSelection}
          disabled={isPointSelectionMode}
          className="mt-2">
          {isPointSelectionMode ? (
            <>
              <Loader2 className="h-4 w-4 animate-spin mr-2" />
              Click on map...
            </>
          ) : (
            <>
              <Target className="h-4 w-4 mr-2" />
              Select Point on Map
            </>
          )}
        </Button>
      </div>
    );
  }

  return (
    <div className="space-y-4">
      {/* Coordinates + Change Point */}
      <div className="flex items-center justify-between bg-gray-50 rounded-lg px-3 py-2">
        <div className="flex items-center gap-2 text-xs text-gray-500">
          <MapPin className="h-3 w-3" />
          <span>
            {selectedPoint.lat.toFixed(6)}, {selectedPoint.lng.toFixed(6)}
          </span>
        </div>
        <Button
          variant="ghost"
          size="sm"
          onClick={onStartPointSelection}
          disabled={isPointSelectionMode}
          className="h-7 text-xs">
          <Target className="h-3 w-3 mr-1" />
          Change
        </Button>
      </div>

      {/* Radius Control */}
      <div className="bg-white rounded-lg border border-gray-200 p-3">
        <div className="flex items-center justify-between mb-2">
          <div className="flex items-center gap-2">
            <Target className="h-4 w-4 text-blue-600" />
            <span className="text-sm font-semibold text-gray-700">Search Radius</span>
          </div>
          <span className="text-sm font-bold text-blue-600">{radius}m</span>
        </div>
        <input
          ref={sliderRef}
          type="range"
          min="100"
          max="5000"
          step="100"
          defaultValue={radius}
          onMouseUp={handleSliderRelease}
          onTouchEnd={handleSliderRelease}
          className="w-full h-2 bg-gray-200 rounded-lg appearance-none cursor-pointer accent-blue-600"
        />
        <div className="flex justify-between text-xs text-gray-500 mt-1">
          <span>100m</span>
          <span>5km</span>
        </div>
      </div>

      {/* Loading State with Cancel */}
      {isPending && (
        <div className="bg-blue-50 border border-blue-200 rounded-lg p-3 mb-4">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-2 text-blue-700">
              <Loader2 className="h-4 w-4 animate-spin" />
              <span className="text-sm font-medium">Analyzing point...</span>
            </div>
            <Button
              variant="ghost"
              size="sm"
              onClick={handleCancelRequest}
              className="h-7 text-xs text-red-600 hover:text-red-700 hover:bg-red-50">
              <X className="h-3 w-3 mr-1" />
              Cancel
            </Button>
          </div>
        </div>
      )}

      {/* Data Display */}
      {!isPending && (
        <BroadbandDataDisplay
          data={broadbandInfo?.data}
          isLoading={false}
          radiusInfo={`Showing data within ${radius}m radius`}
        />
      )}
    </div>
  );
});

/**
 * Hook to draw inward radar effect for area/envelope analysis
 */
function useAreaRadarEffect(
  bounds: { minLat: number; maxLat: number; minLng: number; maxLng: number } | null,
  isActive: boolean,
) {
  const { map, isMapLoaded } = useMap();

  useEffect(() => {
    if (!map || !isMapLoaded || !bounds || !isActive) return;

    const pulseLayerId1 = "admin-area-pulse-1";
    const pulseLayerId2 = "admin-area-pulse-2";
    const pulseLayerId3 = "admin-area-pulse-3";
    const borderLayerId = "admin-area-border";
    const pulseLayerIds = [pulseLayerId1, pulseLayerId2, pulseLayerId3];

    let animationFrame: number;

    const removeLayers = () => {
      if (!map.isStyleLoaded()) return;
      [borderLayerId, pulseLayerId3, pulseLayerId2, pulseLayerId1].forEach((id) => {
        if (map.getLayer(id)) map.removeLayer(id);
        if (map.getSource(id + "-source")) map.removeSource(id + "-source");
      });
    };

    const startAnimation = () => {
      if (!map || !map.isStyleLoaded()) return;
      // Cancel any running animation and remove old layers/sources first
      if (animationFrame) cancelAnimationFrame(animationFrame);
      removeLayers();

      // Create bbox polygon using Turf.js
      const bbox: [number, number, number, number] = [bounds.minLng, bounds.minLat, bounds.maxLng, bounds.maxLat];
      const envelope = bboxPolygon(bbox);

      // Add static border layer
      map.addSource(borderLayerId + "-source", { type: "geojson", data: envelope });
      map.addLayer({
        id: borderLayerId,
        type: "line",
        source: borderLayerId + "-source",
        paint: { "line-color": "#3b82f6", "line-width": 2, "line-dasharray": [4, 2], "line-opacity": 0.8 },
      });

      // Helper to create scaled bbox (inward scaling)
      const createScaledEnvelope = (scale: number) => {
        const centerLng = (bounds.minLng + bounds.maxLng) / 2;
        const centerLat = (bounds.minLat + bounds.maxLat) / 2;
        const halfWidth = (bounds.maxLng - bounds.minLng) / 2;
        const halfHeight = (bounds.maxLat - bounds.minLat) / 2;

        const scaledBbox: [number, number, number, number] = [
          centerLng - halfWidth * scale,
          centerLat - halfHeight * scale,
          centerLng + halfWidth * scale,
          centerLat + halfHeight * scale,
        ];
        return bboxPolygon(scaledBbox);
      };

      // Add three radar pulse layers for inward animation
      const colors = ["#06b6d4", "#a855f7", "#10b981"];
      pulseLayerIds.forEach((id, idx) => {
        map.addSource(id + "-source", { type: "geojson", data: envelope });
        map.addLayer({
          id,
          type: "line",
          source: id + "-source",
          paint: { "line-color": colors[idx], "line-width": 3, "line-opacity": 0 },
        });
      });

      // Animate inward radar pulses (from outside toward center)
      const startTime = Date.now();
      const pulseDuration = 2000;
      const pulseDelay = 666;

      const animate = () => {
        const elapsed = Date.now() - startTime;

        pulseLayerIds.forEach((id, idx) => {
          const rawProgress = ((elapsed - pulseDelay * idx) % pulseDuration) / pulseDuration;
          const progress = rawProgress < 0 ? rawProgress + 1 : rawProgress;
          // Inward scale: starts at 1.5 (outside) and shrinks toward 0.3 (inside the bounds)
          const scale = 1.5 - progress * 1.2;
          // Opacity: fade in then fade out
          const opacity = Math.max(0, 1 - Math.abs(progress - 0.5) * 2);

          if (map.getLayer(id) && scale > 0) {
            const scaledEnvelope = createScaledEnvelope(scale);
            const source = map.getSource(id + "-source") as any;
            if (source) source.setData(scaledEnvelope);
            map.setPaintProperty(id, "line-opacity", opacity * 0.6);
            map.setPaintProperty(id, "line-width", 3 - progress * 2);
          }
        });

        animationFrame = requestAnimationFrame(animate);
      };

      animate();
    };

    startAnimation();
    // Re-draw after a map style change (setStyle() removes all custom layers/sources)
    map.on("styledata", startAnimation);

    return () => {
      map.off("styledata", startAnimation);
      if (animationFrame) cancelAnimationFrame(animationFrame);
      removeLayers();
    };
  }, [map, isMapLoaded, bounds, isActive]);
}

/**
 * Area Analysis Tab Component
 */
const AreaAnalysisTab = memo(function AreaAnalysisTab({
  customEnvelope,
  onStartEnvelopeDrawing,
  isEnvelopeDrawingMode,
  onClearCustomEnvelope,
  isActive,
  onHeaderActionsChange,
}: {
  customEnvelope?: { minLat: number; maxLat: number; minLng: number; maxLng: number } | null;
  onStartEnvelopeDrawing?: () => void;
  isEnvelopeDrawingMode?: boolean;
  onClearCustomEnvelope?: () => void;
  isActive: boolean;
  onHeaderActionsChange?: (actions: AnalyticsHeaderActions) => void;
}) {
  const { map, isMapLoaded } = useMap();
  const { mutate, data: broadbandInfo, isPending, reset, abort } = useAreaBroadbandInfo();
  const [useViewport, setUseViewport] = useState(true);
  const [lastBounds, setLastBounds] = useState<{
    minLat: number;
    maxLat: number;
    minLng: number;
    maxLng: number;
  } | null>(null);

  // Draw radar effect on the map for the active bounds
  const activeBounds = customEnvelope || (useViewport ? lastBounds : null);
  const analysisCenter = activeBounds
    ? { lat: (activeBounds.minLat + activeBounds.maxLat) / 2, lng: (activeBounds.minLng + activeBounds.maxLng) / 2 }
    : null;
  const { results: areaGeoResults } = useReverseGeoCoding({ lat: analysisCenter?.lat, lng: analysisCenter?.lng });
  useAreaRadarEffect(activeBounds, !!activeBounds);

  const fetchAreaData = useCallback(
    (boundsOverride?: { minLat: number; maxLat: number; minLng: number; maxLng: number }) => {
      if (!map || !isMapLoaded) return;

      let boundsToUse = boundsOverride;

      if (!boundsToUse) {
        const bounds = map.getBounds();
        if (!bounds) return;

        const sw = bounds.getSouthWest();
        const ne = bounds.getNorthEast();

        boundsToUse = { minLat: sw.lat, maxLat: ne.lat, minLng: sw.lng, maxLng: ne.lng };
      }

      setLastBounds(boundsToUse);

      mutate({
        pathParams: {},
        payload: {
          minLatitude: boundsToUse.minLat,
          maxLatitude: boundsToUse.maxLat,
          minLongitude: boundsToUse.minLng,
          maxLongitude: boundsToUse.maxLng,
        },
      });
    },
    [map, isMapLoaded, mutate],
  );

  // Fetch when custom envelope changes (only for custom envelope, not viewport)
  useEffect(() => {
    if (customEnvelope) {
      setUseViewport(false);
      fetchAreaData(customEnvelope);
    }
  }, [customEnvelope, fetchAreaData]);

  // Handle viewport checkbox change - don't auto-fetch, require explicit click
  const handleViewportChange = useCallback(
    (checked: boolean) => {
      setUseViewport(checked);
      if (checked) {
        onClearCustomEnvelope?.();
        // Clear last bounds and reset mutation so user can see "Click Analyze" message
        setLastBounds(null);
        reset();
      }
    },
    [onClearCustomEnvelope, reset],
  );

  // Cancel request handler - resets mutation and clears UI state
  const handleCancelRequest = useCallback(() => {
    abort();
    reset();
    setLastBounds(null);
  }, [abort, reset]);

  // Clear custom envelope handler - resets everything
  const handleClearCustomEnvelope = useCallback(() => {
    onClearCustomEnvelope?.();
    setUseViewport(true);
    setLastBounds(null);
    reset();
  }, [onClearCustomEnvelope, reset]);

  const handleGenerateReport = useCallback(() => {
    if (!activeBounds || !analysisCenter || !broadbandInfo?.data || !map) return;

    map.triggerRepaint();
    generateLocationReport({
      mapCanvas: map.getCanvas(),
      lat: analysisCenter.lat,
      lng: analysisCenter.lng,
      geocoding: areaGeoResults as GeocodingResult | undefined,
      broadbandData: broadbandInfo.data,
      searchRadius: Math.round(broadbandInfo.data.envelopeInfo.radiusMeters),
      reportTitle: "Broadband Area Analysis Report",
      analysisTitle: "Area Analysis",
      bounds: activeBounds as AnalysisBounds,
    });
  }, [activeBounds, analysisCenter, broadbandInfo?.data, map, areaGeoResults]);

  const isActionReady = !!activeBounds && !!map && !isPending;
  const isReportReady = isActionReady && !!broadbandInfo?.data;

  useEffect(() => {
    if (!isActive) return;

    onHeaderActionsChange?.({ onGenerateReport: handleGenerateReport, isReportDisabled: !isReportReady });

    return () => {
      onHeaderActionsChange?.(DEFAULT_ANALYTICS_HEADER_ACTIONS);
    };
  }, [isActive, onHeaderActionsChange, handleGenerateReport, isReportReady]);

  return (
    <div className="space-y-4">
      {/* Analysis Mode Selection */}
      <div className="bg-white rounded-lg border border-gray-200 p-3 space-y-3">
        {/* Current Viewport Option */}
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-2">
            <Checkbox
              id="use-viewport"
              checked={useViewport && !customEnvelope}
              onCheckedChange={handleViewportChange}
              disabled={isPending}
            />
            <Label htmlFor="use-viewport" className="text-sm font-medium text-gray-700 cursor-pointer">
              Current viewport analysis
            </Label>
          </div>
          <Button
            variant="outline"
            size="sm"
            onClick={() => fetchAreaData()}
            disabled={isPending || !useViewport || !!customEnvelope}
            className="h-7 text-xs">
            <BarChart3 className="h-3 w-3 mr-1" />
            {lastBounds ? "Refresh" : "Analyze"}
          </Button>
        </div>

        {/* Custom Envelope Option */}
        <div className="flex items-center justify-between pt-2 border-t border-gray-100">
          <div className="flex items-center gap-2">
            <Checkbox
              id="use-custom"
              checked={!!customEnvelope}
              onCheckedChange={(checked) => {
                if (checked) {
                  setUseViewport(false);
                  onStartEnvelopeDrawing?.();
                } else {
                  onClearCustomEnvelope?.();
                  setUseViewport(true);
                }
              }}
              disabled={isPending || isEnvelopeDrawingMode}
            />
            <Label htmlFor="use-custom" className="text-sm font-medium text-gray-700 cursor-pointer">
              Custom area (draw on map)
            </Label>
          </div>
          {customEnvelope ? (
            <Button
              variant="ghost"
              size="sm"
              onClick={handleClearCustomEnvelope}
              className="h-7 text-xs text-red-600 hover:text-red-700 hover:bg-red-50">
              <RotateCcw className="h-3 w-3 mr-1" />
              Clear
            </Button>
          ) : (
            <Button
              variant="outline"
              size="sm"
              onClick={onStartEnvelopeDrawing}
              disabled={isPending || isEnvelopeDrawingMode}
              className="h-7 text-xs">
              {isEnvelopeDrawingMode ? (
                <>
                  <Loader2 className="h-3 w-3 animate-spin mr-1" />
                  Drawing...
                </>
              ) : (
                <>
                  <Pencil className="h-3 w-3 mr-1" />
                  Draw Area
                </>
              )}
            </Button>
          )}
        </div>
      </div>

      {/* Loading indicator for envelope drawing */}
      {isEnvelopeDrawingMode && (
        <div className="bg-blue-50 border border-blue-200 rounded-lg p-3 text-sm text-blue-700">
          <div className="flex items-center gap-2">
            <Pencil className="h-4 w-4" />
            <span>Click and drag on the map to draw a custom analysis area</span>
          </div>
        </div>
      )}

      {/* Bounds Info */}
      {(lastBounds || customEnvelope) && (
        <div className="bg-gray-50 rounded-lg p-2 text-xs text-gray-600 grid grid-cols-2 gap-1">
          <span>
            SW: {(customEnvelope?.minLat ?? lastBounds?.minLat ?? 0).toFixed(4)},{" "}
            {(customEnvelope?.minLng ?? lastBounds?.minLng ?? 0).toFixed(4)}
          </span>
          <span>
            NE: {(customEnvelope?.maxLat ?? lastBounds?.maxLat ?? 0).toFixed(4)},{" "}
            {(customEnvelope?.maxLng ?? lastBounds?.maxLng ?? 0).toFixed(4)}
          </span>
          {customEnvelope && (
            <span className="col-span-2 text-blue-600 font-medium flex items-center gap-1">
              <Check className="h-3 w-3" /> Custom area selected
            </span>
          )}
        </div>
      )}

      {/* Loading State with Cancel */}
      {isPending && (
        <div className="bg-blue-50 border border-blue-200 rounded-lg p-3">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-2 text-blue-700">
              <Loader2 className="h-4 w-4 animate-spin" />
              <span className="text-sm font-medium">Analyzing area...</span>
            </div>
            <Button
              variant="ghost"
              size="sm"
              onClick={handleCancelRequest}
              className="h-7 text-xs text-red-600 hover:text-red-700 hover:bg-red-50">
              <X className="h-3 w-3 mr-1" />
              Cancel
            </Button>
          </div>
          <p className="text-xs text-blue-600 mt-1">This may take a moment for large areas...</p>
        </div>
      )}

      {/* Prompt to analyze when viewport selected but no data */}
      {!isPending && useViewport && !customEnvelope && !lastBounds && !broadbandInfo?.data && (
        <div className="flex flex-col items-center justify-center py-8 gap-3 text-center bg-gray-50 rounded-lg border border-gray-200">
          <div className="w-12 h-12 rounded-full bg-blue-50 flex items-center justify-center">
            <Layers className="h-6 w-6 text-blue-500" />
          </div>
          <div>
            <p className="text-sm font-medium text-gray-700">Ready to Analyze</p>
            <p className="text-xs text-gray-500 mt-1">
              Click "Analyze" to fetch broadband data for the current viewport
            </p>
          </div>
          <Button variant="default" size="sm" onClick={() => fetchAreaData()} className="mt-2">
            <BarChart3 className="h-4 w-4 mr-2" />
            Analyze Viewport
          </Button>
        </div>
      )}

      {/* Data Display */}
      {!isPending && (lastBounds || broadbandInfo?.data) && (
        <BroadbandDataDisplay
          data={broadbandInfo?.data}
          isLoading={false}
          radiusInfo={
            activeBounds
              ? `Analyzing ${((activeBounds.maxLat - activeBounds.minLat) * 111).toFixed(1)}km × ${((activeBounds.maxLng - activeBounds.minLng) * 111 * Math.cos((activeBounds.minLat * Math.PI) / 180)).toFixed(1)}km area`
              : undefined
          }
        />
      )}
    </div>
  );
});

// Legacy placeholder to match old signature - not used
const _LegacyAreaAnalysisTabUnused = memo(function _LegacyAreaAnalysisTabUnused() {
  const { map, isMapLoaded } = useMap();
  const { mutate, data: broadbandInfo, isPending } = useAreaBroadbandInfo();
  const [lastBounds, setLastBounds] = useState<{
    minLat: number;
    maxLat: number;
    minLng: number;
    maxLng: number;
  } | null>(null);

  const fetchAreaData = useCallback(() => {
    if (!map || !isMapLoaded) return;

    const bounds = map.getBounds();
    if (!bounds) return;

    const sw = bounds.getSouthWest();
    const ne = bounds.getNorthEast();

    const newBounds = { minLatitude: sw.lat, maxLatitude: ne.lat, minLongitude: sw.lng, maxLongitude: ne.lng };

    setLastBounds({ minLat: sw.lat, maxLat: ne.lat, minLng: sw.lng, maxLng: ne.lng });

    mutate({ pathParams: {}, payload: newBounds });
  }, [map, isMapLoaded, mutate]);

  // Initial fetch
  useEffect(() => {
    if (isMapLoaded) {
      fetchAreaData();
    }
  }, [isMapLoaded, fetchAreaData]);

  return (
    <div className="space-y-4">
      {/* Refresh Button */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-2 text-xs text-gray-500">
          <Square className="h-3 w-3" />
          <span>Current viewport analysis</span>
        </div>
        <Button variant="outline" size="sm" onClick={fetchAreaData} disabled={isPending} className="h-8 text-xs">
          {isPending ? <Loader2 className="h-3 w-3 animate-spin mr-1" /> : <BarChart3 className="h-3 w-3 mr-1" />}
          Analyze Viewport
        </Button>
      </div>

      {/* Bounds Info */}
      {lastBounds && (
        <div className="bg-gray-50 rounded-lg p-2 text-xs text-gray-600 grid grid-cols-2 gap-1">
          <span>
            SW: {lastBounds.minLat.toFixed(4)}, {lastBounds.minLng.toFixed(4)}
          </span>
          <span>
            NE: {lastBounds.maxLat.toFixed(4)}, {lastBounds.maxLng.toFixed(4)}
          </span>
        </div>
      )}

      {/* Data Display */}
      <BroadbandDataDisplay
        data={broadbandInfo?.data}
        isLoading={isPending}
        radiusInfo={
          lastBounds
            ? `Analyzing ${((lastBounds.maxLat - lastBounds.minLat) * 111).toFixed(1)}km × ${((lastBounds.maxLng - lastBounds.minLng) * 111 * Math.cos((lastBounds.minLat * Math.PI) / 180)).toFixed(1)}km area`
            : undefined
        }
      />
    </div>
  );
});

/**
 * Main Admin Broadband Panel Component
 */
export const AdminBroadbandPanel = memo(function AdminBroadbandPanel({
  isOpen,
  onClose,
  selectedPoint,
  onStartPointSelection,
  isPointSelectionMode,
  customEnvelope,
  onStartEnvelopeDrawing,
  isEnvelopeDrawingMode,
  onClearCustomEnvelope,
}: AdminBroadbandPanelProps) {
  const [activeTab, setActiveTab] = useState<"point" | "area">("area");
  const [headerActions, setHeaderActions] = useState<AnalyticsHeaderActions>(DEFAULT_ANALYTICS_HEADER_ACTIONS);

  useEffect(() => {
    setHeaderActions(DEFAULT_ANALYTICS_HEADER_ACTIONS);
  }, [activeTab]);

  // Switch to point tab when a point is selected
  useEffect(() => {
    if (selectedPoint) {
      setActiveTab("point");
    }
  }, [selectedPoint]);

  return (
    <AnimatePresence>
      {isOpen && (
        <>
          {/* Backdrop for mobile */}
          <motion.div
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            exit={{ opacity: 0 }}
            className="fixed inset-0 bg-black/20 z-40 lg:hidden"
            onClick={onClose}
          />

          <motion.div
            initial={{ x: "100%" }}
            animate={{ x: 0 }}
            exit={{ x: "100%" }}
            transition={{ type: "spring", damping: 30, stiffness: 300 }}
            className="fixed right-0 z-50"
            style={{
              top: `${NAVBAR_HEIGHT_PX}px`,
              height: `calc(100vh - ${NAVBAR_HEIGHT_PX}px)`,
              width: `min(100vw, ${ANALYTICS_PANEL_WIDTH_PX}px)`,
            }}>
            <GlassPanel className="h-full flex flex-col overflow-hidden rounded-none text-sm">
              {/* Header */}
              <div className="flex items-center justify-between px-3 py-2.5 border-b border-gray-200/50">
                <div className="flex items-center gap-2">
                  <BarChart3 className="h-4 w-4 text-primary" />
                  <h2 className="text-base font-semibold text-gray-800">Broadband Analytics</h2>
                </div>
                <div className="flex items-center gap-1">
                  <AnalyticsActionBar
                    onGenerateReport={headerActions.onGenerateReport}
                    isReportDisabled={headerActions.isReportDisabled}
                  />
                  <Button variant="ghost" size="icon" onClick={onClose} className="h-8 w-8">
                    <X className="h-5 w-5 text-gray-500" />
                  </Button>
                </div>
              </div>

              {/* Tabs */}
              <Tabs
                value={activeTab}
                onValueChange={(v) => setActiveTab(v as "point" | "area")}
                className="flex-1 flex flex-col overflow-hidden">
                <TabsList className="mx-3 mt-2.5 grid grid-cols-2">
                  <TabsTrigger value="area" className="text-xs">
                    <Layers className="h-4 w-4 mr-2" />
                    Area Analysis
                  </TabsTrigger>
                  <TabsTrigger value="point" className="text-xs">
                    <MapPin className="h-4 w-4 mr-2" />
                    Point Analysis
                  </TabsTrigger>
                </TabsList>

                <ScrollArea className="flex-1 px-3 py-3">
                  <TabsContent value="area" className="mt-0">
                    <AreaAnalysisTab
                      customEnvelope={customEnvelope}
                      onStartEnvelopeDrawing={onStartEnvelopeDrawing}
                      isEnvelopeDrawingMode={isEnvelopeDrawingMode}
                      onClearCustomEnvelope={onClearCustomEnvelope}
                      isActive={activeTab === "area"}
                      onHeaderActionsChange={setHeaderActions}
                    />
                  </TabsContent>
                  <TabsContent value="point" className="mt-0">
                    <PointAnalysisTab
                      selectedPoint={selectedPoint}
                      onStartPointSelection={onStartPointSelection}
                      isPointSelectionMode={isPointSelectionMode}
                      isActive={activeTab === "point"}
                      onHeaderActionsChange={setHeaderActions}
                    />
                  </TabsContent>
                </ScrollArea>
              </Tabs>
            </GlassPanel>
          </motion.div>
        </>
      )}
    </AnimatePresence>
  );
});

export default AdminBroadbandPanel;
