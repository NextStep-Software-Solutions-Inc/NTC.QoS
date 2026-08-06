import { request } from "@/services/fetch";
import { type APIResponse, type MutationBlueprintFromTemplate } from "@/types/reusable/api";
import { type Link } from "../types/Link";

const endpoint = "/links/:id";

type EditLinkPayload = {
  type: string;
  centerFrequency: number;
  lowerFrequency: number;
  upperFrequency: number;
  rxThreshold: number;
};

export const editLinkRequest: MutationBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<Link>,
  undefined,
  EditLinkPayload
> = {
  endpoint,
  fetcher: async ({ finalUrl, payload, signal }) => {
    return await request.patch<APIResponse<Link>>(finalUrl, payload, signal ? { signal } : undefined);
  },
};
