import { createMutationHookFromTemplate } from "@/hooks/createMutationHookFromTemplate";
import { createMunicipalityRequest } from "../handlers/createMunicipalityRequest";

export const useCreateMunicipality = createMutationHookFromTemplate(createMunicipalityRequest);
