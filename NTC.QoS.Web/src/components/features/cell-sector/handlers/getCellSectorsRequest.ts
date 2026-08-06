import { request } from "@/services/fetch";
import { type PaginationResponse, type QueryBlueprintFromTemplate } from "@/types/reusable/api";
import { type CellSector } from "../types/CellSector";

const endpoint = "/cell-sectors";

type GetCellSectorsQueryParams = {
  pageSize?: number;
  pageNumber?: number;
  cellIds?: string[];
};

export const getCellSectorsRequest: QueryBlueprintFromTemplate<
  typeof endpoint,
  PaginationResponse<CellSector>,
  GetCellSectorsQueryParams
> = {
  endpoint,
  key: ({ url, pathParams, queryParams }) => ["get-cell-sectors", url, endpoint, pathParams, queryParams] as const,
  fetcher: async ({ finalUrl, signal }) => {
    return await request.get<PaginationResponse<CellSector>>(finalUrl, { signal });
  },
};
