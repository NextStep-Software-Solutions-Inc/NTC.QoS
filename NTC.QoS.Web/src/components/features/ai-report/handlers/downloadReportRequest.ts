import { type QueryBlueprintFromTemplate } from "@/types/reusable/api";

const endpoint = "/genai/reports/:id/download" as const;

export const downloadReportRequest: QueryBlueprintFromTemplate<
  typeof endpoint,
  Blob
> = {
  endpoint,
  key: ({ url, pathParams, queryParams }) => ["download-report", url, endpoint, pathParams, queryParams] as const,
  fetcher: async ({ finalUrl, signal }) => {
    // Request binary data as blob
    const response = await fetch(finalUrl, { signal });
    if (!response.ok) {
      throw new Error(`HTTP error! status: ${response.status}`);
    }
    return await response.blob();
  },
};
