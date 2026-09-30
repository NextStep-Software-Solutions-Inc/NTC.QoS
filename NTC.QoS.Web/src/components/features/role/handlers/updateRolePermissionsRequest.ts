import { request } from "@/services/fetch";
import { type APIResponse, type MutationBlueprintFromTemplate } from "@/types/reusable/api";
import { type Role } from "../types/Role";

const endpoint = "/roles/:id/permissions";

type UpdateRolePermissionsPayload = { permissions: string[] };

export const updateRolePermissionsRequest: MutationBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<Role>,
  undefined,
  UpdateRolePermissionsPayload
> = {
  endpoint,
  fetcher: async ({ finalUrl, payload, signal }) => {
    return await request.put<APIResponse<Role>>(finalUrl, payload, signal ? { signal } : undefined);
  },
};
