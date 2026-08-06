import { request } from "@/services/fetch";
import type { APIResponse, MutationBlueprintFromTemplate } from "@/types/reusable/api";
import { Organization } from "../types/Organization";

const endpoint = "/organizations/:id";

export const deleteOrganizationRequest: MutationBlueprintFromTemplate<typeof endpoint, APIResponse<Organization>> = {
  endpoint,
  fetcher: async ({ finalUrl }) => {
    return await request.delete<APIResponse<Organization>>(finalUrl);
  },
};
