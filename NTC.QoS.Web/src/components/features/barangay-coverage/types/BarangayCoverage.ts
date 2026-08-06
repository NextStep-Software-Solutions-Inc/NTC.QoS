// Light response for list endpoint (without relationship data)
export type BarangayCoverageLightDto = {
  id: string;
  barangayId: string;
  wiredFtthPercentage: number;
  wirelessPercentage: number;
  combinedPercentage: number;
  isWiredCovered: boolean;
  isWirelessCovered: boolean;
  updatedAt: string;
  updatedBy: string;
};

// Full response for single record endpoint (with relationship data)
export type BarangayCoverageDto = BarangayCoverageLightDto & {
  barangayName: string;
  barangayCode: string;
};

export type CreateBarangayCoverageDto = {
  barangayId: string;
  wiredFtthPercentage: number;
  wirelessPercentage: number;
};

export type UpdateBarangayCoverageDto = {
  wiredFtthPercentage: number;
  wirelessPercentage: number;
};

// Filter params for list endpoint
export type BarangayCoverageFilterParams = {
  regionIds?: string[];
  provinceIds?: string[];
  municipalityIds?: string[];
  barangayIds?: string[];
};

