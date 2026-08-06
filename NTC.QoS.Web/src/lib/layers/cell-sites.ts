import { type Map } from "@maptiler/sdk";

const imageHeight = 400; // Replace with the actual height of the image in pixels
const iconOffsetY = -(imageHeight / 2); // Negative value to move the icon upward

export function addCellSitesLayer(map: Map, layerId: string, sourceId: string, sourceLayerId?: string) {
  map.addLayer({
    id: layerId,
    type: "symbol",
    source: sourceId,
    minzoom: 8,
    filter: ["==", "type", "point"],
    layout: {
      "icon-image": "cellular-icon",
      "icon-size": 0.2,
      "icon-allow-overlap": true,
      "icon-pitch-alignment": "viewport",
      "icon-offset": [0, iconOffsetY], // Dynamically offset based on image height
    },
    ...(sourceLayerId ? { "source-layer": sourceLayerId } : {}),
  });
  return map;
}
