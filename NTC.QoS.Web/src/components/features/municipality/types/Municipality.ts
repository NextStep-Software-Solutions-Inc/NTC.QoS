import type { Province } from "../../province/types/Province";

export type Municipality = {
  id: string;
  name: string;
  description?: string;
  code?: string;
  area?: number;
  isCity: boolean;
  province?: Province;
};

export type CreateMunicipalityPayload = {
  name: string;
  description?: string;
  code?: string;
  area?: number;
  isCity: boolean;
  provinceId: string;
  boundariesFile?: File;
};

export type UpdateMunicipalityPayload = Partial<CreateMunicipalityPayload>;

export type MunicipalityDetails = Municipality;
