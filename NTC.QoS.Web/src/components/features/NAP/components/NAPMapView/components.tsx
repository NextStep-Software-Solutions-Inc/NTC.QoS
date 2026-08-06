"use client";

import { AnimatePresence, motion } from "framer-motion";
import {
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
  Map as MapIcon,
  Maximize2,
  Minimize2,
  Network,
  RotateCcw,
  Search,
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
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover";
import { ScrollArea } from "@/components/ui/scroll-area";
import { Separator } from "@/components/ui/separator";
import { Tooltip, TooltipContent, TooltipTrigger } from "@/components/ui/tooltip";
import { cn } from "@/lib/utils";
import { MAP_STYLES, MODE_CONFIG } from "./constants";
import { type MapInteractionMode, type ProviderCounts } from "./types";

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
  showHeatmap?: boolean;
  onToggleHeatmap?: () => void;
  showProviderNetwork?: boolean;
  onToggleProviderNetwork?: () => void;
  showPointView?: boolean;
  onTogglePointView?: () => void;
  // Calculate colors button
  isCalculatingColors?: boolean;
  onCalculateColors?: () => void;
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
  showHeatmap = false,
  onToggleHeatmap,
  showProviderNetwork = false,
  onToggleProviderNetwork,
  showPointView = false,
  onTogglePointView,
  isCalculatingColors = false,
  onCalculateColors,
}: MapHeaderProps) {
  return (
    <div className="p-2 sm:p-3 flex flex-shrink-0 border-b bg-gradient-to-r from-slate-50 to-gray-50 dark:from-gray-900 dark:to-slate-900">
      <div className="flex flex-row items-center justify-between w-full">
        <div className="flex items-center gap-1.5 sm:gap-2 min-w-0">
          <div className="p-1 sm:p-1.5 rounded-lg bg-gradient-to-br from-blue-500 to-indigo-600 shadow-sm flex-shrink-0">
            <MapIcon className="h-3 w-3 sm:h-4 sm:w-4 text-white" />
          </div>
          <div className="min-w-0">
            <h3 className="text-xs sm:text-sm font-semibold truncate">NAP Geographic View</h3>
            <p className="text-[9px] sm:text-[10px] text-muted-foreground">
              {filteredCount.toLocaleString()} of {totalCount.toLocaleString()} NAPs
            </p>
          </div>
        </div>
        <div className="flex items-center gap-0.5 sm:gap-1 flex-shrink-0">
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

          {onToggleHeatmap && (
            <Tooltip>
              <TooltipTrigger asChild>
                <Button
                  variant={showHeatmap ? "secondary" : "ghost"}
                  size="sm"
                  className={cn("h-6 w-6 sm:h-7 sm:w-7 p-0", showHeatmap && "text-orange-600")}
                  onClick={onToggleHeatmap}>
                  <Flame className="h-3 w-3 sm:h-3.5 sm:w-3.5" />
                </Button>
              </TooltipTrigger>
              <TooltipContent>{showHeatmap ? "Hide Heatmap" : "Show Heatmap"}</TooltipContent>
            </Tooltip>
          )}

          {onToggleProviderNetwork && (
            <Tooltip>
              <TooltipTrigger asChild>
                <Button
                  variant={showProviderNetwork ? "secondary" : "ghost"}
                  size="sm"
                  className={cn("h-6 w-6 sm:h-7 sm:w-7 p-0", showProviderNetwork && "text-purple-600")}
                  onClick={onToggleProviderNetwork}>
                  <Network className="h-3 w-3 sm:h-3.5 sm:w-3.5" />
                </Button>
              </TooltipTrigger>
              <TooltipContent>{showProviderNetwork ? "Hide Provider Network" : "Show Provider Network"}</TooltipContent>
            </Tooltip>
          )}

          {onTogglePointView && (
            <Tooltip>
              <TooltipTrigger asChild>
                <Button
                  variant={showPointView ? "secondary" : "ghost"}
                  size="sm"
                  className={cn("h-6 w-6 sm:h-7 sm:w-7 p-0", showPointView && "text-teal-600")}
                  onClick={onTogglePointView}>
                  <Grid3X3 className="h-3 w-3 sm:h-3.5 sm:w-3.5" />
                </Button>
              </TooltipTrigger>
              <TooltipContent>{showPointView ? "Hide Point View" : "Show Point View (All Points)"}</TooltipContent>
            </Tooltip>
          )}

          {onCalculateColors && (
            <Tooltip>
              <TooltipTrigger asChild>
                <Button
                  variant="ghost"
                  size="sm"
                  className={cn("h-6 w-6 sm:h-7 sm:w-7 p-0", isCalculatingColors && "text-amber-600")}
                  onClick={onCalculateColors}
                  disabled={isCalculatingColors}>
                  {isCalculatingColors ? (
                    <Circle className="h-3 w-3 sm:h-3.5 sm:w-3.5 animate-spin" />
                  ) : (
                    <Circle className="h-3 w-3 sm:h-3.5 sm:w-3.5" />
                  )}
                </Button>
              </TooltipTrigger>
              <TooltipContent>{isCalculatingColors ? "Calculating..." : "Calculate Dominant Colors"}</TooltipContent>
            </Tooltip>
          )}

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
        <Button variant="ghost" size="sm" className="h-7 w-7 p-0">
          <Layers className="h-3.5 w-3.5" />
        </Button>
      </PopoverTrigger>
      <PopoverContent className="w-48 p-2" align="end">
        <p className="text-xs font-medium mb-2">Map Style</p>
        <div className="space-y-1">
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
              {currentStyle === style.id && <Check className="h-3 w-3" />}
              <span className={cn(currentStyle !== style.id && "ml-5")}>{style.label}</span>
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
// Mode Toolbar Component
// ============================================================================

interface ModeToolbarProps {
  currentMode: MapInteractionMode;
  onModeChange: (mode: MapInteractionMode) => void;
  hasAddHandler?: boolean;
  hasEditHandler?: boolean;
  hasDeleteHandler?: boolean;
}

export function ModeToolbar({
  currentMode,
  onModeChange,
  hasAddHandler = false,
  hasEditHandler = false,
  hasDeleteHandler = false,
}: ModeToolbarProps) {
  const modes: MapInteractionMode[] = ["view", "add", "edit", "delete"];
  const canEdit = hasAddHandler || hasEditHandler || hasDeleteHandler;

  if (!canEdit) return null;

  return (
    <div className="px-3 py-2 border-b bg-muted/30 flex items-center gap-1 flex-wrap">
      {modes.map((mode) => {
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
// Map Controls Component
// ============================================================================

interface MapControlsProps {
  onZoomIn: () => void;
  onZoomOut: () => void;
  onResetView: () => void;
  onLocateMe: () => void;
  onDownloadPNG?: () => void;
  onDownloadPDF?: () => void;
  onCopyClipboard?: () => void;
  isScreenshotReady?: boolean;
}

export function MapControls({ onZoomIn, onZoomOut, onResetView, onLocateMe, onDownloadPNG, onDownloadPDF, onCopyClipboard, isScreenshotReady = true }: MapControlsProps) {
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
          <div className="animate-spin rounded-full h-10 w-10 border-4 border-blue-200 border-t-blue-600 mx-auto" />
          <MapIcon className="h-4 w-4 text-blue-600 absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2" />
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
