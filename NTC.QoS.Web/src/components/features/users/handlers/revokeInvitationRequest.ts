import { request } from "@/services/fetch";
import type { APIResponse, MutationBlueprintFromTemplate } from "@/types/reusable/api";

const endpoint = "/users/invitations/:id/revoke" as const;

export const revokeInvitationRequest: MutationBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<boolean>,
  undefined,
  undefined
> = {
  endpoint,
  fetcher: async ({ finalUrl, signal }) => {
    return await request.post<APIResponse<boolean>>(finalUrl, {}, signal ? { signal } : undefined);
  },
  invalidateQueries: ["get-invitations"],
};
