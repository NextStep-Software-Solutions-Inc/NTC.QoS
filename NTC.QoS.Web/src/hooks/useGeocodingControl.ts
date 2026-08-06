"use client";

import { useEffect } from "react";
import { GeocodingControl } from "@maptiler/geocoding-control/maptilersdk";

import { useMap } from "./useMap";

export const useGeocodingControl = () => {
  const { map } = useMap();

  useEffect(() => {
    if (map === null) return;

    const createGC = () => {
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
    };

    map.on("load", createGC);

    return () => {
      map.off("load", createGC);
    };
  }, [map]);
};
