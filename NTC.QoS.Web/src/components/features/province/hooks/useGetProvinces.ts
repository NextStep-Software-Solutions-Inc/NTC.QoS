import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";
import { getProvincesRequest } from "../handlers/getProvincesRequest";

export const useGetProvinces = createQueryHookFromTemplate(getProvincesRequest);
