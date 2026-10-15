"use client";

import React, { useCallback, useMemo, useState } from "react";
import { AnimatePresence, motion } from "framer-motion";
import { Edit3, Eye, Info, MapPin, MousePointer, Plus, Trash2 } from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Tooltip, TooltipContent, TooltipTrigger } from "@/components/ui/tooltip";
import { cn } from "@/lib/utils";

// Interaction modes for the admin map
export type MapInteractionMode = "view" | "add" | "edit" | "delete";

interface MapModeConfig {
  id: MapInteractionMode;
  label: string;
  icon: React.ReactNode;
  description: string;
  color: string;
  cursorClass: string;
}

const MAP_MODES: MapModeConfig[] = [
  {
    id: "view",
    label: "View",
    icon: <Eye className="h-4 w-4" />,
    description: "Pan and zoom the map to explore NAPs",
    color: "bg-slate-500",
    cursorClass: "cursor-grab",
  },
  {
    id: "add",
    label: "Add",
    icon: <Plus className="h-4 w-4" />,
    description: "Click on the map to place a new NAP",
    color: "bg-emerald-500",
    cursorClass: "cursor-crosshair",
  },
  {
    id: "edit",
    label: "Edit",
    icon: <Edit3 className="h-4 w-4" />,
    description: "Click on a NAP marker to edit its details",
    color: "bg-blue-500",
    cursorClass: "cursor-pointer",
  },
  {
    id: "delete",
    label: "Delete",
    icon: <Trash2 className="h-4 w-4" />,
    description: "Click on NAP markers to select for deletion",
    color: "bg-red-500",
    cursorClass: "cursor-pointer",
  },
];

interface NAPMapModeToolbarProps {
  currentMode: MapInteractionMode;
  onModeChange: (mode: MapInteractionMode) => void;
  disabled?: boolean;
  className?: string;
}

export function NAPMapModeToolbar({ currentMode, onModeChange, disabled = false, className }: NAPMapModeToolbarProps) {
  const [showTooltip, setShowTooltip] = useState(false);

  const currentModeConfig = useMemo(() => MAP_MODES.find((m) => m.id === currentMode) || MAP_MODES[0], [currentMode]);

  return (
    <div
      className={cn(
        "flex flex-col gap-2 p-3 rounded-xl bg-white/90 dark:bg-gray-900/90 backdrop-blur-sm shadow-lg border",
        className,
      )}>
      {/* Mode indicator */}
      <div className="flex items-center gap-2 px-2 pb-2 border-b">
        <Badge className={cn("text-white", currentModeConfig?.color)}>
          {currentModeConfig?.icon}
          <span className="ml-1">{currentModeConfig?.label} Mode</span>
        </Badge>
        <Tooltip open={showTooltip} onOpenChange={setShowTooltip}>
          <TooltipTrigger asChild>
            <Button variant="ghost" size="icon" className="h-6 w-6" onClick={() => setShowTooltip(!showTooltip)}>
              <Info className="h-3.5 w-3.5 text-muted-foreground" />
            </Button>
          </TooltipTrigger>
          <TooltipContent side="right" className="max-w-xs">
            <p>{currentModeConfig?.description}</p>
          </TooltipContent>
        </Tooltip>
      </div>

      {/* Mode buttons */}
      <div className="flex flex-col gap-1">
        {MAP_MODES.map((mode) => (
          <Tooltip key={mode.id}>
            <TooltipTrigger asChild>
              <Button
                variant={currentMode === mode.id ? "default" : "ghost"}
                size="sm"
                className={cn(
                  "justify-start gap-2 transition-all",
                  currentMode === mode.id && mode.color + " text-white hover:opacity-90",
                )}
                onClick={() => onModeChange(mode.id)}
                disabled={disabled}>
                {mode.icon}
                <span>{mode.label}</span>
              </Button>
            </TooltipTrigger>
            <TooltipContent side="right">
              <p>{mode.description}</p>
            </TooltipContent>
          </Tooltip>
        ))}
      </div>
    </div>
  );
}

interface NAPAdminMapProps {
  className?: string;
  onNAPClick?: (napId: string, mode: MapInteractionMode) => void;
  onMapClick?: (lat: number, lng: number, mode: MapInteractionMode) => void;
  onBulkDelete?: (napIds: string[]) => void;
}

/**
 * NAP Admin Map Component
 * Provides interactive map management for Network Access Points
 * with different interaction modes: View, Add, Edit, Delete
 */
export function NAPAdminMap({ className, onNAPClick, onMapClick, onBulkDelete }: NAPAdminMapProps) {
  const [interactionMode, setInteractionMode] = useState<MapInteractionMode>("view");
  const [selectedNAPs, setSelectedNAPs] = useState<Set<string>>(new Set());

  const handleModeChange = useCallback((mode: MapInteractionMode) => {
    setInteractionMode(mode);
    // Clear selections when switching modes
    if (mode !== "delete") {
      setSelectedNAPs(new Set());
    }
  }, []);

  const _handleMapClick = useCallback(
    (lat: number, lng: number) => {
      if (interactionMode === "add") {
        onMapClick?.(lat, lng, interactionMode);
      }
    },
    [interactionMode, onMapClick],
  );

  const _handleNAPMarkerClick = useCallback(
    (napId: string) => {
      if (interactionMode === "edit") {
        onNAPClick?.(napId, interactionMode);
      } else if (interactionMode === "delete") {
        setSelectedNAPs((prev) => {
          const next = new Set(prev);
          if (next.has(napId)) {
            next.delete(napId);
          } else {
            next.add(napId);
          }
          return next;
        });
        onNAPClick?.(napId, interactionMode);
      }
    },
    [interactionMode, onNAPClick],
  );

  const currentModeConfig = useMemo(
    () => MAP_MODES.find((m) => m.id === interactionMode) || MAP_MODES[0],
    [interactionMode],
  );

  return (
    <div className={cn("relative w-full h-full", className)}>
      {/* Map container placeholder */}
      <div
        className={cn(
          "w-full h-full bg-gray-100 dark:bg-gray-800 rounded-lg flex items-center justify-center",
          currentModeConfig?.cursorClass,
        )}>
        <div className="text-center p-8">
          <MapPin className="h-16 w-16 text-gray-400 mx-auto mb-4" />
          <h3 className="text-lg font-medium text-gray-600 dark:text-gray-300">NAP Admin Map</h3>
          <p className="text-sm text-muted-foreground mt-2 max-w-sm">
            This map component integrates with AdminMapClient to provide interactive NAP management capabilities.
          </p>
          <p className="text-xs text-muted-foreground mt-4">
            Current Mode: <span className="font-medium">{interactionMode}</span>
          </p>
        </div>
      </div>

      {/* Mode toolbar - Glassmorphism style */}
      <div className="absolute top-4 left-4 z-10">
        <NAPMapModeToolbar currentMode={interactionMode} onModeChange={handleModeChange} />
      </div>

      {/* Selected NAPs indicator for delete mode */}
      <AnimatePresence>
        {interactionMode === "delete" && selectedNAPs.size > 0 && (
          <motion.div
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            exit={{ opacity: 0, y: 20 }}
            className="absolute bottom-4 left-1/2 -translate-x-1/2 z-10">
            <div className="flex items-center gap-3 px-4 py-2 rounded-lg bg-red-500 text-white shadow-lg">
              <Trash2 className="h-4 w-4" />
              <span className="font-medium">
                {selectedNAPs.size} NAP{selectedNAPs.size > 1 ? "s" : ""} selected
              </span>
              <Button
                variant="secondary"
                size="sm"
                className="bg-white text-red-500 hover:bg-gray-100"
                onClick={() => {
                  // Handle bulk delete through callback
                  const napIds = Array.from(selectedNAPs);
                  onBulkDelete?.(napIds);
                  setSelectedNAPs(new Set());
                }}>
                Delete All
              </Button>
              <Button
                variant="ghost"
                size="sm"
                className="text-white hover:bg-red-600"
                onClick={() => setSelectedNAPs(new Set())}>
                Cancel
              </Button>
            </div>
          </motion.div>
        )}
      </AnimatePresence>

      {/* Mode instructions overlay */}
      <AnimatePresence>
        {interactionMode !== "view" && (
          <motion.div
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            exit={{ opacity: 0 }}
            className="absolute top-4 right-4 z-10">
            <div className="px-3 py-2 rounded-lg bg-black/70 text-white text-sm backdrop-blur-sm">
              <div className="flex items-center gap-2">
                <MousePointer className="h-4 w-4" />
                <span>{currentModeConfig?.description}</span>
              </div>
            </div>
          </motion.div>
        )}
      </AnimatePresence>
    </div>
  );
}

export default NAPAdminMap;
