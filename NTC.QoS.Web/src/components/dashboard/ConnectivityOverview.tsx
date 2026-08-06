"use client";

import * as React from "react";
import { useParams, useRouter, useSearchParams } from "next/navigation";
import { Label, Pie, PieChart, Sector } from "recharts";
import { type PieSectorDataItem } from "recharts/types/polar/Pie";

import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { ChartContainer, ChartStyle, ChartTooltip, ChartTooltipContent, type ChartConfig } from "@/components/ui/chart";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";
import { getCellsRequest } from "../features/cell/handlers/getCellsRequest";

const useCells = createQueryHookFromTemplate(getCellsRequest);

export default function ConnectivityOverview() {
  const searchParams = useSearchParams();
  const router = useRouter();

  const { region } = useParams<{ region: string }>();
  const { data: cells } = useCells({
    pathParams: {},
    queryParams: { regionIds: region ? [region] : undefined, pageSize: 100000, pageNumber: 1 },
  });

  const wirelessProviders = React.useMemo(() => {
    if (!cells?.data) return [];

    const filtered = cells.data.filter((cell) => cell?.serviceProvider?.service?.type === "Wireless");

    const uniqueMap = new Map<string, { key: string; label: string; color: string }>();
    filtered.forEach((cell) => {
      const provider = cell.serviceProvider?.provider;
      if (provider?.id) {
        const key = provider.name.toLowerCase().replace(/\s+/g, "-");
        if (!uniqueMap.has(provider.id)) {
          uniqueMap.set(provider.id, { key, label: provider.name, color: provider.color || "#999999" });
        }
      }
    });

    return Array.from(uniqueMap.values());
  }, [cells]);

  const chartConfig = React.useMemo(
    () =>
      wirelessProviders.reduce((acc, provider) => {
        acc[provider.key] = { label: provider.label, color: provider.color };
        return acc;
      }, {} as ChartConfig),
    [wirelessProviders],
  );

  const providerData = React.useMemo(
    () =>
      wirelessProviders.map((provider) => ({
        provider: provider.key,
        users:
          cells?.data?.filter(
            (cell) =>
              cell.serviceProvider?.service?.type === "Wireless" &&
              cell.serviceProvider?.provider?.name === provider.label,
          ).length ?? 0,
        fill: provider.color,
      })),
    [wirelessProviders, cells],
  );

  const initialProvider = searchParams.get("provider") ?? providerData[0]?.provider ?? "";

  const [activeProvider, setActiveProvider] = React.useState(initialProvider);

  React.useEffect(() => {
    const providerFromUrl = searchParams.get("provider");

    if (providerFromUrl) {
      if (providerFromUrl !== activeProvider) {
        setActiveProvider(providerFromUrl);
      }
    } else if (!activeProvider && providerData.length > 0) {
      const firstProvider = providerData[0]?.provider;
      if (firstProvider) {
        setActiveProvider(firstProvider);

        const params = new URLSearchParams(Array.from(searchParams.entries()));
        params.set("provider", firstProvider);
        router.replace(`?${params.toString()}`);
      }
    }
  }, [searchParams, providerData, activeProvider, router]);

  const handleProviderChange = (provider: string) => {
    const params = new URLSearchParams(Array.from(searchParams.entries()));
    params.set("provider", provider);
    router.replace(`?${params.toString()}`);
    setActiveProvider(provider);
  };

  const id = "connectivity-pie";
  const activeIndex = providerData.findIndex((item) => item.provider === activeProvider);

  return (
    <Card className="col-span-1 sm:col-span-2 md:col-span-2">
      <ChartStyle id={id} config={chartConfig} />
      <CardHeader className="flex flex-row items-center justify-between">
        <div>
          <CardTitle className="text-[17px] font-semibold">Connectivity Overview</CardTitle>
          <p className="text-sm text-gray-500">The distribution of service coverage by provider.</p>
        </div>

        <Select value={activeProvider} onValueChange={handleProviderChange}>
          <SelectTrigger className="ml-auto h-7 w-[130px] rounded-lg pl-2.5">
            <SelectValue placeholder="Select provider" />
          </SelectTrigger>
          <SelectContent align="end" className="rounded-xl">
            {wirelessProviders.map((item) => {
              const config = chartConfig[item.key];
              return (
                <SelectItem key={item.key} value={item.key}>
                  <div className="flex items-center gap-2 text-xs">
                    <span className="flex h-3 w-3 shrink-0 rounded-sm" style={{ backgroundColor: config?.color }} />
                    {config?.label}
                  </div>
                </SelectItem>
              );
            })}
          </SelectContent>
        </Select>
      </CardHeader>

      <CardContent className="flex flex-col items-center justify-center p-4">
        <ChartContainer id={id} config={chartConfig} className="mx-auto aspect-square w-full max-w-[300px]">
          <PieChart>
            <ChartTooltip cursor={false} content={<ChartTooltipContent hideLabel />} />
            <Pie
              data={providerData}
              dataKey="users"
              nameKey="provider"
              innerRadius={60}
              strokeWidth={5}
              activeIndex={activeIndex}
              activeShape={({ outerRadius = 0, ...props }: PieSectorDataItem) => (
                <g>
                  <Sector {...props} outerRadius={outerRadius + 10} />
                  <Sector {...props} outerRadius={outerRadius + 25} innerRadius={outerRadius + 12} />
                </g>
              )}>
              <Label
                content={({ viewBox }) => {
                  if (viewBox && "cx" in viewBox && "cy" in viewBox) {
                    const activePayloadIndex = providerData[activeIndex];
                    const users = activePayloadIndex?.users;
                    if (users === undefined) {
                      return null;
                    }

                    return (
                      <text x={viewBox.cx} y={viewBox.cy} textAnchor="middle" dominantBaseline="middle">
                        <tspan className="fill-foreground text-2xl font-bold">{users}</tspan>
                      </text>
                    );
                  }
                  return null;
                }}
              />
            </Pie>
          </PieChart>
        </ChartContainer>

        {/* Legend below the chart */}
        <div className="flex flex-wrap justify-center gap-4 pt-4">
          {providerData.map((item) => (
            <div key={item.provider} className="flex items-center gap-2 text-sm font-medium">
              <span className="block h-4 w-4 rounded-full" style={{ backgroundColor: item.fill }}></span>
              <p>{chartConfig[item.provider]?.label}</p>
            </div>
          ))}
        </div>
      </CardContent>
    </Card>
  );
}
