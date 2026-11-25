import { request } from "@/services/fetch";
import { type APIResponse, type QueryBlueprintFromTemplate } from "@/types/reusable/api";
import { type CellularCoverageTileset } from "../types/CellularCoverageTileset";

const endpoint = "/cellular-coverages/tilesets" as const;

type QueryParams = {
  regionId?: string;
};

export const getTilesetsRequest: QueryBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<CellularCoverageTileset[]>,
  QueryParams
> = {
  endpoint,
  key: ({ url }) => [url] as const,
  fetcher: async ({ finalUrl, signal }: { finalUrl: string; signal?: AbortSignal }) => {
    return await request.get<APIResponse<CellularCoverageTileset[]>>(finalUrl, { signal });
  },
  defaultOptions: {
    refetchInterval: (query) => {
      const list = query.state.data?.data;
      const hasActive = list?.some((t: CellularCoverageTileset) => {
        const s = (t.statusText || String(t.status ?? "")).toLowerCase();
        return s === "uploading" || s === "processing" || s === "pending" || s === "queued";
      });
      return hasActive ? 3000 : false;
    },
  },
};
