import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";
import { getRegionsRequest } from "../handlers/getRegionsRequest";

export const useGetRegions = createQueryHookFromTemplate(getRegionsRequest);
