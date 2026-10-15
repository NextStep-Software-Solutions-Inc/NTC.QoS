import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";
import { getServicesRequest } from "../handlers/getServicesRequest";

export const useGetServices = createQueryHookFromTemplate(getServicesRequest);
