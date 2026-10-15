/**
 * Utility functions for cell site map layers
 * Extracted from CellsiteMapView for reuse in useMapCells hook
 */
import type { CircleLayerSpecification, HeatmapLayerSpecification } from "@maptiler/sdk";

// Cluster colors
export const CLUSTER_COLORS = {
  small: "#6366f1", // Indigo for small clusters (<10)
  medium: "#8b5cf6", // Purple for medium clusters (10-50)
  large: "#a855f7", // Violet for large clusters (50-100)
  huge: "#c026d3", // Fuchsia for huge clusters (>100)
};

// Signal strength color scale
export const SIGNAL_STRENGTH_COLORS = {
  excellent: "#1a9850", // >= -80 dBm
  good: "#91cf60", // -90 to -80 dBm
  fair: "#fee08b", // -100 to -90 dBm
  weak: "#fc8d59", // -110 to -100 dBm
  poor: "#d73027", // < -110 dBm
};

export function getSignalStrengthColor(strength: number): string {
  if (strength >= -80) return SIGNAL_STRENGTH_COLORS.excellent;
  if (strength >= -90) return SIGNAL_STRENGTH_COLORS.good;
  if (strength >= -100) return SIGNAL_STRENGTH_COLORS.fair;
  if (strength >= -110) return SIGNAL_STRENGTH_COLORS.weak;
  return SIGNAL_STRENGTH_COLORS.poor;
}

export function getSignalStrengthQualityLabel(strength: number): string {
  if (strength >= -80) return "Excellent";
  if (strength >= -90) return "Good";
  if (strength >= -100) return "Fair";
  if (strength >= -110) return "Weak";
  return "Poor";
}

/**
 * Get cluster layer paint configuration
 */
export function getClusterLayerPaint(): CircleLayerSpecification["paint"] {
  return {
    "circle-color": [
      "step",
      ["get", "point_count"],
      CLUSTER_COLORS.small,
      10,
      CLUSTER_COLORS.medium,
      50,
      CLUSTER_COLORS.large,
      100,
      CLUSTER_COLORS.huge,
    ],
    "circle-radius": ["step", ["get", "point_count"], 12, 10, 16, 50, 20, 100, 24],
    "circle-stroke-width": 2,
    "circle-stroke-color": "#ffffff",
    "circle-opacity": 0.9,
  };
}

/**
 * Get cell circle layer paint configuration
 */
export function getCellCircleLayerPaint(): CircleLayerSpecification["paint"] {
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
export function getPointViewCirclePaint(): CircleLayerSpecification["paint"] {
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
export function getCellSiteHeatmapPaint(opacity: number = 0.7): HeatmapLayerSpecification["paint"] {
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
 * Get signal coverage circle layer paint configuration (color-coded by RSSI).
 * Uses circles instead of heatmap to show actual signal strength per point.
 *
 * Color scale thresholds (3GPP-style):
 *   >= -80 dBm   → Excellent (green)
 *   -90 to -80   → Good (light green)
 *   -100 to -90  → Fair (yellow)
 *   -110 to -100 → Weak (orange)
 *   < -110       → Poor (red)
 */
export function getSignalCoverageCirclePaint(opacity: number = 0.8): CircleLayerSpecification["paint"] {
  return {
    "circle-radius": ["interpolate", ["linear"], ["zoom"], 5, 2, 10, 4, 15, 8],
    "circle-color": [
      "step",
      ["coalesce", ["get", "strength"], ["get", "signal_strength_dbm"], -120],
      SIGNAL_STRENGTH_COLORS.poor,
      -110,
      SIGNAL_STRENGTH_COLORS.weak,
      -100,
      SIGNAL_STRENGTH_COLORS.fair,
      -90,
      SIGNAL_STRENGTH_COLORS.good,
      -80,
      SIGNAL_STRENGTH_COLORS.excellent,
    ],
    "circle-opacity": opacity,
    "circle-stroke-width": 0,
  };
}

/**
 * Get provider icon ID for a given provider name
 */
export function getProviderIconId(providerName: string): string {
  const normalizedName = providerName.toLowerCase();
  if (normalizedName.includes("smart")) return "cell-smart";
  if (normalizedName.includes("dito")) return "cell-dito";
  if (normalizedName.includes("globe")) return "cell-globe";
  return "cell-default";
}

// Top-level module cache for icon Blobs to ensure fetch() runs AT MOST ONCE per asset per app session
const globalIconBlobPromises = new Map<string, Promise<Blob>>();

export async function getOrFetchIconBlob(url: string): Promise<Blob> {
  let promise = globalIconBlobPromises.get(url);
  if (!promise) {
    promise = fetch(url).then((res) => {
      if (!res.ok) throw new Error(`Failed to fetch static icon asset: ${url}`);
      return res.blob();
    });
    globalIconBlobPromises.set(url, promise);
  }
  return promise;
}

/**
 * Load provider icons into the map
 */
export async function loadProviderIcons(map: any, iconsLoadedRef: Set<string>): Promise<void> {
  const PROVIDER_ICONS: Record<string, string> = {
    "cell-globe": "/assets/images/cell-globe.png",
    "cell-smart": "/assets/images/cell-smart.png",
    "cell-dito": "/assets/images/cell-dito.png",
    "cell-default": "/assets/images/cell-site.jpg",
  };

  const iconPromises = Object.entries(PROVIDER_ICONS).map(async ([iconId, url]) => {
    if (map.hasImage?.(iconId)) {
      iconsLoadedRef.add(iconId);
      return;
    }

    try {
      const blob = await getOrFetchIconBlob(url);
      const imageBitmap = await createImageBitmap(blob);

      if (!map.hasImage(iconId)) {
        map.addImage(iconId, imageBitmap);
        iconsLoadedRef.add(iconId);
      }
    } catch (err) {
      console.warn(`Failed to load icon ${iconId}:`, err);
    }
  });

  await Promise.all(iconPromises);
}

/**
 * Create popup HTML for signal coverage point with enhanced styling
 * Matches the exact design from CellsiteMapView for consistency
 */
export function createSignalCoveragePopupHTML(properties: {
  strength?: number;
  signal_strength_dbm?: number;
  barangay?: string;
  address?: string;
  location?: string;
  azimuth?: string | number;
  network?: string;
  technology?: string;
  band?: string;
  frequency?: string;
  source_file?: string;
  duplex_mode?: string;
  areaStudy?: string;
  interpolation_note?: string;
}): string {
  const rawStrength = properties.signal_strength_dbm ?? properties.strength;
  const strength = typeof rawStrength === "number" ? rawStrength : -120;
  const signalLabel = getSignalStrengthQualityLabel(strength);
  const signalColor = getSignalStrengthColor(strength);

  const locationText = properties.address || properties.location || properties.barangay || "Unknown location";
  const azimuthText = properties.azimuth !== undefined && properties.azimuth !== null ? `${properties.azimuth}°` : "N/A";
  const network = properties.network || "Unknown";
  const networkText = properties.network || "";
  const technology = properties.technology || "Unknown";
  const band = properties.band || "N/A";
  const frequency = properties.frequency || "N/A";
  const address = properties.address || "N/A";
  const techBandText = [properties.technology, properties.band].filter(Boolean).join(" - ");
  const interpolationNote = properties.interpolation_note ? String(properties.interpolation_note) : "";

  // Enhanced styled popup with gradient background - matches CellsiteMapView
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
            ${interpolationNote ? `<div style="font-size: 11px; opacity: 0.9; margin-top: 1px;">(${interpolationNote})</div>` : ""}
            <div style="font-size: 20px; font-weight: 800; margin-top: 2px;">
              ${strength.toFixed(1)} dBm
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
          ${networkText || techBandText ? `
          <div style="
            display: flex; 
            align-items: center; 
            gap: 10px;
            padding: 8px 10px;
            background: #f1f5f9;
            border-radius: 8px;
          ">
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="#64748b" stroke-width="2">
              <path d="M2 20h20"/>
              <path d="M5 20v-4"/>
              <path d="M9 20v-8"/>
              <path d="M13 20V8"/>
              <path d="M17 20V4"/>
            </svg>
            <div style="flex: 1; min-width: 0;">
              <div style="color: #94a3b8; font-size: 10px; text-transform: uppercase; letter-spacing: 0.5px;">Network</div>
              <div style="color: #1e293b; font-weight: 600; white-space: nowrap; overflow: hidden; text-overflow: ellipsis;">
                ${networkText ? `<span style="color: #2563eb;">${networkText}</span>` : ""} ${techBandText ? `(${techBandText})` : ""}
              </div>
            </div>
          </div>` : ""}

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
              <div style="color: #1e293b; font-weight: 500; white-space: nowrap; overflow: hidden; text-overflow: ellipsis;">${locationText}</div>
            </div>
          </div>

          <div style="
            display: grid;
            grid-template-columns: 1fr 1fr;
            gap: 8px;
          ">
            <div style="padding: 8px 10px; background: #f8fafc; border-radius: 8px;">
              <div style="color: #94a3b8; font-size: 10px; text-transform: uppercase; letter-spacing: 0.5px;">Provider</div>
              <div style="color: #1e293b; font-weight: 500;">${network}</div>
            </div>
            <div style="padding: 8px 10px; background: #f8fafc; border-radius: 8px;">
              <div style="color: #94a3b8; font-size: 10px; text-transform: uppercase; letter-spacing: 0.5px;">Technology</div>
              <div style="color: #1e293b; font-weight: 500;">${technology}</div>
            </div>
            <div style="padding: 8px 10px; background: #f8fafc; border-radius: 8px;">
              <div style="color: #94a3b8; font-size: 10px; text-transform: uppercase; letter-spacing: 0.5px;">Band</div>
              <div style="color: #1e293b; font-weight: 500;">${band}</div>
            </div>
            <div style="padding: 8px 10px; background: #f8fafc; border-radius: 8px;">
              <div style="color: #94a3b8; font-size: 10px; text-transform: uppercase; letter-spacing: 0.5px;">Frequency</div>
              <div style="color: #1e293b; font-weight: 500;">${frequency}</div>
            </div>
          </div>

          <div style="padding: 8px 10px; background: #f8fafc; border-radius: 8px;">
            <div style="color: #94a3b8; font-size: 10px; text-transform: uppercase; letter-spacing: 0.5px;">Address</div>
            <div style="color: #1e293b; font-weight: 500;">${address}</div>
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
              <div style="color: #1e293b; font-weight: 500;">${azimuthText}</div>
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
              width: ${Math.max(0, Math.min(100, ((strength + 120) / 60) * 100))}%;
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
