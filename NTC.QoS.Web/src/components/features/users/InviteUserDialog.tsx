"use client";

import { useState } from "react";
import { toast } from "sonner";

import { ResponsiveDialog } from "@/components/ResponsiveDialog";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { createMutationHookFromTemplate } from "@/hooks/createMutationHookFromTemplate";
import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";
import { extractErrorMessage } from "@/lib/extract-error-message";
import { getAllOrganizationsRequest } from "./handlers/getAllOrganizationsRequest";
import { getAllRolesRequest } from "./handlers/getAllRolesRequest";
import { inviteUserRequest } from "./handlers/inviteUserRequest";

const useGetAllRoles = createQueryHookFromTemplate(getAllRolesRequest);
const useGetAllOrganizations = createQueryHookFromTemplate(getAllOrganizationsRequest);
const useInviteUser = createMutationHookFromTemplate(inviteUserRequest);

interface InviteUserDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onSuccess?: () => void;
}

export function InviteUserDialog({ open, onOpenChange, onSuccess }: InviteUserDialogProps) {
  const [formData, setFormData] = useState({ email: "", roleId: "", organizationId: "" });

  const { data: rolesData } = useGetAllRoles({ pathParams: {}, queryParams: {} });

  const { data: organizationsData } = useGetAllOrganizations({ pathParams: {}, queryParams: {} });

  const inviteUserMutation = useInviteUser();

  const roles = rolesData?.data ?? [];
  const organizations = organizationsData?.data ?? [];

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!formData.email || !formData.roleId || !formData.organizationId) {
      toast.error("Please fill in all fields");
      return;
    }
    toast.promise(inviteUserMutation.mutateAsync({ pathParams: {}, payload: formData }), {
      loading: "Sending invitation...",
      success: () => {
        onOpenChange(false);
        setFormData({ email: "", roleId: "", organizationId: "" });
        onSuccess?.();
        return "User invitation sent successfully!";
      },
      error: (error) => extractErrorMessage(error),
    });
  };

  const handleCancel = () => {
    onOpenChange(false);
    setFormData({ email: "", roleId: "", organizationId: "" });
  };

  return (
    <ResponsiveDialog
      open={open}
      onOpenChange={onOpenChange}
      title="Invite User"
      description="Send an invitation to a new user">
      <form onSubmit={handleSubmit} className="space-y-4">
        <div>
          <Label htmlFor="email">Email Address</Label>
          <Input
            id="email"
            type="email"
            value={formData.email}
            onChange={(e) => setFormData((prev) => ({ ...prev, email: e.target.value }))}
            placeholder="user@example.com"
            required
          />
        </div>

        <div>
          <Label htmlFor="role">Role</Label>
          <Select
            value={formData.roleId}
            onValueChange={(value) => setFormData((prev) => ({ ...prev, roleId: value }))}>
            <SelectTrigger>
              <SelectValue placeholder="Select a role" />
            </SelectTrigger>
            <SelectContent>
              {roles.map((role) => (
                <SelectItem key={role.id} value={role.id}>
                  {role.name}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>

        <div>
          <Label htmlFor="organization">Organization</Label>
          <Select
            value={formData.organizationId}
            onValueChange={(value) => setFormData((prev) => ({ ...prev, organizationId: value }))}>
            <SelectTrigger>
              <SelectValue placeholder="Select an organization" />
            </SelectTrigger>
            <SelectContent>
              {organizations.map((org) => (
                <SelectItem key={org.id} value={org.id}>
                  {org.name}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>

        <div className="flex justify-end gap-2 pt-4">
          <Button type="button" variant="outline" onClick={handleCancel}>
            Cancel
          </Button>
          <Button type="submit" disabled={inviteUserMutation.isPending}>
            {inviteUserMutation.isPending ? "Sending..." : "Send Invitation"}
          </Button>
        </div>
      </form>
    </ResponsiveDialog>
  );
}
