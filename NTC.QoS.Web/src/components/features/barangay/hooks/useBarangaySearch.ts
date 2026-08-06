import { useQuery } from "@tanstack/react-query";
import { getBarangaysRequest } from "../handlers/getBarangaysRequest";
import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";

/**
 * Hook for searching barangays by name (case-insensitive)
 * Returns paginated results from the backend API
 */
export const useBarangaySearch = createQueryHookFromTemplate(getBarangaysRequest);
