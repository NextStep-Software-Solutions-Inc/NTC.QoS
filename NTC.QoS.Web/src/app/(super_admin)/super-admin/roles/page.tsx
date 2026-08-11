"use client";

import { useEffect, useState } from "react";
import { Edit, Plus, Shield, Trash2 } from "lucide-react";
import { toast } from "sonner";

import { createRoleRequest } from "@/components/features/role/handlers/createRoleRequest";
import { deleteRoleRequest } from "@/components/features/role/handlers/deleteRoleRequest";
import { editRoleRequest } from "@/components/features/role/handlers/editRoleRequest";
import { getAllRolesRequest } from "@/components/features/role/handlers/getAllRolesRequest";
import type { Role } from "@/components/features/role/types/Role";
import { ResponsiveDialog } from "@/components/ResponsiveDialog";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Pagination } from "@/components/ui/pagination";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { createMutationHookFromTemplate } from "@/hooks/createMutationHookFromTemplate";
import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";
import { useUrlPagination } from "@/hooks/useUrlPagination";

const useGetAllRoles = createQueryHookFromTemplate(getAllRolesRequest);
const useDeleteRole = createMutationHookFromTemplate(deleteRoleRequest);
const useCreateRole = createMutationHookFromTemplate(createRoleRequest);
const useEditRole = createMutationHookFromTemplate(editRoleRequest);

export default function RolesManagementPage() {
  const [createDialogOpen, setCreateDialogOpen] = useState(false);
  const [editDialogOpen, setEditDialogOpen] = useState(false);
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
  const [selectedRole, setSelectedRole] = useState<Role | null>(null);
  const [searchQuery, setSearchQuery] = useState("");
  const [formValues, setFormValues] = useState({ name: "", key: "", description: "" });

  const { page, pageSize, search, setPage, setPageSize, setSearch } = useUrlPagination();

  // Debounced search
  useEffect(() => {
    const timer = setTimeout(() => {
      setSearch(searchQuery);
    }, 300);
    return () => clearTimeout(timer);
  }, [searchQuery, setSearch]);

  const {
    data: rolesData,
    isLoading,
    refetch,
  } = useGetAllRoles({ pathParams: {}, queryParams: { pageNumber: page, pageSize: pageSize, search: search || undefined } });

  const { mutateAsync: deleteRole, isPending: isDeleting } = useDeleteRole();
  const { mutateAsync: createRole, isPending: isCreating } = useCreateRole();
  const { mutateAsync: editRole, isPending: isEditing } = useEditRole();

  const roles = rolesData?.data ?? [];
  const totalCount = rolesData?.totalRows || 0;
  const totalPages = Math.ceil(totalCount / pageSize);

  const handleCreate = async (e: React.FormEvent) => {
    e.preventDefault();
    toast.promise(createRole({ pathParams: {}, payload: formValues }), {
      loading: "Creating role...",
      success: () => {
        setCreateDialogOpen(false);
        setFormValues({ name: "", key: "", description: "" });
        void refetch();
        return "Role created successfully";
      },
      error: "Failed to create role",
    });
  };

  const handleEdit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedRole) return;
    toast.promise(editRole({ pathParams: { id: selectedRole.id }, payload: formValues }), {
      loading: "Updating role...",
      success: () => {
        setEditDialogOpen(false);
        setSelectedRole(null);
        setFormValues({ name: "", key: "", description: "" });
        void refetch();
        return "Role updated successfully";
      },
      error: "Failed to update role",
    });
  };

  const handleDelete = async () => {
    if (!selectedRole) return;
    toast.promise(deleteRole({ pathParams: { id: selectedRole.id } }), {
      loading: "Deleting role...",
      success: () => {
        setDeleteDialogOpen(false);
        setSelectedRole(null);
        void refetch();
        return "Role deleted successfully";
      },
      error: "Failed to delete role",
    });
  };

  const openEditDialog = (role: Role) => {
    setSelectedRole(role);
    setFormValues({ name: role.name, key: role.key, description: role.description || "" });
    setEditDialogOpen(true);
  };

  const openDeleteDialog = (role: Role) => {
    setSelectedRole(role);
    setDeleteDialogOpen(true);
  };

  return (
    <div className="space-y-6">
      <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-3xl font-bold">Roles Management</h1>
          <p className="text-muted-foreground mt-1">Define and manage user roles</p>
        </div>
        <Button onClick={() => setCreateDialogOpen(true)} className="gap-2">
          <Plus className="h-4 w-4" />
          Create Role
        </Button>
      </div>

      <Card>
        <CardHeader>
          <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4">
            <div>
              <CardTitle className="flex items-center gap-2">
                <Shield className="h-5 w-5" />
                Roles ({totalCount})
              </CardTitle>
              <CardDescription>System roles and their permissions</CardDescription>
            </div>
            <div className="w-full sm:w-64">
              <Input
                placeholder="Search roles..."
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
              />
            </div>
          </div>
        </CardHeader>
        <CardContent>
          {isLoading ? (
            <div className="text-center py-8">Loading roles...</div>
          ) : (
            <div className="overflow-x-auto">
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Role Name</TableHead>
                    <TableHead>Key</TableHead>
                    <TableHead>Description</TableHead>
                    <TableHead className="text-right">Actions</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {roles.length === 0 ? (
                    <TableRow>
                      <TableCell colSpan={4} className="text-center py-8 text-muted-foreground">
                        No roles found
                      </TableCell>
                    </TableRow>
                  ) : (
                    roles.map((role) => (
                      <TableRow key={role.id}>
                        <TableCell className="font-medium">{role.name}</TableCell>
                        <TableCell>
                          <Badge variant="secondary">{role.key}</Badge>
                        </TableCell>
                        <TableCell className="text-muted-foreground">{role.description || "-"}</TableCell>
                        <TableCell className="text-right">
                          <div className="flex justify-end gap-2">
                            <Button variant="ghost" size="sm" onClick={() => openEditDialog(role)}>
                              <Edit className="h-4 w-4" />
                            </Button>
                            <Button variant="ghost" size="sm" onClick={() => openDeleteDialog(role)}>
                              <Trash2 className="h-4 w-4" />
                            </Button>
                          </div>
                        </TableCell>
                      </TableRow>
                    ))
                  )}
                </TableBody>
              </Table>
            </div>
          )}

          {!isLoading && !search && totalCount > 0 && (
            <Pagination
              currentPage={page}
              totalPages={totalPages}
              pageSize={pageSize}
              totalCount={totalCount}
              onPageChange={setPage}
              onPageSizeChange={setPageSize}
            />
          )}
        </CardContent>
      </Card>

      {/* Create Dialog */}
      <ResponsiveDialog
        open={createDialogOpen}
        onOpenChange={setCreateDialogOpen}
        title="Create New Role"
        description="Add a new role to the system">
        <form onSubmit={handleCreate} className="space-y-4">
          <div>
            <Label htmlFor="create-name">Role Name</Label>
            <Input
              id="create-name"
              value={formValues.name}
              onChange={(e) => setFormValues({ ...formValues, name: e.target.value })}
              required
            />
          </div>
          <div>
            <Label htmlFor="create-key">Role Key</Label>
            <Input
              id="create-key"
              value={formValues.key}
              onChange={(e) => setFormValues({ ...formValues, key: e.target.value })}
              placeholder="e.g., admin, user, moderator"
              required
            />
          </div>
          <div>
            <Label htmlFor="create-description">Description</Label>
            <textarea
              id="create-description"
              className="w-full rounded border border-input p-2 text-sm"
              value={formValues.description}
              onChange={(e) => setFormValues({ ...formValues, description: e.target.value })}
              rows={3}
            />
          </div>
          <div className="flex justify-end gap-2">
            <Button type="button" variant="outline" onClick={() => setCreateDialogOpen(false)}>
              Cancel
            </Button>
            <Button type="submit" disabled={isCreating}>
              {isCreating ? "Creating..." : "Create"}
            </Button>
          </div>
        </form>
      </ResponsiveDialog>

      {/* Edit Dialog */}
      <ResponsiveDialog
        open={editDialogOpen}
        onOpenChange={setEditDialogOpen}
        title="Edit Role"
        description="Update role information">
        <form onSubmit={handleEdit} className="space-y-4">
          <div>
            <Label htmlFor="edit-name">Role Name</Label>
            <Input
              id="edit-name"
              value={formValues.name}
              onChange={(e) => setFormValues({ ...formValues, name: e.target.value })}
              required
            />
          </div>
          <div>
            <Label htmlFor="edit-key">Role Key</Label>
            <Input
              id="edit-key"
              value={formValues.key}
              onChange={(e) => setFormValues({ ...formValues, key: e.target.value })}
              required
            />
          </div>
          <div>
            <Label htmlFor="edit-description">Description</Label>
            <textarea
              id="edit-description"
              className="w-full rounded border border-input p-2 text-sm"
              value={formValues.description}
              onChange={(e) => setFormValues({ ...formValues, description: e.target.value })}
              rows={3}
            />
          </div>
          <div className="flex justify-end gap-2">
            <Button type="button" variant="outline" onClick={() => setEditDialogOpen(false)}>
              Cancel
            </Button>
            <Button type="submit" disabled={isEditing}>
              {isEditing ? "Updating..." : "Update"}
            </Button>
          </div>
        </form>
      </ResponsiveDialog>

      {/* Delete Dialog */}
      <AlertDialog open={deleteDialogOpen} onOpenChange={setDeleteDialogOpen}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Delete Role</AlertDialogTitle>
            <AlertDialogDescription>
              Are you sure you want to delete the role &quot;{selectedRole?.name}&quot;? This action cannot be undone.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancel</AlertDialogCancel>
            <AlertDialogAction onClick={handleDelete} disabled={isDeleting}>
              {isDeleting ? "Deleting..." : "Delete"}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}
