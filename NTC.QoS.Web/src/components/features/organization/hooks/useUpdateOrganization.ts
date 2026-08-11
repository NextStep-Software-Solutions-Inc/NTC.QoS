import { createMutationHookFromTemplate } from "@/hooks/createMutationHookFromTemplate";
import { updateOrganizationRequest } from "../handlers/updateOrganizationRequest";

export const useUpdateOrganization = createMutationHookFromTemplate(updateOrganizationRequest);
