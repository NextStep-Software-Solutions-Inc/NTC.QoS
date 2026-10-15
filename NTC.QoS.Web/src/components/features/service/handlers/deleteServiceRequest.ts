import { request } from "@/services/fetch";
import { type APIResponse, type MutationBlueprintFromTemplate } from "@/types/reusable/api";
import { type Service } from "../types/Service";

const endpoint = "/services/:id";

export const deleteServiceRequest: MutationBlueprintFromTemplate<typeof endpoint, APIResponse<Service>> = {
  endpoint,
  fetcher: async ({ finalUrl }) => {
    return await request.delete<APIResponse<Service>>(finalUrl);
  },
  invalidateQueries: "get-services",
};
