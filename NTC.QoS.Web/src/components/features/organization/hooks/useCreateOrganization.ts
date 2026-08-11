import { createMutationHookFromTemplate } from "@/hooks/createMutationHookFromTemplate";
import { createOrganizationRequest } from "../handlers/createOrganizationRequest";

export const useCreateOrganization = createMutationHookFromTemplate(createOrganizationRequest);
