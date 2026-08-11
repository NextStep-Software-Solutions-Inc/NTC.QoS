import type { Municipality } from "../../municipality/types/Municipality";

export type MultiPolygon = {
  type: "MultiPolygon";
  coordinates: number[][][][];
};

export type BarangayBoundaries = {
  type: "Feature";
  geometry: MultiPolygon;
  properties: {
    id: string;
    name: string;
    code: string;
    municipalityId: string;
  };
};

export type Barangay = {
  id: string;
  name: string;
  description?: string;
  code?: string;
  area?: number;
  municipality?: Municipality;
  boundaries?: BarangayBoundaries;
};

export type CreateBarangayPayload = {
  name: string;
  description?: string;
  code?: string;
  area?: number;
  municipalityId: string;
  boundariesFile?: File;
};

export type UpdateBarangayPayload = Partial<CreateBarangayPayload>;

export type BarangayDetails = Barangay;
