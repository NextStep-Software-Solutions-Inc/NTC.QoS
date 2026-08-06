"use client";

import { useMemo } from "react";
import { useParams, useRouter, useSearchParams } from "next/navigation";
import { Activity, ArrowUpRight, ChevronLeft, MoveDownRight, Wifi } from "lucide-react";
import { CartesianGrid, Line, LineChart, ResponsiveContainer, Tooltip, XAxis, YAxis } from "recharts";

import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";
import { getBarangayDetailsRequest } from "../features/barangay/handlers/getBarangayDetailsRequest";
import { getMunicipalityRequest } from "../features/municipality/handlers/getMunicipalityRequest";
import { getSpeedtestsRequest } from "../features/speedtest/handlers/getSpeedtestsRequest";
import { type SpeedTest } from "../features/speedtest/types/SpeedTest";
import { SpeedtestSkeleton } from "../SpeedtestSkeleton";
import { Badge } from "../ui/badge";
import { Button } from "../ui/button";
import WithPlaceholder from "../WithPlaceholder";
import SpeedtestMonthlyPerformance from "./SpeedtestMonthlyPerformance";
import SpeedTestStats from "./SpeedTestStats";

const useBarangay = createQueryHookFromTemplate(getBarangayDetailsRequest);
const useSpeedtests = createQueryHookFromTemplate(getSpeedtestsRequest);
const useMunicipal = createQueryHookFromTemplate(getMunicipalityRequest);

type SignalQuality = "Unknown" | "VeryWeak" | "Weak" | "Average" | "Good" | "Excellent";
// Optional: Map signal quality to numeric values for averaging
const signalQualityMap: Record<SignalQuality, number> = {
  Unknown: 0,
  VeryWeak: 1,
  Weak: 2,
  Average: 3,
  Good: 4,
  Excellent: 5,
};
function analyzeData(data: SpeedTest[]) {
  let totalDownload = 0;
  let totalUpload = 0;
  let totalPing = 0;
  let totalSignal = 0;
  let totalJitter = 0;

  let minDownload = Infinity;
  let maxDownload = -Infinity;
  let minUpload = Infinity;
  let maxUpload = -Infinity;

  for (const item of data) {
    // Sums
    totalDownload += item.downloadSpeed;
    totalUpload += item.uploadSpeed;
    totalPing += item.ping;
    totalSignal += signalQualityMap[item.signalQuality];
    totalJitter += item.jitter;

    // Min/Max
    if (item.downloadSpeed < minDownload) minDownload = item.downloadSpeed;
    if (item.downloadSpeed > maxDownload) maxDownload = item.downloadSpeed;
    if (item.uploadSpeed < minUpload) minUpload = item.uploadSpeed;
    if (item.uploadSpeed > maxUpload) maxUpload = item.uploadSpeed;
  }

  const count = data.length || 1;

  return {
    avgDownload: totalDownload / count,
    avgUpload: totalUpload / count,
    minDownload,
    maxDownload,
    minUpload,
    maxUpload,
    avgLatency: totalPing / count,
    avgSignalStrength: totalSignal / count, // numeric scale
    avgJitter: totalJitter / count,
  };
}

// Custom X-Axis tick component to format the date
const CustomXAxisTick = ({ x, y, payload }: { x?: number; y?: number; payload?: { value: Date } }) => {
  if (!payload?.value) {
    return null;
  }

  const formattedDate = payload.value.toLocaleString("default", { month: "short" });

  return (
    <g transform={`translate(${x},${y})`}>
      <text x={0} y={0} dy={16} textAnchor="middle" fill="#666" fontSize={12}>
        {formattedDate}
      </text>
    </g>
  );
};

// A custom Y-axis tick component to format the label
const CustomYAxisTick = ({ x, y, payload }: { x?: number; y?: number; payload?: { value: number } }) => {
  return (
    <g transform={`translate(${x},${y})`}>
      <text x={0} y={0} dy={5} textAnchor="end" fill="#666" fontSize={12}>
        {`${payload?.value} Mbps`}
      </text>
    </g>
  );
};

export default function SpeedtestChart() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const { region } = useParams<{ region: string }>();
  const municipalIds = searchParams.getAll("municipalId");
  const brgyIds = searchParams.getAll("brgyId");
  const providerIds = searchParams.getAll("providerId") ?? "";

  const { data: barangayDetails, isLoading: isLoadingBarangay } = useBarangay({
    pathParams: { id: brgyIds[0]! },
    queryParams: { pageSize: 1, pageNumber: 1 },
    enabled: !!brgyIds[0],
  });
  const { data: municipalDetails, isLoading: isLoadingMunicipal } = useMunicipal({
    pathParams: { id: municipalIds[0] ?? "" },
    queryParams: { pageSize: 1, pageNumber: 1 },
    enabled: !!municipalIds[0],
  });
  const { data: speedtests, isLoading: isLoadingSpeedtest } = useSpeedtests({
    pathParams: {},
    queryParams: {
      pageSize: 100000,
      pageNumber: 1,
      regionIds: [region],
      municipalityIds: municipalIds,
      barangayIds: brgyIds,
      providerIds: providerIds,
    },
  });

  const analyzedOutput = useMemo(() => analyzeData(speedtests?.data ?? []), [speedtests?.data]);

  const transformedData = speedtests?.data?.length
    ? speedtests?.data
        .map((speedtest) => ({
          date: new Date(speedtest.dateTime),
          download: speedtest.downloadSpeed,
          upload: speedtest.uploadSpeed,
        }))
        .sort((a, b) => a.date.getTime() - b.date.getTime())
    : [];

  const municipalAddress = `${municipalDetails?.data?.name}, ${municipalDetails?.data?.province?.name}`;
  const barangayAddress = `${barangayDetails?.data?.name}, ${barangayDetails?.data?.municipality?.name} ${barangayDetails?.data?.municipality?.province?.name}`;
  const locationAddress = brgyIds[0] ? barangayAddress : municipalAddress;
  const providerNames = new Set([
    ...(speedtests?.data ?? []).map((speedtest) => speedtest.serviceProvider.provider.name),
  ]);
  const isLoading = isLoadingBarangay || isLoadingMunicipal || isLoadingSpeedtest;
  return (
    <WithPlaceholder isLoading={isLoading} placeholder={<SpeedtestSkeleton />}>
      <div className="border p-5 rounded-lg">
        <div className="grid items-center sm:grid-cols-2 mb-1 gap-5">
          <section className=" sm:col-span-1 flex items-start gap-2">
            <Button variant="outline" size="icon" className="w-10 h-10" onClick={() => router.back()}>
              <ChevronLeft />
            </Button>
            <div className="">
              <h2 className="font-semibold">Speed Test Trends</h2>
              <div>{locationAddress}</div>
              <div>
                Providers:{" "}
                {[...providerNames].map((provider) => (
                  <Badge key={provider} className="mx-1">
                    {provider}
                  </Badge>
                ))}
              </div>
              <div className="w-fit">
                {/* <p className="text-muted-foreground text-[13px]">{`${providerDetails?.serviceProvider?.provider?.description}.- June 01-30`}</p>{" "} */}
              </div>
            </div>
          </section>
        </div>
        {/* #TODO separate */}
        <CardHeader className=" w-full grid sm:grid-cols-2 gap-4">
          <SpeedTestStats
            icon={<MoveDownRight className="w-4 h-4" />}
            label={`Avg Download`}
            value={`${analyzedOutput?.avgDownload.toFixed(2)} Mbps`}
            range={`Range: ${analyzedOutput?.minDownload} - ${analyzedOutput?.maxDownload} Mbps`}
          />
          <SpeedTestStats
            icon={<ArrowUpRight className="w-4 h-4" />}
            label="Avg Upload"
            value={`${analyzedOutput.avgUpload.toFixed(2)} Mbps`}
            range={`Range: ${analyzedOutput.minUpload} - ${analyzedOutput.maxUpload} Mbps`}
          />
          <SpeedTestStats
            icon={<Activity className="w-4 h-4" />}
            label="Average Latency"
            value={`${analyzedOutput?.avgLatency.toFixed(2)} ms`}
            range="Good response time"
          />
          <SpeedTestStats
            icon={<Wifi className="w-4 h-4" />}
            label="Network Stability"
            value={`${analyzedOutput?.avgJitter.toFixed(2)} ms`}
            range="Excellent jitter"
          />
        </CardHeader>
        <Card className="w-full mx-auto">
          <CardHeader>
            <CardTitle>Speed Trends Over Time</CardTitle>
            <CardDescription>Monthly download and upload speed measurements (January - July 2025)</CardDescription>
          </CardHeader>
          {/* #TODO separate */}
          <CardContent className="h-[300px]">
            <ResponsiveContainer width="100%" height="100%">
              <LineChart data={transformedData} margin={{ top: 20, right: 30, left: 10, bottom: 10 }}>
                <CartesianGrid vertical={false} />
                <XAxis dataKey={"date"} padding={{ left: 10, right: 0 }} tick={<CustomXAxisTick />} />
                <YAxis domain={[0, 200]} tick={<CustomYAxisTick />} />
                <Tooltip formatter={(value: number) => `${value} Mbps`} />
                <Line
                  type="monotone"
                  dataKey="download"
                  stroke="#3b82f6"
                  strokeWidth={3}
                  dot={{ r: 5 }}
                  name="Download"
                />
                <Line type="monotone" dataKey="upload" stroke="#ef4444" strokeWidth={3} dot={{ r: 5 }} name="Upload" />
              </LineChart>
            </ResponsiveContainer>
          </CardContent>
        </Card>
        <SpeedtestMonthlyPerformance data={transformedData} />
      </div>
    </WithPlaceholder>
  );
}
