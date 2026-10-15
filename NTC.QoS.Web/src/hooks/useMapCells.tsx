"use client";

/**
 * useMapCells Hook - Revamped Version
 *
 * Manages cell site layers on the map with support for:
 * - Clustered cell sites with provider-specific icons
 * - Cell site heatmap (admin-only)
 * - Signal coverage visualization (admin-only)
 * - Point view mode (admin-only)
 * - Viewport-based loading with pagination
 * - Provider filtering
 *
 * Public map: Only shows cells-clusters, cells-symbols, cells-circles
 * Admin map: Shows all layers with full control
 */
import { useCallback, useEffect, useLayoutEffect, useMemo, useRef, useState } from "react";
import { Popup, type GeoJSONSource } from "@maptiler/sdk";
import type { InfiniteData } from "@tanstack/react-query";
import { toast } from "sonner";

import { getPublicCellsInfiniteRequest } from "@/components/features/cell/handlers/getPublicCellsInfiniteRequest";
import { getGenerationsRequest } from "@/components/features/generation/handlers/getGenerationsRequest";
import { getServiceProvidersRequest } from "@/components/features/service-provider/handlers/getServiceProvidersRequest";
import { type CellViewport } from "@/components/features/cell/types/Cell";
import { type CellularCoverageTileset } from "@/components/features/cellular-coverage/types/CellularCoverageTileset";
import { useCellGeoJSONWorker, type CellGeoJSONFeatureCollection } from "@/hooks/useCellGeoJSONWorker";
import { useMap } from "@/hooks/useMap";
import { useMapViewport } from "@/hooks/useMapViewport";
import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";
import { PHILIPPINES_BOUNDS } from "@/lib/constants/map-bounds";
import { request } from "@/services/fetch";
import type { APIResponse, PaginationResponse } from "@/types/reusable/api";
import { createInfiniteQueryHookFromTemplate } from "./createInfiniteQueryHookFromTemplate";
import {
  createSignalCoveragePopupHTML,
  getCellCircleLayerPaint,
  getCellSiteHeatmapPaint,
  getClusterLayerPaint,
  getPointViewCirclePaint,
  getProviderIconId,
  getSignalCoverageCircleColorPaint,
  getSignalCoverageCirclePaint,
  getSignalStrengthColor,
  loadProviderIcons,
} from "./map-cells-utils";
import type { ConfiguredSignalLevel } from "@/components/admin-map/types";

// Constants
const PAGE_SIZE = 2000;
const CELL_RADIUS_KM = 2;
const CLUSTER_RADIUS = 25;
const CLUSTER_MAX_ZOOM = 14;
// Slightly larger icons improve tower legibility on dense/imagery-heavy styles (e.g., Streets/Satellite).
const CELL_ICON_SIZE = 0.5;

// Layer configuration interface (matches CellsiteMapView)
export interface LayerConfig {
  cellSites: boolean;
  cellSiteHeatmap: boolean;
  signalCoverage: boolean;
  pointView: boolean;
}

// Options for cell filtering
export interface CellOptions {
  regionIds?: string[];
  serviceProviderIds?: string[];
  generationIds?: string[];
  isAdminMode?: boolean; // New: determines if admin-only layers are available
  /** Area study label used to scope signal coverage GeoJSON features. */
  signalCoverageAreaStudyName?: string;
  /** Explicit signal-level ranges to include (combined with OR). */
  signalLevelRanges?: Array<{ min?: number; max?: number; color?: string; description?: string; isTop?: boolean }>;
  /** Full set of configured signal levels for the active area study */
  configuredSignalLevels?: ConfiguredSignalLevel[];
  /** Signal strength range filter (dBm) applied to the Radio Network Coverage layer */
  signalStrengthFilter?: { min: number; max: number };
  /** Free-text search for signal coverage properties (e.g., location, technology, cell GUID). */
  searchQuery?: string;
}

const useGetCellsInfinite = createInfiniteQueryHookFromTemplate(getPublicCellsInfiniteRequest);
const useGetWirelessServiceProviders = createQueryHookFromTemplate(getServiceProvidersRequest);
const useGetGenerations = createQueryHookFromTemplate(getGenerationsRequest);

const SIGNAL_COVERAGE_GEOJSON_POC_PATH = "/api/signal-coverage/geojson";
const MAPTILER_SIGNAL_COVERAGE_URL = process.env.NEXT_PUBLIC_MAPTILER_SIGNAL_COVERAGE_URL;
const MAPTILER_SIGNAL_SOURCE_LAYER_ID = process.env.NEXT_PUBLIC_MAPTILER_SIGNAL_SOURCE_LAYER_ID ?? "signal_coverage";

const normalizeSearchToken = (value: unknown): string => String(value ?? "").trim().toLowerCase().replace(/\s+/g, " ");

const PROVIDER_NAME_STRIP_REGEX =
  /\b(INC\.?|CORP\.?|CORPORATION|COMMUNICATIONS?|TELECOM(MUNICATIONS?)?|TELECOMMUNITY|NETWORKS?|DATA|SOLUTIONS?|SERVICES?|CO\.?|LTD\.?|LLC|COMPANY|ENTERPRISES?|TECHNOLOGY|ICT)\b\.?/gi;

const getProviderLookupTokens = (value: unknown): string[] => {
  const normalized = normalizeSearchToken(value);
  if (!normalized) return [];

  const cleaned = normalized.replace(/[^a-z0-9\s]/g, " ").replace(/\s+/g, " ").trim();
  const stripped = cleaned.replace(PROVIDER_NAME_STRIP_REGEX, " ").replace(/\s+/g, " ").trim();
  const firstWord = stripped.split(" ")[0] ?? "";

  return Array.from(new Set([normalized, cleaned, stripped, firstWord].filter(Boolean)));
};

const parseAzimuthToDecimal = (value: unknown): number | undefined => {
  if (value === null || value === undefined) return undefined;

  const cleaned = String(value).replace(",", ".").match(/-?\d+(?:\.\d+)?/);
  if (!cleaned?.[0]) return undefined;

  const parsed = Number(cleaned[0]);
  if (!Number.isFinite(parsed)) return undefined;

  const normalized = ((parsed % 360) + 360) % 360;
  return Number(normalized.toFixed(2));
};

const formatAzimuth = (value?: number): string => {
  if (value === undefined) return "N/A";
  return `${value}°`;
};

const toNonEmptyString = (value: unknown): string | undefined => {
  const text = String(value ?? "").trim();
  return text.length > 0 ? text : undefined;
};

type SignalCoverageFeatureCollection = GeoJSON.FeatureCollection<GeoJSON.Point, Record<string, unknown>>;
type SignalCoveragePolygonCollection = GeoJSON.FeatureCollection<GeoJSON.Polygon, Record<string, unknown>>;
type SignalCoverageGridCollection = GeoJSON.FeatureCollection<GeoJSON.Polygon, Record<string, unknown>>;

const roundLongitudeColumn = (lng: number): string => lng.toFixed(6);

const getSignalColorFromValue = (value: number): string => {
  return getSignalStrengthColor(value);
};

const calculateMedian = (values: number[]): number => {
  if (values.length === 0) return -120;
  const sorted = [...values].sort((a, b) => a - b);
  const middle = Math.floor(sorted.length / 2);
  if (sorted.length % 2 === 0) {
    return ((sorted[middle - 1] as number) + (sorted[middle] as number)) / 2;
  }
  return sorted[middle] as number;
};

const isSameCoordinate = (a: [number, number], b: [number, number]): boolean =>
  Math.abs(a[0] - b[0]) < 1e-9 && Math.abs(a[1] - b[1]) < 1e-9;

const medianStep = (values: number[]): number => {
  if (values.length < 2) return 0;
  const sorted = [...values].sort((a, b) => a - b);
  const diffs: number[] = [];
  for (let i = 1; i < sorted.length; i++) {
    const diff = Math.abs((sorted[i] as number) - (sorted[i - 1] as number));
    if (diff > 0) diffs.push(diff);
  }
  if (diffs.length === 0) return 0;
  const ordered = diffs.sort((a, b) => a - b);
  const mid = Math.floor(ordered.length / 2);
  if (ordered.length % 2 === 0) {
    return ((ordered[mid - 1] as number) + (ordered[mid] as number)) / 2;
  }
  return ordered[mid] as number;
};

const estimateIDWStrength = (
  lng: number,
  lat: number,
  features: GeoJSON.Feature<GeoJSON.Point, Record<string, unknown>>[],
  k: number = 8,
  power: number = 2,
): number | undefined => {
  if (features.length === 0) return undefined;

  const nearest = features
    .map((feature) => {
      const flng = feature.geometry.coordinates[0];
      const flat = feature.geometry.coordinates[1];
      if (flng === undefined || flat === undefined) return { feature, distance2: Infinity };
      const dx = flng - lng;
      const dy = flat - lat;
      return {
        feature,
        distance2: dx * dx + dy * dy,
      };
    })
    .sort((a, b) => a.distance2 - b.distance2)
    .slice(0, Math.min(k, features.length));

  const exact = nearest.find((entry) => entry.distance2 === 0);
  if (exact) {
    const exactStrength = Number(exact.feature.properties?.strength);
    return Number.isFinite(exactStrength) ? exactStrength : undefined;
  }

  let weighted = 0;
  let weightSum = 0;

  for (const item of nearest) {
    const strength = Number(item.feature.properties?.strength);
    if (!Number.isFinite(strength)) continue;

    const distance = Math.sqrt(item.distance2);
    const weight = 1 / Math.pow(Math.max(distance, 1e-6), power);
    weighted += strength * weight;
    weightSum += weight;
  }

  if (weightSum <= 0) return undefined;
  return weighted / weightSum;
};

const buildInterpolatedCoverageGrid = (points: SignalCoverageFeatureCollection): SignalCoverageGridCollection => {
  const features = points.features;
  if (features.length === 0) {
    return { type: "FeatureCollection", features: [] };
  }

  const uniqueLng = Array.from(
    new Set(features.map((f) => f.geometry.coordinates[0]).filter((v): v is number => typeof v === "number")),
  );
  const uniqueLat = Array.from(
    new Set(features.map((f) => f.geometry.coordinates[1]).filter((v): v is number => typeof v === "number")),
  );
  const stepLng = medianStep(uniqueLng) || 0.004;
  const stepLat = medianStep(uniqueLat) || 0.004;
  const halfLng = stepLng / 2;
  const halfLat = stepLat / 2;

  const gridFeatures: SignalCoverageGridCollection["features"] = [];
  for (const feature of features) {
    const lng = feature.geometry.coordinates[0];
    const lat = feature.geometry.coordinates[1];
    if (lng === undefined || lat === undefined || !Number.isFinite(lng) || !Number.isFinite(lat)) continue;

    const measuredStrength = Number(feature.properties?.strength ?? -120);

    const ring: [number, number][] = [
      [lng - halfLng, lat - halfLat],
      [lng + halfLng, lat - halfLat],
      [lng + halfLng, lat + halfLat],
      [lng - halfLng, lat + halfLat],
      [lng - halfLng, lat - halfLat],
    ];

    gridFeatures.push({
      type: "Feature",
      geometry: {
        type: "Polygon",
        coordinates: [ring],
      },
      properties: {
        ...(feature.properties ?? {}),
        strength: Number(measuredStrength.toFixed(2)),
        fillColor: getSignalColorFromValue(measuredStrength),
        interpolation_method: "measured-cell",
      },
    });
  }

  return {
    type: "FeatureCollection",
    features: gridFeatures,
  };
};

const buildCoveragePolygonsFromPoints = (
  points: SignalCoverageFeatureCollection,
): SignalCoveragePolygonCollection => {
  const groups = new Map<string, GeoJSON.Feature<GeoJSON.Point, Record<string, unknown>>[]>();

  for (const feature of points.features) {
    const props = feature.properties ?? {};
    const key =
      String(props.source_file ?? "").trim() ||
      String(props.cell_id ?? "").trim() ||
      [props.network, props.technology, props.band, props.frequency]
        .map((part) => String(part ?? "").trim())
        .filter(Boolean)
        .join("|") ||
      "coverage-default";

    const bucket = groups.get(key) ?? [];
    bucket.push(feature);
    groups.set(key, bucket);
  }

  const polygonFeatures: SignalCoveragePolygonCollection["features"] = [];

  for (const [groupKey, groupFeatures] of groups.entries()) {
    const columns = new Map<string, { lng: number; minLat: number; maxLat: number }>();
    const strengths: number[] = [];

    for (const feature of groupFeatures) {
      const lng = feature.geometry.coordinates[0];
      const lat = feature.geometry.coordinates[1];
      if (lng === undefined || lat === undefined || !Number.isFinite(lng) || !Number.isFinite(lat)) continue;

      const key = roundLongitudeColumn(lng);
      const existing = columns.get(key);
      if (!existing) {
        columns.set(key, { lng, minLat: lat, maxLat: lat });
      } else {
        existing.minLat = Math.min(existing.minLat, lat);
        existing.maxLat = Math.max(existing.maxLat, lat);
      }

      const strength = Number(feature.properties?.strength);
      if (Number.isFinite(strength)) strengths.push(strength);
    }

    if (columns.size < 2) continue;

    const sortedColumns = Array.from(columns.values()).sort((a, b) => a.lng - b.lng);
    const topEdge: [number, number][] = sortedColumns.map((column) => [column.lng, column.maxLat]);
    const bottomEdge: [number, number][] = [...sortedColumns]
      .reverse()
      .map((column) => [column.lng, column.minLat]);

    const ring = [...topEdge, ...bottomEdge];
    if (ring.length < 4) continue;

    const first = ring[0] as [number, number];
    const last = ring[ring.length - 1] as [number, number];
    if (!isSameCoordinate(first, last)) {
      ring.push(first);
    }

    if (ring.length < 4) continue;

    const medianStrength = calculateMedian(strengths);
    const fillColor = getSignalColorFromValue(medianStrength);
    const sample = groupFeatures[0]?.properties ?? {};

    polygonFeatures.push({
      type: "Feature",
      geometry: {
        type: "Polygon",
        coordinates: [ring],
      },
      properties: {
        ...sample,
        coverage_key: groupKey,
        strength: medianStrength,
        fillColor,
      },
    });
  }

  return {
    type: "FeatureCollection",
    features: polygonFeatures,
  };
};

const parseSignalCoverageGeoJSON = (
  raw: unknown,
  providerIdByName: Map<string, string>,
  providerNameById: Map<string, string>,
  generationIdByName: Map<string, string>,
): SignalCoverageFeatureCollection => {
  const empty: SignalCoverageFeatureCollection = { type: "FeatureCollection", features: [] };
  if (!raw || typeof raw !== "object") return empty;

  const maybeCollection = raw as {
    type?: string;
    features?: Array<{
      type?: string;
      geometry?: { type?: string; coordinates?: [number, number] };
      properties?: Record<string, unknown>;
    }>;
  };

  if (maybeCollection.type !== "FeatureCollection" || !Array.isArray(maybeCollection.features)) {
    return empty;
  }

  const features: SignalCoverageFeatureCollection["features"] = [];

  for (const feature of maybeCollection.features) {
    if (feature?.type !== "Feature") continue;
    if (feature.geometry?.type !== "Point") continue;

    const lng = feature.geometry.coordinates?.[0];
    const lat = feature.geometry.coordinates?.[1];
    if (lng === undefined || lat === undefined || !Number.isFinite(lng) || !Number.isFinite(lat)) continue;

    const properties = feature.properties ?? {};
    const strengthRaw = properties.signal_strength_dbm ?? properties.strength;
    const strength = Number(strengthRaw);
    if (!Number.isFinite(strength)) continue;

    const network = toNonEmptyString(properties.network) ?? "Unknown";
    const technology = toNonEmptyString(properties.technology) ?? "Unknown";
    const band = toNonEmptyString(properties.band);
    const frequency = toNonEmptyString(properties.frequency);
    const address = toNonEmptyString(properties.address);
    const location = toNonEmptyString(properties.location);
    const cellId = toNonEmptyString(properties.cell_id);
    const areaStudy = toNonEmptyString(properties.areaStudy);
    const azimuthValue = parseAzimuthToDecimal(properties.azimuth);
    const normalizedNetwork = normalizeSearchToken(network);
    const normalizedTechnology = normalizeSearchToken(technology);
    const providerId = getProviderLookupTokens(normalizedNetwork)
      .map((token) => providerIdByName.get(token))
      .find((value): value is string => Boolean(value));
    const providerName = providerId ? providerNameById.get(providerId) : undefined;
    const generationId = generationIdByName.get(normalizedTechnology);

    const searchText = [network, providerName, technology, band, frequency, address, location, cellId, areaStudy]
      .filter((token): token is string => Boolean(token))
      .join(" ")
      .toLowerCase();

    features.push({
      type: "Feature",
      geometry: { type: "Point", coordinates: [lng, lat] },
      properties: {
        strength,
        barangay: location ?? address,
        azimuth: formatAzimuth(azimuthValue),
        azimuthValue,
        network,
        networkProviderId: providerId,
        technology,
        generationId,
        band,
        frequency,
        address,
        location,
        cell_id: cellId,
        areaStudy,
        search_text: searchText,
        service_type: "Wireless",
      },
    });
  }

  return { type: "FeatureCollection", features };
};

const parseSignalCoverageFlatData = (
  raw: unknown,
  providerIdByName: Map<string, string>,
  providerNameById: Map<string, string>,
  generationIdByName: Map<string, string>,
): SignalCoverageFeatureCollection => {
  const empty: SignalCoverageFeatureCollection = { type: "FeatureCollection", features: [] };
  if (!Array.isArray(raw)) return empty;

  const features: SignalCoverageFeatureCollection["features"] = [];

  for (const point of raw as Array<Record<string, unknown>>) {
    const lat = Number(point.lat);
    const lng = Number(point.lng);
    const strength = Number(point.strength);
    if (!Number.isFinite(lat) || !Number.isFinite(lng) || !Number.isFinite(strength)) continue;

    const network = toNonEmptyString(point.network) ?? "Unknown";
    const technology = toNonEmptyString(point.technology) ?? "Unknown";
    const normalizedNetwork = normalizeSearchToken(network);
    const normalizedTechnology = normalizeSearchToken(technology);
    const providerId = getProviderLookupTokens(normalizedNetwork)
      .map((token) => providerIdByName.get(token))
      .find((value): value is string => Boolean(value));
    const providerName = providerId ? providerNameById.get(providerId) : undefined;

    const azimuthValue = parseAzimuthToDecimal(point.azimuth);
    const location = toNonEmptyString(point.location) ?? toNonEmptyString(point.barangay);
    const areaStudy = toNonEmptyString(point.areaStudy);
    const searchText = [network, providerName, technology, location, toNonEmptyString(point.cell_id), areaStudy]
      .filter((token): token is string => Boolean(token))
      .join(" ")
      .toLowerCase();

    features.push({
      type: "Feature",
      geometry: { type: "Point", coordinates: [lng, lat] },
      properties: {
        strength,
        barangay: location,
        azimuth: formatAzimuth(azimuthValue),
        azimuthValue,
        network,
        networkProviderId: providerId,
        technology,
        generationId: generationIdByName.get(normalizedTechnology),
        cell_id: toNonEmptyString(point.cell_id),
        areaStudy,
        search_text: searchText,
        service_type: "Wireless",
      },
    });
  }

  return { type: "FeatureCollection", features };
};

/**
 * Base map layers that should render BELOW cell layers.
 * Used to find the insertion anchor when re-ordering layers after style changes.
 * Must be in order of typical z-order in MapTiler styles.
 */
const LAYERS_BELOW_CELLS = [
  // Base map elements that commonly exist in most styles
  "background", "water", "landuse", "landcover", "terrain", "building", "road", "bridge", "tunnel",
] as const;

/**
 * Main hook for managing cell site layers
 */
export const useMapCells = (layerConfig: LayerConfig | Record<string, boolean>, options?: CellOptions) => {
  const { map, isMapLoaded } = useMap();
  const iconsLoadedRef = useRef<Set<string>>(new Set());

  // Refs for event handlers and cleanup flag
  const isCleaningUpRef = useRef(false);
  const handleStyleDataRef = useRef<(() => void) | null>(null);
  const handleSourceDataRef = useRef<(() => void) | null>(null);
  // Normalize layer config (support both new and legacy formats)
  const normalizedConfig: LayerConfig = useMemo(() => {
    const input = layerConfig as Record<string, boolean>;

    // Legacy format detection (has "cellular" key)
    if ("cellular" in input) {
      return {
        cellSites: input.cellular ?? false,
        cellSiteHeatmap: false, // Admin only
        signalCoverage: false, // Admin only
        pointView: false, // Admin only
      };
    }

    // New format - restrict admin layers in public mode
    const isAdminMode = options?.isAdminMode ?? false;
    return {
      cellSites: input.cellSites ?? false,
      cellSiteHeatmap: isAdminMode ? (input.cellSiteHeatmap ?? false) : false,
      signalCoverage: isAdminMode ? (input.signalCoverage ?? false) : false,
      pointView: isAdminMode ? (input.pointView ?? false) : false,
    };
  }, [layerConfig, options?.isAdminMode]);

  // State
  const [accumulatedGeoJSON, setAccumulatedGeoJSON] = useState<CellGeoJSONFeatureCollection>({
    type: "FeatureCollection",
    features: [],
  });
  const [totalLoadedCount, setTotalLoadedCount] = useState(0);
  const [rawSignalCoverageData, setRawSignalCoverageData] = useState<unknown>(null);
  const [signalCoverageData, setSignalCoverageData] = useState<GeoJSON.FeatureCollection | null>(null);
  const [coverageSourceType, setCoverageSourceType] = useState<"maptiler" | "geojson" | null>(null);
  const [dynamicCoverageUrl, setDynamicCoverageUrl] = useState<string | null>(null);
  const [signalCoveragePolygons, setSignalCoveragePolygons] = useState<SignalCoveragePolygonCollection | null>(null);
  const [signalCoverageGrid, setSignalCoverageGrid] = useState<SignalCoverageGridCollection | null>(null);

  const lastToastUpdateRef = useRef(0);
  const lastProgressPercentRef = useRef(0);
  const updateTimeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const previousViewportRef = useRef<typeof viewportBounds>(null);
  const previousOptionsRef = useRef<string>("");

  // Keep a ref to the latest accumulated GeoJSON so the main layer setup effect can access it
  // without needing it as a dependency (avoids layer teardown on every data update)
  const accumulatedGeoJSONRef = useRef(accumulatedGeoJSON);
  useLayoutEffect(() => {
    accumulatedGeoJSONRef.current = accumulatedGeoJSON;
  }, [accumulatedGeoJSON]);

  // Always-current ref for normalizedConfig so event handlers (e.g. handleSourceData)
  // can apply the correct visibility after a style change without needing normalizedConfig
  // in the main effect's dependency array (which would cause unnecessary layer teardown).
  const normalizedConfigRef = useRef(normalizedConfig);
  useLayoutEffect(() => {
    normalizedConfigRef.current = normalizedConfig;
  }, [normalizedConfig]);

  // Keep a ref to configuredSignalLevels so popup event handlers can always format with active level definitions
  const configuredSignalLevelsRef = useRef(options?.configuredSignalLevels);
  useLayoutEffect(() => {
    configuredSignalLevelsRef.current = options?.configuredSignalLevels;
  }, [options?.configuredSignalLevels]);

  // Track viewport for spatial loading
  const { bounds: viewportBounds, isStable: isViewportStable } = useMapViewport({
    debounceMs: 800,
    enabled: isMapLoaded && Object.values(normalizedConfig).some((v) => v),
    constraint: PHILIPPINES_BOUNDS,
    showDebugBox: process.env.NODE_ENV === "development",
  });

  // Fetch cells with infinite query
  const {
    data: cellsData,
    fetchNextPage,
    hasNextPage,
    isFetchingNextPage,
    isLoading,
  } = useGetCellsInfinite({
    enabled: isMapLoaded && Object.values(normalizedConfig).some((v) => v) && isViewportStable && !!viewportBounds,
    pathParams: {},
    queryParams: {
      regionIds: options?.regionIds?.length ? options.regionIds : [],
      serviceProviderIds: options?.serviceProviderIds,
      generationIds: options?.generationIds,
      pageSize: PAGE_SIZE,
      ...(viewportBounds && {
        minLat: viewportBounds.minLat,
        maxLat: viewportBounds.maxLat,
        minLng: viewportBounds.minLng,
        maxLng: viewportBounds.maxLng,
        zoom: viewportBounds.zoom,
      }),
    },
  });

  const { data: wirelessProvidersData } = useGetWirelessServiceProviders({
    enabled: options?.isAdminMode && normalizedConfig.signalCoverage,
    pathParams: {},
    queryParams: { pageSize: 1000, serviceTypes: ["Wireless"] },
  });

  const { data: generationsData } = useGetGenerations({
    enabled: options?.isAdminMode && normalizedConfig.signalCoverage,
    pathParams: {},
    queryParams: { pageSize: 100 },
  });

  const providerIdByName = useMemo(() => {
    const mapByName = new Map<string, string>();
    const rows = (wirelessProvidersData as { data?: Array<{ id: string; provider?: { name?: string } }> } | undefined)
      ?.data;
    for (const row of rows ?? []) {
      for (const token of getProviderLookupTokens(row.provider?.name)) {
        mapByName.set(token, row.id);
      }
    }
    return mapByName;
  }, [wirelessProvidersData]);

  const providerNameById = useMemo(() => {
    const mapById = new Map<string, string>();
    const rows = (wirelessProvidersData as { data?: Array<{ id: string; provider?: { name?: string } }> } | undefined)
      ?.data;
    for (const row of rows ?? []) {
      const providerName = toNonEmptyString(row.provider?.name);
      if (!providerName) continue;
      mapById.set(row.id, providerName);
    }
    return mapById;
  }, [wirelessProvidersData]);

  const generationIdByName = useMemo(() => {
    const mapByName = new Map<string, string>();
    const rows = (generationsData as { data?: Array<{ id: string; name?: string }> } | undefined)?.data;
    for (const row of rows ?? []) {
      const name = normalizeSearchToken(row.name);
      if (!name) continue;
      mapByName.set(name, row.id);
    }
    return mapByName;
  }, [generationsData]);

  const generationNameById = useMemo(() => {
    const mapById = new Map<string, string>();
    const rows = (generationsData as { data?: Array<{ id: string; name?: string }> } | undefined)?.data;
    for (const row of rows ?? []) {
      const name = toNonEmptyString(row.name);
      if (!name) continue;
      mapById.set(row.id, name);
    }
    return mapById;
  }, [generationsData]);

  // Accumulate cells from all pages
  const accumulatedCells = useMemo(() => {
    if (cellsData) {
      const infiniteData = cellsData as unknown as InfiniteData<PaginationResponse<CellViewport>>;
      return infiniteData.pages.flatMap((page) => page.data);
    }
    return [];
  }, [cellsData]);

  // Loading progress
  const loadingProgress = useMemo(() => {
    if (cellsData) {
      const infiniteData = cellsData as unknown as InfiniteData<PaginationResponse<CellViewport>>;
      const firstPage = infiniteData.pages[0];
      if (firstPage) {
        return { current: accumulatedCells.length, total: firstPage.totalRows };
      }
    }
    return { current: 0, total: 0 };
  }, [cellsData, accumulatedCells]);

  // Process cells with worker
  const handleCellsProcessed = useCallback((geojson: CellGeoJSONFeatureCollection, count: number) => {
    setAccumulatedGeoJSON(geojson);
    setTotalLoadedCount((prev) => prev + count);

    if (updateTimeoutRef.current) {
      clearTimeout(updateTimeoutRef.current);
    }

    updateTimeoutRef.current = setTimeout(() => {
      // Map will update via effect
    }, 250);
  }, []);

  const { processCells } = useCellGeoJSONWorker(handleCellsProcessed);

  // Reset accumulated data when filter options change
  useEffect(() => {
    const currentSerialized = JSON.stringify({
      generationIds: options?.generationIds,
      serviceProviderIds: options?.serviceProviderIds,
      regionIds: options?.regionIds,
    });
    if (previousOptionsRef.current !== currentSerialized) {
      setAccumulatedGeoJSON({ type: "FeatureCollection", features: [] });
      setTotalLoadedCount(0);
      previousOptionsRef.current = currentSerialized;
    }
  }, [options?.generationIds, options?.serviceProviderIds, options?.regionIds]);

  // Track viewport changes
  useEffect(() => {
    if (viewportBounds && isViewportStable) {
      const viewportChanged =
        !previousViewportRef.current ||
        previousViewportRef.current.minLat !== viewportBounds.minLat ||
        previousViewportRef.current.maxLat !== viewportBounds.maxLat ||
        previousViewportRef.current.minLng !== viewportBounds.minLng ||
        previousViewportRef.current.maxLng !== viewportBounds.maxLng;

      if (viewportChanged) {
        setTotalLoadedCount(() => 0);
        previousViewportRef.current = viewportBounds;
      }
    }
  }, [viewportBounds, isViewportStable]);

  // Process cells when new data arrives
  useEffect(() => {
    if (accumulatedCells.length > 0 && accumulatedCells.length > totalLoadedCount) {
      const newCells = accumulatedCells.slice(totalLoadedCount);
      processCells(newCells, CELL_RADIUS_KM, 6, accumulatedGeoJSON.features);
    }
  }, [accumulatedCells, processCells, totalLoadedCount, accumulatedGeoJSON.features]);

  // Auto-fetch next page
  useEffect(() => {
    if (hasNextPage && !isFetchingNextPage && isMapLoaded && isViewportStable) {
      void fetchNextPage();
    }
  }, [hasNextPage, isFetchingNextPage, fetchNextPage, isMapLoaded, isViewportStable]);

  // Show loading progress toast
  useEffect(() => {
    const now = Date.now();
    const { current, total } = loadingProgress;

    if (total === 0) return;

    const progressPercent = Math.floor((current / total) * 100);
    const timeSinceLastUpdate = now - lastToastUpdateRef.current;
    const progressChanged = Math.abs(progressPercent - lastProgressPercentRef.current) >= 10;

    if (isLoading || isFetchingNextPage) {
      if (timeSinceLastUpdate >= 1000 || progressChanged) {
        toast.loading(`Loading cells... ${progressPercent}%`, {
          id: "loading-cells",
          description: `${current.toLocaleString()} of ${total.toLocaleString()} cells loaded`,
        });
        lastToastUpdateRef.current = now;
        lastProgressPercentRef.current = progressPercent;
      }
    } else if (current > 0) {
      toast.success("Cells loaded successfully!", {
        id: "loading-cells",
        description: `${current.toLocaleString()} cells loaded`,
      });
    }
  }, [isLoading, isFetchingNextPage, loadingProgress]);

  // Load signal coverage data (admin-only) with multi-tier fallback cascade:
  // Primary: Active MapTiler backend tileset -> env fallback
  // Fallback 1: Next.js GeoJSON API (/api/signal-coverage/geojson)
  // Fallback 2: Legacy flat JSON file (/data/all_telco_band_data_flat.json or cell-signal.json)
  useEffect(() => {
    if (!options?.isAdminMode || !normalizedConfig.signalCoverage || rawSignalCoverageData || coverageSourceType) return;

    let cancelled = false;

    const loadCoverage = async () => {
      // 1. Try active MapTiler tileset from backend API first
      let resolvedCoverageUrl: string | undefined =
        process.env.NEXT_PUBLIC_MAPTILER_COVERAGE_URL || MAPTILER_SIGNAL_COVERAGE_URL;

      try {
        const regionParam = options?.regionIds?.[0] ? `?regionId=${encodeURIComponent(options.regionIds[0])}` : "";
        const res = await request.get<APIResponse<CellularCoverageTileset[]>>(
          `/cellular-coverages/tilesets/active${regionParam}`
        );

        if (res.succeeded && res.data && res.data.length > 0) {
          const readyTileset = res.data.find((t) => t.tileJsonUrl);
          if (readyTileset?.tileJsonUrl) {
            resolvedCoverageUrl = readyTileset.tileJsonUrl;
          }
        }
      } catch (err) {
        if (process.env.NODE_ENV === "development") {
          console.debug("[useMapCells] Failed to fetch active tilesets from API:", err);
        }
      }

      // If MapTiler vector tiles source is configured (from API or env)
      if (resolvedCoverageUrl) {
        if (cancelled) return;
        toast.loading("Loading radio network coverage from MapTiler...", {
          id: "loading-coverage",
          description: "Connecting to MapTiler coverage source",
        });

        setDynamicCoverageUrl(resolvedCoverageUrl);
        setCoverageSourceType("maptiler");
        toast.success("Radio network coverage loaded successfully!", {
          id: "loading-coverage",
          description: "Loaded from MapTiler source",
        });
        return;
      }

      // 2. Fallback Tier 1: Next.js GeoJSON API
      try {
        const geojsonRes = await fetch(SIGNAL_COVERAGE_GEOJSON_POC_PATH);
        if (geojsonRes.ok) {
          const rawGeoJSON = await geojsonRes.json();
          if (!cancelled) {
            setRawSignalCoverageData(rawGeoJSON);
            setCoverageSourceType("geojson");
            toast.success("Radio network coverage loaded successfully!", {
              id: "loading-coverage",
              description: "Loaded from GeoJSON source",
            });
            return;
          }
        }
      } catch (geoErr) {
        console.warn("🗺️ [Signal Coverage] Fallback Tier 1 failed:", geoErr);
      }

      // 3. Fallback Tier 2: Static flat JSON assets
      const jsonPaths = ["/data/all_telco_band_data_flat.json", "/data/cell-signal.json"];
      for (const path of jsonPaths) {
        try {
          const res = await fetch(path);
          if (!res.ok) continue;
          const data = await res.json();
          if (!Array.isArray(data) || data.length === 0) continue;

          if (!cancelled) {
            setRawSignalCoverageData(data);
            setCoverageSourceType("geojson");
            toast.success("Radio network coverage loaded successfully!", {
              id: "loading-coverage",
              description: `${data.length.toLocaleString()} coverage points loaded`,
            });
            return;
          }
        } catch (err) {
          console.warn(`Failed to load coverage fallback from ${path}:`, err);
        }
      }

      if (!cancelled) {
        toast.error("Failed to load radio network coverage", {
          id: "loading-coverage",
        });
      }
    };

    void loadCoverage();

    return () => {
      cancelled = true;
    };
  }, [
    normalizedConfig.signalCoverage,
    rawSignalCoverageData,
    coverageSourceType,
    options?.isAdminMode,
    options?.regionIds,
  ]);

  // Transform raw signal data to normalized map GeoJSON with full searchable properties.
  useEffect(() => {
    if (!options?.isAdminMode || !normalizedConfig.signalCoverage || !rawSignalCoverageData) return;

    const transformed = Array.isArray(rawSignalCoverageData)
      ? parseSignalCoverageFlatData(rawSignalCoverageData, providerIdByName, providerNameById, generationIdByName)
      : parseSignalCoverageGeoJSON(rawSignalCoverageData, providerIdByName, providerNameById, generationIdByName);

    setSignalCoverageData(transformed);
  }, [
    options?.isAdminMode,
    normalizedConfig.signalCoverage,
    rawSignalCoverageData,
    providerIdByName,
    providerNameById,
    generationIdByName,
  ]);

  useEffect(() => {
    if (!signalCoverageData) {
      setSignalCoveragePolygons(null);
      setSignalCoverageGrid(null);
      return;
    }

    const typedData = signalCoverageData as SignalCoverageFeatureCollection;
    setSignalCoveragePolygons(buildCoveragePolygonsFromPoints(typedData));
    setSignalCoverageGrid(buildInterpolatedCoverageGrid(typedData));
  }, [signalCoverageData]);

  // Helper to set layer visibility
  const setLayerVisible = useCallback(
    (layerId: string, visible: boolean) => {
      if (map?.getLayer(layerId)) {
        map.setLayoutProperty(layerId, "visibility", visible ? "visible" : "none");
      }
    },
    [map],
  );

  /**
   * Enforces proper layer ordering after style changes or concurrent layer additions.
   * Uses map.moveLayer() to ensure cell layers are positioned consistently,
   * regardless of the map style's base layer structure.
   *
   * Strategy:
   * 1. Find the highest-z "floor" layer from LAYERS_BELOW_CELLS that exists in the current style
   * 2. Find the first non-cell layer above that floor (this is our insertion anchor)
   * 3. Move all cell layers to just above the floor, before the anchor
   *
   * This ensures: [...floor layers..., signal-coverage, cell-layers, <anchor/labels>, ...]
   */
  const enforceCellLayerOrder = useCallback(() => {
    if (!map) return;

    const style = map.getStyle();
    if (!style?.layers) return;

    // Skip if no cell layers exist yet
    const cellLayerIds = [
      "signal-coverage-grid-fill",
      "signal-coverage-polygon-fill",
      "signal-coverage-polygon-border",
      "signal-coverage-circles",
      "cells-heatmap",
      "cells-point-view",
      "cells-clusters",
      "cells-cluster-count",
      "cells-circles",
      "cells-symbols",
    ];

    if (!cellLayerIds.some((id) => map.getLayer(id))) return;

    const layerIds = style.layers.map((l) => l.id).filter((id): id is string => id !== undefined);

    // Phase 1: Find the highest-z "floor" layer from known base map layers
    let floorIdx = -1;
    for (const id of LAYERS_BELOW_CELLS) {
      const idx = layerIds.indexOf(id as string);
      if (idx > floorIdx) floorIdx = idx;
    }

    let anchorId: string | undefined;

    if (floorIdx !== -1) {
      // Walk forward from just above the floor layer to find the first non-cell layer.
      // This is the slot where cell layers will be inserted.
      for (let i = floorIdx + 1; i < layerIds.length; i++) {
        const id = layerIds[i];
        if (id && !cellLayerIds.includes(id)) {
          anchorId = id;
          break;
        }
      }
    }

    // Phase 2 fallback: find the topmost non-cell symbol layer (typically place labels)
    if (!anchorId) {
      for (let i = style.layers.length - 1; i >= 0; i--) {
        const layer = style.layers[i];
        if (layer && layer.type === "symbol" && !cellLayerIds.includes(layer.id)) {
          anchorId = layer.id;
          break;
        }
      }
    }

    if (!anchorId) return; // No suitable anchor found

    if (process.env.NODE_ENV === "development") {
      console.log("🗺️ [Cells] Enforcing layer order before", anchorId);
    }

    // Move cell layers in order (signal coverage first, then others)
    // This ensures proper z-order: signal-coverage below all other cells
    const moveOrder = [
      "signal-coverage-grid-fill",
      "signal-coverage-polygon-fill",
      "signal-coverage-polygon-border",
      "signal-coverage-circles",
      "cells-heatmap",
      "cells-point-view",
      "cells-clusters",
      "cells-cluster-count",
      "cells-circles",
      "cells-symbols",
    ] as const;

    for (const layerId of moveOrder) {
      if (!map.getLayer(layerId)) continue;
      try {
        map.moveLayer(layerId, anchorId);
      } catch (err) {
        // Layer may not be ready yet; will be retried on the next scheduled call
        if (process.env.NODE_ENV === "development") {
          console.debug(`[useMapCells] moveLayer(${layerId}) failed, will retry:`, err);
        }
      }
    }
  }, [map]);

  // Main effect: Create and manage map layers
  useEffect(() => {
    if (!map || !isMapLoaded) return;

    // Reset cleanup flag at the start of the effect lifecycle
    isCleaningUpRef.current = false;

    // Create popup instance for signal coverage
    const popup = new Popup({ closeButton: true, closeOnClick: true, maxWidth: "320px" });

    // Add or update cells source (clustered)
    const addCellsSource = () => {
      if (!map.getSource("cells")) {
        map.addSource("cells", {
          type: "geojson",
          // Use ref so initial data is current without needing accumulatedGeoJSON in effect deps
          data: accumulatedGeoJSONRef.current,
          cluster: true,
          clusterMaxZoom: CLUSTER_MAX_ZOOM,
          clusterRadius: CLUSTER_RADIUS,
        });
      }
      // Data updates are handled by a dedicated effect to avoid layer rebuilding
    };

    // Add non-clustered source for heatmap and point view (admin-only)
    const addCellsHeatmapSource = () => {
      if (options?.isAdminMode) {
        if (!map.getSource("cells-heatmap-source")) {
          map.addSource("cells-heatmap-source", {
            type: "geojson",
            // Use ref so initial data is current without needing accumulatedGeoJSON in effect deps
            data: accumulatedGeoJSONRef.current,
            cluster: false,
          });
        }
        // Data updates are handled by a dedicated effect to avoid layer rebuilding
      }
    };

    // Add signal coverage source (admin-only)
    const addSignalCoverageSource = () => {
      if (!options?.isAdminMode) return;

      try {
        const maptilerCoverageUrl = dynamicCoverageUrl || process.env.NEXT_PUBLIC_MAPTILER_COVERAGE_URL;
        if (coverageSourceType === "maptiler" && maptilerCoverageUrl) {
          if (!map.getSource("signal-coverage-source")) {
            map.addSource("signal-coverage-source", {
              type: "vector",
              url: maptilerCoverageUrl,
            });
          }
        } else if (signalCoverageData) {
          if (!map.getSource("signal-coverage-source")) {
            map.addSource("signal-coverage-source", {
              type: "geojson",
              data: signalCoverageData,
            });
          } else {
            const source = map.getSource<GeoJSONSource>("signal-coverage-source");
            if (source && "setData" in source) {
              source.setData(signalCoverageData);
            }
          }
        }
      } catch (err) {
        if (process.env.NODE_ENV === "development") {
          console.debug("[useMapCells] addSignalCoverageSource error:", err);
        }
      }

      if (options?.isAdminMode && normalizedConfig.signalCoverage && signalCoveragePolygons) {
        if (!map.getSource("signal-coverage-polygon-source")) {
          map.addSource("signal-coverage-polygon-source", { type: "geojson", data: signalCoveragePolygons });
        } else {
          const polygonSource = map.getSource<GeoJSONSource>("signal-coverage-polygon-source");
          if (polygonSource) {
            polygonSource.setData(signalCoveragePolygons);
          }
        }
      }

      if (options?.isAdminMode && normalizedConfig.signalCoverage && signalCoverageGrid) {
        if (!map.getSource("signal-coverage-grid-source")) {
          map.addSource("signal-coverage-grid-source", { type: "geojson", data: signalCoverageGrid });
        } else {
          const gridSource = map.getSource<GeoJSONSource>("signal-coverage-grid-source");
          if (gridSource) {
            gridSource.setData(signalCoverageGrid);
          }
        }
      }
    };

    // Load provider icons
    const loadIcons = async () => {
      await loadProviderIcons(map, iconsLoadedRef.current);
    };

    // Add cell layers
    // Find the appropriate layer to insert custom layers before it
    // We want custom layers ABOVE base map layers but BELOW place labels
    // Strategy: Look for place/settlement/city labels or fallback to any label layer
    const findCellBeforeLayer = () => {
      const layers = map.getStyle().layers;
      if (!layers) return undefined;

      // First, try to find a place name or settlement label layer
      // These typically come after roads/buildings in MapTiler styles (including outdoor)
      for (const layer of layers) {
        if (layer.type === "symbol") {
          const layerId = layer.id.toLowerCase();
          if (layerId.includes("place") || layerId.includes("settlement") || layerId.includes("city") || layerId.includes("town")) {
            return layer.id;
          }
        }
      }

      // Fallback: find any label layer
      for (const layer of layers) {
        if (layer.type === "symbol" && layer.id.toLowerCase().includes("label")) {
          return layer.id;
        }
      }

      return undefined; // Will render on top if no suitable layer found
    };

    const addCellLayers = () => {
      const beforeId = findCellBeforeLayer();

      if (process.env.NODE_ENV === "development") {
        console.log("🗺️ [Cells] Adding layers", beforeId ? `before "${beforeId}"` : "on top");
      }

      // Cluster circles
      if (!map.getLayer("cells-clusters")) {
        map.addLayer({
          id: "cells-clusters",
          type: "circle",
          source: "cells",
          filter: ["has", "point_count"],
          paint: getClusterLayerPaint(),
          layout: {
            visibility: "none", // Start hidden, controlled by visibility effect
          },
        }, beforeId);
      }

      // Cluster count labels
      if (!map.getLayer("cells-cluster-count")) {
        map.addLayer({
          id: "cells-cluster-count",
          type: "symbol",
          source: "cells",
          filter: ["has", "point_count"],
          layout: {
            "text-field": "{point_count_abbreviated}",
            "text-font": ["Open Sans Bold"],
            "text-size": 11,
            visibility: "none", // Start hidden, controlled by visibility effect
          },
          paint: { "text-color": "#ffffff" },
        }, beforeId);
      }

      // Individual cell circles (fallback)
      if (!map.getLayer("cells-circles")) {
        map.addLayer({
          id: "cells-circles",
          type: "circle",
          source: "cells",
          filter: ["!has", "point_count"],
          paint: getCellCircleLayerPaint(),
          layout: {
            visibility: "none", // Start hidden, controlled by visibility effect
          },
        }, beforeId);
      }

      // Cell symbols with provider icons
      if (!map.getLayer("cells-symbols")) {
        map.addLayer({
          id: "cells-symbols",
          type: "symbol",
          source: "cells",
          filter: ["!has", "point_count"],
          layout: {
            "icon-image": ["get", "providerIcon"],
            "icon-size": CELL_ICON_SIZE,
            "icon-allow-overlap": true,
            "icon-ignore-placement": true,
            visibility: "none", // Start hidden, controlled by visibility effect
            "icon-anchor": "bottom",
          },
        }, beforeId);
      }
    };

    // Add admin-only layers
    const addAdminLayers = () => {
      if (!options?.isAdminMode) return;

      const beforeId = findCellBeforeLayer();

      // Cell site heatmap - always add if in admin mode, control visibility separately
      if (!map.getLayer("cells-heatmap")) {
        map.addLayer({
          id: "cells-heatmap",
          type: "heatmap",
          source: "cells-heatmap-source",
          maxzoom: 15,
          paint: getCellSiteHeatmapPaint(0.7),
          layout: {
            visibility: "none", // Start hidden, controlled by applyVisibility
          },
        }, beforeId);
      }

      // Point view layer - always add if in admin mode, control visibility separately
      if (!map.getLayer("cells-point-view")) {
        map.addLayer({
          id: "cells-point-view",
          type: "circle",
          source: "cells-heatmap-source",
          paint: getPointViewCirclePaint(),
          layout: {
            visibility: "none", // Start hidden, controlled by applyVisibility
          },
        }, beforeId);
      }

      // Signal coverage layer - always add if in admin mode, control visibility separately
      // Insert BELOW all cellsite layers (before cells-clusters, the lowest cellsite layer)
      const initialSignalVis = normalizedConfigRef.current.signalCoverage ? "visible" : "none";

      // Signal coverage layers - only add if their source exists in the style
      if (map.getSource("signal-coverage-polygon-source") && !map.getLayer("signal-coverage-polygon-fill")) {
        const polygonBeforeId = map.getLayer("signal-coverage-circles")
          ? "signal-coverage-circles"
          : map.getLayer("cells-clusters")
            ? "cells-clusters"
            : beforeId;

        map.addLayer(
          {
            id: "signal-coverage-polygon-fill",
            type: "fill",
            source: "signal-coverage-polygon-source",
            paint: {
              "fill-color": ["coalesce", ["get", "fillColor"], "#1a9850"],
              "fill-opacity": 0,
            },
            layout: {
              visibility: initialSignalVis,
            },
          },
          polygonBeforeId,
        );
      }

      if (map.getSource("signal-coverage-polygon-source") && !map.getLayer("signal-coverage-polygon-border")) {
        const borderBeforeId = map.getLayer("signal-coverage-circles")
          ? "signal-coverage-circles"
          : map.getLayer("cells-clusters")
            ? "cells-clusters"
            : beforeId;

        map.addLayer(
          {
            id: "signal-coverage-polygon-border",
            type: "line",
            source: "signal-coverage-polygon-source",
            paint: {
              "line-color": "#60a5fa",
              "line-opacity": 0.45,
              "line-width": 1.2,
              "line-dasharray": [4, 3],
            },
            layout: {
              visibility: initialSignalVis,
            },
          },
          borderBeforeId,
        );
      }

      if (map.getSource("signal-coverage-grid-source") && !map.getLayer("signal-coverage-grid-fill")) {
        const gridBeforeId = map.getLayer("signal-coverage-polygon-border")
          ? "signal-coverage-polygon-border"
          : map.getLayer("signal-coverage-circles")
            ? "signal-coverage-circles"
            : beforeId;

        map.addLayer(
          {
            id: "signal-coverage-grid-fill",
            type: "fill",
            source: "signal-coverage-grid-source",
            paint: {
              "fill-color": ["coalesce", ["get", "fillColor"], "#1a9850"],
              "fill-opacity": 0.2,
            },
            layout: {
              visibility: initialSignalVis,
            },
          },
          gridBeforeId,
        );
      }

      if (map.getSource("signal-coverage-source") && !map.getLayer("signal-coverage-circles")) {
        // addCellLayers() always runs synchronously before addAdminLayers() in the
        // same effect body, so cells-clusters should reliably be present here.
        // Fallback keeps the layer below any currently-available cell visual layer.
        const signalBeforeId = map.getLayer("cells-clusters")
          ? "cells-clusters"
          : map.getLayer("cells-circles")
            ? "cells-circles"
            : map.getLayer("cells-symbols")
              ? "cells-symbols"
              : beforeId;

        const coverageLayerSpec: any = {
          id: "signal-coverage-circles",
          type: "circle",
          source: "signal-coverage-source",
          paint: getSignalCoverageCirclePaint(0.8, options?.configuredSignalLevels),
          layout: {
            visibility: initialSignalVis,
          },
        };

        if (coverageSourceType === "maptiler") {
          coverageLayerSpec["source-layer"] =
            process.env.NEXT_PUBLIC_MAPTILER_COVERAGE_SOURCE_LAYER_ID ?? "coverage";
        }

        map.addLayer(coverageLayerSpec, signalBeforeId);
      }
    };

    // Click handler for cell ID selection (for public and admin)
    const handleCellClick = (e: any) => {
      const features = map.queryRenderedFeatures(e.point, { layers: ["cells-symbols", "cells-circles"] });
      if (features && features.length > 0) {
        const feature = features.find((f: any) => f.layer.id === "cells-symbols" || f.layer.id === "cells-circles");
        if (!feature) return;

        const id = feature.properties?.id as string;
        if (!id) return;

        // Update URL with cellId
        const searchParams = new URLSearchParams(window.location.search);
        searchParams.set("cellId", id);
        window.history.replaceState({}, "", `?${searchParams.toString()}`);
      }
    };

    // Hover & click handlers for signal coverage points (admin-only)
    const handleSignalCoverageMouseMove = (e: any) => {
      if (!options?.isAdminMode) return;
      const features = map.queryRenderedFeatures(e.point, { layers: ["signal-coverage-circles"] });
      if (features && features.length > 0) {
        const feature = features[0];
        if (!feature) return;
        map.getCanvas().style.cursor = "pointer";
        const properties = feature.properties as any;

        if (properties) {
          const coordinates = (feature.geometry as any).coordinates.slice();

          // Ensure the popup appears over the correct feature
          while (Math.abs(e.lngLat.lng - coordinates[0]) > 180) {
            coordinates[0] += e.lngLat.lng > coordinates[0] ? 360 : -360;
          }

          popup
            .setLngLat(coordinates)
            .setHTML(createSignalCoveragePopupHTML(properties, configuredSignalLevelsRef.current))
            .addTo(map);
        }
      }
    };

    const handleSignalCoverageGridClick = (e: any) => {
      if (!options?.isAdminMode) return;
      const features = map.queryRenderedFeatures(e.point, { layers: ["signal-coverage-grid-fill"] });
      if (features && features.length > 0) {
        const feature = features[0];
        if (!feature) return;

        const properties = feature.properties as { strength?: number; barangay?: string; azimuth?: string };
        if (!properties) return;

        popup
          .setLngLat(e.lngLat)
          .setHTML(createSignalCoveragePopupHTML(properties as any, configuredSignalLevelsRef.current))
          .addTo(map);
      }
    };

    const handleSignalCoverageInterpolatedMapClick = (e: any) => {
      if (!options?.isAdminMode || !normalizedConfig.signalCoverage || !signalCoverageData) return;

      const hasDirectHit = map.queryRenderedFeatures(e.point, {
        layers: ["signal-coverage-grid-fill", "signal-coverage-circles"],
      });
      if (hasDirectHit.length > 0) return;

      const signalPoints = (signalCoverageData as SignalCoverageFeatureCollection).features;
      if (signalPoints.length === 0) return;

      const interpolated = estimateIDWStrength(e.lngLat.lng, e.lngLat.lat, signalPoints, 8, 2);
      if (interpolated === undefined || !Number.isFinite(interpolated)) return;

      const nearest = signalPoints
        .map((feature) => {
          const flng = feature.geometry.coordinates[0];
          const flat = feature.geometry.coordinates[1];
          if (flng === undefined || flat === undefined) return { feature, distance2: Infinity };
          const dx = flng - e.lngLat.lng;
          const dy = flat - e.lngLat.lat;
          return { feature, distance2: dx * dx + dy * dy };
        })
        .sort((a, b) => a.distance2 - b.distance2)[0]?.feature;

      const properties = {
        ...(nearest?.properties ?? {}),
        strength: Number(interpolated.toFixed(1)),
        interpolation_method: "IDW",
        interpolation_note: "interpolated",
      };

      popup
        .setLngLat(e.lngLat)
        .setHTML(createSignalCoveragePopupHTML(properties as any, configuredSignalLevelsRef.current))
        .addTo(map);
    };

    // Mouse enter/leave handlers for signal coverage (admin-only)
    const handleSignalCoverageEnter = () => {
      if (options?.isAdminMode) {
        map.getCanvas().style.cursor = "pointer";
      }
    };
    const handleSignalCoverageLeave = () => {
      if (options?.isAdminMode) {
        map.getCanvas().style.cursor = "";
        popup.remove();
      }
    };

    // Helper to remove all cell-related layers safely
    const removeCellLayers = () => {
      const layersToRemove = [
        "cells-clusters",
        "cells-cluster-count",
        "cells-circles",
        "cells-symbols",
        "cells-heatmap",
        "cells-point-view",
        "signal-coverage-polygon-fill",
        "signal-coverage-polygon-border",
        "signal-coverage-grid-fill",
        "signal-coverage-circles",
      ];

      for (const layerId of layersToRemove) {
        if (map.getLayer(layerId)) {
          map.removeLayer(layerId);
        }
      }
    };

    // Helper to remove all cell-related sources safely
    const removeCellSources = () => {
      const sourcesToRemove = [
        "cells",
        "cells-heatmap-source",
        "signal-coverage-source",
        "signal-coverage-polygon-source",
        "signal-coverage-grid-source",
      ];

      for (const sourceId of sourcesToRemove) {
        if (map.getSource(sourceId)) {
          map.removeSource(sourceId);
        }
      }
    };

    // Initialize
    void loadIcons();
    addCellsSource();
    addCellsHeatmapSource();
    addSignalCoverageSource();
    addCellLayers();
    addAdminLayers();

    // Enforce proper layer ordering after all layers are added
    // Use deferred setTimeout to ensure all concurrent layer additions complete first
    setTimeout(() => enforceCellLayerOrder(), 0);

    // Apply initial visibility immediately after adding layers
    const applyInitialVisibility = () => {
      const cellSitesVisible = normalizedConfig.cellSites && !normalizedConfig.pointView;

      setLayerVisible("cells-clusters", cellSitesVisible);
      setLayerVisible("cells-cluster-count", cellSitesVisible);
      setLayerVisible("cells-circles", cellSitesVisible);
      setLayerVisible("cells-symbols", cellSitesVisible);

      if (options?.isAdminMode) {
        setLayerVisible("cells-heatmap", normalizedConfig.cellSiteHeatmap);

        if (normalizedConfig.pointView) {
          setLayerVisible("cells-point-view", true);
          setLayerVisible("cells-clusters", false);
          setLayerVisible("cells-cluster-count", false);
          setLayerVisible("cells-circles", false);
          setLayerVisible("cells-symbols", false);
        } else {
          setLayerVisible("cells-point-view", false);
        }

        setLayerVisible("signal-coverage-circles", normalizedConfig.signalCoverage);
        setLayerVisible("signal-coverage-polygon-fill", normalizedConfig.signalCoverage);
        setLayerVisible("signal-coverage-polygon-border", normalizedConfig.signalCoverage);
        setLayerVisible("signal-coverage-grid-fill", normalizedConfig.signalCoverage);
      }
    };

    // Apply visibility after layers are added
    applyInitialVisibility();

    // Attach click handlers
    map.on("click", "cells-circles", handleCellClick);
    map.on("click", "cells-symbols", handleCellClick);

    // Attach signal coverage handlers (admin-only)
    if (options?.isAdminMode) {
      map.on("click", "signal-coverage-circles", handleSignalCoverageMouseMove);
      map.on("mousemove", "signal-coverage-circles", handleSignalCoverageMouseMove);
      map.on("click", "signal-coverage-grid-fill", handleSignalCoverageGridClick);
      map.on("click", handleSignalCoverageInterpolatedMapClick);
      map.on("mouseenter", "signal-coverage-circles", handleSignalCoverageEnter);
      map.on("mouseleave", "signal-coverage-circles", handleSignalCoverageLeave);
      map.on("mouseenter", "signal-coverage-grid-fill", handleSignalCoverageEnter);
      map.on("mouseleave", "signal-coverage-grid-fill", handleSignalCoverageLeave);
    }

    // Handle style changes
    const handleStyleData = () => {
      if (isCleaningUpRef.current) return;
      void loadIcons();
      addCellsSource();
      addCellsHeatmapSource();
      addSignalCoverageSource();
    };

    const handleSourceData = () => {
      if (isCleaningUpRef.current) return;
      // Detect whether layers are about to be newly added (style-change recovery).
      // cells-clusters is the first cell layer added; if it is absent the whole
      // layer set is being re-created and we must re-apply the current visibility
      // after the add, because the layers start with visibility:"none".
      const layersWereMissing = !map.getLayer("cells-clusters");
      addCellLayers();
      addAdminLayers();
      if (layersWereMissing && map.getLayer("cells-clusters")) {
        // Re-apply the current visibility from the always-current ref so that
        // switching map styles doesn't leave every cell layer hidden.
        const cfg = normalizedConfigRef.current;
        const cellSitesVisible = cfg.cellSites && !cfg.pointView;
        setLayerVisible("cells-clusters", cellSitesVisible);
        setLayerVisible("cells-cluster-count", cellSitesVisible);
        setLayerVisible("cells-circles", cellSitesVisible);
        setLayerVisible("cells-symbols", cellSitesVisible);
        if (options?.isAdminMode) {
          setLayerVisible("cells-heatmap", cfg.cellSiteHeatmap);
          if (cfg.pointView) {
            setLayerVisible("cells-point-view", true);
            setLayerVisible("cells-clusters", false);
            setLayerVisible("cells-cluster-count", false);
            setLayerVisible("cells-circles", false);
            setLayerVisible("cells-symbols", false);
          } else {
            setLayerVisible("cells-point-view", false);
          }
          setLayerVisible("signal-coverage-circles", cfg.signalCoverage);
          setLayerVisible("signal-coverage-polygon-fill", cfg.signalCoverage);
          setLayerVisible("signal-coverage-polygon-border", cfg.signalCoverage);
          setLayerVisible("signal-coverage-grid-fill", cfg.signalCoverage);
        }

        // Enforce proper layer ordering after re-adding layers on style change
        setTimeout(() => enforceCellLayerOrder(), 0);
      }
    };

    map.on("styledata", handleStyleData);
    map.on("sourcedata", handleSourceData);

    // Store handler references for cleanup
    handleStyleDataRef.current = handleStyleData;
    handleSourceDataRef.current = handleSourceData;

    return () => {
      // Prevent handlers from running during teardown
      isCleaningUpRef.current = true;

      // Remove event listeners first
      map.off("click", "cells-circles", handleCellClick);
      map.off("click", "cells-symbols", handleCellClick);

      if (options?.isAdminMode) {
        map.off("click", "signal-coverage-circles", handleSignalCoverageMouseMove);
        map.off("mousemove", "signal-coverage-circles", handleSignalCoverageMouseMove);
        map.off("click", "signal-coverage-grid-fill", handleSignalCoverageGridClick);
        map.off("click", handleSignalCoverageInterpolatedMapClick);
        map.off("mouseenter", "signal-coverage-circles", handleSignalCoverageEnter);
        map.off("mouseleave", "signal-coverage-circles", handleSignalCoverageLeave);
        map.off("mouseenter", "signal-coverage-grid-fill", handleSignalCoverageEnter);
        map.off("mouseleave", "signal-coverage-grid-fill", handleSignalCoverageLeave);
      }

      if (handleStyleDataRef.current) {
        map.off("styledata", handleStyleDataRef.current);
      }
      if (handleSourceDataRef.current) {
        map.off("sourcedata", handleSourceDataRef.current);
      }

      // Clean up popup
      popup.remove();

      // Remove layers BEFORE removing sources to avoid "source in use" errors
      removeCellLayers();
      removeCellSources();
    };
    // accumulatedGeoJSON intentionally omitted from deps; updates handled by dedicated effect below
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [
    map,
    isMapLoaded,
    signalCoverageData,
    coverageSourceType,
    dynamicCoverageUrl,
    signalCoveragePolygons,
    signalCoverageGrid,
    options?.isAdminMode,
    setLayerVisible,
  ]);

  // Dedicated effect to update source data whenever accumulatedGeoJSON changes.
  // This calls source.setData() directly WITHOUT removing/re-adding layers, preventing
  // the visible blank flash that would occur if accumulatedGeoJSON were in the main effect's deps.
  useEffect(() => {
    if (!map || !isMapLoaded) return;

    const cellsSource = map.getSource<GeoJSONSource>("cells");
    if (cellsSource) {
      cellsSource.setData(accumulatedGeoJSON);
    }

    if (options?.isAdminMode) {
      const heatmapSource = map.getSource<GeoJSONSource>("cells-heatmap-source");
      if (heatmapSource) {
        heatmapSource.setData(accumulatedGeoJSON);
      }
    }
  }, [map, isMapLoaded, accumulatedGeoJSON, options?.isAdminMode]);

  // Separate effect for visibility control - only runs when config changes
  useEffect(() => {
    if (!map || !isMapLoaded) return;

    const cellSitesVisible = normalizedConfig.cellSites && !normalizedConfig.pointView;

    setLayerVisible("cells-clusters", cellSitesVisible);
    setLayerVisible("cells-cluster-count", cellSitesVisible);
    setLayerVisible("cells-circles", cellSitesVisible);
    setLayerVisible("cells-symbols", cellSitesVisible);

    if (options?.isAdminMode) {
      // Admin-only layer visibility - just toggle, don't remove/add
      setLayerVisible("cells-heatmap", normalizedConfig.cellSiteHeatmap);

      if (normalizedConfig.pointView) {
        setLayerVisible("cells-point-view", true);
        // Hide clusters in point view mode
        setLayerVisible("cells-clusters", false);
        setLayerVisible("cells-cluster-count", false);
        setLayerVisible("cells-circles", false);
        setLayerVisible("cells-symbols", false);
      } else {
        setLayerVisible("cells-point-view", false);
      }

      // Signal coverage - toggle independently
      setLayerVisible("signal-coverage-circles", normalizedConfig.signalCoverage);
      setLayerVisible("signal-coverage-polygon-fill", normalizedConfig.signalCoverage);
      setLayerVisible("signal-coverage-polygon-border", normalizedConfig.signalCoverage);
      setLayerVisible("signal-coverage-grid-fill", normalizedConfig.signalCoverage);
    }
  }, [map, isMapLoaded, normalizedConfig, options?.isAdminMode, setLayerVisible]);

  // Apply signal strength filter to the Radio Network Coverage layer (admin-only)
  // Uses MapTiler's GPU-accelerated filter expressions for efficient large-dataset filtering
  const signalStrengthMin = options?.signalStrengthFilter?.min;
  const signalStrengthMax = options?.signalStrengthFilter?.max;
  const signalLevelRanges = options?.signalLevelRanges ?? [];
  const signalCoverageAreaStudyName = options?.signalCoverageAreaStudyName?.trim().toLowerCase();
  const signalSearchQuery = options?.searchQuery?.trim().toLowerCase();

  useEffect(() => {
    if (!map || !isMapLoaded || !options?.isAdminMode) return;
    if (!map.getLayer("signal-coverage-circles")) return;

    const strengthExpr: any = ["coalesce", ["get", "strength"], ["get", "signal_strength_dbm"]];
    const filterExpression: any[] = ["all"];

    const hasStrengthFilter =
      signalStrengthMin !== undefined && signalStrengthMax !== undefined &&
      !(signalStrengthMin <= -120 && signalStrengthMax >= -80);

    if (signalLevelRanges.length > 0) {
      const rangeExpression: any[] = ["any"];

      for (const range of signalLevelRanges) {
        const min = typeof range.min === "number" ? range.min : undefined;
        const max = typeof range.max === "number" ? range.max : undefined;

        if (min === undefined && max === undefined) continue;

        const singleRange: any[] = ["all"];
        if (min !== undefined) {
          singleRange.push([">=", strengthExpr, min]);
        }
        if (max !== undefined) {
          // Half-open interval [min, max): use '<' for intermediate ranges to prevent boundary overlap,
          // and '<=' for the top level (or when max >= 0) to ensure the highest range is fully closed.
          if (range.isTop || max >= 0) {
            singleRange.push(["<=", strengthExpr, max]);
          } else {
            singleRange.push(["<", strengthExpr, max]);
          }
        }

        rangeExpression.push(singleRange);
      }

      if (rangeExpression.length > 1) {
        filterExpression.push(rangeExpression);
      }
    } else if (hasStrengthFilter) {
      filterExpression.push([">=", strengthExpr, signalStrengthMin]);
      // The top 3GPP bucket is open-ended (Excellent: >= -80 dBm),
      // so we skip an upper bound when the slider handle is at -80.
      if (signalStrengthMax < -80) {
        filterExpression.push(["<=", strengthExpr, signalStrengthMax]);
      }
    }

    if (signalCoverageAreaStudyName) {
      filterExpression.push([
        "==",
        ["downcase", ["coalesce", ["get", "areaStudy"], ["get", "source_file"], ""]],
        signalCoverageAreaStudyName,
      ]);
    }

    if (options.serviceProviderIds && options.serviceProviderIds.length > 0) {
      const selectedNames = options.serviceProviderIds
        .map((id) => providerNameById.get(id)?.toLowerCase())
        .filter((n): n is string => Boolean(n));

      if (selectedNames.length > 0) {
        filterExpression.push([
          "any",
          ["in", ["coalesce", ["get", "networkProviderId"], ""], ["literal", options.serviceProviderIds]],
          ["in", ["downcase", ["coalesce", ["get", "network"], ["get", "provider"], ""]], ["literal", selectedNames]],
        ]);
      } else {
        filterExpression.push(["in", ["coalesce", ["get", "networkProviderId"], ""], ["literal", options.serviceProviderIds]]);
      }
    }

    if (options.generationIds && options.generationIds.length > 0) {
      const selectedGenNames = options.generationIds
        .map((id) => generationNameById.get(id)?.toLowerCase())
        .filter((n): n is string => Boolean(n));

      if (selectedGenNames.length > 0) {
        filterExpression.push([
          "any",
          ["in", ["coalesce", ["get", "generationId"], ""], ["literal", options.generationIds]],
          ["in", ["downcase", ["coalesce", ["get", "technology"], ""]], ["literal", selectedGenNames]],
        ]);
      } else {
        filterExpression.push(["in", ["coalesce", ["get", "generationId"], ""], ["literal", options.generationIds]]);
      }
    }

    if (signalSearchQuery) {
      const searchTokens = signalSearchQuery.split(/\s+/).map((t) => t.trim()).filter(Boolean);
      if (searchTokens.length > 0) {
        const searchTarget: any = [
          "concat",
          ["downcase", ["to-string", ["coalesce", ["get", "search_text"], ""]]], " ",
          ["downcase", ["to-string", ["coalesce", ["get", "band"], ""]]], " ",
          ["downcase", ["to-string", ["coalesce", ["get", "network"], ""]]], " ",
          ["downcase", ["to-string", ["coalesce", ["get", "technology"], ""]]], " ",
          ["downcase", ["to-string", ["coalesce", ["get", "provider"], ""]]], " ",
          ["downcase", ["to-string", ["coalesce", ["get", "duplex_mode"], ""]]], " ",
          ["downcase", ["to-string", ["coalesce", ["get", "frequency"], ""]]], " ",
          ["downcase", ["to-string", ["coalesce", ["get", "location"], ""]]], " ",
          ["downcase", ["to-string", ["coalesce", ["get", "barangay"], ""]]], " ",
          ["downcase", ["to-string", ["coalesce", ["get", "address"], ""]]], " ",
          ["downcase", ["to-string", ["coalesce", ["get", "cell_id"], ""]]], " ",
          ["downcase", ["to-string", ["coalesce", ["get", "cellId"], ""]]], " ",
          ["downcase", ["to-string", ["coalesce", ["get", "areaStudy"], ""]]], " ",
          ["downcase", ["to-string", ["coalesce", ["get", "source_file"], ""]]], " ",
          ["downcase", ["to-string", ["coalesce", ["get", "city"], ""]]], " ",
          ["downcase", ["to-string", ["coalesce", ["get", "municipality"], ""]]], " ",
          ["downcase", ["to-string", ["coalesce", ["get", "province"], ""]]], " ",
          ["downcase", ["to-string", ["coalesce", ["get", "region"], ""]]], " ",
          ["downcase", ["to-string", ["coalesce", ["get", "site_id"], ""]]], " ",
          ["downcase", ["to-string", ["coalesce", ["get", "site_name"], ""]]], " ",
          ["downcase", ["to-string", ["coalesce", ["get", "strength"], ["get", "signal_strength_dbm"], ""]]],
        ];

        for (const token of searchTokens) {
          filterExpression.push([
            ">=",
            ["index-of", token, searchTarget],
            0,
          ]);
        }
      }
    }

    const applyFilter = (layerId: string, filter: any) => {
      if (map.getLayer(layerId)) {
        map.setFilter(layerId, filter);
      }
    };

    if (filterExpression.length === 1) {
      applyFilter("signal-coverage-circles", null);
      applyFilter("signal-coverage-polygon-fill", null);
      applyFilter("signal-coverage-polygon-border", null);
      applyFilter("signal-coverage-grid-fill", null);
      return;
    }

    applyFilter("signal-coverage-circles", filterExpression as any);
    applyFilter("signal-coverage-polygon-fill", filterExpression as any);
    applyFilter("signal-coverage-polygon-border", filterExpression as any);
    applyFilter("signal-coverage-grid-fill", filterExpression as any);
  }, [
    map,
    isMapLoaded,
    options?.isAdminMode,
    options?.serviceProviderIds,
    options?.generationIds,
    signalLevelRanges,
    signalCoverageAreaStudyName,
    signalStrengthMin,
    signalStrengthMax,
    signalSearchQuery,
    providerNameById,
    generationNameById,
  ]);

  // Dynamically update signal coverage circle colors when configured signal levels change
  useEffect(() => {
    if (!map || !isMapLoaded || !options?.isAdminMode) return;
    if (!map.getLayer("signal-coverage-circles")) return;

    const levels = options?.configuredSignalLevels?.length
      ? options.configuredSignalLevels
      : options?.signalLevelRanges?.map((r) => ({
          thresholdFrom: r.min,
          thresholdTo: r.max,
          color: r.color ?? "#6b7280",
          isTop: r.isTop,
        }));

    const colorPaint = getSignalCoverageCircleColorPaint(levels);
    map.setPaintProperty("signal-coverage-circles", "circle-color", colorPaint);
  }, [map, isMapLoaded, options?.isAdminMode, options?.configuredSignalLevels, options?.signalLevelRanges]);

  // Re-enforce cell layer ordering after any map style or source change.
  // This ensures cell layers stay in the correct z-order across all map styles,
  // even when switching between styles with different base layer structures.
  useEffect(() => {
    if (!map || !isMapLoaded) return;

    let pendingEnforce: ReturnType<typeof setTimeout> | null = null;

    const scheduleEnforcement = () => {
      // Cancel any already-scheduled enforcement to debounce rapid-fire events
      if (pendingEnforce !== null) clearTimeout(pendingEnforce);
      pendingEnforce = setTimeout(enforceCellLayerOrder, 0);
    };

    map.on("styledata", scheduleEnforcement);
    map.on("sourcedata", scheduleEnforcement);

    return () => {
      if (pendingEnforce !== null) clearTimeout(pendingEnforce);
      map.off("styledata", scheduleEnforcement);
      map.off("sourcedata", scheduleEnforcement);
    };
  }, [map, isMapLoaded, enforceCellLayerOrder]);

  return { cells: accumulatedCells, setLayerVisible };
};
