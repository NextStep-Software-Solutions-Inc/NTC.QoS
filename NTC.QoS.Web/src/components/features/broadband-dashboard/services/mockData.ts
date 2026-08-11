import { DashboardData } from "../types";

export const getDashboardData = async (
  _region: string,
  _dateRange?: { from: Date; to: Date },
): Promise<DashboardData> => {
  // Simulate API delay
  await new Promise((resolve) => setTimeout(resolve, 1000));

  // Mock data matching the screenshot
  return {
    cellTowerCount: 6669,
    networkAccessPoints: 1859,
    fiberCoverage: { percentage: 67, description: "High-speed internet available in major cities." },
    mobileCoverage: { percentage: 82, description: "LTE/5G connectivity in most areas." },
    location: { status: "Served", area: "3378 km²" },
    demographics: { population: 957000, households: 230000, density: "283 / km²" },
    serviceProviders: [
      { name: "Dito", count: 1234, fill: "#E60000" },
      { name: "Globe", count: 856, fill: "#0057B8" },
      { name: "Smart", count: 645, fill: "#009E49" },
    ],
    topProvinces: [
      { name: "Misamis Oriental", count: 2745 },
      { name: "Bukidnon", count: 2087 },
      { name: "Misamis Occidental", count: 1958 },
      { name: "Lanao Del Norte", count: 1287 },
      { name: "Camiguin", count: 712 },
    ],
    cmtsScores: [
      { subject: "Fiber Coverage", a: 1, fullMark: 3 },
      { subject: "Internet Access", a: 1, fullMark: 3 },
      { subject: "Technology Type", a: 1, fullMark: 3 },
      { subject: "Quality of Service", a: 2, fullMark: 3 },
      { subject: "Mobile Coverage", a: 2, fullMark: 3 },
    ],
    averageSpeed: { download: 28.36, upload: 12.33, totalTests: 546, meetsRequirements: true },
  };
};
