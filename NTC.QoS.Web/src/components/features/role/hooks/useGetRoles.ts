import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";
import { getAllRolesRequest } from "../handlers/getAllRolesRequest";

export const useGetRoles = createQueryHookFromTemplate(getAllRolesRequest);
