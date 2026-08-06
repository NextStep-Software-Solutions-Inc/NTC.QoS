import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";
import { getBarangayDetailsRequest } from "../handlers/getBarangayDetailsRequest";

/**
 * Hook for fetching barangay details with boundaries
 */
export const useBarangayDetails = createQueryHookFromTemplate(getBarangayDetailsRequest);
