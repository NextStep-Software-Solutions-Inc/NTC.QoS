"use client";

import { memo } from "react";
import { AnimatePresence, motion } from "framer-motion";
import { Building2, CheckCircle, ChevronUp, Map, RadioTower, Satellite, XCircle } from "lucide-react";

import { Button } from "@/components/ui/button";
import { Tooltip, TooltipContent, TooltipTrigger } from "@/components/ui/tooltip";
import type { BroadbandStatistics } from "@/hooks/filters/types";

interface CellsiteHeroProps {
  regionName: string;
  totalCount: number;
  statistics?: BroadbandStatistics;
  isCollapsed: boolean;
  onToggleCollapse: () => void;
  onOpenFullscreenMap?: () => void;
}

/**
 * Animated hero section for cell site management page.
 * Features:
 * - Smooth collapse/expand animation with Framer Motion
 * - Stats badges with stagger animation
 * - Background glow effects
 * - Responsive design
 */
function CellsiteHeroComponent({
  regionName,
  totalCount,
  statistics,
  isCollapsed,
  onToggleCollapse,
  onOpenFullscreenMap,
}: CellsiteHeroProps) {
  // Mock statistics if not provided (for demo purposes)
  // Top 3 providers mock data - would come from API in production
  const stats = statistics || {
    totalCount,
    verifiedCount: Math.floor(totalCount * 0.7),
    unverifiedCount: Math.floor(totalCount * 0.3),
    topProviders:
      totalCount > 0
        ? [
            {
              id: "1",
              name: "Globe",
              color: "#e11d48", // Rose
              count: Math.floor(totalCount * 0.35),
            },
            {
              id: "2",
              name: "Smart",
              color: "#22c55e", // Green
              count: Math.floor(totalCount * 0.28),
            },
            {
              id: "3",
              name: "DITO",
              color: "#3b82f6", // Blue
              count: Math.floor(totalCount * 0.18),
            },
          ]
        : [],
    statusBreakdown: {
      active: Math.floor(totalCount * 0.8),
      inactive: Math.floor(totalCount * 0.15),
      maintenance: Math.floor(totalCount * 0.05),
    },
  };

  return (
    <motion.div
      className="overflow-hidden text-white shadow-lg sticky top-0 z-40"
      initial={false}
      animate={{ height: isCollapsed ? 56 : 160 }}
      transition={{ type: "spring", stiffness: 300, damping: 30 }}
      style={{ background: "linear-gradient(to right, rgb(37 99 235), rgb(79 70 229), rgb(126 34 206))" }}>
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
                background: "radial-gradient(circle, rgba(236, 72, 153, 0.4) 0%, transparent 70%)",
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
                background: "radial-gradient(circle, rgba(34, 211, 238, 0.4) 0%, transparent 70%)",
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
            <Satellite
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
            Cell Site Management
          </motion.h1>

          <AnimatePresence>
            {!isCollapsed && (
              <motion.p
                className="text-sm sm:text-base truncate"
                initial={{ opacity: 0, height: 0 }}
                animate={{ opacity: 1, height: "auto" }}
                exit={{ opacity: 0, height: 0 }}
                transition={{ duration: 0.2 }}
                style={{ color: "rgb(191 219 254)" }}>
                {regionName}
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
                {/* Total Sites */}
                <motion.div
                  className="flex items-center gap-1.5 px-3 py-1.5 rounded-full text-xs sm:text-sm"
                  initial={{ opacity: 0, scale: 0.8 }}
                  animate={{ opacity: 1, scale: 1 }}
                  transition={{ delay: 0.15 }}
                  style={{ backgroundColor: "rgba(255, 255, 255, 0.2)", backdropFilter: "blur(8px)" }}>
                  <RadioTower className="h-3.5 w-3.5" />
                  <span className="font-semibold">{stats.totalCount}</span>
                  <span className="hidden sm:inline">Sites</span>
                </motion.div>

                {/* Verified */}
                <motion.div
                  className="flex items-center gap-1.5 px-3 py-1.5 rounded-full text-xs sm:text-sm"
                  initial={{ opacity: 0, scale: 0.8 }}
                  animate={{ opacity: 1, scale: 1 }}
                  transition={{ delay: 0.2 }}
                  style={{ backgroundColor: "rgba(34, 197, 94, 0.3)", backdropFilter: "blur(8px)" }}>
                  <CheckCircle className="h-3.5 w-3.5" />
                  <span className="font-semibold">{stats.verifiedCount}</span>
                  <span className="hidden sm:inline">Verified</span>
                </motion.div>

                {/* Unverified */}
                <motion.div
                  className="flex items-center gap-1.5 px-3 py-1.5 rounded-full text-xs sm:text-sm"
                  initial={{ opacity: 0, scale: 0.8 }}
                  animate={{ opacity: 1, scale: 1 }}
                  transition={{ delay: 0.25 }}
                  style={{ backgroundColor: "rgba(239, 68, 68, 0.3)", backdropFilter: "blur(8px)" }}>
                  <XCircle className="h-3.5 w-3.5" />
                  <span className="font-semibold">{stats.unverifiedCount}</span>
                  <span className="hidden sm:inline">Unverified</span>
                </motion.div>

                {/* Top 3 Providers */}
                {stats.topProviders &&
                  stats.topProviders.slice(0, 3).map((provider, index) => (
                    <motion.div
                      key={provider.id}
                      className="flex items-center gap-1.5 px-3 py-1.5 rounded-full text-xs sm:text-sm"
                      initial={{ opacity: 0, scale: 0.8 }}
                      animate={{ opacity: 1, scale: 1 }}
                      transition={{ delay: 0.3 + index * 0.05 }}
                      style={{ backgroundColor: provider.color + "40", backdropFilter: "blur(8px)" }}>
                      <Building2 className="h-3.5 w-3.5" />
                      <span className="font-semibold">{provider.name}</span>
                      <span className="opacity-75">({provider.count})</span>
                    </motion.div>
                  ))}
              </motion.div>
            )}
          </AnimatePresence>
        </div>

        {/* Controls - Always visible */}
        <div className="flex items-center gap-1 sm:gap-2 flex-shrink-0">
          {/* Collapsed stats badge */}
          <AnimatePresence>
            {isCollapsed && (
              <motion.div
                className="hidden sm:flex items-center gap-1.5 px-3 py-1.5 rounded-full text-xs"
                initial={{ opacity: 0, scale: 0.8 }}
                animate={{ opacity: 1, scale: 1 }}
                exit={{ opacity: 0, scale: 0.8 }}
                style={{ backgroundColor: "rgba(255, 255, 255, 0.2)", backdropFilter: "blur(8px)" }}>
                <RadioTower className="h-3 w-3" />
                <span className="font-semibold">{stats.totalCount}</span>
              </motion.div>
            )}
          </AnimatePresence>

          {/* Fullscreen map button - uses Map icon */}
          {onOpenFullscreenMap && (
            <Tooltip>
              <TooltipTrigger asChild>
                <Button
                  variant="ghost"
                  size="sm"
                  className="h-8 w-8 p-0 text-white hover:bg-white/20"
                  onClick={onOpenFullscreenMap}>
                  <Map className="h-4 w-4" />
                </Button>
              </TooltipTrigger>
              <TooltipContent side="bottom">
                <p>Live map</p>
              </TooltipContent>
            </Tooltip>
          )}

          {/* Toggle collapse button with rotation */}
          <motion.div
            initial={false}
            animate={{ rotate: isCollapsed ? 180 : 0 }}
            transition={{ type: "spring", stiffness: 300, damping: 25 }}>
            <Tooltip>
              <TooltipTrigger asChild>
                <Button
                  variant="ghost"
                  size="sm"
                  className="h-8 w-8 p-0 text-white hover:bg-white/20"
                  onClick={onToggleCollapse}>
                  <ChevronUp className="h-4 w-4" />
                </Button>
              </TooltipTrigger>
              <TooltipContent side="bottom">
                <p>{isCollapsed ? "Expand header" : "Collapse header"}</p>
              </TooltipContent>
            </Tooltip>
          </motion.div>
        </div>
      </div>
    </motion.div>
  );
}

export const CellsiteHero = memo(CellsiteHeroComponent);
