import { type ServiceProviderDetails } from "../../service-provider/types/serviceProvider";

/**
 * Simplified Cell type returned by viewport-based API (/v2/cells/viewport/public)
 * Optimized for map rendering with minimal data transfer
 */
export type CellViewport = {
  id: string; // Cell unique identifier
  isp: string; // Service provider name
  ispColor: string; // Provider brand color (hex)
  lat: number; // Latitude
  lng: number; // Longitude
  name: string; // Cell site name
  status: string; // Cell status (e.g., "Unknown", "Active", etc.)
};

/**
 * Full Cell type returned by detail endpoints (/cells/:id)
 * Contains complete cell information including relationships
 */
export type Cell = {
  id: string;
  name: string;
  description: string;
  image: string;
  group: string;
  serviceProviderId: string;
  serviceProvider: ServiceProviderDetails;
  address: string;
  cid: string;
  lac: string;
  tac: string; // Added TAC field
  siteId: string;
  towerType: string;
  elevation: number;
  latitude: number;
  longitude: number;
  distance: number;
  isVerified: boolean;
  status: string;
};

export type CellDetails = Cell;

export type CellBand = {
  bandId: string;
  bandName: string;
  bandDescription: string;
};

export type CellTechnology = {
  technologyId: string;
  technologyStandard: string;
  technologyDescription: string;
  cellSectorId: string;
  bands: CellBand[];
};

export type CellComplianceItem = {
  serviceProviderId: string;
  providerName: string;
  technologyId: string;
  technologyStandard: string;
  bandId: string;
  bandName: string;
  isAuthorized: boolean;
  matchedAllocationName: string | null;
  matchedAllocationNotes: string | null;
};

export type CellCompliance = {
  cellId: string;
  overallStatus: string;
  items: CellComplianceItem[];
};
