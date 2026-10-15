import { type NAPDetails } from "@/components/features/NAP/types/NAP";

export type MapInteractionMode = "view" | "add" | "edit" | "delete";

export interface ViewportBounds {
  minLat: number;
  maxLat: number;
  minLng: number;
  maxLng: number;
}

export interface NAPMapViewProps {
  naps: NAPDetails[];
  regionId?: string;
  focusedLocation?: { lat: number; lng: number } | null;
  className?: string;
  isExpanded?: boolean;
  onToggleExpand?: () => void;
  onAddNAP?: (lat: number, lng: number) => void;
  onEditNAP?: (nap: NAPDetails) => void;
  onDeleteNAP?: (nap: NAPDetails) => void;
  onSelectNAP?: (nap: NAPDetails | null) => void;
  onViewportChange?: (bounds: ViewportBounds) => void;
  selectedNAPId?: string | null;
  optimisticNAPs?: OptimisticNAP[];
}

export interface OptimisticNAP {
  id: string;
  lat: number;
  lng: number;
  ISP: string;
  ISPColor: string;
  dist: number;
  isPending?: boolean;
  hasError?: boolean;
}

export interface NAPWithOptimistic extends NAPDetails {
  isPending?: boolean;
  hasError?: boolean;
}

export interface ProviderCount {
  count: number;
  color: string;
  visible: boolean;
}

export type ProviderCounts = Record<string, ProviderCount>;
