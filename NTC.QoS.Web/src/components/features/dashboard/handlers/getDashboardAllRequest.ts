import { request } from "@/services/fetch";
import { type QueryBlueprintFromTemplate } from "@/types/reusable/api";
import { type DashboardAll } from "../types/All";

const endpoint = "/dashboard/all";
type QueryParams = {
  startDate?: string;
  endDate?: string;
  regionId?: string;
  provinceId?: string;
  municipalityId?: string;
  daysInPast?: number;
};

export const getDashboardAllRequest: QueryBlueprintFromTemplate<typeof endpoint, DashboardAll, QueryParams> = {
  endpoint,
  key: ({ url }) => [url] as const,
  fetcher: async ({ finalUrl, signal }) => {
    return await request.get<DashboardAll>(finalUrl, { signal });
  },
};
