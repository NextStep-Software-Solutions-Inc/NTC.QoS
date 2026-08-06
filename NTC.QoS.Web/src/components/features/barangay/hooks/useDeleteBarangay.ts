import { createMutationHookFromTemplate } from "@/hooks/createMutationHookFromTemplate";
import { deleteBarangayRequest } from "../handlers/deleteBarangayRequest";

export const useDeleteBarangay = createMutationHookFromTemplate(deleteBarangayRequest);
