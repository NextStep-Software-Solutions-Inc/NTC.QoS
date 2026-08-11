"use client";

import React, { useState } from "react";

import { Card, CardContent } from "../ui/card";
import { Checkbox } from "../ui/checkbox";
import { Input } from "../ui/input";
import { Label } from "../ui/label";
import { Separator } from "../ui/separator";

const DisplayMapPreference = () => {
  const [region, setRegion] = useState("Region X");
  const [province, setProvince] = useState("Misamis Oriental");
  const [municipality, setMunicipality] = useState("Cagayan De Oro");
  const [isMap, setIsMap] = useState(true);
  const [isAdministrative, setIsAdministrative] = useState(false);
  const [isAutoCenter, setIsAutoCenter] = useState(false);
  return (
    <Card className="w-[700px]">
      <CardContent>
        <h2 className="text-sm font-semibold my-4">LOCATION</h2>

        {/* Email */}
        <div className="mb-4 mt-[2rem] w-[300px]">
          <Label htmlFor="email" className="text-sm mb-1 block">
            Region
          </Label>
          <Input id="email" type="email" value={region} onChange={(e) => setRegion(e.target.value)} />
        </div>

        {/* Password */}
        <div className="mb-4 w-[300px]">
          <section className="flex items-center gap-x-5">
            <Label htmlFor="text" className="text-sm mb-1 block">
              Province
            </Label>
            <Separator orientation="vertical" className="bg-gray-300 w-[1px] h-[20px] my-1" />
            <Label htmlFor="province" className="mb-1 block text-blue-600 p-0 h-auto text-sm">
              Change
            </Label>
          </section>
          <div className="">
            <Input
              id="province"
              className="w-full"
              type="text"
              value={province}
              onChange={(e) => setProvince(e.target.value)}
            />
          </div>
        </div>
        {/* Password */}
        <div className="mb-4 w-[300px]">
          <section className="flex items-center gap-x-5">
            <Label htmlFor="municipality" className="text-sm mb-1 block">
              Municipality
            </Label>
            <Separator orientation="vertical" className="bg-gray-300 w-[1px] h-[20px] my-1" />
            <Label htmlFor="municipality" className="mb-1 block text-blue-600 p-0 h-auto text-sm">
              Change
            </Label>
          </section>
          <div className="">
            <Input
              id="municipality"
              className="w-full"
              type="text"
              value={municipality}
              onChange={(e) => setMunicipality(e.target.value)}
            />
          </div>
        </div>

        <Separator className="my-5 w-full h-[1px] bg-gray-300" />
        {/* Checkboxes */}
        <div className="space-y-5 mt-6">
          <div className="flex items-center space-x-2">
            <Checkbox id="keepSignedIn" checked={isMap} onCheckedChange={() => setIsMap(!isMap)} />
            <Label htmlFor="keepSignedIn">Show labels on Map</Label>
          </div>

          <div className="flex items-center space-x-2 ">
            <Checkbox
              id="enable2FA"
              checked={isAdministrative}
              onCheckedChange={() => setIsAdministrative(!isAdministrative)}
            />
            <Label htmlFor="enable2FA">Show Administrative Boundaries</Label>
          </div>
          <div className="flex items-center space-x-2 ">
            <Checkbox id="enable2FA" checked={isAutoCenter} onCheckedChange={() => setIsAutoCenter(!isAutoCenter)} />
            <Label htmlFor="enable2FA">Auto-Center Based on Assigned Jurisdiction</Label>
          </div>
        </div>
      </CardContent>
    </Card>
  );
};

export default DisplayMapPreference;
