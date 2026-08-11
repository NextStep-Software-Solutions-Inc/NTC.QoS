export interface DashboardData {
  cellTowerCount: number;
  networkAccessPoints: number;
  fiberCoverage: { percentage: number; description: string };
  mobileCoverage: { percentage: number; description: string };
  location: { status: string; area: string };
  demographics: { population: number; households: number; density: string };
  serviceProviders: { name: string; count: number; fill: string }[];
  topProvinces: { name: string; count: number }[];
  cmtsScores: { subject: string; a: number; fullMark: number }[];
  averageSpeed: { download: number; upload: number; totalTests: number; meetsRequirements: boolean };
}
