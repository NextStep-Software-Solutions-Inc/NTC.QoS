import { request } from "@/services/fetch";
import type { APIResponse, QueryBlueprintFromTemplate } from "@/types/reusable/api";
import type { Invitation } from "../types/Invitation";

const endpoint = "/users/invitations" as const;
type QueryParams = { status?: string };

export const getInvitationsRequest: QueryBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<Invitation[]>,
  QueryParams
> = {
  endpoint,
  key: ({ url, pathParams, queryParams }) => ["get-invitations", url, endpoint, pathParams, queryParams] as const,
  fetcher: async ({ finalUrl, signal }) => {
    return await request.get<APIResponse<Invitation[]>>(finalUrl, { signal });
  },
};
