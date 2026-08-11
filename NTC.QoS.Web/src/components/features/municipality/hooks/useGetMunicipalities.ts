import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";
import { getMunicipalitiesRequest } from "../handlers/getMunicipalitiesRequest";

export const useGetMunicipalities = createQueryHookFromTemplate(getMunicipalitiesRequest);
