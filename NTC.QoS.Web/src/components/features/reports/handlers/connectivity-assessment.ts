/**
 * API Handler for Connectivity Assessment Operations
 */

import { request } from "@/services/fetch";
import type { APIResponse, MutationBlueprintFromTemplate, QueryBlueprintFromTemplate } from "@/types/reusable/api";
import type {
    ConnectivityAssessmentJobRequest,
    ConnectivityAssessmentJobResponse,
    ConnectivityAssessmentStatusResponse,
} from "../types/connectivity-assessment";

/**
 * Start a new connectivity assessment job
 * POST /analytics/connectivity-assessment/background
 */
export const startConnectivityAssessmentBlueprint: MutationBlueprintFromTemplate<
    "/analytics/connectivity-assessment/background",
    ConnectivityAssessmentJobResponse,
    undefined,
    ConnectivityAssessmentJobRequest
> = {
    endpoint: "/analytics/connectivity-assessment/background",
    fetcher: async ({ finalUrl, payload, signal }) => {
        return await request.post<ConnectivityAssessmentJobResponse>(
            finalUrl,
            payload,
            signal ? { signal } : undefined
        );
    },
};

/**
 * Get connectivity assessment job status
 * GET /analytics/connectivity-assessment/job/{jobId}/status
 */
export const getConnectivityAssessmentStatusBlueprint: QueryBlueprintFromTemplate<
    "/analytics/connectivity-assessment/job/:jobId/status",
    ConnectivityAssessmentStatusResponse,
    { forceVerification?: boolean }
> = {
    endpoint: "/analytics/connectivity-assessment/job/:jobId/status",
    key: ({ url, pathParams, queryParams }) =>
        ["get-connectivity-assessment-status", url, pathParams, queryParams] as const,
    fetcher: async ({ finalUrl, signal }) => {
        return await request.get<ConnectivityAssessmentStatusResponse>(finalUrl, { signal });
    },
};
