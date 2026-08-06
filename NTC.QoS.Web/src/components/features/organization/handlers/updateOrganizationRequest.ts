import { request } from "@/services/fetch";
import type { APIResponse, MutationBlueprintFromTemplate } from "@/types/reusable/api";
import { Organization, UpdateOrganizationPayload } from "../types/Organization";

const endpoint = "/organizations/:id";

export const updateOrganizationRequest: MutationBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<Organization>,
  undefined,
  UpdateOrganizationPayload
> = {
  endpoint,
  fetcher: async ({ finalUrl, payload, signal }) => {
    return await request.patch<APIResponse<Organization>>(finalUrl, payload, signal ? { signal } : undefined);
  },
};
