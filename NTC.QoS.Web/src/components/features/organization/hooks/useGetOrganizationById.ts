import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";
import { getOrganizationByIdRequest } from "../handlers/getOrganizationByIdRequest";

export const useGetOrganizationById = createQueryHookFromTemplate(getOrganizationByIdRequest);
