import {
  useInfiniteQuery,
  type InfiniteData,
  type QueryKey,
  type UseInfiniteQueryOptions,
} from "@tanstack/react-query";

import { buildEndpoint } from "@/lib/endpoint-builder";
import { type ExtractPathParams, type InfiniteQueryBlueprintFromTemplate } from "@/types/reusable/api";

type InfiniteQueryHookArgs<TEndpoint extends string, TQueryParams> = TQueryParams extends undefined
  ? ExtractPathParams<TEndpoint> extends Record<string, string>
    ? { enabled?: boolean }
    : { pathParams: ExtractPathParams<TEndpoint>; enabled?: boolean }
  : { pathParams: ExtractPathParams<TEndpoint>; queryParams: TQueryParams; enabled?: boolean };

export function createInfiniteQueryHookFromTemplate<
  TEndpoint extends string,
  TResponse = unknown,
  TQueryParams = undefined,
  TQueryKey extends readonly unknown[] = readonly unknown[],
>(blueprint: InfiniteQueryBlueprintFromTemplate<TEndpoint, TResponse, TQueryParams, TQueryKey>) {
  return (args: InfiniteQueryHookArgs<TEndpoint, TQueryParams>) => {
    const queryParams = "queryParams" in args ? args.queryParams : ({} as TQueryParams);
    const pathParams = "pathParams" in args ? args.pathParams : ({} as ExtractPathParams<TEndpoint>);
    const enabled = "enabled" in args ? args.enabled : true;

    const queryKey: TQueryKey = blueprint.key({ pathParams, queryParams });

    const options: UseInfiniteQueryOptions<TResponse, unknown, InfiniteData<TResponse, number>, any> = {
      queryKey,
      queryFn: ({ signal, pageParam }) => {
        // Merge pageParam (page number) into query params
        const paramsWithPage = { ...queryParams, pageNumber: pageParam as number } as TQueryParams;

        const finalUrl = buildEndpoint(blueprint.endpoint, pathParams, paramsWithPage);
        return blueprint.fetcher({ finalUrl, signal, pageParam: pageParam as number });
      },
      initialPageParam: blueprint.initialPageParam,
      getNextPageParam: blueprint.getNextPageParam,
      enabled,
    };

    if (blueprint.defaultOptions) {
      Object.assign(options, blueprint.defaultOptions);
    }

    return useInfiniteQuery(options);
  };
}
