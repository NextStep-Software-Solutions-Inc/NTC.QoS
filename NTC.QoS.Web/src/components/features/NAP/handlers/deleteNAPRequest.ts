import { request } from "@/services/fetch";
import { type APIResponse, type MutationBlueprintFromTemplate } from "@/types/reusable/api";
import { type NAP } from "../types/NAP";

const endpoint = "/NAPs";

type DeleteNAPQueryParams = {
  ids: string | string[];
};

export const deleteNAPRequest: MutationBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<NAP>,
  DeleteNAPQueryParams
> = {
  endpoint,
  fetcher: async ({ finalUrl }) => {
    return await request.delete<APIResponse<NAP>>(finalUrl);
  },
  invalidateQueries: ["get-NAPs", "get-NAPs-infinite"],
};
