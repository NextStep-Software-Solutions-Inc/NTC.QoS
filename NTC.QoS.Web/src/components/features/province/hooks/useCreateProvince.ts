import { createMutationHookFromTemplate } from "@/hooks/createMutationHookFromTemplate";
import { createProvinceRequest } from "../handlers/createProvinceRequest";

export const useCreateProvince = createMutationHookFromTemplate(createProvinceRequest);
