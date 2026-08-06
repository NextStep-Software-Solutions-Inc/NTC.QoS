import { request } from "@/services/fetch";
import { type APIResponse, type MutationBlueprintFromTemplate } from "@/types/reusable/api";

const endpoint = "/speedtests";

type DeleteSpeedtestQueryParams = {
  ids: string | string[];
};

export const deleteSpeedtestRequest: MutationBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<boolean>,
  DeleteSpeedtestQueryParams
> = {
  endpoint,
  fetcher: async ({ finalUrl }) => {
    return await request.delete<APIResponse<boolean>>(finalUrl);
  },
  invalidateQueries: ["speedtests", "get-speedtests-infinite"],
};
