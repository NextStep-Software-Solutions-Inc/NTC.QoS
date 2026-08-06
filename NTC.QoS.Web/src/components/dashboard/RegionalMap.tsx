"use client";

import React, { useEffect, useRef } from "react";
import { GeolocateControl, Map, NavigationControl } from "@maptiler/sdk";

import "@maptiler/sdk/dist/maptiler-sdk.css";

import { cn } from "@/lib/utils";

const RegionalMap = () => {
  const mapContainerRef = useRef<HTMLDivElement>(null);
  const maptilerKey = process.env.NEXT_PUBLIC_MAPTILER_API_KEY;

  // Region 10 Boundary (GeoJSON)

  useEffect(() => {
    if (!mapContainerRef.current) return;

    // Initialize Map
    const map = new Map({
      container: mapContainerRef.current,
      style: `https://api.maptiler.com/maps/satellite/style.json?key=${maptilerKey}`,
      center: [124.623, 8.038], // Center at Region 10
      zoom: 10,
      //   interactive:false
    });

    // Add Navigation & Geolocation Controls (Only at Bottom Right)
    map.addControl(new NavigationControl(), "bottom-right");
    map.addControl(
      new GeolocateControl({ positionOptions: { enableHighAccuracy: true }, trackUserLocation: true }),
      "bottom-right",
    );

    // Add Region 10 Boundary Layer
    map.on("load", () => {
      map.addSource("region10", {
        type: "geojson",
        data: {
          type: "FeatureCollection",
          features: [
            {
              type: "Feature",
              properties: { name: "Northern Mindanao" },
              geometry: { type: "Polygon", coordinates: [] },
            },
          ],
        },
      });

      map.addLayer({
        id: "region10-boundary",
        type: "fill",
        source: "region10",
        layout: {},
        paint: { "fill-color": "#0080ff", "fill-opacity": 0.3 },
      });

      // Add a border outline
      map.addLayer({
        id: "region10-outline",
        type: "line",
        source: "region10",
        layout: {},
        paint: { "line-color": "#0000ff", "line-width": 2 },
      });
    });

    return () => map.remove();
  }, [maptilerKey]);

  return <div className={cn("relative h-screen w-full")} ref={mapContainerRef} />;
};

export default RegionalMap;
