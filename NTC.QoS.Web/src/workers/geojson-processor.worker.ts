// Web Worker for processing NAP data into GeoJSON format
// This runs off the main thread to keep UI responsive

// Viewport API Response Type (matches viewport API exactly)
export type NAPViewport = {
  isp: string; // lowercase
  ispColor: string; // camelCase
  lat: number;
  lng: number;
};

export type GeoJSONFeature = {
  type: "Feature";
  geometry: { type: "Point"; coordinates: [number, number] };
  properties: {
    // id removed - not used for NAPs (only Cells need it for detail pages)
    isp: string;
    ispColor: string;
  };
};

export type GeoJSONFeatureCollection = { type: "FeatureCollection"; features: GeoJSONFeature[] };

export type WorkerMessage = {
  type: "PROCESS_NAPS";
  naps: NAPViewport[];
  existingFeatures?: GeoJSONFeature[]; // For intelligent deduplication
};

export type WorkerResponse = {
  type: "NAPS_PROCESSED";
  geojson: GeoJSONFeatureCollection;
  count: number;
  newCount: number; // Count of newly added (non-duplicate) features
  duplicateCount: number; // Count of filtered duplicates
  duration: number;
};

// Log when worker is initialized (only in development)
if (process.env.NODE_ENV === "development") {
  console.log("🔧 [Web Worker] GeoJSON Processor Worker initialized");
}

// Listen for messages from the main thread
self.onmessage = (event: MessageEvent<WorkerMessage>) => {
  const { type, naps, existingFeatures = [] } = event.data;

  if (type === "PROCESS_NAPS") {
    const startTime = performance.now();

    // Create a Set of existing coordinates for fast O(1) lookup
    const existingCoords = new Set(
      existingFeatures.map((f) => `${f.geometry.coordinates[0]},${f.geometry.coordinates[1]}`),
    );

    // Transform NAP data to GeoJSON format with deduplication
    const newFeatures: GeoJSONFeature[] = [];
    let duplicateCount = 0;

    for (const nap of naps) {
      const coordKey = `${nap.lng},${nap.lat}`;

      // Skip if this coordinate already exists
      if (existingCoords.has(coordKey)) {
        duplicateCount++;
        continue;
      }

      // Add to existing coords set to prevent duplicates within this batch
      existingCoords.add(coordKey);

      newFeatures.push({
        type: "Feature",
        geometry: { type: "Point", coordinates: [nap.lng, nap.lat] },
        properties: { isp: nap.isp, ispColor: nap.ispColor },
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
      console.log(`🔄 [NAP Worker] Filtered ${duplicateCount} duplicates, added ${newFeatures.length} new NAPs`);
    }

    // Send the processed GeoJSON back to the main thread
    const response: WorkerResponse = {
      type: "NAPS_PROCESSED",
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
