import { createMutationHookFromTemplate } from "@/hooks/createMutationHookFromTemplate";
import { createRolePermissionRequest } from "../handlers/createRolePermissionRequest";

export const useCreateRolePermission = createMutationHookFromTemplate(createRolePermissionRequest);
