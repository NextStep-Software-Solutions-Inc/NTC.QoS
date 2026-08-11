import { type Band } from "../../band/types/Band";
import { type ServiceProviderDetails } from "../../service-provider/types/serviceProvider";
import { type Technology } from "../../technology/types/Technology";

export type Link = {
  id: string;
  type: string;
  centerFrequency: number;
  lowerFrequency: number;
  upperFrequency: number;
  rxThreshold: number;
  bandId: string;
  technologyBand: TechnologyBand;
  band?: Band;
  serviceProviderId: string;
  serviceProvider: ServiceProviderDetails;
};

export type LinkDetails = Link;

type TechnologyBand = { id: string; band: Band; technology: Technology };
