"use client";

import { useState } from "react";
import Link from "next/link";
import { ChevronRight, Globe2, Grid, List } from "lucide-react";

import { getRegionsRequest } from "@/components/features/region/handlers/getRegionsRequest";
import { Button } from "@/components/ui/button";
import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";
import { formatNumber } from "@/lib/format-number";

const useRegions = createQueryHookFromTemplate(getRegionsRequest);

export function RegionListToggle() {
  const { data: regions } = useRegions({ pathParams: {}, queryParams: { pageNumber: 1, pageSize: 100 } });
  const [view, setView] = useState<"list" | "grid">("grid");
  return (
    <div className="flex-1 min-h-0 flex flex-col ">
      {/* Upper right: Toggle buttons */}
      <div className="flex justify-end mb-6">
        <div className="flex gap-2">
          <Button
            variant={view === "list" ? "default" : "outline"}
            size="sm"
            onClick={() => setView("list")}
            aria-label="List view">
            <List className="w-4 h-4 mr-1" />
          </Button>
          <Button
            variant={view === "grid" ? "default" : "outline"}
            size="sm"
            onClick={() => setView("grid")}
            aria-label="Grid view">
            <Grid className="w-4 h-4 mr-1" />
          </Button>
        </div>
      </div>
      {/* List or Grid content below */}
      {view === "list" ? (
        <ul className="w-full space-y-4 flex-1 min-h-0 overflow-y-auto">
          {(regions?.data ?? []).map((region) => (
            <li key={region.id}>
              <Link href={`/${region.id}/dashboard`}>
                <div className="w-full flex items-center justify-between bg-card border border-border rounded-xl px-6 py-6 shadow-sm hover:shadow-lg hover:bg-accent/60 hover:border-primary transition-all duration-200 group cursor-pointer">
                  <div className="flex items-center gap-3 w-full">
                    <Globe2 className="w-6 h-6 text-primary/60" />
                    <span className="font-semibold break-words whitespace-normal block text-lg group-hover:text-primary transition-colors mb-0">
                      {region.name}
                    </span>
                  </div>
                  <span className="text-sm text-muted-foreground break-words whitespace-normal block mb-2 ml-9">
                    {region.description}
                  </span>
                  <span className="text-xs text-muted-foreground break-words whitespace-normal block ml-9">
                    {region.code} • {formatNumber(parseFloat(region.area))} sqm
                  </span>
                  <ChevronRight className="w-5 h-5 ml-4 text-muted-foreground group-hover:text-primary transition-colors" />
                </div>
              </Link>
            </li>
          ))}
        </ul>
      ) : (
        <div className="w-full flex-1 min-h-0 grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-6 overflow-y-auto">
          {(regions?.data ?? []).map((region) => (
            <Link key={region.id} href={`/${region.id}/dashboard`}>
              <div className="bg-card rounded-xl shadow-sm hover:shadow-lg hover:bg-accent/60 hover:border-primary transition-all duration-200 flex flex-col justify-between p-6 h-full cursor-pointer border border-border">
                <div className="flex items-start gap-3 mb-0">
                  <span className="flex items-center justify-center min-w-[1.5rem] min-h-[1.5rem] pt-1">
                    <Globe2 className="w-6 h-6 text-primary/60" />
                  </span>
                  <span className="font-semibold text-lg block group-hover:text-primary transition-colors">
                    {region.name}
                  </span>
                </div>
                <span className="text-sm text-muted-foreground block mb-2 ml-9">{region.description}</span>
                <div className="flex items-center justify-between mt-auto">
                  <span className="text-xs text-muted-foreground ml-9">
                    {region.code} • {formatNumber(parseFloat(region.area))} sqm
                  </span>
                  <ChevronRight className="w-5 h-5 ml-2 text-muted-foreground group-hover:text-primary transition-colors" />
                </div>
              </div>
            </Link>
          ))}
        </div>
      )}
    </div>
  );
}
