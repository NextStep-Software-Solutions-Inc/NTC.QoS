/**
 * Fiber Information Component
 *
 * Redesigned popup component that displays all fiber feature properties
 * with a modern, glassmorphic styling and comprehensive data display.
 */

"use client";

import { memo, useMemo } from "react";
import type { Popup } from "@maptiler/sdk";
import { Building2, Cable, Hash, Info, Landmark, MapPin, Network, X } from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { ScrollArea } from "@/components/ui/scroll-area";
import { Separator } from "@/components/ui/separator";
import { cn } from "@/lib/utils";

interface FiberInformationProps {
  /** All feature properties from the GeoJSON feature */
  properties: Record<string, unknown>;
  /** MapTiler popup instance for close functionality */
  popup: Popup;
  /** Whether user is admin (shows more details) */
  isAdmin?: boolean;
  /** Optional className for styling */
  className?: string;
}

// Icon mapping for common property names
const PROPERTY_ICONS: Record<string, typeof Cable> = {
  address: MapPin,
  Address: MapPin,
  provider: Building2,
  Provider: Building2,
  providers: Building2,
  Providers: Building2,
  isp: Building2,
  ISP: Building2,
  network: Network,
  Network: Network,
  type: Cable,
  Type: Cable,
  id: Hash,
  Id: Hash,
  ID: Hash,
  name: Landmark,
  Name: Landmark,
};

// Properties to prioritize at the top
const PRIORITY_PROPERTIES = [
  "name",
  "Name",
  "address",
  "Address",
  "provider",
  "Provider",
  "providers",
  "Providers",
  "type",
  "Type",
  "status",
  "Status",
];

// Properties to hide from display
const HIDDEN_PROPERTIES = ["geometry", "bbox", "_id"];

/**
 * Format property name for display
 */
function formatPropertyName(key: string): string {
  return key
    .replace(/([A-Z])/g, " $1")
    .replace(/_/g, " ")
    .replace(/^./, (s) => s.toUpperCase())
    .trim();
}

/**
 * Strip MapTiler demo/free-tier watermark prefixes ("DEMO ", "MAPTILER ") from a string.
 */
function normalizeMaptilerValue(str: string): string {
  return str.replace(/^(DEMO|MAPTILER)\s+/i, "").trim();
}

/**
 * Format property value for display
 */
function formatPropertyValue(value: unknown): string {
  if (value === null || value === undefined) return "N/A";
  if (typeof value === "boolean") return value ? "Yes" : "No";
  if (typeof value === "number") return value.toLocaleString();
  if (Array.isArray(value)) return value.map((v) => (typeof v === "string" ? normalizeMaptilerValue(v) : String(v))).join(", ");
  if (typeof value === "object") return JSON.stringify(value);
  return normalizeMaptilerValue(String(value));
}

/**
 * Parse providers from a string (may be separated by / or ,)
 */
function parseProviders(value: unknown): string[] {
  if (!value) return [];
  const strValue = String(value);
  // Split by / or , and trim whitespace; strip MapTiler demo prefixes
  return strValue
    .split(/[\/,]/)
    .map((p) => normalizeMaptilerValue(p.trim()))
    .filter((p) => p.length > 0);
}

/**
 * PropertyRow Component
 */
const PropertyRow = memo(function PropertyRow({
  propertyKey,
  value,
  isAdmin,
}: {
  propertyKey: string;
  value: unknown;
  isAdmin?: boolean;
}) {
  const Icon = PROPERTY_ICONS[propertyKey] ?? Info;
  const formattedKey = formatPropertyName(propertyKey);

  // Special handling for provider/providers field
  const isProviderField = propertyKey.toLowerCase().includes("provider");

  if (isProviderField) {
    const providers = parseProviders(value);
    return (
      <div className="flex items-start gap-3 py-2">
        <div className="shrink-0 w-8 h-8 rounded-lg bg-purple-50 flex items-center justify-center">
          <Icon className="w-4 h-4 text-purple-500" />
        </div>
        <div className="flex-1 min-w-0">
          <p className="text-xs font-medium text-gray-500 mb-1">{formattedKey}</p>
          {isAdmin ? (
            <div className="flex flex-wrap gap-2">
              {providers.length > 0 ? (
                providers.map((provider, idx) => (
                  <Badge
                    key={idx}
                    variant="secondary"
                    className="text-xs bg-purple-50 text-purple-700 hover:bg-purple-100">
                    {provider.toLowerCase().includes("others") ? "Unknown" : provider}
                  </Badge>
                ))
              ) : (
                <span className="text-sm text-gray-400">N/A</span>
              )}
            </div>
          ) : (
            <p className="text-sm text-gray-800">
              <Badge variant="secondary" className="mr-1">
                {providers.length}
              </Badge>
              service provider{providers.length !== 1 ? "s" : ""} in this area
            </p>
          )}
        </div>
      </div>
    );
  }

  const formattedValue = formatPropertyValue(value);

  return (
    <div className="flex items-start gap-3 py-2">
      <div className="shrink-0 w-8 h-8 rounded-lg bg-gray-50 flex items-center justify-center">
        <Icon className="w-4 h-4 text-gray-500" />
      </div>
      <div className="flex-1 min-w-0">
        <p className="text-xs font-medium text-gray-500">{formattedKey}</p>
        <p className="text-sm text-gray-800 break-words">{formattedValue}</p>
      </div>
    </div>
  );
});

export const FiberInformation = memo(function FiberInformation({
  properties,
  popup,
  isAdmin = false,
  className,
}: FiberInformationProps) {
  // Sort properties: priority first, then alphabetically
  const sortedProperties = useMemo(() => {
    const entries = Object.entries(properties).filter(([key]) => !HIDDEN_PROPERTIES.includes(key));

    return entries.sort(([keyA], [keyB]) => {
      const priorityA = PRIORITY_PROPERTIES.indexOf(keyA);
      const priorityB = PRIORITY_PROPERTIES.indexOf(keyB);

      if (priorityA !== -1 && priorityB !== -1) return priorityA - priorityB;
      if (priorityA !== -1) return -1;
      if (priorityB !== -1) return 1;

      return keyA.localeCompare(keyB);
    });
  }, [properties]);

  // Get title from name or address
  const title = useMemo(() => {
    const name = properties.name ?? properties.Name;
    const address = properties.address ?? properties.Address;
    if (name) return String(name);
    if (address) return String(address).slice(0, 50) + (String(address).length > 50 ? "..." : "");
    return "Fiber Coverage Area";
  }, [properties]);

  return (
    <div
      className={cn(
        "rounded-xl shadow-xl bg-white/95 backdrop-blur-md",
        "border border-gray-200/50",
        "w-[340px] overflow-hidden",
        className,
      )}>
      {/* Header */}
      <div className="px-4 py-3 bg-gradient-to-r from-purple-500 to-pink-500">
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-2">
            <div className="w-8 h-8 rounded-lg bg-white/20 flex items-center justify-center">
              <Cable className="w-4 h-4 text-white" />
            </div>
            <div>
              <h3 className="text-white font-semibold text-sm">Fiber Coverage</h3>
              <p className="text-white/80 text-xs truncate max-w-[200px]">{title}</p>
            </div>
          </div>
          <Button
            variant="ghost"
            size="sm"
            onClick={() => popup.remove()}
            className="h-8 w-8 p-0 hover:bg-white/20 text-white">
            <X className="w-4 h-4" />
          </Button>
        </div>
      </div>

      {/* Content */}
      <ScrollArea className="max-h-[380px]">
        <div className="p-4">
          {sortedProperties.length > 0 ? (
            <div className="space-y-1 divide-y divide-gray-100">
              {sortedProperties.map(([key, value]) => (
                <PropertyRow key={key} propertyKey={key} value={value} isAdmin={isAdmin} />
              ))}
            </div>
          ) : (
            <div className="text-center py-8 text-gray-400">
              <Info className="w-8 h-8 mx-auto mb-2 opacity-50" />
              <p className="text-sm">No properties available</p>
            </div>
          )}
        </div>
      </ScrollArea>

      {/* Footer */}
      <Separator />
      <div className="px-4 py-2 bg-gray-50/50 flex items-center justify-between">
        <p className="text-xs text-gray-400">
          {sortedProperties.length} propert{sortedProperties.length !== 1 ? "ies" : "y"}
        </p>
        <Button
          variant="ghost"
          size="sm"
          onClick={() => popup.remove()}
          className="h-7 text-xs text-gray-500 hover:text-gray-700">
          Close
        </Button>
      </div>
    </div>
  );
});

export default FiberInformation;
