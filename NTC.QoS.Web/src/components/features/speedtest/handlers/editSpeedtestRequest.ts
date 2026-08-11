import { request } from "@/services/fetch";
import { type APIResponse, type MutationBlueprintFromTemplate } from "@/types/reusable/api";
import { type SpeedTest } from "../types/SpeedTest";

const endpoint = "/speedtests/:id";

type UpdateSpeedtestPayload = {
  downloadSpeed?: number;
  uploadSpeed?: number;
  ping?: number;
  jitter?: number;
  latitude?: number;
  longitude?: number;
  serviceProviderId?: string;
  deviceInfo?: string;
  testDate?: string;
  isNoBroadbandCoverage?: boolean;
};

export const editSpeedtestRequest: MutationBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<SpeedTest>,
  undefined,
  UpdateSpeedtestPayload
> = {
  endpoint,
  fetcher: async ({ finalUrl, payload, signal }) => {
    return await request.patch<APIResponse<SpeedTest>>(finalUrl, payload, signal ? { signal } : undefined);
  },
  invalidateQueries: ["speedtests", "get-speedtests-infinite"],
};
