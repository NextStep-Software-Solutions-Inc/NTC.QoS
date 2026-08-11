import { request } from "@/services/fetch";
import { type APIResponse, type MutationBlueprintFromTemplate } from "@/types/reusable/api";
import { type NAP } from "../types/NAP";

const endpoint = "/NAPs/:id";

export type UpdateNAPPayload = {
  serviceProviderId?: string;
  address?: string;
  latitude: number;
  longitude: number;
  isFileUploaded?: boolean;
};

export const editNAPRequest: MutationBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<NAP>,
  undefined,
  UpdateNAPPayload
> = {
  endpoint,
  fetcher: async ({ finalUrl, payload, signal }) => {
    return await request.patch<APIResponse<NAP>>(finalUrl, payload, signal ? { signal } : undefined);
  },
  invalidateQueries: ["get-NAPs", "get-NAPs-infinite"],
};
