import { createMutationHookFromTemplate } from "@/hooks/createMutationHookFromTemplate";
import { editMunicipalityRequest } from "../handlers/editMunicipalityRequest";

export const useEditMunicipality = createMutationHookFromTemplate(editMunicipalityRequest);
