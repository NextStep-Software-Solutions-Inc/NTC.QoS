"use client";

import { useRouter, useSearchParams } from "next/navigation";
import { Gauge, MapPin, Radiation, Radio, RadioTower } from "lucide-react";

import FileList from "@/components/FileList";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";

const tabConfigs = [
  {
    value: "NAP",
    label: "NAP",
    icon: MapPin,
    activeClasses:
      "data-[state=active]:bg-gradient-to-r data-[state=active]:from-emerald-500 data-[state=active]:to-teal-500",
    hoverClasses: "hover:bg-gradient-to-r hover:from-emerald-500/10 hover:to-teal-500/10",
  },
  {
    value: "Speedtest",
    label: "Speedtest",
    icon: Gauge,
    activeClasses:
      "data-[state=active]:bg-gradient-to-r data-[state=active]:from-blue-500 data-[state=active]:to-cyan-500",
    hoverClasses: "hover:bg-gradient-to-r hover:from-blue-500/10 hover:to-cyan-500/10",
  },
  {
    value: "Cell",
    label: "Cell",
    icon: RadioTower,
    activeClasses:
      "data-[state=active]:bg-gradient-to-r data-[state=active]:from-violet-500 data-[state=active]:to-purple-500",
    hoverClasses: "hover:bg-gradient-to-r hover:from-violet-500/10 hover:to-purple-500/10",
  },
  {
    value: "WirelessValidation",
    label: "Wireless Validation",
    icon: Radio,
    activeClasses:
      "data-[state=active]:bg-gradient-to-r data-[state=active]:from-red-500 data-[state=active]:to-orange-500",
    hoverClasses: "hover:bg-gradient-to-r hover:from-orange-500/10 hover:to-red-500/10",
  },
  {
    value: "CellularCoverage",
    label: "Cellular Coverage",
    icon: Radiation,
    activeClasses:
      "data-[state=active]:bg-gradient-to-r data-[state=active]:from-pink-500 data-[state=active]:to-rose-500",
    hoverClasses: "hover:bg-gradient-to-r hover:from-pink-500/10 hover:to-rose-500/10",
  },
];

const ImportTabs = () => {
  const router = useRouter();
  const searchParams = useSearchParams();
  const type = (searchParams.get("type") ?? "NAP") as "Speedtest" | "NAP" | "Cell" | "WirelessValidation" | "CellularCoverage";

  const handleChangTab = (value: string) => {
    const params = new URLSearchParams(searchParams.toString());
    params.set("type", value);
    router.replace(`?${params.toString()}`, { scroll: false });
  };

  return (
    <Tabs value={type} defaultValue={type} className="w-full" onValueChange={handleChangTab}>
      <div className="bg-muted/30 px-6 py-4">
        <div className="overflow-x-auto pb-1">
          <TabsList className="inline-flex h-auto min-w-max gap-2 rounded-xl border border-gray-200/50 bg-white/60 p-1.5 shadow-sm backdrop-blur-sm dark:border-gray-700/50 dark:bg-gray-800/60">
            {tabConfigs.map((tab) => {
              const Icon = tab.icon;
              return (
                <TabsTrigger
                  key={tab.value}
                  value={tab.value}
                  className={`flex h-auto min-h-9 items-center gap-2 rounded-lg px-3 py-2 text-left text-xs font-medium leading-tight transition-all whitespace-normal sm:px-4 sm:text-sm data-[state=active]:text-white data-[state=active]:shadow-md data-[state=inactive]:text-gray-600 data-[state=inactive]:hover:bg-gray-100/50 data-[state=inactive]:hover:text-gray-900 ${tab.activeClasses}`}>
                  <Icon className="h-4 w-4 shrink-0" />
                  <span>{tab.label}</span>
                </TabsTrigger>
              );
            })}
          </TabsList>
        </div>
      </div>
      <div className="p-6">
        <TabsContent value="NAP" className="mt-0">
          <FileList type="NAP" />
        </TabsContent>
        <TabsContent value="Speedtest" className="mt-0">
          <FileList type="Speedtest" />
        </TabsContent>
        <TabsContent value="Cell" className="mt-0">
          <FileList type="Cell" />
        </TabsContent>
        <TabsContent value="WirelessValidation" className="mt-0">
          <FileList type="WirelessValidation" />
        </TabsContent>
        <TabsContent value="CellularCoverage" className="mt-0">
          <FileList type="CellularCoverage" />
        </TabsContent>
      </div>
    </Tabs>
  );
};
export default ImportTabs;
