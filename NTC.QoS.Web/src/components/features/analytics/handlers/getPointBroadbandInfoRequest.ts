import { request } from "@/services/fetch";
import { APIResponse, type MutationBlueprintFromTemplate } from "@/types/reusable/api";
import { BroadbandInfoData, type PointBroadbandInfoPayload } from "../types/BroadbandInfo";

const endpoint = "/analytics/point-broadband-info";

export const getPointBroadbandInfoRequest: MutationBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<BroadbandInfoData>,
  undefined,
  PointBroadbandInfoPayload
> = {
  endpoint,
  fetcher: async ({ finalUrl, payload, signal }) => {
    return await request.post<APIResponse<BroadbandInfoData>>(finalUrl, payload, signal ? { signal } : undefined);
  },
};
