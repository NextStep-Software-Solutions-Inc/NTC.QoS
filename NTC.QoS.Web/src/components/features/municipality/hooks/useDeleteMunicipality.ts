import { createMutationHookFromTemplate } from "@/hooks/createMutationHookFromTemplate";
import { deleteMunicipalityRequest } from "../handlers/deleteMunicipalityRequest";

export const useDeleteMunicipality = createMutationHookFromTemplate(deleteMunicipalityRequest);
