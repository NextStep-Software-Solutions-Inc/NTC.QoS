// components/NetworkStatusCard.tsx
import { useMemo } from "react";

import { Badge } from "@/components/ui/badge";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { type SpeedTest } from "../types/SpeedTest";

type GroupedProvider = { color?: string; sites: SpeedTest[] };

interface SpeedtestCardProps {
  data?: SpeedTest[];
}

export default function NewSpeedtestsCard({ data }: SpeedtestCardProps) {
  const groupedSpeedtestProviders = useMemo(() => {
    const groupedProviders: Record<string, GroupedProvider> = {};
    data?.forEach((site) => {
      const providerName = site.serviceProvider.provider.name || "Unknown";
      const providerColor = site.serviceProvider.provider.color;
      groupedProviders[providerName] ??= { color: providerColor, sites: [] };
      groupedProviders[providerName]?.sites.push(site);
    });
    return groupedProviders;
  }, [data]);

  return (
    <div>
      {Object.entries(groupedSpeedtestProviders).map(([providerName, { color, sites }]) => (
        <Card key={providerName} className=" w-full mx-auto">
          <CardHeader>
            <div className="flex items-center space-x-4">
              <div className="rounded-full bg-gray-200 text-gray-600 w-10 h-10 flex items-center justify-center font-semibold">
                {providerName[0]}
              </div>
              <div>
                <CardTitle
                  style={{ backgroundColor: color }}
                  className="text-base font-semibold text-white py-1 px-3 rounded-full">
                  {providerName}
                </CardTitle>
                <p className="text-sm text-gray-500">Service Provider</p>
              </div>
            </div>
          </CardHeader>
          <CardContent>
            <table className="w-full text-sm mt-2">
              <thead>
                <tr className="text-left text-gray-500 border-b">
                  <th className="py-2">Technology</th>
                  <th className="py-2">Status</th>
                  <th className="py-2">Bands</th>
                </tr>
              </thead>
              <tbody>
                {sites.map((site, index) => (
                  <tr key={index} className="border-b last:border-0">
                    <td className="py-3">{site.phoneModel}</td>
                    <td className="py-3">
                      <Badge variant="outline" className={`rounded-full px-4 py-1 text-sm font-medium `}>
                        {site.signalQuality}
                      </Badge>
                    </td>
                    <td className="py-3">{site.jitter}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </CardContent>
        </Card>
      ))}
    </div>
  );
}
