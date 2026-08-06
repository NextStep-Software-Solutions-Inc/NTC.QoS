import {
  type InfiniteData,
  type QueryKey,
  type UseInfiniteQueryOptions,
  type UseMutationOptions,
  type UseQueryOptions,
} from "@tanstack/react-query";

import { type ErrorDetailsPropsTypes } from "./errors";

export type PaginationResponse<P> = {
  data: P[];
  succeeded: boolean;
  message: string;
  errors: ErrorDetailsPropsTypes;
  pageSize: number;
  pageNumber: number;
  totalPageNumber: number;
  loadMore: boolean;
  totalRows: number;
};

export type APIResponse<R> = { data: R; succeeded: boolean; message: string; errors: ErrorDetailsPropsTypes };
export type QueryParams = Record<string, string | number | undefined>;

export type ExtractPathParams<T extends string> = T extends `${string}:${infer Param}/${infer Rest}`
  ? { [k in Param | keyof ExtractPathParams<`/${Rest}`>]: string }
  : T extends `${string}:${infer Param}`
    ? Record<Param, string>
    : Record<string, string>; // <-- use empty record for static routes

export type QueryBlueprintFromTemplate<
  TEndpoint extends string,
  TResponse = unknown,
  TQueryParams = undefined,
  TQueryKey extends readonly unknown[] = ReturnType<
    ({
      pathParams,
      queryParams,
    }: {
      pathParams: ExtractPathParams<TEndpoint>;
      queryParams: TQueryParams;
    }) => readonly unknown[]
  >,
> = {
  endpoint: TEndpoint;
  headers?: HeadersInit;
  key: ({
    url,
    pathParams,
    queryParams,
  }: {
    url: string;
    pathParams: ExtractPathParams<TEndpoint> | object;
    queryParams?: TQueryParams;
  }) => TQueryKey;
  fetcher: ({ finalUrl, signal }: { finalUrl: string; signal?: AbortSignal }) => Promise<TResponse>;
  defaultOptions?: Omit<UseQueryOptions<TResponse, unknown, TResponse, TQueryKey>, "queryKey" | "queryFn">;
};

export type InfiniteQueryBlueprintFromTemplate<
  TEndpoint extends string,
  TResponse = unknown,
  TQueryParams = undefined,
  TQueryKey extends QueryKey = QueryKey,
> = {
  endpoint: TEndpoint;
  headers?: HeadersInit;
  key: ({
    pathParams,
    queryParams,
  }: {
    pathParams: ExtractPathParams<TEndpoint> | object;
    queryParams?: TQueryParams;
  }) => TQueryKey;
  fetcher: ({
    finalUrl,
    signal,
    pageParam,
  }: {
    finalUrl: string;
    signal?: AbortSignal;
    pageParam: number;
  }) => Promise<TResponse>;
  getNextPageParam: (lastPage: TResponse, allPages: TResponse[]) => number | undefined;
  initialPageParam: number;
  defaultOptions?: Partial<UseInfiniteQueryOptions<TResponse, unknown, InfiniteData<TResponse, number>, QueryKey>>;
};

export type MutationBlueprintFromTemplate<
  TEndpoint extends string,
  TResponse = unknown,
  TQueryParams = undefined,
  TPayload = undefined,
> = {
  endpoint: TEndpoint;
  fetcher: ({
    finalUrl,
    payload,
    signal,
  }: {
    finalUrl: string;
    payload?: TPayload;
    signal?: AbortSignal;
  }) => Promise<TResponse>;
  invalidateQueries?: string[] | string; // Query keys to invalidate after successful mutation
  defaultOptions?: Omit<
    UseMutationOptions<
      TResponse,
      unknown,
      TQueryParams extends undefined
        ? TPayload extends undefined
          ? { pathParams: ExtractPathParams<TEndpoint> } // Exclude both queryParams and payload
          : { pathParams: ExtractPathParams<TEndpoint>; payload: TPayload } // Include payload only
        : TPayload extends undefined
          ? { pathParams: ExtractPathParams<TEndpoint>; queryParams: TQueryParams } // Include queryParams only
          : { pathParams: ExtractPathParams<TEndpoint>; queryParams: TQueryParams; payload: TPayload }, // Include both queryParams and payload
      unknown
    >,
    "mutationFn"
  >;
};
