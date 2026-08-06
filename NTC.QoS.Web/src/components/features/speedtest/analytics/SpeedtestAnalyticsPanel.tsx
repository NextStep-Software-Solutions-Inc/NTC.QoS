"use client";

import React, { useCallback, useEffect, useMemo, useState } from "react";
import { AnimatePresence, motion } from "framer-motion";
import {
  Activity,
  Award,
  BarChart3,
  ChevronDown,
  ChevronUp,
  Download,
  Gauge,
  Loader2,
  Signal,
  Sparkles,
  Upload,
  Users,
  AlertCircle,
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
import { cn } from "@/lib/utils";
import { SIGNAL_QUALITY_CONFIG } from "../types";
import { useSpeedtestAnalytics } from "./hooks/useSpeedtestAnalytics";
import type { SpeedtestAnalyticsQueryParams } from "./types/analytics-api";

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
  gradient = "from-cyan-500 to-blue-600",
  suffix = "",
  trend,
  className,
}: AnimatedStatProps) {
  const [displayValue, setDisplayValue] = useState(0);

  useEffect(() => {
    if (typeof value !== "number") return;

    const duration = 1000;
    const steps = 30;
    const increment = value / steps;
    let current = 0;
    let step = 0;

    const timer = setInterval(() => {
      step++;
      current = Math.min(Math.round(increment * step * 10) / 10, value);
      setDisplayValue(current);

      if (step >= steps) {
        clearInterval(timer);
        setDisplayValue(value);
      }
    }, duration / steps);

    return () => clearInterval(timer);
  }, [value]);

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
              {typeof value === "number" ? displayValue.toLocaleString(undefined, { maximumFractionDigits: 1 }) : value}
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

// Quality badge component
function QualityBadge({
  label,
  value,
  color,
}: {
  label: string;
  value: string;
  color: "cyan" | "green" | "amber" | "red" | "purple" | "blue";
}) {
  const colorClasses = {
    cyan: "bg-cyan-100 text-cyan-700 dark:bg-cyan-900/30 dark:text-cyan-300",
    green: "bg-emerald-100 text-emerald-700 dark:bg-emerald-900/30 dark:text-emerald-300",
    amber: "bg-amber-100 text-amber-700 dark:bg-amber-900/30 dark:text-amber-300",
    red: "bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-300",
    purple: "bg-purple-100 text-purple-700 dark:bg-purple-900/30 dark:text-purple-300",
    blue: "bg-blue-100 text-blue-700 dark:bg-blue-900/30 dark:text-blue-300",
  };

  return (
    <div className={cn("flex items-center gap-2 rounded-lg px-3 py-2", colorClasses[color])}>
      <span className="text-xs font-medium">{label}:</span>
      <span className="text-xs font-bold">{value}</span>
    </div>
  );
}

interface SpeedtestAnalyticsPanelProps {
  queryParams?: SpeedtestAnalyticsQueryParams;
  isCollapsed?: boolean;
  onCollapsedChange?: (collapsed: boolean) => void;
  className?: string;
}

// Custom tooltip for charts
const CustomTooltip = ({ active, payload, label }: any) => {
  if (active && payload && payload.length) {
    return (
      <div className="rounded-lg border bg-white/95 dark:bg-gray-900/95 backdrop-blur-sm px-3 py-2 shadow-xl">
        <p className="text-xs font-semibold text-gray-900 dark:text-gray-100">
          {payload[0]?.payload?.providerName || label}
        </p>
        {payload.map((p: any, i: number) => (
          <p key={i} className="text-sm" style={{ color: p.color }}>
            {p.name}: {p.value?.toFixed?.(1) || p.value}
          </p>
        ))}
      </div>
    );
  }
  return null;
};

export function SpeedtestAnalyticsPanel({
  queryParams,
  isCollapsed = false,
  onCollapsedChange,
  className,
}: SpeedtestAnalyticsPanelProps) {
  // Fetch analytics from API
  const { data, isLoading, isFetching, isError, error } = useSpeedtestAnalytics({
    pathParams: {},
    queryParams: queryParams || {},
  });

  const toggleCollapsed = useCallback(() => {
    onCollapsedChange?.(!isCollapsed);
  }, [isCollapsed, onCollapsedChange]);

  // Map API response to quality distribution data for pie chart
  const qualityDistribution = useMemo(() => {
    if (!data?.data?.signalQualityDistribution) return [];
    const dist = data.data.signalQualityDistribution;
    return [
      {
        quality: "Excellent" as const,
        count: dist.excellentCount,
        percentage: dist.excellentPercent,
        color: SIGNAL_QUALITY_CONFIG.Excellent.color,
      },
      {
        quality: "Good" as const,
        count: dist.goodCount,
        percentage: dist.goodPercent,
        color: SIGNAL_QUALITY_CONFIG.Good.color,
      },
      {
        quality: "Average" as const,
        count: dist.averageCount,
        percentage: dist.averagePercent,
        color: SIGNAL_QUALITY_CONFIG.Average.color,
      },
      {
        quality: "Weak" as const,
        count: dist.weakCount,
        percentage: dist.weakPercent,
        color: SIGNAL_QUALITY_CONFIG.Weak.color,
      },
      {
        quality: "VeryWeak" as const,
        count: dist.veryWeakCount,
        percentage: dist.veryWeakPercent,
        color: SIGNAL_QUALITY_CONFIG.VeryWeak.color,
      },
    ].filter((item) => item.count > 0);
  }, [data]);

  // Loading state
  if (isLoading) {
    return (
      <div className={cn("w-full", className)}>
        <div className="flex items-center justify-center gap-3 rounded-xl bg-gradient-to-r from-cyan-600 via-blue-600 to-purple-600 px-5 py-8 text-white shadow-lg">
          <Loader2 className="h-6 w-6 animate-spin" />
          <span className="text-sm font-medium">Loading analytics...</span>
        </div>
      </div>
    );
  }

  // Error state
  if (isError || !data?.succeeded) {
    return (
      <div className={cn("w-full", className)}>
        <div className="flex items-center gap-3 rounded-xl border border-red-200 bg-red-50 px-5 py-4 text-red-800 dark:border-red-800 dark:bg-red-950/30 dark:text-red-200">
          <AlertCircle className="h-5 w-5" />
          <div>
            <p className="text-sm font-medium">Failed to load analytics</p>
            <p className="text-xs text-red-600 dark:text-red-400">
              {data?.message || (error as Error)?.message || "An error occurred while fetching analytics data"}
            </p>
          </div>
        </div>
      </div>
    );
  }

  // No data
  if (!data?.data || data.data.overview.totalTests === 0) {
    return null;
  }

  const { overview, providerPerformance, providerRankings } = data.data;

  // Track if we're refetching (for loading overlay)
  const isRefetching = isFetching && !isLoading;

  return (
    <div className={cn("w-full", className)}>
      {/* Header with collapse toggle */}
      <motion.div
        whileHover={{ scale: 1.005 }}
        whileTap={{ scale: 0.995 }}
        className={cn(
          "flex items-center justify-between px-5 py-4 rounded-xl cursor-pointer transition-all",
          "bg-gradient-to-r from-cyan-600 via-blue-600 to-purple-600",
          "shadow-lg shadow-cyan-500/25 hover:shadow-xl hover:shadow-cyan-500/30",
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
            <Activity className="h-5 w-5" />
          </div>
          <div>
            <h2 className="text-base font-bold tracking-tight">Speedtest Analytics Dashboard</h2>
            <p className="text-xs text-white/70">
              {overview.totalTests.toLocaleString()} tests across {overview.providerCount} providers
            </p>
          </div>
        </div>

        <div className="relative z-10 flex items-center gap-2">
          {isRefetching && (
            <div className="flex items-center gap-2 px-3 py-1 rounded-lg bg-white/20 backdrop-blur-sm">
              <Loader2 className="h-3.5 w-3.5 animate-spin" />
              <span className="text-xs font-medium">Updating...</span>
            </div>
          )}
          <Badge className="bg-white/20 text-white border-0 backdrop-blur-sm">
            Avg: {overview.averageDownloadMbps.toFixed(1)} Mbps
          </Badge>
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
            <div className="bg-gradient-to-b from-white via-gray-50/50 to-white dark:from-gray-900 dark:via-gray-800/50 dark:to-gray-900 rounded-b-xl border border-t-0 shadow-lg p-5 space-y-5 relative">
              {/* Refetching overlay */}
              {isRefetching && (
                <motion.div
                  initial={{ opacity: 0 }}
                  animate={{ opacity: 1 }}
                  exit={{ opacity: 0 }}
                  className="absolute inset-0 bg-white/60 dark:bg-gray-900/60 backdrop-blur-sm z-50 rounded-b-xl flex items-center justify-center">
                  <div className="flex items-center gap-3 bg-white dark:bg-gray-800 px-5 py-3 rounded-lg shadow-lg border">
                    <Loader2 className="h-5 w-5 animate-spin text-cyan-600" />
                    <span className="text-sm font-medium text-gray-900 dark:text-gray-100">Refreshing analytics...</span>
                  </div>
                </motion.div>
              )}
              {/* Key Metrics Row */}
              <div className="grid grid-cols-2 lg:grid-cols-5 gap-3">
                <AnimatedStat
                  value={overview.totalTests}
                  label="Total Tests"
                  icon={<Activity className="h-5 w-5 text-white" />}
                  gradient="from-cyan-500 to-blue-500"
                />
                <AnimatedStat
                  value={overview.averageDownloadMbps}
                  label="Avg Download"
                  suffix="Mbps"
                  icon={<Download className="h-5 w-5 text-white" />}
                  gradient="from-green-500 to-emerald-500"
                />
                <AnimatedStat
                  value={overview.averageUploadMbps}
                  label="Avg Upload"
                  suffix="Mbps"
                  icon={<Upload className="h-5 w-5 text-white" />}
                  gradient="from-blue-500 to-indigo-500"
                />
                <AnimatedStat
                  value={overview.averagePingMs}
                  label="Avg Ping"
                  suffix="ms"
                  icon={<Gauge className="h-5 w-5 text-white" />}
                  gradient="from-orange-500 to-amber-500"
                />
                <AnimatedStat
                  value={overview.providerCount}
                  label="Providers"
                  icon={<Users className="h-5 w-5 text-white" />}
                  gradient="from-purple-500 to-pink-500"
                />
              </div>

              {/* Insights Row */}
              <div className="flex flex-wrap gap-2">
                <QualityBadge
                  label="Performance"
                  value={overview.performanceRating.charAt(0).toUpperCase() + overview.performanceRating.slice(1)}
                  color={
                    overview.performanceRating.toLowerCase() === "excellent"
                      ? "green"
                      : overview.performanceRating.toLowerCase() === "good"
                        ? "cyan"
                        : overview.performanceRating.toLowerCase() === "average"
                          ? "amber"
                          : "red"
                  }
                />
                <QualityBadge label="Best Provider" value={overview.bestProvider} color="green" />
                <QualityBadge
                  label="Competition"
                  value={overview.competitionLevel.charAt(0).toUpperCase() + overview.competitionLevel.slice(1)}
                  color={
                    overview.competitionLevel.toLowerCase() === "high"
                      ? "green"
                      : overview.competitionLevel.toLowerCase() === "medium"
                        ? "amber"
                        : "red"
                  }
                />
                <QualityBadge label="Coverage" value={`${overview.coverageAreaKm2.toFixed(0)} km²`} color="blue" />
              </div>

              {/* Charts Row */}
              <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
                {/* Provider Performance Bar Chart - NAP Style */}
                <Card className="border-0 shadow-md bg-gradient-to-br from-white to-gray-50 dark:from-gray-800 dark:to-gray-900">
                  <CardHeader className="pb-2 pt-4 px-4">
                    <div className="flex items-center gap-2">
                      <div className="rounded-lg bg-gradient-to-br from-cyan-500 to-blue-600 p-1.5">
                        <BarChart3 className="h-4 w-4 text-white" />
                      </div>
                      <CardTitle className="text-sm font-semibold">Provider Performance</CardTitle>
                    </div>
                  </CardHeader>
                  <CardContent className="p-3">
                    <div className="h-48">
                      <ResponsiveContainer width="100%" height="100%">
                        <BarChart
                          data={providerPerformance.slice(0, 5)}
                          layout="vertical"
                          margin={{ left: 0, right: 10 }}>
                          <defs>
                            {providerPerformance.slice(0, 5).map((entry: typeof providerPerformance[0], index: number) => (
                              <linearGradient
                                key={`providerGradient-${index}`}
                                id={`providerGradient-${index}`}
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
                            tickFormatter={(v) => `${v} Mbps`}
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
                          <Bar
                            dataKey="averageDownloadMbps"
                            name="Download Speed"
                            radius={[0, 6, 6, 0]}
                            animationDuration={800}>
                            {providerPerformance.slice(0, 5).map((_: typeof providerPerformance[0], index: number) => (
                              <Cell key={`cell-${index}`} fill={`url(#providerGradient-${index})`} />
                            ))}
                          </Bar>
                        </BarChart>
                      </ResponsiveContainer>
                    </div>
                    {/* Legend */}
                    <div className="flex flex-wrap justify-center gap-2 mt-2">
                      {providerPerformance.slice(0, 5).map((entry: typeof providerPerformance[0], index: number) => (
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

                {/* Signal Quality Distribution */}
                <Card className="border-0 shadow-md bg-gradient-to-br from-white to-gray-50 dark:from-gray-800 dark:to-gray-900">
                  <CardHeader className="pb-2 pt-4 px-4">
                    <div className="flex items-center gap-2">
                      <div className="rounded-lg bg-gradient-to-br from-purple-500 to-pink-600 p-1.5">
                        <Signal className="h-4 w-4 text-white" />
                      </div>
                      <CardTitle className="text-sm font-semibold">Signal Quality Distribution</CardTitle>
                    </div>
                  </CardHeader>
                  <CardContent className="p-3">
                    <div className="h-48 relative">
                      <ResponsiveContainer width="100%" height="100%">
                        <PieChart>
                          <defs>
                            {qualityDistribution.map((entry, index) => (
                              <linearGradient
                                key={`gradient-${index}`}
                                id={`qualityGradient-${index}`}
                                x1="0"
                                y1="0"
                                x2="0"
                                y2="1">
                                <stop offset="0%" stopColor={entry.color} stopOpacity={1} />
                                <stop offset="100%" stopColor={entry.color} stopOpacity={0.7} />
                              </linearGradient>
                            ))}
                          </defs>
                          <Pie
                            data={qualityDistribution}
                            cx="50%"
                            cy="50%"
                            innerRadius={50}
                            outerRadius={75}
                            paddingAngle={3}
                            dataKey="count"
                            nameKey="quality"
                            animationBegin={0}
                            animationDuration={800}>
                            {qualityDistribution.map((entry, index) => (
                              <Cell
                                key={`cell-${index}`}
                                fill={`url(#qualityGradient-${index})`}
                                stroke={entry.color}
                                strokeWidth={2}
                              />
                            ))}
                          </Pie>
                          <Tooltip content={<CustomTooltip />} />
                        </PieChart>
                      </ResponsiveContainer>
                      {/* Center label */}
                      <div className="absolute inset-0 flex items-center justify-center pointer-events-none">
                        <div className="text-center">
                          <p className="text-2xl font-bold bg-gradient-to-r from-purple-600 to-pink-600 bg-clip-text text-transparent">
                            {overview.totalTests}
                          </p>
                          <p className="text-[10px] text-muted-foreground font-medium">tests</p>
                        </div>
                      </div>
                    </div>
                    {/* Legend */}
                    <div className="flex flex-wrap justify-center gap-2 mt-2">
                      {qualityDistribution.map((item) => (
                        <div key={item.quality} className="flex items-center gap-1.5">
                          <div className="w-2.5 h-2.5 rounded-full shadow-sm" style={{ backgroundColor: item.color }} />
                          <span className="text-[10px] text-muted-foreground font-medium">
                            {SIGNAL_QUALITY_CONFIG[item.quality].label} ({item.percentage}%)
                          </span>
                        </div>
                      ))}
                    </div>
                  </CardContent>
                </Card>
              </div>

              {/* Provider Comparison Table - Full Width with 2-column grid */}
              <Card className="border-0 shadow-md bg-gradient-to-br from-white to-gray-50 dark:from-gray-800 dark:to-gray-900">
                <CardHeader className="pb-2 pt-4 px-4">
                  <div className="flex items-center justify-between">
                    <div className="flex items-center gap-2">
                      <div className="rounded-lg bg-gradient-to-br from-emerald-500 to-teal-600 p-1.5">
                        <Award className="h-4 w-4 text-white" />
                      </div>
                      <CardTitle className="text-sm font-semibold">Provider Rankings</CardTitle>
                    </div>
                    <Badge variant="outline" className="text-xs">
                      Top {Math.min(providerRankings.length, 8)} Providers
                    </Badge>
                  </div>
                </CardHeader>
                <CardContent className="p-4">
                  <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
                    {providerRankings.slice(0, 8).map((provider: typeof providerRankings[0], index: number) => (
                      <motion.div
                        key={provider.providerName}
                        initial={{ opacity: 0, x: -20 }}
                        animate={{ opacity: 1, x: 0 }}
                        transition={{ delay: index * 0.05 }}
                        className={cn(
                          "flex items-center gap-3 p-3 rounded-xl transition-all hover:scale-[1.02]",
                          index < 3
                            ? "bg-gradient-to-r from-emerald-50 to-teal-50 dark:from-emerald-950/30 dark:to-teal-950/30 border border-emerald-200/50 dark:border-emerald-800/50"
                            : "bg-gray-50/80 dark:bg-gray-800/50 border border-gray-100 dark:border-gray-700/50",
                        )}>
                        {/* Rank Badge */}
                        <div
                          className={cn(
                            "flex items-center justify-center h-10 w-10 rounded-xl text-white text-sm font-bold shadow-lg",
                            index === 0 && "bg-gradient-to-br from-yellow-400 to-amber-500",
                            index === 1 && "bg-gradient-to-br from-gray-300 to-gray-400",
                            index === 2 && "bg-gradient-to-br from-orange-400 to-orange-600",
                            index > 2 && "bg-gradient-to-br from-gray-500 to-gray-600",
                          )}
                          style={index > 2 ? { backgroundColor: provider.providerColor } : undefined}>
                          {index === 0 ? "🥇" : index === 1 ? "🥈" : index === 2 ? "🥉" : provider.rank}
                        </div>

                        {/* Provider Info */}
                        <div className="flex-1 min-w-0">
                          <div className="flex items-center gap-2">
                            <p className="text-sm font-semibold truncate">{provider.providerName}</p>
                            {provider.badge && (
                              <Badge className="text-[9px] bg-emerald-500 text-white">{provider.badge}</Badge>
                            )}
                          </div>
                          <div className="flex items-center gap-3 mt-1">
                            <span className="text-xs text-muted-foreground">{provider.testCount} tests</span>
                            <span className="text-xs text-muted-foreground">•</span>
                            <span className="text-xs text-muted-foreground">
                              {provider.averagePingMs.toFixed(0)}ms ping
                            </span>
                          </div>
                        </div>

                        {/* Speed Stats */}
                        <div className="flex flex-col items-end gap-1">
                          <div className="flex items-center gap-1.5">
                            <Download className="h-3.5 w-3.5 text-green-500" />
                            <span className="text-sm font-bold text-green-600 dark:text-green-400">
                              {provider.downloadMbps.toFixed(1)}
                            </span>
                            <span className="text-[10px] text-muted-foreground">Mbps</span>
                          </div>
                          <div className="flex items-center gap-1.5">
                            <Upload className="h-3 w-3 text-blue-500" />
                            <span className="text-xs font-medium text-blue-600 dark:text-blue-400">
                              {provider.uploadMbps.toFixed(1)}
                            </span>
                            <span className="text-[10px] text-muted-foreground">Mbps</span>
                          </div>
                        </div>

                        {/* Quality Score Bar */}
                        <div className="w-20 flex flex-col items-center">
                          <div className="w-full h-2 bg-gray-200 dark:bg-gray-700 rounded-full overflow-hidden">
                            <motion.div
                              className="h-full rounded-full"
                              style={{
                                backgroundColor:
                                  provider.score >= 70 ? "#10b981" : provider.score >= 50 ? "#f59e0b" : "#ef4444",
                              }}
                              initial={{ width: 0 }}
                              animate={{ width: `${provider.score}%` }}
                              transition={{ delay: index * 0.05 + 0.2, duration: 0.5 }}
                            />
                          </div>
                          <span className="text-[10px] text-muted-foreground mt-0.5">Score: {provider.score}</span>
                        </div>
                      </motion.div>
                    ))}
                  </div>
                </CardContent>
              </Card>

            </div>
          </motion.div>
        )}
      </AnimatePresence>
    </div>
  );
}

export default SpeedtestAnalyticsPanel;
