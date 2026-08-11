// Web Worker for processing Cell data into GeoJSON format
// This runs off the main thread to keep UI responsive

/**
 * Simplified Cell type from viewport API
 */
export type CellViewport = {
  id: string; // Cell unique identifier
  isp: string; // Service provider name
  ispColor: string; // Provider brand color (hex)
  lat: number; // Latitude
  lng: number; // Longitude
  name: string; // Cell site name
  status: string; // Cell status
};

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

export type GeoJSONFeatureCollection = {
  type: "FeatureCollection";
  features: (GeoJSONFeature | GeoJSONPolygonFeature)[];
};

export type WorkerMessage = {
  type: "PROCESS_CELLS";
  cells: CellViewport[];
  radiusInKilometer: number;
  segments?: number;
  existingFeatures?: (GeoJSONFeature | GeoJSONPolygonFeature)[]; // For intelligent deduplication
};

export type WorkerResponse = {
  type: "CELLS_PROCESSED";
  geojson: GeoJSONFeatureCollection;
  count: number;
  newCount: number; // Count of newly added (non-duplicate) cells
  duplicateCount: number; // Count of filtered duplicates
  duration: number;
};

// Log when worker is initialized (only in development)
if (process.env.NODE_ENV === "development") {
  console.log("🔧 [Web Worker] Cell Processor Worker initialized");
}

/**
 * Get provider icon ID for a given provider name
 */
function getProviderIconId(providerName: string): string {
  const normalizedName = providerName.toLowerCase();
  if (normalizedName.includes("smart")) return "cell-smart";
  if (normalizedName.includes("dito")) return "cell-dito";
  if (normalizedName.includes("globe")) return "cell-globe";
  return "cell-default";
}

// Listen for messages from the main thread
self.onmessage = (event: MessageEvent<WorkerMessage>) => {
  const { type, cells, radiusInKilometer, segments = 6, existingFeatures = [] } = event.data;

  if (type === "PROCESS_CELLS") {
    const startTime = performance.now();

    // Create a Set of existing cell IDs for fast O(1) lookup
    const existingIds = new Set(
      existingFeatures
        .filter((f) => f.properties.type === "point") // Only check point features for IDs
        .map((f) => (f as GeoJSONFeature).properties.id),
    );

    // Transform CellViewport data to GeoJSON format with polygons and deduplication
    const newFeatures: (GeoJSONFeature | GeoJSONPolygonFeature)[] = [];
    let duplicateCount = 0;

    for (const cell of cells) {
      // Generate unique ID from coordinates (since viewport API doesn't include database IDs)
      const cellId = cell.id;

      // Skip if this cell ID already exists
      if (existingIds.has(cellId)) {
        duplicateCount++;
        continue;
      }

      // Add to existing IDs set to prevent duplicates within this batch
      existingIds.add(cellId);

      const point: GeoJSONFeature = {
        id: `point-${cellId}`,
        type: "Feature",
        geometry: { type: "Point", coordinates: [cell.lng, cell.lat] },
        properties: {
          id: cellId,
          name: cell.name,
          description: "", // Not available in viewport API
          isp: cell.isp,
          ispColor: cell.ispColor,
          providerIcon: getProviderIconId(cell.isp), // Add provider icon ID
          providerColor: cell.ispColor, // Alias for consistency
          image: "/assets/images/cell-site.jpg",
          lat: cell.lat,
          lng: cell.lng,
          isVerified: false, // Not available in viewport API
          status: cell.status,
          type: "point",
        },
      };

      // For new clustered approach, we only need point features
      // Polygon coverage circles are no longer used
      newFeatures.push(point);
    }

    // Merge with existing features
    const geojson: GeoJSONFeatureCollection = {
      type: "FeatureCollection",
      features: [...existingFeatures, ...newFeatures],
    };

    const endTime = performance.now();
    const duration = endTime - startTime;

    if (process.env.NODE_ENV === "development" && duplicateCount > 0) {
      console.log(`🔄 [Cell Worker] Filtered ${duplicateCount} duplicates, added ${newFeatures.length} new Cells`);
    }

    // Send the processed GeoJSON back to the main thread
    const response: WorkerResponse = {
      type: "CELLS_PROCESSED",
      geojson,
      count: geojson.features.length, // Total point features
      newCount: newFeatures.length, // New point features added
      duplicateCount,
      duration,
    };

    self.postMessage(response);
  }
};

// Export empty object to make this a module
export {};
