import { createMutationHookFromTemplate } from "@/hooks/createMutationHookFromTemplate";
import { createServiceRequest } from "../handlers/createServiceRequest";

export const useCreateService = createMutationHookFromTemplate(createServiceRequest);
