"use client";

import React, { useMemo } from "react";
import { Activity, BarChart3, Download, Gauge, Minus, Signal, TrendingDown, TrendingUp, Upload } from "lucide-react";
import {
  Area,
  AreaChart,
  Bar,
  BarChart,
  CartesianGrid,
  Cell,
  Legend,
  Line,
  LineChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from "recharts";

import { type SpeedTest } from "@/components/features/speedtest/types/SpeedTest";
import { Badge } from "@/components/ui/badge";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";

interface ProviderPerformanceChartProps {
  data: SpeedTest[];
  className?: string;
}

interface ProviderStats {
  provider: string;
  color: string;
  avgDownload: number;
  avgUpload: number;
  avgPing: number;
  testCount: number;
  maxDownload: number;
  maxUpload: number;
  minPing: number;
}

// Provider colors for consistent styling
const PROVIDER_COLORS = [
  "#06b6d4", // cyan
  "#3b82f6", // blue
  "#8b5cf6", // violet
  "#ec4899", // pink
  "#f59e0b", // amber
  "#10b981", // emerald
  "#ef4444", // red
  "#6366f1", // indigo
];

// Custom tooltip component
function CustomTooltip({
  active,
  payload,
  label,
}: {
  active?: boolean;
  payload?: Array<{ name: string; value: number; color: string; dataKey: string }>;
  label?: string;
}) {
  if (!active || !payload || payload.length === 0) return null;

  return (
    <div className="bg-white dark:bg-gray-800 border rounded-lg shadow-lg p-3 min-w-[180px]">
      <p className="text-sm font-semibold mb-2 border-b pb-1">{label}</p>
      <div className="space-y-1.5">
        {payload.map((entry, index) => (
          <div key={index} className="flex items-center justify-between gap-3 text-xs">
            <div className="flex items-center gap-1.5">
              <div className="w-2 h-2 rounded-full" style={{ backgroundColor: entry.color }} />
              <span className="text-muted-foreground">{entry.name}:</span>
            </div>
            <span className="font-medium">
              {entry.dataKey.includes("ping") ? `${entry.value.toFixed(0)} ms` : `${entry.value.toFixed(1)} Mbps`}
            </span>
          </div>
        ))}
      </div>
    </div>
  );
}

export function ProviderPerformanceChart({ data, className }: ProviderPerformanceChartProps) {
  // Calculate provider statistics
  const providerStats = useMemo(() => {
    const stats = new Map<string, { color: string; downloads: number[]; uploads: number[]; pings: number[] }>();

    data.forEach((st) => {
      const providerName = st.serviceProvider?.provider?.name || "Unknown";
      const providerColor = st.serviceProvider?.provider?.color || "#6b7280";

      if (!stats.has(providerName)) {
        stats.set(providerName, { color: providerColor, downloads: [], uploads: [], pings: [] });
      }

      const providerData = stats.get(providerName)!;
      if (st.downloadSpeed) providerData.downloads.push(st.downloadSpeed);
      if (st.uploadSpeed) providerData.uploads.push(st.uploadSpeed);
      if (st.ping) providerData.pings.push(st.ping);
    });

    const result: ProviderStats[] = [];
    stats.forEach((value, key) => {
      const avgDownload =
        value.downloads.length > 0 ? value.downloads.reduce((a, b) => a + b, 0) / value.downloads.length : 0;
      const avgUpload = value.uploads.length > 0 ? value.uploads.reduce((a, b) => a + b, 0) / value.uploads.length : 0;
      const avgPing = value.pings.length > 0 ? value.pings.reduce((a, b) => a + b, 0) / value.pings.length : 0;

      result.push({
        provider: key,
        color: value.color,
        avgDownload,
        avgUpload,
        avgPing,
        testCount: value.downloads.length,
        maxDownload: Math.max(...value.downloads, 0),
        maxUpload: Math.max(...value.uploads, 0),
        minPing: Math.min(...value.pings, Infinity) === Infinity ? 0 : Math.min(...value.pings),
      });
    });

    // Sort by test count descending
    return result.sort((a, b) => b.testCount - a.testCount);
  }, [data]);

  // Prepare data for line chart (time series simulation)
  const timeSeriesData = useMemo(() => {
    // Group speedtests by date
    const dateGroups = new Map<string, Map<string, number[]>>();

    data.forEach((st) => {
      const date = st.dateTime
        ? new Date(st.dateTime).toLocaleDateString("en-US", { month: "short", day: "numeric" })
        : "Unknown";
      const provider = st.serviceProvider?.provider?.name || "Unknown";

      if (!dateGroups.has(date)) {
        dateGroups.set(date, new Map());
      }
      const providerMap = dateGroups.get(date)!;
      if (!providerMap.has(provider)) {
        providerMap.set(provider, []);
      }
      if (st.downloadSpeed) {
        providerMap.get(provider)!.push(st.downloadSpeed);
      }
    });

    // Convert to array format for recharts
    type ChartDataPoint = { date: string; [key: string]: string | number };
    const result: ChartDataPoint[] = [];
    const sortedDates = Array.from(dateGroups.keys())
      .sort((a, b) => {
        const dateA = new Date(a);
        const dateB = new Date(b);
        return dateA.getTime() - dateB.getTime();
      })
      .slice(-10); // Last 10 dates

    sortedDates.forEach((date) => {
      const providerMap = dateGroups.get(date)!;
      const entry: ChartDataPoint = { date };

      providerMap.forEach((speeds, provider) => {
        entry[provider] = speeds.reduce((a, b) => a + b, 0) / speeds.length;
      });

      result.push(entry);
    });

    return result;
  }, [data]);

  // Get top providers for line chart (max 5)
  const topProviders = providerStats.slice(0, 5);

  // Get trend for top provider
  const getTrend = (provider: ProviderStats) => {
    const providerData = timeSeriesData.map((d) => {
      const val = d[provider.provider];
      return typeof val === "number" ? val : 0;
    });
    if (providerData.length < 2) return "neutral";
    const firstHalf = providerData.slice(0, Math.floor(providerData.length / 2));
    const secondHalf = providerData.slice(Math.floor(providerData.length / 2));
    const firstAvg = firstHalf.reduce((a, b) => a + b, 0) / firstHalf.length || 0;
    const secondAvg = secondHalf.reduce((a, b) => a + b, 0) / secondHalf.length || 0;
    if (secondAvg > firstAvg * 1.05) return "up";
    if (secondAvg < firstAvg * 0.95) return "down";
    return "neutral";
  };

  if (data.length === 0) {
    return (
      <Card className={className}>
        <CardContent className="flex items-center justify-center h-64">
          <div className="text-center text-muted-foreground">
            <BarChart3 className="h-12 w-12 mx-auto mb-2 opacity-50" />
            <p>No speedtest data available for chart view</p>
          </div>
        </CardContent>
      </Card>
    );
  }

  return (
    <div className={className}>
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
        {/* Download Speed Line Chart */}
        <Card className="border-0 shadow-md bg-gradient-to-br from-white to-gray-50 dark:from-gray-800 dark:to-gray-900">
          <CardHeader className="pb-2 pt-4 px-4">
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-2">
                <div className="rounded-lg bg-gradient-to-br from-cyan-500 to-blue-600 p-1.5">
                  <TrendingUp className="h-4 w-4 text-white" />
                </div>
                <CardTitle className="text-sm font-semibold">Download Speed Trend</CardTitle>
              </div>
              <Badge variant="outline" className="text-[10px]">
                Last 10 days
              </Badge>
            </div>
          </CardHeader>
          <CardContent className="p-3">
            <div className="h-64">
              <ResponsiveContainer width="100%" height="100%">
                <LineChart data={timeSeriesData} margin={{ top: 10, right: 10, left: 0, bottom: 0 }}>
                  <defs>
                    {topProviders.map((provider, index) => (
                      <linearGradient
                        key={`gradient-${index}`}
                        id={`lineGradient-${index}`}
                        x1="0"
                        y1="0"
                        x2="0"
                        y2="1">
                        <stop offset="0%" stopColor={provider.color} stopOpacity={1} />
                        <stop offset="100%" stopColor={provider.color} stopOpacity={0.6} />
                      </linearGradient>
                    ))}
                  </defs>
                  <CartesianGrid strokeDasharray="3 3" opacity={0.2} />
                  <XAxis dataKey="date" tick={{ fontSize: 10, fill: "#6b7280" }} axisLine={{ stroke: "#e5e7eb" }} />
                  <YAxis
                    tick={{ fontSize: 10, fill: "#6b7280" }}
                    axisLine={{ stroke: "#e5e7eb" }}
                    tickFormatter={(v) => `${v}`}
                    label={{ value: "Mbps", angle: -90, position: "insideLeft", fontSize: 10, fill: "#6b7280" }}
                  />
                  <Tooltip content={<CustomTooltip />} />
                  <Legend
                    wrapperStyle={{ fontSize: "10px" }}
                    formatter={(value) => <span className="text-xs">{value}</span>}
                  />
                  {topProviders.map((provider, index) => (
                    <Line
                      key={provider.provider}
                      type="monotone"
                      dataKey={provider.provider}
                      name={provider.provider}
                      stroke={provider.color}
                      strokeWidth={2}
                      dot={{ fill: provider.color, strokeWidth: 0, r: 3 }}
                      activeDot={{ r: 5, stroke: provider.color, strokeWidth: 2 }}
                    />
                  ))}
                </LineChart>
              </ResponsiveContainer>
            </div>
          </CardContent>
        </Card>

        {/* Provider Comparison Bar Chart */}
        <Card className="border-0 shadow-md bg-gradient-to-br from-white to-gray-50 dark:from-gray-800 dark:to-gray-900">
          <CardHeader className="pb-2 pt-4 px-4">
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-2">
                <div className="rounded-lg bg-gradient-to-br from-emerald-500 to-teal-600 p-1.5">
                  <BarChart3 className="h-4 w-4 text-white" />
                </div>
                <CardTitle className="text-sm font-semibold">Provider Comparison</CardTitle>
              </div>
              <Badge variant="outline" className="text-[10px]">
                Top {topProviders.length}
              </Badge>
            </div>
          </CardHeader>
          <CardContent className="p-3">
            <div className="h-64">
              <ResponsiveContainer width="100%" height="100%">
                <BarChart data={topProviders} layout="vertical" margin={{ top: 10, right: 10, left: 0, bottom: 0 }}>
                  <defs>
                    {topProviders.map((provider, index) => (
                      <linearGradient
                        key={`barGradient-${index}`}
                        id={`barGradient-${index}`}
                        x1="0"
                        y1="0"
                        x2="1"
                        y2="0">
                        <stop offset="0%" stopColor={provider.color} stopOpacity={0.8} />
                        <stop offset="100%" stopColor={provider.color} stopOpacity={1} />
                      </linearGradient>
                    ))}
                  </defs>
                  <CartesianGrid strokeDasharray="3 3" horizontal={false} opacity={0.2} />
                  <XAxis type="number" tick={{ fontSize: 10, fill: "#6b7280" }} axisLine={{ stroke: "#e5e7eb" }} />
                  <YAxis
                    type="category"
                    dataKey="provider"
                    width={80}
                    tick={{ fontSize: 10, fill: "#6b7280" }}
                    tickFormatter={(v) => (v.length > 12 ? `${v.slice(0, 12)}...` : v)}
                    axisLine={{ stroke: "#e5e7eb" }}
                  />
                  <Tooltip content={<CustomTooltip />} />
                  <Bar dataKey="avgDownload" name="Download (Mbps)" radius={[0, 6, 6, 0]} animationDuration={800}>
                    {topProviders.map((entry, index) => (
                      <Cell key={`cell-${index}`} fill={`url(#barGradient-${index})`} />
                    ))}
                  </Bar>
                </BarChart>
              </ResponsiveContainer>
            </div>
          </CardContent>
        </Card>

        {/* Upload Speed Area Chart */}
        <Card className="border-0 shadow-md bg-gradient-to-br from-white to-gray-50 dark:from-gray-800 dark:to-gray-900">
          <CardHeader className="pb-2 pt-4 px-4">
            <div className="flex items-center gap-2">
              <div className="rounded-lg bg-gradient-to-br from-emerald-500 to-green-600 p-1.5">
                <Upload className="h-4 w-4 text-white" />
              </div>
              <CardTitle className="text-sm font-semibold">Upload Speed Comparison</CardTitle>
            </div>
          </CardHeader>
          <CardContent className="p-3">
            <div className="h-64">
              <ResponsiveContainer width="100%" height="100%">
                <AreaChart data={timeSeriesData} margin={{ top: 10, right: 10, left: 0, bottom: 0 }}>
                  <defs>
                    {topProviders.map((provider, index) => (
                      <linearGradient
                        key={`areaGradient-${index}`}
                        id={`areaGradient-${index}`}
                        x1="0"
                        y1="0"
                        x2="0"
                        y2="1">
                        <stop offset="0%" stopColor={provider.color} stopOpacity={0.4} />
                        <stop offset="100%" stopColor={provider.color} stopOpacity={0.05} />
                      </linearGradient>
                    ))}
                  </defs>
                  <CartesianGrid strokeDasharray="3 3" opacity={0.2} />
                  <XAxis dataKey="date" tick={{ fontSize: 10, fill: "#6b7280" }} axisLine={{ stroke: "#e5e7eb" }} />
                  <YAxis
                    tick={{ fontSize: 10, fill: "#6b7280" }}
                    axisLine={{ stroke: "#e5e7eb" }}
                    label={{ value: "Mbps", angle: -90, position: "insideLeft", fontSize: 10, fill: "#6b7280" }}
                  />
                  <Tooltip content={<CustomTooltip />} />
                  <Legend
                    wrapperStyle={{ fontSize: "10px" }}
                    formatter={(value) => <span className="text-xs">{value}</span>}
                  />
                  {topProviders.map((provider, index) => (
                    <Area
                      key={provider.provider}
                      type="monotone"
                      dataKey={provider.provider}
                      name={provider.provider}
                      stroke={provider.color}
                      strokeWidth={2}
                      fill={`url(#areaGradient-${index})`}
                    />
                  ))}
                </AreaChart>
              </ResponsiveContainer>
            </div>
          </CardContent>
        </Card>

        {/* Provider Stats Cards */}
        <Card className="border-0 shadow-md bg-gradient-to-br from-white to-gray-50 dark:from-gray-800 dark:to-gray-900">
          <CardHeader className="pb-2 pt-4 px-4">
            <div className="flex items-center gap-2">
              <div className="rounded-lg bg-gradient-to-br from-purple-500 to-pink-600 p-1.5">
                <Activity className="h-4 w-4 text-white" />
              </div>
              <CardTitle className="text-sm font-semibold">Provider Statistics</CardTitle>
            </div>
          </CardHeader>
          <CardContent className="p-3">
            <div className="grid grid-cols-1 gap-2 max-h-64 overflow-auto">
              {topProviders.map((provider, index) => {
                const trend = getTrend(provider);
                return (
                  <div
                    key={provider.provider}
                    className="flex items-center gap-3 p-2.5 rounded-lg bg-gray-50 dark:bg-gray-800/50 hover:bg-gray-100 dark:hover:bg-gray-700/50 transition-colors">
                    <div className="w-1 h-10 rounded-full" style={{ backgroundColor: provider.color }} />
                    <div className="flex-1 min-w-0">
                      <div className="flex items-center gap-2">
                        <p className="text-sm font-medium truncate">{provider.provider}</p>
                        {trend === "up" && <TrendingUp className="h-3 w-3 text-green-500" />}
                        {trend === "down" && <TrendingDown className="h-3 w-3 text-red-500" />}
                        {trend === "neutral" && <Minus className="h-3 w-3 text-gray-400" />}
                      </div>
                      <p className="text-[10px] text-muted-foreground">{provider.testCount} tests</p>
                    </div>
                    <div className="flex items-center gap-3 text-xs">
                      <div className="text-center">
                        <div className="flex items-center gap-1 text-cyan-600">
                          <Download className="h-3 w-3" />
                          <span className="font-medium">{provider.avgDownload.toFixed(1)}</span>
                        </div>
                        <p className="text-[9px] text-muted-foreground">Mbps</p>
                      </div>
                      <div className="text-center">
                        <div className="flex items-center gap-1 text-emerald-600">
                          <Upload className="h-3 w-3" />
                          <span className="font-medium">{provider.avgUpload.toFixed(1)}</span>
                        </div>
                        <p className="text-[9px] text-muted-foreground">Mbps</p>
                      </div>
                      <div className="text-center">
                        <div className="flex items-center gap-1 text-amber-600">
                          <Gauge className="h-3 w-3" />
                          <span className="font-medium">{provider.avgPing.toFixed(0)}</span>
                        </div>
                        <p className="text-[9px] text-muted-foreground">ms</p>
                      </div>
                    </div>
                  </div>
                );
              })}
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
