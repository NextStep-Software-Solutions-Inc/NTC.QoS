import { request } from "@/services/fetch";
import { type APIResponse, type MutationBlueprintFromTemplate } from "@/types/reusable/api";
import { type Role } from "../types/Role";

const endpoint = "/roles/:id";

type EditRolePayload = { name?: string; key?: string; description?: string };

export const editRoleRequest: MutationBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<Role>,
  undefined,
  EditRolePayload
> = {
  endpoint,
  fetcher: async ({ finalUrl, payload, signal }) => {
    return await request.patch<APIResponse<Role>>(finalUrl, payload, signal ? { signal } : undefined);
  },
};
