"use client ";

import React, { useMemo } from "react";
import { ArrowUpDown, Radio } from "lucide-react";

import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";
import { getSpeedtestsRequest } from "../features/speedtest/handlers/getSpeedtestsRequest";
import ViewISPDialog from "./ViewISPDialog";

interface InternetServiceDetailsProps {
  lat: number;
  lng: number;
  service: { id: string };
}

const useSpeedtests = createQueryHookFromTemplate(getSpeedtestsRequest);

export default function InternetServiceDetails({ lat, lng, service }: InternetServiceDetailsProps) {
  const { data: speedtests } = useSpeedtests({
    pathParams: {},
    queryParams: {
      pageNumber: 1,
      pageSize: 100,
      latitude: lat,
      longitude: lng,
      distance: 100,
      serviceIds: [service.id],
    },
  });

  const stats = useMemo(() => {
    if (!speedtests?.data?.length) {
      return {
        providers: new Map<
          string,
          { name: string; id: string; color: string; uploadSpeed: number; downloadSpeed: number; rssi: number }
        >(),
        maxSpeed: 0,
        averageStrength: "Unknown",
      };
    }

    const result = speedtests.data.reduce(
      (acc, data) => {
        // Add provider to the map using `id` as the key (or `name` if preferred)
        const providerId = data.serviceProvider.provider.id;
        const providerName = data.serviceProvider.provider.name;

        if (providerId || providerName) {
          acc.providers.set(providerId || providerName, {
            name: providerName,
            id: providerId,
            color: data.serviceProvider.provider.color,
            uploadSpeed: data.uploadSpeed ?? 0,
            downloadSpeed: data.downloadSpeed ?? 0,
            rssi: data.rssi ?? 0,
          });
        }

        // Calculate max speed
        const uploadSpeed = data.uploadSpeed ?? 0;
        const downloadSpeed = data.downloadSpeed ?? 0;
        const currentMax = Math.max(uploadSpeed, downloadSpeed);
        acc.maxSpeed = Math.max(acc.maxSpeed, currentMax);

        // Accumulate RSSI for average strength calculation
        acc.totalRSSI += data.rssi ?? 0;
        acc.count += 1;

        return acc;
      },
      {
        providers: new Map<
          string,
          { name: string; id: string; color: string; uploadSpeed: number; downloadSpeed: number; rssi: number }
        >(),
        maxSpeed: 0,
        totalRSSI: 0,
        count: 0,
      },
    );

    // Calculate average strength
    const averageRSSI = result.totalRSSI / result.count;
    const averageStrength = averageRSSI > -50 ? "Strong" : averageRSSI > -70 ? "Moderate" : "Weak";

    return { providers: result.providers, maxSpeed: result.maxSpeed, averageStrength, averageRSSI };
  }, [speedtests]);

  return (
    <div className="mb-2 mt-4 grid justify-center text-gray-700">
      <div className="grid pt-2 space-y-2">
        <p className="text-xs text-gray-500">
          Total ISPs Available:
          <span className="ml-1 text-xs font-bold text-primary">{stats.providers.size}</span>
        </p>
        <p className="mt-1 flex items-center gap-1 text-xs">
          <ArrowUpDown size={16} className="" />
          Speed: Up to <span className="font-bold text-primary">{stats.maxSpeed} Mbps</span>
        </p>
        <p className="mt-1 flex items-center gap-1 text-xs">
          <Radio size={16} className="" />
          Coverage:{" "}
          <span className="font-bold text-primary">
            {stats.averageRSSI?.toFixed(0)} dBm - {stats.averageStrength}
          </span>
        </p>
      </div>
      <ViewISPDialog providers={Array.from(stats.providers.values())} />
    </div>
  );
}
