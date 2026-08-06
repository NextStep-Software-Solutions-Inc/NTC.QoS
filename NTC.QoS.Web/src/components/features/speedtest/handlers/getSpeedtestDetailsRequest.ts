import { request } from "@/services/fetch";
import { type APIResponse, type QueryBlueprintFromTemplate } from "@/types/reusable/api";
import { type SpeedTest } from "../types/SpeedTest";

const endpoint = "/speedtests/:id";

export const getSpeedtestDetailsRequest: QueryBlueprintFromTemplate<typeof endpoint, APIResponse<SpeedTest>> = {
  endpoint,
  key: ({ url }) => [url],
  fetcher: async ({ finalUrl, signal }) => {
    return await request.get<APIResponse<SpeedTest>>(finalUrl, { signal });
  },
};
