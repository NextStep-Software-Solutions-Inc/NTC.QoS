/**
 * Search Bar Component
 *
 * Floating search bar with autocomplete for locations, barangays, cellsites, and NAPs.
 * Uses enhanced geocoding hook with support for barangay search from backend API.
 */

"use client";

import { memo, useCallback, useEffect, useRef, useState } from "react";
import * as maptilersdk from "@maptiler/sdk";
import { type GeocodingFeature, type GeoJSONSource, type LngLatBoundsLike, type Map } from "@maptiler/sdk";
import { AnimatePresence, motion } from "framer-motion";
import { History, Loader2, MapPin, Search, X, XCircle } from "lucide-react";
import { createPortal } from "react-dom";

import { useEnhancedGeocodingSearch, type EnhancedSearchResult } from "@/hooks/useEnhancedGeocodingSearch";
import { useMap } from "@/hooks/useMap";
import { cn } from "@/lib/utils";
import { GlassPanel } from "../ui/GlassPanel";
import { request } from "@/services/fetch";
import { APIResponse } from "@/types/reusable/api";
import { Barangay } from "@/components/features/barangay/types/Barangay";

interface SearchBarProps {
  className?: string;
  placeholder?: string;
  onLocationSelect?: (feature: GeocodingFeature) => void;
}

export const SearchBar = memo(function SearchBar({
  className,
  placeholder = "Search locations, barangays, cellsites...",
  onLocationSelect,
}: SearchBarProps) {
  const { map, isMapLoaded } = useMap();
  const [isDropdownVisible, setIsDropdownVisible] = useState(false);
  const [isFocused, setIsFocused] = useState(false);
  const { results, query, setQuery, fetchResults, clearResults, loading: isLoading } = useEnhancedGeocodingSearch();
  const [selectedIndex, setSelectedIndex] = useState(-1);
  const [recentSearches, setRecentSearches] = useState<EnhancedSearchResult[]>([]);
  const [hasHighlight, setHasHighlight] = useState(false);
  const inputRef = useRef<HTMLInputElement>(null);
  const containerRef = useRef<HTMLDivElement>(null);

  // Load recent searches from localStorage
  useEffect(() => {
    const saved = localStorage.getItem("adminMapRecentSearches");
    if (saved) {
      try {
        setRecentSearches(JSON.parse(saved));
      } catch {
        // Invalid JSON, ignore
      }
    }
  }, []);

  // Save to recent searches
  const saveToRecent = useCallback((result: EnhancedSearchResult) => {
    setRecentSearches((prev) => {
      const getId = (r: EnhancedSearchResult) =>
        r.type === "maptiler" ? r.feature.id : r.barangay.id;

      const filtered = prev.filter((r) => getId(r) !== getId(result));
      const updated = [result, ...filtered].slice(0, 5); // Keep last 5
      localStorage.setItem("adminMapRecentSearches", JSON.stringify(updated));
      return updated;
    });
  }, []);

  const clearHighlight = useCallback(() => {
    const s = map?.getSource<GeoJSONSource>("highlighted-feature");
    if (s) {
      s.setData({ type: "Feature", properties: {}, geometry: { type: "Polygon", coordinates: [[]] } });
      setHasHighlight(false);
    }
  }, [map]);

  const handleClickPlace = useCallback(
    async (result: EnhancedSearchResult) => {
      if (result.type === "maptiler") {
        // Handle MapTiler geocoding results - fetch full feature details
        try {
          const fullFeature = await maptilersdk.geocoding.forward(result.feature.id, {
            limit: 1,
          });

          const feature = fullFeature.features[0] || result.feature;
          onLocationSelect?.(feature);

          // Update map with the selected feature
          const s = map?.getSource<GeoJSONSource>("highlighted-feature");
          if (s && map) {
            const addBoundingBox = (coordinates: GeoJSON.Position[][]) => [
              [
                [-180, -90],
                [180, -90],
                [180, 90],
                [-180, 90],
                [-180, -90],
              ],
              ...coordinates,
            ];

            const featureCopy = { ...feature } as GeocodingFeature;
            const geometry = feature.geometry;

            switch (geometry.type) {
              case "Polygon":
                if ('coordinates' in geometry) {
                  featureCopy.geometry = {
                    ...geometry,
                    coordinates: addBoundingBox(geometry.coordinates),
                  };
                  s.setData(featureCopy);
                  setHasHighlight(true);
                  map.fitBounds(feature.bbox as LngLatBoundsLike, { padding: 50 });
                }
                break;
              case "MultiPolygon":
                if ('coordinates' in geometry) {
                  featureCopy.geometry = {
                    ...geometry,
                    coordinates: geometry.coordinates.map(addBoundingBox),
                  };
                  s.setData(featureCopy);
                  setHasHighlight(true);
                  map.fitBounds(feature.bbox as LngLatBoundsLike, { padding: 50 });
                }
                break;
              case "Point":
                // For point geometries, just fly to the location without boundary highlighting
                if (feature.bbox) {
                  map.fitBounds(feature.bbox as LngLatBoundsLike, { padding: 100, maxZoom: 15 });
                } else if ('coordinates' in geometry) {
                  const coords = geometry.coordinates as [number, number];
                  map.flyTo({ center: coords, zoom: 14, duration: 1000 });
                }
                break;
              default:
                break;
            }
          }
        } catch (error) {
          console.error("Failed to fetch MapTiler feature details:", error);
          // Fallback to using the original feature
          onLocationSelect?.(result.feature);
        }
      } else {
        // For barangay, fetch its boundaries and display
        try {
          const response = await request.get<APIResponse<Barangay>>(`/barangays/${result.barangay.id}`);
          if (response.succeeded && response.data?.boundaries) {
            const barangay = response.data;
            const boundaries = barangay.boundaries;

            // Update map with barangay boundaries (boundaries is already a GeoJSON Feature)
            const s = map?.getSource<GeoJSONSource>("highlighted-feature");
            if (s && map && boundaries) {
              // Apply inverse mask transformation
              const addBoundingBox = (coordinates: GeoJSON.Position[][]) => [
                [
                  [-180, -90],
                  [180, -90],
                  [180, 90],
                  [-180, 90],
                  [-180, -90],
                ],
                ...coordinates,
              ];

              const boundariesCopy: typeof boundaries = { ...boundaries };
              if (boundaries.geometry.type === "MultiPolygon") {
                boundariesCopy.geometry = {
                  type: "MultiPolygon",
                  coordinates: boundaries.geometry.coordinates.map(addBoundingBox),
                };
              } else if (boundaries.geometry.type === "Polygon") {
                // Convert Polygon to MultiPolygon to apply inverse mask
                const worldBox: GeoJSON.Position[][] = [
                  [
                    [-180, -90],
                    [180, -90],
                    [180, 90],
                    [-180, 90],
                    [-180, -90],
                  ],
                ];
                boundariesCopy.geometry = {
                  type: "MultiPolygon",
                  coordinates: [worldBox, boundaries.geometry.coordinates] as GeoJSON.Position[][][],
                };
              }

              s.setData(boundariesCopy);
              setHasHighlight(true);

              // Calculate bounds from MultiPolygon coordinates
              // MultiPolygon structure: [polygon][ring][position][lng, lat]
              const coords = boundaries.geometry.coordinates.flat(3); // Flatten to get all [lng, lat] pairs
              const lngs = coords.filter((_, i) => i % 2 === 0);
              const lats = coords.filter((_, i) => i % 2 === 1);
              const bounds: LngLatBoundsLike = [
                [Math.min(...lngs), Math.min(...lats)],
                [Math.max(...lngs), Math.max(...lats)],
              ];

              map.fitBounds(bounds, { padding: 100, maxZoom: 15 });
            }
          }
        } catch (error) {
          console.error("Failed to fetch barangay boundaries:", error);
        }
      }
      setIsDropdownVisible(false);
      inputRef.current?.blur();
      saveToRecent(result);
    },
    [fetchResults, saveToRecent, onLocationSelect, map],
  );

  const handleKeyDown = useCallback(
    async (e: React.KeyboardEvent<HTMLInputElement>) => {
      if (e.key === "Escape") {
        setIsDropdownVisible(false);
        inputRef.current?.blur();
        return;
      }

      const items = results.length > 0 ? results : recentSearches;
      if (!isDropdownVisible || items.length === 0) return;

      if (e.key === "ArrowDown") {
        e.preventDefault();
        setSelectedIndex((prev) => Math.min(prev + 1, items.length - 1));
      } else if (e.key === "ArrowUp") {
        e.preventDefault();
        setSelectedIndex((prev) => Math.max(prev - 1, 0));
      } else if (e.key === "Enter") {
        if (selectedIndex >= 0 && selectedIndex < items.length) {
          const item = items[selectedIndex];
          if (item) {
            await handleClickPlace(item);
          }
        }
      }
    },
    [handleClickPlace, isDropdownVisible, results, recentSearches, selectedIndex],
  );

  const handleChange = useCallback(
    (e: React.ChangeEvent<HTMLInputElement>) => {
      setQuery(e.target.value);
      setIsDropdownVisible(true);
      setSelectedIndex(-1);
    },
    [setQuery],
  );

  const handleClear = useCallback(() => {
    clearResults();
    inputRef.current?.focus();
  }, [clearResults]);

  // Initialize map layers for highlighted features with enhanced styling
  useEffect(() => {
    if (!map || !isMapLoaded || !map.isStyleLoaded()) return;

    const addSourceAndLayers = () => {
      if (!map || !map.isStyleLoaded()) return;
      if (!map.getSource("highlighted-feature")) {
        map.addSource("highlighted-feature", {
          type: "geojson",
          data: { type: "Feature", properties: {}, geometry: { type: "Polygon", coordinates: [[]] } },
        });
      }

      if (!map.getLayer("highlighted-feature-fill")) {
        map.addLayer({
          id: "highlighted-feature-fill",
          type: "fill",
          source: "highlighted-feature",
          paint: {
            "fill-color": "#000000",
            "fill-opacity": 0.4,
            "fill-antialias": true
          },
        });
      }
      if (!map.getLayer("highlighted-feature-line")) {
        map.addLayer({
          id: "highlighted-feature-line",
          type: "line",
          source: "highlighted-feature",
          paint: {
            "line-dasharray": [2, 2],
            "line-width": 2,
            "line-color": "#3b82f6",
            "line-opacity": 1
          },
        });
      }
      // Add an outer glow effect for more emphasis
      if (!map.getLayer("highlighted-feature-glow")) {
        map.addLayer({
          id: "highlighted-feature-glow",
          type: "line",
          source: "highlighted-feature",
          paint: {
            "line-width": 4,
            "line-color": "#3b82f6",
            "line-opacity": 0.3,
            "line-blur": 3
          },
        }, "highlighted-feature-line");
      }
    };

    addSourceAndLayers();
    map.on("styledata", addSourceAndLayers);

    return () => {
      map.off("styledata", addSourceAndLayers);
    };
  }, [map, isMapLoaded]);

  // Keyboard shortcut to focus search
  useEffect(() => {
    const handleGlobalKeyDown = (e: KeyboardEvent) => {
      if ((e.key === "/" || (e.ctrlKey && e.key === "k")) && !e.metaKey) {
        const target = e.target as HTMLElement;
        if (target.tagName !== "INPUT" && target.tagName !== "TEXTAREA") {
          e.preventDefault();
          inputRef.current?.focus();
        }
      }
    };

    document.addEventListener("keydown", handleGlobalKeyDown);
    return () => document.removeEventListener("keydown", handleGlobalKeyDown);
  }, []);

  const showDropdown = isDropdownVisible && (results.length > 0 || (query === "" && recentSearches.length > 0));

  return (
    <div ref={containerRef} className={cn("relative w-full max-w-md", className)}>
      <GlassPanel
        padding="none"
        className={cn(
          "flex items-center gap-2 px-4 py-2.5 transition-all duration-300",
          isFocused && "ring-2 ring-primary/50 shadow-xl",
        )}>
        {isLoading ? (
          <Loader2 className="h-5 w-5 text-primary animate-spin shrink-0" />
        ) : (
          <Search className="h-5 w-5 text-gray-400 shrink-0" />
        )}
        <input
          ref={inputRef}
          type="text"
          placeholder={placeholder}
          className="flex-1 bg-transparent text-sm text-gray-700 placeholder:text-gray-400 outline-none min-w-0"
          value={query}
          onChange={handleChange}
          onFocus={() => {
            setIsFocused(true);
            setIsDropdownVisible(true);
          }}
          onBlur={() => {
            setIsFocused(false);
            setTimeout(() => setIsDropdownVisible(false), 200);
          }}
          onKeyDown={handleKeyDown}
          aria-label="Search locations"
          aria-expanded={showDropdown}
          aria-autocomplete="list"
        />
        <div className="flex items-center gap-1 shrink-0">
          {hasHighlight && (
            <motion.button
              initial={{ opacity: 0, scale: 0.8 }}
              animate={{ opacity: 1, scale: 1 }}
              exit={{ opacity: 0, scale: 0.8 }}
              onClick={clearHighlight}
              className="p-1 rounded-lg hover:bg-gray-100 transition-colors"
              title="Clear highlight">
              <XCircle className="h-4 w-4 text-gray-400" />
            </motion.button>
          )}
          {query && (
            <motion.button
              initial={{ opacity: 0, scale: 0.8 }}
              animate={{ opacity: 1, scale: 1 }}
              exit={{ opacity: 0, scale: 0.8 }}
              onClick={handleClear}
              className="p-1 rounded-lg hover:bg-gray-100 transition-colors">
              <X className="h-4 w-4 text-gray-400" />
            </motion.button>
          )}
          <kbd className="hidden sm:flex px-2 py-0.5 rounded-md bg-gray-100 text-xs text-gray-500 font-medium">/</kbd>
        </div>
      </GlassPanel>

      {/* Dropdown results - rendered via portal to avoid ScrollArea stacking context */}
      {showDropdown &&
        createPortal(
          <AnimatePresence>
            <motion.div
              initial={{ opacity: 0, y: -10, scale: 0.98 }}
              animate={{ opacity: 1, y: 0, scale: 1 }}
              exit={{ opacity: 0, y: -10, scale: 0.98 }}
              transition={{ duration: 0.15 }}
              className="fixed z-[10000] w-full max-w-md"
              style={{
                top: containerRef.current ? containerRef.current.getBoundingClientRect().bottom + 8 : 0,
                left: containerRef.current ? containerRef.current.getBoundingClientRect().left : 0,
                width: containerRef.current ? containerRef.current.getBoundingClientRect().width : "auto",
              }}>
              <GlassPanel padding="sm" className="max-h-80 overflow-y-auto">
                {/* Recent searches (when no query) */}
                {query === "" && recentSearches.length > 0 && (
                  <div className="mb-2">
                    <div className="flex items-center gap-2 px-2 py-1 text-xs text-gray-500 font-medium uppercase tracking-wide">
                      <History className="w-3 h-3" />
                      Recent Searches
                    </div>
                    {recentSearches.map((result, index) => (
                      <SearchResultItem
                        key={result.type === "maptiler" ? result.feature.id : result.barangay.id}
                        result={result}
                        isSelected={selectedIndex === index}
                        onClick={() => handleClickPlace(result)}
                      />
                    ))}
                  </div>
                )}

                {/* Search results - separated by type */}
                {results.length > 0 && (() => {
                  const barangayResults = results.filter(r => r.type === "barangay");
                  const maptilerResults = results.filter(r => r.type === "maptiler");

                  return (
                    <>
                      {/* Barangay results section */}
                      {barangayResults.length > 0 && (
                        <div className="mb-2">
                          <div className="px-2 py-1 text-xs text-gray-500 font-medium uppercase tracking-wide">
                            Barangays
                          </div>
                          {barangayResults.map((result, index) => (
                            <SearchResultItem
                              key={result.barangay.id}
                              result={result}
                              isSelected={selectedIndex === index}
                              onClick={() => handleClickPlace(result)}
                            />
                          ))}
                        </div>
                      )}

                      {/* MapTiler results section */}
                      {maptilerResults.length > 0 && (
                        <div>
                          <div className="px-2 py-1 text-xs text-gray-500 font-medium uppercase tracking-wide">
                            Municipalities & Regions
                          </div>
                          {maptilerResults.map((result, index) => {
                            const adjustedIndex = barangayResults.length + index;
                            return (
                              <SearchResultItem
                                key={result.feature.id}
                                result={result}
                                isSelected={selectedIndex === adjustedIndex}
                                onClick={() => handleClickPlace(result)}
                              />
                            );
                          })}
                        </div>
                      )}
                    </>
                  );
                })()}

                {/* No results */}
                {query && results.length === 0 && !isLoading && (
                  <div className="px-4 py-8 text-center text-gray-500">
                    <Search className="w-8 h-8 mx-auto mb-2 text-gray-300" />
                    <p className="text-sm">No locations found</p>
                    <p className="text-xs text-gray-400 mt-1">Try a different search term</p>
                  </div>
                )}
              </GlassPanel>
            </motion.div>
          </AnimatePresence>,
          document.body,
        )}
    </div>
  );
});

// Search result item component
interface SearchResultItemProps {
  result: EnhancedSearchResult;
  isSelected: boolean;
  onClick: () => void;
}

function SearchResultItem({ result, isSelected, onClick }: SearchResultItemProps) {
  // Determine icon and display text based on result type
  const getDisplayInfo = () => {
    if (result.type === "maptiler") {
      const placeType = result.feature.place_type?.[0] ?? "";
      const name = result.feature.place_name?.split(",")[0] ?? "Unknown";
      const subtitle = result.feature.place_name?.split(",").slice(1).join(",").trim() ?? "";
      return { icon: <MapPin className="w-4 h-4" />, name, subtitle };
    } else {
      const name = result.barangay.name;
      const parts = [
        result.barangay.municipality?.name,
        result.barangay.municipality?.province?.name,
      ].filter(Boolean);
      const subtitle = parts.join(", ");
      return { icon: <MapPin className="w-4 h-4" />, name: `Brgy. ${name}`, subtitle };
    }
  };

  const { icon, name, subtitle } = getDisplayInfo();

  return (
    <button
      onClick={onClick}
      className={cn(
        "w-full flex items-center gap-3 px-3 py-2.5 rounded-xl text-left",
        "transition-colors duration-150",
        "hover:bg-gray-100/60 focus:outline-none focus:bg-gray-100/60",
        isSelected && "bg-primary/5",
      )}>
      <div
        className={cn(
          "w-8 h-8 rounded-lg flex items-center justify-center shrink-0",
          isSelected ? "bg-primary/10 text-primary" : "bg-gray-100 text-gray-500",
        )}>
        {icon}
      </div>
      <div className="flex-1 min-w-0">
        <span className={cn("text-sm font-medium block truncate", isSelected ? "text-primary" : "text-gray-800")}>
          {name}
        </span>
        <span className="text-xs text-gray-400 block truncate">
          {subtitle}
        </span>
      </div>
    </button>
  );
}
