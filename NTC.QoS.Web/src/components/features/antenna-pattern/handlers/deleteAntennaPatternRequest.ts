import { request } from "@/services/fetch";
import { type APIResponse, type MutationBlueprintFromTemplate } from "@/types/reusable/api";
import { type AntennaPattern } from "../types/AntennaPattern";

const endpoint = "/antenna-patterns/:id";

export const deleteAntennaPatternRequest: MutationBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<AntennaPattern>
> = {
  endpoint,
  fetcher: async ({ finalUrl }) => {
    return await request.delete<APIResponse<AntennaPattern>>(finalUrl);
  },
};
