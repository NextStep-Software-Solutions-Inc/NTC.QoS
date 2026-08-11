"use client";

import { useMemo, useState } from "react";
import { Workflow } from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardFooter, CardHeader } from "@/components/ui/card";
import { ScrollArea } from "@/components/ui/scroll-area";
import { DropdownEditNAPsProvider } from "../../NAP/dropdown-menu/DropDownEditNAPsProvider";
import { type NAP } from "../../NAP/types/NAP";

interface BarangayNameWithNAPProps {
  data?: NAP[];
}

interface NAPsItemProps {
  color?: string;
  locations: NAP[];
}

export default function CardBrgyNAPs({ data }: BarangayNameWithNAPProps) {
  const [expandedProviders, setExpandedProviders] = useState<Record<string, boolean>>({});

  const groupedProviderNaps = useMemo(() => {
    const groupedNaps: Record<string, NAPsItemProps> = {};

    data?.forEach((nap) => {
      const providerName = nap.ISP;
      const providerColor = nap.ISPColor;

      groupedNaps[providerName] ??= { color: providerColor, locations: [] };
      groupedNaps[providerName].locations.push(nap);
    });

    return groupedNaps;
  }, [data]);

  const toggleExpanded = (providerName: string) => {
    setExpandedProviders((prev) => ({ ...prev, [providerName]: !prev[providerName] }));
  };

  return (
    <section className="border rounded-md p-6">
      {Object.entries(groupedProviderNaps).map(([providerName, { color, locations }]) => {
        const isExpanded = expandedProviders[providerName] ?? false;
        const visibleLocations = isExpanded ? locations : locations.slice(0, 3);

        return (
          <Card key={providerName} className="mb-4">
            <CardHeader>
              <section className="flex justify-between">
                <div className="flex items-center gap-4">
                  <div className="rounded-full bg-muted w-10 h-10 flex items-center justify-center">
                    <span style={{ color: color }} className="opacity-50 text-lg font-bold text-muted-foreground">
                      {providerName[0]}
                    </span>
                  </div>
                  <div>
                    <Badge style={{ backgroundColor: color }}>{providerName}</Badge>
                    <p className="text-xs text-muted-foreground">Service Provider</p>
                  </div>
                </div>

                <div className="flex items-center gap-x-3 text-sm text-muted-foreground">
                  <div className="flex p-2 rounded-full bg-muted">
                    <Workflow className="w-5 h-5" />
                  </div>
                  <div>
                    <p className="text-xs">Network Access Points</p>
                  </div>
                  <DropdownEditNAPsProvider />
                </div>
              </section>
            </CardHeader>

            <CardContent>
              <ScrollArea className="max-h-fit">
                {visibleLocations.map((nap, index) => (
                  <div key={index} className="grid grid-cols-3 items-center py-2">
                    <div className="h-fit col-span-2">
                      <div className="w-fit">
                        <p className="text-sm font-medium">Coordinates</p>
                        <p className="text-sm text-muted-foreground">
                          {nap.lat}, {nap.lng}
                        </p>
                      </div>
                    </div>
                    <Button variant="outline" className="text-sm col-span-1">
                      See Location
                    </Button>
                  </div>
                ))}
              </ScrollArea>
            </CardContent>

            {locations.length > 3 && (
              <CardFooter className="w-full flex justify-center">
                <Button variant="outline" className="text-xs w-full" onClick={() => toggleExpanded(providerName)}>
                  {isExpanded ? "Show Less" : "Show More"}
                </Button>
              </CardFooter>
            )}
          </Card>
        );
      })}
    </section>
  );
}
