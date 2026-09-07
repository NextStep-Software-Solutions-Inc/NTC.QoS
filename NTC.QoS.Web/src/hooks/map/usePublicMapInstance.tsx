"use client";

import { createContext, useCallback, useContext, useEffect, useRef, useState, type ReactNode } from "react";
import { useSearchParams } from "next/navigation";
import * as maptilersdk from "@maptiler/sdk";
import { Popup } from "@maptiler/sdk";

import "@maptiler/sdk/dist/maptiler-sdk.css";

// Types for layer management
export interface LayerConfig {
  id: string;
  type: "fill" | "line" | "circle" | "symbol" | "heatmap";
  source: string;
  sourceLayer?: string;
  paint?: Record<string, unknown>;
  layout?: Record<string, unknown>;
  filter?: unknown[];
  minzoom?: number;
  maxzoom?: number;
  beforeId?: string;
}

export interface SourceConfig {
  id: string;
  type: "geojson" | "vector";
  data?: GeoJSON.FeatureCollection | GeoJSON.Feature | string;
  url?: string;
  cluster?: boolean;
  clusterRadius?: number;
  clusterMaxZoom?: number;
  clusterProperties?: Record<string, unknown>;
  generateId?: boolean;
}

// Custom layer registry to track layers that need to be re-added on style change
interface LayerRegistry {
  sources: Map<string, SourceConfig>;
  layers: Map<string, LayerConfig>;
  order: string[]; // Layer order for proper rendering
}

interface PublicMapContextProps {
  map: maptilersdk.Map | null;
  isMapReady: boolean;
  setMapContainer: (node: HTMLDivElement | null) => void;

  // Style management
  setMapStyle: (style: maptilersdk.ReferenceMapStyle | maptilersdk.MapStyleVariant) => void;
  currentStyle: string;

  // Layer management
  addSource: (config: SourceConfig) => void;
  updateSource: (id: string, data: GeoJSON.FeatureCollection | GeoJSON.Feature) => void;
  removeSource: (id: string) => void;
  addLayer: (config: LayerConfig) => void;
  removeLayer: (id: string) => void;
  setLayerVisibility: (layerId: string, visible: boolean) => void;

  // Popup management
  showPopup: (lngLat: [number, number], content: HTMLElement, options?: PopupOptions) => void;
  hidePopup: () => void;

  // Map controls
  flyTo: (center: [number, number], zoom?: number) => void;
  zoomIn: () => void;
  zoomOut: () => void;
  resetBearing: () => void;
  getBearing: () => number;
  getZoom: () => number;
  getCenter: () => { lng: number; lat: number };
}

interface PopupOptions {
  offset?: number;
  closeButton?: boolean;
  closeOnClick?: boolean;
  maxWidth?: string;
  className?: string;
}

interface PublicMapProviderProps {
  apiKey: string;
  children: ReactNode;
}

const PublicMapContext = createContext<PublicMapContextProps | undefined>(undefined);

// Default map center (Philippines)
const DEFAULT_CENTER: [number, number] = [122.405211, 12.796927];
const DEFAULT_ZOOM = 5.38;

export function PublicMapProvider({ children, apiKey }: PublicMapProviderProps) {
  const [map, setMap] = useState<maptilersdk.Map | null>(null);
  const [isMapReady, setIsMapReady] = useState(false);
  const [currentStyle, setCurrentStyle] = useState("topo");

  const mapContainerRef = useRef<HTMLDivElement | null>(null);
  const [containerReady, setContainerReady] = useState(false);
  const layerRegistryRef = useRef<LayerRegistry>({ sources: new Map(), layers: new Map(), order: [] });
  const popupRef = useRef<Popup | null>(null);
  const isRestoringLayersRef = useRef(false);

  const searchParams = useSearchParams();

  // Initialize API key
  useEffect(() => {
    maptilersdk.config.apiKey = apiKey;
  }, [apiKey]);

  // Get initial position from URL params
  const getInitialPosition = useCallback(() => {
    const lat = searchParams.get("lat");
    const lng = searchParams.get("lng");
    const zoom = searchParams.get("zoom");

    return {
      center: lat && lng ? ([parseFloat(lng), parseFloat(lat)] as [number, number]) : DEFAULT_CENTER,
      zoom: zoom ? parseFloat(zoom) : DEFAULT_ZOOM,
    };
  }, [searchParams]);

  // Set map container ref
  const setMapContainer = useCallback((node: HTMLDivElement | null) => {
    mapContainerRef.current = node;
    setContainerReady(!!node);
  }, []);

  // Restore layers after style change
  const restoreLayers = useCallback((mapInstance: maptilersdk.Map) => {
    try {
      if (!mapInstance.isStyleLoaded()) return;
      const registry = layerRegistryRef.current;
      isRestoringLayersRef.current = true;

      // Re-add all sources
      registry.sources.forEach((config) => {
        if (!mapInstance.getSource(config.id)) {
          if (config.type === "geojson") {
            mapInstance.addSource(config.id, {
              type: "geojson",
              data: config.data as GeoJSON.FeatureCollection,
              cluster: config.cluster,
              clusterRadius: config.clusterRadius,
              clusterMaxZoom: config.clusterMaxZoom,
              clusterProperties: config.clusterProperties,
              generateId: config.generateId,
            });
          } else if (config.type === "vector" && config.url) {
            mapInstance.addSource(config.id, { type: "vector", url: config.url });
          }
        }
      });

      // Re-add all layers in order
      registry.order.forEach((layerId) => {
        const config = registry.layers.get(layerId);
        if (config && mapInstance.isStyleLoaded() && !mapInstance.getLayer(layerId)) {
          const layerSpec: maptilersdk.LayerSpecification = {
            id: config.id,
            type: config.type,
            source: config.source,
            ...(config.sourceLayer && { "source-layer": config.sourceLayer }),
            ...(config.paint && { paint: config.paint }),
            ...(config.layout && { layout: config.layout }),
            ...(config.filter && { filter: config.filter }),
            ...(config.minzoom !== undefined && { minzoom: config.minzoom }),
            ...(config.maxzoom !== undefined && { maxzoom: config.maxzoom }),
          } as maptilersdk.LayerSpecification;

          mapInstance.addLayer(layerSpec, config.beforeId);
        }
      });
    } catch {
      // Style not ready yet
    } finally {
      isRestoringLayersRef.current = false;
    }
  }, []);

  // Initialize map
  useEffect(() => {
    if (!mapContainerRef.current || map) return;

    const { center, zoom } = getInitialPosition();

    const newMap = new maptilersdk.Map({
      container: mapContainerRef.current,
      style: maptilersdk.MapStyle.TOPO,
      center,
      zoom,
      projection: "mercator",
      maxZoom: 20,
      minZoom: 5.2,
      // Disable all default controls
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
      setIsMapReady(true);
    });

    // Handle style changes - restore custom layers
    newMap.on("style.load", () => {
      if (isRestoringLayersRef.current) return;
      restoreLayers(newMap);
    });

    return () => {
      // Cleanup is intentionally minimal - map instance is managed by React lifecycle
      // The map will be cleaned up when the component unmounts
      if (popupRef.current) {
        popupRef.current.remove();
        popupRef.current = null;
      }
    };
  }, [containerReady, map, getInitialPosition, restoreLayers]);

  // Set map style with layer preservation
  const setMapStyle = useCallback(
    (style: maptilersdk.ReferenceMapStyle | maptilersdk.MapStyleVariant) => {
      if (!map) return;

      // Preserve current position
      const center = map.getCenter();
      const zoom = map.getZoom();
      const bearing = map.getBearing();
      const pitch = map.getPitch();

      // Extract style name for tracking
      let styleName = "unknown";
      if (typeof style === "string") {
        styleName = style;
      } else if (style && typeof style === "object" && "getName" in style) {
        styleName = (style as { getName: () => string }).getName();
      }

      map.setStyle(style);

      // Restore position after style loads
      map.once("style.load", () => {
        map.setCenter([center.lng, center.lat]);
        map.setZoom(zoom);
        map.setBearing(bearing);
        map.setPitch(pitch);
        setCurrentStyle(styleName);
      });
    },
    [map],
  );

  // Add source
  const addSource = useCallback(
    (config: SourceConfig) => {
      if (!map || !isMapReady) return;

      // Store in registry for style change restoration
      layerRegistryRef.current.sources.set(config.id, config);

      try {
        if (!map.isStyleLoaded()) return;

        if (!map.getSource(config.id)) {
          if (config.type === "geojson") {
            map.addSource(config.id, {
              type: "geojson",
              data: config.data as GeoJSON.FeatureCollection,
              cluster: config.cluster,
              clusterRadius: config.clusterRadius,
              clusterMaxZoom: config.clusterMaxZoom,
              clusterProperties: config.clusterProperties,
              generateId: config.generateId,
            });
          } else if (config.type === "vector" && config.url) {
            map.addSource(config.id, { type: "vector", url: config.url });
          }
        }
      } catch {
        // Style not ready yet
      }
    },
    [map, isMapReady],
  );

  // Update source data
  const updateSource = useCallback(
    (id: string, data: GeoJSON.FeatureCollection | GeoJSON.Feature) => {
      if (!map || !isMapReady) return;

      try {
        if (!map.isStyleLoaded()) return;
        const source = map.getSource(id) as maptilersdk.GeoJSONSource | undefined;
        if (source) {
          source.setData(data);
        }
      } catch {
        // Style not ready yet
      }

      // Update registry
      const config = layerRegistryRef.current.sources.get(id);
      if (config && config.type === "geojson") {
        config.data = data;
      }
    },
    [map, isMapReady],
  );

  // Remove source
  const removeSource = useCallback(
    (id: string) => {
      if (!map) return;

      try {
        if (map.isStyleLoaded() && map.getSource(id)) {
          map.removeSource(id);
        }
      } catch {
        // Style not ready yet
      }
      layerRegistryRef.current.sources.delete(id);
    },
    [map],
  );

  // Add layer
  const addLayer = useCallback(
    (config: LayerConfig) => {
      if (!map || !isMapReady || !map.isStyleLoaded()) return;

      // Store in registry
      layerRegistryRef.current.layers.set(config.id, config);
      if (!layerRegistryRef.current.order.includes(config.id)) {
        layerRegistryRef.current.order.push(config.id);
      }

      try {
        if (!map.isStyleLoaded()) return;

        if (!map.getLayer(config.id)) {
          const layerSpec: maptilersdk.LayerSpecification = {
            id: config.id,
            type: config.type,
            source: config.source,
            ...(config.sourceLayer && { "source-layer": config.sourceLayer }),
            ...(config.paint && { paint: config.paint }),
            ...(config.layout && { layout: config.layout }),
            ...(config.filter && { filter: config.filter }),
            ...(config.minzoom !== undefined && { minzoom: config.minzoom }),
            ...(config.maxzoom !== undefined && { maxzoom: config.maxzoom }),
          } as maptilersdk.LayerSpecification;

          map.addLayer(layerSpec, config.beforeId);
        }
      } catch {
        // Style not ready yet
      }
    },
    [map, isMapReady],
  );

  // Remove layer
  const removeLayer = useCallback(
    (id: string) => {
      if (!map) return;

      try {
        if (map.isStyleLoaded() && map.getLayer(id)) {
          map.removeLayer(id);
        }
      } catch {
        // Style not ready yet
      }
      layerRegistryRef.current.layers.delete(id);
      layerRegistryRef.current.order = layerRegistryRef.current.order.filter((l) => l !== id);
    },
    [map],
  );

  // Set layer visibility
  const setLayerVisibility = useCallback(
    (layerId: string, visible: boolean) => {
      if (!map) return;

      try {
        if (map.isStyleLoaded() && map.getLayer(layerId)) {
          map.setLayoutProperty(layerId, "visibility", visible ? "visible" : "none");
        }
      } catch {
        // Style not ready yet
      }

      // Update registry
      const config = layerRegistryRef.current.layers.get(layerId);
      if (config) {
        config.layout = { ...config.layout, visibility: visible ? "visible" : "none" };
      }
    },
    [map],
  );

  // Show popup
  const showPopup = useCallback(
    (lngLat: [number, number], content: HTMLElement, options?: PopupOptions) => {
      if (!map) return;

      // Remove existing popup
      if (popupRef.current) {
        popupRef.current.remove();
      }

      popupRef.current = new Popup({
        offset: options?.offset ?? 10,
        closeButton: options?.closeButton ?? false,
        closeOnClick: options?.closeOnClick ?? true,
        maxWidth: options?.maxWidth ?? "300px",
        className: options?.className ?? "public-map-popup",
      })
        .setLngLat(lngLat)
        .setDOMContent(content)
        .addTo(map);
    },
    [map],
  );

  // Hide popup
  const hidePopup = useCallback(() => {
    if (popupRef.current) {
      popupRef.current.remove();
      popupRef.current = null;
    }
  }, []);

  // Map controls
  const flyTo = useCallback(
    (center: [number, number], zoom?: number) => {
      if (!map) return;
      map.flyTo({ center, zoom: zoom ?? map.getZoom(), duration: 1500 });
    },
    [map],
  );

  const zoomIn = useCallback(() => {
    map?.zoomIn({ duration: 300 });
  }, [map]);

  const zoomOut = useCallback(() => {
    map?.zoomOut({ duration: 300 });
  }, [map]);

  const resetBearing = useCallback(() => {
    map?.easeTo({ bearing: 0, pitch: 0, duration: 500 });
  }, [map]);

  const getBearing = useCallback(() => map?.getBearing() ?? 0, [map]);
  const getZoom = useCallback(() => map?.getZoom() ?? DEFAULT_ZOOM, [map]);
  const getCenter = useCallback(() => map?.getCenter() ?? { lng: DEFAULT_CENTER[0], lat: DEFAULT_CENTER[1] }, [map]);

  return (
    <PublicMapContext.Provider
      value={{
        map,
        isMapReady,
        setMapContainer,
        setMapStyle,
        currentStyle,
        addSource,
        updateSource,
        removeSource,
        addLayer,
        removeLayer,
        setLayerVisibility,
        showPopup,
        hidePopup,
        flyTo,
        zoomIn,
        zoomOut,
        resetBearing,
        getBearing,
        getZoom,
        getCenter,
      }}>
      {children}
    </PublicMapContext.Provider>
  );
}

export function usePublicMap() {
  const context = useContext(PublicMapContext);
  if (!context) {
    throw new Error("usePublicMap must be used within a PublicMapProvider");
  }
  return context;
}
