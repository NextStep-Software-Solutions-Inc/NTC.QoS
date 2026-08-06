"use client";

import { useCallback, useEffect, useRef } from "react";

import { type SpeedTestViewport } from "@/workers/speedtest-processor.worker";

export type GeoJSONFeature = {
  type: "Feature";
  geometry: { type: "Point"; coordinates: [number, number] };
  properties: {
    id: string;
    isp: string;
    ispColor: string;
    downloadSpeed: number;
    uploadSpeed: number;
    signalQuality: string;
  };
};

export type SpeedTestGeoJSONFeatureCollection = { type: "FeatureCollection"; features: GeoJSONFeature[] };

type WorkerMessage = {
  type: "PROCESS_SPEEDTESTS";
  speedtests: SpeedTestViewport[];
  existingFeatures?: GeoJSONFeature[];
};

type WorkerResponse = {
  type: "SPEEDTESTS_PROCESSED";
  geojson: SpeedTestGeoJSONFeatureCollection;
  count: number;
  newCount: number;
  duplicateCount: number;
  duration: number;
};

/**
 * Hook to manage SpeedTest GeoJSON processing Web Worker
 * Offloads SpeedTest to GeoJSON transformation to a background thread
 */
export const useSpeedTestGeoJSONWorker = (
  onProcessed: (geojson: SpeedTestGeoJSONFeatureCollection, count: number) => void,
) => {
  const workerRef = useRef<Worker | null>(null);
  const callbackRef = useRef(onProcessed);

  // Keep callback ref up to date
  useEffect(() => {
    callbackRef.current = onProcessed;
  }, [onProcessed]);

  // Initialize worker
  useEffect(() => {
    // Create worker instance
    workerRef.current = new Worker(new URL("../workers/speedtest-processor.worker.ts", import.meta.url), {
      type: "module",
    });

    // Dispatch init event for debug panel
    if (typeof window !== "undefined") {
      window.dispatchEvent(
        new CustomEvent("worker-stats", { detail: { type: "WORKER_INIT", workerType: "speedtest" } }),
      );
    }

    // Handle messages from worker
    workerRef.current.onmessage = (event: MessageEvent<WorkerResponse>) => {
      const { type, geojson, count, newCount, duplicateCount } = event.data;

      if (type === "SPEEDTESTS_PROCESSED") {
        // Get timing if available
        const duration = event.data.duration || 0;

        // Dispatch complete event for debug panel
        if (typeof window !== "undefined") {
          window.dispatchEvent(
            new CustomEvent("worker-stats", {
              detail: { type: "WORKER_COMPLETE", count, duration, workerType: "speedtest", newCount, duplicateCount },
            }),
          );
        }

        callbackRef.current(geojson, count);
      }
    };

    // Handle worker errors
    workerRef.current.onerror = (error) => {
      console.error("❌ [Main Thread] SpeedTest Worker error:", error);
    };

    // Cleanup on unmount
    return () => {
      if (workerRef.current) {
        workerRef.current.terminate();
        workerRef.current = null;
      }
    };
  }, []);

  // Function to send SpeedTests to worker for processing with existing features for deduplication
  const processSpeedTests = useCallback((speedtests: SpeedTestViewport[], existingFeatures: GeoJSONFeature[] = []) => {
    if (!workerRef.current) {
      if (process.env.NODE_ENV === "development") {
        console.warn("⚠️ [Main Thread] SpeedTest Worker not initialized");
      }
      return;
    }

    // Dispatch process event for debug panel
    if (typeof window !== "undefined") {
      window.dispatchEvent(
        new CustomEvent("worker-stats", {
          detail: { type: "WORKER_PROCESS", count: speedtests.length, workerType: "speedtest" },
        }),
      );
    }

    const message: WorkerMessage = { type: "PROCESS_SPEEDTESTS", speedtests, existingFeatures };

    workerRef.current.postMessage(message);
  }, []);

  return { processSpeedTests };
};
