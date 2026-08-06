import { type Map } from "@maptiler/sdk";

export function addMergedPolygonLayer(map: Map, layerId: string, sourceId: string, sourceLayerId?: string) {
  map.addLayer(
    {
      id: layerId,
      "source-layer": sourceLayerId,
      source: sourceId,
      minzoom: 14,
      type: "fill",
      paint: { "fill-color": "rgba(0, 550, 50, 0.2)" },
    },
    "Meadow",
  );
  return map;
}
