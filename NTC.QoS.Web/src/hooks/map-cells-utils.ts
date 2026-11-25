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
 * Get dynamic signal coverage circle color paint expression.
 * If configured area study signal levels are provided, builds a MapLibre case expression
 * matching each level's threshold range [min, max) to its configured hex color.
 * Falls back to the default 3GPP color scale if no custom levels are configured.
 */
export function getSignalCoverageCircleColorPaint(
  configuredLevels?: Array<{
    thresholdFrom?: number;
    thresholdTo?: number;
    color?: string;
    isTop?: boolean;
  }>,
): any {
  const strengthExpr: any = ["coalesce", ["get", "strength"], ["get", "signal_strength_dbm"], -120];

  const default3GPP: any = [
    "step",
    strengthExpr,
    SIGNAL_STRENGTH_COLORS.poor,
    -110,
    SIGNAL_STRENGTH_COLORS.weak,
    -100,
    SIGNAL_STRENGTH_COLORS.fair,
    -90,
    SIGNAL_STRENGTH_COLORS.good,
    -80,
    SIGNAL_STRENGTH_COLORS.excellent,
  ];

  if (!configuredLevels || configuredLevels.length === 0) {
    return default3GPP;
  }

  const validLevels = configuredLevels.filter(
    (lvl) => (lvl.thresholdFrom !== undefined || lvl.thresholdTo !== undefined) && Boolean(lvl.color),
  );

  if (validLevels.length === 0) {
    return default3GPP;
  }

  const allTos = validLevels
    .map((l) => (typeof l.thresholdTo === "number" ? l.thresholdTo : undefined))
    .filter((v): v is number => v !== undefined);
  const globalMax = allTos.length > 0 ? Math.max(...allTos) : undefined;

  const caseExpr: any[] = ["case"];

  for (const level of validLevels) {
    const min = level.thresholdFrom;
    const max = level.thresholdTo;
    const isTop = level.isTop || (max !== undefined && max === globalMax) || (max !== undefined && max >= 0);

    const conditions: any[] = ["all"];
    if (min !== undefined) {
      conditions.push([">=", strengthExpr, min]);
    }
    if (max !== undefined) {
      if (isTop) {
        conditions.push(["<=", strengthExpr, max]);
      } else {
        conditions.push(["<", strengthExpr, max]);
      }
    }

    if (conditions.length > 1) {
      caseExpr.push(conditions, level.color!);
    }
  }

  if (caseExpr.length <= 1) {
    return default3GPP;
  }

  // Fallback if none of the explicit level ranges match
  caseExpr.push(default3GPP);

  return caseExpr;
}

/**
 * Get signal coverage circle layer paint configuration (color-coded by RSSI / configured signal level).
 */
export function getSignalCoverageCirclePaint(
  opacity: number = 0.8,
  configuredLevels?: Array<{
    thresholdFrom?: number;
    thresholdTo?: number;
    color?: string;
    isTop?: boolean;
  }>,
): CircleLayerSpecification["paint"] {
  return {
    "circle-radius": ["interpolate", ["linear"], ["zoom"], 5, 2, 10, 4, 15, 8],
    "circle-color": getSignalCoverageCircleColorPaint(configuredLevels),
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
export function createSignalCoveragePopupHTML(
  properties: {
    strength?: number;
    signal_strength_dbm?: number;
    cell_id?: string;
    cellId?: string;
    cell_guid?: string;
    cellGuid?: string;
    barangay?: string;
    address?: string;
    location?: string;
    azimuth?: string | number;
    network?: string;
    generation?: string;
    technology?: string;
    band?: string;
    frequency?: string;
    source_file?: string;
    duplex_mode?: string;
    areaStudy?: string;
    area_study?: string;
    interpolation_note?: string;
  },
  configuredLevels?: Array<{
    thresholdFrom?: number;
    thresholdTo?: number;
    color?: string;
    description?: string;
    isTop?: boolean;
  }>,
): string {
  const rawStrength = properties.signal_strength_dbm ?? properties.strength;
  const strength = typeof rawStrength === "number" ? rawStrength : -120;
  let signalLabel = getSignalStrengthQualityLabel(strength);
  let signalColor = getSignalStrengthColor(strength);

  if (configuredLevels && configuredLevels.length > 0) {
    const allTos = configuredLevels
      .map((l) => (typeof l.thresholdTo === "number" ? l.thresholdTo : undefined))
      .filter((v): v is number => v !== undefined);
    const globalMax = allTos.length > 0 ? Math.max(...allTos) : undefined;

    const matched = configuredLevels.find((level) => {
      const min = level.thresholdFrom;
      const max = level.thresholdTo;
      const isTop = level.isTop || (max !== undefined && max === globalMax) || (max !== undefined && max >= 0);

      if (min !== undefined && strength < min) return false;
      if (max !== undefined) {
        if (isTop) {
          if (strength > max) return false;
        } else {
          if (strength >= max) return false;
        }
      }
      return true;
    });

    if (matched) {
      if (matched.description) signalLabel = matched.description;
      if (matched.color) signalColor = matched.color;
    }
  }

  const cellGuid = properties.cell_id || properties.cellId || properties.cell_guid || properties.cellGuid || "N/A";
  const areaStudyText = properties.area_study || properties.areaStudy || properties.source_file || "N/A";
  const locationText = properties.location || properties.barangay || properties.address || "Unknown location";
  const addressText = properties.address && properties.address !== locationText ? properties.address : "";
  const rawAzimuth =
    properties.azimuth !== undefined && properties.azimuth !== null && String(properties.azimuth).trim() !== ""
      ? String(properties.azimuth).replace(/[\uFFFD°\s]+$/g, "").trim()
      : "";
  const azimuthText = rawAzimuth ? `${rawAzimuth}°` : "N/A";
  const network = properties.network || "Unknown";
  const generation = properties.generation || "";
  const technology = properties.technology || "Unknown";
  const band = properties.band || "N/A";
  const frequency = properties.frequency || "N/A";
  const techBandText = [technology, band !== "N/A" ? band : ""].filter(Boolean).join(" · ") || "N/A";
  const networkDisplay = generation ? `${network} (${generation})` : network;
  const interpolationNote = properties.interpolation_note ? String(properties.interpolation_note) : "";

  return `
    <div style="
      width: 260px;
      font-family: system-ui, -apple-system, sans-serif;
      background: #ffffff;
      border-radius: 10px;
      overflow: hidden;
      box-shadow: 0 8px 20px rgba(0,0,0,0.14);
    ">
      <!-- Compact Header -->
      <div style="
        background: linear-gradient(135deg, ${signalColor}ee, ${signalColor}cc);
        padding: 8px 24px 8px 10px;
        color: white;
      ">
        <div style="display: flex; align-items: center; gap: 8px;">
          <div style="
            width: 30px;
            height: 30px;
            border-radius: 8px;
            background: rgba(255,255,255,0.22);
            display: flex;
            align-items: center;
            justify-content: center;
            flex-shrink: 0;
          ">
            <svg width="17" height="17" viewBox="0 0 24 24" fill="white" stroke="none">
              <rect x="2" y="18" width="3" height="4" rx="1" opacity="0.35"/>
              <rect x="7" y="14" width="3" height="8" rx="1" opacity="${strength >= -100 ? "1" : "0.35"}"/>
              <rect x="12" y="10" width="3" height="12" rx="1" opacity="${strength >= -90 ? "1" : "0.35"}"/>
              <rect x="17" y="6" width="3" height="16" rx="1" opacity="${strength >= -80 ? "1" : "0.35"}"/>
            </svg>
          </div>
          <div style="min-width: 0; line-height: 1.2;">
            <div style="display: flex; align-items: baseline; gap: 6px;">
              <span style="font-size: 15px; font-weight: 800; text-shadow: 0 1px 2px rgba(0,0,0,0.2);">
                ${strength.toFixed(1)} dBm
              </span>
              <span style="font-size: 11px; font-weight: 600; opacity: 0.95; text-shadow: 0 1px 2px rgba(0,0,0,0.18);">
                ${signalLabel}
              </span>
            </div>
            ${interpolationNote ? `<div style="font-size: 10px; opacity: 0.9;">(${interpolationNote})</div>` : ""}
          </div>
        </div>
      </div>

      <!-- Compact Details -->
      <div style="padding: 8px 10px; display: flex; flex-direction: column; gap: 6px; font-size: 11.5px;">
        <div style="padding: 5px 8px; background: #f1f5f9; border-radius: 6px;">
          <div style="color: #64748b; font-size: 9px; font-weight: 600; text-transform: uppercase; letter-spacing: 0.4px;">Cell GUID</div>
          <div style="color: #0f172a; font-family: ui-monospace, SFMono-Regular, Menlo, monospace; font-size: 10.5px; font-weight: 600; word-break: break-all; margin-top: 1px;">
            ${cellGuid}
          </div>
        </div>

        <div style="padding: 5px 8px; background: #f1f5f9; border-radius: 6px;">
          <div style="color: #64748b; font-size: 9px; font-weight: 600; text-transform: uppercase; letter-spacing: 0.4px;">Area Study</div>
          <div style="color: #1e293b; font-weight: 500; white-space: nowrap; overflow: hidden; text-overflow: ellipsis;" title="${areaStudyText}">
            ${areaStudyText}
          </div>
        </div>

        <div style="padding: 5px 8px; background: #f1f5f9; border-radius: 6px;">
          <div style="color: #64748b; font-size: 9px; font-weight: 600; text-transform: uppercase; letter-spacing: 0.4px;">Location</div>
          <div style="color: #1e293b; font-weight: 500; white-space: nowrap; overflow: hidden; text-overflow: ellipsis;" title="${locationText}">
            ${locationText}
          </div>
          ${addressText ? `<div style="color: #64748b; font-size: 10.5px; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; margin-top: 1px;" title="${addressText}">${addressText}</div>` : ""}
        </div>

        <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 5px;">
          <div style="padding: 5px 7px; background: #f8fafc; border-radius: 6px;">
            <div style="color: #64748b; font-size: 9px; font-weight: 600; text-transform: uppercase; letter-spacing: 0.4px;">Network</div>
            <div style="color: #2563eb; font-weight: 600; white-space: nowrap; overflow: hidden; text-overflow: ellipsis;">${networkDisplay}</div>
          </div>
          <div style="padding: 5px 7px; background: #f8fafc; border-radius: 6px;">
            <div style="color: #64748b; font-size: 9px; font-weight: 600; text-transform: uppercase; letter-spacing: 0.4px;">Tech / Band</div>
            <div style="color: #1e293b; font-weight: 500; white-space: nowrap; overflow: hidden; text-overflow: ellipsis;">${techBandText}</div>
          </div>
          <div style="padding: 5px 7px; background: #f8fafc; border-radius: 6px;">
            <div style="color: #64748b; font-size: 9px; font-weight: 600; text-transform: uppercase; letter-spacing: 0.4px;">Frequency</div>
            <div style="color: #1e293b; font-weight: 500;">${frequency}</div>
          </div>
          <div style="padding: 5px 7px; background: #f8fafc; border-radius: 6px;">
            <div style="color: #64748b; font-size: 9px; font-weight: 600; text-transform: uppercase; letter-spacing: 0.4px;">Azimuth</div>
            <div style="color: #1e293b; font-weight: 500;">${azimuthText}</div>
          </div>
        </div>

        <!-- Signal strength bar -->
        <div style="margin-top: 2px;">
          <div style="height: 4px; background: #e2e8f0; border-radius: 2px; overflow: hidden;">
            <div style="
              height: 100%;
              width: ${Math.max(0, Math.min(100, ((strength + 120) / 60) * 100))}%;
              background: linear-gradient(90deg, ${signalColor}, ${signalColor}aa);
              border-radius: 2px;
            "></div>
          </div>
        </div>
      </div>
    </div>
  `;
}
