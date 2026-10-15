"use client";

import React, { useCallback, useState } from "react";
import { AnimatePresence, motion } from "framer-motion";
import { ChevronDown, ChevronUp, Download, HelpCircle, Maximize2, Minimize2, RefreshCw } from "lucide-react";

import NAPAnalyticsPanel from "@/components/features/NAP/analytics/NAPAnalyticsPanel";
import NAPAdminMap from "@/components/features/NAP/management/NAPAdminMap";
import NAPTable from "@/components/features/NAP/tables/NAPTable";
import { type NAPDetails } from "@/components/features/NAP/types/NAP";
import { Button } from "@/components/ui/button";
import { Tooltip, TooltipContent, TooltipTrigger } from "@/components/ui/tooltip";
import { cn } from "@/lib/utils";

interface SectionConfig {
  id: "analytics" | "map" | "table";
  title: string;
  isCollapsed: boolean;
  height?: string;
}

interface NAPDashboardProps {
  data: NAPDetails[];
  isLoading?: boolean;
  totalCount?: number;
  currentPage?: number;
  pageSize?: number;
  totalPages?: number;
  onPageChange?: (page: number) => void;
  onPageSizeChange?: (size: number) => void;
  onRefresh?: () => void;
  readonly?: boolean;
  className?: string;
  // Location filters for analytics
  regionIds?: string[];
  provinceIds?: string[];
  municipalityIds?: string[];
  barangayIds?: string[];
  serviceProviderIds?: string[];
}

/**
 * NAP Dashboard Component
 * Unified dashboard combining Analytics, Map, and Table views
 *
 * Structure:
 * - Top: Analytics Panel (Collapsible)
 * - Middle: Interactive Map (Full width)
 * - Bottom: Data Table (Collapsible/Resizable)
 */
export function NAPDashboard({
  data,
  isLoading = false,
  totalCount = 0,
  currentPage = 1,
  pageSize = 10,
  totalPages: _totalPages = 1,
  onPageChange,
  onPageSizeChange,
  onRefresh,
  readonly = false,
  className,
  regionIds,
  provinceIds,
  municipalityIds,
  barangayIds,
  serviceProviderIds,
}: NAPDashboardProps) {
  // Section visibility state
  const [sections, setSections] = useState<SectionConfig[]>([
    { id: "analytics", title: "Analytics", isCollapsed: false },
    { id: "map", title: "Map View", isCollapsed: false, height: "400px" },
    { id: "table", title: "Data Table", isCollapsed: false },
  ]);

  const [isFullscreenMap, setIsFullscreenMap] = useState(false);
  const [showTour, setShowTour] = useState(false);

  // Toggle section collapse
  const toggleSection = useCallback((sectionId: SectionConfig["id"]) => {
    setSections((prev) =>
      prev.map((section) => (section.id === sectionId ? { ...section, isCollapsed: !section.isCollapsed } : section)),
    );
  }, []);

  const getSectionConfig = useCallback((id: SectionConfig["id"]) => sections.find((s) => s.id === id), [sections]);

  return (
    <div className={cn("flex flex-col h-full bg-gray-50 dark:bg-gray-950", className)}>
      {/* Dashboard Header */}
      <div className="flex items-center justify-between px-6 py-4 bg-white dark:bg-gray-900 border-b shadow-sm">
        <div>
          <h1 className="text-2xl font-bold bg-gradient-to-r from-blue-600 to-indigo-600 bg-clip-text text-transparent">
            NAP Management Dashboard
          </h1>
          <p className="text-sm text-muted-foreground mt-1">Network Access Point monitoring and management</p>
        </div>
        <div className="flex items-center gap-2">
          <Tooltip>
            <TooltipTrigger asChild>
              <Button variant="outline" size="icon" onClick={onRefresh} disabled={isLoading}>
                <RefreshCw className={cn("h-4 w-4", isLoading && "animate-spin")} />
              </Button>
            </TooltipTrigger>
            <TooltipContent>Refresh data</TooltipContent>
          </Tooltip>

          <Tooltip>
            <TooltipTrigger asChild>
              <Button variant="outline" size="icon" onClick={() => setShowTour(!showTour)}>
                <HelpCircle className="h-4 w-4" />
              </Button>
            </TooltipTrigger>
            <TooltipContent>Show tour</TooltipContent>
          </Tooltip>

          <Button variant="outline" size="sm" className="gap-2">
            <Download className="h-4 w-4" />
            Export
          </Button>
        </div>
      </div>

      {/* Tour overlay */}
      <AnimatePresence>
        {showTour && (
          <motion.div
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            exit={{ opacity: 0 }}
            className="absolute inset-0 z-50 bg-black/50 flex items-center justify-center"
            onClick={() => setShowTour(false)}>
            <motion.div
              initial={{ scale: 0.9 }}
              animate={{ scale: 1 }}
              exit={{ scale: 0.9 }}
              className="bg-white dark:bg-gray-900 rounded-xl p-6 max-w-md shadow-2xl"
              onClick={(e) => e.stopPropagation()}>
              <h3 className="text-lg font-semibold mb-4">Dashboard Tour</h3>
              <div className="space-y-4 text-sm">
                <div className="flex items-start gap-3">
                  <div className="h-6 w-6 rounded-full bg-blue-100 dark:bg-blue-900 flex items-center justify-center text-blue-600 font-bold text-xs">
                    1
                  </div>
                  <div>
                    <p className="font-medium">Analytics Panel</p>
                    <p className="text-muted-foreground">View market share, provider comparison, and growth trends.</p>
                  </div>
                </div>
                <div className="flex items-start gap-3">
                  <div className="h-6 w-6 rounded-full bg-emerald-100 dark:bg-emerald-900 flex items-center justify-center text-emerald-600 font-bold text-xs">
                    2
                  </div>
                  <div>
                    <p className="font-medium">Interactive Map</p>
                    <p className="text-muted-foreground">
                      Use View, Add, Edit, or Delete modes to manage NAPs spatially.
                    </p>
                  </div>
                </div>
                <div className="flex items-start gap-3">
                  <div className="h-6 w-6 rounded-full bg-purple-100 dark:bg-purple-900 flex items-center justify-center text-purple-600 font-bold text-xs">
                    3
                  </div>
                  <div>
                    <p className="font-medium">Data Table</p>
                    <p className="text-muted-foreground">
                      Search, filter, and manage NAP records with full CRUD operations.
                    </p>
                  </div>
                </div>
              </div>
              <Button className="w-full mt-6" onClick={() => setShowTour(false)}>
                Got it!
              </Button>
            </motion.div>
          </motion.div>
        )}
      </AnimatePresence>

      {/* Main Content */}
      <div className="flex-1 overflow-auto p-6 space-y-6">
        {/* Analytics Section */}
        <section>
          <NAPAnalyticsPanel
            regionIds={regionIds}
            provinceIds={provinceIds}
            municipalityIds={municipalityIds}
            barangayIds={barangayIds}
            serviceProviderIds={serviceProviderIds}
            isCollapsed={getSectionConfig("analytics")?.isCollapsed}
            onCollapsedChange={() => toggleSection("analytics")}
          />
        </section>

        {/* Map Section */}
        <section className="relative">
          <div className="flex items-center justify-between mb-2">
            <div className="flex items-center gap-2">
              <h2 className="font-semibold text-lg">Map View</h2>
              <Button variant="ghost" size="icon" className="h-6 w-6" onClick={() => toggleSection("map")}>
                {getSectionConfig("map")?.isCollapsed ? (
                  <ChevronDown className="h-4 w-4" />
                ) : (
                  <ChevronUp className="h-4 w-4" />
                )}
              </Button>
            </div>
            <div className="flex items-center gap-1">
              <Tooltip>
                <TooltipTrigger asChild>
                  <Button
                    variant="ghost"
                    size="icon"
                    className="h-8 w-8"
                    onClick={() => setIsFullscreenMap(!isFullscreenMap)}>
                    {isFullscreenMap ? <Minimize2 className="h-4 w-4" /> : <Maximize2 className="h-4 w-4" />}
                  </Button>
                </TooltipTrigger>
                <TooltipContent>{isFullscreenMap ? "Exit fullscreen" : "Fullscreen map"}</TooltipContent>
              </Tooltip>
            </div>
          </div>

          <AnimatePresence>
            {!getSectionConfig("map")?.isCollapsed && (
              <motion.div
                initial={{ height: 0, opacity: 0 }}
                animate={{ height: isFullscreenMap ? "calc(100vh - 200px)" : 400, opacity: 1 }}
                exit={{ height: 0, opacity: 0 }}
                transition={{ duration: 0.3 }}
                className="overflow-hidden rounded-lg border shadow-sm">
                <NAPAdminMap className="w-full h-full" />
              </motion.div>
            )}
          </AnimatePresence>
        </section>

        {/* Table Section */}
        <section>
          <div className="flex items-center justify-between mb-2">
            <div className="flex items-center gap-2">
              <h2 className="font-semibold text-lg">Data Table</h2>
              <Button variant="ghost" size="icon" className="h-6 w-6" onClick={() => toggleSection("table")}>
                {getSectionConfig("table")?.isCollapsed ? (
                  <ChevronDown className="h-4 w-4" />
                ) : (
                  <ChevronUp className="h-4 w-4" />
                )}
              </Button>
            </div>
          </div>

          <AnimatePresence>
            {!getSectionConfig("table")?.isCollapsed && (
              <motion.div
                initial={{ height: 0, opacity: 0 }}
                animate={{ height: "auto", opacity: 1 }}
                exit={{ height: 0, opacity: 0 }}
                transition={{ duration: 0.3 }}
                className="overflow-hidden bg-white dark:bg-gray-900 rounded-lg border shadow-sm">
                <div className="p-4">
                  <NAPTable
                    data={data}
                    totalCount={totalCount}
                    readonly={readonly}
                    isLoading={isLoading}
                    currentPage={currentPage}
                    pageSize={pageSize}
                    onPageChange={onPageChange}
                    onPageSizeChange={onPageSizeChange}
                  />
                </div>
              </motion.div>
            )}
          </AnimatePresence>
        </section>
      </div>
    </div>
  );
}

export default NAPDashboard;
