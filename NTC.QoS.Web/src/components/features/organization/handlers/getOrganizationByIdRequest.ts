import { request } from "@/services/fetch";
import type { APIResponse, QueryBlueprintFromTemplate } from "@/types/reusable/api";
import { Organization } from "../types/Organization";

const endpoint = "/organizations/:id" as const;

export const getOrganizationByIdRequest: QueryBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<Organization>,
  never
> = {
  endpoint,
  key: ({ url }) => [url] as const,
  fetcher: async ({ finalUrl }) => {
    return await request.get<APIResponse<Organization>>(finalUrl);
  },
};
