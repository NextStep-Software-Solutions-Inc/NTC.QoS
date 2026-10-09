import { type APIResponse } from "@/types/reusable/api";
import { type CellularCoverageTileset } from "../types/CellularCoverageTileset";

export const getActiveTilesetsRequest = {
  url: "/cellular-coverages/tilesets/active",
  method: "GET" as const,
  types: {} as {
    response: APIResponse<CellularCoverageTileset[]>;
    queryParams: {
      regionId?: string;
      serviceProviderId?: string;
    };
    pathParams: {};
  },
};
