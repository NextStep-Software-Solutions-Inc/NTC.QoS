import { request } from "@/services/fetch";
import { type APIResponse, type MutationBlueprintFromTemplate } from "@/types/reusable/api";
import { type Sector } from "../types/Sector";

const endpoint = "/sectors/:id";

type UpdateSectorPayload = Partial<Omit<Sector, "id" | "createdAt" | "updatedAt">>;

export const updateSectorRequest: MutationBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<Sector>,
  undefined,
  UpdateSectorPayload
> = {
  endpoint,
  fetcher: async ({ finalUrl, payload, signal }) => {
    return await request.patch<APIResponse<Sector>>(finalUrl, payload, signal ? { signal } : undefined);
  },
};
