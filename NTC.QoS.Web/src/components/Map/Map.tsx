"use client";

import "@maptiler/sdk/dist/maptiler-sdk.css";

import { useEffect } from "react";
import { GeocodingControl } from "@maptiler/geocoding-control/maptilersdk";

import "@maptiler/geocoding-control/style.css";

import { useMap } from "@/hooks/useMap";

export default function Map() {
  const { map, setMapContainer } = useMap();

  useEffect(() => {
    if (map === null) return;

    const gc = new GeocodingControl({
      types: ["municipality", "region", "country"],
      country: "ph",
      errorMessage: "Something went wrong with the search",
      noResultsMessage: "No results found or out of bounds. Try searching within the Philippines",
    });

    map.addControl(gc, "top-left");

    return () => {
      map.removeControl(gc);
    };
  }, [map]);

  return (
    <div className="relative h-full w-full">
      <div ref={(ref) => setMapContainer(ref)} className="relative h-full w-full rounded-[5px]" />
    </div>
  );
}
