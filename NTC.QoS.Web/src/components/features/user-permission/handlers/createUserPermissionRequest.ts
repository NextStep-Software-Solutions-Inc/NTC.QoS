import { request } from "@/services/fetch";
import { type APIResponse, type MutationBlueprintFromTemplate } from "@/types/reusable/api";
import { type UserPermissionDetails } from "../types/UserPermission";

const endpoint = "/user-permissions";

type CreateUserPermissionPayload = { userId: string; permissionId: string; status?: string };

export const createUserPermissionRequest: MutationBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<UserPermissionDetails>,
  undefined,
  CreateUserPermissionPayload
> = {
  endpoint,
  fetcher: async ({ finalUrl, payload, signal }) => {
    return await request.post<APIResponse<UserPermissionDetails>>(finalUrl, payload, signal ? { signal } : undefined);
  },
};
