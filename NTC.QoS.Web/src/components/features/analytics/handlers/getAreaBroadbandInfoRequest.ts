import { request } from "@/services/fetch";
import { APIResponse, type MutationBlueprintFromTemplate } from "@/types/reusable/api";
import { BroadbandInfoData, type AreaBroadbandInfoPayload } from "../types/BroadbandInfo";

const endpoint = "/analytics/area-broadband-info";

export const getAreaBroadbandInfoRequest: MutationBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<BroadbandInfoData>,
  undefined,
  AreaBroadbandInfoPayload
> = {
  endpoint,
  fetcher: async ({ finalUrl, payload, signal }) => {
    return await request.post<APIResponse<BroadbandInfoData>>(finalUrl, payload, signal ? { signal } : undefined);
  },
};
