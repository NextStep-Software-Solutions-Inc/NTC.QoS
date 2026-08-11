import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";
import { getAllUsersRequest } from "../handlers/getAllUsersRequest";

export const useGetUsers = createQueryHookFromTemplate(getAllUsersRequest);
