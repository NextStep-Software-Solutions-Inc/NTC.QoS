"use client";

import { useEffect, useRef } from "react";

import { useMap } from "@/hooks/useMap";

export const useMapMove = () => {
  const { map, isMapLoaded } = useMap();
  const lastUpdateRef = useRef<number>(0);
  const updateTimeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  useEffect(() => {
    if (!map || !isMapLoaded) return;

    const handleMove = () => {
      const now = Date.now();
      const timeSinceLastUpdate = now - lastUpdateRef.current;

      // Throttle history updates to max once per 2 seconds
      if (timeSinceLastUpdate < 2000) {
        // Clear any pending update
        if (updateTimeoutRef.current) {
          clearTimeout(updateTimeoutRef.current);
        }

        // Schedule update for later
        updateTimeoutRef.current = setTimeout(() => {
          updateURL();
        }, 2000 - timeSinceLastUpdate);
        return;
      }

      updateURL();
    };

    const updateURL = () => {
      try {
        if (!map || !map.isStyleLoaded()) return;
        const center = map.getCenter();
        const zoom = map.getZoom();

        const lat = center.lat.toFixed(2);
        const lng = center.lng.toFixed(2);
        const searchParams = new URLSearchParams(window.location.search);
        searchParams.set("lat", lat);
        searchParams.set("lng", lng);
        searchParams.set("zoom", zoom.toFixed(2));

        window.history.replaceState({}, "", `?${searchParams.toString()}`);
        lastUpdateRef.current = Date.now();
      } catch (error) {
        // Silently fail if browser blocks the update
        console.warn("Failed to update URL:", error);
      }
    };

    map.on("moveend", handleMove);

    return () => {
      map.off("moveend", handleMove);
      if (updateTimeoutRef.current) {
        clearTimeout(updateTimeoutRef.current);
      }
    };
  }, [map, isMapLoaded]);
};
