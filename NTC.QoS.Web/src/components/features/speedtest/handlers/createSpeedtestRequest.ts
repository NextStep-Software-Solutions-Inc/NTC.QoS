import { request } from "@/services/fetch";
import { type APIResponse, type MutationBlueprintFromTemplate } from "@/types/reusable/api";
import { type SpeedTest } from "../types/SpeedTest";

const endpoint = "/speedtests";

type CreateSpeedtestPayload = {
  // Required fields
  serviceProviderId: string;
  generationId: string;
  technologyId: string;
  dateTime: string;
  uploadSpeed: number;
  downloadSpeed: number;
  latitude: number;
  longitude: number;
  // Optional fields
  address?: string;
  ping?: number;
  jitter?: number;
  rssi?: number;
  phoneModel?: string;
  mcc?: string;
  mnc?: string;
  tac?: string;
  signalQuality?: "Unknown" | "VeryWeak" | "Weak" | "Average" | "Good" | "Excellent";
  cellIdentifier?: string;
  isFixedBroadband?: boolean;
  isNoBroadbandCoverage?: boolean;
};

export const createSpeedtestRequest: MutationBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<SpeedTest>,
  undefined,
  CreateSpeedtestPayload
> = {
  endpoint,
  fetcher: async ({ finalUrl, payload, signal }) => {
    return await request.post<APIResponse<SpeedTest>>(finalUrl, payload, signal ? { signal } : undefined);
  },
  invalidateQueries: ["speedtests", "get-speedtests-infinite"],
};
