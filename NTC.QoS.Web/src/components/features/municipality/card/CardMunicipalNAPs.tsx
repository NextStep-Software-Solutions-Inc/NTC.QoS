"use client";

import { useMemo, useState } from "react";

import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardFooter, CardHeader } from "@/components/ui/card";
import { ScrollArea } from "@/components/ui/scroll-area";
import { type NAP } from "../../NAP/types/NAP";

interface BarangayNameWithNAPProps {
  data?: NAP[];
}

interface NAPsItemProps {
  color?: string;
  locations: NAP[];
}

export default function CardMunicipalNAPs({ data }: BarangayNameWithNAPProps) {
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
    <div className="w-full space-y-2">
      {Object.entries(groupedProviderNaps).map(([providerName, { color, locations }]) => {
        const isExpanded = expandedProviders[providerName] ?? false;
        const visibleLocations = isExpanded ? locations : locations.slice(0, 3);

        return (
          <Card key={providerName} className="">
            <CardHeader className="p-4">
              <section className="flex justify-between">
                <div className="flex items-center gap-2">
                  <div className="rounded-full bg-muted w-8 h-8 flex items-center justify-center">
                    <span style={{ color: color }} className="font-bold text-muted-foreground">
                      {providerName[0]}
                    </span>
                  </div>
                  <div className="space-y-1">
                    <Badge style={{ backgroundColor: color }}>{providerName}</Badge>
                    <p className="text-xs text-muted-foreground">
                      <span className="font-bold text-primary">{locations.length}</span> Network Access Points
                    </p>
                  </div>
                </div>

                {/* <div className="flex items-center gap-x-3 text-sm text-muted-foreground">
                  <div className="flex p-2 rounded-full bg-muted">
                    <Workflow className="w-5 h-5" />
                  </div>
                  <div>
                    <p className="text-xs">Network Access Points</p>
                    <p className="text-sm font-medium">{locations.length}</p>
                  </div>
                  <DropdownEditNAPsProvider />
                </div> */}
              </section>
            </CardHeader>

            <CardContent>
              <ScrollArea className="max-h-[200px] overflow-y-auto">
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
    </div>
  );
}
