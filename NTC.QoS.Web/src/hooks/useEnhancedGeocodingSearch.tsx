"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { useQueries } from "@tanstack/react-query";
import * as maptilersdk from "@maptiler/sdk";
import { Barangay } from "@/components/features/barangay/types/Barangay";
import { getBarangaysRequest } from "@/components/features/barangay/handlers/getBarangaysRequest";
import { buildEndpoint } from "@/lib/endpoint-builder";
import { PaginationResponse } from "@/types/reusable/api";

export type EnhancedSearchResult =
  | { type: "maptiler"; feature: maptilersdk.GeocodingFeature }
  | { type: "barangay"; barangay: Barangay };

export const useEnhancedGeocodingSearch = () => {
  const [query, setQuery] = useState("");
  const [debouncedQuery, setDebouncedQuery] = useState("");

  // Debounce the query
  useEffect(() => {
    const timeout = setTimeout(() => setDebouncedQuery(query), 300);
    return () => clearTimeout(timeout);
  }, [query]);

  // Use React Query's useQueries for parallel data fetching
  const queries = useQueries({
    queries: [
      // MapTiler geocoding query
      {
        queryKey: ["maptiler-geocoding", debouncedQuery] as const,
        queryFn: async () => {
          if (!debouncedQuery.trim()) return { features: [] };
          return await maptilersdk.geocoding.forward(debouncedQuery, {
            types: ["place", "locality", "municipality", "region", "country"],
            country: ["ph"],
            limit: 5,
            autocomplete: true,
          });
        },
        enabled: Boolean(debouncedQuery.trim()),
        staleTime: 5 * 60 * 1000, // 5 minutes
      },
      // Barangay search query using the request handler
      {
        queryKey: getBarangaysRequest.key({
          url: buildEndpoint(getBarangaysRequest.endpoint, {}, { name: debouncedQuery, pageSize: 5, pageNumber: 1 }),
          pathParams: {},
          queryParams: { name: debouncedQuery, pageSize: 5, pageNumber: 1 },
        }),
        queryFn: async ({ signal }) => {
          if (!debouncedQuery.trim()) {
            return {
              data: [],
              succeeded: true,
              message: "",
              errors: {},
              pageNumber: 1,
              pageSize: 5,
              totalPageNumber: 0,
              loadMore: false,
              totalRows: 0,
            } as PaginationResponse<Barangay>;
          }
          const finalUrl = buildEndpoint(getBarangaysRequest.endpoint, {}, { name: debouncedQuery, pageSize: 5, pageNumber: 1 });
          return await getBarangaysRequest.fetcher({ finalUrl, signal });
        },
        enabled: Boolean(debouncedQuery.trim()),
        staleTime: 5 * 60 * 1000, // 5 minutes
      },
    ],
  });

  const [maptilerQuery, barangayQuery] = queries;

  // Combine and memoize results
  const results = useMemo(() => {
    if (!debouncedQuery.trim()) return [];

    const maptilerResults: EnhancedSearchResult[] = (maptilerQuery.data?.features || [])
      .filter((feature) => feature.place_name !== "")
      .map((feature) => ({ type: "maptiler" as const, feature }));

    const barangayResults: EnhancedSearchResult[] = (barangayQuery.data?.data || [])
      .map((barangay) => ({ type: "barangay" as const, barangay }));

    // Combine results: barangays first, then maptiler
    return [...barangayResults, ...maptilerResults];
  }, [debouncedQuery, maptilerQuery.data, barangayQuery.data]);

  const loading = maptilerQuery.isLoading || barangayQuery.isLoading;

  const clearResults = useCallback(() => {
    setQuery("");
    setDebouncedQuery("");
  }, []);

  const fetchResults = useCallback(async (searchQuery: string) => {
    setQuery(searchQuery);
    // Results will be automatically fetched by React Query when debouncedQuery updates
    return [];
  }, []);

  return { results, loading, query, setQuery, fetchResults, clearResults };
};
