import { request } from "@/services/fetch";
import { type APIResponse, type MutationBlueprintFromTemplate } from "@/types/reusable/api";
import { type Province } from "../types/Province";

const endpoint = "/provinces/:id";
export const deleteProvinceRequest: MutationBlueprintFromTemplate<typeof endpoint, APIResponse<Province>> = {
  endpoint,
  fetcher: async ({ finalUrl }) => {
    return await request.delete<APIResponse<Province>>(finalUrl);
  },
};
