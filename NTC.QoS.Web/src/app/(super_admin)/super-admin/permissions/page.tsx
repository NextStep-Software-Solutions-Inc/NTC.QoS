"use client";

import { useEffect, useState } from "react";
import { Edit, Key, Plus, Trash2 } from "lucide-react";
import { toast } from "sonner";

import { createPermissionRequest } from "@/components/features/permission/handlers/createPermissionRequest";
import { deletePermissionRequest } from "@/components/features/permission/handlers/deletePermissionRequest";
import { editPermissionRequest } from "@/components/features/permission/handlers/editPermissionRequest";
import { getPermissionsRequest } from "@/components/features/permission/handlers/getPermissionsRequest";
import type { Permission } from "@/components/features/permission/types/Permission";
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

const useGetPermissions = createQueryHookFromTemplate(getPermissionsRequest);
const useDeletePermission = createMutationHookFromTemplate(deletePermissionRequest);
const useCreatePermission = createMutationHookFromTemplate(createPermissionRequest);
const useEditPermission = createMutationHookFromTemplate(editPermissionRequest);

export default function PermissionsManagementPage() {
  const [createDialogOpen, setCreateDialogOpen] = useState(false);
  const [editDialogOpen, setEditDialogOpen] = useState(false);
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
  const [selectedPermission, setSelectedPermission] = useState<Permission | null>(null);
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
    data: permissionsData,
    isLoading,
    refetch,
  } = useGetPermissions({ pathParams: {}, queryParams: { pageNumber: page, pageSize: pageSize, search: search || undefined } });

  const { mutateAsync: deletePermission, isPending: isDeleting } = useDeletePermission();
  const { mutateAsync: createPermission, isPending: isCreating } = useCreatePermission();
  const { mutateAsync: editPermission, isPending: isEditing } = useEditPermission();

  const permissions = permissionsData?.data ?? [];
  const totalCount = permissionsData?.totalRows || 0;
  const totalPages = Math.ceil(totalCount / pageSize);

  const handleCreate = async (e: React.FormEvent) => {
    e.preventDefault();
    toast.promise(createPermission({ pathParams: {}, payload: formValues }), {
      loading: "Creating permission...",
      success: () => {
        setCreateDialogOpen(false);
        setFormValues({ name: "", key: "", description: "" });
        void refetch();
        return "Permission created successfully";
      },
      error: "Failed to create permission",
    });
  };

  const handleEdit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedPermission) return;
    toast.promise(editPermission({ pathParams: { id: selectedPermission.id }, payload: formValues }), {
      loading: "Updating permission...",
      success: () => {
        setEditDialogOpen(false);
        setSelectedPermission(null);
        setFormValues({ name: "", key: "", description: "" });
        void refetch();
        return "Permission updated successfully";
      },
      error: "Failed to update permission",
    });
  };

  const handleDelete = async () => {
    if (!selectedPermission) return;
    toast.promise(deletePermission({ pathParams: { id: selectedPermission.id } }), {
      loading: "Deleting permission...",
      success: () => {
        setDeleteDialogOpen(false);
        setSelectedPermission(null);
        void refetch();
        return "Permission deleted successfully";
      },
      error: "Failed to delete permission",
    });
  };

  const openEditDialog = (permission: Permission) => {
    setSelectedPermission(permission);
    setFormValues({ name: permission.name, key: permission.key, description: permission.description || "" });
    setEditDialogOpen(true);
  };

  const openDeleteDialog = (permission: Permission) => {
    setSelectedPermission(permission);
    setDeleteDialogOpen(true);
  };

  return (
    <div className="space-y-6">
      <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-3xl font-bold">Permissions Management</h1>
          <p className="text-muted-foreground mt-1">Configure system permissions</p>
        </div>
        <Button onClick={() => setCreateDialogOpen(true)} className="gap-2">
          <Plus className="h-4 w-4" />
          Create Permission
        </Button>
      </div>

      <Card>
        <CardHeader>
          <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4">
            <div>
              <CardTitle className="flex items-center gap-2">
                <Key className="h-5 w-5" />
                Permissions ({totalCount})
              </CardTitle>
              <CardDescription>System permissions and access controls</CardDescription>
            </div>
            <div className="w-full sm:w-64">
              <Input
                placeholder="Search permissions..."
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
              />
            </div>
          </div>
        </CardHeader>
        <CardContent>
          {isLoading ? (
            <div className="text-center py-8">Loading permissions...</div>
          ) : (
            <div className="overflow-x-auto">
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Permission Name</TableHead>
                    <TableHead>Key</TableHead>
                    <TableHead>Description</TableHead>
                    <TableHead className="text-right">Actions</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {permissions.length === 0 ? (
                    <TableRow>
                      <TableCell colSpan={4} className="text-center py-8 text-muted-foreground">
                        No permissions found
                      </TableCell>
                    </TableRow>
                  ) : (
                    permissions.map((permission) => (
                      <TableRow key={permission.id}>
                        <TableCell className="font-medium">{permission.name}</TableCell>
                        <TableCell>
                          <Badge variant="secondary">{permission.key}</Badge>
                        </TableCell>
                        <TableCell className="text-muted-foreground">{permission.description || "-"}</TableCell>
                        <TableCell className="text-right">
                          <div className="flex justify-end gap-2">
                            <Button variant="ghost" size="sm" onClick={() => openEditDialog(permission)}>
                              <Edit className="h-4 w-4" />
                            </Button>
                            <Button variant="ghost" size="sm" onClick={() => openDeleteDialog(permission)}>
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
        title="Create New Permission"
        description="Add a new permission to the system">
        <form onSubmit={handleCreate} className="space-y-4">
          <div>
            <Label htmlFor="create-name">Permission Name</Label>
            <Input
              id="create-name"
              value={formValues.name}
              onChange={(e) => setFormValues({ ...formValues, name: e.target.value })}
              required
            />
          </div>
          <div>
            <Label htmlFor="create-key">Permission Key</Label>
            <Input
              id="create-key"
              value={formValues.key}
              onChange={(e) => setFormValues({ ...formValues, key: e.target.value })}
              placeholder="e.g., create_user, delete_report"
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
        title="Edit Permission"
        description="Update permission information">
        <form onSubmit={handleEdit} className="space-y-4">
          <div>
            <Label htmlFor="edit-name">Permission Name</Label>
            <Input
              id="edit-name"
              value={formValues.name}
              onChange={(e) => setFormValues({ ...formValues, name: e.target.value })}
              required
            />
          </div>
          <div>
            <Label htmlFor="edit-key">Permission Key</Label>
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
            <AlertDialogTitle>Delete Permission</AlertDialogTitle>
            <AlertDialogDescription>
              Are you sure you want to delete the permission &quot;{selectedPermission?.name}&quot;? This action cannot
              be undone.
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
