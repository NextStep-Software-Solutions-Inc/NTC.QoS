import { request } from "@/services/fetch";
import type { APIResponse, MutationBlueprintFromTemplate } from "@/types/reusable/api";
import type { User } from "../types/User";

const endpoint = "/users/:id";

export type UpdateUserPayload = {
  roleId?: string;
  firstName?: string;
  lastName?: string;
  email?: string;
};

export const updateUserRequest: MutationBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<User>,
  undefined,
  UpdateUserPayload
> = {
  endpoint,
  fetcher: async ({ finalUrl, payload, signal }) => {
    return await request.patch<APIResponse<User>>(finalUrl, payload, signal ? { signal } : undefined);
  },
};
