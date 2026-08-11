import { useQuery } from "@tanstack/react-query";

import { buildEndpoint } from "@/lib/endpoint-builder";
import { type ExtractPathParams, type QueryBlueprintFromTemplate } from "@/types/reusable/api";

type QueryHookArgs<TEndpoint extends string, TQueryParams> = TQueryParams extends undefined
  ? ExtractPathParams<TEndpoint> extends Record<string, string>
    ? { enabled?: boolean }
    : { pathParams: ExtractPathParams<TEndpoint>; enabled?: boolean }
  : { pathParams: ExtractPathParams<TEndpoint>; queryParams: TQueryParams; enabled?: boolean };

export function createQueryHookFromTemplate<TEndpoint extends string, TResponse = unknown, TQueryParams = undefined>(
  blueprint: QueryBlueprintFromTemplate<TEndpoint, TResponse, TQueryParams>,
) {
  return (args: QueryHookArgs<TEndpoint, TQueryParams>) => {
    const queryParams = "queryParams" in args ? args.queryParams : ({} as TQueryParams);
    const pathParams = "pathParams" in args ? args.pathParams : ({} as ExtractPathParams<TEndpoint>);
    const enabled = "enabled" in args ? args.enabled : true;
    const finalUrl = buildEndpoint(blueprint.endpoint, pathParams, queryParams);
    const queryKey = blueprint.key({ url: finalUrl, pathParams, queryParams });
    return useQuery({
      queryKey,
      queryFn: ({ signal }) => blueprint.fetcher({ finalUrl, signal }),
      enabled,
      ...blueprint.defaultOptions,
    });
  };
}
