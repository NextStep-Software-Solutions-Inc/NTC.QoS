"use client";

import { useCallback, useEffect, useRef } from "react";

import type { CellViewport } from "@/components/features/cell/types/Cell";

export type GeoJSONFeature = {
  id: string;
  type: "Feature";
  geometry: { type: "Point"; coordinates: [number, number] };
  properties: {
    id: string;
    name: string;
    description: string;
    isp: string;
    ispColor: string;
    providerIcon: string; // Icon ID for MapTiler symbol layer
    providerColor: string; // Alias for ispColor
    image: string;
    lat: number;
    lng: number;
    isVerified: boolean;
    status: string;
    type: "point";
  };
};

export type GeoJSONPolygonFeature = {
  id: string;
  type: "Feature";
  geometry: { type: "Polygon"; coordinates: number[][][] };
  properties: { id: string; isp: string; ispColor: string; isVerified: boolean; status: string; type: "polygon" };
};

export type CellGeoJSONFeatureCollection = {
  type: "FeatureCollection";
  features: (GeoJSONFeature | GeoJSONPolygonFeature)[];
};

type WorkerMessage = {
  type: "PROCESS_CELLS";
  cells: CellViewport[];
  radiusInKilometer: number;
  segments?: number;
  existingFeatures?: (GeoJSONFeature | GeoJSONPolygonFeature)[];
};

type WorkerResponse = {
  type: "CELLS_PROCESSED";
  geojson: CellGeoJSONFeatureCollection;
  count: number;
  newCount: number;
  duplicateCount: number;
  duration: number;
};

/**
 * Hook to manage Cell GeoJSON processing Web Worker
 * Offloads Cell to GeoJSON transformation to a background thread
 */
export const useCellGeoJSONWorker = (onProcessed: (geojson: CellGeoJSONFeatureCollection, count: number) => void) => {
  const workerRef = useRef<Worker | null>(null);
  const callbackRef = useRef(onProcessed);

  // Keep callback ref up to date
  useEffect(() => {
    callbackRef.current = onProcessed;
  }, [onProcessed]);

  // Initialize worker
  useEffect(() => {
    // Create worker instance
    workerRef.current = new Worker(new URL("../workers/cell-processor.worker.ts", import.meta.url), { type: "module" });

    // Dispatch init event for debug panel
    if (typeof window !== "undefined") {
      window.dispatchEvent(new CustomEvent("worker-stats", { detail: { type: "WORKER_INIT", workerType: "cell" } }));
    }

    // Handle messages from worker
    workerRef.current.onmessage = (event: MessageEvent<WorkerResponse>) => {
      const { type, geojson, count, newCount, duplicateCount } = event.data;

      if (type === "CELLS_PROCESSED") {
        // Get timing if available
        const duration = event.data.duration ?? 0;

        // Dispatch complete event for debug panel
        if (typeof window !== "undefined") {
          window.dispatchEvent(
            new CustomEvent("worker-stats", {
              detail: { type: "WORKER_COMPLETE", count, duration, workerType: "cell", newCount, duplicateCount },
            }),
          );
        }

        callbackRef.current(geojson, count);
      }
    };

    // Handle worker errors
    workerRef.current.onerror = (error) => {
      console.error("❌ [Main Thread] Cell Worker error:", error);
    };

    // Cleanup on unmount
    return () => {
      if (workerRef.current) {
        workerRef.current.terminate();
        workerRef.current = null;
      }
    };
  }, []);

  // Function to send CellViewport data to worker for processing with existing features for deduplication
  const processCells = useCallback(
    (
      cells: CellViewport[],
      radiusInKilometer: number,
      segments = 6,
      existingFeatures: (GeoJSONFeature | GeoJSONPolygonFeature)[] = [],
    ) => {
      if (!workerRef.current) {
        if (process.env.NODE_ENV === "development") {
          console.warn("⚠️ [Main Thread] Cell Worker not initialized");
        }
        return;
      }

      // Dispatch process event for debug panel
      if (typeof window !== "undefined") {
        window.dispatchEvent(
          new CustomEvent("worker-stats", {
            detail: { type: "WORKER_PROCESS", count: cells.length, workerType: "cell" },
          }),
        );
      }

      const message: WorkerMessage = { type: "PROCESS_CELLS", cells, radiusInKilometer, segments, existingFeatures };

      workerRef.current.postMessage(message);
    },
    [],
  );

  return { processCells };
};
