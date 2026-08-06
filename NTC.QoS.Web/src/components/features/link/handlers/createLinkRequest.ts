import { request } from "@/services/fetch";
import { type APIResponse, type MutationBlueprintFromTemplate } from "@/types/reusable/api";
import { type Link } from "../types/Link";

const endpoint = "/links";

type CreateLinkPayload = {
  type: string;
  centerFrequency: number;
  lowerFrequency: number;
  upperFrequency: number;
  rxThreshold: number;
  bandId: string;
  serviceProviderId: string;
};

export const createLinkRequest: MutationBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<Link>,
  undefined,
  CreateLinkPayload
> = {
  endpoint,
  fetcher: async ({ finalUrl, payload, signal }) => {
    return await request.post<APIResponse<Link>>(finalUrl, payload, signal ? { signal } : undefined);
  },
};
