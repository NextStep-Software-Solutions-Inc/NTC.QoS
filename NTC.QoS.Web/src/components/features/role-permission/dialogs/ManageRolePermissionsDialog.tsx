"use client";

import { useEffect, useState } from "react";
import { CheckSquare, Loader2, Shield, Square } from "lucide-react";
import { toast } from "sonner";

import { useGetPermissions } from "@/components/features/permission/hooks/useGetPermissions";
import type { Permission } from "@/components/features/permission/types/Permission";
import { useCreateRolePermission } from "@/components/features/role-permission/hooks/useCreateRolePermission";
import { useDeleteRolePermission } from "@/components/features/role-permission/hooks/useDeleteRolePermission";
import { useGetRolePermissions } from "@/components/features/role-permission/hooks/useGetRolePermissions";
import type { Role } from "@/components/features/role/types/Role";
import { ResponsiveDialog } from "@/components/ResponsiveDialog";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { ScrollArea } from "@/components/ui/scroll-area";
import { Separator } from "@/components/ui/separator";

interface ManageRolePermissionsDialogProps {
  role: Role | null;
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onSuccess?: () => void;
}

export function ManageRolePermissionsDialog({ role, open, onOpenChange, onSuccess }: ManageRolePermissionsDialogProps) {
  const [searchQuery, setSearchQuery] = useState("");
  const [debouncedSearch, setDebouncedSearch] = useState("");
  const [selectedPermissions, setSelectedPermissions] = useState<Set<string>>(new Set());
  const [initialPermissions, setInitialPermissions] = useState<Set<string>>(new Set());

  // Debounced search (300ms)
  useEffect(() => {
    const timer = setTimeout(() => {
      setDebouncedSearch(searchQuery);
    }, 300);
    return () => clearTimeout(timer);
  }, [searchQuery]);

  // Fetch permissions with server-side search
  const { data: permissionsData, isLoading: isLoadingPermissions } = useGetPermissions({
    pathParams: {},
    queryParams: { pageNumber: 1, pageSize: 1000, search: debouncedSearch || undefined },
  });

  // Fetch current role permissions by filtering with roleIds
  const {
    data: rolePermissionsData,
    isLoading: isLoadingRolePermissions,
    refetch: refetchRolePermissions,
  } = useGetRolePermissions({
    pathParams: {},
    queryParams: { pageNumber: 1, pageSize: 10000, roleIds: role?.id ? [role.id] : undefined },
    enabled: !!role?.id && open,
  });

  const { mutateAsync: createRolePermission } = useCreateRolePermission();
  const { mutateAsync: deleteRolePermission } = useDeleteRolePermission();
  const [isSaving, setIsSaving] = useState(false);

  const permissions = permissionsData?.data ?? [];
  // Filter role permissions to only include the current role (safety check)
  const allRolePermissions = rolePermissionsData?.data ?? [];
  const rolePermissions = allRolePermissions.filter((rp) => rp.role.id === role?.id);

  // Initialize selected permissions when role permissions are loaded
  useEffect(() => {
    if (!open) return;

    if (rolePermissions.length > 0 && role?.id) {
      const permissionIds = new Set(rolePermissions.map((rp) => rp.permission.id));
      setSelectedPermissions(permissionIds);
      setInitialPermissions(permissionIds);
    } else if (!isLoadingRolePermissions) {
      setSelectedPermissions(new Set());
      setInitialPermissions(new Set());
    }
  }, [open, role?.id, isLoadingRolePermissions, allRolePermissions.length]);

  // Reset state when dialog closes
  useEffect(() => {
    if (!open) {
      setSearchQuery("");
      setDebouncedSearch("");
      setSelectedPermissions(new Set());
      setInitialPermissions(new Set());
    }
  }, [open]);

  const handleTogglePermission = (permissionId: string) => {
    setSelectedPermissions((prev) => {
      const newSet = new Set(prev);
      if (newSet.has(permissionId)) {
        newSet.delete(permissionId);
      } else {
        newSet.add(permissionId);
      }
      return newSet;
    });
  };

  // Server-side search is used; permissions already filtered by debouncedSearch

  const handleToggleAll = () => {
    if (selectedPermissions.size === permissions.length) {
      // Deselect all visible permissions
      setSelectedPermissions((prev) => {
        const newSet = new Set(prev);
        permissions.forEach((p) => newSet.delete(p.id));
        return newSet;
      });
    } else {
      // Select all visible permissions
      setSelectedPermissions((prev) => {
        const newSet = new Set(prev);
        permissions.forEach((p) => newSet.add(p.id));
        return newSet;
      });
    }
  };

  const handleSave = async () => {
    if (!role) return;

    setIsSaving(true);

    try {
      // Calculate permissions to add and remove
      const permissionsToAdd = Array.from(selectedPermissions).filter((permId) => !initialPermissions.has(permId));
      const permissionsToRemove = Array.from(initialPermissions).filter((permId) => !selectedPermissions.has(permId));

      // Find the role permission IDs to delete
      const rolePermissionsToDelete = rolePermissions
        .filter((rp) => permissionsToRemove.includes(rp.permission.id))
        .map((rp) => rp.id);

      // Execute all operations
      const operations = [
        ...permissionsToAdd.map((permissionId) =>
          createRolePermission({ pathParams: {}, payload: { roleId: role.id, permissionId } }),
        ),
        ...rolePermissionsToDelete.map((rpId) => deleteRolePermission({ pathParams: { id: rpId } })),
      ];

      await Promise.all(operations);

      toast.success(`Permissions updated successfully for role "${role.name}"`);
      await refetchRolePermissions();
      onOpenChange(false);
      onSuccess?.();
    } catch (error) {
      const err = error as Error;
      toast.error(`Failed to update permissions: ${err.message || "Unknown error"}`);
    } finally {
      setIsSaving(false);
    }
  };

  const hasChanges = !arePermissionSetsEqual(selectedPermissions, initialPermissions);
  const allFilteredSelected = permissions.length > 0 && permissions.every((p) => selectedPermissions.has(p.id));

  return (
    <ResponsiveDialog
      open={open}
      onOpenChange={onOpenChange}
      title={
        <div className="flex items-center gap-2">
          <Shield className="h-5 w-5" />
          <span>Manage Permissions</span>
        </div>
      }
      description={
        role ? (
          <div className="space-y-1">
            <div className="flex items-center gap-2">
              <span>Role:</span>
              <Badge variant="outline">{role.name}</Badge>
            </div>
            <p className="text-xs text-muted-foreground">{role.description || "No description"}</p>
          </div>
        ) : (
          "Select permissions for this role"
        )
      }>
      <div className="space-y-4">
        {/* Search and Stats */}
        <div className="space-y-2">
          <Input
            placeholder="Search permissions..."
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            className="w-full"
          />
          <div className="flex items-center justify-between text-sm text-muted-foreground">
            <span>
              {selectedPermissions.size} of {permissions.length} permissions selected
            </span>
            {hasChanges && (
              <Badge variant="secondary" className="text-xs">
                Unsaved changes
              </Badge>
            )}
          </div>
        </div>

        <Separator />

        {/* Permissions List */}
        {isLoadingPermissions || isLoadingRolePermissions ? (
          <div className="flex items-center justify-center py-8">
            <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
          </div>
        ) : (
          <>
            {/* Select/Deselect All */}
            {permissions.length > 0 && (
              <div className="flex items-center gap-2 py-2">
                <Button type="button" variant="outline" size="sm" onClick={handleToggleAll} className="gap-2">
                  {allFilteredSelected ? (
                    <>
                      <Square className="h-4 w-4" />
                      Deselect All
                    </>
                  ) : (
                    <>
                      <CheckSquare className="h-4 w-4" />
                      Select All
                    </>
                  )}
                </Button>
                {searchQuery && (
                  <span className="text-xs text-muted-foreground">({permissions.length} filtered)</span>
                )}
              </div>
            )}

            <ScrollArea className="h-[400px] pr-4">
              {permissions.length === 0 ? (
                <div className="text-center py-8 text-muted-foreground">
                  {searchQuery ? "No permissions match your search" : "No permissions available"}
                </div>
              ) : (
                <div className="space-y-3">
                  {permissions.map((permission) => (
                    <div
                      key={permission.id}
                      className="flex items-start gap-3 p-3 rounded-lg border hover:bg-accent transition-colors">
                      <Checkbox
                        id={`permission-${permission.id}`}
                        checked={selectedPermissions.has(permission.id)}
                        onCheckedChange={() => handleTogglePermission(permission.id)}
                        className="mt-1"
                      />
                      <Label htmlFor={`permission-${permission.id}`} className="flex-1 cursor-pointer space-y-1">
                        <div className="font-medium">{permission.name}</div>
                        <div className="flex items-center gap-2">
                          <code className="text-xs bg-muted px-2 py-0.5 rounded">{permission.key}</code>
                        </div>
                        {permission.description && (
                          <p className="text-sm text-muted-foreground">{permission.description}</p>
                        )}
                      </Label>
                    </div>
                  ))}
                </div>
              )}
            </ScrollArea>
          </>
        )}

        {/* Actions */}
        <Separator />
        <div className="flex justify-end gap-2">
          <Button type="button" variant="outline" onClick={() => onOpenChange(false)} disabled={isSaving}>
            Cancel
          </Button>
          <Button
            type="button"
            onClick={handleSave}
            disabled={isSaving || !hasChanges || isLoadingPermissions || isLoadingRolePermissions}>
            {isSaving ? (
              <>
                <Loader2 className="h-4 w-4 animate-spin mr-2" />
                Saving...
              </>
            ) : (
              "Save Changes"
            )}
          </Button>
        </div>
      </div>
    </ResponsiveDialog>
  );
}

// Helper function to compare two sets
function arePermissionSetsEqual(set1: Set<string>, set2: Set<string>): boolean {
  if (set1.size !== set2.size) return false;
  for (const item of set1) {
    if (!set2.has(item)) return false;
  }
  return true;
}
