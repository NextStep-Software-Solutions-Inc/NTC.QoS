import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";
import { getOrganizationsRequest } from "../handlers/getOrganizationsRequest";

export const useGetOrganizations = createQueryHookFromTemplate(getOrganizationsRequest);
