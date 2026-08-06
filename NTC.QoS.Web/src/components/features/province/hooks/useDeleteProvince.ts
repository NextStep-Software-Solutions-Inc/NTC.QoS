import { createMutationHookFromTemplate } from "@/hooks/createMutationHookFromTemplate";
import { deleteProvinceRequest } from "../handlers/deleteProvinceRequest";

export const useDeleteProvince = createMutationHookFromTemplate(deleteProvinceRequest);
