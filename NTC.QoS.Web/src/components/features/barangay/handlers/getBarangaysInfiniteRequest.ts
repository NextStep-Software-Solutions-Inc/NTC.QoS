import { request } from "@/services/fetch";
import { type InfiniteQueryBlueprintFromTemplate } from "@/types/reusable/api";
import { type PaginationResponse } from "../../../../types/reusable/api";
import { type Barangay } from "../types/Barangay";

const endpoint = "/barangays";
type QueryParams = {
    pageSize?: number;
    pageNumber?: number;
    municipalityIds?: string[];
    municipality?: string;
    name?: string;
    province?: string;
};

export const getBarangaysInfiniteRequest: InfiniteQueryBlueprintFromTemplate<
    typeof endpoint,
    PaginationResponse<Barangay>,
    QueryParams
> = {
    endpoint,
    key: ({ pathParams, queryParams }) => ["get-barangays-infinite", endpoint, pathParams, queryParams] as const,
    fetcher: async ({ finalUrl, signal, pageParam }) => {
        return await request.get<PaginationResponse<Barangay>>(finalUrl, { signal });
    },
    getNextPageParam: (lastPage, allPages) => {
        return lastPage.loadMore ? lastPage.pageNumber + 1 : undefined;
    },
    initialPageParam: 1,
};
