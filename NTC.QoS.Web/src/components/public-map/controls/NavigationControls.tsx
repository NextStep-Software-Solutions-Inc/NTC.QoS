"use client";

import { useCallback, useEffect, useState } from "react";
import { motion } from "framer-motion";
import { Box, Compass, Locate, Maximize, Minus, Mountain, Plus } from "lucide-react";

import { useMap } from "@/hooks/useMap";
import { GlassPanel } from "./GlassPanel";
import { MapControlButton } from "./MapControlButton";

export function NavigationControls() {
  const { map, isMapLoaded } = useMap();
  const [isLocating, setIsLocating] = useState(false);
  const [isFullscreen, setIsFullscreen] = useState(false);
  const [bearing, setBearing] = useState(0);
  const [_pitch, setPitch] = useState(0);
  const [is3DMode, setIs3DMode] = useState(false);
  const [terrainEnabled, setTerrainEnabled] = useState(false);

  // Track map bearing and pitch for compass rotation
  useEffect(() => {
    if (!map || !isMapLoaded) return;

    const handleRotate = () => {
      setBearing(map.getBearing());
    };

    const handlePitch = () => {
      setPitch(map.getPitch());
      setIs3DMode(map.getPitch() > 0);
    };

    map.on("rotate", handleRotate);
    map.on("pitch", handlePitch);
    return () => {
      map.off("rotate", handleRotate);
      map.off("pitch", handlePitch);
    };
  }, [map, isMapLoaded]);

  // Track fullscreen state
  useEffect(() => {
    const handleFullscreenChange = () => {
      setIsFullscreen(!!document.fullscreenElement);
    };

    document.addEventListener("fullscreenchange", handleFullscreenChange);
    return () => {
      document.removeEventListener("fullscreenchange", handleFullscreenChange);
    };
  }, []);

  const handleZoomIn = useCallback(() => {
    if (!map) return;
    map.zoomIn({ duration: 300 });
  }, [map]);

  const handleZoomOut = useCallback(() => {
    if (!map) return;
    map.zoomOut({ duration: 300 });
  }, [map]);

  const handleResetBearing = useCallback(() => {
    if (!map) return;
    map.easeTo({ bearing: 0, pitch: 0, duration: 500 });
  }, [map]);

  const handleToggle3D = useCallback(() => {
    if (!map) return;
    const newPitch = is3DMode ? 0 : 60;
    map.easeTo({ pitch: newPitch, duration: 500 });
    setIs3DMode(!is3DMode);
  }, [map, is3DMode]);

  const handleToggleTerrain = useCallback(() => {
    if (!map) return;

    if (terrainEnabled) {
      // Disable terrain
      map.setTerrain(null);
      setTerrainEnabled(false);
    } else {
      // Enable terrain - first check if terrain source exists
      if (!map.getSource("terrain")) {
        map.addSource("terrain", {
          type: "raster-dem",
          url: "https://api.maptiler.com/tiles/terrain-rgb-v2/tiles.json",
        });
      }
      map.setTerrain({ source: "terrain", exaggeration: 1.5 });
      // Also enable 3D view to see terrain effect
      if (!is3DMode) {
        map.easeTo({ pitch: 60, duration: 500 });
        setIs3DMode(true);
      }
      setTerrainEnabled(true);
    }
  }, [map, terrainEnabled, is3DMode]);

  const handleGeolocate = useCallback(() => {
    if (!map || !navigator.geolocation) return;

    setIsLocating(true);
    navigator.geolocation.getCurrentPosition(
      (position) => {
        map.flyTo({ center: [position.coords.longitude, position.coords.latitude], zoom: 14, duration: 2000 });
        setIsLocating(false);
      },
      () => {
        setIsLocating(false);
      },
      { enableHighAccuracy: true },
    );
  }, [map]);

  const handleFullscreen = useCallback(() => {
    const mapContainer = map?.getContainer();
    if (!mapContainer) return;

    if (document.fullscreenElement) {
      document.exitFullscreen();
    } else {
      mapContainer.requestFullscreen();
    }
  }, [map]);

  return (
    <GlassPanel className="flex flex-col gap-1 p-1.5">
      <MapControlButton tooltip="Zoom in" onClick={handleZoomIn}>
        <Plus className="h-5 w-5 text-gray-700" />
      </MapControlButton>

      <MapControlButton tooltip="Zoom out" onClick={handleZoomOut}>
        <Minus className="h-5 w-5 text-gray-700" />
      </MapControlButton>

      <div className="h-px bg-gray-200 my-1" />

      <MapControlButton tooltip="Reset north" onClick={handleResetBearing}>
        <motion.div animate={{ rotate: -bearing }} transition={{ type: "spring", stiffness: 300, damping: 30 }}>
          <Compass className="h-5 w-5 text-gray-700" />
        </motion.div>
      </MapControlButton>

      <MapControlButton tooltip={is3DMode ? "2D view" : "3D view (tilt)"} onClick={handleToggle3D} active={is3DMode}>
        <Box className={`h-5 w-5 ${is3DMode ? "text-primary" : "text-gray-700"}`} />
      </MapControlButton>

      <MapControlButton
        tooltip={terrainEnabled ? "Disable terrain" : "Enable terrain"}
        onClick={handleToggleTerrain}
        active={terrainEnabled}>
        <Mountain className={`h-5 w-5 ${terrainEnabled ? "text-primary" : "text-gray-700"}`} />
      </MapControlButton>

      <div className="h-px bg-gray-200 my-1" />

      <MapControlButton tooltip="My location" onClick={handleGeolocate} disabled={isLocating}>
        <motion.div
          animate={isLocating ? { scale: [1, 1.2, 1] } : { scale: 1 }}
          transition={{ repeat: isLocating ? Infinity : 0, duration: 1 }}>
          <Locate className={`h-5 w-5 ${isLocating ? "text-primary" : "text-gray-700"}`} />
        </motion.div>
      </MapControlButton>

      <MapControlButton
        tooltip={isFullscreen ? "Exit fullscreen" : "Fullscreen"}
        onClick={handleFullscreen}
        active={isFullscreen}>
        <Maximize className="h-5 w-5 text-gray-700" />
      </MapControlButton>
    </GlassPanel>
  );
}
