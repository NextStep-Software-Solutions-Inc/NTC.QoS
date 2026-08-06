import { createMutationHookFromTemplate } from "@/hooks/createMutationHookFromTemplate";
import { editBarangayRequest } from "../handlers/editBarangayRequest";

export const useEditBarangay = createMutationHookFromTemplate(editBarangayRequest);
