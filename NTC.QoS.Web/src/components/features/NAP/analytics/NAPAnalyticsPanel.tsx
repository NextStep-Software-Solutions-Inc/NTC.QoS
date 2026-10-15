"use client";

import React, { useCallback, useMemo, useState } from "react";
import { AnimatePresence, motion } from "framer-motion";
import {
  Award,
  BarChart3,
  ChevronDown,
  ChevronUp,
  Globe,
  Loader2,
  PieChartIcon,
  Sparkles,
  Target,
  Users,
  Wifi,
} from "lucide-react";
import {
  Bar,
  BarChart,
  CartesianGrid,
  Cell,
  Pie,
  PieChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from "recharts";

import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Progress } from "@/components/ui/progress";
import { cn } from "@/lib/utils";
import { useNAPAnalytics } from "../hooks/useNAPAnalytics";
import type { NAPAnalyticsData } from "../types/analytics";

// Type aliases for cleaner component code
type MarketShareItem = NAPAnalyticsData["marketShareDistribution"][number];
type ProviderComparisonItem = NAPAnalyticsData["providerComparison"][number];

// Animated stat component with gradient background
interface AnimatedStatProps {
  value: number | string;
  label: string;
  icon: React.ReactNode;
  gradient?: string;
  suffix?: string;
  trend?: "up" | "down" | "neutral";
  className?: string;
}

function AnimatedStat({
  value,
  label,
  icon,
  gradient = "from-blue-500 to-blue-600",
  suffix = "",
  trend,
  className,
}: AnimatedStatProps) {

  return (
    <motion.div
      initial={{ opacity: 0, scale: 0.95 }}
      animate={{ opacity: 1, scale: 1 }}
      transition={{ duration: 0.3 }}
      className={cn("relative overflow-hidden rounded-xl p-4 shadow-lg", `bg-gradient-to-br ${gradient}`, className)}>
      {/* Decorative background pattern */}
      <div className="absolute inset-0 opacity-10">
        <div className="absolute -right-4 -top-4 h-24 w-24 rounded-full bg-white/30" />
        <div className="absolute -bottom-2 -left-2 h-16 w-16 rounded-full bg-white/20" />
      </div>

      <div className="relative z-10 flex items-center gap-3">
        <div className="rounded-lg bg-white/20 p-2 backdrop-blur-sm">{icon}</div>
        <div className="flex-1">
          <div className="flex items-baseline gap-1">
            <p className="text-2xl font-bold text-white">
              {typeof value === "number" ? value.toLocaleString() : value}
            </p>
            {suffix && <span className="text-sm text-white/80">{suffix}</span>}
            {trend && (
              <span
                className={cn(
                  "ml-2 text-xs font-medium",
                  trend === "up" && "text-emerald-300",
                  trend === "down" && "text-red-300",
                )}>
                {trend === "up" ? "↑" : trend === "down" ? "↓" : ""}
              </span>
            )}
          </div>
          <p className="text-xs text-white/70">{label}</p>
        </div>
      </div>
    </motion.div>
  );
}

// Insight badge component
function InsightBadge({
  label,
  value,
  color,
}: {
  label: string;
  value: string;
  color: "blue" | "green" | "amber" | "red" | "purple";
}) {
  const colorClasses = {
    blue: "bg-blue-100 text-blue-700 dark:bg-blue-900/30 dark:text-blue-300",
    green: "bg-emerald-100 text-emerald-700 dark:bg-emerald-900/30 dark:text-emerald-300",
    amber: "bg-amber-100 text-amber-700 dark:bg-amber-900/30 dark:text-amber-300",
    red: "bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-300",
    purple: "bg-purple-100 text-purple-700 dark:bg-purple-900/30 dark:text-purple-300",
  };

  return (
    <div className={cn("flex items-center gap-2 rounded-lg px-3 py-2", colorClasses[color])}>
      <span className="text-xs font-medium">{label}:</span>
      <span className="text-xs font-bold">{value}</span>
    </div>
  );
}

interface NAPAnalyticsPanelProps {
  barangayIds?: string[];
  municipalityIds?: string[];
  provinceIds?: string[];
  regionIds?: string[];
  serviceProviderIds?: string[];
  includeGeographicCoverage?: boolean;
  isCollapsed?: boolean;
  onCollapsedChange?: (collapsed: boolean) => void;
  className?: string;
}

/**
 * Map API market analysis to color scheme
 */
function getMarketColor(market: string): "blue" | "green" | "amber" | "red" | "purple" {
  const normalized = market.toLowerCase();
  if (normalized.includes("competitive") || normalized.includes("balanced")) return "green";
  if (normalized.includes("moderate")) return "amber";
  if (normalized.includes("dominated") || normalized.includes("high")) return "red";
  return "blue";
}

/**
 * Map API coverage quality to color scheme
 */
function getCoverageColor(coverage: string): "blue" | "green" | "amber" | "red" | "purple" {
  const normalized = coverage.toLowerCase();
  if (normalized.includes("excellent")) return "green";
  if (normalized.includes("good")) return "blue";
  if (normalized.includes("moderate")) return "amber";
  if (normalized.includes("sparse") || normalized.includes("poor")) return "red";
  return "blue";
}

/**
 * Map API competition score to color scheme
 */
function getCompetitionColor(score: number): "blue" | "green" | "amber" | "red" | "purple" {
  if (score > 60) return "green";
  if (score > 40) return "amber";
  return "red";
}

// Custom tooltip for charts
const CustomTooltip = ({ active, payload, label }: any) => {
  if (active && payload && payload.length) {
    const data = payload[0]?.payload;
    const providerName = data?.providerName || label;
    const providerColor = data?.providerColor || "#6b7280";
    const napCount = data?.napCount || payload[0]?.value;
    const marketShare = data?.marketSharePercentage;

    return (
      <div className="rounded-lg border bg-white/95 dark:bg-gray-900/95 backdrop-blur-sm px-3 py-2 shadow-xl">
        <p className="text-xs font-semibold text-gray-900 dark:text-gray-100">{providerName}</p>
        <p className="text-sm font-bold" style={{ color: providerColor }}>
          {napCount?.toLocaleString()} NAPs
        </p>
        {marketShare && <p className="text-xs text-muted-foreground">{marketShare}% market share</p>}
      </div>
    );
  }
  return null;
};

export function NAPAnalyticsPanel({
  barangayIds,
  municipalityIds,
  provinceIds,
  regionIds,
  serviceProviderIds,
  includeGeographicCoverage = true,
  isCollapsed = false,
  onCollapsedChange,
  className,
}: NAPAnalyticsPanelProps) {
  // Fetch analytics data from API
  const { data, isLoading, isError } = useNAPAnalytics({
    pathParams: {},
    queryParams: {
      barangayIds,
      municipalityIds,
      provinceIds,
      regionIds,
      serviceProviderIds,
      includeGeographicCoverage,
    },
  });

  const toggleCollapsed = useCallback(() => {
    onCollapsedChange?.(!isCollapsed);
  }, [isCollapsed, onCollapsedChange]);

  // Show loading state
  if (isLoading) {
    return (
      <div className={cn("w-full", className)}>
        <div className="flex items-center justify-center py-12 bg-gradient-to-br from-white to-gray-50 dark:from-gray-800 dark:to-gray-900 rounded-xl border shadow-lg">
          <div className="text-center space-y-3">
            <Loader2 className="h-8 w-8 animate-spin text-indigo-600 mx-auto" />
            <p className="text-sm text-muted-foreground">Loading analytics...</p>
          </div>
        </div>
      </div>
    );
  }

  // Show error state
  if (isError || !data?.succeeded || !data?.data) {
    return (
      <div className={cn("w-full", className)}>
        <div className="flex items-center justify-center py-12 bg-gradient-to-br from-red-50 to-red-100 dark:from-red-900/20 dark:to-red-800/20 rounded-xl border border-red-200 dark:border-red-800 shadow-lg">
          <div className="text-center space-y-2">
            <p className="text-sm font-medium text-red-700 dark:text-red-400">Failed to load analytics</p>
            <p className="text-xs text-red-600/80 dark:text-red-500/80">{data?.message || "An error occurred"}</p>
          </div>
        </div>
      </div>
    );
  }

  // Show empty state
  if (data.data.summary.totalNAPs === 0) {
    return (
      <div className={cn("w-full", className)}>
        <div className="flex items-center justify-center py-12 bg-gradient-to-br from-gray-50 to-gray-100 dark:from-gray-800 dark:to-gray-900 rounded-xl border shadow-lg">
          <div className="text-center space-y-3">
            <div className="flex justify-center">
              <div className="rounded-full bg-gray-200 dark:bg-gray-700 p-4">
                <Wifi className="h-8 w-8 text-gray-400" />
              </div>
            </div>
            <p className="text-sm font-medium text-gray-700 dark:text-gray-300">No NAP Data Available</p>
            <p className="text-xs text-gray-500 dark:text-gray-400">Analytics will appear once NAPs are registered</p>
          </div>
        </div>
      </div>
    );
  }

  const analytics = data.data;

  return (
    <div className={cn("w-full", className)}>
      {/* Header with collapse toggle - Enhanced gradient */}
      <motion.div
        whileHover={{ scale: 1.005 }}
        whileTap={{ scale: 0.995 }}
        className={cn(
          "flex items-center justify-between px-5 py-4 rounded-xl cursor-pointer transition-all",
          "bg-gradient-to-r from-violet-600 via-indigo-600 to-blue-600",
          "shadow-lg shadow-indigo-500/25 hover:shadow-xl hover:shadow-indigo-500/30",
          "text-white relative overflow-hidden",
        )}
        onClick={toggleCollapsed}>
        {/* Animated background decoration */}
        <div className="absolute inset-0 overflow-hidden">
          <motion.div
            className="absolute -right-10 -top-10 h-40 w-40 rounded-full bg-white/10"
            animate={{ scale: [1, 1.2, 1], rotate: [0, 90, 0] }}
            transition={{ duration: 10, repeat: Infinity }}
          />
          <motion.div
            className="absolute -left-5 -bottom-5 h-24 w-24 rounded-full bg-white/5"
            animate={{ scale: [1, 1.3, 1] }}
            transition={{ duration: 8, repeat: Infinity }}
          />
        </div>

        <div className="relative z-10 flex items-center gap-3">
          <div className="rounded-lg bg-white/20 p-2 backdrop-blur-sm">
            <Sparkles className="h-5 w-5" />
          </div>
          <div>
            <h2 className="text-base font-bold tracking-tight">NAP Analytics Dashboard</h2>
            <p className="text-xs text-white/70">
              {analytics.summary.totalNAPs.toLocaleString()} NAPs across {analytics.summary.activeProviders} providers
            </p>
          </div>
        </div>

        <div className="relative z-10 flex items-center gap-2">
          <Badge className="bg-white/20 text-white border-0 backdrop-blur-sm">Live Data</Badge>
          <Button variant="ghost" size="sm" className="h-8 w-8 p-0 text-white hover:bg-white/20 rounded-full">
            {isCollapsed ? <ChevronDown className="h-5 w-5" /> : <ChevronUp className="h-5 w-5" />}
          </Button>
        </div>
      </motion.div>

      <AnimatePresence>
        {!isCollapsed && (
          <motion.div
            initial={{ height: 0, opacity: 0 }}
            animate={{ height: "auto", opacity: 1 }}
            exit={{ height: 0, opacity: 0 }}
            transition={{ duration: 0.3, ease: "easeInOut" }}
            className="overflow-hidden">
            <div className="bg-gradient-to-b from-white via-gray-50/50 to-white dark:from-gray-900 dark:via-gray-800/50 dark:to-gray-900 rounded-b-xl border border-t-0 shadow-lg p-5 space-y-5">
              {/* Key Metrics Row - Colorful gradient cards */}
              <div className="grid grid-cols-2 lg:grid-cols-4 gap-3">
                <AnimatedStat
                  value={analytics.summary.totalNAPs}
                  label="Total NAPs"
                  icon={<Wifi className="h-5 w-5 text-white" />}
                  gradient="from-blue-500 to-cyan-500"
                />
                <AnimatedStat
                  value={analytics.summary.activeProviders}
                  label="Active Providers"
                  icon={<Users className="h-5 w-5 text-white" />}
                  gradient="from-purple-500 to-pink-500"
                />
                <AnimatedStat
                  value={analytics.summary.avgNAPsPerProvider}
                  label="Avg NAPs/Provider"
                  icon={<BarChart3 className="h-5 w-5 text-white" />}
                  gradient="from-amber-500 to-orange-500"
                />
                <AnimatedStat
                  value={analytics.summary.densityPerKm2}
                  label="Density (NAPs/km²)"
                  icon={<Target className="h-5 w-5 text-white" />}
                  gradient="from-emerald-500 to-teal-500"
                  suffix=""
                />
              </div>

              {/* Insights Row */}
              <div className="flex flex-wrap gap-2">
                <InsightBadge
                  label="Market"
                  value={analytics.marketAnalysis.market}
                  color={getMarketColor(analytics.marketAnalysis.market)}
                />
                <InsightBadge
                  label="Competition Score"
                  value={`${analytics.marketAnalysis.competitionScore}%`}
                  color={getCompetitionColor(analytics.marketAnalysis.competitionScore)}
                />
                <InsightBadge
                  label="Coverage"
                  value={analytics.marketAnalysis.coverage}
                  color={getCoverageColor(analytics.marketAnalysis.coverage)}
                />
              </div>

              {/* Charts Row */}
              <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
                {/* Enhanced Market Share Donut Chart */}
                <Card className="border-0 shadow-md bg-gradient-to-br from-white to-gray-50 dark:from-gray-800 dark:to-gray-900">
                  <CardHeader className="pb-2 pt-4 px-4">
                    <div className="flex items-center gap-2">
                      <div className="rounded-lg bg-gradient-to-br from-violet-500 to-purple-600 p-1.5">
                        <PieChartIcon className="h-4 w-4 text-white" />
                      </div>
                      <CardTitle className="text-sm font-semibold">Market Share Distribution</CardTitle>
                    </div>
                  </CardHeader>
                  <CardContent className="p-3">
                    <div className="h-48 relative">
                      <ResponsiveContainer width="100%" height="100%">
                        <PieChart>
                          <defs>
                            {analytics.marketShareDistribution.slice(0, 5).map((entry, index) => (
                              <linearGradient
                                key={`gradient-${index}`}
                                id={`pieGradient-${index}`}
                                x1="0"
                                y1="0"
                                x2="0"
                                y2="1">
                                <stop offset="0%" stopColor={entry.providerColor} stopOpacity={1} />
                                <stop offset="100%" stopColor={entry.providerColor} stopOpacity={0.7} />
                              </linearGradient>
                            ))}
                          </defs>
                          <Pie
                            data={analytics.marketShareDistribution.slice(0, 5)}
                            cx="50%"
                            cy="50%"
                            innerRadius={50}
                            outerRadius={75}
                            paddingAngle={3}
                            dataKey="napCount"
                            nameKey="providerName"
                            animationBegin={0}
                            animationDuration={800}>
                            {analytics.marketShareDistribution.slice(0, 5).map((entry, index) => (
                              <Cell
                                key={`cell-${index}`}
                                fill={`url(#pieGradient-${index})`}
                                stroke={entry.providerColor}
                                strokeWidth={2}
                              />
                            ))}
                          </Pie>
                          <Tooltip content={<CustomTooltip />} />
                        </PieChart>
                      </ResponsiveContainer>
                      {/* Central info */}
                      <div className="absolute inset-0 flex items-center justify-center pointer-events-none">
                        <div className="text-center">
                          <p className="text-2xl font-bold bg-gradient-to-r from-violet-600 to-indigo-600 bg-clip-text text-transparent">
                            {analytics.summary.totalNAPs.toLocaleString()}
                          </p>
                          <p className="text-[10px] text-muted-foreground font-medium">Total NAPs</p>
                        </div>
                      </div>
                    </div>
                    {/* Legend */}
                    <div className="flex flex-wrap justify-center gap-2 mt-2">
                      {analytics.marketShareDistribution.slice(0, 5).map((entry, index) => (
                        <div key={index} className="flex items-center gap-1.5">
                          <div
                            className="w-2.5 h-2.5 rounded-full shadow-sm"
                            style={{ backgroundColor: entry.providerColor }}
                          />
                          <span className="text-[10px] text-muted-foreground font-medium">
                            {entry.providerName.length > 10 ? `${entry.providerName.slice(0, 10)}...` : entry.providerName}
                          </span>
                        </div>
                      ))}
                    </div>
                  </CardContent>
                </Card>

                {/* Enhanced Provider Comparison Bar Chart */}
                <Card className="border-0 shadow-md bg-gradient-to-br from-white to-gray-50 dark:from-gray-800 dark:to-gray-900">
                  <CardHeader className="pb-2 pt-4 px-4">
                    <div className="flex items-center gap-2">
                      <div className="rounded-lg bg-gradient-to-br from-blue-500 to-cyan-600 p-1.5">
                        <BarChart3 className="h-4 w-4 text-white" />
                      </div>
                      <CardTitle className="text-sm font-semibold">Provider Comparison</CardTitle>
                    </div>
                  </CardHeader>
                  <CardContent className="p-3">
                    <div className="h-48">
                      <ResponsiveContainer width="100%" height="100%">
                        <BarChart
                          data={analytics.providerComparison.slice(0, 5)}
                          layout="vertical"
                          margin={{ left: 0, right: 10 }}>
                          <defs>
                            {analytics.providerComparison.slice(0, 5).map((entry, index) => (
                              <linearGradient
                                key={`barGradient-${index}`}
                                id={`barGradient-${index}`}
                                x1="0"
                                y1="0"
                                x2="1"
                                y2="0">
                                <stop offset="0%" stopColor={entry.providerColor} stopOpacity={0.8} />
                                <stop offset="100%" stopColor={entry.providerColor} stopOpacity={1} />
                              </linearGradient>
                            ))}
                          </defs>
                          <CartesianGrid strokeDasharray="3 3" horizontal={false} opacity={0.2} />
                          <XAxis
                            type="number"
                            tick={{ fontSize: 10, fill: "#6b7280" }}
                            axisLine={{ stroke: "#e5e7eb" }}
                          />
                          <YAxis
                            type="category"
                            dataKey="providerName"
                            width={70}
                            tick={{ fontSize: 10, fill: "#6b7280" }}
                            tickFormatter={(value) => (value.length > 10 ? `${value.slice(0, 10)}...` : value)}
                            axisLine={{ stroke: "#e5e7eb" }}
                          />
                          <Tooltip content={<CustomTooltip />} />
                          <Bar dataKey="napCount" name="NAPs" radius={[0, 6, 6, 0]} animationDuration={800}>
                            {analytics.providerComparison.slice(0, 5).map((entry, index) => (
                              <Cell key={`cell-${index}`} fill={`url(#barGradient-${index})`} />
                            ))}
                          </Bar>
                        </BarChart>
                      </ResponsiveContainer>
                    </div>
                  </CardContent>
                </Card>
              </div>

              {/* Top Provider Highlight */}
              <motion.div
                initial={{ opacity: 0, y: 10 }}
                animate={{ opacity: 1, y: 0 }}
                transition={{ delay: 0.2 }}
                className="relative overflow-hidden rounded-xl p-4 bg-gradient-to-r from-amber-50 via-orange-50 to-yellow-50 dark:from-amber-900/20 dark:via-orange-900/20 dark:to-yellow-900/20 border border-amber-200/50 dark:border-amber-700/30">
                <div className="absolute right-4 top-1/2 -translate-y-1/2 opacity-10">
                  <Award className="h-20 w-20 text-amber-500" />
                </div>
                <div className="relative z-10 flex items-center gap-4">
                  <div
                    className="h-12 w-12 rounded-xl flex items-center justify-center shadow-lg"
                    style={{ backgroundColor: analytics.marketShareDistribution[0]?.providerColor || "#6b7280" }}>
                    <Award className="h-6 w-6 text-white" />
                  </div>
                  <div className="flex-1">
                    <p className="text-xs text-amber-700 dark:text-amber-300 font-medium">Leading Provider</p>
                    <p className="text-lg font-bold text-gray-900 dark:text-white">{analytics.leadingProvider.providerName}</p>
                  </div>
                  <div className="text-right">
                    <p className="text-2xl font-bold text-amber-600 dark:text-amber-400">
                      {analytics.leadingProvider.marketSharePercentage}%
                    </p>
                    <p className="text-xs text-muted-foreground">market share</p>
                  </div>
                </div>
                <div className="mt-3">
                  <Progress
                    value={analytics.leadingProvider.marketSharePercentage}
                    className="h-2 bg-amber-200/50 dark:bg-amber-800/30"
                  />
                </div>
              </motion.div>

              {/* Coverage info with enhanced styling */}
              <div className="flex items-center justify-between gap-4 text-xs text-muted-foreground bg-gray-50 dark:bg-gray-800/50 rounded-lg px-4 py-3">
                <div className="flex items-center gap-2">
                  <Globe className="h-4 w-4 text-blue-500" />
                  <span className="font-medium">Geographic Coverage:</span>
                  <span>{analytics.geographicCoverage.coverageString}</span>
                </div>
                <Badge variant="outline" className="text-[10px]">
                  ~{analytics.geographicCoverage.totalAreaKm2.toLocaleString()} km²
                </Badge>
              </div>
            </div>
          </motion.div>
        )}
      </AnimatePresence>
    </div>
  );
}

export default NAPAnalyticsPanel;
