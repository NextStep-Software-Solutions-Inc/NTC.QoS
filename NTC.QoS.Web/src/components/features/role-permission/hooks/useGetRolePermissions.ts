import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";
import { getRolePermissionsRequest } from "../handlers/getRolePermissionsRequest";

export const useGetRolePermissions = createQueryHookFromTemplate(getRolePermissionsRequest);
