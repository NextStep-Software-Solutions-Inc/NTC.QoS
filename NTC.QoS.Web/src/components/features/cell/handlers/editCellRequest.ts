import { request } from "@/services/fetch";
import { type APIResponse, type MutationBlueprintFromTemplate } from "@/types/reusable/api";
import { type Cell } from "../types/Cell";

const endpoint = "/cells/:id";

type UpdateCellPayload = {
  name?: string;
  group?: string; // typically auto-filled from service provider name in the UI
  description?: string;
  address?: string;
  serviceProviderId?: string; // UUID format
  cid?: string;
  lac?: string;
  tac?: string;
  siteId?: string;
  towerType?: string;
  elevation?: number;
  isVerified?: boolean; // checkbox to verify site details
  latitude?: number; // must be between -90 and 90
  longitude?: number; // must be between -180 and 180
};

export const editCellRequest: MutationBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<Cell>,
  undefined,
  UpdateCellPayload
> = {
  endpoint,
  fetcher: async ({ finalUrl, payload, signal }) => {
    return await request.patch<APIResponse<Cell>>(finalUrl, payload, signal ? { signal } : undefined);
  },
  invalidateQueries: ["get-cells", "get-cells-infinite"],
};
