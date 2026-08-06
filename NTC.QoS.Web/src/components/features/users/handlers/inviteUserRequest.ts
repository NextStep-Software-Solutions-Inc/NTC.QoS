import { request } from "@/services/fetch";
import type { APIResponse, MutationBlueprintFromTemplate } from "@/types/reusable/api";
import type { User } from "../types/User";

const endpoint = "/users/invite";

export type InviteUserPayload = { email: string; roleId: string; organizationId: string };

export const inviteUserRequest: MutationBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<User>,
  undefined,
  InviteUserPayload
> = {
  endpoint,
  fetcher: async ({ finalUrl, payload, signal }) => {
    return await request.post<APIResponse<User>>(finalUrl, payload, signal ? { signal } : undefined);
  },
};
