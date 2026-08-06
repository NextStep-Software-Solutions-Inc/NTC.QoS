import { request } from "@/services/fetch";
import { type APIResponse, type MutationBlueprintFromTemplate } from "@/types/reusable/api";
import { type CreateRolePermissionPayload, type RolePermission } from "../types/RolePermission";

const endpoint = "/role-permissions";

export const createRolePermissionRequest: MutationBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<RolePermission>,
  undefined,
  CreateRolePermissionPayload
> = {
  endpoint,
  fetcher: async ({ finalUrl, payload, signal }) => {
    return await request.post<APIResponse<RolePermission>>(finalUrl, payload, signal ? { signal } : undefined);
  },
};
