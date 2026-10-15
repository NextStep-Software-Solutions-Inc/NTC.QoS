import { createMutationHookFromTemplate } from "@/hooks/createMutationHookFromTemplate";
import { deleteServiceRequest } from "../handlers/deleteServiceRequest";

export const useDeleteService = createMutationHookFromTemplate(deleteServiceRequest);
