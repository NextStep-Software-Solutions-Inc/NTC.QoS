import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";
import { getPermissionsRequest } from "../handlers/getPermissionsRequest";

export const useGetPermissions = createQueryHookFromTemplate(getPermissionsRequest);
