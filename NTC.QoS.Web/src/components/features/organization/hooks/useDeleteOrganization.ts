import { createMutationHookFromTemplate } from "@/hooks/createMutationHookFromTemplate";
import { deleteOrganizationRequest } from "../handlers/deleteOrganizationRequest";

export const useDeleteOrganization = createMutationHookFromTemplate(deleteOrganizationRequest);
