import { request } from "@/services/fetch";
import type { APIResponse, MutationBlueprintFromTemplate } from "@/types/reusable/api";
import type { User } from "../types/User";

const endpoint = "/users/invitations/:id/resend" as const;

export const resendInvitationRequest: MutationBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<User>,
  undefined,
  undefined
> = {
  endpoint,
  fetcher: async ({ finalUrl, signal }) => {
    return await request.post<APIResponse<User>>(finalUrl, {}, signal ? { signal } : undefined);
  },
  invalidateQueries: ["get-invitations"],
};
