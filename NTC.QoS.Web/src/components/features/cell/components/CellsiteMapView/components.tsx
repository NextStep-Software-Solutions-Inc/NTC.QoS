"use client";

import { AnimatePresence, motion } from "framer-motion";
import {
  Box,
  Camera,
  Check,
  ChevronDown,
  ChevronUp,
  Circle,
  ClipboardCopy,
  FileText,
  Filter,
  Flame,
  Grid3X3,
  Layers,
  Loader2,
  Locate,
  Map,
  Maximize2,
  Minimize2,
  Mountain,
  Radio,
  RotateCcw,
  Search,
  Signal,
  X,
  ZoomIn,
  ZoomOut,
} from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover";
import { ScrollArea } from "@/components/ui/scroll-area";
import { Separator } from "@/components/ui/separator";
import { Slider } from "@/components/ui/slider";
import { Switch } from "@/components/ui/switch";
import { Tooltip, TooltipContent, TooltipTrigger } from "@/components/ui/tooltip";
import { cn } from "@/lib/utils";
import {
  DEFAULT_LAYER_OPACITY,
  MAP_STYLES,
  MODE_CONFIG,
  SIGNAL_STRENGTH_COLORS,
  SIGNAL_STRENGTH_FILTERS,
} from "./constants";
import {
  type CellsiteMapFilters,
  type LayerConfig,
  type LayerOpacity,
  type MapInteractionMode,
  type ProviderCounts,
  type SignalStrengthFilter,
} from "./types";

// ============================================================================
// Map Header Component
// ============================================================================

interface MapHeaderProps {
  filteredCount: number;
  totalCount: number;
  showSearch: boolean;
  onToggleSearch: () => void;
  currentMapStyle: string;
  onChangeStyle: (styleId: string) => void;
  isExpanded: boolean;
  onToggleExpand?: () => void;
  // Layer controls
  layerConfig: LayerConfig;
  layerOpacity: LayerOpacity;
  onLayerConfigChange: (config: LayerConfig) => void;
  onLayerOpacityChange: (opacity: LayerOpacity) => void;
  // Signal strength filter
  signalStrengthFilter: SignalStrengthFilter;
  onSignalStrengthFilterChange: (filter: SignalStrengthFilter) => void;
  // Mode controls
  currentMode: MapInteractionMode;
  onModeChange: (mode: MapInteractionMode) => void;
  hasAddHandler?: boolean;
  hasEditHandler?: boolean;
  hasDeleteHandler?: boolean;
  // Filter controls
  showFilterPanel: boolean;
  onToggleFilterPanel: () => void;
  activeFilterCount: number;
}

export function MapHeader({
  filteredCount,
  totalCount,
  showSearch,
  onToggleSearch,
  currentMapStyle,
  onChangeStyle,
  isExpanded,
  onToggleExpand,
  layerConfig,
  layerOpacity,
  onLayerConfigChange,
  onLayerOpacityChange,
  signalStrengthFilter,
  onSignalStrengthFilterChange,
  currentMode,
  onModeChange,
  hasAddHandler = false,
  hasEditHandler = false,
  hasDeleteHandler = false,
  showFilterPanel,
  onToggleFilterPanel,
  activeFilterCount,
}: MapHeaderProps) {
  const canEdit = hasAddHandler || hasEditHandler || hasDeleteHandler;

  return (
    <div className="p-2 sm:p-3 flex flex-shrink-0 border-b bg-gradient-to-r from-slate-50 to-gray-50 dark:from-gray-900 dark:to-slate-900">
      <div className="flex flex-row items-center justify-between w-full gap-2">
        {/* Left side - Title */}
        <div className="flex items-center gap-1.5 sm:gap-2 min-w-0">
          <div className="p-1 sm:p-1.5 rounded-lg bg-gradient-to-br from-red-500 to-orange-600 shadow-sm flex-shrink-0">
            <Radio className="h-3 w-3 sm:h-4 sm:w-4 text-white" />
          </div>
          <div className="min-w-0">
            <h3 className="text-xs sm:text-sm font-semibold truncate">Cellsite Geographic View</h3>
            <p className="text-[9px] sm:text-[10px] text-muted-foreground">
              {filteredCount.toLocaleString()} of {totalCount.toLocaleString()} Cell Sites
            </p>
          </div>
        </div>

        {/* Right side - Controls */}
        <div className="flex items-center gap-0.5 sm:gap-1 flex-shrink-0 flex-wrap justify-end">
          {/* Mode buttons - inline in header */}
          {canEdit && (
            <>
              <ModeButton mode="view" currentMode={currentMode} onModeChange={onModeChange} />
              {hasAddHandler && <ModeButton mode="add" currentMode={currentMode} onModeChange={onModeChange} />}
              {hasEditHandler && <ModeButton mode="edit" currentMode={currentMode} onModeChange={onModeChange} />}
              {hasDeleteHandler && <ModeButton mode="delete" currentMode={currentMode} onModeChange={onModeChange} />}
              <Separator orientation="vertical" className="h-5 mx-0.5" />
            </>
          )}

          {/* Filter button */}
          <Tooltip>
            <TooltipTrigger asChild>
              <Button
                variant={showFilterPanel ? "secondary" : "ghost"}
                size="sm"
                className="h-6 w-6 sm:h-7 sm:w-7 p-0 relative"
                onClick={onToggleFilterPanel}>
                <Filter className="h-3 w-3 sm:h-3.5 sm:w-3.5" />
                {activeFilterCount > 0 && (
                  <span className="absolute -top-1 -right-1 h-3.5 w-3.5 rounded-full bg-blue-500 text-[8px] text-white flex items-center justify-center">
                    {activeFilterCount}
                  </span>
                )}
              </Button>
            </TooltipTrigger>
            <TooltipContent>Filters{activeFilterCount > 0 ? ` (${activeFilterCount} active)` : ""}</TooltipContent>
          </Tooltip>

          <Tooltip>
            <TooltipTrigger asChild>
              <Button
                variant={showSearch ? "secondary" : "ghost"}
                size="sm"
                className="h-6 w-6 sm:h-7 sm:w-7 p-0"
                onClick={onToggleSearch}>
                <Search className="h-3 w-3 sm:h-3.5 sm:w-3.5" />
              </Button>
            </TooltipTrigger>
            <TooltipContent>Search by coordinates or location</TooltipContent>
          </Tooltip>

          {/* Layer Controls */}
          <LayerControlsPopover
            layerConfig={layerConfig}
            layerOpacity={layerOpacity}
            onLayerConfigChange={onLayerConfigChange}
            onLayerOpacityChange={onLayerOpacityChange}
            signalStrengthFilter={signalStrengthFilter}
            onSignalStrengthFilterChange={onSignalStrengthFilterChange}
          />

          <MapStyleSelector currentStyle={currentMapStyle} onChangeStyle={onChangeStyle} />

          {onToggleExpand && (
            <Tooltip>
              <TooltipTrigger asChild>
                <Button variant="ghost" size="sm" onClick={onToggleExpand} className="h-6 w-6 sm:h-7 sm:w-7 p-0">
                  {isExpanded ? (
                    <Minimize2 className="h-3 w-3 sm:h-3.5 sm:w-3.5" />
                  ) : (
                    <Maximize2 className="h-3 w-3 sm:h-3.5 sm:w-3.5" />
                  )}
                </Button>
              </TooltipTrigger>
              <TooltipContent>{isExpanded ? "Minimize" : "Maximize"}</TooltipContent>
            </Tooltip>
          )}
        </div>
      </div>
    </div>
  );
}

// ============================================================================
// Mode Button Component (for inline mode controls in header)
// ============================================================================

interface ModeButtonProps {
  mode: MapInteractionMode;
  currentMode: MapInteractionMode;
  onModeChange: (mode: MapInteractionMode) => void;
}

function ModeButton({ mode, currentMode, onModeChange }: ModeButtonProps) {
  const config = MODE_CONFIG[mode];
  const Icon = config.icon;
  const isActive = currentMode === mode;

  const activeColors: Record<MapInteractionMode, string> = {
    view: "bg-slate-600 text-white hover:bg-slate-700",
    add: "bg-emerald-600 text-white hover:bg-emerald-700",
    edit: "bg-blue-600 text-white hover:bg-blue-700",
    delete: "bg-red-600 text-white hover:bg-red-700",
  };

  return (
    <Tooltip>
      <TooltipTrigger asChild>
        <Button
          variant="ghost"
          size="sm"
          className={cn("h-6 w-6 sm:h-7 sm:w-7 p-0", isActive && activeColors[mode])}
          onClick={() => onModeChange(mode)}>
          <Icon className="h-3 w-3 sm:h-3.5 sm:w-3.5" />
        </Button>
      </TooltipTrigger>
      <TooltipContent>
        {config.label}: {config.description}
      </TooltipContent>
    </Tooltip>
  );
}

// ============================================================================
// Layer Controls Popover Component
// ============================================================================

interface LayerControlsPopoverProps {
  layerConfig: LayerConfig;
  layerOpacity: LayerOpacity;
  onLayerConfigChange: (config: LayerConfig) => void;
  onLayerOpacityChange: (opacity: LayerOpacity) => void;
  signalStrengthFilter: SignalStrengthFilter;
  onSignalStrengthFilterChange: (filter: SignalStrengthFilter) => void;
}

function LayerControlsPopover({
  layerConfig,
  layerOpacity,
  onLayerConfigChange,
  onLayerOpacityChange,
  signalStrengthFilter,
  onSignalStrengthFilterChange,
}: LayerControlsPopoverProps) {
  // Handle Cell Sites toggle - turn off Point View if Cell Sites is enabled
  const handleCellSitesChange = (checked: boolean) => {
    if (checked) {
      onLayerConfigChange({ ...layerConfig, cellSites: true, pointView: false });
    } else {
      onLayerConfigChange({ ...layerConfig, cellSites: false });
    }
  };

  // Handle Point View toggle - turn off Cell Sites if Point View is enabled
  const handlePointViewChange = (checked: boolean) => {
    if (checked) {
      onLayerConfigChange({ ...layerConfig, pointView: true, cellSites: false });
    } else {
      onLayerConfigChange({ ...layerConfig, pointView: false });
    }
  };

  return (
    <Popover>
      <PopoverTrigger asChild>
        <Button variant="ghost" size="sm" className="h-6 w-6 sm:h-7 sm:w-7 p-0">
          <Layers className="h-3 w-3 sm:h-3.5 sm:w-3.5" />
        </Button>
      </PopoverTrigger>
      <PopoverContent className="w-80 p-3" align="end">
        <ScrollArea className="max-h-[60vh]">
          <div className="space-y-4">
            <div className="flex items-center justify-between">
              <h4 className="text-sm font-semibold flex items-center gap-1.5">
                <Layers className="h-4 w-4 text-primary" />
                Layer Controls
              </h4>
              <Button
                variant="ghost"
                size="sm"
                className="h-6 px-2 text-[10px]"
                onClick={() => {
                  onLayerConfigChange({
                    cellSites: true,
                    cellSiteHeatmap: false,
                    signalCoverage: false,
                    pointView: false,
                  });
                  onLayerOpacityChange(DEFAULT_LAYER_OPACITY);
                  onSignalStrengthFilterChange("all");
                }}>
                Reset
              </Button>
            </div>

            {/* Cell Sites Layer */}
            <div className="space-y-2 p-2 rounded-lg bg-blue-50/50 dark:bg-blue-950/20 border border-blue-100 dark:border-blue-900">
              <div className="flex items-center justify-between">
                <div className="flex items-center gap-2">
                  <div className="p-1 rounded bg-blue-100 dark:bg-blue-900">
                    <Radio className="h-3.5 w-3.5 text-blue-600 dark:text-blue-400" />
                  </div>
                  <Label className="text-xs font-medium">Cell Sites (Clustered)</Label>
                </div>
                <Switch checked={layerConfig.cellSites} onCheckedChange={handleCellSitesChange} />
              </div>
              {layerConfig.cellSites && (
                <div className="flex items-center gap-2 pl-7">
                  <span className="text-[10px] text-muted-foreground w-14">Opacity</span>
                  <Slider
                    value={[layerOpacity.cellSites * 100]}
                    min={10}
                    max={100}
                    step={10}
                    onValueChange={([value]) =>
                      onLayerOpacityChange({ ...layerOpacity, cellSites: (value ?? 100) / 100 })
                    }
                    className="flex-1"
                  />
                  <span className="text-[10px] w-8 text-right">{Math.round(layerOpacity.cellSites * 100)}%</span>
                </div>
              )}
            </div>

            {/* Point View Layer (All Points unclustered) - mutually exclusive with Cell Sites */}
            <div className="space-y-2 p-2 rounded-lg bg-teal-50/50 dark:bg-teal-950/20 border border-teal-100 dark:border-teal-900">
              <div className="flex items-center justify-between">
                <div className="flex items-center gap-2">
                  <div className="p-1 rounded bg-teal-100 dark:bg-teal-900">
                    <Grid3X3 className="h-3.5 w-3.5 text-teal-600 dark:text-teal-400" />
                  </div>
                  <Label className="text-xs font-medium">Point View (All Points)</Label>
                </div>
                <Switch checked={layerConfig.pointView} onCheckedChange={handlePointViewChange} />
              </div>
              {layerConfig.pointView && (
                <div className="flex items-center gap-2 pl-7">
                  <span className="text-[10px] text-muted-foreground w-14">Opacity</span>
                  <Slider
                    value={[layerOpacity.pointView * 100]}
                    min={10}
                    max={100}
                    step={10}
                    onValueChange={([value]) =>
                      onLayerOpacityChange({ ...layerOpacity, pointView: (value ?? 80) / 100 })
                    }
                    className="flex-1"
                  />
                  <span className="text-[10px] w-8 text-right">{Math.round(layerOpacity.pointView * 100)}%</span>
                </div>
              )}
              <p className="text-[9px] text-muted-foreground pl-7">⚡ Cannot be enabled with Cell Sites</p>
            </div>

            <Separator />

            {/* Cell Site Heatmap Layer */}
            <div className="space-y-2 p-2 rounded-lg bg-orange-50/50 dark:bg-orange-950/20 border border-orange-100 dark:border-orange-900">
              <div className="flex items-center justify-between">
                <div className="flex items-center gap-2">
                  <div className="p-1 rounded bg-orange-100 dark:bg-orange-900">
                    <Flame className="h-3.5 w-3.5 text-orange-600 dark:text-orange-400" />
                  </div>
                  <Label className="text-xs font-medium">Cell Site Density</Label>
                </div>
                <Switch
                  checked={layerConfig.cellSiteHeatmap}
                  onCheckedChange={(checked) => onLayerConfigChange({ ...layerConfig, cellSiteHeatmap: checked })}
                />
              </div>
              {layerConfig.cellSiteHeatmap && (
                <div className="flex items-center gap-2 pl-7">
                  <span className="text-[10px] text-muted-foreground w-14">Opacity</span>
                  <Slider
                    value={[layerOpacity.cellSiteHeatmap * 100]}
                    min={10}
                    max={100}
                    step={10}
                    onValueChange={([value]) =>
                      onLayerOpacityChange({ ...layerOpacity, cellSiteHeatmap: (value ?? 70) / 100 })
                    }
                    className="flex-1"
                  />
                  <span className="text-[10px] w-8 text-right">{Math.round(layerOpacity.cellSiteHeatmap * 100)}%</span>
                </div>
              )}
            </div>

            <Separator />

            {/* Signal Coverage Layer with Filter */}
            <div className="space-y-2 p-2 rounded-lg bg-green-50/50 dark:bg-green-950/20 border border-green-100 dark:border-green-900">
              <div className="flex items-center justify-between">
                <div className="flex items-center gap-2">
                  <div className="p-1 rounded bg-green-100 dark:bg-green-900">
                    <Signal className="h-3.5 w-3.5 text-green-600 dark:text-green-400" />
                  </div>
                  <Label className="text-xs font-medium">Signal Coverage</Label>
                </div>
                <Switch
                  checked={layerConfig.signalCoverage}
                  onCheckedChange={(checked) => onLayerConfigChange({ ...layerConfig, signalCoverage: checked })}
                />
              </div>
              {layerConfig.signalCoverage && (
                <>
                  <div className="flex items-center gap-2 pl-7">
                    <span className="text-[10px] text-muted-foreground w-14">Opacity</span>
                    <Slider
                      value={[layerOpacity.signalCoverage * 100]}
                      min={10}
                      max={100}
                      step={10}
                      onValueChange={([value]) =>
                        onLayerOpacityChange({ ...layerOpacity, signalCoverage: (value ?? 80) / 100 })
                      }
                      className="flex-1"
                    />
                    <span className="text-[10px] w-8 text-right">{Math.round(layerOpacity.signalCoverage * 100)}%</span>
                  </div>

                  {/* Signal Strength Filter */}
                  <div className="pl-7 pt-1">
                    <p className="text-[10px] text-muted-foreground mb-2">Filter by Signal Strength</p>
                    <div className="flex flex-wrap gap-1">
                      {SIGNAL_STRENGTH_FILTERS.map((filter) => (
                        <button
                          key={filter.id}
                          onClick={() => onSignalStrengthFilterChange(filter.id as SignalStrengthFilter)}
                          className={cn(
                            "flex items-center gap-1 px-1.5 py-0.5 rounded text-[9px] border transition-all",
                            signalStrengthFilter === filter.id
                              ? "bg-gray-900 text-white border-gray-900 dark:bg-white dark:text-gray-900"
                              : "bg-white dark:bg-gray-800 border-gray-200 dark:border-gray-700 hover:border-gray-400",
                          )}
                          title={filter.label}>
                          <div className="w-2 h-2 rounded-full" style={{ backgroundColor: filter.color }} />
                          {filter.label}
                        </button>
                      ))}
                    </div>
                  </div>

                  {/* Signal Strength Legend */}
                  <div className="pl-7 pt-2">
                    <p className="text-[9px] text-muted-foreground mb-1">Signal Strength Legend</p>
                    <div className="flex items-center gap-1 flex-wrap">
                      {[
                        { label: "≥-60 dBm", color: SIGNAL_STRENGTH_COLORS.excellent },
                        { label: "-70 to -60", color: SIGNAL_STRENGTH_COLORS.good },
                        { label: "-80 to -70", color: SIGNAL_STRENGTH_COLORS.fair },
                        { label: "-90 to -80", color: SIGNAL_STRENGTH_COLORS.poor },
                        { label: "≤-90 dBm", color: SIGNAL_STRENGTH_COLORS.veryPoor },
                      ].map((item) => (
                        <div key={item.label} className="flex items-center gap-0.5">
                          <div className="w-2 h-2 rounded-full" style={{ backgroundColor: item.color }} />
                          <span className="text-[8px]">{item.label}</span>
                        </div>
                      ))}
                    </div>
                  </div>
                </>
              )}
            </div>

            <div className="pt-2 border-t">
              <p className="text-[9px] text-muted-foreground leading-relaxed">
                💡 <strong>Cell Sites</strong> and <strong>Point View</strong> are mutually exclusive. Click on coverage
                points to view signal details.
              </p>
            </div>
          </div>
        </ScrollArea>
      </PopoverContent>
    </Popover>
  );
}

// ============================================================================
// Map Style Selector Component
// ============================================================================

interface MapStyleSelectorProps {
  currentStyle: string;
  onChangeStyle: (styleId: string) => void;
}

function MapStyleSelector({ currentStyle, onChangeStyle }: MapStyleSelectorProps) {
  return (
    <Popover>
      <PopoverTrigger asChild>
        <Button variant="ghost" size="sm" className="h-6 w-6 sm:h-7 sm:w-7 p-0">
          <Map className="h-3 w-3 sm:h-3.5 sm:w-3.5" />
        </Button>
      </PopoverTrigger>
      <PopoverContent className="w-56 p-2" align="end">
        <p className="text-xs font-medium mb-2 flex items-center gap-1.5">
          <Map className="h-3.5 w-3.5" />
          Map Style
        </p>
        <div className="space-y-1 max-h-48 overflow-y-auto">
          {MAP_STYLES.map((style) => (
            <button
              key={style.id}
              onClick={() => onChangeStyle(style.id)}
              className={cn(
                "w-full text-left px-2 py-1.5 text-xs rounded-md flex items-center gap-2 transition-colors",
                currentStyle === style.id
                  ? "bg-blue-100 text-blue-700 dark:bg-blue-900/30 dark:text-blue-300"
                  : "hover:bg-gray-100 dark:hover:bg-gray-800",
              )}>
              <span className="text-sm w-5">{style.icon}</span>
              <span className="flex-1">{style.label}</span>
              {currentStyle === style.id && <Check className="h-3 w-3" />}
            </button>
          ))}
        </div>
      </PopoverContent>
    </Popover>
  );
}

// ============================================================================
// Search Bar Component
// ============================================================================

interface SearchBarProps {
  show: boolean;
  value: string;
  onChange: (value: string) => void;
  onSearch?: (value: string) => void;
}

export function SearchBar({ show, value, onChange, onSearch }: SearchBarProps) {
  return (
    <AnimatePresence>
      {show && (
        <motion.div
          initial={{ height: 0, opacity: 0 }}
          animate={{ height: "auto", opacity: 1 }}
          exit={{ height: 0, opacity: 0 }}
          className="border-b overflow-hidden">
          <div className="p-2 flex flex-col gap-1.5">
            <div className="relative flex-1">
              <Search className="absolute left-2.5 top-1/2 -translate-y-1/2 h-3.5 w-3.5 text-muted-foreground" />
              <Input
                value={value}
                onChange={(e) => onChange(e.target.value)}
                onKeyDown={(e) => {
                  if (e.key === "Enter" && value.trim()) {
                    const trimmed = value.trim();
                    onChange(trimmed);
                    onSearch?.(trimmed);
                  }
                }}
                placeholder="Search by coordinates (e.g. 14.5995, 120.9842)..."
                className="h-8 pl-8 pr-8 text-xs"
              />
              {value && (
                <Button
                  variant="ghost"
                  size="sm"
                  className="absolute right-1 top-1/2 -translate-y-1/2 h-6 w-6 p-0"
                  onClick={() => onChange("")}>
                  <X className="h-3 w-3" />
                </Button>
              )}
            </div>
            <p className="text-[10px] text-muted-foreground px-1">
              Tip: Enter coordinates (lat, lng) and press Enter to navigate the map. Use filters for provider selection.
            </p>
          </div>
        </motion.div>
      )}
    </AnimatePresence>
  );
}

// ============================================================================
// Filter Panel Component
// ============================================================================

interface FilterPanelProps {
  show: boolean;
  onToggle: () => void;
  filters: CellsiteMapFilters;
  onFiltersChange: (filters: CellsiteMapFilters) => void;
  selectedProviders: string[];
  onProvidersChange: (providers: string[]) => void;
  availableProviders: string[];
  availableGenerations: string[];
  availableBands: string[];
  availableTechnologies: string[];
}

export function FilterPanel({
  show,
  onToggle,
  filters,
  onFiltersChange,
  selectedProviders,
  onProvidersChange,
  availableProviders,
  availableGenerations,
  availableBands,
  availableTechnologies,
}: FilterPanelProps) {
  const providerFilterActive =
    availableProviders.length > 0 && selectedProviders.length !== availableProviders.length;

  const activeFilterCount = [
    providerFilterActive,
    filters.generations.length > 0,
    filters.bands.length > 0,
    filters.technologies.length > 0,
  ].filter(Boolean).length;

  const toggleFilter = (category: keyof CellsiteMapFilters, value: string) => {
    if (category === "search") return;

    if (category === "providers") {
      const newValues = selectedProviders.includes(value)
        ? selectedProviders.filter((v) => v !== value)
        : [...selectedProviders, value];
      onProvidersChange(newValues);
      return;
    }

    const current = filters[category] as string[];
    const newValues = current.includes(value) ? current.filter((v) => v !== value) : [...current, value];
    onFiltersChange({ ...filters, [category]: newValues });
  };

  const clearAllFilters = () => {
    onProvidersChange(availableProviders);
    onFiltersChange({ providers: [], generations: [], bands: [], technologies: [], search: filters.search });
  };

  if (!show) {
    return (
      <Button variant="outline" size="sm" className="h-7 px-2.5 text-xs gap-1.5" onClick={onToggle}>
        <Filter className="h-3.5 w-3.5" />
        Filters
        {activeFilterCount > 0 && (
          <Badge variant="secondary" className="h-4 px-1 text-[10px] ml-1">
            {activeFilterCount}
          </Badge>
        )}
      </Button>
    );
  }

  return (
    <motion.div
      initial={{ width: 0, opacity: 0 }}
      animate={{ width: 280, opacity: 1 }}
      exit={{ width: 0, opacity: 0 }}
      className="border-l bg-white/95 dark:bg-gray-900/95 backdrop-blur-sm overflow-hidden flex flex-col">
      <div className="p-3 border-b flex items-center justify-between">
        <div className="flex items-center gap-2">
          <Filter className="h-4 w-4 text-muted-foreground" />
          <span className="text-sm font-semibold">Filters</span>
          {activeFilterCount > 0 && (
            <Badge variant="secondary" className="h-5 px-1.5 text-[10px]">
              {activeFilterCount} active
            </Badge>
          )}
        </div>
        <div className="flex items-center gap-1">
          {activeFilterCount > 0 && (
            <Button
              variant="ghost"
              size="sm"
              className="h-6 px-2 text-[10px] text-red-500 hover:text-red-600"
              onClick={clearAllFilters}>
              Clear
            </Button>
          )}
          <Button variant="ghost" size="sm" className="h-6 w-6 p-0" onClick={onToggle}>
            <X className="h-4 w-4" />
          </Button>
        </div>
      </div>

      <ScrollArea className="flex-1">
        <div className="p-3 space-y-4">
          {/* Provider Filter */}
          <FilterSection
            title="Provider"
            items={availableProviders}
            selected={selectedProviders}
            onToggle={(value) => toggleFilter("providers", value)}
            emptyMessage="No providers available"
          />

          {/* Generation Filter */}
          <FilterSection
            title="Generation"
            items={availableGenerations}
            selected={filters.generations}
            onToggle={(value) => toggleFilter("generations", value)}
            emptyMessage="No generation data available"
          />

          {/* Band Filter */}
          <FilterSection
            title="Band"
            items={availableBands}
            selected={filters.bands}
            onToggle={(value) => toggleFilter("bands", value)}
            emptyMessage="No band data available"
          />

          {/* Technology Filter */}
          <FilterSection
            title="Technology"
            items={availableTechnologies}
            selected={filters.technologies}
            onToggle={(value) => toggleFilter("technologies", value)}
            emptyMessage="No technology data available"
          />
        </div>
      </ScrollArea>
    </motion.div>
  );
}

// ============================================================================
// Filter Section Component
// ============================================================================

interface FilterSectionProps {
  title: string;
  items: string[];
  selected: string[];
  onToggle: (value: string) => void;
  emptyMessage?: string;
}

function FilterSection({ title, items, selected, onToggle, emptyMessage }: FilterSectionProps) {
  return (
    <div>
      <h5 className="text-[10px] font-semibold text-muted-foreground uppercase tracking-wide mb-2">{title}</h5>
      {items.length === 0 ? (
        <p className="text-[10px] text-muted-foreground italic">{emptyMessage || "No options available"}</p>
      ) : (
        <div className="flex flex-wrap gap-1.5">
          {items.map((item) => {
            const isSelected = selected.includes(item);
            return (
              <button
                key={item}
                onClick={() => onToggle(item)}
                className={cn(
                  "px-2 py-1 text-[11px] rounded-md border transition-all",
                  isSelected
                    ? "bg-blue-100 border-blue-300 text-blue-700 dark:bg-blue-900/30 dark:border-blue-700 dark:text-blue-300"
                    : "bg-gray-50 border-gray-200 text-gray-600 hover:bg-gray-100 dark:bg-gray-800 dark:border-gray-700 dark:text-gray-300",
                )}>
                {item}
              </button>
            );
          })}
        </div>
      )}
    </div>
  );
}

// ============================================================================
// Mode Toolbar Component
// ============================================================================

interface ModeToolbarProps {
  currentMode: MapInteractionMode;
  onModeChange: (mode: MapInteractionMode) => void;
  hasAddHandler?: boolean;
  hasEditHandler?: boolean;
  hasDeleteHandler?: boolean;
  showFilterPanel: boolean;
  onToggleFilterPanel: () => void;
  activeFilterCount: number;
}

export function ModeToolbar({
  currentMode,
  onModeChange,
  hasAddHandler = false,
  hasEditHandler = false,
  hasDeleteHandler = false,
  showFilterPanel,
  onToggleFilterPanel,
  activeFilterCount,
}: ModeToolbarProps) {
  const modes: MapInteractionMode[] = ["view", "add", "edit", "delete"];
  const canEdit = hasAddHandler || hasEditHandler || hasDeleteHandler;

  return (
    <div className="px-3 py-2 border-b bg-muted/30 flex items-center gap-1 flex-wrap">
      {/* Filter Toggle */}
      <Button
        variant={showFilterPanel ? "secondary" : "outline"}
        size="sm"
        className="h-7 px-2.5 text-xs gap-1.5"
        onClick={onToggleFilterPanel}>
        <Filter className="h-3.5 w-3.5" />
        Filters
        {activeFilterCount > 0 && (
          <Badge variant="secondary" className="h-4 px-1 text-[10px] ml-1">
            {activeFilterCount}
          </Badge>
        )}
      </Button>

      <Separator orientation="vertical" className="h-5 mx-1" />

      {/* Mode Buttons */}
      {canEdit &&
        modes.map((mode) => {
          const config = MODE_CONFIG[mode];
          const Icon = config.icon;
          const isActive = currentMode === mode;

          // Skip modes without handlers
          if (mode === "add" && !hasAddHandler) return null;
          if (mode === "edit" && !hasEditHandler) return null;
          if (mode === "delete" && !hasDeleteHandler) return null;

          // Determine active styles based on mode
          const activeStyles = isActive
            ? {
                view: "!bg-slate-700 !text-white hover:!bg-slate-800 ring-2 ring-slate-400",
                add: "!bg-emerald-600 !text-white hover:!bg-emerald-700 ring-2 ring-emerald-400",
                edit: "!bg-blue-600 !text-white hover:!bg-blue-700 ring-2 ring-blue-400",
                delete: "!bg-red-600 !text-white hover:!bg-red-700 ring-2 ring-red-400",
              }[mode]
            : "";

          return (
            <Tooltip key={mode}>
              <TooltipTrigger asChild>
                <Button
                  variant={isActive ? "ghost" : "outline"}
                  size="sm"
                  className={cn(
                    "h-7 px-2.5 text-xs gap-1.5 transition-all font-medium",
                    isActive
                      ? `${activeStyles} border-0 shadow-md ring-offset-1 ring-offset-background`
                      : "hover:bg-muted/80",
                  )}
                  onClick={() => onModeChange(mode)}>
                  <Icon className="h-3.5 w-3.5" />
                  {config.label}
                </Button>
              </TooltipTrigger>
              <TooltipContent side="bottom" className="max-w-[200px]">
                <p className="text-xs">{config.description}</p>
              </TooltipContent>
            </Tooltip>
          );
        })}

      <div className="flex-1" />

      {currentMode !== "view" && (
        <Badge
          variant="outline"
          className={cn(
            "text-[10px] gap-1 animate-pulse",
            currentMode === "add" && "border-emerald-500 text-emerald-600",
            currentMode === "edit" && "border-blue-500 text-blue-600",
            currentMode === "delete" && "border-red-500 text-red-600",
          )}>
          <Circle className="h-2 w-2 fill-current" />
          {MODE_CONFIG[currentMode].label} Mode
        </Badge>
      )}
    </div>
  );
}

// ============================================================================
// Mode Indicator Component (shows active mode badge on map)
// ============================================================================

interface ModeIndicatorProps {
  currentMode: MapInteractionMode;
}

export function ModeIndicator({ currentMode }: ModeIndicatorProps) {
  if (currentMode === "view") return null;

  return (
    <div className="absolute top-3 left-1/2 -translate-x-1/2 z-20">
      <Badge
        variant="outline"
        className={cn(
          "text-xs gap-1.5 py-1 px-3 shadow-lg backdrop-blur-sm animate-pulse",
          currentMode === "add" && "border-emerald-500 text-emerald-600 bg-emerald-50/90",
          currentMode === "edit" && "border-blue-500 text-blue-600 bg-blue-50/90",
          currentMode === "delete" && "border-red-500 text-red-600 bg-red-50/90",
        )}>
        <Circle className="h-2 w-2 fill-current" />
        {MODE_CONFIG[currentMode].label} Mode - {MODE_CONFIG[currentMode].description}
      </Badge>
    </div>
  );
}

// ============================================================================
// Map Controls Component
// ============================================================================

interface MapControlsProps {
  onZoomIn: () => void;
  onZoomOut: () => void;
  onResetView: () => void;
  onLocateMe: () => void;
  enableTerrain?: boolean;
  onTerrainChange?: (enabled: boolean) => void;
  enable3D?: boolean;
  on3DChange?: (enabled: boolean) => void;
  onDownloadPNG?: () => void;
  onDownloadPDF?: () => void;
  onCopyClipboard?: () => void;
  isScreenshotReady?: boolean;
}

export function MapControls({
  onZoomIn,
  onZoomOut,
  onResetView,
  onLocateMe,
  enableTerrain = false,
  onTerrainChange,
  enable3D = false,
  on3DChange,
  onDownloadPNG,
  onDownloadPDF,
  onCopyClipboard,
  isScreenshotReady = true,
}: MapControlsProps) {
  return (
    <div className="absolute right-3 top-3 flex flex-col gap-1 z-20">
      <div className="bg-white dark:bg-gray-800 rounded-lg shadow-lg border overflow-hidden">
        <Tooltip>
          <TooltipTrigger asChild>
            <Button variant="ghost" size="sm" className="h-8 w-8 p-0 rounded-none" onClick={onZoomIn}>
              <ZoomIn className="h-4 w-4" />
            </Button>
          </TooltipTrigger>
          <TooltipContent side="left">Zoom in</TooltipContent>
        </Tooltip>
        <Separator />
        <Tooltip>
          <TooltipTrigger asChild>
            <Button variant="ghost" size="sm" className="h-8 w-8 p-0 rounded-none" onClick={onZoomOut}>
              <ZoomOut className="h-4 w-4" />
            </Button>
          </TooltipTrigger>
          <TooltipContent side="left">Zoom out</TooltipContent>
        </Tooltip>
      </div>

      <div className="bg-white dark:bg-gray-800 rounded-lg shadow-lg border overflow-hidden">
        <Tooltip>
          <TooltipTrigger asChild>
            <Button variant="ghost" size="sm" className="h-8 w-8 p-0 rounded-none" onClick={onResetView}>
              <RotateCcw className="h-4 w-4" />
            </Button>
          </TooltipTrigger>
          <TooltipContent side="left">Reset view</TooltipContent>
        </Tooltip>
        <Separator />
        <Tooltip>
          <TooltipTrigger asChild>
            <Button variant="ghost" size="sm" className="h-8 w-8 p-0 rounded-none" onClick={onLocateMe}>
              <Locate className="h-4 w-4" />
            </Button>
          </TooltipTrigger>
          <TooltipContent side="left">My location</TooltipContent>
        </Tooltip>
      </div>

      {/* Terrain and 3D controls */}
      <div className="bg-white dark:bg-gray-800 rounded-lg shadow-lg border overflow-hidden">
        <Tooltip>
          <TooltipTrigger asChild>
            <Button
              variant="ghost"
              size="sm"
              className={cn(
                "h-8 w-8 p-0 rounded-none",
                enableTerrain && "bg-amber-100 text-amber-700 dark:bg-amber-900/30 dark:text-amber-400",
              )}
              onClick={() => onTerrainChange?.(!enableTerrain)}>
              <Mountain className="h-4 w-4" />
            </Button>
          </TooltipTrigger>
          <TooltipContent side="left">{enableTerrain ? "Disable terrain" : "Enable terrain"}</TooltipContent>
        </Tooltip>
        <Separator />
        <Tooltip>
          <TooltipTrigger asChild>
            <Button
              variant="ghost"
              size="sm"
              className={cn(
                "h-8 w-8 p-0 rounded-none",
                enable3D && "bg-purple-100 text-purple-700 dark:bg-purple-900/30 dark:text-purple-400",
              )}
              onClick={() => on3DChange?.(!enable3D)}>
              <Box className="h-4 w-4" />
            </Button>
          </TooltipTrigger>
          <TooltipContent side="left">{enable3D ? "Disable 3D buildings" : "Enable 3D buildings"}</TooltipContent>
        </Tooltip>
      </div>

      {(onDownloadPNG ?? onDownloadPDF ?? onCopyClipboard) && (
        <div className="bg-white dark:bg-gray-800 rounded-lg shadow-lg border overflow-hidden">
          <DropdownMenu>
            <Tooltip>
              <TooltipTrigger asChild>
                <DropdownMenuTrigger asChild>
                  <Button
                    variant="ghost"
                    size="sm"
                    className="h-8 w-8 p-0 rounded-none"
                    disabled={!isScreenshotReady}
                    aria-label={isScreenshotReady ? "Export map" : "Map is rendering..."}
                  >
                    {isScreenshotReady ? (
                      <Camera className="h-4 w-4" />
                    ) : (
                      <Loader2 className="h-4 w-4 animate-spin opacity-50" />
                    )}
                  </Button>
                </DropdownMenuTrigger>
              </TooltipTrigger>
              <TooltipContent side="left">
                {isScreenshotReady ? "Export map" : "Map is rendering..."}
              </TooltipContent>
            </Tooltip>
            <DropdownMenuContent side="left" align="start" className="min-w-[160px]">
              {onDownloadPNG && (
                <DropdownMenuItem onClick={onDownloadPNG} disabled={!isScreenshotReady}>
                  <Camera className="h-4 w-4 mr-2" />
                  Download PNG
                </DropdownMenuItem>
              )}
              {onDownloadPDF && (
                <DropdownMenuItem onClick={onDownloadPDF} disabled={!isScreenshotReady}>
                  <FileText className="h-4 w-4 mr-2" />
                  Download PDF
                </DropdownMenuItem>
              )}
              {(onDownloadPNG ?? onDownloadPDF) && onCopyClipboard && <DropdownMenuSeparator />}
              {onCopyClipboard && (
                <DropdownMenuItem onClick={onCopyClipboard} disabled={!isScreenshotReady}>
                  <ClipboardCopy className="h-4 w-4 mr-2" />
                  Copy to clipboard
                </DropdownMenuItem>
              )}
            </DropdownMenuContent>
          </DropdownMenu>
        </div>
      )}
    </div>
  );
}

// ============================================================================
// Zoom Level Indicator Component
// ============================================================================

interface ZoomLevelIndicatorProps {
  level: number;
}

export function ZoomLevelIndicator({ level }: ZoomLevelIndicatorProps) {
  return (
    <div className="absolute left-3 bottom-3 z-20">
      <Badge variant="secondary" className="text-[10px] font-mono bg-white/90 dark:bg-gray-800/90 shadow">
        Zoom: {level}
      </Badge>
    </div>
  );
}

// ============================================================================
// Loading Overlay Component
// ============================================================================

interface LoadingOverlayProps {
  show: boolean;
}

export function LoadingOverlay({ show }: LoadingOverlayProps) {
  if (!show) return null;

  return (
    <div className="absolute inset-0 flex items-center justify-center bg-gradient-to-br from-gray-100 to-gray-200 dark:from-gray-800 dark:to-gray-900 z-10">
      <div className="text-center">
        <div className="relative">
          <div className="animate-spin rounded-full h-10 w-10 border-4 border-red-200 border-t-red-600 mx-auto" />
          <Radio className="h-4 w-4 text-red-600 absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2" />
        </div>
        <p className="text-xs text-muted-foreground mt-3 font-medium">Loading map...</p>
      </div>
    </div>
  );
}

// ============================================================================
// Error Overlay Component
// ============================================================================

interface ErrorOverlayProps {
  error: string | null;
}

export function ErrorOverlay({ error }: ErrorOverlayProps) {
  if (!error) return null;

  return (
    <div className="absolute inset-0 flex items-center justify-center bg-gray-100 dark:bg-gray-800 z-10">
      <div className="text-center p-4">
        <div className="h-12 w-12 rounded-full bg-red-100 dark:bg-red-900/30 flex items-center justify-center mx-auto mb-3">
          <X className="h-6 w-6 text-red-600" />
        </div>
        <p className="text-sm font-medium text-red-600">{error}</p>
        <Button variant="outline" size="sm" className="mt-3" onClick={() => window.location.reload()}>
          Retry
        </Button>
      </div>
    </div>
  );
}

// ============================================================================
// Provider Legend Component
// ============================================================================

interface ProviderLegendProps {
  show: boolean;
  providerCounts: ProviderCounts;
  onToggle: () => void;
  onToggleProvider: (provider: string) => void;
  onToggleAll: (show: boolean) => void;
}

export function ProviderLegend({ show, providerCounts, onToggle, onToggleProvider, onToggleAll }: ProviderLegendProps) {
  if (!show) {
    return (
      <div className="border-t bg-white dark:bg-gray-900 p-1.5 flex justify-center">
        <Button variant="ghost" size="sm" className="h-5 text-[10px] gap-1" onClick={onToggle}>
          <ChevronUp className="h-3 w-3" />
          Show Legend ({Object.keys(providerCounts).length} providers)
        </Button>
      </div>
    );
  }

  return (
    <motion.div
      initial={{ height: 0, opacity: 0 }}
      animate={{ height: "auto", opacity: 1 }}
      exit={{ height: 0, opacity: 0 }}
      className="border-t bg-white/95 dark:bg-gray-900/95 backdrop-blur-sm overflow-hidden">
      <div className="p-2">
        <div className="flex items-center justify-between mb-2">
          <div className="flex items-center gap-2">
            <Filter className="h-3 w-3 text-muted-foreground" />
            <span className="text-[10px] font-medium text-muted-foreground uppercase tracking-wide">Providers</span>
          </div>
          <div className="flex items-center gap-1">
            <Button variant="ghost" size="sm" className="h-5 px-1.5 text-[10px]" onClick={() => onToggleAll(true)}>
              All
            </Button>
            <Button variant="ghost" size="sm" className="h-5 px-1.5 text-[10px]" onClick={() => onToggleAll(false)}>
              None
            </Button>
            <Button variant="ghost" size="sm" className="h-5 w-5 p-0" onClick={onToggle}>
              <ChevronDown className="h-3 w-3" />
            </Button>
          </div>
        </div>
        <ScrollArea className="max-h-24">
          <div className="flex flex-wrap gap-x-3 gap-y-1.5">
            {Object.entries(providerCounts)
              .sort((a, b) => b[1].count - a[1].count)
              .map(([provider, { count, color, visible }]) => (
                <div
                  key={provider}
                  role="button"
                  tabIndex={0}
                  onClick={() => onToggleProvider(provider)}
                  onKeyDown={(e) => e.key === "Enter" && onToggleProvider(provider)}
                  className={cn(
                    "flex items-center gap-1.5 cursor-pointer text-xs transition-opacity select-none",
                    !visible && "opacity-40",
                  )}>
                  {/* Custom checkbox with proper alignment */}
                  <div
                    className="flex items-center justify-center h-3.5 w-3.5 rounded-sm border-2 shrink-0 transition-colors"
                    style={{ borderColor: color, backgroundColor: visible ? color : "transparent" }}>
                    {visible && <Check className="h-2.5 w-2.5 text-white" strokeWidth={3} />}
                  </div>
                  <span className="text-gray-700 dark:text-gray-300 truncate max-w-[80px]">{provider}</span>
                  <span className="text-gray-400 text-[10px]">({count})</span>
                </div>
              ))}
          </div>
        </ScrollArea>
      </div>
    </motion.div>
  );
}
