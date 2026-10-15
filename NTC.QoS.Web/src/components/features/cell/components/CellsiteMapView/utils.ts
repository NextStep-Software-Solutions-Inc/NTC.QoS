import * as maptilersdk from "@maptiler/sdk";

import { CLUSTER_COLORS, getProviderIconUrl, SIGNAL_STRENGTH_COLORS } from "./constants";
import { type CellMapDetails } from "./types";
import { escapeHtml } from "@/utils/escapeHtml";

/**
 * Create popup HTML for a cell with "View Details" button
 */
export function createCellPopupHTML(cell: CellMapDetails, showViewDetails: boolean = true): string {
  const statusColor =
    cell.status?.toLowerCase() === "active"
      ? "#10b981"
      : cell.status?.toLowerCase() === "inactive"
        ? "#64748b"
        : "#f59e0b";

  const verifiedBadge = cell.isVerified
    ? '<span style="display: inline-flex; align-items: center; gap: 2px; font-size: 10px; color: #3b82f6; margin-left: 4px;"><svg width="12" height="12" viewBox="0 0 24 24" fill="currentColor"><path d="M9 16.17L4.83 12l-1.42 1.41L9 19 21 7l-1.41-1.41z"/></svg>Verified</span>'
    : "";

  // Escape cell ID for use in JavaScript to prevent XSS
  const escapedCellId = escapeHtml(cell.id);

  const viewDetailsButton = showViewDetails
    ? `<button 
        onclick="window.dispatchEvent(new CustomEvent('viewCellDetails', { detail: { cellId: '${escapedCellId}' } }))"
        style="
          width: 100%;
          margin-top: 12px;
          padding: 8px 12px;
          background: linear-gradient(135deg, #3b82f6, #2563eb);
          color: white;
          border: none;
          border-radius: 6px;
          font-size: 12px;
          font-weight: 500;
          cursor: pointer;
          display: flex;
          align-items: center;
          justify-content: center;
          gap: 6px;
          transition: all 0.2s;
        "
        onmouseover="this.style.background='linear-gradient(135deg, #2563eb, #1d4ed8)'"
        onmouseout="this.style.background='linear-gradient(135deg, #3b82f6, #2563eb)'"
      >
        <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
          <path d="M2 12s3-7 10-7 10 7 10 7-3 7-10 7-10-7-10-7Z"/>
          <circle cx="12" cy="12" r="3"/>
        </svg>
        View Details
      </button>`
    : "";

  // Escape user-provided data to prevent XSS
  const safeName = escapeHtml(cell.name);
  const safeProviderName = escapeHtml(cell.providerName);
  const safeStatus = escapeHtml(cell.status) || "Unknown";
  const safeSiteId = escapeHtml(cell.siteId);
  const safeTowerType = escapeHtml(cell.towerType);

  return `
    <div style="padding: 12px; min-width: 240px; max-width: 300px; font-family: system-ui, -apple-system, sans-serif;">
      <div style="display: flex; align-items: center; gap: 10px; margin-bottom: 10px;">
        <div style="width: 36px; height: 36px; border-radius: 8px; background: linear-gradient(135deg, ${cell.providerColor}20, ${cell.providerColor}40); display: flex; align-items: center; justify-content: center; border: 2px solid ${cell.providerColor}; flex-shrink: 0;">
          <img src="${getProviderIconUrl(cell.providerName)}" alt="${safeProviderName}" style="width: 22px; height: 22px; object-fit: contain;" onerror="this.style.display='none'" />
        </div>
        <div style="flex: 1; min-width: 0;">
          <div style="font-weight: 600; font-size: 14px; color: #1f2937; white-space: nowrap; overflow: hidden; text-overflow: ellipsis;">
            ${safeName}
            ${verifiedBadge}
          </div>
          <div style="font-size: 12px; color: ${cell.providerColor}; font-weight: 500;">
            ${safeProviderName}
          </div>
        </div>
      </div>
      <div style="display: flex; flex-direction: column; gap: 6px; font-size: 12px; color: #6b7280;">
        <div style="display: flex; align-items: center; gap: 6px; padding: 4px 8px; background: ${statusColor}15; border-radius: 4px;">
          <span style="width: 8px; height: 8px; border-radius: 50%; background: ${statusColor};"></span>
          <span style="color: ${statusColor}; font-weight: 500;">${safeStatus}</span>
        </div>
        ${safeSiteId ? `<div style="display: flex; justify-content: space-between;"><span style="color: #9ca3af;">Site ID:</span><span style="font-weight: 500;">${safeSiteId}</span></div>` : ""}
        ${safeTowerType ? `<div style="display: flex; justify-content: space-between;"><span style="color: #9ca3af;">Tower:</span><span style="font-weight: 500;">${safeTowerType}</span></div>` : ""}
        ${cell.elevation ? `<div style="display: flex; justify-content: space-between;"><span style="color: #9ca3af;">Elevation:</span><span style="font-weight: 500;">${cell.elevation}m</span></div>` : ""}
        <div style="font-size: 10px; color: #9ca3af; margin-top: 4px; padding: 4px 8px; background: #f3f4f6; border-radius: 4px; font-family: monospace; text-align: center;">
          ${cell.lat.toFixed(6)}, ${cell.lng.toFixed(6)}
        </div>
      </div>
      ${viewDetailsButton}
    </div>
  `;
}

/**
 * Get cluster layer paint configuration - using distinct colors from cell sites
 */
export function getClusterLayerPaint(): maptilersdk.CircleLayerSpecification["paint"] {
  return {
    "circle-color": [
      "step",
      ["get", "point_count"],
      CLUSTER_COLORS.small, // Indigo for <10
      10,
      CLUSTER_COLORS.medium, // Purple for 10-50
      50,
      CLUSTER_COLORS.large, // Violet for 50-100
      100,
      CLUSTER_COLORS.huge, // Fuchsia for >100
    ],
    "circle-radius": [
      "step",
      ["get", "point_count"],
      12, // Reduced from 15
      10,
      16, // Reduced from 20
      50,
      20, // Reduced from 25
      100,
      24, // Reduced from 30
    ],
    "circle-stroke-width": 2,
    "circle-stroke-color": "#ffffff",
    "circle-opacity": 0.9,
  };
}

/**
 * Get cell circle layer paint configuration
 */
export function getCellCircleLayerPaint(): maptilersdk.CircleLayerSpecification["paint"] {
  return {
    "circle-radius": ["case", ["get", "isSelected"], 10, ["get", "isPending"], 7, 6],
    "circle-color": ["get", "providerColor"],
    "circle-stroke-width": ["case", ["get", "isSelected"], 3, ["get", "isPending"], 2, 1.5],
    "circle-stroke-color": ["case", ["get", "isSelected"], "#3b82f6", ["get", "isPending"], "#f59e0b", "#ffffff"],
    "circle-opacity": ["case", ["get", "hasError"], 0.5, 1],
  };
}

/**
 * Get point view circle paint configuration (non-clustered small dots)
 */
export function getPointViewCirclePaint(): maptilersdk.CircleLayerSpecification["paint"] {
  return {
    "circle-radius": 3,
    "circle-color": ["get", "providerColor"],
    "circle-stroke-width": 0.5,
    "circle-stroke-color": "#ffffff",
    "circle-opacity": 0.8,
  };
}

/**
 * Get cell site heatmap layer paint configuration (density-based)
 */
export function getCellSiteHeatmapPaint(opacity: number = 0.7): maptilersdk.HeatmapLayerSpecification["paint"] {
  return {
    "heatmap-weight": 1,
    "heatmap-intensity": ["interpolate", ["linear"], ["zoom"], 0, 1, 15, 3],
    "heatmap-color": [
      "interpolate",
      ["linear"],
      ["heatmap-density"],
      0,
      "rgba(33,102,172,0)",
      0.2,
      "rgb(103,169,207)",
      0.4,
      "rgb(209,229,240)",
      0.6,
      "rgb(253,219,199)",
      0.8,
      "rgb(239,138,98)",
      1,
      "rgb(178,24,43)",
    ],
    "heatmap-radius": ["interpolate", ["linear"], ["zoom"], 0, 2, 15, 20],
    "heatmap-opacity": opacity,
  };
}

/**
 * Get signal coverage circle layer paint configuration (color-coded by RSSI)
 * Uses circles instead of heatmap to show actual signal strength per point.
 *
 * Color scale thresholds:
 *   ≥ -60 dBm  → Excellent (green)
 *   -70 to -60 → Good (lime)
 *   -80 to -70 → Fair (yellow)
 *   -90 to -80 → Poor (orange)
 *   < -90 dBm  → Very Poor (red)
 */
export function getSignalCoverageCirclePaint(opacity: number = 0.8): maptilersdk.CircleLayerSpecification["paint"] {
  return {
    "circle-radius": ["interpolate", ["linear"], ["zoom"], 5, 2, 10, 4, 15, 8],
    "circle-color": [
      "interpolate",
      ["linear"],
      ["get", "strength"],
      -120,
      SIGNAL_STRENGTH_COLORS.veryPoor, // Very weak - red
      -90,
      SIGNAL_STRENGTH_COLORS.poor, // Poor - orange
      -80,
      SIGNAL_STRENGTH_COLORS.fair, // Fair - yellow
      -70,
      SIGNAL_STRENGTH_COLORS.good, // Good - lime
      -60,
      SIGNAL_STRENGTH_COLORS.excellent, // Excellent - green
    ],
    "circle-opacity": opacity,
    "circle-stroke-width": 0,
  };
}

/**
 * Create popup HTML for signal coverage point with enhanced styling
 */
export function createSignalCoveragePopupHTML(properties: {
  strength: number;
  barangay?: string;
  azimuth?: string;
}): string {
  const strength = properties.strength;
  const signalLabel =
    strength >= -60
      ? "Excellent"
      : strength >= -70
        ? "Good"
        : strength >= -80
          ? "Fair"
          : strength >= -90
            ? "Poor"
            : "Very Poor";

  const signalColor =
    strength >= -60
      ? SIGNAL_STRENGTH_COLORS.excellent
      : strength >= -70
        ? SIGNAL_STRENGTH_COLORS.good
        : strength >= -80
          ? SIGNAL_STRENGTH_COLORS.fair
          : strength >= -90
            ? SIGNAL_STRENGTH_COLORS.poor
            : SIGNAL_STRENGTH_COLORS.veryPoor;

  const barangay = properties.barangay || "Unknown location";
  const azimuth = properties.azimuth || "N/A";

  // Enhanced styled popup with gradient background
  return `
    <div style="
      min-width: 220px; 
      font-family: system-ui, -apple-system, sans-serif;
      background: linear-gradient(135deg, #ffffff 0%, #f8fafc 100%);
      border-radius: 12px;
      overflow: hidden;
      box-shadow: 0 10px 25px rgba(0,0,0,0.15);
    ">
      <!-- Header with signal strength bar -->
      <div style="
        background: linear-gradient(135deg, ${signalColor}ee, ${signalColor}cc);
        padding: 14px 16px;
        color: white;
      ">
        <div style="display: flex; align-items: center; gap: 12px;">
          <div style="
            width: 44px; 
            height: 44px; 
            border-radius: 12px; 
            background: rgba(255,255,255,0.25);
            backdrop-filter: blur(4px);
            display: flex; 
            align-items: center; 
            justify-content: center;
          ">
            <svg width="24" height="24" viewBox="0 0 24 24" fill="white" stroke="none">
              <rect x="2" y="18" width="3" height="4" rx="1" opacity="0.3"/>
              <rect x="7" y="14" width="3" height="8" rx="1" opacity="${strength >= -90 ? "1" : "0.3"}"/>
              <rect x="12" y="10" width="3" height="12" rx="1" opacity="${strength >= -80 ? "1" : "0.3"}"/>
              <rect x="17" y="6" width="3" height="16" rx="1" opacity="${strength >= -70 ? "1" : "0.3"}"/>
              <rect x="22" y="2" width="0" height="0" rx="1" opacity="${strength >= -60 ? "1" : "0.3"}"/>
            </svg>
          </div>
          <div>
            <div style="font-weight: 700; font-size: 16px; text-shadow: 0 1px 2px rgba(0,0,0,0.2);">
              ${signalLabel}
            </div>
            <div style="font-size: 20px; font-weight: 800; margin-top: 2px;">
              ${strength.toFixed(0)} dBm
            </div>
          </div>
        </div>
      </div>
      
      <!-- Details section -->
      <div style="padding: 14px 16px;">
        <div style="
          display: flex; 
          flex-direction: column; 
          gap: 10px; 
          font-size: 13px;
        ">
          <div style="
            display: flex; 
            align-items: center; 
            gap: 10px;
            padding: 8px 10px;
            background: #f1f5f9;
            border-radius: 8px;
          ">
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="#64748b" stroke-width="2">
              <path d="M21 10c0 7-9 13-9 13s-9-6-9-13a9 9 0 0 1 18 0z"/>
              <circle cx="12" cy="10" r="3"/>
            </svg>
            <div style="flex: 1; min-width: 0;">
              <div style="color: #94a3b8; font-size: 10px; text-transform: uppercase; letter-spacing: 0.5px;">Location</div>
              <div style="color: #1e293b; font-weight: 500; white-space: nowrap; overflow: hidden; text-overflow: ellipsis;">${barangay}</div>
            </div>
          </div>
          
          <div style="
            display: flex; 
            align-items: center; 
            gap: 10px;
            padding: 8px 10px;
            background: #f1f5f9;
            border-radius: 8px;
          ">
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="#64748b" stroke-width="2">
              <circle cx="12" cy="12" r="10"/>
              <line x1="12" y1="8" x2="12" y2="12"/>
              <line x1="12" y1="12" x2="16" y2="14"/>
            </svg>
            <div style="flex: 1;">
              <div style="color: #94a3b8; font-size: 10px; text-transform: uppercase; letter-spacing: 0.5px;">Azimuth</div>
              <div style="color: #1e293b; font-weight: 500;">${azimuth}</div>
            </div>
          </div>
        </div>
        
        <!-- Signal strength bar -->
        <div style="margin-top: 12px;">
          <div style="display: flex; justify-content: space-between; margin-bottom: 4px;">
            <span style="font-size: 10px; color: #94a3b8;">Signal Quality</span>
            <span style="font-size: 10px; color: ${signalColor}; font-weight: 600;">${signalLabel}</span>
          </div>
          <div style="height: 6px; background: #e2e8f0; border-radius: 3px; overflow: hidden;">
            <div style="
              height: 100%; 
              width: ${Math.max(0, Math.min(100, ((strength + 100) / 40) * 100))}%;
              background: linear-gradient(90deg, ${signalColor}, ${signalColor}aa);
              border-radius: 3px;
              transition: width 0.3s;
            "></div>
          </div>
        </div>
      </div>
    </div>
  `;
}

/**
 * Get cellular coverage heatmap layer paint configuration (signal strength based)
 * @deprecated Use getSignalCoverageCirclePaint instead for better visualization
 */
export function getCellularCoverageHeatmapPaint(opacity: number = 0.6): maptilersdk.HeatmapLayerSpecification["paint"] {
  return {
    "heatmap-weight": [
      "interpolate",
      ["linear"],
      ["get", "strength"],
      -120,
      0, // Weakest signal
      -100,
      0.3,
      -90,
      0.5,
      -80,
      0.7,
      -70,
      0.85,
      -60,
      1, // Strongest signal
    ],
    "heatmap-intensity": ["interpolate", ["linear"], ["zoom"], 0, 0.5, 12, 2],
    "heatmap-color": [
      "interpolate",
      ["linear"],
      ["heatmap-density"],
      0,
      "rgba(0,0,0,0)",
      0.1,
      "rgba(220,38,38,0.4)", // Red - weak
      0.3,
      "rgba(251,146,60,0.5)", // Orange
      0.5,
      "rgba(250,204,21,0.6)", // Yellow
      0.7,
      "rgba(132,204,22,0.7)", // Lime
      0.9,
      "rgba(34,197,94,0.8)", // Green - strong
      1,
      "rgba(22,163,74,0.9)", // Dark green - strongest
    ],
    "heatmap-radius": ["interpolate", ["linear"], ["zoom"], 0, 4, 10, 25, 15, 40],
    "heatmap-opacity": opacity,
  };
}

/**
 * Get heatmap layer paint configuration (deprecated - use getCellSiteHeatmapPaint)
 */
export function getHeatmapLayerPaint(): maptilersdk.HeatmapLayerSpecification["paint"] {
  return getCellSiteHeatmapPaint();
}
