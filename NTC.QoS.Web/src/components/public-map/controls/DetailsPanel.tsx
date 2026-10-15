"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import { useUser } from "@clerk/nextjs";
import type { MapMouseEvent } from "@maptiler/sdk";
import circle from "@turf/circle";
import { point } from "@turf/helpers";
import { AnimatePresence, motion } from "framer-motion";
import { Antenna, BarChart3, Building2, FileDown, Lock, Loader2, MapPin, Network, Target, Wifi, X } from "lucide-react";

import { getPointBroadbandInfoRequest } from "@/components/features/analytics/handlers/getPointBroadbandInfoRequest";
import type { BroadbandInfoData } from "@/components/features/analytics/types/BroadbandInfo";
import { getAuthUserRequest } from "@/components/features/users/handlers/getAuthUserRequest";
import { createMutationHookFromTemplate } from "@/hooks/createMutationHookFromTemplate";
import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";
import { useMap } from "@/hooks/useMap";
import useReverseGeoCoding from "@/hooks/useReverseGeoCoding";
import { generateLocationReport, parseFiberFeatureProperties, type GeocodingResult } from "@/utils/map-report";
import { GlassPanel } from "./GlassPanel";

const usePointBroadbandInfo = createMutationHookFromTemplate(getPointBroadbandInfoRequest);
const useAuthUser = createQueryHookFromTemplate(getAuthUserRequest);

type GeoResults = ReturnType<typeof useReverseGeoCoding>["results"];

interface DetailsPanelProps {
  isOpen: boolean;
  onClose: () => void;
  lat: number;
  lng: number;
  selectedFiberFeature?: Record<string, unknown> | null;
}

interface FiberFeatureDetailsProps {
  feature: Record<string, unknown>;
}

interface ServiceAvailabilityProps {
  lat: number;
  lng: number;
  radius: number;
  onRadiusChange: (radius: number) => void;
  onDataChange?: (data: BroadbandInfoData | null) => void;
}

function useLocationAnalyticsAccess() {
  const { isLoaded, isSignedIn, user } = useUser();
  const { data: authUser, isLoading: isUserLoading } = useAuthUser({
    pathParams: { id: user?.id ?? "" },
    queryParams: {},
  });

  const isLoading = !isLoaded || isUserLoading;
  const hasRole = !!authUser?.data?.role?.key;

  return {
    isLoading,
    isSignedIn: !!isSignedIn,
    canViewAnalytics: !!isSignedIn && hasRole,
  };
}

function usePointRadarEffect(lat: number, lng: number, radius: number, isActive: boolean) {
  const { map, isMapLoaded } = useMap();

  useEffect(() => {
    if (!map || !isMapLoaded || !isActive) return;

    const ids = ["public-point-pulse-1", "public-point-pulse-2", "public-point-pulse-3"];
    ids.forEach((id) => {
      if (map.getLayer(id)) map.removeLayer(id);
      if (map.getSource(`${id}-source`)) map.removeSource(`${id}-source`);
    });

    const center = point([lng, lat]);
    const options = { steps: 64, units: "kilometers" as const };
    const polygon = circle(center, radius / 1000, options);
    const colors = ["#06b6d4", "#a855f7", "#10b981"];

    ids.forEach((id, idx) => {
      map.addSource(`${id}-source`, { type: "geojson", data: polygon });
      map.addLayer({
        id,
        type: "line",
        source: `${id}-source`,
        paint: { "line-color": colors[idx], "line-width": 3, "line-opacity": 0 },
      });
    });

    let animationFrame = 0;
    const startTime = Date.now();

    const animate = () => {
      const elapsed = Date.now() - startTime;
      ids.forEach((id, idx) => {
        const progress = ((elapsed - idx * 666) % 2000) / 2000;
        const opacity = Math.max(0, 1 - progress);
        if (!map.getLayer(id)) return;

        const pulseRadius = radius * (0.3 + progress * 0.7);
        const source = map.getSource(`${id}-source`) as { setData: (d: unknown) => void } | undefined;
        if (source) source.setData(circle(center, pulseRadius / 1000, options));
        map.setPaintProperty(id, "line-opacity", opacity * 0.6);
        map.setPaintProperty(id, "line-width", 3 - progress * 2);
      });
      animationFrame = requestAnimationFrame(animate);
    };

    animate();

    return () => {
      if (animationFrame) cancelAnimationFrame(animationFrame);
      ids.forEach((id) => {
        if (map.getLayer(id)) map.removeLayer(id);
        if (map.getSource(`${id}-source`)) map.removeSource(`${id}-source`);
      });
    };
  }, [map, isMapLoaded, lat, lng, radius, isActive]);
}

function RadiusControl({ radius, sliderRef, handleSliderChange, handleSliderRelease }: {
  radius: number;
  sliderRef: React.RefObject<HTMLInputElement | null>;
  handleSliderChange: (value: number) => void;
  handleSliderRelease: () => void;
}) {
  return (
    <div className="bg-white rounded-lg border border-gray-200 p-3 mb-3">
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
        onChange={(e) => handleSliderChange(Number(e.target.value))}
        onMouseUp={handleSliderRelease}
        onTouchEnd={handleSliderRelease}
        className="w-full h-2 bg-gray-200 rounded-lg appearance-none cursor-pointer accent-blue-600"
      />
    </div>
  );
}

function ServiceAvailability({ lat, lng, radius, onRadiusChange, onDataChange }: ServiceAvailabilityProps) {
  const { mutate, data: broadbandInfo, isPending, error } = usePointBroadbandInfo();
  const sliderRef = useRef<HTMLInputElement>(null);

  usePointRadarEffect(lat, lng, radius, true);

  useEffect(() => {
    if (broadbandInfo?.data && broadbandInfo.succeeded) onDataChange?.(broadbandInfo.data);
    if (!isPending && !broadbandInfo?.data) onDataChange?.(null);
  }, [broadbandInfo, isPending, onDataChange]);

  useEffect(() => {
    mutate({ pathParams: {}, payload: { latitude: lat, longitude: lng, radiusMeters: radius } });
  }, [lat, lng, mutate]);

  const handleSliderChange = (newRadius: number) => {
    if (sliderRef.current) sliderRef.current.value = newRadius.toString();
  };

  const handleSliderRelease = () => {
    const value = Number(sliderRef.current?.value ?? radius);
    mutate({ pathParams: {}, payload: { latitude: lat, longitude: lng, radiusMeters: value } });
    onRadiusChange(value);
  };

  if (isPending) {
    return (
      <>
        <RadiusControl radius={radius} sliderRef={sliderRef} handleSliderChange={handleSliderChange} handleSliderRelease={handleSliderRelease} />
        <div className="flex items-center gap-2 text-gray-500">
          <Loader2 className="h-4 w-4 animate-spin" />
          <span className="text-sm">Analyzing broadband coverage...</span>
        </div>
      </>
    );
  }

  if (error || !broadbandInfo?.data || !broadbandInfo.succeeded) {
    return (
      <>
        <RadiusControl radius={radius} sliderRef={sliderRef} handleSliderChange={handleSliderChange} handleSliderRelease={handleSliderRelease} />
        <div className="text-sm text-gray-500 bg-gray-50 rounded-lg p-3">No broadband data available for this location.</div>
      </>
    );
  }

  const { summary } = broadbandInfo.data;
  const { serviceProviders, municipalities } = broadbandInfo.data;

  const topMunicipalities = municipalities
    .filter((m) => m.totalAssets > 0)
    .sort((a, b) => b.totalAssets - a.totalAssets)
    .slice(0, 8);

  return (
    <div className="space-y-3">
      <RadiusControl radius={radius} sliderRef={sliderRef} handleSliderChange={handleSliderChange} handleSliderRelease={handleSliderRelease} />
      <div className="grid grid-cols-2 gap-3">
        <div className="bg-blue-50 rounded-xl p-3 border border-blue-200 min-h-[76px]">
          <div className="flex items-center gap-1.5 mb-1 text-blue-700 leading-none">
            <Antenna className="h-3.5 w-3.5" />
            <span className="text-xs font-medium tracking-tight">Cell Sites</span>
          </div>
          <p className="text-[2rem] leading-none font-bold text-blue-900">{summary.totalCells}</p>
        </div>
        <div className="bg-green-50 rounded-xl p-3 border border-green-200 min-h-[76px]">
          <div className="flex items-center gap-1.5 mb-1 text-green-700 leading-none">
            <Network className="h-3.5 w-3.5" />
            <span className="text-xs font-medium tracking-tight">NAPs</span>
          </div>
          <p className="text-[2rem] leading-none font-bold text-green-900">{summary.totalNaps}</p>
        </div>
        <div className="bg-purple-50 rounded-xl p-3 border border-purple-200 min-h-[76px]">
          <div className="flex items-center gap-1.5 mb-1 text-purple-700 leading-none">
            <Building2 className="h-3.5 w-3.5" />
            <span className="text-xs font-medium tracking-tight">Providers</span>
          </div>
          <p className="text-[2rem] leading-none font-bold text-purple-900">{summary.serviceProviderCount}</p>
        </div>
        <div className="bg-orange-50 rounded-xl p-3 border border-orange-200 min-h-[76px]">
          <div className="flex items-center gap-1.5 mb-1 text-orange-700 leading-none">
            <Wifi className="h-3.5 w-3.5" />
            <span className="text-xs font-medium tracking-tight">Total Assets</span>
          </div>
          <p className="text-[2rem] leading-none font-bold text-orange-900">{summary.totalAssets}</p>
        </div>
      </div>

      <div className="bg-gray-50 rounded-xl p-3 border border-gray-200 space-y-2">
        <div className="flex items-center gap-2">
          <MapPin className="h-4 w-4 text-gray-600" />
          <p className="text-xs font-semibold text-gray-600 uppercase tracking-wide">Coverage Area</p>
        </div>
        <div className="grid grid-cols-2 gap-x-4 gap-y-1 text-sm">
          <div>
            <p className="text-gray-500 leading-tight">Area Size:</p>
            <p className="font-semibold text-gray-800">{summary.coverageAreaKm2.toFixed(2)} km²</p>
          </div>
          <div>
            <p className="text-gray-500 leading-tight">Density:</p>
            <p className="font-semibold text-gray-800">{summary.assetDensityPerKm2.toFixed(1)} assets/km²</p>
          </div>
          <div>
            <p className="text-gray-500 leading-tight">Municipalities:</p>
            <p className="font-semibold text-gray-800">{summary.municipalityCount}</p>
          </div>
          <div>
            <p className="text-gray-500 leading-tight">With Assets:</p>
            <p className="font-semibold text-gray-800">{summary.municipalitiesWithAssets}</p>
          </div>
        </div>
      </div>

      {serviceProviders.length > 0 && (
        <div className="space-y-2">
          <p className="text-xs font-semibold text-gray-500 uppercase tracking-wide">Service Providers</p>
          <div className="space-y-2 max-h-64 overflow-y-auto pr-1">
            {serviceProviders
              .sort((a, b) => b.totalAssets - a.totalAssets)
              .map((provider) => (
                <div key={provider.id} className="bg-white rounded-xl border border-gray-200 p-3">
                  <div className="flex items-center justify-between gap-2 mb-2">
                    <div className="flex items-center gap-2 min-w-0">
                      <span
                        className="h-2.5 w-2.5 rounded-full shrink-0"
                        style={{ backgroundColor: provider.color || "#6b7280" }}
                      />
                      <p className="text-xs font-semibold text-gray-800 truncate">{provider.name}</p>
                    </div>
                    <span className="text-[10px] px-2 py-0.5 rounded-full bg-blue-50 text-blue-700 border border-blue-100">
                      {provider.serviceType}
                    </span>
                  </div>
                  <div className="grid grid-cols-3 gap-2 text-[11px] text-gray-600">
                    <div>Cells: <span className="font-semibold text-gray-800">{provider.cellCount}</span></div>
                    <div>NAPs: <span className="font-semibold text-gray-800">{provider.napCount}</span></div>
                    <div>Assets: <span className="font-semibold text-gray-800">{provider.totalAssets}</span></div>
                  </div>
                </div>
              ))}
          </div>
        </div>
      )}

      {topMunicipalities.length > 0 && (
        <div className="space-y-2">
          <p className="text-xs font-semibold text-gray-500 uppercase tracking-wide">Municipalities</p>
          <div className="bg-white rounded-xl border border-gray-200 p-3 space-y-2 max-h-48 overflow-y-auto">
            {topMunicipalities.map((m) => (
              <div key={m.id} className="flex items-center justify-between text-xs">
                <span className="text-gray-700 truncate pr-2">{m.displayName}</span>
                <span className="font-semibold text-gray-900 shrink-0">{m.totalAssets} assets</span>
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  );
}

function LocationAnalyticsSection({ lat, lng, radius, onRadiusChange, onDataChange }: {
  lat: number;
  lng: number;
  radius: number;
  onRadiusChange: (radius: number) => void;
  onDataChange?: (data: BroadbandInfoData | null) => void;
}) {
  const { isLoading, isSignedIn, canViewAnalytics } = useLocationAnalyticsAccess();

  if (isLoading) {
    return <div className="text-sm text-gray-400">Checking permissions...</div>;
  }

  if (!isSignedIn || !canViewAnalytics) {
    return (
      <div className="rounded-xl border border-dashed border-gray-200 bg-gray-50/80 p-4 space-y-2">
        <div className="flex items-center gap-2 text-gray-500">
          <Lock className="h-4 w-4" />
          <span className="text-sm font-semibold">Analytics Unavailable</span>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-3">
      <div className="flex items-center gap-2">
        <BarChart3 className="h-4 w-4 text-primary" />
        <p className="text-xs font-semibold text-gray-500 uppercase tracking-wide">Broadband Analytics</p>
      </div>
      <ServiceAvailability lat={lat} lng={lng} radius={radius} onRadiusChange={onRadiusChange} onDataChange={onDataChange} />
    </div>
  );
}

function LocationDetails({ results, isLoading }: { results: GeoResults; isLoading: boolean }) {
  if (isLoading) {
    return (
      <div className="flex items-center gap-2 text-gray-500">
        <Loader2 className="h-4 w-4 animate-spin" />
        <span className="text-sm">Loading location...</span>
      </div>
    );
  }

  if (!results) return null;

  return (
    <div className="space-y-1">
      {results.address && <p className="text-base font-semibold text-gray-800">{results.address}</p>}
      {(results.place ?? results.municipality) && <p className="text-sm text-gray-600">{results.place} {results.municipality}</p>}
      {(results.region ?? results.postal_code) && <p className="text-xs text-gray-500">{results.region} {results.postal_code}</p>}
    </div>
  );
}

function FiberFeatureDetails({ feature }: FiberFeatureDetailsProps) {
  const fiber = parseFiberFeatureProperties(feature);

  const knownKeys = new Set([
    "id",
    "region",
    "Region",
    "province",
    "Province",
    "municipality",
    "Municipality",
    "barangay",
    "Barangay",
    "zipcode",
    "Zipcode",
    "providers",
    "Providers",
    "provider",
    "Provider",
  ]);

  const additionalProperties = Object.entries(feature)
    .filter(([key, value]) => !knownKeys.has(key) && value !== null && value !== undefined && String(value).trim() !== "")
    .slice(0, 8);

  return (
    <div className="space-y-3">
      <div className="flex items-center gap-2">
        <Network className="h-4 w-4 text-primary" />
        <p className="text-xs font-semibold text-gray-500 uppercase tracking-wide">Fiber Coverage Details</p>
      </div>

      <div className="rounded-xl border border-gray-200 bg-gray-50/70 p-3 space-y-2">
        {fiber.region && <div className="flex justify-between gap-3 text-sm"><span className="text-gray-500">Region</span><span className="text-gray-800 font-medium text-right">{fiber.region}</span></div>}
        {fiber.province && <div className="flex justify-between gap-3 text-sm"><span className="text-gray-500">Province</span><span className="text-gray-800 font-medium text-right">{fiber.province}</span></div>}
        {fiber.municipality && <div className="flex justify-between gap-3 text-sm"><span className="text-gray-500">Municipality</span><span className="text-gray-800 font-medium text-right">{fiber.municipality}</span></div>}
        {fiber.barangay && <div className="flex justify-between gap-3 text-sm"><span className="text-gray-500">Barangay</span><span className="text-gray-800 font-medium text-right">{fiber.barangay}</span></div>}
        {fiber.zipcode && <div className="flex justify-between gap-3 text-sm"><span className="text-gray-500">Postal</span><span className="text-gray-800 font-medium text-right">{fiber.zipcode}</span></div>}
      </div>

      {fiber.providers.length > 0 && (
        <div className="rounded-xl border border-blue-100 bg-blue-50/50 p-3">
          <p className="text-xs font-semibold text-gray-600 uppercase tracking-wide mb-2">
            Service Providers ({fiber.providers.length})
          </p>
          <div className="flex flex-wrap gap-1.5">
            {fiber.providers.map((provider) => (
              <span key={provider} className="px-2 py-1 rounded-full text-xs font-medium bg-white border border-blue-200 text-blue-700">
                {provider}
              </span>
            ))}
          </div>
        </div>
      )}

      {additionalProperties.length > 0 && (
        <div className="rounded-xl border border-gray-200 bg-white p-3">
          <p className="text-xs font-semibold text-gray-500 uppercase tracking-wide mb-2">Additional Properties</p>
          <div className="space-y-2">
            {additionalProperties.map(([key, value]) => (
              <div key={key} className="flex justify-between gap-3 text-xs">
                <span className="text-gray-500 uppercase">{key}</span>
                <span className="text-gray-800 font-medium text-right break-all">
                  {typeof value === "object" ? JSON.stringify(value) : String(value)}
                </span>
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  );
}

export function DetailsPanel({ isOpen, onClose, lat, lng, selectedFiberFeature }: DetailsPanelProps) {
  const [radius, setRadius] = useState(1000);
  const [broadbandData, setBroadbandData] = useState<BroadbandInfoData | null>(null);
  const { map } = useMap();
  const { results: geoResults, isLoading: geoLoading } = useReverseGeoCoding({ lat, lng });

  const handleGenerateReport = useCallback(() => {
    if (!map) return;
    map.triggerRepaint();
    generateLocationReport({
      mapCanvas: map.getCanvas(),
      lat,
      lng,
      geocoding: geoResults as GeocodingResult | undefined,
      broadbandData: broadbandData ?? undefined,
      searchRadius: radius,
      fiberCoverage: selectedFiberFeature ? parseFiberFeatureProperties(selectedFiberFeature) : null,
    });
  }, [map, lat, lng, geoResults, broadbandData, radius, selectedFiberFeature]);

  useEffect(() => {
    const onKeyDown = (e: KeyboardEvent) => {
      if (e.key === "Escape" && isOpen) onClose();
    };
    document.addEventListener("keydown", onKeyDown);
    return () => document.removeEventListener("keydown", onKeyDown);
  }, [isOpen, onClose]);

  return (
    <AnimatePresence>
      {isOpen && (
        <>
          <motion.div initial={{ opacity: 0 }} animate={{ opacity: 1 }} exit={{ opacity: 0 }} className="fixed inset-0 bg-black/20 z-40 md:hidden" onClick={onClose} />
          <motion.div initial={{ x: "100%" }} animate={{ x: 0 }} exit={{ x: "100%" }} transition={{ type: "spring", damping: 30, stiffness: 300 }} className="fixed right-0 top-0 bottom-0 w-full max-w-sm z-50">
            <GlassPanel className="h-full flex flex-col overflow-hidden rounded-none md:rounded-l-2xl">
              <div className="flex items-center justify-between px-4 py-3 border-b border-gray-200/50">
                <div className="flex items-center gap-2">
                  <MapPin className="h-5 w-5 text-primary" />
                  <h2 className="text-lg font-semibold text-gray-800">Location Details</h2>
                </div>
                <div className="flex items-center gap-1">
                  <button onClick={handleGenerateReport} disabled={!map} className="p-2 hover:bg-blue-50 rounded-full transition-colors disabled:opacity-40 disabled:cursor-not-allowed" aria-label="Download location report" title="Generate &amp; download location report">
                    <FileDown className="h-5 w-5 text-blue-600" />
                  </button>
                  <button onClick={onClose} className="p-2 hover:bg-gray-100 rounded-full transition-colors" aria-label="Close panel">
                    <X className="h-5 w-5 text-gray-500" />
                  </button>
                </div>
              </div>

              <div className="flex-1 overflow-y-auto p-4 space-y-6">
                <div className="flex items-center gap-2 text-xs text-gray-500 bg-gray-50 rounded-lg px-3 py-2">
                  <MapPin className="h-3 w-3" />
                  <span>{lat.toFixed(6)}, {lng.toFixed(6)}</span>
                </div>

                <div className="space-y-2">
                  <p className="text-xs font-semibold text-gray-500 uppercase tracking-wide">Address</p>
                  <LocationDetails results={geoResults} isLoading={geoLoading} />
                </div>

                {selectedFiberFeature && <FiberFeatureDetails feature={selectedFiberFeature} />}

                <LocationAnalyticsSection lat={lat} lng={lng} radius={radius} onRadiusChange={setRadius} onDataChange={setBroadbandData} />
              </div>
            </GlassPanel>
          </motion.div>
        </>
      )}
    </AnimatePresence>
  );
}

export function useDetailsPanel() {
  const { map, isMapLoaded } = useMap();
  const [isOpen, setIsOpen] = useState(false);
  const [coords, setCoords] = useState({ lat: 0, lng: 0 });
  const [isPointSelectionMode, setIsPointSelectionMode] = useState(false);

  useEffect(() => {
    if (!map || !isMapLoaded) return;

    const handleClick = (e: MapMouseEvent) => {
      if (!isPointSelectionMode) return;
      setCoords({ lat: e.lngLat.lat, lng: e.lngLat.lng });
      setIsOpen(true);
      setIsPointSelectionMode(false);
    };

    map.on("click", handleClick);
    return () => {
      map.off("click", handleClick);
    };
  }, [map, isMapLoaded, isPointSelectionMode]);

  useEffect(() => {
    if (!map) return;
    const canvas = map.getCanvas();
    const container = map.getContainer();
    const cursor = isPointSelectionMode ? "crosshair" : "";
    canvas.style.cursor = cursor;
    container.style.cursor = cursor;
    return () => {
      canvas.style.cursor = "";
      container.style.cursor = "";
    };
  }, [map, isPointSelectionMode]);

  return {
    isOpen,
    onClose: () => setIsOpen(false),
    lat: coords.lat,
    lng: coords.lng,
    isPointSelectionMode,
    startPointSelection: () => setIsPointSelectionMode(true),
    cancelPointSelection: () => setIsPointSelectionMode(false),
  };
}
