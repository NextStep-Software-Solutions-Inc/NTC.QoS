import { request } from "@/services/fetch";
import { type APIResponse, type MutationBlueprintFromTemplate } from "@/types/reusable/api";
import { type PermissionDetails } from "../types/Permission";

const endpoint = "/permissions/:id";

type EditPermissionPayload = { name?: string; key?: string; description?: string };

export const editPermissionRequest: MutationBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<PermissionDetails>,
  undefined,
  EditPermissionPayload
> = {
  endpoint,
  fetcher: async ({ finalUrl, payload, signal }) => {
    return await request.put<APIResponse<PermissionDetails>>(finalUrl, payload, signal ? { signal } : undefined);
  },
};
