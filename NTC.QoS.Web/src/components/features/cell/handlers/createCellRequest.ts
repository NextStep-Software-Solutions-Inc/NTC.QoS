import { request } from "@/services/fetch";
import { type APIResponse, type MutationBlueprintFromTemplate } from "@/types/reusable/api";
import { type Cell } from "../types/Cell";

const endpoint = "/cells";

type CreateCellPayload = {
  name: string; // required
  group: string; // required - auto-filled from service provider name
  description?: string;
  address?: string;
  serviceProviderId: string; // required - UUID format
  cid?: string;
  lac?: string;
  tac?: string;
  siteId?: string;
  towerType?: string;
  elevation?: number;
  latitude: number; // required, must be between -90 and 90
  longitude: number; // required, must be between -180 and 180
  status: string; // required - valid values: "Pending", "Licensed", "Unknown"
};

export const createCellRequest: MutationBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<Cell>,
  undefined,
  CreateCellPayload
> = {
  endpoint,
  fetcher: async ({ finalUrl, payload, signal }) => {
    return await request.post<APIResponse<Cell>>(finalUrl, payload, signal ? { signal } : undefined);
  },
  invalidateQueries: ["get-cells", "get-cells-infinite"],
};
