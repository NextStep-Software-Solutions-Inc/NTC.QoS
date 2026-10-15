import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";
import { speedtestAnalyticsRequest } from "../handlers/speedtest-analytics-handler";

/**
 * React Query hook for fetching speedtest analytics
 * @param params Query parameters for filtering analytics data
 * @param enabled Whether the query should be enabled (default: true)
 */
export const useSpeedtestAnalytics = createQueryHookFromTemplate(speedtestAnalyticsRequest);
