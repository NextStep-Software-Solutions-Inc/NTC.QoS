import { createMutationHookFromTemplate } from "@/hooks/createMutationHookFromTemplate";
import { deleteRolePermissionRequest } from "../handlers/deleteRolePermissionRequest";

export const useDeleteRolePermission = createMutationHookFromTemplate(deleteRolePermissionRequest);
