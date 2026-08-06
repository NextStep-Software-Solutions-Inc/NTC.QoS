export type Organization = { id: string; name: string; description?: string };

export type OrganizationDetails = Organization & {};

export interface CreateOrganizationPayload {
  name: string;
  description?: string;
}

export interface UpdateOrganizationPayload {
  name?: string;
  description?: string;
}
