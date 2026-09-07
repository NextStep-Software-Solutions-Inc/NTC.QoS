"use client";

import { useCallback, useEffect, useMemo } from "react";
import {
  Popup,
  type GeoJSONSource,
  type MapGeoJSONFeature,
  type MapLibreEvent,
  type MapMouseEvent,
  type MapSourceDataType,
  type SourceSpecification,
} from "@maptiler/sdk";
import { createRoot } from "react-dom/client";

import { FiberInformation } from "@/components/admin-map/components/FiberInformation";
import { useMap } from "@/hooks/useMap";

type LayerId = "fiber" | "fiber-line" | "highlighted-fiber";
const MAPTILER_FIBER_SOURCE_LAYER_ID = process.env.NEXT_PUBLIC_MAPTILER_FIBER_SOURCE_LAYER_ID ?? "merged";

/**
 * Extracts a short keyword from a full provider name for fuzzy vector-tile matching.
 * e.g. "PLDT COMMUNICATIONS INC." → "PLDT"
 *      "GLOBE COMMUNICATIONS INC." → "GLOBE"
 *      "CONVERGE ICT SOLUTIONS INC." → "CONVERGE"
 */
function toFilterKeyword(name: string): string {
  const STRIP =
    /\b(INC\.?|CORP\.?|CORPORATION|COMMUNICATIONS?|TELECOM(MUNICATIONS?)?|NETWORKS?|DATA|SOLUTIONS?|SERVICES?|CO\.?|LTD\.?|LLC|COMPANY|ENTERPRISES?|TECHNOLOGY|ICT)\b\.?/gi;
  const stripped = name.replace(STRIP, "").replace(/\s+/g, " ").trim();
  return (stripped.split(" ")[0] ?? name).toUpperCase();
}

export const useMapFibers = (visibleLayers: Record<LayerId, boolean>, isAdmin = false, providerNames?: string[]) => {
  const { map, isMapLoaded } = useMap();

  // Helper to set visibility for a layer
  const setLayerVisible = useCallback(
    (layer: LayerId, visible: boolean) => {
      try {
        if (map && map.isStyleLoaded() && map.getLayer(layer)) {
          map.setLayoutProperty(layer, "visibility", visible ? "visible" : "none");
        }
      } catch {
        // Style not ready yet
      }
    },
    [map],
  );

  const popup = useMemo(
    () => new Popup({ className: "hover:cursor-pointer", closeOnClick: false, maxWidth: "none", closeButton: false }),
    [],
  );

  useEffect(() => {
    if (!map || !isMapLoaded) return;

    /**
     * Finds the insertion layer for fiber so it stays behind operational point layers
     * (cell sites and NAPs), while still remaining below top map labels.
     */
    const findFiberBeforeLayer = (): string | undefined => {
      try {
        if (!map || !map.isStyleLoaded()) return undefined;
        const priorityOperationalLayers = [
          "signal-coverage-circles",
          "cells-heatmap",
          "cells-point-view",
          "cells-clusters",
          "cells-cluster-count",
          "cells-circles",
          "cells-symbols",
          "NAPs-cluster",
          "NAPs-cluster-count",
          "NAPs-unclustered",
          "speedtests-cellular",
          "speedtests-circles",
          "speedtests-heatmap",
        ] as const;

        for (const layerId of priorityOperationalLayers) {
          if (map.getLayer(layerId)) {
            return layerId;
          }
        }

        const layers = map.getStyle()?.layers;
        if (!layers) return undefined;

        // Prefer place / settlement / city / town labels
        for (const layer of layers) {
          if (layer.type === "symbol") {
            const id = layer.id.toLowerCase();
            if (id.includes("place") || id.includes("settlement") || id.includes("city") || id.includes("town")) {
              return layer.id;
            }
          }
        }

        // Fallback: any symbol layer whose id contains "label"
        for (const layer of layers) {
          if (layer.type === "symbol" && layer.id.toLowerCase().includes("label")) {
            return layer.id;
          }
        }

        return undefined; // Render on top if no label layer is found
      } catch {
        return undefined;
      }
    };

    const ensureFiberLayers = () => {
      if (!map || !map.isStyleLoaded() || !map.getSource("map-tiles")) return;

      try {
        // Determine the insertion point so fiber is behind point assets (NAP/cellsites)
        // while still respecting style label stacking when those layers are absent.
        const fiberBeforeLayer = findFiberBeforeLayer();

        if (!map.getLayer("fiber")) {
          if (process.env.NODE_ENV === "development") {
            console.info("Adding fiber layer");
          }
          map.addLayer(
            {
              id: "fiber",
              type: "fill",
              source: "map-tiles",
              "source-layer": MAPTILER_FIBER_SOURCE_LAYER_ID,
              paint: {
                "fill-color": ["case", ["boolean", ["feature-state", "hover"], false], "#f00", "#f0a"],
                "fill-opacity": ["case", ["boolean", ["feature-state", "hover"], false], 0.5, 0.3],
              },
              layout: { visibility: visibleLayers.fiber ? "visible" : "none" },
            },
            fiberBeforeLayer,
          );
        } else if (fiberBeforeLayer && fiberBeforeLayer !== "fiber") {
          try {
            map.moveLayer("fiber", fiberBeforeLayer);
            if (map.getLayer("fiber-line")) {
              map.moveLayer("fiber-line", "fiber");
            }
          } catch {}
        }

        if (!map.getLayer("fiber-line")) {
          if (process.env.NODE_ENV === "development") {
            console.info("Adding fiber-line layer");
          }
          const beforeFiber = map.getLayer("fiber") ? "fiber" : fiberBeforeLayer;
          map.addLayer(
            {
              id: "fiber-line",
              type: "line",
              source: "map-tiles",
              "source-layer": MAPTILER_FIBER_SOURCE_LAYER_ID,
              paint: { "line-color": "#FF00FF", "line-opacity": 0.5, "line-width": 1.5, "line-dasharray": [2, 2] },
              layout: { visibility: visibleLayers["fiber-line"] ? "visible" : "none" },
            },
            // Render fiber-line directly below the fiber fill to preserve visual stacking
            beforeFiber,
          );
        }

        if (!map.getLayer("highlighted-fiber")) {
          if (process.env.NODE_ENV === "development") {
            console.info("Adding highlighted-fiber layer");
          }
          map.addLayer({
            id: "highlighted-fiber",
            type: "line",
            source: "highlighted-fiber",
            paint: { "line-color": "#00f", "line-width": 2, "line-dasharray": [1, 1] },
            layout: { visibility: visibleLayers["highlighted-fiber"] ? "visible" : "none" },
          });
        }
      } catch {
        // Style not ready yet
      }
    };

    const addFiberSource = () => {
      if (!map || !map.isStyleLoaded()) return;
      if (!map.getSource("map-tiles")) {
        if (process.env.NODE_ENV === "development") {
          console.info("Adding map-tiles source");
        }
        map.addSource("map-tiles", { type: "vector", url: process.env.NEXT_PUBLIC_MAPTILER_URL });
      }
      if (!map.getSource("highlighted-fiber")) {
        map.addSource("highlighted-fiber", {
          type: "geojson",
          data: { type: "Feature", properties: {}, geometry: { type: "Polygon", coordinates: [[]] } },
        });
      }
      ensureFiberLayers();
    };

    const addSourceLayers = (
      e: MapLibreEvent<unknown> & {
        dataType: "source";
        isSourceLoaded: boolean;
        source: SourceSpecification;
        sourceId: string;
        sourceDataType: MapSourceDataType;
        sourceDataChanged?: boolean;
        tile: unknown;
      } & {},
    ) => {
      if (e.sourceId !== "map-tiles" || !map.isStyleLoaded()) return;
      ensureFiberLayers();
    };

    // Ensure source and layers are initialized on mount
    addFiberSource();

    const handleFiberClick = (e: MapMouseEvent & { features?: MapGeoJSONFeature[] }) => {
      map.getCanvas().style.cursor = "pointer";
      if (e.features && e.features.length > 0) {
        const feature = e.features[0];
        const properties = feature?.properties ?? {};
        const coordinates = e.lngLat; // Get the clicked coordinates

        const div = document.createElement("div");
        createRoot(div).render(
          <FiberInformation properties={properties as Record<string, unknown>} popup={popup} isAdmin={isAdmin} />,
        );
        popup.setLngLat([coordinates.lng, coordinates.lat]).setDOMContent(div).addTo(map);
      }
    };

    const handleFiberMouseMove = (e: MapMouseEvent & { features?: MapGeoJSONFeature[] }) => {
      map.getCanvas().style.cursor = "pointer";
      if (e.features && e.features.length > 0) {
        const feature = e.features[0] as unknown as {
          id: string | number;
          properties: { Address: string; Provider: string };
        };

        map.removeFeatureState({ source: "map-tiles", sourceLayer: MAPTILER_FIBER_SOURCE_LAYER_ID });
        map.setFeatureState({ id: feature.id, source: "map-tiles", sourceLayer: MAPTILER_FIBER_SOURCE_LAYER_ID }, { hover: true });
      }
    };

    const handleFiberMouseLeave = () => {
      map.getCanvas().style.cursor = "";
      map.removeFeatureState({ source: "map-tiles", sourceLayer: MAPTILER_FIBER_SOURCE_LAYER_ID });
    };

    const highlighFiber = (e: MapMouseEvent & { features?: MapGeoJSONFeature[] } & object) => {
      const source = map.getSource<GeoJSONSource>("highlighted-fiber");
      if (e.features && e.features.length > 0) {
        const feature = e.features[0]!;
        source?.setData(feature);

        const coordinates = e.lngLat;
        // ✅ Update URL with lat/lng
        const searchParams = new URLSearchParams(window.location.search);

        searchParams.set("lat", coordinates.lat.toFixed(6)); // Optional: round to 6 decimals
        searchParams.set("lng", coordinates.lng.toFixed(6));
        searchParams.set("featureId", `${feature.id}`);

        window.history.replaceState({}, "", `?${searchParams.toString()}`);
      }
    };

    map.on("styledata", addFiberSource);
    map.on("sourcedata", addSourceLayers);
    map.on("click", "fiber", handleFiberClick);
    map.on("click", "fiber", highlighFiber);
    map.on("mousemove", "fiber", handleFiberMouseMove);
    map.on("mouseleave", "fiber", handleFiberMouseLeave);

    return () => {
      map.off("styledata", addFiberSource);
      map.off("sourcedata", addSourceLayers);
      map.off("click", "fiber", handleFiberClick);
      map.off("click", "fiber", highlighFiber);
      map.off("mousemove", "fiber", handleFiberMouseMove);
      map.off("mouseleave", "fiber", handleFiberMouseLeave);
    };
  }, [map, popup, visibleLayers, isMapLoaded, isAdmin]);

  useEffect(() => {
    Object.entries(visibleLayers).forEach(([layer, visible]) => {
      setLayerVisible(layer as LayerId, visible);
    });
  }, [map, setLayerVisible, visibleLayers]);

  // Apply provider filter to the fiber vector tile layers
  useEffect(() => {
    if (!map || !isMapLoaded || !map.isStyleLoaded()) return;

    const filterExpr =
      providerNames && providerNames.length > 0
        ? ([
            "any",
            ...providerNames.map((name) => [
              "in",
              toFilterKeyword(name),
              ["upcase", ["to-string", ["get", "Providers"]]],
            ]),
          ] as Parameters<typeof map.setFilter>[1])
        : null;

    if (map.getLayer("fiber")) map.setFilter("fiber", filterExpr);
    if (map.getLayer("fiber-line")) map.setFilter("fiber-line", filterExpr);
  }, [map, isMapLoaded, providerNames]);

  return null;
};
