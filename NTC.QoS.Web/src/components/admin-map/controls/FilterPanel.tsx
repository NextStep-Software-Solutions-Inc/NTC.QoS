/**
 * Filter Panel Component
 *
 * Redesigned sidebar panel with functional filters, search, and glassmorphic styling.
 * Replaces the old PanelMenu with a modern, feature-rich interface.
 *
 * Features:
 * - Dynamic provider filtering from API
 * - Layer toggles synced with URL params
 * - Signal strength and speed test filters
 */

"use client";

import { memo, useCallback, useEffect, useMemo, useState } from "react";
import { motion } from "framer-motion";
import { format } from "date-fns";
import {
  Activity,
  Building2,
  Cable,
  Calendar,
  Check,
  ChevronLeft,
  ChevronRight,
  Filter,
  Loader2,
  Radio,
  RotateCcw,
  Signal,
  Wifi,
} from "lucide-react";

import { getGenerationsRequest } from "@/components/features/generation/handlers/getGenerationsRequest";
import { getServiceProvidersRequest } from "@/components/features/service-provider/handlers/getServiceProvidersRequest";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { ScrollArea } from "@/components/ui/scroll-area";
import { Slider } from "@/components/ui/slider";
import { Switch } from "@/components/ui/switch";
import { Tabs, TabsList, TabsTrigger } from "@/components/ui/tabs";
// Import API hooks for providers
import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";
import { cn } from "@/lib/utils";
import { useAdminMapUI, useFilterState, useLayerState } from "../context/AdminMapContext";
import {
  FILTER_PANEL_HANDLE_HEIGHT_PX,
  FILTER_PANEL_HANDLE_OFFSET_PX,
  FILTER_PANEL_HANDLE_WIDTH_PX,
  FILTER_PANEL_WIDTH_PX,
  NAVBAR_HEIGHT_PX,
} from "../layout-constants";
import type { AdminMapFilters, ProviderOption } from "../types";
import { DEFAULT_ADMIN_MAP_FILTERS } from "../types";
import { CollapsibleSection } from "../ui/CollapsibleSection";
import { GlassPanel } from "../ui/GlassPanel";
import { SearchBar } from "./SearchBar";

const useGetWirelessServiceProviders = createQueryHookFromTemplate(getServiceProvidersRequest);
const useGetWiredServiceProviders = createQueryHookFromTemplate(getServiceProvidersRequest);
const useGetGenerations = createQueryHookFromTemplate(getGenerationsRequest);

const DOWNLOAD_SPEED_RANGE: [number, number] = [0, 1000];
const UPLOAD_SPEED_RANGE: [number, number] = [0, 500];

const toDateInputValue = (date?: Date) => {
  if (!date) return "";
  return format(date, "yyyy-MM-dd");
};

const fromDateInputValue = (value: string) => {
  if (!value) return undefined;
  return new Date(`${value}T00:00:00`);
};

const SIGNAL_QUALITY_LABELS = [
  { value: -110, label: "Very Weak" },
  { value: -90, label: "Weak" },
  { value: -70, label: "Fair" },
  { value: -50, label: "Good" },
  { value: -40, label: "Excellent" },
];

interface FilterPanelProps {
  className?: string;
}

export const FilterPanel = memo(function FilterPanel({ className }: FilterPanelProps) {
  const { isPanelCollapsed, togglePanelCollapsed } = useAdminMapUI();
  const { filters, updateFilters, resetFilters } = useFilterState();
  const { layerState, setLayerVisibility } = useLayerState();
  const [activeTab, setActiveTab] = useState("cellular");
  const filterPanelTopOffsetPx = NAVBAR_HEIGHT_PX;
  const filterPanelHeightCss = `calc(100vh - ${filterPanelTopOffsetPx}px)`;

  // Pending (staged) filters — only committed to context when "Apply Filters" is clicked
  const [pendingFilters, setPendingFilters] = useState<AdminMapFilters>({ ...filters });
  const panelCollapseDistance = FILTER_PANEL_WIDTH_PX;

  // Sync pending filters when applied filters change externally (e.g. after reset from context)
  useEffect(() => {
    setPendingFilters({ ...filters });
  }, [filters]);

  // Helper to update pending filters partially (mirrors updateFilters API)
  const updatePending = useCallback((partial: Partial<AdminMapFilters>) => {
    setPendingFilters((prev) => ({ ...prev, ...partial }));
  }, []);

  // True when staged filters differ from the currently applied filters
  const hasPendingChanges = useMemo(
    () => JSON.stringify(pendingFilters) !== JSON.stringify(filters),
    [pendingFilters, filters],
  );

  // Fetch providers from API

  // Fetch wireless service providers for speedtest filtering
  const { data: wirelessServiceProvidersData, isLoading: isLoadingWirelessServiceProviders } =
    useGetWirelessServiceProviders({
      pathParams: {},
      queryParams: { pageSize: 1000, serviceTypes: ["Wireless"] },
    });

  // Fetch wired service providers for NAP/fiber filtering
  const { data: wiredServiceProvidersData, isLoading: isLoadingWiredServiceProviders } = useGetWiredServiceProviders({
    pathParams: {},
    queryParams: { pageSize: 1000, serviceTypes: ["Wired"] },
  });

  // Fetch generations (network types) for cellular filtering
  const { data: generationsData, isLoading: isLoadingGenerations } = useGetGenerations({
    pathParams: {},
    queryParams: { pageSize: 100 },
  });

  const generations = useMemo(() => generationsData?.data ?? [], [generationsData]);

  // Transform wireless service providers to options (for cellular & speedtest tabs)
  const wirelessServiceProviders = useMemo<Array<{ id: string; name: string; color?: string }>>(() => {
    if (!wirelessServiceProvidersData?.data) return [];
    return wirelessServiceProvidersData.data.map((sp) => {
      const rawName = sp.provider?.name || "Unknown";
      return { id: sp.id, name: rawName.toLowerCase() === "others" ? "Unknown" : rawName, color: sp.provider?.color };
    });
  }, [wirelessServiceProvidersData]);

  // Transform wired service providers to options (for fiber/NAP tab)
  const wiredServiceProviders = useMemo<Array<{ id: string; name: string; color?: string }>>(() => {
    if (!wiredServiceProvidersData?.data) return [];
    return wiredServiceProvidersData.data.map((sp) => {
      const rawName = sp.provider?.name || "Unknown";
      return { id: sp.id, name: rawName.toLowerCase() === "others" ? "Unknown" : rawName, color: sp.provider?.color };
    });
  }, [wiredServiceProvidersData]);

  // Count staged filters (pending, not yet applied)
  const activeFilterCount = useMemo(() => {
    let count = 0;
    if (pendingFilters.providers.length > 0) count++;
    if (pendingFilters.wirelessServiceProviderIds.length > 0) count++;
    if (pendingFilters.wiredServiceProviderIds.length > 0) count++;
    if (pendingFilters.generationIds.length > 0) count++;
    if (pendingFilters.dateRange.startDate || pendingFilters.dateRange.endDate) count++;
    if (pendingFilters.signalStrength.min > -120 || pendingFilters.signalStrength.max < -40) count++;
    if (
      pendingFilters.speedTest.downloadMin !== undefined ||
      pendingFilters.speedTest.downloadMax !== undefined ||
      pendingFilters.speedTest.uploadMin !== undefined ||
      pendingFilters.speedTest.uploadMax !== undefined
    ) {
      count++;
    }
    return count;
  }, [pendingFilters]);

  // Toggle provider selection
  const toggleProvider = useCallback(
    (providerId: string) => {
      const current = pendingFilters.providers;
      const updated = current.includes(providerId) ? current.filter((p) => p !== providerId) : [...current, providerId];
      updatePending({ providers: updated });
    },
    [pendingFilters.providers, updatePending],
  );

  // Toggle wireless service provider selection for speedtest
  const toggleWirelessServiceProvider = useCallback(
    (serviceProviderId: string) => {
      const current = pendingFilters.wirelessServiceProviderIds;
      const updated = current.includes(serviceProviderId)
        ? current.filter((id) => id !== serviceProviderId)
        : [...current, serviceProviderId];
      updatePending({ wirelessServiceProviderIds: updated });
    },
    [pendingFilters.wirelessServiceProviderIds, updatePending],
  );

  // Toggle generation ID for network type filtering
  const toggleGenerationId = useCallback(
    (generationId: string) => {
      const current = pendingFilters.generationIds;
      const updated = current.includes(generationId)
        ? current.filter((id) => id !== generationId)
        : [...current, generationId];
      updatePending({ generationIds: updated });
    },
    [pendingFilters.generationIds, updatePending],
  );

  // Toggle wired service provider selection for NAP/fiber filtering
  const toggleWiredServiceProvider = useCallback(
    (serviceProviderId: string, providerName: string) => {
      const isRemoving = pendingFilters.wiredServiceProviderIds.includes(serviceProviderId);
      const updatedIds = isRemoving
        ? pendingFilters.wiredServiceProviderIds.filter((id) => id !== serviceProviderId)
        : [...pendingFilters.wiredServiceProviderIds, serviceProviderId];
      const updatedNames = isRemoving
        ? pendingFilters.fiberProviderNames.filter((n) => n !== providerName)
        : [...pendingFilters.fiberProviderNames, providerName];
      updatePending({ wiredServiceProviderIds: updatedIds, fiberProviderNames: updatedNames });
    },
    [pendingFilters.wiredServiceProviderIds, pendingFilters.fiberProviderNames, updatePending],
  );

  // Select all / none for providers
  const selectAllProviders = useCallback(
    (providers: ProviderOption[]) => {
      const allIds = providers.map((p) => p.id);
      const allSelected = allIds.every((id) => pendingFilters.providers.includes(id));
      if (allSelected) {
        updatePending({ providers: pendingFilters.providers.filter((p) => !allIds.includes(p)) });
      } else {
        updatePending({ providers: [...new Set([...pendingFilters.providers, ...allIds])] });
      }
    },
    [pendingFilters.providers, updatePending],
  );

  // Handle signal strength change
  const handleSignalStrengthChange = useCallback(
    (values: number[]) => {
      updatePending({ signalStrength: { min: values[0] ?? -120, max: values[1] ?? -40 } });
    },
    [updatePending],
  );

  // Handle speed test range filter change
  const handleSpeedTestRangeChange = useCallback(
    (type: "download" | "upload", values: number[]) => {
      const [rangeMin, rangeMax] = type === "download" ? DOWNLOAD_SPEED_RANGE : UPLOAD_SPEED_RANGE;
      const min = values[0] ?? rangeMin;
      const max = values[1] ?? rangeMax;

      updatePending({
        speedTest: {
          ...pendingFilters.speedTest,
          ...(type === "download" ? { downloadMin: min, downloadMax: max } : { uploadMin: min, uploadMax: max }),
        },
      });
    },
    [pendingFilters.speedTest, updatePending],
  );

  const handleStartDateChange = useCallback(
    (value: string) => {
      const newStart = fromDateInputValue(value);
      let newEnd = pendingFilters.dateRange.endDate;

      // If new start is after current end, auto-swap
      if (newStart && newEnd && newStart > newEnd) {
        newEnd = newStart;
      }

      updatePending({ dateRange: { startDate: newStart, endDate: newEnd } });
    },
    [pendingFilters.dateRange.endDate, updatePending],
  );

  const handleEndDateChange = useCallback(
    (value: string) => {
      const newEnd = fromDateInputValue(value);
      let newStart = pendingFilters.dateRange.startDate;

      // If new end is before current start, auto-swap
      if (newEnd && newStart && newEnd < newStart) {
        newStart = newEnd;
      }

      updatePending({ dateRange: { startDate: newStart, endDate: newEnd } });
    },
    [pendingFilters.dateRange.startDate, updatePending],
  );

  // Toggle layer visibility via AdminMapContext
  const handleLayerToggle = useCallback(
    (layerId: string, checked: boolean) => {
      setLayerVisibility(layerId, checked);
    },
    [setLayerVisibility],
  );

  const downloadRangeValue: [number, number] = useMemo(
    () => [
      pendingFilters.speedTest.downloadMin ?? DOWNLOAD_SPEED_RANGE[0],
      pendingFilters.speedTest.downloadMax ?? DOWNLOAD_SPEED_RANGE[1],
    ],
    [pendingFilters.speedTest.downloadMax, pendingFilters.speedTest.downloadMin],
  );

  const uploadRangeValue: [number, number] = useMemo(
    () => [
      pendingFilters.speedTest.uploadMin ?? UPLOAD_SPEED_RANGE[0],
      pendingFilters.speedTest.uploadMax ?? UPLOAD_SPEED_RANGE[1],
    ],
    [pendingFilters.speedTest.uploadMax, pendingFilters.speedTest.uploadMin],
  );

  const dateRangeLabel = useMemo(() => {
    if (pendingFilters.dateRange.startDate && pendingFilters.dateRange.endDate) {
      return `${format(pendingFilters.dateRange.startDate, "MMM d, yyyy")} - ${format(pendingFilters.dateRange.endDate, "MMM d, yyyy")}`;
    }
    if (pendingFilters.dateRange.startDate) {
      return `From ${format(pendingFilters.dateRange.startDate, "MMM d, yyyy")}`;
    }
    if (pendingFilters.dateRange.endDate) {
      return `Until ${format(pendingFilters.dateRange.endDate, "MMM d, yyyy")}`;
    }
    return "Select date range";
  }, [pendingFilters.dateRange.endDate, pendingFilters.dateRange.startDate]);

  return (
    <motion.div
      initial={false}
      animate={{ x: isPanelCollapsed ? -panelCollapseDistance : 0, opacity: 1 }}
      transition={{ duration: 0.32, ease: [0.4, 0, 0.2, 1] }}
      className={cn("fixed left-0 z-[100] flex flex-col", className)}
      style={{
        top: `${filterPanelTopOffsetPx}px`,
        width: `${FILTER_PANEL_WIDTH_PX}px`,
        height: filterPanelHeightCss,
      }}>
      <GlassPanel
        variant="default"
        rounded="sm"
        className="h-full flex flex-col overflow-hidden rounded-none border-l-0 border-t-0 border-b-0">
              {/* Header */}
              <div className="shrink-0 border-b border-gray-200/50 bg-white/50 backdrop-blur-md">
                <div className="p-4">
                  <SearchBar className="mb-3 z-[2147483647]" />
                  <div className="flex items-center justify-between">
                    <div className="flex items-center gap-2">
                      <Filter className="w-5 h-5 text-gray-600" />
                      <div>
                        <h2 className="font-semibold text-gray-800">Filters & Layers</h2>
                        <p className="text-xs text-gray-500">Customize map data display</p>
                      </div>
                    </div>
                    <div className="flex items-center gap-2">
                      {activeFilterCount > 0 && (
                        <Badge variant="secondary" className="bg-primary/10 text-primary">
                          {activeFilterCount} active
                        </Badge>
                      )}
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={() => {
                          resetFilters();
                          setPendingFilters({ ...DEFAULT_ADMIN_MAP_FILTERS });
                        }}
                        className="text-gray-500 hover:text-gray-700">
                        <RotateCcw className="w-4 h-4 mr-1" />
                        Reset
                      </Button>
                    </div>
                  </div>
                </div>

                {/* Tabs */}
                <Tabs value={activeTab} onValueChange={setActiveTab} className="px-4 pb-2">
                  <TabsList className="grid w-full grid-cols-3 h-9 bg-gray-100/50">
                    <TabsTrigger
                      value="cellular"
                      className="text-xs data-[state=active]:bg-white data-[state=active]:shadow-sm">
                      <Radio className="w-3.5 h-3.5 mr-1" />
                      Cellular
                    </TabsTrigger>
                    <TabsTrigger
                      value="fiber"
                      className="text-xs data-[state=active]:bg-white data-[state=active]:shadow-sm">
                      <Cable className="w-3.5 h-3.5 mr-1" />
                      Fiber
                    </TabsTrigger>
                    <TabsTrigger
                      value="speedtest"
                      className="text-xs data-[state=active]:bg-white data-[state=active]:shadow-sm">
                      <Activity className="w-3.5 h-3.5 mr-1" />
                      Speed
                    </TabsTrigger>
                  </TabsList>
                </Tabs>
              </div>
              {/* Content */}
              <ScrollArea className="flex-1 relative z-0 pointer-events-none">
                <div className="pointer-events-auto">
                  <div className="p-4">
                    {/* Cellular Tab */}
                    {activeTab === "cellular" && (
                      <div className="space-y-0">
                        {/* ── Layers ── */}
                        <div className="px-1 pt-3 pb-1">
                          <div className="flex items-center gap-2">
                            <span className="text-[10px] font-semibold uppercase tracking-widest text-gray-400">
                              Layers
                            </span>
                            <div className="flex-1 h-px bg-gray-200" />
                          </div>
                        </div>
                        <div className="space-y-2 pb-3">
                          {/* Layer visibility */}
                          <CollapsibleSection
                            title="Layer Display"
                            icon={<Radio className="w-4 h-4" />}
                            defaultExpanded={true}>
                            <div className="space-y-3">
                              <div className="flex items-center justify-between">
                                <Label className="flex items-center gap-2 text-sm">
                                  <span className="w-3 h-3 rounded-full bg-blue-500" />
                                  Cell Sites
                                </Label>
                                <Switch
                                  checked={layerState.visibility.cellular ?? false}
                                  onCheckedChange={(checked) => handleLayerToggle("cellular", checked)}
                                />
                              </div>
                              <div className="flex items-center justify-between">
                                <Label className="flex items-center gap-2 text-sm">
                                  <span className="w-3 h-3 rounded-full bg-orange-500" />
                                  Cell Density Heatmap
                                </Label>
                                <Switch
                                  checked={layerState.visibility["cellular-heatmap"] ?? false}
                                  onCheckedChange={(checked) => handleLayerToggle("cellular-heatmap", checked)}
                                />
                              </div>
                              <div className="flex items-center justify-between">
                                <Label className="flex items-center gap-2 text-sm">
                                  <span className="w-3 h-3 rounded-full bg-cyan-500" />
                                  Radio Network Coverage
                                </Label>
                                <Switch
                                  checked={layerState.visibility["cellular-signal"] ?? false}
                                  onCheckedChange={(checked) => handleLayerToggle("cellular-signal", checked)}
                                />
                              </div>
                            </div>
                          </CollapsibleSection>
                        </div>
                        {/* ── Filters ── */}
                        <div className="px-1 pt-2 pb-1">
                          <div className="flex items-center gap-2">
                            <span className="text-[10px] font-semibold uppercase tracking-widest text-gray-400">
                              Filters
                            </span>
                            <div className="flex-1 h-px bg-gray-200" />
                          </div>
                        </div>
                        <div className="space-y-2">
                          {/* Network Type */}
                          <CollapsibleSection
                            title="Network Type"
                            icon={<Signal className="w-4 h-4" />}
                            defaultExpanded={true}
                            badge={
                              pendingFilters.generationIds.length > 0 ? (
                                <Badge variant="outline" className="text-xs">
                                  {pendingFilters.generationIds.length} selected
                                </Badge>
                              ) : undefined
                            }>
                            {isLoadingGenerations ? (
                              <div className="flex items-center gap-2 py-2">
                                <Loader2 className="w-4 h-4 animate-spin text-gray-400" />
                                <span className="text-sm text-gray-500">Loading...</span>
                              </div>
                            ) : generations.length === 0 ? (
                              <p className="text-sm text-gray-400 py-2">No generations available</p>
                            ) : (
                              <div className="flex flex-wrap gap-2 pt-1">
                                {generations.map((gen) => {
                                  const isSelected = pendingFilters.generationIds.includes(gen.id);
                                  return (
                                    <Button
                                      key={gen.id}
                                      variant={isSelected ? "default" : "outline"}
                                      size="sm"
                                      onClick={() => toggleGenerationId(gen.id)}
                                      className={cn("min-w-[60px]", isSelected && "bg-primary hover:bg-primary/90")}>
                                      {gen.name}
                                    </Button>
                                  );
                                })}
                              </div>
                            )}
                          </CollapsibleSection>

                          {/* Providers (wireless service providers only) */}
                          <CollapsibleSection
                            title="Providers"
                            icon={<Building2 className="w-4 h-4" />}
                            defaultExpanded={false}
                            badge={
                              <Button
                                variant="ghost"
                                size="sm"
                                className="h-6 text-xs px-2"
                                onClick={(e) => {
                                  e.stopPropagation();
                                  selectAllProviders(wirelessServiceProviders);
                                }}>
                                {wirelessServiceProviders.every((p) => pendingFilters.providers.includes(p.id))
                                  ? "Clear"
                                  : "All"}
                              </Button>
                            }>
                            <div className="space-y-2">
                              {isLoadingWirelessServiceProviders ? (
                                <div className="flex items-center justify-center py-4">
                                  <Loader2 className="w-5 h-5 animate-spin text-gray-400" />
                                  <span className="ml-2 text-sm text-gray-500">Loading providers...</span>
                                </div>
                              ) : wirelessServiceProviders.length === 0 ? (
                                <p className="text-sm text-gray-400 text-center py-4">
                                  No wireless providers available
                                </p>
                              ) : (
                                wirelessServiceProviders.map((provider) => (
                                  <div key={provider.id} className="flex items-center gap-2">
                                    <Checkbox
                                      id={`provider-${provider.id}`}
                                      checked={pendingFilters.providers.includes(provider.id)}
                                      onCheckedChange={() => toggleProvider(provider.id)}
                                    />
                                    <span
                                      className="w-3 h-3 rounded-full flex-shrink-0"
                                      style={{ backgroundColor: provider.color || "#6b7280" }}
                                    />
                                    <label
                                      htmlFor={`provider-${provider.id}`}
                                      className="text-sm cursor-pointer flex-1">
                                      {provider.name}
                                    </label>
                                  </div>
                                ))
                              )}
                            </div>
                          </CollapsibleSection>

                          {/* Signal Strength */}
                          <CollapsibleSection
                            title="Signal Strength"
                            icon={<Signal className="w-4 h-4" />}
                            defaultExpanded={false}>
                            <div className="space-y-4 pt-2">
                              <Slider
                                value={[pendingFilters.signalStrength.min, pendingFilters.signalStrength.max]}
                                min={-120}
                                max={-40}
                                step={5}
                                onValueChange={handleSignalStrengthChange}
                                className="w-full"
                              />
                              <div className="flex justify-between text-xs text-gray-500">
                                <span>{pendingFilters.signalStrength.min} dBm</span>
                                <span>{pendingFilters.signalStrength.max} dBm</span>
                              </div>
                              <div className="flex justify-between text-xs">
                                {SIGNAL_QUALITY_LABELS.map((item) => (
                                  <span
                                    key={item.value}
                                    className={cn(
                                      "px-1",
                                      item.value >= pendingFilters.signalStrength.min &&
                                        item.value <= pendingFilters.signalStrength.max
                                        ? "text-primary font-medium"
                                        : "text-gray-400",
                                    )}>
                                    {item.label}
                                  </span>
                                ))}
                              </div>
                            </div>
                          </CollapsibleSection>
                        </div>
                      </div>
                    )}

                    {/* Fiber Tab */}
                    {activeTab === "fiber" && (
                      <div className="space-y-0">
                        {/* ── Layers ── */}
                        <div className="px-1 pt-3 pb-1">
                          <div className="flex items-center gap-2">
                            <span className="text-[10px] font-semibold uppercase tracking-widest text-gray-400">
                              Layers
                            </span>
                            <div className="flex-1 h-px bg-gray-200" />
                          </div>
                        </div>
                        <div className="space-y-2 pb-3">
                          {/* Layer visibility */}
                          <CollapsibleSection
                            title="Layer Display"
                            icon={<Cable className="w-4 h-4" />}
                            defaultExpanded={true}>
                            <div className="space-y-3">
                              <div className="flex items-center justify-between">
                                <Label className="flex items-center gap-2 text-sm">
                                  <span className="w-3 h-3 rounded-full bg-purple-500" />
                                  Fiber Coverage
                                </Label>
                                <Switch
                                  checked={layerState.visibility.fiber ?? false}
                                  onCheckedChange={(checked) => handleLayerToggle("fiber", checked)}
                                />
                              </div>
                              <div className="flex items-center justify-between">
                                <Label className="flex items-center gap-2 text-sm">
                                  <span className="w-3 h-3 rounded-full bg-green-500" />
                                  Network Access Points
                                </Label>
                                <Switch
                                  checked={layerState.visibility.naps ?? false}
                                  onCheckedChange={(checked) => handleLayerToggle("naps", checked)}
                                />
                              </div>
                            </div>
                          </CollapsibleSection>
                        </div>
                        {/* ── Filters ── */}
                        <div className="px-1 pt-2 pb-1">
                          <div className="flex items-center gap-2">
                            <span className="text-[10px] font-semibold uppercase tracking-widest text-gray-400">
                              Filters
                            </span>
                            <div className="flex-1 h-px bg-gray-200" />
                          </div>
                        </div>
                        <div className="space-y-2">
                          {/* Fiber Providers (wired service providers) */}
                          <CollapsibleSection
                            title="Providers"
                            icon={<Building2 className="w-4 h-4" />}
                            defaultExpanded={false}>
                            <div className="space-y-2">
                              {isLoadingWiredServiceProviders ? (
                                <div className="flex items-center justify-center py-4">
                                  <Loader2 className="w-5 h-5 animate-spin text-gray-400" />
                                  <span className="ml-2 text-sm text-gray-500">Loading providers...</span>
                                </div>
                              ) : wiredServiceProviders.length === 0 ? (
                                <p className="text-sm text-gray-400 text-center py-4">No wired providers available</p>
                              ) : (
                                wiredServiceProviders.map((provider) => (
                                  <div key={provider.id} className="flex items-center gap-2">
                                    <Checkbox
                                      id={`fiber-provider-${provider.id}`}
                                      checked={pendingFilters.wiredServiceProviderIds.includes(provider.id)}
                                      onCheckedChange={() => toggleWiredServiceProvider(provider.id, provider.name)}
                                    />
                                    <span
                                      className="w-3 h-3 rounded-full flex-shrink-0"
                                      style={{ backgroundColor: provider.color || "#6b7280" }}
                                    />
                                    <label
                                      htmlFor={`fiber-provider-${provider.id}`}
                                      className="text-sm cursor-pointer flex-1">
                                      {provider.name}
                                    </label>
                                  </div>
                                ))
                              )}
                            </div>
                          </CollapsibleSection>
                        </div>
                      </div>
                    )}

                    {/* Speed Test Tab */}
                    {activeTab === "speedtest" && (
                      <div className="space-y-0">
                        {/* ── Layers ── */}
                        <div className="px-1 pt-3 pb-1">
                          <div className="flex items-center gap-2">
                            <span className="text-[10px] font-semibold uppercase tracking-widest text-gray-400">
                              Layers
                            </span>
                            <div className="flex-1 h-px bg-gray-200" />
                          </div>
                        </div>
                        <div className="space-y-2 pb-3">
                          {/* Layer visibility */}
                          <CollapsibleSection
                            title="Layer Display"
                            icon={<Activity className="w-4 h-4" />}
                            defaultExpanded={true}>
                            <div className="space-y-3">
                              <div className="flex items-center justify-between">
                                <Label className="flex items-center gap-2 text-sm">
                                  <span className="w-3 h-3 rounded-full bg-orange-500" />
                                  Speed Test Points
                                </Label>
                                <Switch
                                  checked={layerState.visibility["speedtests-cellular"] ?? false}
                                  onCheckedChange={(checked) => handleLayerToggle("speedtests-cellular", checked)}
                                />
                              </div>
                            </div>
                          </CollapsibleSection>
                        </div>
                        {/* ── Filters ── */}
                        <div className="px-1 pt-2 pb-1">
                          <div className="flex items-center gap-2">
                            <span className="text-[10px] font-semibold uppercase tracking-widest text-gray-400">
                              Filters
                            </span>
                            <div className="flex-1 h-px bg-gray-200" />
                          </div>
                        </div>
                        <div className="space-y-2">
                          {/* Wireless Service Providers */}
                          <CollapsibleSection
                            title="Wireless Providers"
                            icon={<Wifi className="w-4 h-4" />}
                            defaultExpanded={false}>
                            <div className="space-y-3 pt-2">
                              {isLoadingWirelessServiceProviders ? (
                                <div className="flex items-center justify-center py-4">
                                  <span className="ml-2 text-sm text-gray-500">Loading providers...</span>
                                </div>
                              ) : wirelessServiceProviders.length === 0 ? (
                                <p className="text-sm text-gray-400 text-center py-4">
                                  No wireless providers available
                                </p>
                              ) : (
                                <div className="space-y-2">
                                  {wirelessServiceProviders.map((provider) => (
                                    <div key={provider.id} className="flex items-center gap-2">
                                      <Checkbox
                                        id={`wireless-provider-${provider.id}`}
                                        checked={pendingFilters.wirelessServiceProviderIds.includes(provider.id)}
                                        onCheckedChange={() => toggleWirelessServiceProvider(provider.id)}
                                      />
                                      <span
                                        className="w-3 h-3 rounded-full flex-shrink-0"
                                        style={{ backgroundColor: provider.color || "#6b7280" }}
                                      />
                                      <label
                                        htmlFor={`wireless-provider-${provider.id}`}
                                        className="text-sm cursor-pointer flex-1">
                                        {provider.name}
                                      </label>
                                    </div>
                                  ))}
                                </div>
                              )}
                            </div>
                          </CollapsibleSection>

                          {/* Download Speed */}
                          <CollapsibleSection
                            title="Download Speed"
                            icon={<Activity className="w-4 h-4" />}
                            defaultExpanded={false}>
                            <div className="space-y-4 pt-2">
                              <div className="space-y-2">
                                <Label className="text-xs text-gray-500">Range (Mbps)</Label>
                                <Slider
                                  value={downloadRangeValue}
                                  min={DOWNLOAD_SPEED_RANGE[0]}
                                  max={DOWNLOAD_SPEED_RANGE[1]}
                                  step={10}
                                  onValueChange={(values) => handleSpeedTestRangeChange("download", values)}
                                />
                                <div className="flex items-center justify-between text-xs text-gray-500">
                                  <span>{downloadRangeValue[0]} Mbps</span>
                                  <span>{downloadRangeValue[1]} Mbps</span>
                                </div>
                              </div>
                            </div>
                          </CollapsibleSection>

                          {/* Upload Speed */}
                          <CollapsibleSection
                            title="Upload Speed"
                            icon={<Activity className="w-4 h-4" />}
                            defaultExpanded={false}>
                            <div className="space-y-4 pt-2">
                              <div className="space-y-2">
                                <Label className="text-xs text-gray-500">Range (Mbps)</Label>
                                <Slider
                                  value={uploadRangeValue}
                                  min={UPLOAD_SPEED_RANGE[0]}
                                  max={UPLOAD_SPEED_RANGE[1]}
                                  step={5}
                                  onValueChange={(values) => handleSpeedTestRangeChange("upload", values)}
                                />
                                <div className="flex items-center justify-between text-xs text-gray-500">
                                  <span>{uploadRangeValue[0]} Mbps</span>
                                  <span>{uploadRangeValue[1]} Mbps</span>
                                </div>
                              </div>
                            </div>
                          </CollapsibleSection>

                          {/* Date Range */}
                          <CollapsibleSection
                            title="Date Range"
                            icon={<Calendar className="w-4 h-4" />}
                            defaultExpanded={false}>
                            <div className="space-y-3 pt-2">
                              <div className="grid grid-cols-2 gap-2">
                                <div className="space-y-1">
                                  <Label className="text-xs text-gray-500">Start Date</Label>
                                  <Input
                                    type="date"
                                    value={toDateInputValue(pendingFilters.dateRange.startDate)}
                                    onChange={(e) => handleStartDateChange(e.target.value)}
                                  />
                                </div>
                                <div className="space-y-1">
                                  <Label className="text-xs text-gray-500">End Date</Label>
                                  <Input
                                    type="date"
                                    value={toDateInputValue(pendingFilters.dateRange.endDate)}
                                    onChange={(e) => handleEndDateChange(e.target.value)}
                                  />
                                </div>
                              </div>
                              <div className="text-xs text-gray-500">{dateRangeLabel}</div>
                              {(pendingFilters.dateRange.startDate || pendingFilters.dateRange.endDate) && (
                                <Button
                                  variant="ghost"
                                  size="sm"
                                  className="h-7 px-2 text-xs"
                                  onClick={() =>
                                    updatePending({ dateRange: { startDate: undefined, endDate: undefined } })
                                  }>
                                  Clear date range
                                </Button>
                              )}
                            </div>
                          </CollapsibleSection>
                        </div>
                      </div>
                    )}
                  </div>
                </div>
              </ScrollArea>

              {/* Footer */}
              <div className="shrink-0 border-t border-gray-200/50 p-4 bg-white/30">
                <div className="flex gap-2">
                  <Button
                    variant="outline"
                    className="flex-1"
                    onClick={() => {
                      resetFilters();
                      setPendingFilters({ ...DEFAULT_ADMIN_MAP_FILTERS });
                    }}>
                    <RotateCcw className="w-4 h-4 mr-2" />
                    Reset All
                  </Button>
                  <Button
                    className={cn("flex-1", hasPendingChanges && "ring-2 ring-offset-1 ring-primary/60")}
                    onClick={() => updateFilters(pendingFilters)}>
                    <Check className="w-4 h-4 mr-2" />
                    Apply Filters
                    {hasPendingChanges && <span className="ml-1.5 w-2 h-2 rounded-full bg-white/80 inline-block" />}
                  </Button>
                </div>
              </div>
      </GlassPanel>

      {/* Integrated handle tab (always attached to drawer) */}
      <button
        onClick={togglePanelCollapsed}
        className={cn(
          "absolute top-1/2 -translate-y-1/2 rounded-r-xl",
          "bg-white/90 backdrop-blur-md",
          "border border-l-0 border-gray-200/50",
          "hover:bg-white transition-colors",
          "focus:outline-none focus:ring-2 focus:ring-primary/50",
          isPanelCollapsed ? "shadow-xl flex flex-col items-center justify-center gap-2" : "shadow-lg flex items-center justify-center",
        )}
        style={{
          right: `-${FILTER_PANEL_HANDLE_OFFSET_PX}px`,
          width: `${FILTER_PANEL_HANDLE_WIDTH_PX}px`,
          height: `${FILTER_PANEL_HANDLE_HEIGHT_PX}px`,
        }}
        aria-label={isPanelCollapsed ? "Expand filters panel" : "Collapse filters panel"}
        title={isPanelCollapsed ? "Expand filters panel" : "Collapse filters panel"}>
        {isPanelCollapsed ? (
          <>
            <Filter className="w-5 h-5 text-gray-600" />
            <ChevronRight className="w-4 h-4 text-gray-600" />
            {activeFilterCount > 0 && (
              <span className="absolute -top-1 -right-1 w-5 h-5 rounded-full bg-primary text-white text-xs flex items-center justify-center font-medium">
                {activeFilterCount}
              </span>
            )}
          </>
        ) : (
          <ChevronLeft className="w-4 h-4 text-gray-600" />
        )}
      </button>
    </motion.div>
  );
});
