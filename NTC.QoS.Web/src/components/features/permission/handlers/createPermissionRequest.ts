import { request } from "@/services/fetch";
import { type APIResponse, type MutationBlueprintFromTemplate } from "@/types/reusable/api";
import { type PermissionDetails } from "../types/Permission";

const endpoint = "/permissions";

type CreatePermissionPayload = { name: string; key: string; description?: string };

export const createPermissionRequest: MutationBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<PermissionDetails>,
  undefined,
  CreatePermissionPayload
> = {
  endpoint,
  fetcher: async ({ finalUrl, payload, signal }) => {
    return await request.post<APIResponse<PermissionDetails>>(finalUrl, payload, signal ? { signal } : undefined);
  },
};
