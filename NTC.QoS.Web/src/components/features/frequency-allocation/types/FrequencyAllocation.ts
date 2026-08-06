import { type Provider } from "../../provider/types/Provider";
import { type Link } from "../../link/types/Link";

export type FrequencyAllocation = {
  id: string;
  name: string;
  notes: string;
  isActive: boolean;
  provider: Provider;
  upLink: Link;
  downLink: Link;
};

export type FrequencyAllocationDetails = FrequencyAllocation;

export type CreateFrequencyAllocationPayload = {
  name: string;
  notes: string;
  providerId: string;
  uplinkId: string;
  downlinkId: string;
};

export type EditFrequencyAllocationPayload = {
  name?: string;
  notes?: string;
  isActive?: boolean;
  uplinkId?: string;
  downlinkId?: string;
};
