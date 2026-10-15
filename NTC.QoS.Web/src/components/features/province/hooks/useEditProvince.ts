import { createMutationHookFromTemplate } from "@/hooks/createMutationHookFromTemplate";
import { editProvinceRequest } from "../handlers/editProvinceRequest";

export const useEditProvince = createMutationHookFromTemplate(editProvinceRequest);
