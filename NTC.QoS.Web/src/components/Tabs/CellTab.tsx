"use client";

import { useMemo, useState } from "react";
import { useSearchParams } from "next/navigation";
import { CheckCircle, RadioTower } from "lucide-react";

import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
import { Label } from "@/components/ui/label";
import { Separator } from "@/components/ui/separator";
import { Switch } from "@/components/ui/switch";

export default function CellTab() {
  const searchParams = useSearchParams();
  const params = useMemo(() => Object.fromEntries(searchParams.entries()), [searchParams]);
  const [operators, setOperators] = useState({ globe: true, smart: true, dito: true, others: true });

  const [selectedFrequencies, setSelectedFrequencies] = useState<string[]>([
    "GSM-900",
    "GSM-1800",
    "WCDMA-850",
    "WCDMA-900",
    "LTE-700",
    "LTE-850",
    "LTE-900",
    "LTE-1800",
    "LTE-2100",
    "LTE-TDD-2300",
    "LTE-TDD-2500",
    "5G-2600",
  ]);

  const toggleOperator = (key: keyof typeof operators) => {
    setOperators((prev) => ({ ...prev, [key]: !prev[key] }));
  };

  const selectAll = () => {
    const allSelected = Object.values(operators).every(Boolean);
    const newState = Object.fromEntries(Object.keys(operators).map((key) => [key, !allSelected])) as typeof operators;
    setOperators(newState);
  };

  const toggleFrequency = (freq: string) => {
    setSelectedFrequencies((prev) => (prev.includes(freq) ? prev.filter((f) => f !== freq) : [...prev, freq]));
  };

  const toggleShowCell = (checked: boolean) => {
    const searchParams = new URLSearchParams(window.location.search);
    if (checked) {
      searchParams.set("cellular", "true");
    } else {
      searchParams.delete("cellular");
    }
    window.history.replaceState({}, "", `${window.location.pathname}?${searchParams}`);
  };
  const toggleCellularCoverage = (checked: boolean) => {
    const searchParams = new URLSearchParams(window.location.search);
    if (checked) {
      searchParams.set("cellular_coverage", "true");
    } else {
      searchParams.delete("cellular_coverage");
    }
    window.history.replaceState({}, "", `${window.location.pathname}?${searchParams}`);
  };

  const renderFrequencyButton = (freq: string) => {
    const isSelected = selectedFrequencies.includes(freq);
    const displayFreq = freq.split("-").pop();
    return (
      <Button
        key={freq}
        variant="outline"
        size="sm"
        onClick={() => toggleFrequency(freq)}
        className={`font-normal ${isSelected ? "bg-yellow-400 hover:bg-yellow-500" : ""}`}>
        {displayFreq}
      </Button>
    );
  };

  return (
    <div className="px-2 space-y-4">
      {/* Layer Visibility */}
      <div className="space-y-2">
        <h3 className="text-sm font-medium">Layer Visibility</h3>
        <div className="flex items-center justify-between">
          <Label htmlFor="show-cell-sites" className="flex-1 flex items-center gap-2">
            <RadioTower className="w-4 h-4 text-gray-600" />
            Show Cell Sites
          </Label>
          <Switch id="show-cell-sites" onCheckedChange={toggleShowCell} checked={params.cellular === "true"} />
        </div>
        <div className="flex items-center justify-between">
          <Label htmlFor="show-cellular-coverage" className="flex-1 flex items-center gap-2">
            <CheckCircle className="w-4 h-4 text-gray-600" />
            Show Cellular Coverage
          </Label>
          <Switch
            id="show-cellular-coverage"
            onCheckedChange={toggleCellularCoverage}
            checked={params.cellular === "true" && params.cellular_coverage === "true"}
          />
        </div>
      </div>

      <Separator />

      {/* Operators Section */}
      <div className="space-y-2">
        <div className="flex items-center justify-between">
          <h3 className="text-sm font-medium">Operators</h3>
          <Button variant="link" size="sm" className="p-0 h-auto hover:underline" onClick={selectAll}>
            Select All
          </Button>
        </div>
        <div className="space-y-3">
          <div className="flex items-center gap-2">
            <Checkbox checked={operators.globe} onCheckedChange={() => toggleOperator("globe")} id="globe-telecom" />
            <Label htmlFor="globe-telecom" className="font-normal">
              Globe Telecom
            </Label>
          </div>
          <div className="flex items-center gap-2">
            <Checkbox
              checked={operators.smart}
              onCheckedChange={() => toggleOperator("smart")}
              id="smart-communications"
            />
            <Label htmlFor="smart-communications" className="font-normal">
              Smart Communications
            </Label>
          </div>
          <div className="flex items-center gap-2">
            <Checkbox checked={operators.dito} onCheckedChange={() => toggleOperator("dito")} id="dito-telecommunity" />
            <Label htmlFor="dito-telecommunity" className="font-normal">
              DITO Telecommunity
            </Label>
          </div>
        </div>
      </div>

      <Separator />

      {/* Network Technology & Bands */}
      <div className="space-y-3">
        <h3 className="text-sm font-medium">Network Technology & Bands</h3>

        {/* 2G */}
        <div>
          <Label className="text-xs text-muted-foreground">2G GSM</Label>
          <div className="grid grid-cols-3 gap-2 mt-1">
            {["GSM-900", "GSM-1800"].map(renderFrequencyButton)}
            {/* placeholder for equal width */}
            <div aria-hidden className="h-9 opacity-0 pointer-events-none" />
          </div>
        </div>

        {/* 3G */}
        <div>
          <Label className="text-xs text-muted-foreground">3G WCDMA</Label>
          <div className="grid grid-cols-3 gap-2 mt-1">{["WCDMA-850", "WCDMA-2100"].map(renderFrequencyButton)}</div>
        </div>

        {/* 4G FDD */}
        <div>
          <Label className="text-xs text-muted-foreground">4G FDD-LTE</Label>
          <div className="grid grid-cols-3 gap-2 mt-1">
            {["LTE-700", "LTE-900", "LTE-1800", "LTE-2100"].map(renderFrequencyButton)}
          </div>
        </div>

        {/* 4G TDD */}
        <div>
          <Label className="text-xs text-muted-foreground">4G TDD-LTE</Label>
          <div className="grid grid-cols-3 gap-2 mt-1">
            {["LTE-TDD-2300", "LTE-TDD-2500", "LTE-TDD-2600"].map(renderFrequencyButton)}
          </div>
        </div>

        {/* 5G */}
        <div>
          <Label className="text-xs text-muted-foreground">5G</Label>
          <div className="grid grid-cols-3 gap-2 mt-1">
            {["5G-2600", "5G-3500"].map(renderFrequencyButton)}
            {/* placeholder for equal width */}
            <div aria-hidden className="h-9 opacity-0 pointer-events-none" />
          </div>
        </div>
      </div>
    </div>
  );
}
