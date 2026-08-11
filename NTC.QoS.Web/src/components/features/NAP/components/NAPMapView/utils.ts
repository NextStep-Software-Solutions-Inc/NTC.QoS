import type * as maptilersdk from "@maptiler/sdk";

import { type NAPDetails } from "@/components/features/NAP/types/NAP";
import { DEFAULT_PROVIDER_COLOR } from "./constants";

/**
 * Generate HTML content for NAP popup
 */
export function createNAPPopupHTML(nap: NAPDetails): string {
  const color = nap.ISPColor || DEFAULT_PROVIDER_COLOR;
  const provider = nap.ISP || "Unknown";

  return `
    <div class="p-3">
      <div class="flex items-center gap-2 mb-2">
        <div class="w-3 h-3 rounded-full flex-shrink-0" style="background-color: ${color}"></div>
        <span class="font-semibold text-sm text-gray-900">${provider}</span>
      </div>
      <div class="text-xs text-gray-500 space-y-1">
        <div class="flex items-center gap-1.5 bg-gray-50 rounded px-2 py-1">
          <svg class="w-3 h-3 flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z"/>
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 11a3 3 0 11-6 0 3 3 0 016 0z"/>
          </svg>
          <code class="font-mono text-[10px]">${nap.lat.toFixed(6)}, ${nap.lng.toFixed(6)}</code>
        </div>
        <div class="text-[10px] text-gray-400 truncate mt-1">ID: ${nap.id.slice(0, 20)}...</div>
      </div>
    </div>
  `;
}

/**
 * Calculate dominant color from a list of provider colors
 * Returns the color that appears most frequently
 */

/**
 * Get cluster layer paint configuration
 * Uses dominant color from clusterProperties or fallback gradient
 */
export function getClusterLayerPaint(): maptilersdk.CircleLayerSpecification["paint"] {
  return {
    // Use dominant color from cluster properties or fallback gradient
    "circle-color": [
      "case",
      ["has", "dominantColor"],
      ["get", "dominantColor"],
      // Fallback gradient based on count
      [
        "step",
        ["get", "point_count"],
        "#60a5fa", // blue for small clusters
        50,
        "#8b5cf6", // purple for medium
        200,
        "#ec4899", // pink for large
      ],
    ],
    // Smaller radius: 10/14/18
    "circle-radius": ["step", ["get", "point_count"], 10, 50, 14, 200, 18],
    "circle-stroke-width": 2,
    "circle-stroke-color": "rgba(255,255,255,0.9)",
  };
}

/**
 * Get individual NAP circle layer paint configuration
 */
export function getNAPCircleLayerPaint(): maptilersdk.CircleLayerSpecification["paint"] {
  return {
    "circle-radius": ["case", ["boolean", ["get", "isSelected"], false], 10, 6],
    "circle-color": ["case", ["boolean", ["get", "hasError"], false], "#ef4444", ["get", "ispColor"]],
    "circle-stroke-width": ["case", ["boolean", ["get", "isSelected"], false], 3, 2],
    "circle-stroke-color": ["case", ["boolean", ["get", "isSelected"], false], "#3b82f6", "rgba(255,255,255,0.9)"],
    "circle-opacity": ["case", ["boolean", ["get", "isPending"], false], 0.5, 1],
  };
}

/**
 * Get point view layer paint for small dots (no border/stroke, 50% opacity)
 */
export function getPointViewCirclePaint(): maptilersdk.CircleLayerSpecification["paint"] {
  return { "circle-radius": 2.5, "circle-color": ["get", "ispColor"], "circle-stroke-width": 0, "circle-opacity": 0.5 };
}
