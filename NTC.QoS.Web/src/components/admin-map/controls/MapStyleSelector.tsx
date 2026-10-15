/**
 * Map Style Selector Component
 *
 * Glassmorphic dropdown for switching between map styles.
 * Defaults to Topo v4 as requested.
 * Follows Single Responsibility - handles only style selection.
 */

"use client";

import { useCallback, useEffect, useState } from "react";
import { createPortal } from "react-dom";
import Image from "next/image";
import * as maptilersdk from "@maptiler/sdk";
import { AnimatePresence, motion } from "framer-motion";
import { ChevronUp, Map as MapIcon } from "lucide-react";

import { useMap } from "@/hooks/useMap";
import { cn } from "@/lib/utils";
import type { MapStyleConfig } from "../types";
import { MAP_CONTROL_POPOVER_WRAPPER_Z_INDEX, MAP_CONTROL_POPOVER_Z_INDEX } from "../layout-constants";
import { usePopupPositioner } from "../hooks/usePopupPositioner";
import { GlassPanel } from "../ui/GlassPanel";
import { MapControlButton } from "../ui/MapControlButton";

// Available map styles with Topo as default
const mapStyles: MapStyleConfig[] = [
  {
    id: "topo",
    name: "Topography",
    thumbnailUrl: "https://cloud.maptiler.com/static/img/maps/topo-v4.png",
    style: maptilersdk.MapStyle.TOPO,
    isDefault: true, // Topo v4 is default
  },
  {
    id: "topo_dark",
    name: "Topography Dark",
    thumbnailUrl: "https://cloud.maptiler.com/static/img/maps/topo-v4-dark.png",
    style: maptilersdk.MapStyle.TOPO.DARK,
    isDefault: true, // Topo v4 is default
  },
  {
    id: "streets",
    name: "Streets",
    thumbnailUrl: "https://cloud.maptiler.com/static/img/maps/streets-v4.png",
    style: maptilersdk.MapStyle.STREETS_V4,
  },
  {
    id: "satellite",
    name: "Satellite",
    thumbnailUrl: "https://cloud.maptiler.com/static/img/maps/hybrid.png",
    style: maptilersdk.MapStyle.HYBRID,
  },
  {
    id: "dark",
    name: "Dark Mode",
    thumbnailUrl: "https://cloud.maptiler.com/static/img/maps/streets-v4-dark.png",
    style: maptilersdk.MapStyle.STREETS_V4.DARK,
  },
  {
    id: "terrain",
    name: "Outdoor",
    thumbnailUrl: "https://cloud.maptiler.com/static/img/maps/outdoor.png",
    style: maptilersdk.MapStyle.OUTDOOR,
  },
  {
    id: "basic",
    name: "Basic",
    thumbnailUrl: "https://cloud.maptiler.com/static/img/maps/basic.png",
    style: maptilersdk.MapStyle.BASIC_V2,
  },
];

interface MapStyleSelectorProps {
  /** Position of the component */
  position?: "top-right" | "top-left" | "bottom-right" | "bottom-left";
  /** Additional class names */
  className?: string;
  /** Callback when style changes */
  onStyleChange?: (styleId: string) => void;
  /** Compact icon-only trigger (for toolbar islands) */
  compact?: boolean;
  /** Dropdown horizontal alignment */
  dropdownAlign?: "left" | "right";
  /** Controlled open state */
  open?: boolean;
  /** Callback for controlled open state changes */
  onOpenChange?: (open: boolean) => void;
}

export function MapStyleSelector({
  position = "bottom-left",
  className,
  onStyleChange,
  compact = false,
  dropdownAlign = "left",
  open,
  onOpenChange,
}: MapStyleSelectorProps) {
  const { setMapStyle, isMapLoaded } = useMap();
  const [isOpenState, setIsOpenState] = useState(false);
  const [activeStyleId, setActiveStyleId] = useState("topo"); // Default to Topo
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



  const handleStyleChange = useCallback(
    (styleConfig: MapStyleConfig) => {
      setActiveStyleId(styleConfig.id);
      setMapStyle(styleConfig.style);
      setIsOpen(false);
      onStyleChange?.(styleConfig.id);
    },
    [setIsOpen, setMapStyle, onStyleChange],
  );

  const activeStyleData = mapStyles.find((s) => s.id === activeStyleId) ?? mapStyles[0];

  // Non-compact dropdown positioning (CSS-based, used when not in toolbar)
  const dropdownPosition = position.includes("bottom") ? "bottom-full mb-2" : "top-full mt-2";
  const dropdownOriginClass = compact ? "origin-right" : dropdownAlign === "right" ? "origin-top-right" : "origin-top-left";

  // Shared popup content rendered in both compact (portal) and non-compact (absolute) modes
  const stylePickerContent = (
    <GlassPanel padding="sm" className="min-w-[240px]">
      <div className="flex items-center gap-2 px-2 py-1.5 mb-2 border-b border-gray-100/50">
        <MapIcon className="w-4 h-4 text-gray-500" />
        <span className="text-xs font-semibold text-gray-600 uppercase tracking-wide">Map Style</span>
      </div>
      <div className="grid grid-cols-3 gap-2 p-1">
        {mapStyles.map((style) => (
          <button
            key={style.id}
            onClick={() => handleStyleChange(style)}
            className={cn(
              "flex flex-col items-center gap-1.5 p-2 rounded-xl",
              "transition-all duration-200",
              "hover:bg-gray-100/80 focus:outline-none focus:ring-2 focus:ring-primary/40",
              activeStyleId === style.id && "bg-primary/10 ring-2 ring-primary/40",
            )}>
            <div
              className={cn(
                "relative w-14 h-14 rounded-lg overflow-hidden shadow-md",
                "ring-2 transition-all duration-200",
                activeStyleId === style.id ? "ring-primary" : "ring-transparent hover:ring-gray-300",
              )}>
              <Image src={style.thumbnailUrl} alt={style.name} fill unoptimized className="object-cover" sizes="56px" />
            </div>
            <span
              className={cn(
                "text-xs font-medium",
                activeStyleId === style.id ? "text-primary" : "text-gray-600",
              )}>
              {style.name}
            </span>
          </button>
        ))}
      </div>
    </GlassPanel>
  );

  return (
    <div className={cn("relative overflow-visible", className)} style={{ zIndex: MAP_CONTROL_POPOVER_WRAPPER_Z_INDEX }}>
      {/*
       * NON-COMPACT: absolute-positioned dropdown relative to the wrapper div.
       * No backdrop-filter ancestor issue here because it uses CSS absolute, not fixed.
       */}
      {!compact && (
        <AnimatePresence>
          {isOpen && (
            <motion.div
              initial={{ opacity: 0, y: position.includes("bottom") ? 10 : -10, scale: 0.95 }}
              animate={{ opacity: 1, y: 0, scale: 1 }}
              exit={{ opacity: 0, y: position.includes("bottom") ? 10 : -10, scale: 0.95 }}
              transition={{ duration: 0.2, ease: "easeOut" }}
              className={cn(
                "absolute",
                dropdownPosition,
                dropdownAlign === "right" ? "right-0" : "left-0",
                dropdownOriginClass,
              )}
              style={{ zIndex: MAP_CONTROL_POPOVER_Z_INDEX }}>
              {stylePickerContent}
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
                initial={{ opacity: 0, x: -10, scale: 0.95 }}
                animate={{ opacity: 1, x: 0, scale: 1 }}
                exit={{ opacity: 0, x: -10, scale: 0.95 }}
                transition={{ duration: 0.2, ease: "easeOut" }}
                className={dropdownOriginClass}
                style={{ ...popupStyle, zIndex: MAP_CONTROL_POPOVER_Z_INDEX }}>
                {stylePickerContent}
              </motion.div>
            )}
          </AnimatePresence>,
          document.body,
        )}

      {/* Toggle button */}
      {compact ? (
        <MapControlButton
          ref={btnRef}
          tooltip={`Map style: ${activeStyleData?.name ?? "Topography"}`}
          tooltipSide="left"
          onClick={() => setIsOpen(!isOpen)}
          active={isOpen}
          size="sm">
          <MapIcon className="h-5 w-5 text-gray-700" />
        </MapControlButton>
      ) : (
        <GlassPanel padding="none" className="shadow-lg">
          <button
            onClick={() => setIsOpen(!isOpen)}
            className={cn(
              "flex items-center gap-2 px-3 py-2.5 rounded-xl",
              "transition-all duration-200",
              "hover:bg-gray-50/80 focus:outline-none focus:ring-2 focus:ring-primary/40",
            )}>
            <div className="relative w-9 h-9 rounded-lg overflow-hidden shadow-sm ring-1 ring-gray-200">
              <Image
                src={activeStyleData?.thumbnailUrl ?? mapStyles[0]?.thumbnailUrl ?? ""}
                alt={activeStyleData?.name ?? "Map style"}
                fill
                unoptimized
                className="object-cover"
                sizes="36px"
              />
            </div>
            <div className="flex flex-col items-start">
              <span className="text-[10px] uppercase tracking-wide text-gray-400 font-medium">Style</span>
              <span className="text-sm font-semibold text-gray-700">{activeStyleData?.name ?? "Topography"}</span>
            </div>
            <motion.div animate={{ rotate: isOpen ? 180 : 0 }} transition={{ duration: 0.2 }} className="ml-1">
              <ChevronUp className="h-4 w-4 text-gray-400" />
            </motion.div>
          </button>
        </GlassPanel>
      )}
    </div>
  );
}
