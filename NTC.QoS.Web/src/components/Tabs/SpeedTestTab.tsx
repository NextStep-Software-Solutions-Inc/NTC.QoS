"use client";

import { useMemo, useState } from "react";
import { useSearchParams } from "next/navigation";

import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
import { Label } from "@/components/ui/label";
import { Slider } from "@/components/ui/slider";
import { Switch } from "@/components/ui/switch";

export default function FiberTab() {
  const searchParams = useSearchParams();
  const params = useMemo(() => Object.fromEntries(searchParams.entries()), [searchParams]);

  const networkTypes = ["2G", "3G", "4G LTE", "5G"];
  const operators = ["Globe", "Smart", "DITO"];
  const signalQuality = ["Weak", "Fair", "Good", "Excellent"];

  // Separate states for min and max
  const [uploadMin, setUploadMin] = useState([0]);
  const [uploadMax, setUploadMax] = useState([150]);
  const [downloadMin, setDownloadMin] = useState([0]);
  const [downloadMax, setDownloadMax] = useState([150]);

  const [selectedProviders, setSelectedProviders] = useState<string[]>([...networkTypes, ...operators]);

  const [selectedSignal, setSelectedSignal] = useState<string[]>(["Fair", "Good"]); // preselect some

  const toggleProvider = (provider: string) => {
    setSelectedProviders((prev) =>
      prev.includes(provider) ? prev.filter((p) => p !== provider) : [...prev, provider],
    );
  };

  const checkAll = (providers: string[]) => {
    setSelectedProviders((prev) => {
      const allSelected = providers.every((p) => prev.includes(p));
      return allSelected ? prev.filter((p) => !providers.includes(p)) : [...new Set([...prev, ...providers])];
    });
  };

  const toggleSignal = (signal: string) => {
    setSelectedSignal((prev) => (prev.includes(signal) ? prev.filter((s) => s !== signal) : [...prev, signal]));
  };

  const toggleShowSpeedTest = (checked: boolean) => {
    const searchParams = new URLSearchParams(window.location.search);
    if (checked) {
      searchParams.set("speedtests-cellular", "true");
    } else {
      searchParams.delete("speedtests-cellular");
    }
    window.history.replaceState({}, "", `${window.location.pathname}?${searchParams}`);
  };
  const renderSignalButton = (signal: string) => {
    const isSelected = selectedSignal.includes(signal);
    return (
      <Button
        key={signal}
        variant="outline"
        size="sm"
        onClick={() => toggleSignal(signal)}
        className={`flex-1 font-normal ${isSelected ? "bg-yellow-400 hover:bg-yellow-500" : ""}`}>
        {signal}
      </Button>
    );
  };
  return (
    <div className="p-0 space-y-4">
      {/* Fiber Toggle */}
      <div className="px-2">
        <p className="text-sm font-medium">Coverage Display</p>
        <div className="flex items-center mt-1">
          <Label className="text-sm flex items-center gap-2">
            {/* Pink indicator circle */}
            <span className="w-3 h-3 rounded-full bg-pink-500 inline-block"></span>
            Show Speed Test Location
          </Label>
          <div className="ml-auto pl-4">
            <Switch checked={params["speedtests-cellular"] === "true"} onCheckedChange={toggleShowSpeedTest} />
          </div>
        </div>
      </div>
      {/* Network Types */}
      <div>
        <div className="flex items-center justify-between bg-gray-100 py-2 px-2">
          <h3 className="text-sm font-semibold uppercase">Network Type</h3>
          <Button variant="ghost" size="sm" className="text-xs hover:underline" onClick={() => checkAll(networkTypes)}>
            Select All
          </Button>
        </div>
        <div className="flex flex-col gap-2 py-2 px-2">
          {networkTypes.map((network) => (
            <label key={network} className="flex items-center space-x-2">
              <Checkbox checked={selectedProviders.includes(network)} onCheckedChange={() => toggleProvider(network)} />
              <span className="text-sm">{network}</span>
            </label>
          ))}
        </div>
      </div>

      {/* Operators */}
      <div>
        <div className="flex items-center justify-between bg-gray-100 py-2 px-2">
          <h3 className="text-sm font-semibold">Operators</h3>
          <Button variant="ghost" size="sm" className="text-xs hover:underline" onClick={() => checkAll(operators)}>
            Select All
          </Button>
        </div>
        <div className="flex flex-col gap-2 py-2 px-2">
          {operators.map((operator) => (
            <label key={operator} className="flex items-center space-x-2">
              <Checkbox
                checked={selectedProviders.includes(operator)}
                onCheckedChange={() => toggleProvider(operator)}
              />
              <span className="text-sm">{operator}</span>
            </label>
          ))}
        </div>
      </div>

      {/* Speed Range with Sliders */}
      <div>
        <div className="flex items-center justify-between bg-gray-100 py-2 px-2">
          <h3 className="text-sm font-semibold">Speed Range</h3>
          <Button
            variant="ghost"
            size="sm"
            className="text-xs hover:underline"
            onClick={() => {
              setUploadMin([0]);
              setUploadMax([150]);
              setDownloadMin([0]);
              setDownloadMax([150]);
            }}>
            Reset
          </Button>
        </div>

        <div className="py-4 px-2 space-y-6">
          {/* Upload */}
          <div>
            <div className="flex justify-between text-sm mb-3">
              <span>Upload</span>
              <span>
                {uploadMin[0]} - {uploadMax[0]} Mbps
              </span>
            </div>
            <div className="flex flex-col gap-6">
              <Slider value={uploadMin} onValueChange={setUploadMin} max={150} step={1} />
              <Slider value={uploadMax} onValueChange={setUploadMax} max={150} step={1} />
            </div>
          </div>

          {/* Download */}
          <div className="pt-6">
            <div className="flex justify-between text-sm mb-1">
              <span>Download</span>
              <span>
                {downloadMin[0]} - {downloadMax[0]} Mbps
              </span>
            </div>
            <div className="flex flex-col gap-6">
              <Slider value={downloadMin} onValueChange={setDownloadMin} max={150} step={1} />
              <Slider value={downloadMax} onValueChange={setDownloadMax} max={150} step={1} />
            </div>
          </div>
        </div>
      </div>

      {/* Signal Quality as full-width equal buttons */}
      <div>
        <div className="flex items-center justify-between bg-gray-100 py-2 px-2">
          <h3 className="text-sm font-semibold">Signal Quality</h3>
          <Button
            variant="ghost"
            size="sm"
            className="text-xs hover:underline"
            onClick={() => setSelectedSignal(selectedSignal.length === signalQuality.length ? [] : signalQuality)}>
            Select All
          </Button>
        </div>
        <div className="flex w-full gap-2 py-2 px-2">{signalQuality.map(renderSignalButton)}</div>
      </div>

      {/* Footer buttons */}
      <div className="flex items-center justify-end gap-2 pt-2 px-2">
        <Button
          variant="outline"
          onClick={() => {
            setSelectedProviders([...networkTypes, ...operators]);
            setSelectedSignal([]);
            setUploadMin([0]);
            setUploadMax([150]);
            setDownloadMin([0]);
            setDownloadMax([150]);
          }}>
          Reset
        </Button>
        <Button>Apply Filters</Button>
      </div>
    </div>
  );
}
