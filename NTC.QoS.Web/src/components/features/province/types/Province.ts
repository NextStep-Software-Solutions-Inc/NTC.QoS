import type { Region } from "../../region/types/Region";

export type Province = {
  id: string;
  name: string;
  description?: string;
  code?: string;
  area?: number;
  region?: Region;
  municipalityCount?: number;
};

export type CreateProvincePayload = {
  name: string;
  description?: string;
  code?: string;
  area?: number;
  regionId: string;
  boundariesFile?: File;
};

export type UpdateProvincePayload = Partial<CreateProvincePayload>;

export type ProvinceDetails = Province;
