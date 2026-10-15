import { request } from "@/services/fetch";
import { type APIResponse, type QueryBlueprintFromTemplate } from "@/types/reusable/api";
import { type Link } from "../types/Link";

const endpoint = "/links/:id";
export const getLinkDetailsRequest: QueryBlueprintFromTemplate<typeof endpoint, APIResponse<Link>> = {
  endpoint,
  key: ({ url, pathParams, queryParams }) => ["get-link-details", url, endpoint, pathParams, queryParams] as const,
  fetcher: async ({ finalUrl, signal }) => {
    return await request.get<APIResponse<Link>>(finalUrl, { signal });
  },
};
