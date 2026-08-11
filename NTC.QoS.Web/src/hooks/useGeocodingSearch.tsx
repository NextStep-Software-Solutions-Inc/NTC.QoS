"use client";

import { useCallback, useEffect, useState } from "react";
import * as maptilersdk from "@maptiler/sdk";

export const useGeocodingSearch = () => {
  const [results, setResults] = useState<maptilersdk.GeocodingFeature[]>([]);
  const [loading, setLoading] = useState(false);
  const [query, setQuery] = useState("");

  const clearResults = useCallback(() => {
    setResults([]);
    setQuery("");
  }, []);

  const fetchResults = useCallback(async (query: string) => {
    setLoading(true);
    const res = await maptilersdk.geocoding.forward(query, {
      types: ["place", "locality", "municipality", "region", "country"],
      country: ["ph"],
      limit: 10,
      autocomplete: true,
    });
    const result = res.features.filter((feature) => feature.place_name !== "");
    setResults(result);
    setLoading(false);
    return result;
  }, []);

  useEffect(() => {
    if (!query.trim()) {
      return;
    }
    const timeout = setTimeout(() => void fetchResults(query), 300); // debounce
    return () => clearTimeout(timeout);
  }, [fetchResults, query]);

  return { results, loading, query, setQuery, fetchResults, clearResults };
};
