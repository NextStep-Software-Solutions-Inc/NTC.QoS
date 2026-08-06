import { request } from "@/services/fetch";
import type { APIResponse, MutationBlueprintFromTemplate } from "@/types/reusable/api";
import { CreateOrganizationPayload, Organization } from "../types/Organization";

const endpoint = "/organizations";

export const createOrganizationRequest: MutationBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<Organization>,
  undefined,
  CreateOrganizationPayload
> = {
  endpoint,
  fetcher: async ({ finalUrl, payload, signal }) => {
    return await request.post<APIResponse<Organization>>(finalUrl, payload, signal ? { signal } : undefined);
  },
};
