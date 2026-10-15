"use client";

import React, { memo, useCallback } from "react";
import Image from "next/image";
import { motion } from "framer-motion";
import {
  Clock,
  Copy,
  Download,
  Edit,
  Gauge,
  MapPin,
  MoreVertical,
  Navigation,
  Signal,
  Smartphone,
  Trash2,
  Upload,
  WifiOff,
  Zap,
} from "lucide-react";
import { toast } from "sonner";

import { type SpeedTest } from "@/components/features/speedtest/types/SpeedTest";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { Checkbox } from "@/components/ui/checkbox";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { Progress } from "@/components/ui/progress";
import { Tooltip, TooltipContent, TooltipTrigger } from "@/components/ui/tooltip";
import { cn } from "@/lib/utils";
import { SIGNAL_QUALITY_CONFIG, type SignalQuality } from "../../../types";

// Speed quality thresholds
const getSpeedQuality = (speed: number): { label: string; color: string; bg: string } => {
  if (speed >= 100) return { label: "Excellent", color: "text-emerald-600", bg: "bg-emerald-500" };
  if (speed >= 50) return { label: "Good", color: "text-green-600", bg: "bg-green-500" };
  if (speed >= 20) return { label: "Average", color: "text-yellow-600", bg: "bg-yellow-500" };
  if (speed >= 5) return { label: "Fair", color: "text-orange-600", bg: "bg-orange-500" };
  return { label: "Poor", color: "text-red-600", bg: "bg-red-500" };
};

// Ping quality thresholds
const getPingQuality = (ping: number): { label: string; color: string } => {
  if (ping <= 20) return { label: "Excellent", color: "text-emerald-600" };
  if (ping <= 50) return { label: "Good", color: "text-green-600" };
  if (ping <= 100) return { label: "Fair", color: "text-yellow-600" };
  if (ping <= 200) return { label: "Poor", color: "text-orange-600" };
  return { label: "Very Poor", color: "text-red-600" };
};

interface SpeedtestCardProps {
  speedtest: SpeedTest;
  isSelected: boolean;
  onToggleSelection: (id: string) => void;
  onEdit: (speedtest: SpeedTest) => void;
  onDelete: (speedtest: SpeedTest) => void;
  onViewOnMap?: (speedtest: SpeedTest) => void;
  index: number;
  readonly?: boolean;
  viewMode?: "grid" | "list";
}

export const SpeedtestCard = memo(function SpeedtestCard({
  speedtest,
  isSelected,
  onToggleSelection,
  onEdit,
  onDelete,
  onViewOnMap,
  index,
  readonly = false,
  viewMode = "grid",
}: SpeedtestCardProps) {
  const handleCopyCoordinates = useCallback(
    (e: React.MouseEvent) => {
      e.stopPropagation();
      const coords = `${speedtest.latitude}, ${speedtest.longitude}`;
      void navigator.clipboard.writeText(coords);
      toast.success("Coordinates copied!", { duration: 1500 });
    },
    [speedtest.latitude, speedtest.longitude],
  );

  const provider = speedtest.serviceProvider?.provider;
  const providerName = provider?.name || "Unknown";
  const providerColor = provider?.color || "#6b7280";
  const providerLogo = provider?.logo;
  const initials = providerName.substring(0, 2).toUpperCase();

  const downloadQuality = getSpeedQuality(speedtest.downloadSpeed || 0);
  const uploadQuality = getSpeedQuality(speedtest.uploadSpeed || 0);
  const pingQuality = getPingQuality(speedtest.ping || 0);
  const signalConfig = SIGNAL_QUALITY_CONFIG[speedtest.signalQuality as SignalQuality] || SIGNAL_QUALITY_CONFIG.Unknown;

  const formattedDate = speedtest.dateTime
    ? new Date(speedtest.dateTime).toLocaleDateString("en-US", {
        month: "short",
        day: "numeric",
        hour: "2-digit",
        minute: "2-digit",
      })
    : "N/A";

  // List View - Horizontal compact layout
  if (viewMode === "list") {
    return (
      <motion.div
        initial={{ opacity: 0, x: -10 }}
        animate={{ opacity: 1, x: 0 }}
        transition={{ duration: 0.15, delay: Math.min(index * 0.01, 0.1) }}
        className="w-full">
        <Card
          className={cn(
            "relative group overflow-hidden transition-all duration-200 cursor-pointer",
            "bg-white dark:bg-gray-900",
            "border shadow-sm hover:shadow-md",
            isSelected
              ? "border-cyan-500 ring-2 ring-cyan-500/30"
              : "border-gray-200 dark:border-gray-700 hover:border-cyan-300",
          )}>
          {/* Left accent bar */}
          <div className="absolute left-0 top-0 bottom-0 w-1" style={{ backgroundColor: providerColor }} />

          <div
            className="items-center p-3 grid gap-4"
            style={{
              gridTemplateColumns: "10px 1fr 600px 2fr 1fr 40px", // Define columns 40px 1fr 3fr 2fr 2fr auto auto auto
            }}>
            {/* Checkbox */}
            <Checkbox
              checked={isSelected}
              onCheckedChange={() => onToggleSelection(speedtest.id)}
              onClick={(e) => e.stopPropagation()}
              className="h-4 w-4 rounded-[4px] border-gray-300 flex-shrink-0"
            />

            {/* Provider */}
            <div className="flex items-center gap-3 min-w-[180px]">
              {providerLogo ? (
                <div
                  className="flex h-6 w-6 items-center justify-center rounded-lg border bg-white p-0.5 flex-shrink-0"
                  style={{ borderColor: providerColor }}>
                  <Image
                    src={providerLogo}
                    alt={providerName}
                    className="h-full w-full object-contain"
                    width={24}
                    height={24}
                  />
                </div>
              ) : (
                <div
                  className="flex h-6 w-6 items-center justify-center rounded-lg text-xs font-bold text-white flex-shrink-0"
                  style={{ backgroundColor: providerColor }}>
                  {initials}
                </div>
              )}
              <div className="min-w-0">
                <p className="text-xs font-semibold text-gray-800 dark:text-gray-200 truncate">{providerName}</p>
                <div className="flex items-center gap-1.5 text-[10px] text-gray-500">
                  <Clock className="h-2.5 w-2.5" />
                  {formattedDate}
                </div>
              </div>
            </div>

            {/* Speed Metrics - Horizontal */}
            <div className="grid grid-cols-5 gap-4 items-center w-full">
              {/* Download */}
              <div className="flex items-center gap-2 w-[100px]">
                <div className="p-1.5 rounded-lg bg-green-50 dark:bg-green-950/30">
                  <Download className="h-4 w-4 text-green-600" />
                </div>
                <div>
                  <div className="flex items-baseline gap-1">
                    <span className={cn("text-base font-bold", downloadQuality.color)}>
                      {(speedtest.downloadSpeed || 0).toFixed(1)}
                    </span>
                    <span className="text-[10px] text-gray-500">Mbps</span>
                  </div>
                  <div className="w-16 h-1 bg-gray-200 dark:bg-gray-700 rounded-full overflow-hidden">
                    <div
                      className="h-full rounded-full"
                      style={{
                        width: `${Math.min(((speedtest.downloadSpeed || 0) / 100) * 100, 100)}%`,
                        backgroundColor: downloadQuality.color.includes("emerald")
                          ? "#10b981"
                          : downloadQuality.color.includes("green")
                            ? "#22c55e"
                            : downloadQuality.color.includes("yellow")
                              ? "#eab308"
                              : downloadQuality.color.includes("orange")
                                ? "#f97316"
                                : "#ef4444",
                      }}
                    />
                  </div>
                </div>
              </div>

              {/* Upload */}
              <div className="flex items-center gap-2 w-[100px]">
                <div className="p-1.5 rounded-lg bg-blue-50 dark:bg-blue-950/30">
                  <Upload className="h-4 w-4 text-blue-600" />
                </div>
                <div>
                  <div className="flex items-baseline gap-1">
                    <span className={cn("text-base font-bold", uploadQuality.color)}>
                      {(speedtest.uploadSpeed || 0).toFixed(1)}
                    </span>
                    <span className="text-[10px] text-gray-500">Mbps</span>
                  </div>
                  <div className="w-16 h-1 bg-gray-200 dark:bg-gray-700 rounded-full overflow-hidden">
                    <div
                      className="h-full rounded-full"
                      style={{
                        width: `${Math.min(((speedtest.uploadSpeed || 0) / 100) * 100, 100)}%`,
                        backgroundColor: "#3b82f6",
                      }}
                    />
                  </div>
                </div>
              </div>

              {/* Ping */}
              <div className="flex items-center gap-2 w-[90px] ">
                <div className="p-1.5 rounded-lg bg-orange-50 dark:bg-orange-950/30">
                  <Gauge className="h-4 w-4 text-orange-500" />
                </div>
                <div>
                  <span className={cn("text-sm font-bold", pingQuality.color)}>{speedtest.ping || 0}</span>
                  <span className="text-[10px] text-gray-500 ml-0.5">ms</span>
                </div>
              </div>

              {/* Signal Quality */}
              <Badge
                variant="outline"
                className="gap-1 text-xs w-[100px] text-nowrap"
                style={{ borderColor: signalConfig.color, color: signalConfig.color }}>
                <Signal className="h-3 w-3" />
                {signalConfig.label}
              </Badge>

              {/* NBC Badge */}
              {speedtest.isNoBroadbandCoverage && (
                <Badge
                  variant="outline"
                  className="gap-1 text-xs text-gray-600 dark:text-gray-400 border-gray-400 dark:border-gray-500 bg-gray-100 dark:bg-gray-800">
                  <WifiOff className="h-3 w-3" />
                  NBC
                </Badge>
              )}

              {/* Jitter */}
              {speedtest.jitter && (
                <Badge variant="outline" className="gap-1 text-xs text-purple-600 border-purple-300 w-[100px]">
                  <Zap className="h-3 w-3" />
                  {speedtest.jitter}ms
                </Badge>
              )}
            </div>

            {/* Location */}
            <div className="flex items-center gap-2 justify-end">
              <code className="text-[10px] font-mono text-gray-500 truncate">
                {typeof speedtest.latitude === "number" ? speedtest.latitude.toFixed(4) : "N/A"},{" "}
                {typeof speedtest.longitude === "number" ? speedtest.longitude.toFixed(4) : "N/A"}
              </code>
              <MapPin className="h-3.5 w-3.5 flex-shrink-0" style={{ color: providerColor }} />
            </div>

            {/* Device */}
            {speedtest.phoneModel && (
              <Tooltip>
                <TooltipTrigger>
                  <Badge variant="secondary" className="gap-1 text-[10px] w-full text-right">
                    <span className="min-w-[80px] w-full">{speedtest.phoneModel}</span>
                    <Smartphone className="h-3 w-3" />
                  </Badge>
                </TooltipTrigger>
                <TooltipContent>{speedtest.phoneModel}</TooltipContent>
              </Tooltip>
            )}

            {/* Actions */}
            {!readonly && (
              <div className="flex items-center gap-1 opacity-0 group-hover:opacity-100 transition-opacity">
                <Tooltip>
                  <TooltipTrigger asChild>
                    <Button
                      variant="ghost"
                      size="sm"
                      className="h-7 w-7 p-0 hover:bg-blue-50 dark:hover:bg-blue-950/30"
                      onClick={(e) => {
                        e.stopPropagation();
                        onEdit(speedtest);
                      }}>
                      <Edit className="h-3.5 w-3.5 text-blue-600" />
                    </Button>
                  </TooltipTrigger>
                  <TooltipContent>Edit</TooltipContent>
                </Tooltip>
                {onViewOnMap && (
                  <Tooltip>
                    <TooltipTrigger asChild>
                      <Button
                        variant="ghost"
                        size="sm"
                        className="h-7 w-7 p-0 hover:bg-cyan-50 dark:hover:bg-cyan-950/30"
                        onClick={(e) => {
                          e.stopPropagation();
                          onViewOnMap(speedtest);
                        }}>
                        <Navigation className="h-3.5 w-3.5 text-cyan-600" />
                      </Button>
                    </TooltipTrigger>
                    <TooltipContent>View on Map</TooltipContent>
                  </Tooltip>
                )}
                <Tooltip>
                  <TooltipTrigger asChild>
                    <Button
                      variant="ghost"
                      size="sm"
                      className="h-7 w-7 p-0 hover:bg-red-50 dark:hover:bg-red-950/30"
                      onClick={(e) => {
                        e.stopPropagation();
                        onDelete(speedtest);
                      }}>
                      <Trash2 className="h-3.5 w-3.5 text-red-600" />
                    </Button>
                  </TooltipTrigger>
                  <TooltipContent>Delete</TooltipContent>
                </Tooltip>
              </div>
            )}
          </div>
        </Card>
      </motion.div>
    );
  }

  // Grid View - Original card layout
  return (
    <motion.div
      initial={{ opacity: 0, scale: 0.97 }}
      animate={{ opacity: 1, scale: 1 }}
      transition={{ duration: 0.15, delay: Math.min(index * 0.01, 0.1) }}
      whileHover={{ scale: 1.02, y: -2 }}
      whileTap={{ scale: 0.98 }}
      className="h-full">
      <Card
        className={cn(
          "relative h-full group overflow-hidden transition-all duration-200 cursor-pointer",
          "bg-white dark:bg-gray-900",
          "border shadow-sm hover:shadow-lg",
          isSelected
            ? "border-cyan-500 ring-2 ring-cyan-500/30 shadow-cyan-100 dark:shadow-cyan-900/20"
            : "border-gray-200 dark:border-gray-700 hover:border-cyan-300 dark:hover:border-cyan-600",
        )}>
        {/* Top gradient accent */}
        <div
          className="absolute top-0 left-0 right-0 h-1 bg-gradient-to-r"
          style={{ backgroundImage: `linear-gradient(to right, ${providerColor}, ${providerColor}88)` }}
        />

        <div className="p-3">
          {/* Header row */}
          <div className="flex items-start justify-between gap-2 mb-2">
            <div className="flex items-center gap-2">
              <Checkbox
                checked={isSelected}
                onCheckedChange={() => onToggleSelection(speedtest.id)}
                onClick={(e) => e.stopPropagation()}
                className="h-4 w-4 rounded-[4px] border-gray-300"
              />

              {/* Provider badge */}
              {providerLogo ? (
                <div
                  className="flex h-7 w-7 items-center justify-center rounded-lg border bg-white p-0.5 flex-shrink-0"
                  style={{ borderColor: providerColor }}>
                  <Image
                    src={providerLogo}
                    alt={providerName}
                    className="h-full w-full object-contain"
                    width={24}
                    height={24}
                  />
                </div>
              ) : (
                <div
                  className="flex h-7 w-7 items-center justify-center rounded-lg text-[10px] font-bold text-white flex-shrink-0"
                  style={{ backgroundColor: providerColor }}>
                  {initials}
                </div>
              )}

              <div className="flex-1 min-w-0">
                <span className="text-sm font-semibold text-gray-800 dark:text-gray-200 truncate block">
                  {providerName}
                </span>
                <span className="text-[10px] text-gray-500">{formattedDate}</span>
              </div>
            </div>

            {!readonly && (
              <DropdownMenu>
                <DropdownMenuTrigger asChild onClick={(e) => e.stopPropagation()}>
                  <Button
                    variant="ghost"
                    size="sm"
                    className="h-6 w-6 p-0 opacity-0 group-hover:opacity-100 transition-opacity rounded-full hover:bg-gray-100 dark:hover:bg-gray-800">
                    <MoreVertical className="h-4 w-4 text-gray-400" />
                  </Button>
                </DropdownMenuTrigger>
                <DropdownMenuContent align="end" className="w-40 shadow-lg">
                  <DropdownMenuLabel className="text-[10px] text-gray-500">Actions</DropdownMenuLabel>
                  <DropdownMenuSeparator />
                  <DropdownMenuItem onClick={() => onEdit(speedtest)} className="text-xs gap-2">
                    <Edit className="h-3.5 w-3.5" />
                    Edit
                  </DropdownMenuItem>
                  <DropdownMenuItem onClick={handleCopyCoordinates} className="text-xs gap-2">
                    <Copy className="h-3.5 w-3.5" />
                    Copy Coords
                  </DropdownMenuItem>
                  {onViewOnMap && (
                    <DropdownMenuItem onClick={() => onViewOnMap(speedtest)} className="text-xs gap-2">
                      <Navigation className="h-3.5 w-3.5" />
                      View on Map
                    </DropdownMenuItem>
                  )}
                  <DropdownMenuSeparator />
                  <DropdownMenuItem
                    onClick={() => onDelete(speedtest)}
                    className="text-xs gap-2 text-red-600 focus:text-red-600 focus:bg-red-50">
                    <Trash2 className="h-3.5 w-3.5" />
                    Delete
                  </DropdownMenuItem>
                </DropdownMenuContent>
              </DropdownMenu>
            )}
          </div>

          {/* Speed metrics */}
          <div className="grid grid-cols-2 gap-2 mb-2">
            {/* Download */}
            <div className={cn(
              "rounded-lg p-2",
              speedtest.isNoBroadbandCoverage
                ? "bg-gray-50 dark:bg-gray-800/50"
                : "bg-gradient-to-br from-green-50 to-emerald-50/50 dark:from-green-950/30 dark:to-emerald-950/20"
            )}>
              <div className="flex items-center gap-1.5 mb-1">
                <Download className={cn("h-3.5 w-3.5", speedtest.isNoBroadbandCoverage ? "text-gray-400" : "text-green-600")} />
                <span className={cn("text-[10px] font-medium", speedtest.isNoBroadbandCoverage ? "text-gray-400 dark:text-gray-500" : "text-green-700 dark:text-green-400")}>Download</span>
              </div>
              <div className="flex items-baseline gap-1">
                <span className={cn("text-lg font-bold", speedtest.isNoBroadbandCoverage ? "text-gray-400" : downloadQuality.color)}>
                  {(speedtest.downloadSpeed || 0).toFixed(1)}
                </span>
                <span className="text-[10px] text-gray-500">Mbps</span>
              </div>
              <Progress value={Math.min(((speedtest.downloadSpeed || 0) / 100) * 100, 100)} className="h-1 mt-1" />
            </div>

            {/* Upload */}
            <div className={cn(
              "rounded-lg p-2",
              speedtest.isNoBroadbandCoverage
                ? "bg-gray-50 dark:bg-gray-800/50"
                : "bg-gradient-to-br from-blue-50 to-cyan-50/50 dark:from-blue-950/30 dark:to-cyan-950/20"
            )}>
              <div className="flex items-center gap-1.5 mb-1">
                <Upload className={cn("h-3.5 w-3.5", speedtest.isNoBroadbandCoverage ? "text-gray-400" : "text-blue-600")} />
                <span className={cn("text-[10px] font-medium", speedtest.isNoBroadbandCoverage ? "text-gray-400 dark:text-gray-500" : "text-blue-700 dark:text-blue-400")}>Upload</span>
              </div>
              <div className="flex items-baseline gap-1">
                <span className={cn("text-lg font-bold", speedtest.isNoBroadbandCoverage ? "text-gray-400" : uploadQuality.color)}>
                  {(speedtest.uploadSpeed || 0).toFixed(1)}
                </span>
                <span className="text-[10px] text-gray-500">Mbps</span>
              </div>
              <Progress value={Math.min(((speedtest.uploadSpeed || 0) / 100) * 100, 100)} className="h-1 mt-1" />
            </div>
          </div>

          {/* Secondary metrics row */}
          <div className="flex items-center gap-2 text-[10px] text-gray-600 dark:text-gray-400 mb-2">
            {speedtest.isNoBroadbandCoverage && (
              <Badge
                variant="outline"
                className="flex items-center gap-1 px-2 py-0.5 text-[10px] font-semibold bg-gray-100 dark:bg-gray-800 text-gray-600 dark:text-gray-400 border-gray-300 dark:border-gray-600">
                <WifiOff className="h-2.5 w-2.5" />
                NBC
              </Badge>
            )}
            <Tooltip>
              <TooltipTrigger asChild>
                <div className="flex items-center gap-1 px-2 py-1 rounded-full bg-orange-50 dark:bg-orange-950/30">
                  <Gauge className="h-3 w-3 text-orange-500" />
                  <span className={pingQuality.color}>{speedtest.ping || 0}ms</span>
                </div>
              </TooltipTrigger>
              <TooltipContent>Ping latency</TooltipContent>
            </Tooltip>

            <Tooltip>
              <TooltipTrigger asChild>
                <div
                  className="flex items-center gap-1 px-2 py-1 rounded-full"
                  style={{ backgroundColor: `${signalConfig.color}15` }}>
                  <Signal className="h-3 w-3" style={{ color: signalConfig.color }} />
                  <span style={{ color: signalConfig.color }}>{signalConfig.label}</span>
                </div>
              </TooltipTrigger>
              <TooltipContent>Signal Quality</TooltipContent>
            </Tooltip>

            {speedtest.jitter && (
              <Tooltip>
                <TooltipTrigger asChild>
                  <div className="flex items-center gap-1 px-2 py-1 rounded-full bg-purple-50 dark:bg-purple-950/30">
                    <Zap className="h-3 w-3 text-purple-500" />
                    <span className="text-purple-600">{speedtest.jitter}ms</span>
                  </div>
                </TooltipTrigger>
                <TooltipContent>Jitter</TooltipContent>
              </Tooltip>
            )}
          </div>

          {/* Footer */}
          <div className="flex items-center gap-2 pt-2 border-t border-gray-100 dark:border-gray-800">
            <MapPin className="h-3 w-3 flex-shrink-0" style={{ color: providerColor }} />
            <code className="text-[9px] font-mono text-gray-500 dark:text-gray-400 truncate flex-1">
              {typeof speedtest.latitude === "number" ? speedtest.latitude.toFixed(5) : "N/A"},{" "}
              {typeof speedtest.longitude === "number" ? speedtest.longitude.toFixed(5) : "N/A"}
            </code>
            {speedtest.phoneModel && (
              <Tooltip>
                <TooltipTrigger asChild>
                  <div className="flex items-center gap-1">
                    <Smartphone className="h-3 w-3 text-gray-400" />
                  </div>
                </TooltipTrigger>
                <TooltipContent>{speedtest.phoneModel}</TooltipContent>
              </Tooltip>
            )}
          </div>
        </div>
      </Card>
    </motion.div>
  );
});

export default SpeedtestCard;
