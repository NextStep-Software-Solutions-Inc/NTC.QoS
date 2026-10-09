export interface CellularCoverageTileset {
  id: string;
  name: string;
  fileName: string;
  fileSize: number;
  ingestId: string;
  documentId?: string;
  tileJsonUrl?: string;
  status: number;
  statusText: string;
  errorMessage?: string;
  regionId?: string;
  regionName?: string;
  serviceProviderId?: string;
  serviceProviderName?: string;
  bandId?: string;
  bandName?: string;
  technologyId?: string;
  technologyName?: string;
  isActive: boolean;
  createdAt: string;
}

export interface InitTilesetIngestResult {
  tilesetId: string;
  ingestId: string;
  uploadUrl: string;
  state: string;
}

export interface InitTilesetIngestPayload {
  fileName: string;
  fileSize: number;
  displayName?: string;
  regionId?: string;
  serviceProviderId?: string;
  bandId?: string;
  technologyId?: string;
}
