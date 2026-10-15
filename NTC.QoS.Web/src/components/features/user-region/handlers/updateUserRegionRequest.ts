import { request } from "@/services/fetch";
import type { APIResponse, MutationBlueprintFromTemplate } from "@/types/reusable/api";
import type { UserRegion } from "../types/UserRegion";

const endpoint = "/user-regions/:id/status";

export type UpdateUserRegionPayload = { status: string };

export const updateUserRegionRequest: MutationBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<UserRegion>,
  undefined,
  UpdateUserRegionPayload
> = {
  endpoint,
  fetcher: async ({ finalUrl, payload, signal }) => {
    return await request.put<APIResponse<UserRegion>>(finalUrl, payload, signal ? { signal } : undefined);
  },
};
