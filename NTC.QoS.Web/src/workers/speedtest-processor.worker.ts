// Web Worker for processing SpeedTest data into GeoJSON format
// This runs off the main thread to keep UI responsive

export type SpeedTestViewport = {
  // id removed - not needed for map rendering
  // Backend can exclude this field to save memory
  lat: number;
  lng: number;
  downloadSpeed: number;
  uploadSpeed: number;
  isp: string;
  ispColor: string;
  signalQuality: string;
};

export type GeoJSONFeature = {
  type: "Feature";
  geometry: { type: "Point"; coordinates: [number, number] };
  properties: {
    // id removed - not used for SpeedTests (only Cells need it for detail pages)
    isp: string;
    ispColor: string;
    downloadSpeed: number;
    uploadSpeed: number;
    signalQuality: string;
  };
};

export type GeoJSONFeatureCollection = { type: "FeatureCollection"; features: GeoJSONFeature[] };

export type WorkerMessage = {
  type: "PROCESS_SPEEDTESTS";
  speedtests: SpeedTestViewport[];
  existingFeatures?: GeoJSONFeature[]; // For intelligent deduplication
};

export type WorkerResponse = {
  type: "SPEEDTESTS_PROCESSED";
  geojson: GeoJSONFeatureCollection;
  count: number;
  newCount: number; // Count of newly added (non-duplicate) features
  duplicateCount: number; // Count of filtered duplicates
  duration: number;
};

// Log when worker is initialized (only in development)
if (process.env.NODE_ENV === "development") {
  console.log("🔧 [Web Worker] SpeedTest Processor Worker initialized");
}

// Listen for messages from the main thread
self.onmessage = (event: MessageEvent<WorkerMessage>) => {
  const { type, speedtests, existingFeatures = [] } = event.data;

  if (type === "PROCESS_SPEEDTESTS") {
    const startTime = performance.now();

    // Create a Set of existing coordinates for fast O(1) lookup
    // Use coordinate key since speedtests don't have IDs
    const existingCoords = new Set(
      existingFeatures.map((f) => `${f.geometry.coordinates[0]},${f.geometry.coordinates[1]}`),
    );

    // Transform SpeedTest data to GeoJSON format with deduplication
    const newFeatures: GeoJSONFeature[] = [];
    let duplicateCount = 0;

    for (const test of speedtests) {
      const coordKey = `${test.lng},${test.lat}`;

      // Skip if this coordinate already exists
      if (existingCoords.has(coordKey)) {
        duplicateCount++;
        continue;
      }

      // Add to existing coords set to prevent duplicates within this batch
      existingCoords.add(coordKey);

      newFeatures.push({
        type: "Feature",
        geometry: { type: "Point", coordinates: [test.lng, test.lat] },
        properties: {
          // Note: id removed - not used in rendering or interactions
          // Backend can exclude this field to save ~80MB per million speedtests
          isp: test.isp,
          ispColor: test.ispColor,
          downloadSpeed: test.downloadSpeed,
          uploadSpeed: test.uploadSpeed,
          signalQuality: test.signalQuality,
        },
      });
    }

    // Merge with existing features
    const geojson: GeoJSONFeatureCollection = {
      type: "FeatureCollection",
      features: [...existingFeatures, ...newFeatures],
    };

    const endTime = performance.now();
    const duration = endTime - startTime;

    if (process.env.NODE_ENV === "development" && duplicateCount > 0) {
      console.log(
        `🔄 [SpeedTest Worker] Filtered ${duplicateCount} duplicates, added ${newFeatures.length} new SpeedTests`,
      );
    }

    // Send the processed GeoJSON back to the main thread
    const response: WorkerResponse = {
      type: "SPEEDTESTS_PROCESSED",
      geojson,
      count: geojson.features.length,
      newCount: newFeatures.length,
      duplicateCount,
      duration,
    };

    self.postMessage(response);
  }
};

// Export empty object to make this a module
export {};
