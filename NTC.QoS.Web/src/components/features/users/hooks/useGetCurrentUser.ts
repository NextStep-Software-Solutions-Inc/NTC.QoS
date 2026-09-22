import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";
import { getCurrentUserRequest } from "../handlers/getCurrentUserRequest";

export const useGetCurrentUser = createQueryHookFromTemplate(getCurrentUserRequest);
