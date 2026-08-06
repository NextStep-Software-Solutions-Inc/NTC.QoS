"use client";

import { useMemo } from "react";
import Link from "next/link";
import { useParams, useSearchParams } from "next/navigation";

import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { useDebounceContext } from "@/hooks/useDebounceContext";
import { type Provider } from "../../provider/types/Provider";
import { type Technology } from "../../technology/types/Technology";
import { DropdownEditSpeedtestProvider } from "../dropdown-menu/DropDownEditSpeedtestProvider";
import { type SpeedTest } from "../types/SpeedTest";

interface SpeedTestResultProps {
  data?: SpeedTest[];
}
export default function ProvidersSpeedTestResult({ data }: SpeedTestResultProps) {
  const { debounceValue } = useDebounceContext();
  const searchParams = useSearchParams();
  const municipalId = searchParams.get("municipalId");
  const barangayId = searchParams.get("barangayId");
  const filteredData = useMemo(() => {
    if (!debounceValue) return data;

    return data?.filter(
      (speedtest) =>
        speedtest?.serviceProvider?.provider.name.toLowerCase().includes(debounceValue.toLowerCase()) ||
        speedtest?.address?.toLowerCase().includes(debounceValue.toLowerCase()),
    );
  }, [data, debounceValue]);

  // grouped speedtests by provider by combining average DL UL Ping Signal Signal Strength RSSi
  const groupedSpeedTestsByProvider = useMemo(() => {
    const groups: Record<
      string,
      {
        color?: string;
        downloadSpeed: number[];
        uploadSpeed: number[];
        ping: number[];
        signalStrength: string[];
        rssi: number[];
        sites: SpeedTest[];
        address: string[];
        technologies: Technology[];
        generation: string[];
        provider: Provider[];
        providerId: string;
      }
    > = {};

    filteredData?.forEach((speedtest) => {
      const providerName = speedtest?.serviceProvider?.provider?.name || "Unknown Provider";
      const providerColor = speedtest?.serviceProvider?.provider?.color;
      const providerId = speedtest?.serviceProvider?.provider?.id;
      // Initialize group if it doesn't exist
      groups[providerName] ??= {
        color: providerColor,
        downloadSpeed: [],
        uploadSpeed: [],
        ping: [],
        signalStrength: [],
        rssi: [],
        sites: [],
        address: [],
        technologies: [],
        generation: [],
        provider: [],
        providerId: providerId,
      };

      // Push values to their respective arrays
      groups[providerName].downloadSpeed.push(speedtest.downloadSpeed);
      groups[providerName].uploadSpeed.push(speedtest.uploadSpeed);
      groups[providerName].ping.push(speedtest.ping);
      groups[providerName].signalStrength.push(speedtest.signalQuality);
      groups[providerName].rssi.push(speedtest.rssi);
      groups[providerName].sites.push(speedtest);
      groups[providerName].address.push(speedtest.address ?? "unknown");
      // groups[providerName].generation.push(speedtest.generation.name);
      groups[providerName].technologies.push(speedtest?.technology);
      groups[providerName].provider.push(speedtest?.serviceProvider?.provider);
      // groups[providerName].providerId.push(speedtest?.serviceProvider?.provider?.id);
    });

    return groups;
  }, [filteredData]);
  const { region } = useParams<{ region?: string }>();
  return (
    <div className=" w-full">
      {/* group speedtests by service provider */}
      {Object.entries(groupedSpeedTestsByProvider).map(([providerName, providerGroup]) => {
        // Calculate the average download speed
        const avgDownload = providerGroup.downloadSpeed.length
          ? providerGroup.downloadSpeed.reduce((sum, speed) => sum + speed, 0) / providerGroup.downloadSpeed.length
          : 0;

        // Calculate the average upload speed
        const avgUpload = providerGroup.uploadSpeed.length
          ? providerGroup.uploadSpeed.reduce((sum, speed) => sum + speed, 0) / providerGroup.uploadSpeed.length
          : 0;

        // Calculate the average ping
        const avgPing = providerGroup.ping.length
          ? providerGroup.ping.reduce((sum, p) => sum + p, 0) / providerGroup.ping.length
          : 0;

        // Calculate the average RSSI
        const avgRssi = providerGroup.rssi
          ? providerGroup.rssi.reduce((sum, r) => sum + r, 0) / providerGroup.rssi.length
          : 0;

        const maxDownloadSpeed = providerGroup.downloadSpeed
          ? providerGroup.downloadSpeed.reduce((max, d) => Math.max(max, d), 0)
          : 0;

        const maxUploadSpeed = providerGroup.uploadSpeed
          ? providerGroup.uploadSpeed.reduce((max, u) => Math.max(max, u), 0)
          : 0;

        const signalQualityMap: Record<string, number> = { VeryWeak: 1, Weak: 2, Good: 3, Excellent: 4 };

        // Calculate the numerical average from the string array
        const totalSignalQualityValue = providerGroup.signalStrength.reduce(
          (sum, quality) => sum + (signalQualityMap[quality] ?? 0),
          0,
        );
        // const avgSignalQualityValue =
        //   providerGroup.signalStrength.length > 0 ? totalSignalQualityValue / providerGroup.signalStrength.length : 0;
        const avgSignalQualityValue =
          providerGroup.signalStrength.length > 0 ? totalSignalQualityValue / providerGroup.signalStrength.length : 0;

        const avgSignalNumeric = avgSignalQualityValue;
        const avgSignalQuality =
          avgSignalQualityValue === null
            ? "Unknown"
            : avgSignalQualityValue < 1.5
              ? "Very Weak"
              : avgSignalQualityValue < 2.5
                ? "Weak"
                : avgSignalQualityValue < 3.5
                  ? "Average"
                  : avgSignalQualityValue <= 4.5
                    ? "Good"
                    : "Excellent";

        const uniqueTechnologies = [...new Set(providerGroup?.technologies)];
        const queryParams = new URLSearchParams();

        // Always add providerId
        if (providerGroup?.providerId) {
          queryParams.append("providerId", providerGroup.providerId);
        }
        if (municipalId) {
          queryParams.append("municipalId", municipalId);
        }
        if (barangayId) {
          queryParams.append("brgyId", barangayId);
        }

        const showChartPath = `/${region}/speedtest-details/chart?${queryParams.toString()}`;
        return (
          <Card key={providerName} className="w-full p-3 my-4">
            <div className="w-full flex justify-between">
              <div className="flex items-center gap-2">
                <div className="w-8 h-8 rounded-full bg-muted flex items-center justify-center">
                  <span style={{ color: providerGroup.color }}>{providerName.charAt(0)}</span>
                </div>
                <div>
                  <Badge variant="default" style={{ backgroundColor: providerGroup.color }}>
                    {providerName}
                  </Badge>
                  {/* <p className="text-sm text-muted-foreground">Service Provider</p> */}
                </div>
              </div>
              <DropdownEditSpeedtestProvider />
            </div>

            {/* Display aggregated data for the provider group */}
            <CardContent className="space-y-4 p-2">
              <div className="grid grid-cols-1 sm:grid-cols-4 gap-4">
                <MetricBox label="Avg. Download" value={avgDownload} unit="Mbps" />
                <MetricBox label="Avg. Upload" value={avgUpload} unit="Mbps" />
                <MetricBox label="Avg. Ping" value={avgPing} unit="ms" />
                {/* You'll need to adapt SignalBox to take an average signal value */}
                <SignalBox value={avgSignalNumeric} />
              </div>

              <div className="flex flex-col sm:flex-row justify-between text-sm text-muted-foreground gap-2">
                <div className="space-y-3 mt-2">
                  <p>
                    Total Test Locations: <strong className="text-foreground">{providerGroup.address.length}</strong>
                  </p>
                  <p>
                    Best download: <strong className="text-foreground">{maxDownloadSpeed.toFixed(2)} Mbps</strong>
                  </p>
                  <p>
                    Best upload: <strong className="text-foreground">{maxUploadSpeed.toFixed(2)} Mbps</strong>
                  </p>
                </div>
                <div className="space-y-3 mt-2">
                  <p>
                    Avg signal strength <strong className="text-foreground">{avgSignalQuality}</strong>
                  </p>
                  <p>
                    Avg RSSI: <strong className="text-foreground">{avgRssi}</strong> <span>dBm</span>
                  </p>
                  <span className="inline-flex items-center gap-2">
                    Network Technologies:
                    {uniqueTechnologies?.map((technology) => {
                      return <Badge key={technology?.id}>{technology?.standard ?? "unverified"}</Badge>;
                    })}
                  </span>
                </div>
                <div>{/* Additional info can go here */}</div>
              </div>
              <Link href={showChartPath}>
                <Button variant="outline" size="sm" className="w-full mt-4">
                  Show Chart
                </Button>
              </Link>
            </CardContent>
          </Card>
        );
      })}
    </div>
  );
}

function MetricBox({ label, value, unit }: { label: string; value: number; unit: string }) {
  return (
    <div className="border rounded-lg p-3">
      <p className="text-sm text-muted-foreground">{label}</p>
      <p className="text-xl font-semibold">
        {value.toFixed(2)} <span className="text-sm text-muted-foreground">{unit}</span>
      </p>
    </div>
  );
}
function SignalBox({ value }: { value: number }) {
  const rounded = Math.round(value);

  const getBarColor = (i: number): string => {
    if (rounded === 0) return "#d1d5db";
    if (i >= rounded) return "#d1d5db";

    switch (rounded) {
      case 1:
        return "#dc2626"; // red-600: Very Weak
      case 2:
        return "#f97316"; // orange-500: Weak
      case 3:
        return "#eab308"; // yellow-500: Average
      case 4:
        return "#84cc16"; // lime-500: Good
      case 5:
        return "#22c55e"; // green-500: Excellent
      default:
        return "#d1d5db";
    }
  };

  return (
    <div className="border rounded-lg p-3">
      <p className="text-sm text-muted-foreground">Avg Signal</p>
      <div className="flex gap-1 mt-2">
        {Array.from({ length: 5 }).map((_, i) => (
          <div key={i} className="w-2 h-4 rounded-sm" style={{ backgroundColor: getBarColor(i) }}></div>
        ))}
      </div>
    </div>
  );
}
