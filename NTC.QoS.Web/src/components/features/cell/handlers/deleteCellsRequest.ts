import { request } from "@/services/fetch";
import { type APIResponse, type MutationBlueprintFromTemplate } from "@/types/reusable/api";

const endpoint = "/cells" as const;
type QueryParams = { ids: string | string[] }; // Accept single ID or array, matching NAP pattern

// Note: API uses query parameter 'ids' (array) for bulk delete
export const deleteCellsRequest: MutationBlueprintFromTemplate<typeof endpoint, APIResponse<boolean>, QueryParams> = {
  endpoint,
  fetcher: async ({ finalUrl }) => {
    return await request.delete<APIResponse<boolean>>(finalUrl);
  },
  invalidateQueries: ["get-cells", "get-cells-infinite"],
};
