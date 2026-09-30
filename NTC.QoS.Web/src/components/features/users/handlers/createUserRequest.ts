import { request } from "@/services/fetch";
import type { APIResponse, MutationBlueprintFromTemplate } from "@/types/reusable/api";
import type { User } from "../types/User";

const endpoint = "/users";

export type CreateUserPayload = {
  authUserId: string;
  email: string;
  firstName?: string;
  lastName?: string;
  roleId: string;
  regions?: { regionId: string; roleId?: string; status?: string }[];
};

export const createUserRequest: MutationBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<User>,
  undefined,
  CreateUserPayload
> = {
  endpoint,
  fetcher: async ({ finalUrl, payload, signal }) => {
    return await request.post<APIResponse<User>>(finalUrl, payload, signal ? { signal } : undefined);
  },
};
