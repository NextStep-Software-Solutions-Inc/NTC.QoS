import { type SpeedTest } from "@/components/features/speedtest/types/SpeedTest";
import { escapeHtml } from "@/utils/escapeHtml";
import { NBC_COLOR, NBC_COLOR_COVERAGE, SIGNAL_QUALITY_COLORS, SPEED_COLORS, SPEED_THRESHOLDS } from "./constants";

/**
 * Build the HTML for the No Broadband Coverage (NBC) badge shown in map popups.
 * Uses a plain SVG wifi-off icon since popups are rendered as raw HTML strings (not React JSX).
 */
function buildNbcBadgeHTML(): string {
  return `<div class="flex items-center gap-1 mb-2 px-2 py-1 rounded-md bg-gray-100 border border-gray-300">
    <svg class="w-3 h-3" style="color:#6b7280" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
      <line x1="1" y1="1" x2="23" y2="23"/>
      <path d="M16.72 11.06A10.94 10.94 0 0 1 19 12.55"/>
      <path d="M5 12.55a10.94 10.94 0 0 1 5.17-2.39"/>
      <path d="M10.71 5.05A16 16 0 0 1 22.56 9"/>
      <path d="M1.42 9a15.91 15.91 0 0 1 4.7-2.88"/>
      <path d="M8.53 16.11a6 6 0 0 1 6.95 0"/>
      <line x1="12" y1="20" x2="12.01" y2="20"/>
    </svg>
    <span class="text-[10px] font-semibold" style="color:#4b5563">No Broadband Coverage</span>
  </div>`;
}

/**
 * Create HTML content for speedtest popup
 */
export function createSpeedtestPopupHTML(st: SpeedTest): string {
  const provider = escapeHtml(st.serviceProvider?.provider?.name) || "Unknown Provider";
  const providerColor = st.isNoBroadbandCoverage ? NBC_COLOR : (st.serviceProvider?.provider?.color || "#6b7280");
  const signalQuality = st.signalQuality || "Unknown";
  const signalColor = SIGNAL_QUALITY_COLORS[signalQuality] || "#6b7280";

  const nbcBadge = st.isNoBroadbandCoverage ? buildNbcBadgeHTML() : "";

  return `
    <div class="p-3 min-w-[240px] max-w-[280px]">
      <div class="flex items-center gap-2 mb-3">
        <div class="w-8 h-8 rounded-lg flex items-center justify-center shadow-md" style="background: ${providerColor}">
          <svg class="w-4 h-4 text-white" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8.111 16.404a5.5 5.5 0 017.778 0M12 20h.01m-7.08-7.071c3.904-3.905 10.236-3.905 14.14 0M1.394 9.393c5.857-5.857 15.355-5.857 21.213 0" />
          </svg>
        </div>
        <div class="flex-1 min-w-0">
          <p class="font-semibold text-sm truncate">${provider}</p>
          <p class="text-[10px] text-gray-500 truncate">${escapeHtml(st.phoneModel) || "Unknown device"}</p>
        </div>
      </div>

      ${nbcBadge}

      <div class="grid grid-cols-3 gap-2 mb-3">
        <div class="text-center p-2 rounded-lg ${st.isNoBroadbandCoverage ? "bg-gray-50" : "bg-gradient-to-br from-cyan-50 to-blue-50"}">
          <p class="text-xs font-bold ${st.isNoBroadbandCoverage ? "text-gray-400" : "text-cyan-600"}">${(st.downloadSpeed || 0).toFixed(1)}</p>
          <p class="text-[9px] text-gray-500">Mbps ↓</p>
        </div>
        <div class="text-center p-2 rounded-lg ${st.isNoBroadbandCoverage ? "bg-gray-50" : "bg-gradient-to-br from-emerald-50 to-green-50"}">
          <p class="text-xs font-bold ${st.isNoBroadbandCoverage ? "text-gray-400" : "text-emerald-600"}">${(st.uploadSpeed || 0).toFixed(1)}</p>
          <p class="text-[9px] text-gray-500">Mbps ↑</p>
        </div>
        <div class="text-center p-2 rounded-lg bg-gradient-to-br from-amber-50 to-orange-50">
          <p class="text-xs font-bold text-amber-600">${(st.ping || 0).toFixed(0)}</p>
          <p class="text-[9px] text-gray-500">ms ping</p>
        </div>
      </div>

      <div class="flex items-center justify-between pt-2 border-t border-gray-100">
        <div class="flex items-center gap-1.5">
          <div class="w-2 h-2 rounded-full" style="background: ${signalColor}"></div>
          <span class="text-[10px] font-medium" style="color: ${signalColor}">${signalQuality}</span>
        </div>
        <p class="text-[9px] text-gray-400">${st.latitude?.toFixed(4)}, ${st.longitude?.toFixed(4)}</p>
      </div>
    </div>
  `;
}

/**
 * Get color for download speed value
 */
export function getSpeedColor(speed: number): string {
  if (speed >= SPEED_THRESHOLDS.excellent) return SPEED_COLORS.excellent;
  if (speed >= SPEED_THRESHOLDS.good) return SPEED_COLORS.good;
  if (speed >= SPEED_THRESHOLDS.average) return SPEED_COLORS.average;
  if (speed >= SPEED_THRESHOLDS.fair) return SPEED_COLORS.fair;
  return SPEED_COLORS.poor;
}

/**
 * Get paint properties for speed-based circles (radius proportional to speed)
 */
export function getSpeedCirclesPaint() {
  return {
    // Radius based on download speed (min 4, max 20 pixels)
    "circle-radius": [
      "interpolate",
      ["linear"],
      ["get", "downloadSpeed"],
      0,
      4, // 0 Mbps = 4px
      10,
      6, // 10 Mbps = 6px
      25,
      8, // 25 Mbps = 8px
      50,
      12, // 50 Mbps = 12px
      100,
      16, // 100 Mbps = 16px
      200,
      20, // 200+ Mbps = 20px
    ] as unknown,
    // Color based on download speed; NBC points are always grey
    "circle-color": [
      "case",
      ["==", ["get", "isNoBroadbandCoverage"], true],
      NBC_COLOR,
      [
        "interpolate",
        ["linear"],
        ["get", "downloadSpeed"],
        0,
        SPEED_COLORS.poor,
        10,
        SPEED_COLORS.fair,
        25,
        SPEED_COLORS.average,
        50,
        SPEED_COLORS.good,
        100,
        SPEED_COLORS.excellent,
      ],
    ] as unknown,
    "circle-stroke-width": 0.5,
    "circle-stroke-color": "#ffffff",
    "circle-opacity": 0.5,
  };
}

/**
 * Get paint properties for simple point view
 */
export function getPointsPaint() {
  return {
    "circle-radius": [
      "interpolate",
      ["linear"],
      ["zoom"],
      5,
      2, // At zoom 5, radius 2
      10,
      4, // At zoom 10, radius 4
      15,
      6, // At zoom 15, radius 6
    ] as unknown,
    // NBC points use a distinct grey color; others use provider color
    "circle-color": [
      "case",
      ["==", ["get", "isNoBroadbandCoverage"], true],
      NBC_COLOR,
      ["get", "providerColor"],
    ] as unknown,
    "circle-stroke-width": [
      "case",
      ["==", ["get", "isNoBroadbandCoverage"], true],
      1.5,
      0.5,
    ] as unknown,
    "circle-stroke-color": [
      "case",
      ["==", ["get", "isNoBroadbandCoverage"], true],
      "#6b7280",
      "#ffffff",
    ] as unknown,
    "circle-opacity": [
      "case",
      ["==", ["get", "isNoBroadbandCoverage"], true],
      0.7,
      0.8,
    ] as unknown,
  };
}

/**
 * Get paint properties for heatmap layer with download speed weighting
 */
export function getHeatmapLayerPaint() {
  return {
    // Weight based on download speed (faster = more weight)
    "heatmap-weight": [
      "interpolate",
      ["linear"],
      ["get", "downloadSpeed"],
      0,
      0.1,
      25,
      0.3,
      50,
      0.5,
      100,
      0.8,
      200,
      1,
    ] as unknown,
    "heatmap-intensity": ["interpolate", ["linear"], ["zoom"], 0, 0.5, 5, 1, 10, 2, 15, 3] as unknown,
    // Green = fast, Red = slow/dense with slow speeds
    "heatmap-color": [
      "interpolate",
      ["linear"],
      ["heatmap-density"],
      0,
      "rgba(0, 0, 0, 0)",
      0.1,
      "rgba(239, 68, 68, 0.3)", // red (poor)
      0.3,
      "rgba(249, 115, 22, 0.5)", // orange (fair)
      0.5,
      "rgba(245, 158, 11, 0.6)", // amber (average)
      0.7,
      "rgba(34, 197, 94, 0.7)", // green (good)
      1,
      "rgba(16, 185, 129, 0.85)", // emerald (excellent)
    ] as unknown,
    "heatmap-radius": ["interpolate", ["linear"], ["zoom"], 0, 8, 5, 15, 10, 25, 15, 40] as unknown,
    "heatmap-opacity": ["interpolate", ["linear"], ["zoom"], 7, 0.9, 15, 0.6] as unknown,
  };
}

/**
 * Get paint for signal quality visualization
 */
export function getSignalQualityCirclePaint() {
  return {
    "circle-radius": ["interpolate", ["linear"], ["zoom"], 5, 3, 10, 5, 15, 8] as unknown,
    // NBC points use grey regardless of signal quality
    "circle-color": [
      "case",
      ["==", ["get", "isNoBroadbandCoverage"], true],
      NBC_COLOR,
      ["get", "signalQualityColor"],
    ] as unknown,
    "circle-stroke-width": 1,
    "circle-stroke-color": "#ffffff",
    "circle-opacity": 0.85,
  };
}

/**
 * Get paint for coverage quality overlay
 * Uses download speed to show coverage quality as a gradient
 */
export function getCoverageCirclePaint() {
  return {
    // Larger radius for coverage effect
    "circle-radius": ["interpolate", ["linear"], ["zoom"], 5, 8, 10, 15, 15, 25] as unknown,
    // NBC points are shown in grey; others use speed-based color
    "circle-color": [
      "case",
      ["==", ["get", "isNoBroadbandCoverage"], true],
      NBC_COLOR_COVERAGE, // semi-transparent grey for NBC coverage overlay
      [
        "interpolate",
        ["linear"],
        ["get", "downloadSpeed"],
        0,
        "rgba(239, 68, 68, 0.6)", // red
        10,
        "rgba(249, 115, 22, 0.6)", // orange
        25,
        "rgba(245, 158, 11, 0.6)", // amber
        50,
        "rgba(34, 197, 94, 0.6)", // green
        100,
        "rgba(16, 185, 129, 0.6)", // emerald
      ],
    ] as unknown,
    "circle-blur": 0.8,
    "circle-opacity": 0.5,
  };
}
