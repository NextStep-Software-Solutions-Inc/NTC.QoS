export type Cellular = {
  id: string;
  cellSectorId?: string;
  serviceProviderId?: string;
  technologyId?: string;
  bandId?: string;
  address?: string;
  // Technology details (returned from API)
  technologyStandard?: string;
  technologyDescription?: string;
  // Band details (returned from API)
  bandName?: string;
  bandDescription?: string;
  // Additional band details
  bands?: Array<{
    id: string;
    name: string;
    description?: string;
  }>;
};

export type CellularDetails = Cellular;
