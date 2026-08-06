import { request } from "@/services/fetch";
import { type APIResponse, type MutationBlueprintFromTemplate } from "@/types/reusable/api";

const endpoint = "/role-permissions/:id";

export const deleteRolePermissionRequest: MutationBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<null>,
  undefined,
  undefined
> = {
  endpoint,
  fetcher: async ({ finalUrl, signal }) => {
    return await request.delete<APIResponse<null>>(finalUrl, signal ? { signal } : undefined);
  },
};
