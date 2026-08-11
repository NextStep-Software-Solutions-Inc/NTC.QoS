"use client";

import { Antenna, ArrowUpRight, Building2, Compass, RadioTower } from "lucide-react";

import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";

const TowerAntennaSpec = () => {
  return (
    <Card className="w-full border mt-3 p-3">
      <CardHeader>
        <CardTitle className="text-lg font-semibold">
          <div className="flex items-center gap-2">
            <RadioTower className="h-5 w-5" />
            <p className="text-[20px]">Tower & Antenna Specifications</p>
          </div>
        </CardTitle>
      </CardHeader>
      <CardContent>
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
          <div className="w-full flex items-center gap-x-2">
            <div className="p-3 bg-gray-200 rounded-full">
              <Building2 />
            </div>
            <section>
              <p className="text-[14px] font-semibold">Tower Type</p>
              <p className="text-[12px]">Stealth</p>
            </section>
          </div>

          <div className="w-full flex items-center gap-x-2">
            <div className="p-3 bg-gray-200 rounded-full">
              <Antenna />
            </div>
            <section>
              <p className="text-[14px] font-semibold">Antenna Type</p>
              <p className="text-[12px]">Panel</p>
            </section>
          </div>

          <div className="w-full flex items-center gap-x-2">
            <div className="p-3 bg-gray-200 rounded-full">
              <RadioTower />
            </div>
            <section>
              <p className="text-[14px] font-semibold">Height</p>
              <p className="text-[12px]">41 meters</p>
            </section>
          </div>

          <div className="w-full flex items-center gap-x-2">
            <div className="p-3 bg-gray-200 rounded-full">
              <ArrowUpRight />
            </div>
            <section>
              <p className="text-[14px] font-semibold">Barangay 2</p>
              <p className="text-[12px]">BUKIDNON - QUEZON</p>
            </section>
          </div>

          <div className="w-full flex items-center gap-x-2">
            <div className="p-3 bg-gray-200 rounded-full">
              <Compass />
            </div>
            <section>
              <p className="text-[14px] font-semibold">Azimuth</p>
              <p className="text-[12px]">150°, 120°, 90°</p>
            </section>
          </div>
        </div>
      </CardContent>
    </Card>
  );
};

export default TowerAntennaSpec;
