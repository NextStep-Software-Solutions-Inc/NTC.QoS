import { createMutationHookFromTemplate } from "@/hooks/createMutationHookFromTemplate";
import { createBarangayRequest } from "../handlers/createBarangayRequest";

export const useCreateBarangay = createMutationHookFromTemplate(createBarangayRequest);
