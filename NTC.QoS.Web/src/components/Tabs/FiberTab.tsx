"use client";

import { useMemo, useState } from "react";
import { useSearchParams } from "next/navigation";

import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
import { Label } from "@/components/ui/label";
import { Switch } from "@/components/ui/switch";
import { cn } from "@/lib/utils";

export default function FiberTab() {
  const searchParams = useSearchParams();
  const params = useMemo(() => Object.fromEntries(searchParams.entries()), [searchParams]);
  const majorProviders = ["Globe", "Converge", "PLDT"];

  const groupedProviders = {
    "Luzon Providers": ["GCORP", "INTELCO", "Radius", "StreamTech", "Laguna", "DSM", "King", "PML", "RD", "RS"],
    "Visayas Providers": [
      "Cine Cebu",
      "BITS",
      "Danao Cable TV",
      "DCTV",
      "Premium  Line",
      "BTS",
      "CCR",
      "Pk",
      "RMS",
      "EZTech",
    ],
    "Mindanao Providers": [
      "AFTECH",
      "CS",
      "GoodGame Netowrk",
      "NC",
      "YAM",
      "Archon",
      "DBSS Cable",
      "Drixnet",
      "JMJR",
      "Marble Telecomm",
    ],
    Others: ["ProviderX", "ProviderY", "ProviderZ"],
  };

  const [selectedProviders, setSelectedProviders] = useState<string[]>([
    // Major Providers
    "Globe",
    "Converge",
    "PLDT",

    // Luzon defaults (5)
    "GCORP",
    "INTELCO",
    "Radius",
    "StreamTech",
    "Laguna",

    // Visayas defaults (5)
    "Cine Cebu",
    "BITS",
    "Danao Cable TV",
    "DCTV",
    "Premium  Line",

    // Mindanao defaults (6)
    "AFTECH",
    "CS",
    "GoodGame Netowrk",
    "NC",
    "YAM",
    "Archon",

    // Others (optional)
    "ProviderX",
  ]);

  const [expandedRegions, setExpandedRegions] = useState<Record<string, boolean>>({
    "Luzon Providers": false,
    "Visayas Providers": false,
    "Mindanao Providers": false,
  });

  const toggleProvider = (provider: string) => {
    setSelectedProviders((prev) =>
      prev.includes(provider) ? prev.filter((p) => p !== provider) : [...prev, provider],
    );
  };

  const toggleShowFiberCoverage = (checked: boolean) => {
    const searchParams = new URLSearchParams(window.location.search);
    if (checked) {
      searchParams.set("fiber", "true");
    } else {
      searchParams.delete("fiber");
    }
    window.history.replaceState({}, "", `${window.location.pathname}?${searchParams}`);
  };

  const toggleShowNAPs = (checked: boolean) => {
    const searchParams = new URLSearchParams(window.location.search);
    if (checked) {
      searchParams.set("naps", "true");
    } else {
      searchParams.delete("naps");
    }
    window.history.replaceState({}, "", `${window.location.pathname}?${searchParams}`);
  };

  const providerColors: Record<string, string> = {
    Globe: "bg-blue-500",
    Converge: "bg-purple-500",
    PLDT: "bg-red-500",
  };

  const regionColors: Record<string, string> = {
    "Luzon Providers": "bg-gray-600",
    "Visayas Providers": "bg-gray-500",
    "Mindanao Providers": "bg-gray-400",
    Others: "bg-gray-300",
  };

  return (
    <div className="p-0 space-y-4">
      {/* Fiber Toggle */}
      <div className="px-2">
        <p className="text-sm font-medium">Coverage Display</p>
        <div className="flex items-center mt-1">
          <div className="flex items-center space-x-2">
            <div className="w-3 h-3 rounded-full bg-pink-400" />
            <Label className="text-sm">Show Fiber Coverage</Label>
          </div>
          <div className="ml-auto pl-4">
            <Switch checked={params.fiber === "true"} onCheckedChange={toggleShowFiberCoverage} />
          </div>
        </div>
        <div className="flex items-center mt-1">
          <div className="flex items-center space-x-2">
            <div className="w-3 h-3 rounded-full bg-pink-400" />
            <Label className="text-sm">Show NAPs</Label>
          </div>
          <div className="ml-auto pl-4">
            <Switch checked={params.naps === "true"} onCheckedChange={toggleShowNAPs} />
          </div>
        </div>
      </div>

      {/* Major Providers */}
      <div>
        <div className="flex items-center justify-between bg-gray-100 py-2 px-2">
          <h3 className="text-sm font-semibold uppercase">Major Providers</h3>
        </div>
        <div className="flex flex-col gap-2 py-2 px-2">
          {majorProviders.map((provider) => (
            <label key={provider} className="flex items-center space-x-2">
              <Checkbox
                checked={selectedProviders.includes(provider)}
                onCheckedChange={() => toggleProvider(provider)}
              />
              <div className={cn("w-3 h-3 rounded-full", providerColors[provider] ?? "bg-gray-400")} />
              <span className="text-sm">{provider}</span>
            </label>
          ))}
        </div>
      </div>

      {/* Sub Sections */}
      {Object.entries(groupedProviders).map(([region, providers]) =>
        region === "Others" ? (
          <div key={region} className="flex items-center justify-center gap-2 bg-gray-100 py-2 px-2">
            <div className={cn("w-3 h-3 rounded-full", regionColors[region])} />
            <h3 className="text-sm font-medium text-center">Unknown</h3>
          </div>
        ) : (
          <div key={region}>
            <div className="flex items-center justify-between bg-gray-100 py-2 px-2">
              <div className="flex items-center gap-2">
                <div className={cn("w-3 h-3 rounded-full", regionColors[region])} />
                <h3 className="text-sm font-medium">{region}</h3>
              </div>
              <Button
                variant="ghost"
                size="sm"
                className="text-xs hover:underline"
                onClick={() => setExpandedRegions((prev) => ({ ...prev, [region]: !prev[region] }))}>
                {expandedRegions[region] ? "See Less" : "See All"}
              </Button>
            </div>

            <div className="grid grid-cols-2 gap-2 py-2 px-2">
              {(expandedRegions[region] ? providers : providers.slice(0, 4)).map((provider) => (
                <label key={provider} className="flex items-center space-x-2">
                  <Checkbox
                    checked={selectedProviders.includes(provider)}
                    onCheckedChange={() => toggleProvider(provider)}
                  />
                  <span className="text-sm">{provider}</span>
                </label>
              ))}
            </div>
          </div>
        ),
      )}
    </div>
  );
}
