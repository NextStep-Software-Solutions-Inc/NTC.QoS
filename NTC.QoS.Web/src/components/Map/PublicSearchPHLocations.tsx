"use client";

import { useCallback, useEffect, useState } from "react";
import { type GeocodingFeature, type GeoJSONSource, type LngLatBoundsLike, type Map } from "@maptiler/sdk";
import { MapPin, Search, X } from "lucide-react";

import { useGeocodingSearch } from "@/hooks/useGeocodingSearch";
import { useMap } from "@/hooks/useMap";
import { cn } from "@/lib/utils";

const PublicSearchPHLocations = () => {
  const { map, isMapLoaded } = useMap();
  const [isDropdownVisible, setIsDropdownVisible] = useState(false);
  const { results, query, setQuery, fetchResults, clearResults } = useGeocodingSearch();
  const [selectedIndex, setSelectedIndex] = useState(-1);

  const handleClickPlace = useCallback(
    async (feature: GeocodingFeature) => {
      await fetchResults(feature.id); // Set the input value to the selected place name
      setIsDropdownVisible(false);
    },
    [fetchResults],
  );

  useEffect(() => {
    if (!map || !isMapLoaded) return;

    const s = map.getSource<GeoJSONSource>("highlighted-feature");
    if (results.length > 0 && s) {
      const addBoundingBox = (coordinates: GeoJSON.Position[][]) => [
        [
          [-180, -90], // Outer bounding box (worldwide)
          [180, -90],
          [180, 90],
          [-180, 90],
          [-180, -90],
        ],
        ...coordinates,
      ];

      const updateMapWithFeature = (feature: GeocodingFeature, source: GeoJSONSource, map: Map) => {
        source.setData(feature);
        map.fitBounds(feature.bbox as LngLatBoundsLike);
      };

      const feature = { ...results[0]! }; // Create a copy to avoid mutating the original
      switch (feature.geometry.type) {
        case "Polygon":
          feature.geometry.coordinates = addBoundingBox(feature.geometry.coordinates);

          updateMapWithFeature(feature, s, map);
          break;

        case "MultiPolygon":
          feature.geometry.coordinates = feature.geometry.coordinates.map(addBoundingBox);

          updateMapWithFeature(feature, s, map);
          break;

        default:
          break;
      }
    } else {
      s?.setData({ type: "Feature", properties: {}, geometry: { type: "Polygon", coordinates: [[]] } });
    }
  }, [map, results, isMapLoaded]);

  const handleKeyDown = useCallback(
    async (e: React.KeyboardEvent<HTMLInputElement>) => {
      if (!isDropdownVisible || results.length === 0) return;

      if (e.key === "ArrowDown") {
        setSelectedIndex((prevIndex) => Math.min(prevIndex + 1, results.length - 1));
      } else if (e.key === "ArrowUp") {
        setSelectedIndex((prevIndex) => Math.max(prevIndex - 1, 0));
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
      setIsDropdownVisible(true); // Show dropdown when typing
      setSelectedIndex(-1);
    },
    [setQuery],
  );

  useEffect(() => {
    if (!map || !isMapLoaded) return;
    const addSourceAndLayers = () => {
      if (!map.getSource("highlighted-feature")) {
        console.info("Adding highlighted-feature source");
        map.addSource("highlighted-feature", {
          type: "geojson",
          data: { type: "Feature", properties: {}, geometry: { type: "Polygon", coordinates: [[]] } },
        });
      }

      if (!map.getLayer("highlighted-feature")) {
        console.info("Adding highlighted-feature layer");
        map.addLayer({
          id: "highlighted-feature",
          type: "line",
          source: "highlighted-feature",
          paint: { "line-dasharray": [1, 1], "line-width": 2, "line-color": "#00f" },
        });
      }
      if (!map.getLayer("dimmed-area")) {
        console.info("Adding dimmed-area layer");
        map.addLayer({
          id: "dimmed-area",
          type: "fill",
          source: "highlighted-feature",
          paint: {
            "fill-color": "rgba(0, 0, 0, 0.3)", // Semi-transparent black to dim the outer area
            "fill-opacity": 0.3,
          },
        });
      }
    };
    if (isMapLoaded) addSourceAndLayers();
  }, [map, isMapLoaded]);

  if (!open) return;

  return (
    <div className="mx-2 flex flex-col space-x-2">
      <div className={cn("mx-1 flex items-center rounded-md border px-2 py-2")}>
        {!!query && <Search size={16} className={cn("mr-1 text-gray-500")} />}
        <input
          type="text"
          placeholder="Enter Location..."
          className="w-full text-[13px] outline-none bg-transparent"
          value={query}
          onChange={handleChange}
          onFocus={() => setIsDropdownVisible(true)} // Show dropdown on focus
          onKeyDown={handleKeyDown}
        />
        {!query ? <Search className={cn("text-gray-300")} /> : <X onClick={clearResults} />}
      </div>
      {/* Autocomplete dropdown */}
      {isDropdownVisible && query && results?.length > 0 && (
        <ul className="rounded m-2 shadow-lg border ">
          {results.map((feature, idx) => (
            <li
              key={idx}
              className={cn("cursor-pointer", "flex items-center px-3 py-2 text-sm", "hover:bg-gray-100", {
                "bg-gray-100 text-primary": selectedIndex === idx,
              })}
              onClick={() => handleClickPlace(feature)}>
              <MapPin size={18} className={cn("")} />
              <div className="ml-2 flex-1">
                <div className="font-bold">{feature.place_name}</div>
                <div className="text-xs text-gray-500">{feature.place_type}</div>
              </div>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
};

export default PublicSearchPHLocations;
