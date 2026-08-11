"use client";

import React, { createContext, useCallback, useContext, useEffect, useRef, useState, type ReactNode } from "react";
import { useSearchParams } from "next/navigation";
import * as maptilersdk from "@maptiler/sdk";

import "@maptiler/sdk/dist/maptiler-sdk.css";

import { PHILIPPINES_BOUNDS } from "@/lib/constants/map-bounds";

interface MapContextProps {
  map: maptilersdk.Map | null;
  setMapContainer: (node: HTMLDivElement | null) => void;
  setMapStyle: (mapType: maptilersdk.ReferenceMapStyle | maptilersdk.MapStyleVariant) => void;
  isMapLoaded: boolean;
}

interface MapProviderProps {
  apiKey: string;
  children: ReactNode;
}

const MapContext = createContext<MapContextProps | undefined>(undefined);

export const MapProvider = ({ children, apiKey }: MapProviderProps) => {
  const [map, setMap] = useState<maptilersdk.Map | null>(null);
  const mapContainer = useRef<HTMLDivElement | null>(null);
  const [containerReady, setContainerReady] = useState(false);
  const [isMapLoaded, setIsMapLoaded] = useState(false);

  // Set API key when component mounts
  useEffect(() => {
    maptilersdk.config.apiKey = apiKey;
  }, [apiKey]);
  const center = useRef<{ lng: number; lat: number }>({ lng: 122.405211, lat: 12.796927 });
  const zoomLevel = useRef(5.38);

  const searchParams = useSearchParams();
  const lat = searchParams.get("lat");
  const lng = searchParams.get("lng");
  const zoom = searchParams.get("zoom");

  // Set initial values from URL params
  useEffect(() => {
    if (lat && lng) {
      center.current = { lng: parseFloat(lng), lat: parseFloat(lat) };
    }
    if (zoom) {
      zoomLevel.current = parseFloat(zoom);
    }
  }, [lat, lng, zoom]);

  const setMapContainer = useCallback((node: HTMLDivElement | null) => {
    mapContainer.current = node;
    setContainerReady(!!node);
  }, []);

  useEffect(() => {
    if (!mapContainer.current || map) return;
    const newMap = new maptilersdk.Map({
      container: mapContainer.current,
      style: maptilersdk.MapStyle.TOPO,
      center: [center.current.lng, center.current.lat],
      zoom: zoomLevel.current,
      projection: "mercator",
      maxZoom: 20,
      minZoom: 4.5, // Updated min zoom
      // Set bounds to Philippines
      maxBounds: [
        [PHILIPPINES_BOUNDS.minLng - 2, PHILIPPINES_BOUNDS.minLat - 2], // Southwest
        [PHILIPPINES_BOUNDS.maxLng + 2, PHILIPPINES_BOUNDS.maxLat + 2], // Northeast
      ],
      // Disable all default controls - we use custom ones
      navigationControl: false,
      geolocateControl: false,
      scaleControl: false,
      fullscreenControl: false,
      terrainControl: false,
      // Required for canvas.toDataURL() to return non-blank screenshots
      canvasContextAttributes: { preserveDrawingBuffer: true },
    });

    newMap.on("load", () => {
      setMap(newMap);
      setIsMapLoaded(true);
    });

    return () => {
      // if (newMap) newMap.remove();
    };
  }, [containerReady, map]);

  const setMapStyle = useCallback(
    (mapType: maptilersdk.ReferenceMapStyle | maptilersdk.MapStyleVariant) => {
      if (map) {
        // Preserve current map position when changing styles
        const currentCenter = map.getCenter();
        const currentZoom = map.getZoom();
        const currentBearing = map.getBearing();
        const currentPitch = map.getPitch();

        map.setStyle(mapType);

        // Restore position after style change
        map.once("styledata", () => {
          map.setCenter([currentCenter.lng, currentCenter.lat]);
          map.setZoom(currentZoom);
          map.setBearing(currentBearing);
          map.setPitch(currentPitch);
        });
      }
    },
    [map],
  );

  return (
    <MapContext.Provider value={{ map, setMapContainer, isMapLoaded, setMapStyle }}>{children}</MapContext.Provider>
  );
};
interface UseMapOptions {
  zoom: 4.5;
}

export const useMap = (options?: UseMapOptions) => {
  const mapContext = useContext(MapContext);
  if (!mapContext) {
    throw new Error("useMap must be used within a MapProvider");
  }
  options?.zoom && mapContext.map?.setZoom(options.zoom);
  return mapContext;
};
