export type Provider = {
  id: string;
  name: string;
  color: string;
  description?: string;
  licenseNumber: string;
  createdDate: string;
  createdBy: string;
  modifiedDate: string;
  modifiedBy: string;
  logo?: string;
};

export type ProviderDetails = Provider;
