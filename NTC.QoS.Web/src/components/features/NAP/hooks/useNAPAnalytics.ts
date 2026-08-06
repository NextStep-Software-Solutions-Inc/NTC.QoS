import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";
import { getNAPAnalyticsRequest } from "../handlers/getNAPAnalyticsRequest";

/**
 * Custom hook to fetch NAP analytics data
 * Uses React Query for caching and state management
 */
export const useNAPAnalytics = createQueryHookFromTemplate(getNAPAnalyticsRequest);
