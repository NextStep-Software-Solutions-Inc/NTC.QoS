/**
 * Layer Controls Component
 *
 * Collapsible panel for layer management with opacity controls.
 * Follows Interface Segregation - each layer type has its own section.
 */

"use client";

import { memo, useCallback, useMemo, useState } from "react";
import { createPortal } from "react-dom";
import { AnimatePresence, motion } from "framer-motion";
import { Activity, Cable, ChevronDown, Eye, EyeOff, Layers, MapPin, Radio, Signal, Wifi } from "lucide-react";

import { cn } from "@/lib/utils";
import { useLayerState } from "../context/AdminMapContext";
import { MAP_CONTROL_POPOVER_WRAPPER_Z_INDEX, MAP_CONTROL_POPOVER_Z_INDEX } from "../layout-constants";
import { usePopupPositioner } from "../hooks/usePopupPositioner";
import type { LayerCategory, LayerConfig } from "../types";
import { CollapsibleSection } from "../ui/CollapsibleSection";
import { GlassPanel } from "../ui/GlassPanel";
import { LayerToggle } from "../ui/LayerToggle";
import { MapControlButton } from "../ui/MapControlButton";

// Layer configurations following Open/Closed principle
// Add new layers here without modifying existing code
const LAYER_CONFIGS: LayerConfig[] = [
  // Cellular layers
  {
    id: "cellular",
    label: "Cell Sites",
    icon: Radio,
    param: "cellular",
    color: "text-blue-500",
    description: "Cell tower locations with clustering",
    hasOpacity: true,
    defaultOpacity: 100,
    category: "cellular",
    subLayers: ["cells-clusters", "cells-cluster-count", "cells-circles", "cells-symbols"],
  },
  {
    id: "cellular-heatmap",
    label: "Cell Density Heatmap",
    icon: Activity,
    param: ".cellular_heatmap",
    color: "text-orange-500",
    description: "Cell site density visualization",
    hasOpacity: true,
    defaultOpacity: 70,
    category: "cellular",
    subLayers: ["cells-heatmap"],
  },
  {
    id: "cellular-signal",
    label: "Signal Coverage",
    icon: Signal,
    param: ".cellular_coverage",
    color: "text-cyan-500",
    description: "Cellular signal strength coverage",
    hasOpacity: true,
    defaultOpacity: 80,
    category: "cellular",
    subLayers: ["signal-coverage-circles"],
  },
  // NAP layers
  {
    id: "naps",
    label: "Network Access Points",
    icon: MapPin,
    param: "naps",
    color: "text-green-500",
    description: "NAP locations",
    hasOpacity: true,
    defaultOpacity: 100,
    category: "fiber",
    subLayers: ["NAPs-cluster", "NAPs-cluster-count", "NAPs-unclustered"],
  },
  // Fiber layers
  {
    id: "fiber",
    label: "Fiber Networks",
    icon: Cable,
    param: "fiber",
    color: "text-purple-500",
    description: "Fiber optic coverage",
    hasOpacity: true,
    defaultOpacity: 50,
    category: "fiber",
    subLayers: ["fiber-line", "highlighted-fiber"],
  },
  // Speedtest layers
  {
    id: "speedtests-cellular",
    label: "Speed Tests",
    icon: Activity,
    param: "speedtests-cellular",
    color: "text-orange-500",
    description: "Speed test results",
    hasOpacity: true,
    defaultOpacity: 70,
    category: "speedtest",
    subLayers: ["speedtests-circles", "speedtests-heatmap"],
  },
];

// Group layers by category
const LAYER_CATEGORIES: { category: LayerCategory; label: string; icon: typeof Layers }[] = [
  { category: "cellular", label: "Cellular Network", icon: Radio },
  { category: "fiber", label: "Fiber & NAPs", icon: Cable },
  { category: "speedtest", label: "Performance", icon: Activity },
];

interface LayerControlsProps {
  className?: string;
  position?: "left" | "right";
  defaultExpanded?: boolean;
  compact?: boolean;
  open?: boolean;
  onOpenChange?: (open: boolean) => void;
}

export const LayerControls = memo(function LayerControls({
  className,
  position = "right",
  defaultExpanded = false,
  compact = false,
  open,
  onOpenChange,
}: LayerControlsProps) {
  const [isOpenState, setIsOpenState] = useState(defaultExpanded);
  const { layerState, setLayerVisibility, setLayerOpacity } = useLayerState();
  const isControlled = open !== undefined;
  const isOpen = isControlled ? open : isOpenState;

  // Activates only when the compact popup is open; hook is always called to satisfy Rules of Hooks.
  const { btnRef, popupRef, popupStyle, isMounted } = usePopupPositioner(isOpen && compact);

  const setIsOpen = useCallback(
    (value: boolean) => {
      if (!isControlled) {
        setIsOpenState(value);
      }
      onOpenChange?.(value);
    },
    [isControlled, onOpenChange],
  );

  // Count active layers
  const activeCount = useMemo(() => {
    return LAYER_CONFIGS.filter((layer) => layerState.visibility[layer.id]).length;
  }, [layerState.visibility]);

  // Handle visibility change for a layer and its sub-layers
  const handleVisibilityChange = useCallback(
    (layer: LayerConfig, visible: boolean) => {
      setLayerVisibility(layer.id, visible);
      // Also toggle sub-layers
      layer.subLayers?.forEach((subLayerId) => {
        setLayerVisibility(subLayerId, visible);
      });
    },
    [setLayerVisibility],
  );

  // Handle opacity change
  const handleOpacityChange = useCallback(
    (layerId: string, opacity: number) => {
      setLayerOpacity(layerId, opacity);
    },
    [setLayerOpacity],
  );

  // Toggle all layers in a category
  const toggleCategory = useCallback(
    (category: LayerCategory, enable: boolean) => {
      LAYER_CONFIGS.filter((l) => l.category === category).forEach((layer) => {
        handleVisibilityChange(layer, enable);
      });
    },
    [handleVisibilityChange],
  );

  // Check if all layers in category are enabled
  const isCategoryActive = useCallback(
    (category: LayerCategory) => {
      const categoryLayers = LAYER_CONFIGS.filter((l) => l.category === category);
      return categoryLayers.every((l) => layerState.visibility[l.id]);
    },
    [layerState.visibility],
  );

  // Group layers by category
  const layersByCategory = useMemo(() => {
    const grouped: Record<LayerCategory, LayerConfig[]> = {
      cellular: [],
      fiber: [],
      speedtest: [],
      coverage: [],
      infrastructure: [],
    };
    LAYER_CONFIGS.forEach((layer) => {
      if (layer.category) {
        grouped[layer.category].push(layer);
      }
    });
    return grouped;
  }, []);

  const compactMotionX = position === "right" ? -12 : 12;
  const panelOriginClass = (() => {
    if (compact && position === "right") return "origin-right";
    if (compact && position === "left") return "origin-left";
    if (position === "right") return "origin-bottom-right";
    return "origin-bottom-left";
  })();

  // Shared panel content rendered in both compact (portal) and non-compact (absolute) modes
  const layerPanelContent = (
    <GlassPanel padding="none" className="overflow-hidden">
      {/* Header */}
      <div className="flex items-center justify-between px-4 py-3 border-b border-gray-100/50 bg-gray-50/30">
        <div className="flex items-center gap-2">
          <Layers className="w-5 h-5 text-primary" />
          <span className="font-semibold text-gray-800">Map Layers</span>
        </div>
        <div className="flex items-center gap-2">
          <button
            onClick={() => LAYER_CONFIGS.forEach((l) => handleVisibilityChange(l, true))}
            className="p-1.5 rounded-lg hover:bg-gray-100 transition-colors"
            title="Show all layers">
            <Eye className="w-4 h-4 text-gray-500" />
          </button>
          <button
            onClick={() => LAYER_CONFIGS.forEach((l) => handleVisibilityChange(l, false))}
            className="p-1.5 rounded-lg hover:bg-gray-100 transition-colors"
            title="Hide all layers">
            <EyeOff className="w-4 h-4 text-gray-500" />
          </button>
        </div>
      </div>

      {/* Layer sections */}
      <div className="max-h-[calc(70vh-60px)] overflow-y-auto">
        {LAYER_CATEGORIES.map(({ category, label, icon: Icon }) => {
          const layers = layersByCategory[category];
          if (!layers || layers.length === 0) return null;

          const activeLayers = layers.filter((l) => layerState.visibility[l.id]).length;

          return (
            <CollapsibleSection
              key={category}
              title={label}
              icon={<Icon className="w-4 h-4" />}
              defaultExpanded={activeLayers > 0}
              badge={
                activeLayers > 0 && (
                  <span className="px-2 py-0.5 rounded-full bg-primary/10 text-primary text-xs font-medium">
                    {activeLayers}
                  </span>
                )
              }>
              <div className="space-y-1">
                {layers.map((layer) => {
                  const IconComponent = layer.icon;
                  return (
                    <LayerToggle
                      key={layer.id}
                      id={layer.id}
                      label={layer.label}
                      description={layer.description}
                      icon={<IconComponent className="w-full h-full" />}
                      activeColor={layer.color}
                      isVisible={layerState.visibility[layer.id] ?? false}
                      opacity={layerState.opacity[layer.id] ?? layer.defaultOpacity ?? 100}
                      showOpacitySlider={layer.hasOpacity}
                      onVisibilityChange={(visible) => handleVisibilityChange(layer, visible)}
                      onOpacityChange={(opacity) => handleOpacityChange(layer.id, opacity)}
                    />
                  );
                })}
              </div>
            </CollapsibleSection>
          );
        })}
      </div>
    </GlassPanel>
  );

  return (
    <div className={cn("relative overflow-visible", className)} style={{ zIndex: MAP_CONTROL_POPOVER_WRAPPER_Z_INDEX }}>
      {/*
       * NON-COMPACT: absolute-positioned panel relative to the wrapper div.
       */}
      {!compact && (
        <AnimatePresence>
          {isOpen && (
            <motion.div
              initial={{ opacity: 0, x: position === "right" ? 20 : -20, scale: 0.95 }}
              animate={{ opacity: 1, x: 0, scale: 1 }}
              exit={{ opacity: 0, x: position === "right" ? 20 : -20, scale: 0.95 }}
              transition={{ duration: 0.25, ease: "easeOut" }}
              className={cn(
                "absolute",
                position === "right" ? "right-0 bottom-full mb-2" : "left-0 bottom-full mb-2",
                "w-80 max-h-[calc(100vh-2rem)]",
                panelOriginClass,
              )}
              style={{ zIndex: MAP_CONTROL_POPOVER_Z_INDEX }}>
              {layerPanelContent}
            </motion.div>
          )}
        </AnimatePresence>
      )}

      {/*
       * COMPACT: rendered via ReactDOM.createPortal into document.body so that
       * position:fixed is always relative to the viewport, regardless of any
       * backdrop-filter / transform on ancestor elements (e.g. GlassPanel).
       */}
      {compact &&
        isMounted &&
        createPortal(
          <AnimatePresence>
            {isOpen && (
              <motion.div
                ref={popupRef}
                initial={{ opacity: 0, x: compactMotionX, scale: 0.95 }}
                animate={{ opacity: 1, x: 0, scale: 1 }}
                exit={{ opacity: 0, x: compactMotionX, scale: 0.95 }}
                transition={{ duration: 0.25, ease: "easeOut" }}
                className={cn("w-80 max-h-[calc(100vh-2rem)]", panelOriginClass)}
                style={{ ...popupStyle, zIndex: MAP_CONTROL_POPOVER_Z_INDEX }}>
                {layerPanelContent}
              </motion.div>
            )}
          </AnimatePresence>,
          document.body,
        )}

      {/* Toggle button */}
      {compact ? (
        <MapControlButton
          ref={btnRef}
          tooltip={activeCount > 0 ? `Map layers (${activeCount} active)` : "Map layers"}
          tooltipSide="left"
          onClick={() => setIsOpen(!isOpen)}
          active={isOpen || activeCount > 0}
          size="sm"
          className="relative">
          <Layers className="w-5 h-5 text-gray-700" />
          {activeCount > 0 && (
            <span className="absolute -top-1 -right-1 min-w-4 h-4 px-1 rounded-full bg-primary text-white text-[10px] font-semibold leading-4 text-center">
              {activeCount}
            </span>
          )}
        </MapControlButton>
      ) : (
        <GlassPanel
          padding="none"
          className={cn("shadow-lg", activeCount > 0 ? "ring-2 ring-primary/40" : "ring-1 ring-gray-200/60")}>
          <button
            onClick={() => setIsOpen(!isOpen)}
            title="View Map Layers"
            className={cn(
              "flex items-center gap-2 px-3 py-2.5 rounded-xl",
              "transition-all duration-200",
              "hover:bg-primary/5 focus:outline-none focus:ring-2 focus:ring-primary/40",
              isOpen && "bg-primary/5",
            )}>
            <div className={cn("w-9 h-9 rounded-lg flex items-center justify-center", "bg-primary/10")}>
              <Layers className="w-5 h-5 text-primary" />
            </div>
            <div className="flex flex-col items-start">
              <span className="text-[10px] uppercase tracking-wide text-primary/70 font-semibold">Map Layers</span>
              <span className="text-sm font-semibold text-gray-700">
                {activeCount > 0 ? `${activeCount} Active` : "View Layers"}
              </span>
            </div>
            {activeCount > 0 && (
              <span className="ml-1 px-2 py-0.5 rounded-full bg-primary text-white text-xs font-semibold">
                {activeCount}
              </span>
            )}
            <motion.div animate={{ rotate: isOpen ? 180 : 0 }} transition={{ duration: 0.2 }} className="ml-1">
              <ChevronDown className="h-4 w-4 text-primary/60" />
            </motion.div>
          </button>
        </GlassPanel>
      )}
    </div>
  );
});
