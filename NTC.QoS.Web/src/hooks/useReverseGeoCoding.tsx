"use client";

import { useCallback, useEffect, useState } from "react";
import * as maptilersdk from "@maptiler/sdk";

maptilersdk.config.apiKey = process.env.NEXT_PUBLIC_MAPTILER_API_KEY ?? "";

const placeTypes: maptilersdk.GeocodingOptions["types"] = [
  "address",
  "country",
  "region",
  "locality",
  "place",
  "postal_code",
  "municipality",
];

type PlaceTypeObject = Record<(typeof placeTypes)[number], string>;

export default function useReverseGeoCoding({ lng, lat }: { lng?: number; lat?: number }) {
  const [isLoading, setIsLoading] = useState(false);
  const [results, setResults] = useState<PlaceTypeObject>();

  const handleSearch = useCallback(async ([lng, lat]: [number, number]) => {
    if (!lat || !lng) {
      alert("Please enter valid coordinates.");
      return;
    }

    try {
      setIsLoading(true);
      const response = await maptilersdk.geocoding.reverse([lng, lat], { types: placeTypes });
      const location = response.features.reduce((acc, feature) => {
        const key = feature.id.split(".")[0] as keyof PlaceTypeObject;
        const value = feature.text;
        if (key) acc[key] = value;
        return acc;
      }, {} as PlaceTypeObject);
      setResults(location);
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    if (lat && lng) {
      void handleSearch([lng, lat]);
    }
  }, [handleSearch, lat, lng]);

  return { results, handleSearch, isLoading }; // Return the state and function to be used in the component
}
