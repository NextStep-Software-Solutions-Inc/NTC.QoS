import { request } from "@/services/fetch";
import { type APIResponse, type MutationBlueprintFromTemplate } from "@/types/reusable/api";
import { type Role } from "../types/Role";

const endpoint = "/roles";

type CreateRolePayload = { name: string; key: string; description?: string };

export const createRoleRequest: MutationBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<Role>,
  undefined,
  CreateRolePayload
> = {
  endpoint,
  fetcher: async ({ finalUrl, payload, signal }) => {
    return await request.post<APIResponse<Role>>(finalUrl, payload, signal ? { signal } : undefined);
  },
};
