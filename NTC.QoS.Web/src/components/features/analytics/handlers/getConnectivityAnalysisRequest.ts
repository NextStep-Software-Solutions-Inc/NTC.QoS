import { request } from "@/services/fetch";
import { type MutationBlueprintFromTemplate } from "@/types/reusable/api";
import { type ConnectivityAnalysisRequest, type ConnectivityAnalysisResponse } from "../types/ConnectivityAnalysis";

const endpoint = "/analytics/connectivity-analysis";

export const getConnectivityAnalysisRequest: MutationBlueprintFromTemplate<
  typeof endpoint,
  ConnectivityAnalysisResponse,
  undefined,
  ConnectivityAnalysisRequest
> = {
  endpoint,
  fetcher: async ({ finalUrl, payload, signal }) => {
    return await request.post<ConnectivityAnalysisResponse>(finalUrl, payload, signal ? { signal } : undefined);
  },
};
