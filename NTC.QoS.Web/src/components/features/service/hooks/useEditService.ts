import { createMutationHookFromTemplate } from "@/hooks/createMutationHookFromTemplate";
import { editServiceRequest } from "../handlers/editServiceRequest";

export const useEditService = createMutationHookFromTemplate(editServiceRequest);
