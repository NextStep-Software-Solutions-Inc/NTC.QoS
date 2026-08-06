"use client";

import Image from "next/image";
import { Building2, Globe, RadioTower, UsersRound } from "lucide-react";

import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";

const CoverageMap = () => {
  return (
    <Card>
      <CardHeader className="my-3">
        <CardTitle className="text-lg font-semibold flex items-center gap-2">
          <Globe className="h-5 w-5" />
          <p className="text-[20px]">Coverage Map</p>
        </CardTitle>
        <p className="text-sm text-muted-foreground">Estimated coverage range and population served</p>
      </CardHeader>
      <CardContent>
        <div className="w-full flex flex-wrap gap-x-[4rem] mb-4">
          <div className="w-fit flex items-center gap-x-2">
            <div className="p-3 bg-gray-200 rounded-full">
              <Building2 />
            </div>
            <section>
              <p className="text-[14px] font-semibold">Coverage Radius</p>
              <p className="text-[12px]">~7.2 kilometers</p>
            </section>
          </div>

          <div className="w-fit flex items-center gap-x-2">
            <div className="p-3 bg-gray-200 rounded-full">
              <UsersRound />
            </div>
            <section>
              <p className="text-[14px] font-semibold">Population Served</p>
              <p className="text-[12px]">~12,900</p>
            </section>
          </div>

          <div className="w-fit flex items-center gap-x-2">
            <div className="p-3 bg-gray-200 rounded-full">
              <RadioTower />
            </div>
            <section>
              <p className="text-[14px] font-semibold">Barangays Covered</p>
              <p className="text-[12px]">~25 Barangays</p>
            </section>
          </div>
        </div>

        {/* Map Image */}
        <div className="overflow-hidden rounded-lg">
          <Image
            src={"/assets/images/coverage_brgy.png"}
            alt="Coverage Map"
            width={600}
            height={300}
            className="w-full h-auto object-cover"
          />
        </div>
      </CardContent>
    </Card>
  );
};

export default CoverageMap;
