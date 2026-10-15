"use client";

import React, { type ReactNode } from "react";
import { CardSim, MapPinHouse, RadioTower, Search } from "lucide-react";

import { Input } from "@/components/ui/input";

interface AdminRouteToolbarProps {
  boundaryType?: ReactNode;
  totalPlace?: ReactNode;
  installedName?: ReactNode;
  installedIcon?: ReactNode;
  totalInstalled?: ReactNode;
  topISPs?: ReactNode;
  placeholder?: string;
  searchValue?: string;
  setSearchValue?: (value: string) => void;
}

const AdminRouteToolbar = ({
  boundaryType,
  totalPlace,
  installedName,
  installedIcon,
  totalInstalled,
  topISPs,
  placeholder,
  searchValue,
  setSearchValue,
}: AdminRouteToolbarProps) => {
  return (
    <div className="w-full border rounded-md shadow-sm p-4">
      {/* Left Info Section */}
      <div className="grid gap-2 grid-cols-1 xl:grid-cols-4 lg:grid-cols-3 sm:grid-cols-2">
        {/* Total Municipality */}
        <div className="flex items-center gap-2">
          <div className=" p-2 rounded-full bg-gray-100">
            <MapPinHouse className="w-4 h-4 text-muted-foreground" />
          </div>
          <div className="text-sm">
            <p className="text-sm font-semibold">{boundaryType ?? "Total [Name]"}</p>
            <p className="font-medium text-primary">{totalPlace ?? "0"}</p>
          </div>
        </div>

        {/* Total Cell Sites */}
        <div className="flex items-center gap-2">
          <div className=" p-2 rounded-full bg-gray-100">
            {installedIcon ?? <RadioTower className="w-4 h-4 text-muted-foreground" />}
          </div>

          <div className="text-sm">
            <p className="text-sm font-semibold">{installedName ?? "Total [Name]"}</p>
            <p className="font-medium text-primary">{totalInstalled}</p>
          </div>
        </div>

        {/* Top ISPs */}
        <div>
          <div className="flex items-center gap-2">
            <div className="p-2 rounded-full bg-gray-100">
              <CardSim className="w-4 h-4 text-muted-foreground" />
            </div>

            <div className="">
              <p className="text-sm font-semibold">Top Internet Service Providers</p>
              <div className="w-full flex flex-wrap items-center gap-1">{topISPs ?? "0"}</div>
            </div>
          </div>
        </div>
        {/* Search Input */}
        <div className=" flex items-center sm:w-auto  border rounded-md bg-gray-50 px-2">
          <Search className="w-5 h-5 text-muted-foreground" />
          <Input
            type="search"
            placeholder={placeholder}
            value={searchValue}
            onChange={(e) => setSearchValue?.(e.target.value)}
            className="border-none bg-gray-50 focus:outline-none focus:ring-0 focus-visible:ring-0"
          />
        </div>
      </div>
    </div>
  );
};

export default AdminRouteToolbar;
