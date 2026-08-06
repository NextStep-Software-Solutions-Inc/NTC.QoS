import type { Region } from "../../region/types/Region";
import type { User } from "../../users/types/User";

export type UserRegion = { id: string; status: string; user: User; region: Region };
