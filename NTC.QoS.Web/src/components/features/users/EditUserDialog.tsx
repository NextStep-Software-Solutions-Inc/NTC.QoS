"use client";

import React, { useState } from "react";
import { toast } from "sonner";

import { getAllRolesRequest } from "@/components/features/users/handlers/getAllRolesRequest";
import type { Role } from "@/components/features/users/handlers/getAllRolesRequest";
import { updateUserRequest } from "@/components/features/users/handlers/updateUserRequest";
import type { User } from "@/components/features/users/types/User";
import { ResponsiveDialog } from "@/components/ResponsiveDialog";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { createMutationHookFromTemplate } from "@/hooks/createMutationHookFromTemplate";
import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";

import { usePermissions } from "@/providers/PermissionsContext";

const useUpdateUser = createMutationHookFromTemplate(updateUserRequest);
const useGetAllRoles = createQueryHookFromTemplate(getAllRolesRequest);

interface EditUserDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  user: User | null;
  onSuccess: () => void;
}

export function EditUserDialog({ open, onOpenChange, user, onSuccess }: EditUserDialogProps) {
  const { currentUser } = usePermissions();
  const isSelf = Boolean(currentUser && user && currentUser.id === user.id);
  const [form, setForm] = useState({ roleId: "" });

  const { data: rolesData } = useGetAllRoles({ pathParams: {}, queryParams: {} });
  const updateUserMutation = useUpdateUser();

  const roles = rolesData?.data ?? [];

  // Update form when user changes
  React.useEffect(() => {
    if (user) {
      setForm({ roleId: user.role?.id ?? "" });
    }
  }, [user]);

  const handleSubmit = async () => {
    if (!user || !form.roleId) {
      toast.error("Please select a role");
      return;
    }

    try {
      await updateUserMutation.mutateAsync({ pathParams: { id: user.id }, payload: { roleId: form.roleId } });
      toast.success("User role updated successfully!");
      onOpenChange(false);
      onSuccess();
    } catch {
      toast.error("Failed to update user");
    }
  };

  const handleCancel = () => {
    setForm({ roleId: user?.role?.id ?? "" });
    onOpenChange(false);
  };

  return (
    <ResponsiveDialog
      open={open}
      onOpenChange={onOpenChange}
      title="Edit User"
      description="Update user primary system role">
      <div className="space-y-4">
        {isSelf && (
          <div className="p-3 bg-amber-500/10 border border-amber-500/20 rounded-md text-xs text-amber-700 dark:text-amber-400">
            <strong>Self-Management Restriction:</strong> You cannot modify your own role. Another administrator must make these changes.
          </div>
        )}

        <div>
          <Label>User ID</Label>
          <div className="font-mono text-sm bg-muted px-3 py-2 rounded-md">{user?.authUserId}</div>
        </div>

        <div>
          <Label htmlFor="edit-role">Role</Label>
          <Select
            value={form.roleId}
            onValueChange={(value) => setForm((prev) => ({ ...prev, roleId: value }))}
            disabled={isSelf}>
            <SelectTrigger id="edit-role">
              <SelectValue placeholder="Select a role" />
            </SelectTrigger>
            <SelectContent>
              {roles.map((role: Role) => (
                <SelectItem key={role.id} value={role.id}>
                  {role.name}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>

        <div className="flex justify-end gap-2 pt-4">
          <Button variant="outline" onClick={handleCancel}>
            Cancel
          </Button>
          <Button onClick={handleSubmit} disabled={isSelf || updateUserMutation.isPending}>
            {updateUserMutation.isPending ? "Updating..." : "Update Role"}
          </Button>
        </div>
      </div>
    </ResponsiveDialog>
  );
}
