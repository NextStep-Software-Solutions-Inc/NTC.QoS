export interface InvitationRegion {
  regionId: string;
  regionName: string;
  regionCode?: string;
  roleId?: string;
  roleName?: string;
  status: string;
}

export interface Invitation {
  id: string;
  email: string;
  status: "pending" | "accepted" | "revoked" | "expired" | string;
  url?: string;
  createdAt?: string;
  updatedAt?: string;
  roleId?: string;
  roleName?: string;
  roleScope?: string;
  regions: InvitationRegion[];
}
