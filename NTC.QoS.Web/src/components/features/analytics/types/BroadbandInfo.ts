export type BroadbandInfoSummary = {
  totalCells: number;
  totalNaps: number;
  totalAssets: number;
  coverageAreaKm2: number;
  serviceProviderCount: number;
  municipalityCount: number;
  assetDensityPerKm2: number;
  municipalitiesWithAssets: number;
  municipalitiesWithoutAssets: number;
};

export type ServiceProviderInfo = {
  id: string;
  name: string;
  color: string;
  serviceType: string;
  cellCount: number;
  napCount: number;
  totalAssets: number;
};

export type EnvelopeInfo = {
  areaSizeKm2: number;
  geometryType: string;
  boundingBox: { minLatitude: number; maxLatitude: number; minLongitude: number; maxLongitude: number };
  centerPoint: { latitude: number; longitude: number };
  radiusMeters: number;
};

export type MunicipalityInfo = {
  id: string;
  name: string;
  cellCount: number;
  napCount: number;
  totalAssets: number;
  assetPercentage: number;
  hasBoundaryData: boolean;
  isCoveredByQuery: boolean;
  hasAssets: boolean;
  coverageStatus: string;
  displayName: string;
};

export type BroadbandInfoData = {
  summary: BroadbandInfoSummary;
  serviceProviders: ServiceProviderInfo[];
  envelopeInfo: EnvelopeInfo;
  municipalities: MunicipalityInfo[];
};

export type PointBroadbandInfoPayload = { latitude: number; longitude: number; radiusMeters: number };

export type AreaBroadbandInfoPayload = {
  minLatitude: number;
  maxLatitude: number;
  minLongitude: number;
  maxLongitude: number;
};
