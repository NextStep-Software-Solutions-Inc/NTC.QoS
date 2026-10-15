import type { Region } from "../../region/types/Region";
import type { Role } from "../../role/types/Role";
import type { User } from "../../users/types/User";

export type UserRegion = {
  id: string;
  status: string;
  roleId?: string;
  role?: Role;
  user: User;
  region: Region;
};
