import { request } from "@/services/fetch";
import { type APIResponse, type QueryBlueprintFromTemplate } from "@/types/reusable/api";
import { type AreaStudyDetails } from "../types/AreaStudy";

const endpoint = "/area-studies/:id";

export const getAreaStudyDetailsRequest: QueryBlueprintFromTemplate<typeof endpoint, APIResponse<AreaStudyDetails>> = {
  endpoint,
  key: ({ url, pathParams, queryParams }) => ["get-area-study", url, endpoint, pathParams, queryParams] as const,
  fetcher: async ({ finalUrl, signal }) => {
    return await request.get<APIResponse<AreaStudyDetails>>(finalUrl, { signal });
  },
};
