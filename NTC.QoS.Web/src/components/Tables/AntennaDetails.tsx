import React from "react";

import { Input } from "../ui/input";
import { Label } from "../ui/label";
import { Select, SelectContent, SelectGroup, SelectItem, SelectLabel, SelectTrigger, SelectValue } from "../ui/select";

const AntennaDetails = () => {
  return (
    <div className="w-full space-x-4 flex items-center">
      <section className="w-fit">
        <Label>Antenna Height</Label>
        <Input
          id="antenna-height"
          type="text"
          placeholder="Antenna Height"
          className="w-full px-2 py-1 border rounded-md focus:outline-none focus:ring-1 focus:ring-blue-500"
        />
      </section>
      <section className="w-fit">
        <Label className="text-nowrap">Antenna Type</Label>
        <Select>
          <SelectTrigger className="w-full">
            <SelectValue placeholder="Select antenna type" />
          </SelectTrigger>
          <SelectContent>
            <SelectGroup>
              <SelectLabel>Antenna Type</SelectLabel>
              <SelectItem value="guyed">Guyed</SelectItem>
              <SelectItem value="self-supporting">Self Supporting</SelectItem>
              <SelectItem value="monopole">Monopole</SelectItem>
              <SelectItem value="unknown">Unknown</SelectItem>
            </SelectGroup>
          </SelectContent>
        </Select>
      </section>
      <section className="w-fit">
        <Label>Coordinates</Label>
        <Input
          id="antenna-coordinates"
          type="text"
          placeholder="123, 0, 311, 0"
          className="w-full px-2 py-1 border rounded-md focus:outline-none focus:ring-1 focus:ring-blue-500"
        />
      </section>
    </div>
  );
};

export default AntennaDetails;
