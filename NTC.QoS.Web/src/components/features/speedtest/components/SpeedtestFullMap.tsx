"use client";

import "@maptiler/sdk/dist/maptiler-sdk.css";
import "@/styles/maptiler.css";

import React, { useCallback, useEffect, useMemo, useRef, useState } from "react";
import * as maptilersdk from "@maptiler/sdk";
import {
  Download,
  Edit2,
  Eye,
  Gauge,
  Layers,
  LocateFixed,
  MapPin,
  Signal,
  Thermometer,
  Trash2,
  Upload,
  X,
  ZoomIn,
  ZoomOut,
} from "lucide-react";

import { type SpeedTest } from "@/components/features/speedtest/types/SpeedTest";
import MapType from "@/components/Map/MapType";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { cn } from "@/lib/utils";

// Philippines center and bounds
const PH_CENTER: [number, number] = [122.5, 12.5];
const PH_ZOOM = 5.5;
const PH_BOUNDS: [[number, number], [number, number]] = [
  [116.0, 4.5],
  [127.0, 21.0],
];

// Signal quality colors
const SIGNAL_COLORS = {
  Excellent: "#10b981",
  Good: "#22c55e",
  Average: "#f59e0b",
  Weak: "#ef4444",
  VeryWeak: "#dc2626",
  Unknown: "#6b7280",
};

interface SpeedtestFullMapProps {
  speedtests: SpeedTest[];
  isLoading?: boolean;
  onAddSpeedtest?: (lat: number, lng: number) => void;
  onEditSpeedtest?: (speedtest: SpeedTest) => void;
  onDeleteSpeedtest?: (speedtest: SpeedTest) => void;
  onSelectSpeedtest?: (speedtest: SpeedTest | null) => void;
  selectedSpeedtestId?: string;
  className?: string;
}

export default function SpeedtestFullMap({
  speedtests,
  isLoading = false,
  onAddSpeedtest,
  onEditSpeedtest,
  onDeleteSpeedtest,
  onSelectSpeedtest,
  selectedSpeedtestId,
  className,
}: SpeedtestFullMapProps) {
  const mapContainerRef = useRef<HTMLDivElement>(null);
  const mapRef = useRef<maptilersdk.Map | null>(null);
  const popupRef = useRef<maptilersdk.Popup | null>(null);

  const [mapLoaded, setMapLoaded] = useState(false);
  const [showHeatmap, setShowHeatmap] = useState(true);
  const [showPoints, setShowPoints] = useState(true);
  const [zoomLevel, setZoomLevel] = useState(PH_ZOOM);
  const [hoveredSpeedtest, setHoveredSpeedtest] = useState<SpeedTest | null>(null);

  // Convert speedtests to GeoJSON
  const speedtestsGeoJSON = useMemo(() => {
    const features = speedtests
      .filter((st) => st.latitude != null && st.longitude != null)
      .map((st) => ({
        type: "Feature" as const,
        geometry: { type: "Point" as const, coordinates: [st.longitude!, st.latitude!] },
        properties: {
          id: st.id,
          downloadSpeed: st.downloadSpeed || 0,
          uploadSpeed: st.uploadSpeed || 0,
          ping: st.ping || 0,
          signalQuality: st.signalQuality || "Unknown",
          provider: st.serviceProvider?.provider?.name || "Unknown",
          providerColor: st.serviceProvider?.provider?.color || "#6b7280",
          isSelected: st.id === selectedSpeedtestId,
        },
      }));

    return { type: "FeatureCollection" as const, features };
  }, [speedtests, selectedSpeedtestId]);

  // Initialize map
  useEffect(() => {
    if (!mapContainerRef.current || mapRef.current) return;

    // Configure MapTiler SDK
    maptilersdk.config.apiKey = process.env.NEXT_PUBLIC_MAPTILER_API_KEY || "";

    const map = new maptilersdk.Map({
      container: mapContainerRef.current,
      style: maptilersdk.MapStyle.TOPO,
      center: PH_CENTER,
      zoom: PH_ZOOM,
      maxBounds: PH_BOUNDS,
      minZoom: 4,
      maxZoom: 18,
      // Required for canvas.toDataURL() to return non-blank screenshots
      canvasContextAttributes: { preserveDrawingBuffer: true },
    });

    mapRef.current = map;

    map.on("load", () => {
      setMapLoaded(true);
      setZoomLevel(map.getZoom());

      // Add source for speedtests
      map.addSource("speedtests", {
        type: "geojson",
        data: speedtestsGeoJSON,
        cluster: true,
        clusterMaxZoom: 14,
        clusterRadius: 50,
      });

      // Add heatmap layer
      map.addLayer({
        id: "speedtests-heatmap",
        type: "heatmap",
        source: "speedtests",
        maxzoom: 15,
        paint: {
          "heatmap-weight": ["interpolate", ["linear"], ["get", "downloadSpeed"], 0, 0.1, 50, 0.5, 100, 1],
          "heatmap-intensity": ["interpolate", ["linear"], ["zoom"], 0, 1, 15, 3],
          "heatmap-color": [
            "interpolate",
            ["linear"],
            ["heatmap-density"],
            0,
            "rgba(6, 182, 212, 0)",
            0.2,
            "rgba(59, 130, 246, 0.4)",
            0.4,
            "rgba(34, 197, 94, 0.6)",
            0.6,
            "rgba(250, 204, 21, 0.8)",
            0.8,
            "rgba(249, 115, 22, 0.9)",
            1,
            "rgba(239, 68, 68, 1)",
          ],
          "heatmap-radius": ["interpolate", ["linear"], ["zoom"], 0, 10, 15, 30],
          "heatmap-opacity": 0.8,
        },
      });

      // Add cluster circles layer
      map.addLayer({
        id: "speedtests-clusters",
        type: "circle",
        source: "speedtests",
        filter: ["has", "point_count"],
        paint: {
          "circle-color": ["step", ["get", "point_count"], "#06b6d4", 10, "#3b82f6", 50, "#8b5cf6", 100, "#d946ef"],
          "circle-radius": ["step", ["get", "point_count"], 15, 10, 20, 50, 25, 100, 30],
          "circle-stroke-width": 2,
          "circle-stroke-color": "#fff",
        },
      });

      // Add cluster count labels
      map.addLayer({
        id: "speedtests-cluster-count",
        type: "symbol",
        source: "speedtests",
        filter: ["has", "point_count"],
        layout: { "text-field": "{point_count_abbreviated}", "text-font": ["Open Sans Bold"], "text-size": 12 },
        paint: { "text-color": "#fff" },
      });

      // Add individual points layer
      map.addLayer({
        id: "speedtests-points",
        type: "circle",
        source: "speedtests",
        filter: ["!has", "point_count"],
        paint: {
          "circle-radius": ["case", ["get", "isSelected"], 10, 6],
          "circle-color": ["get", "providerColor"],
          "circle-stroke-width": ["case", ["get", "isSelected"], 3, 2],
          "circle-stroke-color": ["case", ["get", "isSelected"], "#06b6d4", "#fff"],
          "circle-opacity": 0.9,
        },
      });

      // Click on cluster to zoom in
      map.on("click", "speedtests-clusters", async (e) => {
        const features = map.queryRenderedFeatures(e.point, { layers: ["speedtests-clusters"] });
        if (!features.length || !features[0]) return;

        const clusterId = features[0].properties?.cluster_id;
        if (clusterId == null) return;

        const source = map.getSource("speedtests") as maptilersdk.GeoJSONSource;
        try {
          const zoom = await source.getClusterExpansionZoom(clusterId);
          const geometry = features[0].geometry;
          if (geometry && geometry.type === "Point") {
            map.easeTo({ center: geometry.coordinates as [number, number], zoom: zoom || 12 });
          }
        } catch {
          // Ignore cluster expansion errors
        }
      });

      // Click on individual point
      map.on("click", "speedtests-points", (e: any) => {
        const features = map.queryRenderedFeatures(e.point, { layers: ["speedtests-points"] });
        if (!features?.length || !features[0]) return;
        const properties = features[0].properties;
        const speedtest = speedtests.find((st) => st.id === properties?.id);
        if (speedtest) {
          onSelectSpeedtest?.(speedtest);
        }
      });

      // Hover effects
      map.on("mouseenter", "speedtests-points", () => {
        map.getCanvas().style.cursor = "pointer";
      });

      map.on("mouseleave", "speedtests-points", () => {
        map.getCanvas().style.cursor = "";
      });

      map.on("mouseenter", "speedtests-clusters", () => {
        map.getCanvas().style.cursor = "pointer";
      });

      map.on("mouseleave", "speedtests-clusters", () => {
        map.getCanvas().style.cursor = "";
      });

      // Double-click to add new speedtest
      map.on("dblclick", (e) => {
        e.preventDefault();
        onAddSpeedtest?.(e.lngLat.lat, e.lngLat.lng);
      });
    });

    map.on("zoom", () => {
      setZoomLevel(map.getZoom());
    });

    return () => {
      map.remove();
      mapRef.current = null;
    };
  }, []); // eslint-disable-line react-hooks/exhaustive-deps

  // Update source data when speedtests change
  useEffect(() => {
    if (!mapRef.current || !mapLoaded) return;
    const source = mapRef.current.getSource("speedtests") as maptilersdk.GeoJSONSource;
    if (source) {
      source.setData(speedtestsGeoJSON);
    }
  }, [speedtestsGeoJSON, mapLoaded]);

  // Toggle heatmap visibility
  useEffect(() => {
    if (!mapRef.current || !mapLoaded) return;
    mapRef.current.setLayoutProperty("speedtests-heatmap", "visibility", showHeatmap ? "visible" : "none");
  }, [showHeatmap, mapLoaded]);

  // Toggle points visibility
  useEffect(() => {
    if (!mapRef.current || !mapLoaded) return;
    mapRef.current.setLayoutProperty("speedtests-points", "visibility", showPoints ? "visible" : "none");
    mapRef.current.setLayoutProperty("speedtests-clusters", "visibility", showPoints ? "visible" : "none");
    mapRef.current.setLayoutProperty("speedtests-cluster-count", "visibility", showPoints ? "visible" : "none");
  }, [showPoints, mapLoaded]);

  // Map controls
  const handleZoomIn = useCallback(() => {
    mapRef.current?.zoomIn();
  }, []);

  const handleZoomOut = useCallback(() => {
    mapRef.current?.zoomOut();
  }, []);

  const handleResetView = useCallback(() => {
    mapRef.current?.flyTo({ center: PH_CENTER, zoom: PH_ZOOM });
  }, []);

  // Find selected speedtest
  const selectedSpeedtest = useMemo(() => {
    return speedtests.find((st) => st.id === selectedSpeedtestId);
  }, [speedtests, selectedSpeedtestId]);

  return (
    <div className={cn("relative h-full w-full", className)}>
      {/* Map Container */}
      <div ref={mapContainerRef} className="absolute inset-0" />

      {/* Map Controls */}
      <div className="absolute top-4 left-4 flex flex-col gap-2 z-10">
        {/* Layer toggles */}
        <Card className="shadow-lg">
          <CardContent className="p-2 flex flex-col gap-1">
            <Button
              variant={showHeatmap ? "default" : "ghost"}
              size="sm"
              className={cn("h-8 gap-2 justify-start", showHeatmap && "bg-gradient-to-r from-cyan-500 to-blue-600")}
              onClick={() => setShowHeatmap(!showHeatmap)}>
              <Thermometer className="h-4 w-4" />
              <span className="text-xs">Heatmap</span>
            </Button>
            <Button
              variant={showPoints ? "default" : "ghost"}
              size="sm"
              className={cn("h-8 gap-2 justify-start", showPoints && "bg-gradient-to-r from-cyan-500 to-blue-600")}
              onClick={() => setShowPoints(!showPoints)}>
              <MapPin className="h-4 w-4" />
              <span className="text-xs">Points</span>
            </Button>
          </CardContent>
        </Card>

        {/* Zoom controls */}
        <Card className="shadow-lg">
          <CardContent className="p-1 flex flex-col">
            <Button variant="ghost" size="sm" className="h-8 w-8 p-0" onClick={handleZoomIn}>
              <ZoomIn className="h-4 w-4" />
            </Button>
            <Button variant="ghost" size="sm" className="h-8 w-8 p-0" onClick={handleZoomOut}>
              <ZoomOut className="h-4 w-4" />
            </Button>
            <div className="h-px bg-border my-1" />
            <Button variant="ghost" size="sm" className="h-8 w-8 p-0" onClick={handleResetView}>
              <LocateFixed className="h-4 w-4" />
            </Button>
          </CardContent>
        </Card>
      </div>

      {/* Zoom level indicator */}
      <div className="absolute bottom-4 left-4 z-10">
        <Badge variant="secondary" className="shadow-lg">
          Zoom: {zoomLevel.toFixed(1)}
        </Badge>
      </div>

      {/* Map type control */}
      <div className="absolute bottom-4 right-4 z-10">
        <MapType />
      </div>

      {/* Selected Speedtest Panel */}
      {selectedSpeedtest && (
        <Card className="absolute top-4 right-4 w-72 shadow-2xl z-10 bg-white/95 dark:bg-gray-900/95 backdrop-blur-sm">
          <CardContent className="p-4">
            <div className="flex items-start justify-between mb-3">
              <div className="flex items-center gap-2">
                <div
                  className="h-8 w-8 rounded-lg flex items-center justify-center text-white text-xs font-bold"
                  style={{ backgroundColor: selectedSpeedtest.serviceProvider?.provider?.color || "#6b7280" }}>
                  {(selectedSpeedtest.serviceProvider?.provider?.name || "?").slice(0, 2).toUpperCase()}
                </div>
                <div>
                  <p className="text-sm font-semibold">
                    {selectedSpeedtest.serviceProvider?.provider?.name || "Unknown"}
                  </p>
                  <p className="text-[10px] text-muted-foreground">
                    {new Date(selectedSpeedtest.dateTime).toLocaleString()}
                  </p>
                </div>
              </div>
              <Button variant="ghost" size="sm" className="h-6 w-6 p-0" onClick={() => onSelectSpeedtest?.(null)}>
                <X className="h-4 w-4" />
              </Button>
            </div>

            {/* Speed metrics */}
            <div className="grid grid-cols-2 gap-2 mb-3">
              <div className="flex items-center gap-2 p-2 rounded-lg bg-green-50 dark:bg-green-950/30">
                <Download className="h-4 w-4 text-green-600" />
                <div>
                  <p className="text-lg font-bold text-green-600">{selectedSpeedtest.downloadSpeed?.toFixed(1) || 0}</p>
                  <p className="text-[10px] text-muted-foreground">Mbps ↓</p>
                </div>
              </div>
              <div className="flex items-center gap-2 p-2 rounded-lg bg-blue-50 dark:bg-blue-950/30">
                <Upload className="h-4 w-4 text-blue-600" />
                <div>
                  <p className="text-lg font-bold text-blue-600">{selectedSpeedtest.uploadSpeed?.toFixed(1) || 0}</p>
                  <p className="text-[10px] text-muted-foreground">Mbps ↑</p>
                </div>
              </div>
            </div>

            <div className="flex items-center gap-2 mb-3">
              <Badge
                variant="outline"
                className="gap-1"
                style={{ borderColor: SIGNAL_COLORS[selectedSpeedtest.signalQuality as keyof typeof SIGNAL_COLORS] }}>
                <Signal
                  className="h-3 w-3"
                  style={{ color: SIGNAL_COLORS[selectedSpeedtest.signalQuality as keyof typeof SIGNAL_COLORS] }}
                />
                {selectedSpeedtest.signalQuality || "Unknown"}
              </Badge>
              <Badge variant="outline" className="gap-1">
                <Gauge className="h-3 w-3" />
                {selectedSpeedtest.ping?.toFixed(0) || 0}ms
              </Badge>
            </div>

            {/* Actions */}
            <div className="flex gap-2">
              <Button
                variant="outline"
                size="sm"
                className="flex-1 gap-1"
                onClick={() => onEditSpeedtest?.(selectedSpeedtest)}>
                <Edit2 className="h-3.5 w-3.5" />
                Edit
              </Button>
              <Button
                variant="destructive"
                size="sm"
                className="flex-1 gap-1"
                onClick={() => onDeleteSpeedtest?.(selectedSpeedtest)}>
                <Trash2 className="h-3.5 w-3.5" />
                Delete
              </Button>
            </div>
          </CardContent>
        </Card>
      )}

      {/* Loading overlay */}
      {isLoading && (
        <div className="absolute inset-0 bg-white/50 dark:bg-gray-900/50 flex items-center justify-center z-20">
          <div className="text-center">
            <div className="animate-spin rounded-full h-10 w-10 border-b-2 border-cyan-600 mx-auto mb-3" />
            <p className="text-sm text-muted-foreground">Loading speedtest data...</p>
          </div>
        </div>
      )}

      {/* Help text */}
      <div className="absolute bottom-4 left-1/2 -translate-x-1/2 z-10">
        <Badge variant="secondary" className="shadow-lg text-xs">
          Double-click to add a new speedtest
        </Badge>
      </div>
    </div>
  );
}
