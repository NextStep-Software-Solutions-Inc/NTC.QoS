import { useQuery } from "@tanstack/react-query";

import { getDashboardData } from "../services/mockData";
import { DashboardData } from "../types";

export const useBroadbandDashboardData = (region: string, dateRange?: { from: Date; to: Date }) => {
  return useQuery<DashboardData>({
    queryKey: ["broadband-dashboard", region, dateRange],
    queryFn: () => getDashboardData(region, dateRange),
  });
};
