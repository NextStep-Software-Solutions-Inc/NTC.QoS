import { type Map } from "@maptiler/sdk";

export function addCellSitesCircleLayer(map: Map, layerId: string, sourceId: string, sourceLayerId?: string) {
  map.addLayer(
    {
      id: layerId,
      type: "fill",
      source: sourceId,
      minzoom: 10,
      maxzoom: 13.5,
      filter: ["==", "type", "polygon"],
      paint: { "fill-color": "#014F86", "fill-opacity": 0.2 },
      ...(sourceLayerId ? { "source-layer": sourceLayerId } : {}),
    },
    "cellular-sites-layer",
  );
  map.addLayer(
    {
      id: `${layerId}-stroke`,
      type: "line",
      source: sourceId,
      minzoom: 10,
      filter: ["==", "type", "polygon"],
      paint: { "line-color": "#ffffff", "line-width": 1 },
      ...(sourceLayerId ? { "source-layer": sourceLayerId } : {}),
    },
    "cellular-sites-layer",
  );

  return map;
}
