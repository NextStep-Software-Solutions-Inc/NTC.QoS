"use client";

import { Pin } from "lucide-react";

import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";

interface FilterOption {
  id: string;
  name: string;
}

interface FilterPanelProps {
  // Region
  regionId?: string;
  regionFilter: string;
  onRegionFilterChange: (value: string) => void;
  regions: FilterOption[];
  // Filters
  statusFilter: string;
  verifiedFilter: string;
  providerFilter: string;
  provinceFilter: string;
  municipalityFilter: string;
  barangayFilter: string;
  onStatusFilterChange: (value: string) => void;
  onVerifiedFilterChange: (value: string) => void;
  onProviderFilterChange: (value: string) => void;
  onProvinceFilterChange: (value: string) => void;
  onMunicipalityFilterChange: (value: string) => void;
  onBarangayFilterChange: (value: string) => void;
  // Filter options
  providers: FilterOption[];
  provinces: FilterOption[];
  municipalities: FilterOption[];
  barangays: FilterOption[];
}

export function FilterPanel({
  regionId,
  regionFilter,
  onRegionFilterChange,
  regions,
  statusFilter,
  verifiedFilter,
  providerFilter,
  provinceFilter,
  municipalityFilter,
  barangayFilter,
  onStatusFilterChange,
  onVerifiedFilterChange,
  onProviderFilterChange,
  onProvinceFilterChange,
  onMunicipalityFilterChange,
  onBarangayFilterChange,
  providers,
  provinces,
  municipalities,
  barangays,
}: FilterPanelProps) {
  const isRegionPinned = !!regionId;

  return (
    <div className="flex flex-wrap items-center gap-2">
      {/* Region Filter - Pinned if in URL */}
      <div className="relative">
        <Select value={regionFilter} onValueChange={onRegionFilterChange} disabled={isRegionPinned}>
          <SelectTrigger
            className={`w-48 h-9 ${isRegionPinned ? "bg-blue-50 dark:bg-blue-950/20 border-blue-300" : ""}`}>
            {isRegionPinned && <Pin className="h-3 w-3 mr-1 text-blue-500" />}
            <SelectValue placeholder="Region" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">All Regions</SelectItem>
            {regions.map((region) => (
              <SelectItem key={region.id} value={region.id}>
                {region.name}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>

      <Select value={statusFilter} onValueChange={onStatusFilterChange}>
        <SelectTrigger className="w-36 h-9">
          <SelectValue placeholder="Status" />
        </SelectTrigger>
        <SelectContent>
          <SelectItem value="all">All Status</SelectItem>
          <SelectItem value="unknown">Unknown</SelectItem>
          <SelectItem value="pending">Pending</SelectItem>
          <SelectItem value="licensed">Licensed</SelectItem>
        </SelectContent>
      </Select>

      <Select value={verifiedFilter} onValueChange={onVerifiedFilterChange}>
        <SelectTrigger className="w-36 h-9">
          <SelectValue placeholder="Verified" />
        </SelectTrigger>
        <SelectContent>
          <SelectItem value="all">All</SelectItem>
          <SelectItem value="true">Verified</SelectItem>
          <SelectItem value="false">Unverified</SelectItem>
        </SelectContent>
      </Select>

      <Select value={providerFilter} onValueChange={onProviderFilterChange}>
        <SelectTrigger className="w-48 h-9">
          <SelectValue placeholder="Provider" />
        </SelectTrigger>
        <SelectContent>
          <SelectItem value="all">All Providers</SelectItem>
          {providers.map((provider) => (
            <SelectItem key={provider.id} value={provider.id}>
              {provider.name}
            </SelectItem>
          ))}
        </SelectContent>
      </Select>

      <Select value={provinceFilter} onValueChange={onProvinceFilterChange}>
        <SelectTrigger className="w-48 h-9">
          <SelectValue placeholder="Province" />
        </SelectTrigger>
        <SelectContent>
          <SelectItem value="all">All Provinces</SelectItem>
          {provinces.map((province) => (
            <SelectItem key={province.id} value={province.id}>
              {province.name}
            </SelectItem>
          ))}
        </SelectContent>
      </Select>

      <Select value={municipalityFilter} onValueChange={onMunicipalityFilterChange} disabled={provinceFilter === "all"}>
        <SelectTrigger className="w-48 h-9">
          <SelectValue placeholder="Municipality" />
        </SelectTrigger>
        <SelectContent>
          <SelectItem value="all">All Municipalities</SelectItem>
          {municipalities.map((municipality) => (
            <SelectItem key={municipality.id} value={municipality.id}>
              {municipality.name}
            </SelectItem>
          ))}
        </SelectContent>
      </Select>

      <Select value={barangayFilter} onValueChange={onBarangayFilterChange} disabled={municipalityFilter === "all"}>
        <SelectTrigger className="w-48 h-9">
          <SelectValue placeholder="Barangay" />
        </SelectTrigger>
        <SelectContent>
          <SelectItem value="all">All Barangays</SelectItem>
          {barangays.map((barangay) => (
            <SelectItem key={barangay.id} value={barangay.id}>
              {barangay.name}
            </SelectItem>
          ))}
        </SelectContent>
      </Select>
    </div>
  );
}
