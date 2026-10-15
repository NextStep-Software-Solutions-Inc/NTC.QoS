/**
 * Admin Map Context Provider
 *
 * Centralized state management for the admin map following Dependency Inversion principle.
 * All map state flows through this context, making components loosely coupled.
 *
 * Features:
 * - URL-based layer visibility persistence
 * - Provider filtering with API integration
 * - Opacity controls for all layers
 */

"use client";

import React, { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from "react";
import { useParams, useSearchParams } from "next/navigation";
import { useUser } from "@clerk/nextjs";

import { getAuthUserRequest } from "@/components/features/users/handlers/getAuthUserRequest";
import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";
import {
  DEFAULT_ADMIN_MAP_FILTERS,
  type AdminMapContextValue,
  type AdminMapFilters,
  type LayerOpacity,
  type LayerState,
  type LayerVisibility,
} from "../types";

const useAuthUser = createQueryHookFromTemplate(getAuthUserRequest);

// Layer URL param mapping - maps layer IDs to their URL param keys
const LAYER_URL_PARAMS: Record<string, string> = {
  cellular: "cellular",
  "cellular-radius": "cellular",
  "cellular-radius-stroke": "cellular",
  "cellular-signal": "cellular_coverage",
  "signal-coverage-circles": "cellular_coverage",
  "cellular-heatmap": "cellular_heatmap", // Separate param for heatmap
  naps: "naps",
  "NAPs-cluster": "naps",
  "NAPs-cluster-count": "naps",
  "NAPs-unclustered": "naps",
  fiber: "fiber",
  "fiber-line": "fiber",
  "highlighted-fiber": "fiber",
  "speedtests-cellular": "speedtests-cellular",
  "speedtests-circles": "speedtests-cellular",
  "speedtests-heatmap": "speedtests-cellular",
};

// Sub-layer hierarchy mapping - maps parent layer IDs to their constituent map layers
export const LAYER_SUB_LAYERS: Record<string, string[]> = {
  cellular: ["cells-clusters", "cells-cluster-count", "cells-circles", "cells-symbols", "cellular-radius", "cellular-radius-stroke"],
  "cellular-heatmap": ["cells-heatmap"],
  "cellular-signal": ["signal-coverage-circles"],
  naps: ["NAPs-cluster", "NAPs-cluster-count", "NAPs-unclustered"],
  fiber: ["fiber-line", "highlighted-fiber"],
  "speedtests-cellular": ["speedtests-circles", "speedtests-heatmap"],
};

const SUB_LAYER_TO_PARENT: Record<string, string> = {};
Object.entries(LAYER_SUB_LAYERS).forEach(([parent, subs]) => {
  subs.forEach((sub) => {
    SUB_LAYER_TO_PARENT[sub] = parent;
  });
});

// Default layer visibility (all false until URL params are checked)
const DEFAULT_LAYER_VISIBILITY: LayerVisibility = {
  cellular: false,
  "cellular-radius": false,
  "cellular-radius-stroke": false,
  "cellular-signal": false,
  "signal-coverage-circles": false,
  "cellular-heatmap": false,
  naps: false,
  "NAPs-cluster": false,
  "NAPs-cluster-count": false,
  "NAPs-unclustered": false,
  fiber: false,
  "fiber-line": false,
  "highlighted-fiber": false,
  "speedtests-cellular": false,
  "speedtests-circles": false,
  "speedtests-heatmap": false,
};

const DEFAULT_LAYER_OPACITY: LayerOpacity = {
  cellular: 100,
  "cellular-radius": 20,
  "cellular-signal": 60,
  "cellular-heatmap": 60,
  naps: 100,
  fiber: 50,
  "speedtests-cellular": 70,
};

const DEFAULT_LAYER_STATE: LayerState = { visibility: DEFAULT_LAYER_VISIBILITY, opacity: DEFAULT_LAYER_OPACITY };

// Create context with undefined default
const AdminMapContext = createContext<AdminMapContextValue | undefined>(undefined);

interface AdminMapProviderProps {
  children: ReactNode;
  initialFilters?: Partial<AdminMapFilters>;
  initialLayerState?: Partial<LayerState>;
}

export function AdminMapProvider({ children, initialFilters, initialLayerState }: AdminMapProviderProps) {
  const searchParams = useSearchParams();
  const routeParams = useParams();
  const regionParam = routeParams?.region as string | undefined;

  // Initialize layer state from URL params
  const initialVisibilityFromURL = useMemo(() => {
    const visibility: LayerVisibility = { ...DEFAULT_LAYER_VISIBILITY };

    // Check each layer's URL param
    Object.entries(LAYER_URL_PARAMS).forEach(([layerId, paramKey]) => {
      const paramValue = searchParams.get(paramKey);
      if (paramValue === "true") {
        visibility[layerId] = true;
      }
    });

    return visibility;
  }, [searchParams]);

  const { user } = useUser();
  const { data: authUser } = useAuthUser({
    pathParams: { id: user?.id ?? "" },
    queryParams: {},
  });
  const isSuperAdmin = authUser?.data?.role?.key === "super_admin";

  // Compute default region filters from URL route and user role
  const defaultRegionIds = useMemo(() => {
    // Super admins by default view nationwide (unfiltered by region)
    if (isSuperAdmin) {
      return [];
    }
    if (regionParam && regionParam !== "all" && regionParam !== "select-region") {
      return [regionParam];
    }
    return [];
  }, [regionParam, isSuperAdmin]);

  // Layer state
  const [layerState, setLayerState] = useState<LayerState>({
    visibility: { ...initialVisibilityFromURL, ...initialLayerState?.visibility },
    opacity: { ...DEFAULT_LAYER_STATE.opacity, ...initialLayerState?.opacity },
  });

  // Filter state
  const [filters, setFilters] = useState<AdminMapFilters>({
    ...DEFAULT_ADMIN_MAP_FILTERS,
    regionIds: defaultRegionIds,
    ...initialFilters,
  });

  // Update regionIds in filters when route param changes
  useEffect(() => {
    if (defaultRegionIds.length > 0) {
      setFilters((prev) => ({ ...prev, regionIds: defaultRegionIds }));
    }
  }, [defaultRegionIds]);

  // UI state
  const [selectedFeatureId, setSelectedFeatureId] = useState<string | undefined>();
  const [isPanelCollapsed, setIsPanelCollapsed] = useState(false);

  // Sync layer state when user navigates browser history (back/forward)
  useEffect(() => {
    const handlePopState = () => {
      const params = new URLSearchParams(window.location.search);
      const updatedVisibility: Record<string, boolean> = {};
      Object.entries(LAYER_URL_PARAMS).forEach(([layerId, paramKey]) => {
        updatedVisibility[layerId] = params.get(paramKey) === "true";
      });
      setLayerState((prev) => ({
        ...prev,
        visibility: { ...prev.visibility, ...updatedVisibility },
      }));
    };

    window.addEventListener("popstate", handlePopState);
    return () => window.removeEventListener("popstate", handlePopState);
  }, []);

  // Layer visibility handler - updates both state and URL
  const setLayerVisibility = useCallback((layerId: string, visible: boolean) => {
    setLayerState((prev) => {
      const nextVisibility: LayerVisibility = { ...prev.visibility, [layerId]: visible };

      // If toggling a parent layer, cascade visibility to all its sub-layers
      const subLayers = LAYER_SUB_LAYERS[layerId];
      if (subLayers) {
        subLayers.forEach((subId) => {
          nextVisibility[subId] = visible;
        });
      }

      // If toggling a sub-layer, reflect in parent if needed
      const parentId = SUB_LAYER_TO_PARENT[layerId];
      if (parentId) {
        const siblingSubLayers = LAYER_SUB_LAYERS[parentId] ?? [];
        if (visible) {
          nextVisibility[parentId] = true;
        } else {
          // If all sub-layers are turned off, parent becomes false
          const hasAnySubActive = siblingSubLayers.some((sub) => sub !== layerId && nextVisibility[sub]);
          if (!hasAnySubActive) {
            nextVisibility[parentId] = false;
          }
        }
      }

      return { ...prev, visibility: nextVisibility };
    });

    // Update URL params for persistence
    const urlParam = LAYER_URL_PARAMS[layerId];
    if (urlParam) {
      const params = new URLSearchParams(window.location.search);
      if (visible) {
        params.set(urlParam, "true");
      } else {
        params.delete(urlParam);
      }
      const qs = params.toString();
      window.history.replaceState({}, "", qs ? `${window.location.pathname}?${qs}` : window.location.pathname);
    }
  }, []);

  // Layer opacity handler
  const setLayerOpacity = useCallback((layerId: string, opacity: number) => {
    setLayerState((prev) => ({ ...prev, opacity: { ...prev.opacity, [layerId]: opacity } }));
  }, []);

  // Filter update handler
  const updateFilters = useCallback((newFilters: Partial<AdminMapFilters>) => {
    setFilters((prev) => ({ ...prev, ...newFilters }));
  }, []);

  // Reset filters handler
  const resetFilters = useCallback(() => {
    setFilters(DEFAULT_ADMIN_MAP_FILTERS);
  }, []);

  // Toggle panel collapse
  const togglePanelCollapsed = useCallback(() => {
    setIsPanelCollapsed((prev) => !prev);
  }, []);

  // Memoize context value to prevent unnecessary re-renders
  const contextValue = useMemo<AdminMapContextValue>(
    () => ({
      layerState,
      filters,
      setLayerVisibility,
      setLayerOpacity,
      updateFilters,
      resetFilters,
      selectedFeatureId,
      setSelectedFeatureId,
      isPanelCollapsed,
      togglePanelCollapsed,
    }),
    [
      layerState,
      filters,
      setLayerVisibility,
      setLayerOpacity,
      updateFilters,
      resetFilters,
      selectedFeatureId,
      isPanelCollapsed,
      togglePanelCollapsed,
    ],
  );

  return <AdminMapContext.Provider value={contextValue}>{children}</AdminMapContext.Provider>;
}

/**
 * Hook to access admin map context
 * @throws Error if used outside of AdminMapProvider
 */
export function useAdminMapContext(): AdminMapContextValue {
  const context = useContext(AdminMapContext);
  if (!context) {
    throw new Error("useAdminMapContext must be used within an AdminMapProvider");
  }
  return context;
}

/**
 * Hook to access only layer state (for optimized renders)
 */
export function useLayerState() {
  const { layerState, setLayerVisibility, setLayerOpacity } = useAdminMapContext();
  return { layerState, setLayerVisibility, setLayerOpacity };
}

/**
 * Hook to access only filter state (for optimized renders)
 */
export function useFilterState() {
  const { filters, updateFilters, resetFilters } = useAdminMapContext();
  return { filters, updateFilters, resetFilters };
}

/**
 * Hook to access only UI state (for optimized renders)
 */
export function useAdminMapUI() {
  const { selectedFeatureId, setSelectedFeatureId, isPanelCollapsed, togglePanelCollapsed } = useAdminMapContext();
  return { selectedFeatureId, setSelectedFeatureId, isPanelCollapsed, togglePanelCollapsed };
}
