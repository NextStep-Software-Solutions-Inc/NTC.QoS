"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import * as maptilersdk from "@maptiler/sdk";
import { type GeocodingFeature, type GeoJSONSource, type LngLatBoundsLike, type Map } from "@maptiler/sdk";
import { AnimatePresence, motion } from "framer-motion";
import { Loader2, MapPin, Search, X, XCircle } from "lucide-react";

import { useEnhancedGeocodingSearch, type EnhancedSearchResult } from "@/hooks/useEnhancedGeocodingSearch";
import { useMap } from "@/hooks/useMap";
import { cn } from "@/lib/utils";
import { GlassPanel } from "./GlassPanel";
import { request } from "@/services/fetch";
import { APIResponse } from "@/types/reusable/api";
import { Barangay } from "@/components/features/barangay/types/Barangay";

export function FloatingSearch() {
  const { map, isMapLoaded } = useMap();
  const [isDropdownVisible, setIsDropdownVisible] = useState(false);
  const [isFocused, setIsFocused] = useState(false);
  const { results, query, setQuery, fetchResults, clearResults, loading: isLoading } = useEnhancedGeocodingSearch();
  const [selectedIndex, setSelectedIndex] = useState(-1);
  const [hasHighlight, setHasHighlight] = useState(false);
  const inputRef = useRef<HTMLInputElement>(null);

  const handleClickPlace = useCallback(
    async (result: EnhancedSearchResult) => {
      if (result.type === "maptiler") {
        // Handle MapTiler geocoding results - fetch full feature details
        try {
          const fullFeature = await maptilersdk.geocoding.forward(result.feature.id, {
            limit: 1,
          });

          const feature = fullFeature.features[0] || result.feature;

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
    },
    [fetchResults, map],
  );

  const clearHighlight = useCallback(() => {
    const s = map?.getSource<GeoJSONSource>("highlighted-feature");
    if (s) {
      s.setData({ type: "Feature", properties: {}, geometry: { type: "Polygon", coordinates: [[]] } });
      setHasHighlight(false);
    }
  }, [map]);

  const handleKeyDown = useCallback(
    async (e: React.KeyboardEvent<HTMLInputElement>) => {
      if (e.key === "Escape") {
        setIsDropdownVisible(false);
        inputRef.current?.blur();
        return;
      }

      if (!isDropdownVisible || results.length === 0) return;

      if (e.key === "ArrowDown") {
        e.preventDefault();
        setSelectedIndex((prev) => Math.min(prev + 1, results.length - 1));
      } else if (e.key === "ArrowUp") {
        e.preventDefault();
        setSelectedIndex((prev) => Math.max(prev - 1, 0));
      } else if (e.key === "Enter") {
        if (selectedIndex >= 0 && selectedIndex < results.length) {
          if (results[selectedIndex]) {
            await handleClickPlace(results[selectedIndex]);
          }
        }
      }
    },
    [handleClickPlace, isDropdownVisible, results, selectedIndex],
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
    if (!map || !isMapLoaded) return;

    const addSourceAndLayers = () => {
      if (!map.getSource("highlighted-feature")) {
        map.addSource("highlighted-feature", {
          type: "geojson",
          data: { type: "Feature", properties: {}, geometry: { type: "Polygon", coordinates: [[]] } },
        });
      }

      // Add dimmed fill for inverse mask effect (dims everything except selected area)
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
      if (!map.getLayer("highlighted-feature")) {
        map.addLayer({
          id: "highlighted-feature",
          type: "line",
          source: "highlighted-feature",
          paint: {
            "line-dasharray": [1, 1],
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
        }, "highlighted-feature");
      }
    };
    if (isMapLoaded) addSourceAndLayers();
  }, [map, isMapLoaded]);

  // Keyboard shortcut to focus search
  useEffect(() => {
    const handleGlobalKeyDown = (e: KeyboardEvent) => {
      if (e.key === "/" && !e.ctrlKey && !e.metaKey) {
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

  return (
    <div className="relative w-full max-w-md">
      <GlassPanel
        className={cn(
          "flex items-center gap-2 px-4 py-2 transition-all duration-300",
          isFocused && "ring-2 ring-primary/50 shadow-lg",
        )}>
        {isLoading ? (
          <Loader2 className="h-5 w-5 text-primary animate-spin" />
        ) : (
          <Search className="h-5 w-5 text-gray-400" />
        )}
        <input
          ref={inputRef}
          type="text"
          placeholder="Search locations, barangays... (press /)"
          className="flex-1 bg-transparent text-sm text-gray-700 placeholder:text-gray-400 outline-none"
          value={query}
          onChange={handleChange}
          onFocus={() => {
            setIsFocused(true);
            if (query) setIsDropdownVisible(true);
          }}
          onBlur={() => {
            setIsFocused(false);
            // Delay hiding to allow click on results
            setTimeout(() => setIsDropdownVisible(false), 200);
          }}
          onKeyDown={handleKeyDown}
          aria-label="Search locations"
          aria-expanded={isDropdownVisible && results.length > 0}
          aria-autocomplete="list"
        />
        <AnimatePresence>
          {hasHighlight && (
            <motion.button
              initial={{ opacity: 0, scale: 0.8 }}
              animate={{ opacity: 1, scale: 1 }}
              exit={{ opacity: 0, scale: 0.8 }}
              onClick={clearHighlight}
              className="p-1.5 rounded-lg hover:bg-white/60 transition-colors"
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
              className="p-1 hover:bg-gray-200/50 rounded-full transition-colors"
              aria-label="Clear search">
              <X className="h-4 w-4 text-gray-500" />
            </motion.button>
          )}
        </AnimatePresence>
      </GlassPanel>

      {/* Autocomplete dropdown */}
      <AnimatePresence>
        {isDropdownVisible && query && results.length > 0 && (
          <motion.div
            initial={{ opacity: 0, y: -10, scale: 0.95 }}
            animate={{ opacity: 1, y: 0, scale: 1 }}
            exit={{ opacity: 0, y: -10, scale: 0.95 }}
            transition={{ duration: 0.2 }}
            className="absolute top-full mt-2 w-full z-50">
            <GlassPanel className="py-2 max-h-80 overflow-y-auto">
              {(() => {
                const barangayResults = results.filter(r => r.type === "barangay");
                const maptilerResults = results.filter(r => r.type === "maptiler");

                return (
                  <>
                    {/* Barangay results section */}
                    {barangayResults.length > 0 && (
                      <div className="mb-2">
                        <div className="px-4 py-1 text-xs text-gray-500 font-medium uppercase tracking-wide">
                          Barangays
                        </div>
                        <ul role="listbox" aria-label="Barangay results">
                          {barangayResults.map((result, idx) => {
                            const name = `Brgy. ${result.barangay.name}`;
                            const parts = [
                              result.barangay.municipality?.name,
                              result.barangay.municipality?.province?.name,
                            ].filter(Boolean);
                            const type = parts.join(", ");

                            return (
                              <li
                                key={result.barangay.id}
                                role="option"
                                aria-selected={selectedIndex === idx}
                                className={cn(
                                  "cursor-pointer flex items-center px-4 py-2.5 transition-colors",
                                  "hover:bg-primary/10",
                                  selectedIndex === idx && "bg-primary/10",
                                )}
                                onClick={() => handleClickPlace(result)}
                                onMouseEnter={() => setSelectedIndex(idx)}>
                                <MapPin className="h-4 w-4 text-primary shrink-0" />
                                <div className="ml-3 flex-1 min-w-0">
                                  <p className="text-sm font-medium text-gray-800 truncate">{name}</p>
                                  <p className="text-xs text-gray-500 capitalize truncate">{type}</p>
                                </div>
                              </li>
                            );
                          })}
                        </ul>
                      </div>
                    )}

                    {/* MapTiler results section */}
                    {maptilerResults.length > 0 && (
                      <div>
                        <div className="px-4 py-1 text-xs text-gray-500 font-medium uppercase tracking-wide">
                          Municipalities & Regions
                        </div>
                        <ul role="listbox" aria-label="Municipality and region results">
                          {maptilerResults.map((result, idx) => {
                            const adjustedIndex = barangayResults.length + idx;
                            const name = result.feature.place_name;
                            const type = result.feature.place_type;

                            return (
                              <li
                                key={result.feature.id}
                                role="option"
                                aria-selected={selectedIndex === adjustedIndex}
                                className={cn(
                                  "cursor-pointer flex items-center px-4 py-2.5 transition-colors",
                                  "hover:bg-primary/10",
                                  selectedIndex === adjustedIndex && "bg-primary/10",
                                )}
                                onClick={() => handleClickPlace(result)}
                                onMouseEnter={() => setSelectedIndex(adjustedIndex)}>
                                <MapPin className="h-4 w-4 text-primary shrink-0" />
                                <div className="ml-3 flex-1 min-w-0">
                                  <p className="text-sm font-medium text-gray-800 truncate">{name}</p>
                                  <p className="text-xs text-gray-500 capitalize truncate">{type}</p>
                                </div>
                              </li>
                            );
                          })}
                        </ul>
                      </div>
                    )}
                  </>
                );
              })()}
            </GlassPanel>
          </motion.div>
        )}
      </AnimatePresence>
    </div>
  );
}
