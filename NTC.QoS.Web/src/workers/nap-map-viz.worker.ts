// Web Worker for NAP Map Visualization processing
// Handles heavy computations off the main thread:
// - Provider network line generation (loop/chain algorithm)
// - Dominant color calculation for clusters

export type NAPPoint = { id: string; lat: number; lng: number; isp: string; ispColor: string };

export type LineFeature = {
  type: "Feature";
  geometry: { type: "LineString"; coordinates: [number, number][] };
  properties: { provider: string; color: string };
};

export type LineFeatureCollection = { type: "FeatureCollection"; features: LineFeature[] };

export type ClusterColorResult = { clusterId: number; dominantColor: string; dominantProvider: string; count: number };

// Message types
export type WorkerMessage =
  | {
      type: "GENERATE_NETWORK_LINES";
      naps: NAPPoint[];
      maxDistance: number; // in degrees (~0.01 = 1km)
    }
  | { type: "CALCULATE_CLUSTER_COLORS"; clusters: { clusterId: number; points: { ispColor: string; isp: string }[] }[] }
  | { type: "CANCEL" };

export type WorkerResponse =
  | { type: "NETWORK_LINES_GENERATED"; geojson: LineFeatureCollection; lineCount: number; duration: number }
  | {
      type: "NETWORK_LINES_PROGRESS";
      progress: number; // 0-100
      currentProvider: string;
      processedProviders: number;
      totalProviders: number;
    }
  | { type: "CLUSTER_COLORS_CALCULATED"; results: ClusterColorResult[]; duration: number }
  | {
      type: "CLUSTER_COLORS_PROGRESS";
      progress: number; // 0-100
      processed: number;
      total: number;
    }
  | { type: "CANCELLED"; operationType: "network" | "colors" };

// Track cancellation per operation type
let networkCancelled = false;
let colorsCancelled = false;

// Log when worker is initialized
if (process.env.NODE_ENV === "development") {
  console.log("🔧 [Web Worker] NAP Map Visualization Worker initialized");
}

/**
 * Calculate the Euclidean distance between two points (in degrees)
 */
function distance(p1: NAPPoint, p2: NAPPoint): number {
  const dLat = p1.lat - p2.lat;
  const dLng = p1.lng - p2.lng;
  return Math.sqrt(dLat * dLat + dLng * dLng);
}

/**
 * Generate network lines using a fast nearest-neighbor mesh approach
 * Each point connects to its N nearest neighbors within maxDistance
 * Much faster than MST for large datasets
 */
async function generateNetworkLines(
  naps: NAPPoint[],
  maxDistance: number,
  sendProgress: (progress: WorkerResponse) => void,
): Promise<LineFeatureCollection | null> {
  const features: LineFeature[] = [];
  const MAX_NEIGHBORS = 3; // Each point connects to up to 3 nearest neighbors

  // Group NAPs by provider
  const napsByProvider = new Map<string, NAPPoint[]>();
  naps.forEach((nap) => {
    const provider = nap.isp || "Unknown";
    if (!napsByProvider.has(provider)) {
      napsByProvider.set(provider, []);
    }
    napsByProvider.get(provider)!.push(nap);
  });

  const totalProviders = napsByProvider.size;
  let processedProviders = 0;
  const providers = Array.from(napsByProvider.entries());

  // For each provider, connect each point to nearest neighbors
  for (const [provider, providerNaps] of providers) {
    // Check for cancellation
    if (networkCancelled) {
      return null;
    }

    // Send progress update and yield to event loop
    sendProgress({
      type: "NETWORK_LINES_PROGRESS",
      progress: Math.round((processedProviders / totalProviders) * 100),
      currentProvider: provider,
      processedProviders,
      totalProviders,
    });

    // Yield to allow progress message to be sent
    await new Promise((resolve) => setTimeout(resolve, 0));

    if (providerNaps.length < 2) {
      processedProviders++;
      continue;
    }

    const color = providerNaps[0]?.ispColor || "#6b7280";
    const addedEdges = new Set<string>(); // Track edges to avoid duplicates

    // For each point, find and connect to nearest neighbors
    for (let i = 0; i < providerNaps.length; i++) {
      const fromNap = providerNaps[i]!;

      // Calculate distances to all other points
      const distances: { idx: number; dist: number }[] = [];
      for (let j = 0; j < providerNaps.length; j++) {
        if (i === j) continue;
        const toNap = providerNaps[j]!;
        const dist = distance(fromNap, toNap);
        if (dist <= maxDistance) {
          distances.push({ idx: j, dist });
        }
      }

      // Sort by distance and take nearest neighbors
      distances.sort((a, b) => a.dist - b.dist);
      const neighbors = distances.slice(0, MAX_NEIGHBORS);

      // Create edges to neighbors
      for (const neighbor of neighbors) {
        const toNap = providerNaps[neighbor.idx]!;

        // Create unique edge key (smaller index first)
        const edgeKey = i < neighbor.idx ? `${i}-${neighbor.idx}` : `${neighbor.idx}-${i}`;

        // Skip if already added
        if (addedEdges.has(edgeKey)) continue;
        addedEdges.add(edgeKey);

        features.push({
          type: "Feature",
          geometry: {
            type: "LineString",
            coordinates: [
              [fromNap.lng, fromNap.lat],
              [toNap.lng, toNap.lat],
            ],
          },
          properties: { provider, color },
        });
      }
    }

    processedProviders++;
  }

  // Check for cancellation before final result
  if (networkCancelled) {
    return null;
  }

  // Send final progress
  sendProgress({
    type: "NETWORK_LINES_PROGRESS",
    progress: 100,
    currentProvider: "Complete",
    processedProviders: totalProviders,
    totalProviders,
  });

  return { type: "FeatureCollection", features };
}

/**
 * Calculate dominant color for clusters with progress reporting
 */
async function calculateClusterColors(
  clusters: { clusterId: number; points: { ispColor: string; isp: string }[] }[],
  sendProgress: (progress: WorkerResponse) => void,
): Promise<ClusterColorResult[] | null> {
  const total = clusters.length;
  const results: ClusterColorResult[] = [];

  for (let index = 0; index < clusters.length; index++) {
    // Check for cancellation
    if (colorsCancelled) {
      return null;
    }

    const cluster = clusters[index]!;

    // Send progress every 10 clusters or at specific milestones
    if (index % 10 === 0 || index === total - 1) {
      sendProgress({
        type: "CLUSTER_COLORS_PROGRESS",
        progress: Math.round(((index + 1) / total) * 100),
        processed: index + 1,
        total,
      });
      // Yield to allow progress message to be sent
      await new Promise((resolve) => setTimeout(resolve, 0));
    }

    const colorCounts = new Map<string, { count: number; provider: string }>();

    cluster.points.forEach((point) => {
      const color = point.ispColor || "#6b7280";
      const existing = colorCounts.get(color);
      if (existing) {
        existing.count++;
      } else {
        colorCounts.set(color, { count: 1, provider: point.isp || "Unknown" });
      }
    });

    let dominantColor = "#6b7280";
    let dominantProvider = "Unknown";
    let maxCount = 0;

    colorCounts.forEach((data, color) => {
      if (data.count > maxCount) {
        maxCount = data.count;
        dominantColor = color;
        dominantProvider = data.provider;
      }
    });

    results.push({ clusterId: cluster.clusterId, dominantColor, dominantProvider, count: cluster.points.length });
  }

  return results;
}

// Helper to send progress messages
const sendProgress = (response: WorkerResponse) => {
  self.postMessage(response);
};

// Listen for messages from the main thread
self.onmessage = async (event: MessageEvent<WorkerMessage>) => {
  const message = event.data;

  if (message.type === "CANCEL") {
    // Cancel both operations
    networkCancelled = true;
    colorsCancelled = true;
    self.postMessage({ type: "CANCELLED", operationType: "network" } as WorkerResponse);
    return;
  }

  if (message.type === "GENERATE_NETWORK_LINES") {
    // Reset only network cancellation flag
    networkCancelled = false;

    const startTime = performance.now();

    const geojson = await generateNetworkLines(message.naps, message.maxDistance, sendProgress);

    if (geojson === null) {
      // Operation was cancelled
      return;
    }

    const endTime = performance.now();

    const response: WorkerResponse = {
      type: "NETWORK_LINES_GENERATED",
      geojson,
      lineCount: geojson.features.length,
      duration: endTime - startTime,
    };

    self.postMessage(response);
  } else if (message.type === "CALCULATE_CLUSTER_COLORS") {
    // Reset only colors cancellation flag
    colorsCancelled = false;

    const startTime = performance.now();

    const results = await calculateClusterColors(message.clusters, sendProgress);

    if (results === null) {
      // Operation was cancelled
      return;
    }

    const endTime = performance.now();

    const response: WorkerResponse = { type: "CLUSTER_COLORS_CALCULATED", results, duration: endTime - startTime };

    self.postMessage(response);
  }
};
