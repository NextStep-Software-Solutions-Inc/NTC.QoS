/**
 * React Query Hooks for Connectivity Assessment
 */

import { useQuery } from "@tanstack/react-query";
import { buildEndpoint } from "@/lib/endpoint-builder";
import { createMutationHookFromTemplate } from "@/hooks/createMutationHookFromTemplate";
import {
    getConnectivityAssessmentStatusBlueprint,
    startConnectivityAssessmentBlueprint,
} from "../handlers/connectivity-assessment";

/**
 * Hook to start a new connectivity assessment job
 */
export const useStartConnectivityAssessment = createMutationHookFromTemplate(
    startConnectivityAssessmentBlueprint
);

/**
 * Hook to get connectivity assessment job status
 */
export const useConnectivityAssessmentStatus = (jobId: string | null, forceVerification = false) => {
    const pathParams = { jobId: jobId || "" };
    const queryParams = { forceVerification };
    const finalUrl = buildEndpoint(
        getConnectivityAssessmentStatusBlueprint.endpoint,
        pathParams,
        queryParams
    );
    const queryKey = getConnectivityAssessmentStatusBlueprint.key({
        url: finalUrl,
        pathParams,
        queryParams,
    });

    return useQuery({
        queryKey,
        queryFn: ({ signal }) =>
            getConnectivityAssessmentStatusBlueprint.fetcher({ finalUrl, signal }),
        enabled: !!jobId,
        refetchInterval: (query) => {
            const data = query.state.data?.data;
            // Poll every 2 seconds if job is running or pending/processing
            const isActive =
                data?.isRunning ||
                data?.status === "Pending" ||
                data?.status === "Processing";
            return isActive ? 2000 : false;
        },
    });
};
