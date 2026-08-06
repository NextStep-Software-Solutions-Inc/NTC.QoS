"use client";

import React, { useMemo } from "react";

import { Badge } from "@/components/ui/badge";

interface ProviderItem {
  serviceProvider: { provider: { name: string; color: string } };
}

interface TopThreeProvidersProps {
  data?: ProviderItem[]; // Any list that has the same provider shape
}

const TopThreeProviders: React.FC<TopThreeProvidersProps> = ({ data }) => {
  const topBadges = useMemo(() => {
    if (!data || data.length === 0) {
      return <p className="text-sm">No ISP available</p>;
    }

    const providerCountMap = data.reduce(
      (acc, item) => {
        const name = item.serviceProvider.provider.name;
        const color = item.serviceProvider.provider.color;

        acc[name] ??= { count: 0, color };
        acc[name].count += 1;

        return acc;
      },
      {} as Record<string, { count: number; color: string }>,
    );

    const top3 = Object.entries(providerCountMap)
      .sort((a, b) => b[1].count - a[1].count)
      .slice(0, 3);

    return top3.map(([name, { color }]) => {
      const firstWord = name.split(" ")[0];
      return (
        <Badge key={name} style={{ backgroundColor: color }}>
          {firstWord}
        </Badge>
      );
    });
  }, [data]);

  return <div className="flex gap-1">{topBadges}</div>;
};

export default TopThreeProviders;
