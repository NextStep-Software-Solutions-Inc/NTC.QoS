import { request } from "@/services/fetch";
import { type APIResponse, type MutationBlueprintFromTemplate } from "@/types/reusable/api";
import { type Cell } from "../types/Cell";

const endpoint = "/cells/:id/status";

// Payload is a simple string value for status
// Valid values: "Pending", "Licensed", "Unknown"
export const editCellStatusRequest: MutationBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<Cell>,
  undefined,
  string
> = {
  endpoint,
  fetcher: async ({ finalUrl, payload, signal }) => {
    // Send the string payload directly - the request service will JSON.stringify it
    // which will properly quote the string for the API
    return await request.put<APIResponse<Cell>>(finalUrl, payload, signal ? { signal } : undefined);
  },
  invalidateQueries: ["get-cells", "get-cells-infinite"],
};
