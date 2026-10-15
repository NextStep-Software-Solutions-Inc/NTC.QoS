"use client";

import { motion } from "framer-motion";
import { Filter, WifiOff, X } from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { DateRangeFilter } from "@/components/features/speedtest/components/DateRangeFilter";
import { SIGNAL_QUALITY_CONFIG, type SignalQuality } from "../../../types";

interface FilterOption {
  id: string;
  name: string;
  color?: string;
}

interface FilterPanelProps {
  // Region
  regionId?: string;
  regionFilter: string;
  onRegionFilterChange: (value: string) => void;
  regions: FilterOption[];
  // Filters
  providerFilter: string;
  provinceFilter: string;
  municipalityFilter: string;
  barangayFilter: string;
  signalQualityFilter: string;
  nbcFilter?: string;
  startDate?: string;
  endDate?: string;
  onProviderFilterChange: (value: string) => void;
  onProvinceFilterChange: (value: string) => void;
  onMunicipalityFilterChange: (value: string) => void;
  onBarangayFilterChange: (value: string) => void;
  onSignalQualityFilterChange: (value: string) => void;
  onNbcFilterChange?: (value: string) => void;
  onDateRangeChange?: (startDate?: string, endDate?: string) => void;
  // Filter options
  providers: FilterOption[];
  provinces: FilterOption[];
  municipalities: FilterOption[];
  barangays: FilterOption[];
  // Actions
  onResetFilters: () => void;
  activeFilterCount: number;
}

const signalQualities: SignalQuality[] = ["Unknown", "VeryWeak", "Weak", "Average", "Good", "Excellent"];

export function FilterPanel({
  regionId,
  regionFilter,
  onRegionFilterChange,
  regions,
  providerFilter,
  provinceFilter,
  municipalityFilter,
  barangayFilter,
  signalQualityFilter,
  nbcFilter = "all",
  startDate,
  endDate,
  onProviderFilterChange,
  onProvinceFilterChange,
  onMunicipalityFilterChange,
  onBarangayFilterChange,
  onSignalQualityFilterChange,
  onNbcFilterChange,
  onDateRangeChange,
  providers,
  provinces,
  municipalities,
  barangays,
  onResetFilters,
  activeFilterCount,
}: FilterPanelProps) {
  const isRegionPinned = !!regionId;

  return (
    <motion.div
      initial={{ opacity: 0, height: 0 }}
      animate={{ opacity: 1, height: "auto" }}
      exit={{ opacity: 0, height: 0 }}
      className="flex flex-wrap items-center gap-2 p-3 bg-gradient-to-r from-gray-50/80 to-white dark:from-gray-900/80 dark:to-gray-800 rounded-xl border border-gray-200/50 dark:border-gray-700/50">
      <div className="flex items-center gap-2 text-sm font-medium text-gray-700 dark:text-gray-300">
        <Filter className="h-4 w-4 text-blue-500" />
        <span>Filters</span>
        {activeFilterCount > 0 && (
          <Badge className="bg-gradient-to-r from-blue-500 to-purple-500 text-white border-0 text-xs px-2">
            {activeFilterCount}
          </Badge>
        )}
      </div>

      {/* Date Range Filter */}
      {onDateRangeChange && (
        <DateRangeFilter
          startDate={startDate}
          endDate={endDate}
          onChange={onDateRangeChange}
          size="sm"
        />
      )}

      {/* Region Filter */}
      <Select value={regionFilter} onValueChange={onRegionFilterChange} disabled={isRegionPinned}>
        <SelectTrigger
          className={`w-40 h-9 text-sm ${isRegionPinned ? "bg-blue-50 dark:bg-blue-950/20 border-blue-300" : ""}`}>
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

      {/* Provider Filter */}
      <Select value={providerFilter} onValueChange={onProviderFilterChange}>
        <SelectTrigger className="w-36 h-9 text-sm">
          <SelectValue placeholder="Provider" />
        </SelectTrigger>
        <SelectContent>
          <SelectItem value="all">All Providers</SelectItem>
          {providers.map((provider) => (
            <SelectItem key={provider.id} value={provider.id}>
              <div className="flex items-center gap-2">
                {provider.color && (
                  <span className="h-2.5 w-2.5 rounded-full" style={{ backgroundColor: provider.color }} />
                )}
                {provider.name}
              </div>
            </SelectItem>
          ))}
        </SelectContent>
      </Select>

      {/* Signal Quality Filter */}
      <Select value={signalQualityFilter} onValueChange={onSignalQualityFilterChange}>
        <SelectTrigger className="w-36 h-9 text-sm">
          <SelectValue placeholder="Signal Quality" />
        </SelectTrigger>
        <SelectContent>
          <SelectItem value="all">All Quality</SelectItem>
          {signalQualities.map((quality) => (
            <SelectItem key={quality} value={quality}>
              <div className="flex items-center gap-2">
                <span
                  className="h-2.5 w-2.5 rounded-full"
                  style={{ backgroundColor: SIGNAL_QUALITY_CONFIG[quality].color }}
                />
                {SIGNAL_QUALITY_CONFIG[quality].label}
              </div>
            </SelectItem>
          ))}
        </SelectContent>
      </Select>

      {/* NBC Filter */}
      <Select value={nbcFilter ?? "all"} onValueChange={onNbcFilterChange} disabled={!onNbcFilterChange}>
        <SelectTrigger className="w-36 h-9 text-sm disabled:cursor-not-allowed disabled:opacity-50">
          <SelectValue placeholder="Coverage" />
        </SelectTrigger>
        <SelectContent>
          <SelectItem value="all">All Coverage</SelectItem>
          <SelectItem value="nbc">
            <div className="flex items-center gap-2">
              <WifiOff className="h-3.5 w-3.5 text-gray-500" />
              NBC Only
            </div>
          </SelectItem>
          <SelectItem value="non-nbc">Non-NBC Only</SelectItem>
        </SelectContent>
      </Select>

      {/* Province Filter */}
      <Select value={provinceFilter} onValueChange={onProvinceFilterChange}>
        <SelectTrigger className="w-36 h-9 text-sm">
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

      {/* Municipality Filter */}
      <Select value={municipalityFilter} onValueChange={onMunicipalityFilterChange}>
        <SelectTrigger className="w-40 h-9 text-sm">
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

      {/* Barangay Filter */}
      <Select value={barangayFilter} onValueChange={onBarangayFilterChange}>
        <SelectTrigger className="w-36 h-9 text-sm">
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

      {/* Reset Filters Button */}
      {activeFilterCount > 0 && (
        <Button
          variant="ghost"
          size="sm"
          onClick={onResetFilters}
          className="h-9 px-3 text-red-600 hover:text-red-700 hover:bg-red-50 dark:hover:bg-red-950/30">
          <X className="h-4 w-4 mr-1" />
          Clear
        </Button>
      )}
    </motion.div>
  );
}
