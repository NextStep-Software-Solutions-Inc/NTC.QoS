import { useRef } from "react";
import { useMutation, useQueryClient, type UseMutationResult } from "@tanstack/react-query";

import { buildEndpoint } from "@/lib/endpoint-builder";
import { type ExtractPathParams, type MutationBlueprintFromTemplate } from "@/types/reusable/api";

export function createMutationHookFromTemplate<
  TEndpoint extends string,
  TQueryParams = undefined,
  TPayload = unknown,
  TResponse = unknown,
>(blueprint: MutationBlueprintFromTemplate<TEndpoint, TResponse, TQueryParams, TPayload>) {
  return (): UseMutationResult<
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
  > & { abort: () => void } => {
    const abortControllerRef = useRef<AbortController | null>(null);
    const queryClient = useQueryClient();

    const mutation = useMutation({
      ...blueprint.defaultOptions,
      mutationFn: async (args) => {
        // Create a new AbortController for this request
        abortControllerRef.current = new AbortController();
        const signal = abortControllerRef.current.signal;

        const { pathParams } = args;
        const queryParams = "queryParams" in args ? args.queryParams : undefined;
        const payload = "payload" in args ? args.payload : undefined;
        const fullUrl = buildEndpoint(blueprint.endpoint, pathParams, queryParams);

        try {
          return await blueprint.fetcher({ finalUrl: fullUrl, payload, signal });
        } finally {
          abortControllerRef.current = null;
        }
      },
      onSuccess: (...args) => {
        // Invalidate queries if specified in the blueprint
        if (blueprint.invalidateQueries) {
          const queryKeys = Array.isArray(blueprint.invalidateQueries)
            ? blueprint.invalidateQueries
            : [blueprint.invalidateQueries];
          
          queryKeys.forEach((queryKey) => {
            queryClient.invalidateQueries({ queryKey: [queryKey] });
          });
        }
        
        // Call the blueprint's onSuccess if it exists
        blueprint.defaultOptions?.onSuccess?.(...args);
      },
      onSettled: (...args) => {
        // Clean up abort controller when mutation settles (success or error)
        abortControllerRef.current = null;
        
        // Call the blueprint's onSettled if it exists
        blueprint.defaultOptions?.onSettled?.(...args);
      },
    });

    // Add abort method to the mutation result
    return {
      ...mutation,
      abort: () => {
        if (abortControllerRef.current) {
          abortControllerRef.current.abort();
        }
      },
    };
  };
}
