import { request } from "@/services/fetch";
import { type APIResponse, type MutationBlueprintFromTemplate } from "@/types/reusable/api";
import { type UserPermissionDetails } from "../types/UserPermission";

const endpoint = "/user-permissions/:id";

type UpdateUserPermissionPayload = { status?: string };

export const updateUserPermissionRequest: MutationBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<UserPermissionDetails>,
  { id: string },
  UpdateUserPermissionPayload
> = {
  endpoint,
  fetcher: async ({ finalUrl, payload, signal }) => {
    return await request.put<APIResponse<UserPermissionDetails>>(finalUrl, payload, signal ? { signal } : undefined);
  },
};
