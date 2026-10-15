import { useEffect } from "react";
import { Marker, type MapMouseEvent } from "@maptiler/sdk";

import { useMap } from "./useMap";

const marker = new Marker({ anchor: "center", className: "hover:cursor-pointer", offset: [0, -20] });

export const useClickMarker = () => {
  const { map, isMapLoaded } = useMap();
  useEffect(() => {
    if (!map || !isMapLoaded) return;

    // When clicking on the map, create a marker at the clicked location and remove any existing markers
    const handleMapClick = (event: MapMouseEvent) => {
      // Transfer the old marker to the new location
      if (marker.getLngLat()) {
        marker.setLngLat(event.lngLat);
      } else {
        marker.setLngLat(event.lngLat).addTo(map);
      }
    };

    map.on("click", handleMapClick);

    // Cleanup function to remove the event listener
    return () => {
      map.off("click", handleMapClick);
    };
  }, [isMapLoaded, map]);
};
