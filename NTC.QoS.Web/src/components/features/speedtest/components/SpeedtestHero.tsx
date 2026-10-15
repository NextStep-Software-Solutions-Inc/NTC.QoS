"use client";

import { memo, useEffect, useRef, useState } from "react";
import { AnimatePresence, motion } from "framer-motion";
import {
  Activity,
  ChevronDown,
  ChevronUp,
  Download,
  Gauge,
  Map,
  Signal,
  SignalHigh,
  SignalLow,
  SignalMedium,
  Timer,
  Upload,
} from "lucide-react";

import { Button } from "@/components/ui/button";

interface SpeedtestStatistics {
  totalCount: number;
  avgDownload: number;
  avgUpload: number;
  avgPing: number;
  excellent: number;
  good: number;
  average: number;
  weak: number;
}

interface SpeedtestHeroProps {
  regionName: string;
  totalCount: number;
  statistics?: SpeedtestStatistics;
  isCollapsed: boolean;
  showMap: boolean;
  onToggleCollapse: () => void;
  onToggleMap: () => void;
  /** Container ref to attach scroll listener for auto-collapse */
  scrollContainerRef?: React.RefObject<HTMLElement | null>;
}

/**
 * Animated hero section for speedtest management page.
 * Features:
 * - Smooth collapse/expand animation with Framer Motion
 * - Stats badges with stagger animation
 * - Background glow effects with cyan/blue theme
 * - Auto-collapse on scroll (when scrollContainerRef provided)
 * - Responsive design
 */
function SpeedtestHeroComponent({
  regionName,
  totalCount,
  statistics,
  isCollapsed,
  showMap,
  onToggleCollapse,
  onToggleMap,
  scrollContainerRef,
}: SpeedtestHeroProps) {
  // Track if auto-collapsed due to scroll
  const [autoCollapsed, setAutoCollapsed] = useState(false);
  const wasManuallyExpanded = useRef(false);

  // Auto-collapse on scroll
  useEffect(() => {
    const container = scrollContainerRef?.current;
    if (!container) return;

    let lastScrollTop = 0;
    const scrollThreshold = 100;

    const handleScroll = () => {
      const scrollTop = container.scrollTop;

      // Scrolling down and past threshold - collapse
      if (scrollTop > scrollThreshold && !isCollapsed && !wasManuallyExpanded.current) {
        setAutoCollapsed(true);
        onToggleCollapse();
      }
      // Scrolled to top - expand if was auto-collapsed
      else if (scrollTop <= 20 && isCollapsed && autoCollapsed) {
        setAutoCollapsed(false);
        onToggleCollapse();
      }

      lastScrollTop = scrollTop;
    };

    container.addEventListener("scroll", handleScroll, { passive: true });
    return () => container.removeEventListener("scroll", handleScroll);
  }, [scrollContainerRef, isCollapsed, autoCollapsed, onToggleCollapse]);

  // Track manual toggle
  const handleManualToggle = () => {
    if (isCollapsed) {
      wasManuallyExpanded.current = true;
    } else {
      wasManuallyExpanded.current = false;
    }
    setAutoCollapsed(false);
    onToggleCollapse();
  };
  // Calculate stats or use defaults
  const stats = statistics || {
    totalCount,
    avgDownload: 0,
    avgUpload: 0,
    avgPing: 0,
    excellent: 0,
    good: 0,
    average: 0,
    weak: 0,
  };

  return (
    <motion.div
      className="overflow-hidden text-white shadow-lg sticky top-0 z-40"
      initial={false}
      animate={{ height: isCollapsed ? 56 : 180 }}
      transition={{ type: "spring", stiffness: 300, damping: 30 }}
      style={{ background: "linear-gradient(to right, rgb(6 182 212), rgb(59 130 246), rgb(124 58 237))" }}>
      {/* Background glow effects */}
      <AnimatePresence>
        {!isCollapsed && (
          <>
            <motion.div
              className="absolute top-0 right-0 h-48 w-48 rounded-full"
              initial={{ opacity: 0, scale: 0.5 }}
              animate={{ opacity: 0.15, scale: 1 }}
              exit={{ opacity: 0, scale: 0.5 }}
              transition={{ duration: 0.3 }}
              style={{
                background: "radial-gradient(circle, rgba(34, 211, 238, 0.4) 0%, transparent 70%)",
                transform: "translate(30%, -30%)",
                filter: "blur(40px)",
              }}
            />
            <motion.div
              className="absolute bottom-0 left-0 h-48 w-48 rounded-full"
              initial={{ opacity: 0, scale: 0.5 }}
              animate={{ opacity: 0.15, scale: 1 }}
              exit={{ opacity: 0, scale: 0.5 }}
              transition={{ duration: 0.3, delay: 0.1 }}
              style={{
                background: "radial-gradient(circle, rgba(139, 92, 246, 0.4) 0%, transparent 70%)",
                transform: "translate(-30%, 30%)",
                filter: "blur(40px)",
              }}
            />
          </>
        )}
      </AnimatePresence>

      <div className="relative flex items-center gap-2 sm:gap-4 md:gap-6 px-3 sm:px-4 md:px-6 h-full">
        {/* Icon with animation */}
        <motion.div
          className="flex-shrink-0"
          initial={false}
          animate={{ scale: isCollapsed ? 0.7 : 1, opacity: isCollapsed ? 0.8 : 1 }}
          transition={{ type: "spring", stiffness: 300, damping: 25 }}>
          <div
            className="rounded-xl p-2 sm:p-3 border"
            style={{
              backgroundColor: "rgba(255, 255, 255, 0.2)",
              backdropFilter: "blur(8px)",
              borderColor: "rgba(255, 255, 255, 0.3)",
            }}>
            <Activity
              className={`transition-all duration-200 ${isCollapsed ? "h-5 w-5" : "h-8 w-8 sm:h-10 sm:w-10"}`}
            />
          </div>
        </motion.div>

        {/* Content */}
        <div className="flex-1 min-w-0">
          <motion.h1
            className="font-bold tracking-tight truncate"
            initial={false}
            animate={{ fontSize: isCollapsed ? "1.125rem" : "1.5rem" }}
            transition={{ type: "spring", stiffness: 300, damping: 25 }}>
            Speedtest Management
          </motion.h1>

          <AnimatePresence>
            {!isCollapsed && (
              <motion.p
                className="text-sm sm:text-base truncate"
                initial={{ opacity: 0, height: 0 }}
                animate={{ opacity: 1, height: "auto" }}
                exit={{ opacity: 0, height: 0 }}
                transition={{ duration: 0.2 }}
                style={{ color: "rgb(186 230 253)" }}>
                {regionName} • Monitor and analyze broadband performance
              </motion.p>
            )}
          </AnimatePresence>

          {/* Stats badges - Only visible when expanded */}
          <AnimatePresence>
            {!isCollapsed && (
              <motion.div
                className="flex flex-wrap items-center gap-2 mt-3"
                initial={{ opacity: 0, y: 10 }}
                animate={{ opacity: 1, y: 0 }}
                exit={{ opacity: 0, y: 10 }}
                transition={{ duration: 0.2, delay: 0.1 }}>
                {/* Total Tests */}
                <motion.div
                  className="flex items-center gap-1.5 px-3 py-1.5 rounded-full text-xs sm:text-sm"
                  initial={{ opacity: 0, scale: 0.8 }}
                  animate={{ opacity: 1, scale: 1 }}
                  transition={{ delay: 0.15 }}
                  style={{ backgroundColor: "rgba(255, 255, 255, 0.2)", backdropFilter: "blur(8px)" }}>
                  <Gauge className="h-3.5 w-3.5" />
                  <span className="font-semibold">{stats.totalCount.toLocaleString()}</span>
                  <span className="hidden sm:inline">Tests</span>
                </motion.div>

                {/* Avg Download */}
                <motion.div
                  className="flex items-center gap-1.5 px-3 py-1.5 rounded-full text-xs sm:text-sm"
                  initial={{ opacity: 0, scale: 0.8 }}
                  animate={{ opacity: 1, scale: 1 }}
                  transition={{ delay: 0.2 }}
                  style={{ backgroundColor: "rgba(34, 197, 94, 0.3)", backdropFilter: "blur(8px)" }}>
                  <Download className="h-3.5 w-3.5" />
                  <span className="font-semibold">{stats.avgDownload.toFixed(1)}</span>
                  <span className="hidden sm:inline">Mbps ↓</span>
                </motion.div>

                {/* Avg Upload */}
                <motion.div
                  className="flex items-center gap-1.5 px-3 py-1.5 rounded-full text-xs sm:text-sm"
                  initial={{ opacity: 0, scale: 0.8 }}
                  animate={{ opacity: 1, scale: 1 }}
                  transition={{ delay: 0.25 }}
                  style={{ backgroundColor: "rgba(59, 130, 246, 0.3)", backdropFilter: "blur(8px)" }}>
                  <Upload className="h-3.5 w-3.5" />
                  <span className="font-semibold">{stats.avgUpload.toFixed(1)}</span>
                  <span className="hidden sm:inline">Mbps ↑</span>
                </motion.div>

                {/* Avg Ping */}
                <motion.div
                  className="flex items-center gap-1.5 px-3 py-1.5 rounded-full text-xs sm:text-sm"
                  initial={{ opacity: 0, scale: 0.8 }}
                  animate={{ opacity: 1, scale: 1 }}
                  transition={{ delay: 0.3 }}
                  style={{ backgroundColor: "rgba(249, 115, 22, 0.3)", backdropFilter: "blur(8px)" }}>
                  <Timer className="h-3.5 w-3.5" />
                  <span className="font-semibold">{stats.avgPing.toFixed(0)}</span>
                  <span className="hidden sm:inline">ms</span>
                </motion.div>

                {/* Signal Quality Breakdown */}
                {stats.excellent > 0 && (
                  <motion.div
                    className="flex items-center gap-1.5 px-3 py-1.5 rounded-full text-xs sm:text-sm"
                    initial={{ opacity: 0, scale: 0.8 }}
                    animate={{ opacity: 1, scale: 1 }}
                    transition={{ delay: 0.35 }}
                    style={{ backgroundColor: "rgba(16, 185, 129, 0.3)", backdropFilter: "blur(8px)" }}>
                    <SignalHigh className="h-3.5 w-3.5" />
                    <span className="font-semibold">{stats.excellent}</span>
                    <span className="hidden sm:inline">Excellent</span>
                  </motion.div>
                )}

                {stats.good > 0 && (
                  <motion.div
                    className="flex items-center gap-1.5 px-3 py-1.5 rounded-full text-xs sm:text-sm"
                    initial={{ opacity: 0, scale: 0.8 }}
                    animate={{ opacity: 1, scale: 1 }}
                    transition={{ delay: 0.4 }}
                    style={{ backgroundColor: "rgba(34, 197, 94, 0.3)", backdropFilter: "blur(8px)" }}>
                    <Signal className="h-3.5 w-3.5" />
                    <span className="font-semibold">{stats.good}</span>
                    <span className="hidden sm:inline">Good</span>
                  </motion.div>
                )}

                {stats.average > 0 && (
                  <motion.div
                    className="flex items-center gap-1.5 px-3 py-1.5 rounded-full text-xs sm:text-sm"
                    initial={{ opacity: 0, scale: 0.8 }}
                    animate={{ opacity: 1, scale: 1 }}
                    transition={{ delay: 0.45 }}
                    style={{ backgroundColor: "rgba(245, 158, 11, 0.3)", backdropFilter: "blur(8px)" }}>
                    <SignalMedium className="h-3.5 w-3.5" />
                    <span className="font-semibold">{stats.average}</span>
                    <span className="hidden sm:inline">Average</span>
                  </motion.div>
                )}

                {stats.weak > 0 && (
                  <motion.div
                    className="flex items-center gap-1.5 px-3 py-1.5 rounded-full text-xs sm:text-sm"
                    initial={{ opacity: 0, scale: 0.8 }}
                    animate={{ opacity: 1, scale: 1 }}
                    transition={{ delay: 0.5 }}
                    style={{ backgroundColor: "rgba(239, 68, 68, 0.3)", backdropFilter: "blur(8px)" }}>
                    <SignalLow className="h-3.5 w-3.5" />
                    <span className="font-semibold">{stats.weak}</span>
                    <span className="hidden sm:inline">Weak</span>
                  </motion.div>
                )}
              </motion.div>
            )}
          </AnimatePresence>
        </div>

        {/* Actions */}
        <div className="flex items-center gap-2">
          {/* Map Toggle Button */}
          <AnimatePresence>
            {!isCollapsed && (
              <motion.div
                initial={{ opacity: 0, scale: 0.8 }}
                animate={{ opacity: 1, scale: 1 }}
                exit={{ opacity: 0, scale: 0.8 }}>
                <Button variant="ghost" size="sm" onClick={onToggleMap} className="text-white hover:bg-white/20">
                  <Map className="h-4 w-4 mr-1" />
                  {showMap ? "Hide Map" : "Show Map"}
                </Button>
              </motion.div>
            )}
          </AnimatePresence>

          {/* Collapse/Expand Button */}
          <Button
            variant="ghost"
            size="icon"
            onClick={handleManualToggle}
            className="text-white hover:bg-white/20 h-8 w-8">
            {isCollapsed ? <ChevronDown className="h-5 w-5" /> : <ChevronUp className="h-5 w-5" />}
          </Button>
        </div>
      </div>
    </motion.div>
  );
}

export const SpeedtestHero = memo(SpeedtestHeroComponent);
export default SpeedtestHero;
