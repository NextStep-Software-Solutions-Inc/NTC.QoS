"use client";

import { memo } from "react";
import { AnimatePresence, motion } from "framer-motion";
import { ChevronUp, Map, Wifi } from "lucide-react";

import { Button } from "@/components/ui/button";

interface NAPHeroProps {
  regionName: string;
  totalCount: number;
  isCollapsed: boolean;
  showMap: boolean;
  onToggleCollapse: () => void;
  onToggleMap: () => void;
}

/**
 * Compact hero section for NAP management page.
 * Shows region name and total count with minimal design.
 */
function NAPHeroComponent({
  regionName,
  totalCount,
  isCollapsed,
  showMap,
  onToggleCollapse,
  onToggleMap,
}: NAPHeroProps) {
  return (
    <motion.div
      className="overflow-hidden text-white shadow-md sticky top-0 z-40"
      initial={false}
      animate={{ height: isCollapsed ? 48 : 100 }}
      transition={{ type: "spring", stiffness: 300, damping: 30 }}
      style={{ background: "linear-gradient(to right, rgb(14 165 233), rgb(59 130 246), rgb(99 102 241))" }}>
      <div className="relative flex items-center gap-3 px-4 h-full">
        {/* Icon */}
        <motion.div
          className="flex-shrink-0"
          initial={false}
          animate={{ scale: isCollapsed ? 0.8 : 1 }}
          transition={{ type: "spring", stiffness: 300, damping: 25 }}>
          <div
            className="rounded-lg p-2 border"
            style={{
              backgroundColor: "rgba(255, 255, 255, 0.2)",
              backdropFilter: "blur(8px)",
              borderColor: "rgba(255, 255, 255, 0.3)",
            }}>
            <Wifi className={`transition-all duration-200 ${isCollapsed ? "h-4 w-4" : "h-6 w-6"}`} />
          </div>
        </motion.div>

        {/* Content */}
        <div className="flex-1 min-w-0">
          <motion.h1
            className="font-bold tracking-tight truncate"
            initial={false}
            animate={{ fontSize: isCollapsed ? "1rem" : "1.25rem" }}
            transition={{ type: "spring", stiffness: 300, damping: 25 }}>
            NAP Management
          </motion.h1>

          <AnimatePresence>
            {!isCollapsed && (
              <motion.p
                className="text-sm truncate"
                initial={{ opacity: 0, height: 0 }}
                animate={{ opacity: 1, height: "auto" }}
                exit={{ opacity: 0, height: 0 }}
                transition={{ duration: 0.15 }}
                style={{ color: "rgb(186 230 253)" }}>
                {regionName}
              </motion.p>
            )}
          </AnimatePresence>

          {/* Stats - Only visible when expanded */}
          <AnimatePresence>
            {!isCollapsed && (
              <motion.div
                className="flex items-center gap-2 mt-2"
                initial={{ opacity: 0, y: 5 }}
                animate={{ opacity: 1, y: 0 }}
                exit={{ opacity: 0, y: 5 }}
                transition={{ duration: 0.15, delay: 0.05 }}>
                <motion.div
                  className="flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs"
                  initial={{ opacity: 0, scale: 0.9 }}
                  animate={{ opacity: 1, scale: 1 }}
                  transition={{ delay: 0.1 }}
                  style={{ backgroundColor: "rgba(255, 255, 255, 0.2)", backdropFilter: "blur(8px)" }}>
                  <Wifi className="h-3 w-3" />
                  <span className="font-semibold">{totalCount}</span>
                  <span>NAPs</span>
                </motion.div>
              </motion.div>
            )}
          </AnimatePresence>
        </div>

        {/* Controls */}
        <div className="flex items-center gap-1 flex-shrink-0">
          {/* Collapsed stats badge */}
          <AnimatePresence>
            {isCollapsed && (
              <motion.div
                className="flex items-center gap-1 px-2 py-1 rounded-full text-xs"
                initial={{ opacity: 0, scale: 0.9 }}
                animate={{ opacity: 1, scale: 1 }}
                exit={{ opacity: 0, scale: 0.9 }}
                style={{ backgroundColor: "rgba(255, 255, 255, 0.2)", backdropFilter: "blur(8px)" }}>
                <Wifi className="h-3 w-3" />
                <span className="font-semibold">{totalCount}</span>
              </motion.div>
            )}
          </AnimatePresence>

          {/* Toggle map button */}
          <Button
            variant="ghost"
            size="sm"
            className={`h-7 w-7 p-0 text-white hover:bg-white/20 ${showMap ? "bg-white/20" : ""}`}
            onClick={onToggleMap}
            title={showMap ? "Hide Map" : "Show Map"}>
            <Map className="h-4 w-4" />
          </Button>

          {/* Toggle collapse button */}
          <motion.div
            initial={false}
            animate={{ rotate: isCollapsed ? 180 : 0 }}
            transition={{ type: "spring", stiffness: 300, damping: 25 }}>
            <Button
              variant="ghost"
              size="sm"
              className="h-7 w-7 p-0 text-white hover:bg-white/20"
              onClick={onToggleCollapse}>
              <ChevronUp className="h-4 w-4" />
            </Button>
          </motion.div>
        </div>
      </div>
    </motion.div>
  );
}

export const NAPHero = memo(NAPHeroComponent);
