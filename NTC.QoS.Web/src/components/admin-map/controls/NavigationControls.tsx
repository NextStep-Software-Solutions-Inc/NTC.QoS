/**
 * Navigation Controls Component
 *
 * Zoom, compass, geolocation, 3D view, and fullscreen controls.
 * Follows Google Maps-inspired design with glassmorphic styling.
 */

"use client";

import { memo, useCallback, useEffect, useState } from "react";
import { motion } from "framer-motion";
import { Box, Compass, Locate, Maximize, Minimize, Minus, Mountain, Plus, RotateCcw } from "lucide-react";

import { useMap } from "@/hooks/useMap";
import { GlassPanel } from "../ui/GlassPanel";
import { MapControlButton } from "../ui/MapControlButton";

interface NavigationControlsProps {
  className?: string;
  showTerrain?: boolean;
  show3D?: boolean;
}

export const NavigationControls = memo(function NavigationControls({
  className,
  showTerrain = true,
  show3D = true,
}: NavigationControlsProps) {
  const { map, isMapLoaded } = useMap();
  const [isLocating, setIsLocating] = useState(false);
  const [isFullscreen, setIsFullscreen] = useState(false);
  const [bearing, setBearing] = useState(0);
  const [pitch, setPitch] = useState(0);
  const [is3DMode, setIs3DMode] = useState(false);
  const [terrainEnabled, setTerrainEnabled] = useState(false);
  const [currentZoom, setCurrentZoom] = useState(5);

  // Track map state changes
  useEffect(() => {
    if (!map || !isMapLoaded) return;

    const handleRotate = () => setBearing(map.getBearing());
    const handlePitch = () => {
      const p = map.getPitch();
      setPitch(p);
      setIs3DMode(p > 0);
    };
    const handleZoom = () => setCurrentZoom(Math.round(map.getZoom() * 10) / 10);

    map.on("rotate", handleRotate);
    map.on("pitch", handlePitch);
    map.on("zoom", handleZoom);

    // Set initial values safely
    try {
      if (map.isStyleLoaded()) {
        setBearing(map.getBearing());
        setPitch(map.getPitch());
        setCurrentZoom(Math.round(map.getZoom() * 10) / 10);
      }
    } catch (e) {
      console.warn("Failed to get initial map view values:", e);
    }

    return () => {
      map.off("rotate", handleRotate);
      map.off("pitch", handlePitch);
      map.off("zoom", handleZoom);
    };
  }, [map, isMapLoaded]);

  // Track fullscreen state
  useEffect(() => {
    const handleFullscreenChange = () => {
      setIsFullscreen(!!document.fullscreenElement);
    };
    document.addEventListener("fullscreenchange", handleFullscreenChange);
    return () => document.removeEventListener("fullscreenchange", handleFullscreenChange);
  }, []);

  const handleZoomIn = useCallback(() => {
    if (!map) return;
    map.zoomIn({ duration: 300 });
  }, [map]);

  const handleZoomOut = useCallback(() => {
    if (!map) return;
    map.zoomOut({ duration: 300 });
  }, [map]);

  const handleResetView = useCallback(() => {
    if (!map) return;
    map.easeTo({
      center: [122.405211, 12.796927], // Philippines center
      zoom: 5.38,
      bearing: 0,
      pitch: 0,
      duration: 1000,
    });
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
      map.setTerrain(null);
      setTerrainEnabled(false);
    } else {
      if (!map.getSource("terrain")) {
        map.addSource("terrain", {
          type: "raster-dem",
          url: "https://api.maptiler.com/tiles/terrain-rgb-v2/tiles.json",
        });
      }
      map.setTerrain({ source: "terrain", exaggeration: 1.5 });
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
    <div className={className}>
      <GlassPanel padding="sm" className="flex flex-col gap-1">
        {/* Zoom controls */}
        <MapControlButton tooltip="Zoom in" onClick={handleZoomIn} size="sm">
          <Plus className="h-5 w-5 text-gray-700" />
        </MapControlButton>

        {/* Zoom level indicator */}
        <div className="text-center text-xs font-medium text-gray-500 py-0.5">{currentZoom.toFixed(1)}</div>

        <MapControlButton tooltip="Zoom out" onClick={handleZoomOut} size="sm">
          <Minus className="h-5 w-5 text-gray-700" />
        </MapControlButton>

        <div className="h-px bg-gray-200/50 my-1" />

        {/* Compass / Reset north */}
        <MapControlButton
          tooltip={bearing !== 0 || pitch !== 0 ? "Reset north" : "North"}
          onClick={handleResetBearing}
          active={bearing !== 0}
          size="sm">
          <motion.div animate={{ rotate: -bearing }} transition={{ type: "spring", stiffness: 300, damping: 30 }}>
            <Compass className="h-5 w-5 text-gray-700" />
          </motion.div>
        </MapControlButton>

        {/* 3D Toggle */}
        {show3D && (
          <MapControlButton
            tooltip={is3DMode ? "2D View" : "3D View"}
            onClick={handleToggle3D}
            active={is3DMode}
            size="sm">
            <Box className={`h-5 w-5 ${is3DMode ? "text-primary" : "text-gray-700"}`} />
          </MapControlButton>
        )}

        {/* Terrain Toggle */}
        {showTerrain && (
          <MapControlButton
            tooltip={terrainEnabled ? "Disable terrain" : "Enable terrain"}
            onClick={handleToggleTerrain}
            active={terrainEnabled}
            size="sm">
            <Mountain className={`h-5 w-5 ${terrainEnabled ? "text-primary" : "text-gray-700"}`} />
          </MapControlButton>
        )}

        <div className="h-px bg-gray-200/50 my-1" />

        {/* Geolocation */}
        <MapControlButton tooltip="My location" onClick={handleGeolocate} disabled={isLocating} size="sm">
          <motion.div
            animate={isLocating ? { scale: [1, 1.2, 1] } : { scale: 1 }}
            transition={{ repeat: isLocating ? Infinity : 0, duration: 1 }}>
            <Locate className={`h-5 w-5 ${isLocating ? "text-primary" : "text-gray-700"}`} />
          </motion.div>
        </MapControlButton>

        {/* Reset view */}
        <MapControlButton tooltip="Reset view" onClick={handleResetView} size="sm">
          <RotateCcw className="h-5 w-5 text-gray-700" />
        </MapControlButton>

        {/* Fullscreen */}
        <MapControlButton
          tooltip={isFullscreen ? "Exit fullscreen" : "Fullscreen"}
          onClick={handleFullscreen}
          active={isFullscreen}
          size="sm">
          {isFullscreen ? (
            <Minimize className="h-5 w-5 text-gray-700" />
          ) : (
            <Maximize className="h-5 w-5 text-gray-700" />
          )}
        </MapControlButton>
      </GlassPanel>
    </div>
  );
});
