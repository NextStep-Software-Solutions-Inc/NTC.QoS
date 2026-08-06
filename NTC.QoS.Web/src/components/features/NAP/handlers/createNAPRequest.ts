import { request } from "@/services/fetch";
import { type APIResponse, type MutationBlueprintFromTemplate } from "@/types/reusable/api";
import { type SpeedTest } from "../../speedtest/types/SpeedTest";

const endpoint = "/NAPs";

type CreateNAPPayload = {
  serviceProviderId: string;
  address: string;
  latitude: number;
  longitude: number;
  isFileUploaded: boolean;
};

export const createNAPRequest: MutationBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<SpeedTest>,
  undefined,
  CreateNAPPayload
> = {
  endpoint,
  fetcher: async ({ finalUrl, payload, signal }) => {
    return await request.post<APIResponse<SpeedTest>>(finalUrl, payload, signal ? { signal } : undefined);
  },
  invalidateQueries: ["get-NAPs", "get-NAPs-infinite"],
};
