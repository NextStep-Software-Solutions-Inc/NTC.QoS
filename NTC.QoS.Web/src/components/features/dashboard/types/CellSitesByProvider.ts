import { type APIResponse } from "@/types/reusable/api";

export type CellSitesByProvider = {
  serviceProviderId: string;
  providerName: string;
  providerColor: string;
  cellCount: number;
  percentage: number;
};

export type CellSitesByProviderData = { totalCellSites: number; providers: CellSitesByProvider[] };

export type CellSitesByProviderResponse = APIResponse<CellSitesByProviderData>;

export type CellSitesByProviderQueryParams = {
  regionId?: string;
  serviceProviderId?: string;
  startDate?: string;
  endDate?: string;
};
