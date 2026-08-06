"use client";

import { useCallback, useEffect, useRef } from "react";

import type { NAPViewport } from "@/components/features/NAP/types/NAP";

export type GeoJSONFeature = {
  type: "Feature";
  geometry: { type: "Point"; coordinates: [number, number] };
  properties: { id: string; isp: string; ispColor: string };
};

export type GeoJSONFeatureCollection = { type: "FeatureCollection"; features: GeoJSONFeature[] };

type WorkerMessage = { type: "PROCESS_NAPS"; naps: NAPViewport[]; existingFeatures?: GeoJSONFeature[] };

type WorkerResponse = {
  type: "NAPS_PROCESSED";
  geojson: GeoJSONFeatureCollection;
  count: number;
  newCount: number;
  duplicateCount: number;
  duration: number;
};

/**
 * Hook to manage GeoJSON processing Web Worker
 * Offloads NAP to GeoJSON transformation to a background thread
 */
export const useNAPGeoJSONWorker = (onProcessed: (geojson: GeoJSONFeatureCollection, count: number) => void) => {
  const workerRef = useRef<Worker | null>(null);
  const callbackRef = useRef(onProcessed);

  // Keep callback ref up to date
  useEffect(() => {
    callbackRef.current = onProcessed;
  }, [onProcessed]);

  // Initialize worker
  useEffect(() => {
    // Create worker instance
    workerRef.current = new Worker(new URL("../workers/nap-processor.worker.ts", import.meta.url), { type: "module" });

    // Dispatch init event for debug panel
    if (typeof window !== "undefined") {
      window.dispatchEvent(new CustomEvent("worker-stats", { detail: { type: "WORKER_INIT", workerType: "nap" } }));
    }

    // Handle messages from worker
    workerRef.current.onmessage = (event: MessageEvent<WorkerResponse>) => {
      const { type, geojson, count, newCount, duplicateCount } = event.data;

      if (type === "NAPS_PROCESSED") {
        // Get timing if available
        const duration = event.data.duration ?? 0;

        // Dispatch complete event for debug panel
        if (typeof window !== "undefined") {
          window.dispatchEvent(
            new CustomEvent("worker-stats", {
              detail: { type: "WORKER_COMPLETE", count, duration, workerType: "nap", newCount, duplicateCount },
            }),
          );
        }

        callbackRef.current(geojson, count);
      }
    };

    // Handle worker errors
    workerRef.current.onerror = (error) => {
      console.error("❌ [Main Thread] GeoJSON Worker error:", error);
    };

    // Cleanup on unmount
    return () => {
      if (workerRef.current) {
        workerRef.current.terminate();
        workerRef.current = null;
      }
    };
  }, []);

  // Function to send NAPs to worker for processing with existing features for deduplication
  const processNAPs = useCallback((naps: NAPViewport[], existingFeatures: GeoJSONFeature[] = []) => {
    if (!workerRef.current) {
      if (process.env.NODE_ENV === "development") {
        console.warn("⚠️ [Main Thread] Worker not initialized");
      }
      return;
    }

    // Dispatch process event for debug panel
    if (typeof window !== "undefined") {
      window.dispatchEvent(
        new CustomEvent("worker-stats", { detail: { type: "WORKER_PROCESS", count: naps.length, workerType: "nap" } }),
      );
    }

    const message: WorkerMessage = { type: "PROCESS_NAPS", naps, existingFeatures };

    workerRef.current.postMessage(message);
  }, []);

  return { processNAPs };
};
