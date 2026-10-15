import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";
import { getBarangaysRequest } from "../handlers/getBarangaysRequest";

export const useGetBarangays = createQueryHookFromTemplate(getBarangaysRequest);
