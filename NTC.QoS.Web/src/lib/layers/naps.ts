import { type Map } from "@maptiler/sdk";

export function addNAPsLayer(
  map: Map,
  layerId: string,
  sourceId: string,
  providersName: string[],
  sourceLayerId?: string,
) {
  const iconColorCase = providersName
    .map((provider) => {
      return [
        ["==", ["get", "providerName"], provider.toLocaleLowerCase()],
        `pulsing-dot-${provider.toLocaleLowerCase()}`,
      ];
    })
    .flatMap((item) => item);

  map.addLayer({
    id: layerId,
    type: "symbol",
    source: sourceId,
    // minzoom: 5,
    layout: {
      "icon-image": ["case", ...(iconColorCase as never), "pulsing-dot"],
      // "icon-size": 0.1,
      "icon-allow-overlap": false,
      "icon-pitch-alignment": "map",
    },
    ...(sourceLayerId ? { "source-layer": sourceLayerId } : {}),
  });
  return map;
}
