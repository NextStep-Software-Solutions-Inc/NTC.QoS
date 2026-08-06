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

export type WorkerMessage = { type: "PROCESS_NAPS"; naps: NAPViewport[]; existingFeatures?: GeoJSONFeature[] };

export type WorkerResponse = {
  type: "NAPS_PROCESSED";
  geojson: GeoJSONFeatureCollection;
  count: number;
  newCount: number;
  duplicateCount: number;
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

    // Create a Set for deduplication based on coordinates (rounded to avoid floating point issues)
    const existingCoords = new Set<string>();
    existingFeatures.forEach((feature) => {
      const [lng, lat] = feature.geometry.coordinates;
      // Round to 5 decimal places for deduplication (about 1 meter precision)
      const key = `${Math.round(lat * 100000) / 100000},${Math.round(lng * 100000) / 100000}`;
      existingCoords.add(key);
    });

    // Transform new NAP data to GeoJSON format with deduplication
    const newFeatures: GeoJSONFeature[] = [];
    let duplicateCount = 0;

    naps.forEach((nap) => {
      // Round coordinates for deduplication key
      const key = `${Math.round(nap.lat * 100000) / 100000},${Math.round(nap.lng * 100000) / 100000}`;

      if (!existingCoords.has(key)) {
        newFeatures.push({
          type: "Feature",
          geometry: { type: "Point", coordinates: [nap.lng, nap.lat] },
          properties: { isp: nap.isp, ispColor: nap.ispColor },
        });
      } else {
        duplicateCount++;
      }
    });

    // Merge existing features with new features
    const allFeatures = [...existingFeatures, ...newFeatures];

    const geojson: GeoJSONFeatureCollection = { type: "FeatureCollection", features: allFeatures };

    const endTime = performance.now();
    const duration = endTime - startTime;

    // Send the processed GeoJSON back to the main thread
    const response: WorkerResponse = {
      type: "NAPS_PROCESSED",
      geojson,
      count: naps.length, // Number of NAPs processed (for hook's totalLoadedCount tracking)
      newCount: newFeatures.length,
      duplicateCount,
      duration,
    };

    self.postMessage(response);
  }
};

// Export empty object to make this a module
export {};
