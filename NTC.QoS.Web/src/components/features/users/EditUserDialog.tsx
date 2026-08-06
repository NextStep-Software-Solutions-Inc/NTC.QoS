"use client";

import React, { useState } from "react";
import { toast } from "sonner";

import { getAllOrganizationsRequest } from "@/components/features/users/handlers/getAllOrganizationsRequest";
import type { Organization } from "@/components/features/users/handlers/getAllOrganizationsRequest";
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

const useUpdateUser = createMutationHookFromTemplate(updateUserRequest);
const useGetAllRoles = createQueryHookFromTemplate(getAllRolesRequest);
const useGetAllOrganizations = createQueryHookFromTemplate(getAllOrganizationsRequest);

interface EditUserDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  user: User | null;
  onSuccess: () => void;
}

export function EditUserDialog({ open, onOpenChange, user, onSuccess }: EditUserDialogProps) {
  const [form, setForm] = useState({ roleId: "", organizationId: "" });

  const { data: rolesData } = useGetAllRoles({ pathParams: {}, queryParams: {} });

  const { data: organizationsData } = useGetAllOrganizations({ pathParams: {}, queryParams: {} });

  const updateUserMutation = useUpdateUser();

  const roles = rolesData?.data ?? [];
  const organizations = organizationsData?.data ?? [];

  // Update form when user changes
  React.useEffect(() => {
    if (user) {
      setForm({ roleId: user.role.id, organizationId: user.organization.id });
    }
  }, [user]);

  const handleSubmit = async () => {
    if (!user || !form.roleId || !form.organizationId) {
      toast.error("Please fill in all fields");
      return;
    }

    try {
      await updateUserMutation.mutateAsync({ pathParams: { id: user.id }, payload: form });
      toast.success("User updated successfully!");
      onOpenChange(false);
      onSuccess();
    } catch {
      toast.error("Failed to update user");
    }
  };

  const handleCancel = () => {
    setForm({ roleId: user?.role.id ?? "", organizationId: user?.organization.id ?? "" });
    onOpenChange(false);
  };

  return (
    <ResponsiveDialog
      open={open}
      onOpenChange={onOpenChange}
      title="Edit User"
      description="Update user role and organization">
      <div className="space-y-4">
        <div>
          <Label>User ID</Label>
          <div className="font-mono text-sm bg-muted px-3 py-2 rounded-md">{user?.authUserId}</div>
        </div>

        <div>
          <Label htmlFor="edit-role">Role</Label>
          <Select value={form.roleId} onValueChange={(value) => setForm((prev) => ({ ...prev, roleId: value }))}>
            <SelectTrigger>
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

        <div>
          <Label htmlFor="edit-organization">Organization</Label>
          <Select
            value={form.organizationId}
            onValueChange={(value) => setForm((prev) => ({ ...prev, organizationId: value }))}>
            <SelectTrigger>
              <SelectValue placeholder="Select an organization" />
            </SelectTrigger>
            <SelectContent>
              {organizations.map((org: Organization) => (
                <SelectItem key={org.id} value={org.id}>
                  {org.name}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>

        <div className="flex justify-end gap-2 pt-4">
          <Button variant="outline" onClick={handleCancel}>
            Cancel
          </Button>
          <Button onClick={handleSubmit} disabled={updateUserMutation.isPending}>
            {updateUserMutation.isPending ? "Updating..." : "Update User"}
          </Button>
        </div>
      </div>
    </ResponsiveDialog>
  );
}
