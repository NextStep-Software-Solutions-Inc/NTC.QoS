"use client";

import { useCallback, useEffect, useRef } from "react";
import type { MapGeoJSONFeature, MapMouseEvent } from "@maptiler/sdk";
import { createRoot, type Root } from "react-dom/client";

import { normalizeQoSProperties, type NormalizedQoSProperties, type QoSFeatureProperties } from "@/lib/map/qos-utils";
import { usePublicMap } from "./usePublicMapInstance";

// Re-export types for convenience
export type { QoSFeatureProperties } from "@/lib/map/qos-utils";

interface UseQoSLayerOptions {
  sourceUrl?: string;
  sourceLayerId?: string;
  visible?: boolean;
  enablePopup?: boolean;
  enableHover?: boolean;
}

export function useQoSLayer(options: UseQoSLayerOptions = {}) {
  const {
    sourceUrl = process.env.NEXT_PUBLIC_MAPTILER_URL,
    sourceLayerId = process.env.NEXT_PUBLIC_MAPTILER_FIBER_SOURCE_LAYER_ID ?? "merged",
    visible = true,
    enablePopup = true,
    enableHover = true,
  } = options;

  const { map, isMapReady, addSource, addLayer, setLayerVisibility, showPopup, hidePopup } = usePublicMap();

  const hoveredFeatureIdRef = useRef<string | number | undefined>(undefined);
  const popupRootRef = useRef<Root | null>(null);
  const popupContainerRef = useRef<HTMLDivElement | null>(null);

  // Layer IDs
  const SOURCE_ID = "qos-source";
  const FILL_LAYER_ID = "qos-fill";
  const LINE_LAYER_ID = "qos-line";
  const HIGHLIGHT_LAYER_ID = "qos-highlight";

  // Initialize source and layers
  useEffect(() => {
    if (!isMapReady || !map || !sourceUrl) return;

    // Add vector source
    addSource({ id: SOURCE_ID, type: "vector", url: sourceUrl });

    // Add fill layer for polygons
    addLayer({
      id: FILL_LAYER_ID,
      type: "fill",
      source: SOURCE_ID,
      sourceLayer: sourceLayerId,
      paint: {
        "fill-color": [
          "case",
          ["boolean", ["feature-state", "hover"], false],
          "rgba(59, 130, 246, 0.6)", // Blue highlight on hover
          "rgba(139, 92, 246, 0.3)", // Purple default
        ],
        "fill-opacity": ["case", ["boolean", ["feature-state", "hover"], false], 0.6, 0.3],
      },
      layout: { visibility: visible ? "visible" : "none" },
    });

    // Add line layer for polygon borders
    addLayer({
      id: LINE_LAYER_ID,
      type: "line",
      source: SOURCE_ID,
      sourceLayer: sourceLayerId,
      paint: {
        "line-color": [
          "case",
          ["boolean", ["feature-state", "hover"], false],
          "#3b82f6", // Blue on hover
          "#8b5cf6", // Purple default
        ],
        "line-width": ["case", ["boolean", ["feature-state", "hover"], false], 2, 1],
        "line-opacity": 0.8,
      },
      layout: { visibility: visible ? "visible" : "none" },
    });
  }, [isMapReady, map, sourceUrl, sourceLayerId, visible, addSource, addLayer]);

  // Toggle visibility
  useEffect(() => {
    setLayerVisibility(FILL_LAYER_ID, visible);
    setLayerVisibility(LINE_LAYER_ID, visible);
  }, [visible, setLayerVisibility]);

  // Create popup content element
  const createPopupContent = useCallback((props: NormalizedQoSProperties) => {
    // Create container if not exists
    if (!popupContainerRef.current) {
      popupContainerRef.current = document.createElement("div");
    }

    // Cleanup previous root asynchronously to avoid race conditions
    if (popupRootRef.current) {
      queueMicrotask(() => {
        if (popupRootRef.current) {
          popupRootRef.current.unmount();
        }
      });
    }

    // Create new root and render
    popupRootRef.current = createRoot(popupContainerRef.current);
    popupRootRef.current.render(<QoSPopupContent {...props} />);

    return popupContainerRef.current;
  }, []);

  // Handle mouse events
  useEffect(() => {
    if (!map || !isMapReady) return;

    const handleMouseMove = (e: MapMouseEvent & { features?: MapGeoJSONFeature[] }) => {
      if (!enableHover) return;

      const features = map.queryRenderedFeatures(e.point, { layers: [FILL_LAYER_ID] });

      if (features.length === 0) {
        // Reset hover state for the specific feature
        if (hoveredFeatureIdRef.current !== undefined) {
          map.setFeatureState(
            { source: SOURCE_ID, sourceLayer: sourceLayerId, id: hoveredFeatureIdRef.current },
            { hover: false },
          );
          hoveredFeatureIdRef.current = undefined;
        }
        map.getCanvas().style.cursor = "";
        return;
      }

      map.getCanvas().style.cursor = "pointer";

      const feature = features[0]!;
      const featureId = feature.id;

      if (featureId !== hoveredFeatureIdRef.current) {
        // Clear previous hover
        if (hoveredFeatureIdRef.current !== undefined) {
          map.setFeatureState(
            { source: SOURCE_ID, sourceLayer: sourceLayerId, id: hoveredFeatureIdRef.current },
            { hover: false },
          );
        }

        // Set new hover
        hoveredFeatureIdRef.current = featureId;
        map.setFeatureState({ source: SOURCE_ID, sourceLayer: sourceLayerId, id: featureId }, { hover: true });
      }
    };

    const handleMouseLeave = () => {
      if (hoveredFeatureIdRef.current !== undefined) {
        map.setFeatureState(
          { source: SOURCE_ID, sourceLayer: sourceLayerId, id: hoveredFeatureIdRef.current },
          { hover: false },
        );
        hoveredFeatureIdRef.current = undefined;
      }
      map.getCanvas().style.cursor = "";
      hidePopup();
    };

    const handleClick = (e: MapMouseEvent & { features?: MapGeoJSONFeature[] }) => {
      if (!enablePopup) return;

      const features = map.queryRenderedFeatures(e.point, { layers: [FILL_LAYER_ID] });

      if (features.length === 0) {
        hidePopup();
        return;
      }

      const feature = features[0]!;
      const props = normalizeQoSProperties(feature.properties as QoSFeatureProperties);
      const content = createPopupContent(props);

      showPopup([e.lngLat.lng, e.lngLat.lat], content, {
        maxWidth: "320px",
        closeButton: true,
        closeOnClick: false,
        className: "qos-popup",
      });

      // Update URL with feature info
      const searchParams = new URLSearchParams(window.location.search);
      searchParams.set("lat", e.lngLat.lat.toFixed(6));
      searchParams.set("lng", e.lngLat.lng.toFixed(6));
      if (feature.id) {
        searchParams.set("featureId", String(feature.id));
      }
      window.history.replaceState({}, "", `?${searchParams.toString()}`);
    };

    map.on("mousemove", FILL_LAYER_ID, handleMouseMove);
    map.on("mouseleave", FILL_LAYER_ID, handleMouseLeave);
    map.on("click", FILL_LAYER_ID, handleClick);

    return () => {
      map.off("mousemove", FILL_LAYER_ID, handleMouseMove);
      map.off("mouseleave", FILL_LAYER_ID, handleMouseLeave);
      map.off("click", FILL_LAYER_ID, handleClick);
    };
  }, [map, isMapReady, sourceLayerId, enableHover, enablePopup, createPopupContent, showPopup, hidePopup]);

  // Cleanup on unmount
  useEffect(() => {
    return () => {
      // Cleanup popup root asynchronously to avoid race conditions
      if (popupRootRef.current) {
        queueMicrotask(() => {
          if (popupRootRef.current) {
            popupRootRef.current.unmount();
          }
        });
      }
    };
  }, []);

  return { layerIds: { fill: FILL_LAYER_ID, line: LINE_LAYER_ID, highlight: HIGHLIGHT_LAYER_ID }, sourceId: SOURCE_ID };
}

// Popup content component
function QoSPopupContent({
  zipcode,
  barangay,
  municipality,
  province,
  region,
  providers,
}: {
  zipcode: string;
  barangay: string;
  municipality: string;
  province: string;
  region: string;
  providers: string[];
}) {
  return (
    <div className="bg-white rounded-xl shadow-xl p-4 min-w-[280px]">
      <div className="border-b border-gray-100 pb-3 mb-3">
        <h3 className="text-lg font-semibold text-gray-800">Coverage Information</h3>
      </div>

      <div className="space-y-2 text-sm">
        {region && (
          <div className="flex items-start gap-2">
            <span className="text-gray-500 w-24 shrink-0">Region:</span>
            <span className="text-gray-800 font-medium">{region}</span>
          </div>
        )}
        {province && (
          <div className="flex items-start gap-2">
            <span className="text-gray-500 w-24 shrink-0">Province:</span>
            <span className="text-gray-800 font-medium">{province}</span>
          </div>
        )}
        {municipality && (
          <div className="flex items-start gap-2">
            <span className="text-gray-500 w-24 shrink-0">Municipality:</span>
            <span className="text-gray-800 font-medium">{municipality}</span>
          </div>
        )}
        {barangay && (
          <div className="flex items-start gap-2">
            <span className="text-gray-500 w-24 shrink-0">Barangay:</span>
            <span className="text-gray-800 font-medium">{barangay}</span>
          </div>
        )}
        {zipcode && (
          <div className="flex items-start gap-2">
            <span className="text-gray-500 w-24 shrink-0">Zip Code:</span>
            <span className="text-gray-800 font-medium">{zipcode}</span>
          </div>
        )}
      </div>

      {providers.length > 0 && (
        <div className="mt-4 pt-3 border-t border-gray-100">
          <p className="text-xs font-semibold text-gray-500 uppercase tracking-wide mb-2">
            Internet Service Providers ({providers.length})
          </p>
          <div className="flex flex-wrap gap-1.5">
            {providers.map((provider, idx) => (
              <span
                key={idx}
                className="inline-flex items-center px-2.5 py-1 rounded-full text-xs font-medium bg-primary/10 text-primary">
                {provider}
              </span>
            ))}
          </div>
        </div>
      )}

      {providers.length === 0 && (
        <div className="mt-4 pt-3 border-t border-gray-100">
          <p className="text-sm text-gray-500 italic">No provider information available</p>
        </div>
      )}
    </div>
  );
}
