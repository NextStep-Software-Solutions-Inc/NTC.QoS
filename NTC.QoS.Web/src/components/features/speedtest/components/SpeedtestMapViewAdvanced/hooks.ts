import { useMemo } from "react";

import { type SpeedTest } from "@/components/features/speedtest/types/SpeedTest";
import { DEFAULT_PROVIDER_COLOR, NBC_COLOR, SIGNAL_QUALITY_COLORS, UNKNOWN_PROVIDER } from "./constants";
import { type ProviderCounts, type SignalQualityCounts, type SpeedtestWithMeta } from "./types";
import { hasValidCoordinates } from "./utils";

/**
 * Hook to filter speedtests by search query and visible providers
 */
export function useFilteredSpeedtests(
  speedtests: SpeedtestWithMeta[],
  visibleProviders: Set<string>,
  visibleSignalQualities: Set<string>,
  _searchQuery: string,
  startDate?: string,
  endDate?: string,
): SpeedtestWithMeta[] {
  return useMemo(() => {
    const startMs = startDate ? new Date(startDate).getTime() : undefined;
    const endMs = endDate ? new Date(endDate).getTime() : undefined;

    return speedtests.filter((st) => {
      const provider = st.serviceProvider?.provider?.name || UNKNOWN_PROVIDER;
      const signalQuality = st.signalQuality || "Unknown";

      // Check provider visibility
      if (!visibleProviders.has(provider)) return false;

      // Check signal quality visibility
      if (!visibleSignalQualities.has(signalQuality)) return false;

      // Check date range
      if (startMs !== undefined || endMs !== undefined) {
        if (!st.dateTime) return false;
        const testMs = new Date(st.dateTime).getTime();
        if (isNaN(testMs)) return false;
        if (startMs !== undefined && testMs < startMs) return false;
        if (endMs !== undefined && testMs > endMs) return false;
      }

      return true;
    });
  }, [speedtests, visibleProviders, visibleSignalQualities, startDate, endDate]);
}

/**
 * Hook to generate GeoJSON from speedtests
 */
export function useSpeedtestsGeoJSON(
  filteredSpeedtests: SpeedtestWithMeta[],
  selectedSpeedtestId?: string | null,
): GeoJSON.FeatureCollection {
  return useMemo(() => {
    return {
      type: "FeatureCollection" as const,
      features: filteredSpeedtests
        .filter(hasValidCoordinates)
        .map((st) => ({
          type: "Feature" as const,
          geometry: { type: "Point" as const, coordinates: [st.longitude, st.latitude] as [number, number] },
          properties: {
            id: st.id,
            downloadSpeed: st.downloadSpeed || 0,
            uploadSpeed: st.uploadSpeed || 0,
            ping: st.ping || 0,
            signalQuality: st.signalQuality || "Unknown",
            signalQualityColor: SIGNAL_QUALITY_COLORS[st.signalQuality || "Unknown"] || SIGNAL_QUALITY_COLORS.Unknown,
            provider: st.serviceProvider?.provider?.name || UNKNOWN_PROVIDER,
            providerColor: st.isNoBroadbandCoverage
              ? NBC_COLOR
              : (st.serviceProvider?.provider?.color || DEFAULT_PROVIDER_COLOR),
            lat: st.latitude,
            lng: st.longitude,
            isSelected: st.id === selectedSpeedtestId,
            isPending: st.isPending || false,
            hasError: st.hasError || false,
            isNoBroadbandCoverage: st.isNoBroadbandCoverage || false,
          },
        })),
    };
  }, [filteredSpeedtests, selectedSpeedtestId]);
}

/**
 * Hook to count speedtests by provider
 */
export function useProviderCounts(speedtests: SpeedtestWithMeta[], visibleProviders: Set<string>): ProviderCounts {
  return useMemo(() => {
    const counts: ProviderCounts = {};
    speedtests.forEach((st) => {
      const providerName = st.serviceProvider?.provider?.name || UNKNOWN_PROVIDER;
      const color = st.serviceProvider?.provider?.color || DEFAULT_PROVIDER_COLOR;
      if (!counts[providerName]) {
        counts[providerName] = {
          count: 0,
          color,
          visible: visibleProviders.has(providerName),
          avgDownload: 0,
          avgUpload: 0,
        };
      }
      counts[providerName].count++;
      counts[providerName].avgDownload += st.downloadSpeed || 0;
      counts[providerName].avgUpload += st.uploadSpeed || 0;
    });
    // Calculate averages
    Object.keys(counts).forEach((provider) => {
      const providerData = counts[provider];
      if (providerData && providerData.count > 0) {
        providerData.avgDownload /= providerData.count;
        providerData.avgUpload /= providerData.count;
      }
    });
    return counts;
  }, [speedtests, visibleProviders]);
}

/**
 * Hook to count speedtests by signal quality
 */
export function useSignalQualityCounts(
  speedtests: SpeedtestWithMeta[],
  visibleSignalQualities: Set<string>,
): SignalQualityCounts {
  return useMemo(() => {
    const counts: SignalQualityCounts = {};
    speedtests.forEach((st) => {
      const quality = st.signalQuality || "Unknown";
      const color = SIGNAL_QUALITY_COLORS[quality] || SIGNAL_QUALITY_COLORS.Unknown || "#6b7280";
      if (!counts[quality]) {
        counts[quality] = { count: 0, color, visible: visibleSignalQualities.has(quality) };
      }
      const qualityData = counts[quality];
      if (qualityData) {
        qualityData.count++;
      }
    });
    return counts;
  }, [speedtests, visibleSignalQualities]);
}

/**
 * Hook to calculate statistics
 */
export function useSpeedtestStats(speedtests: SpeedtestWithMeta[]) {
  return useMemo(() => {
    if (speedtests.length === 0) {
      return { avgDownload: 0, avgUpload: 0, avgPing: 0, total: 0 };
    }
    const totals = speedtests.reduce(
      (acc, st) => ({
        download: acc.download + (st.downloadSpeed || 0),
        upload: acc.upload + (st.uploadSpeed || 0),
        ping: acc.ping + (st.ping || 0),
      }),
      { download: 0, upload: 0, ping: 0 },
    );
    return {
      avgDownload: totals.download / speedtests.length,
      avgUpload: totals.upload / speedtests.length,
      avgPing: totals.ping / speedtests.length,
      total: speedtests.length,
    };
  }, [speedtests]);
}
