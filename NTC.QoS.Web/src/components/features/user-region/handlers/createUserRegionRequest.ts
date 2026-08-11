import { request } from "@/services/fetch";
import type { APIResponse, MutationBlueprintFromTemplate } from "@/types/reusable/api";
import type { UserRegion } from "../types/UserRegion";

const endpoint = "/user-regions";

export type CreateUserRegionPayload = { userId: string; regionId: string; status?: string };

export const createUserRegionRequest: MutationBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<UserRegion>,
  undefined,
  CreateUserRegionPayload
> = {
  endpoint,
  fetcher: async ({ finalUrl, payload, signal }) => {
    return await request.post<APIResponse<UserRegion>>(finalUrl, payload, signal ? { signal } : undefined);
  },
};
