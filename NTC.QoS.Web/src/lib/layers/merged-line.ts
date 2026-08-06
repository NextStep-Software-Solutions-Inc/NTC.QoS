import { type Map } from "@maptiler/sdk";

export function addMergedLineLayer(map: Map, layerId: string, sourceId: string, sourceLayerId?: string) {
  map.addLayer(
    {
      id: layerId,
      ...(sourceLayerId ? { "source-layer": sourceLayerId } : {}),
      source: sourceId,
      minzoom: 14,
      type: "line",
      paint: { "line-color": "rgba(0, 550, 50, 0.5)", "line-width": 2 },
    },
    "merged-polygon-layer",
  );
  return map;
}
