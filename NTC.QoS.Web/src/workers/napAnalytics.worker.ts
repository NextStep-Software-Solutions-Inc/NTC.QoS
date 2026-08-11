/**
 * NAP Analytics Web Worker
 * Performs heavy computational tasks off the main thread for smooth UI
 *
 * Tasks:
 * - Calculate Provider Market Share
 * - Generate Density Heatmap data
 * - Create synthetic Time-Series data for growth charts
 */

// Efficient string hashing function (djb2 algorithm)
// Much faster than split().reduce() for large datasets
function hashString(str: string): number {
  let hash = 5381;
  for (let i = 0; i < str.length; i++) {
    hash = ((hash << 5) + hash) ^ str.charCodeAt(i);
  }
  return Math.abs(hash);
}

// Types for worker communication
export interface NAPDataPoint {
  id: string;
  isp: string;
  ispColor: string;
  lat: number;
  lng: number;
  status?: string;
  healthScore?: number;
  installationDate?: string;
}

export interface MarketShareData {
  provider: string;
  color: string;
  count: number;
  percentage: number;
}

export interface DensityPoint {
  lat: number;
  lng: number;
  weight: number;
}

export interface TimeSeriesData {
  date: string;
  total: number;
  byProvider: Record<string, number>;
}

export interface ProviderComparison {
  provider: string;
  color: string;
  totalNAPs: number;
  activeNAPs: number;
  averageHealth: number;
}

export interface AnalyticsResult {
  marketShare: MarketShareData[];
  densityHeatmap: DensityPoint[];
  timeSeries: TimeSeriesData[];
  providerComparison: ProviderComparison[];
  summary: {
    totalNAPs: number;
    activeNAPs: number;
    maintenanceNAPs: number;
    inactiveNAPs: number;
    averageHealth: number;
    topProvider: string;
    coverage: { minLat: number; maxLat: number; minLng: number; maxLng: number };
  };
}

export type WorkerMessage = {
  type: "CALCULATE_ANALYTICS";
  naps: NAPDataPoint[];
  options?: { gridSize?: number; timeSeriesMonths?: number };
};

export type WorkerResponse = { type: "ANALYTICS_COMPLETE"; result: AnalyticsResult; duration: number };

// Debounce state
let debounceTimer: ReturnType<typeof setTimeout> | null = null;
const DEBOUNCE_MS = 500;

// Log initialization
if (process.env.NODE_ENV === "development") {
  console.log("📊 [Analytics Worker] NAP Analytics Worker initialized");
}

// Calculate market share by provider
function calculateMarketShare(naps: NAPDataPoint[]): MarketShareData[] {
  const providerCounts = new Map<string, { count: number; color: string }>();

  for (const nap of naps) {
    const existing = providerCounts.get(nap.isp);
    if (existing) {
      existing.count++;
    } else {
      providerCounts.set(nap.isp, { count: 1, color: nap.ispColor || "#6b7280" });
    }
  }

  const total = naps.length;
  const result: MarketShareData[] = [];

  for (const [provider, data] of providerCounts) {
    result.push({
      provider,
      color: data.color,
      count: data.count,
      percentage: Math.round((data.count / total) * 10000) / 100, // 2 decimal places
    });
  }

  // Sort by count descending
  return result.sort((a, b) => b.count - a.count);
}

// Generate density heatmap data using grid-based aggregation
function generateDensityHeatmap(naps: NAPDataPoint[], gridSize: number = 0.01): DensityPoint[] {
  const grid = new Map<string, { lat: number; lng: number; weight: number }>();

  for (const nap of naps) {
    // Round to grid cell
    const gridLat = Math.round(nap.lat / gridSize) * gridSize;
    const gridLng = Math.round(nap.lng / gridSize) * gridSize;
    const key = `${gridLat},${gridLng}`;

    const existing = grid.get(key);
    if (existing) {
      existing.weight++;
    } else {
      grid.set(key, { lat: gridLat, lng: gridLng, weight: 1 });
    }
  }

  return Array.from(grid.values());
}

// Generate synthetic time series data based on installation dates
function generateTimeSeries(naps: NAPDataPoint[], months: number = 12): TimeSeriesData[] {
  const result: TimeSeriesData[] = [];
  const now = new Date();

  // Group NAPs by installation month
  const monthlyData = new Map<string, Map<string, number>>();

  for (const nap of naps) {
    // Use installation date if available, otherwise generate based on ID hash
    let installDate: Date;
    if (nap.installationDate) {
      installDate = new Date(nap.installationDate);
    } else {
      // Generate consistent date from ID using efficient hash
      const hash = hashString(nap.id);
      const monthsAgo = hash % (months * 2);
      installDate = new Date(now);
      installDate.setMonth(installDate.getMonth() - monthsAgo);
    }

    const monthKey = `${installDate.getFullYear()}-${String(installDate.getMonth() + 1).padStart(2, "0")}`;

    if (!monthlyData.has(monthKey)) {
      monthlyData.set(monthKey, new Map<string, number>());
    }

    const providerData = monthlyData.get(monthKey)!;
    providerData.set(nap.isp, (providerData.get(nap.isp) || 0) + 1);
  }

  // Generate monthly time series for the last N months
  let cumulativeTotal = 0;
  const cumulativeByProvider = new Map<string, number>();

  for (let i = months - 1; i >= 0; i--) {
    const date = new Date(now);
    date.setMonth(date.getMonth() - i);
    const monthKey = `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, "0")}`;

    const monthData = monthlyData.get(monthKey);
    if (monthData) {
      for (const [provider, count] of monthData) {
        cumulativeByProvider.set(provider, (cumulativeByProvider.get(provider) || 0) + count);
        cumulativeTotal += count;
      }
    }

    result.push({
      date: date.toLocaleDateString("en-US", { year: "numeric", month: "short" }),
      total: cumulativeTotal,
      byProvider: Object.fromEntries(cumulativeByProvider),
    });
  }

  return result;
}

// Calculate provider comparison metrics
function calculateProviderComparison(naps: NAPDataPoint[]): ProviderComparison[] {
  const providerStats = new Map<
    string,
    { color: string; total: number; active: number; healthSum: number; healthCount: number }
  >();

  for (const nap of naps) {
    const existing = providerStats.get(nap.isp);
    const isActive = nap.status?.toLowerCase() === "active";
    const health = nap.healthScore || 0;

    if (existing) {
      existing.total++;
      if (isActive) existing.active++;
      if (health > 0) {
        existing.healthSum += health;
        existing.healthCount++;
      }
    } else {
      providerStats.set(nap.isp, {
        color: nap.ispColor || "#6b7280",
        total: 1,
        active: isActive ? 1 : 0,
        healthSum: health,
        healthCount: health > 0 ? 1 : 0,
      });
    }
  }

  const result: ProviderComparison[] = [];
  for (const [provider, stats] of providerStats) {
    result.push({
      provider,
      color: stats.color,
      totalNAPs: stats.total,
      activeNAPs: stats.active,
      averageHealth: stats.healthCount > 0 ? Math.round(stats.healthSum / stats.healthCount) : 0,
    });
  }

  return result.sort((a, b) => b.totalNAPs - a.totalNAPs);
}

// Calculate summary statistics
function calculateSummary(naps: NAPDataPoint[], marketShare: MarketShareData[]) {
  let activeCount = 0;
  let maintenanceCount = 0;
  let inactiveCount = 0;
  let healthSum = 0;
  let healthCount = 0;
  let minLat = Infinity;
  let maxLat = -Infinity;
  let minLng = Infinity;
  let maxLng = -Infinity;

  for (const nap of naps) {
    // Status counts
    const status = nap.status?.toLowerCase();
    if (status === "active") activeCount++;
    else if (status === "maintenance") maintenanceCount++;
    else if (status === "inactive") inactiveCount++;

    // Health average
    if (nap.healthScore && nap.healthScore > 0) {
      healthSum += nap.healthScore;
      healthCount++;
    }

    // Coverage bounds
    if (nap.lat < minLat) minLat = nap.lat;
    if (nap.lat > maxLat) maxLat = nap.lat;
    if (nap.lng < minLng) minLng = nap.lng;
    if (nap.lng > maxLng) maxLng = nap.lng;
  }

  return {
    totalNAPs: naps.length,
    activeNAPs: activeCount,
    maintenanceNAPs: maintenanceCount,
    inactiveNAPs: inactiveCount,
    averageHealth: healthCount > 0 ? Math.round(healthSum / healthCount) : 0,
    topProvider: marketShare[0]?.provider || "Unknown",
    coverage: {
      minLat: minLat === Infinity ? 0 : minLat,
      maxLat: maxLat === -Infinity ? 0 : maxLat,
      minLng: minLng === Infinity ? 0 : minLng,
      maxLng: maxLng === -Infinity ? 0 : maxLng,
    },
  };
}

// Main analytics calculation
function calculateAnalytics(
  naps: NAPDataPoint[],
  options: { gridSize?: number; timeSeriesMonths?: number } = {},
): AnalyticsResult {
  const { gridSize = 0.01, timeSeriesMonths = 12 } = options;

  const marketShare = calculateMarketShare(naps);
  const densityHeatmap = generateDensityHeatmap(naps, gridSize);
  const timeSeries = generateTimeSeries(naps, timeSeriesMonths);
  const providerComparison = calculateProviderComparison(naps);
  const summary = calculateSummary(naps, marketShare);

  return { marketShare, densityHeatmap, timeSeries, providerComparison, summary };
}

// Listen for messages from main thread
self.onmessage = (event: MessageEvent<WorkerMessage>) => {
  const { type, naps, options } = event.data;

  if (type === "CALCULATE_ANALYTICS") {
    // Debounce to wait for user to stop interacting
    if (debounceTimer) {
      clearTimeout(debounceTimer);
    }

    debounceTimer = setTimeout(() => {
      const startTime = performance.now();

      const result = calculateAnalytics(naps, options);

      const endTime = performance.now();
      const duration = endTime - startTime;

      if (process.env.NODE_ENV === "development") {
        console.log(`📊 [Analytics Worker] Processed ${naps.length} NAPs in ${duration.toFixed(2)}ms`);
      }

      const response: WorkerResponse = { type: "ANALYTICS_COMPLETE", result, duration };

      self.postMessage(response);
    }, DEBOUNCE_MS);
  }
};

// Export empty object to make this a module
export {};
