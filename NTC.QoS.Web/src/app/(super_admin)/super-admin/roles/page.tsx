"use client";

import { useEffect, useMemo, useState } from "react";
import {
  Check,
  Copy,
  Edit,
  Globe,
  Key,
  Layers,
  MapPin,
  Plus,
  Search,
  Shield,
  ShieldAlert,
  ShieldCheck,
  Trash2,
} from "lucide-react";
import { toast } from "sonner";

import { useGetRegions } from "@/components/features/region/hooks/useGetRegions";
import { createRoleRequest } from "@/components/features/role/handlers/createRoleRequest";
import { deleteRoleRequest } from "@/components/features/role/handlers/deleteRoleRequest";
import { editRoleRequest } from "@/components/features/role/handlers/editRoleRequest";
import { getAllRolesRequest } from "@/components/features/role/handlers/getAllRolesRequest";
import type { Role } from "@/components/features/role/types/Role";
import { ManageRolePermissionsDialog } from "@/components/features/role-permission/dialogs/ManageRolePermissionsDialog";
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
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Tabs, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { createMutationHookFromTemplate } from "@/hooks/createMutationHookFromTemplate";
import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";
import { useUrlPagination } from "@/hooks/useUrlPagination";

const useGetAllRoles = createQueryHookFromTemplate(getAllRolesRequest);
const useDeleteRole = createMutationHookFromTemplate(deleteRoleRequest);
const useCreateRole = createMutationHookFromTemplate(createRoleRequest);
const useEditRole = createMutationHookFromTemplate(editRoleRequest);

interface RoleFormState {
  name: string;
  key: string;
  description: string;
  scope: string;
  regionId?: string;
}

const initialFormValues: RoleFormState = {
  name: "",
  key: "",
  description: "",
  scope: "Platform",
  regionId: undefined,
};

export default function RolesManagementPage() {
  const [createDialogOpen, setCreateDialogOpen] = useState(false);
  const [editDialogOpen, setEditDialogOpen] = useState(false);
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
  const [permissionsDialogOpen, setPermissionsDialogOpen] = useState(false);
  const [selectedRole, setSelectedRole] = useState<Role | null>(null);
  const [copiedKey, setCopiedKey] = useState<string | null>(null);
  const [filterTab, setFilterTab] = useState<string>("all");
  const [searchQuery, setSearchQuery] = useState("");
  const [formValues, setFormValues] = useState<RoleFormState>(initialFormValues);

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
  } = useGetAllRoles({
    pathParams: {},
    queryParams: {
      pageNumber: page,
      pageSize: pageSize,
      search: search || undefined,
      scope: filterTab === "Platform" || filterTab === "Regional" ? filterTab : undefined,
    },
  });

  const { data: regionsData } = useGetRegions({
    pathParams: {},
    queryParams: { pageNumber: 1, pageSize: 100 },
  });

  const { mutateAsync: deleteRole, isPending: isDeleting } = useDeleteRole();
  const { mutateAsync: createRole, isPending: isCreating } = useCreateRole();
  const { mutateAsync: editRole, isPending: isEditing } = useEditRole();

  const allRoles = useMemo(() => rolesData?.data ?? [], [rolesData?.data]);
  const regions = useMemo(() => regionsData?.data ?? [], [regionsData?.data]);
  const totalCount = rolesData?.totalRows || 0;
  const totalPages = Math.ceil(totalCount / pageSize);

  // Region map for quick name lookup
  const regionMap = useMemo(() => {
    const map = new Map<string, string>();
    regions.forEach((r) => map.set(r.id, r.name));
    return map;
  }, [regions]);

  // Client-side filtering for system/custom tabs
  const displayedRoles = useMemo(() => {
    if (filterTab === "system") {
      return allRoles.filter((r) => r.isSystemRole);
    }
    if (filterTab === "custom") {
      return allRoles.filter((r) => !r.isSystemRole);
    }
    return allRoles;
  }, [allRoles, filterTab]);

  // KPI Calculations
  const stats = useMemo(() => {
    const platformCount = allRoles.filter((r) => r.scope === "Platform").length;
    const regionalCount = allRoles.filter((r) => r.scope === "Regional").length;
    const systemCount = allRoles.filter((r) => r.isSystemRole).length;
    const customCount = allRoles.filter((r) => !r.isSystemRole).length;
    return { platformCount, regionalCount, systemCount, customCount };
  }, [allRoles]);

  const copyToClipboard = (key: string) => {
    navigator.clipboard.writeText(key);
    setCopiedKey(key);
    toast.success(`Copied "${key}" to clipboard`);
    setTimeout(() => setCopiedKey(null), 2000);
  };

  const handleCreate = async (e: React.FormEvent) => {
    e.preventDefault();
    toast.promise(
      createRole({
        pathParams: {},
        payload: {
          name: formValues.name,
          key: formValues.key.toLowerCase().trim().replace(/\s+/g, "_"),
          description: formValues.description,
          scope: formValues.scope,
          regionId: formValues.scope === "Regional" && formValues.regionId ? formValues.regionId : undefined,
        },
      }),
      {
        loading: "Creating role...",
        success: () => {
          setCreateDialogOpen(false);
          setFormValues(initialFormValues);
          void refetch();
          return "Role created successfully";
        },
        error: (err: Error) => err?.message || "Failed to create role",
      }
    );
  };

  const handleEdit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedRole) return;
    toast.promise(
      editRole({
        pathParams: { id: selectedRole.id },
        payload: {
          name: formValues.name,
          key: selectedRole.isSystemRole ? undefined : formValues.key,
          description: formValues.description,
          scope: selectedRole.isSystemRole ? undefined : formValues.scope,
          regionId: selectedRole.isSystemRole || formValues.scope !== "Regional" ? undefined : formValues.regionId,
        },
      }),
      {
        loading: "Updating role...",
        success: () => {
          setEditDialogOpen(false);
          setSelectedRole(null);
          setFormValues(initialFormValues);
          void refetch();
          return "Role updated successfully";
        },
        error: (err: Error) => err?.message || "Failed to update role",
      }
    );
  };

  const handleDelete = async () => {
    if (!selectedRole) return;
    if (selectedRole.isSystemRole) {
      toast.error("System roles cannot be deleted.");
      return;
    }
    toast.promise(deleteRole({ pathParams: { id: selectedRole.id } }), {
      loading: "Deleting role...",
      success: () => {
        setDeleteDialogOpen(false);
        setSelectedRole(null);
        void refetch();
        return "Role deleted successfully";
      },
      error: (err: Error) => err?.message || "Failed to delete role",
    });
  };

  const openManagePermissions = (role: Role) => {
    setSelectedRole(role);
    setPermissionsDialogOpen(true);
  };

  const openEditDialog = (role: Role) => {
    setSelectedRole(role);
    setFormValues({
      name: role.name,
      key: role.key,
      description: role.description || "",
      scope: role.scope || "Platform",
      regionId: role.regionId || undefined,
    });
    setEditDialogOpen(true);
  };

  const openDeleteDialog = (role: Role) => {
    setSelectedRole(role);
    setDeleteDialogOpen(true);
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-3xl font-bold flex items-center gap-2.5">
            <Shield className="h-8 w-8 text-primary" />
            Roles & Jurisdictions
          </h1>
          <p className="text-muted-foreground mt-1">
            Configure system-wide Platform roles and jurisdiction-scoped Regional custom roles.
          </p>
        </div>
        <Button
          onClick={() => {
            setFormValues(initialFormValues);
            setCreateDialogOpen(true);
          }}
          className="gap-2 shadow-sm">
          <Plus className="h-4 w-4" />
          Create New Role
        </Button>
      </div>

      {/* KPI Stats Cards */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        <Card className="shadow-xs">
          <CardHeader className="p-4 pb-2">
            <CardDescription className="text-xs">Total Roles</CardDescription>
            <CardTitle className="text-2xl font-bold flex items-center justify-between">
              <span>{totalCount}</span>
              <Layers className="h-5 w-5 text-muted-foreground/60" />
            </CardTitle>
          </CardHeader>
          <CardContent className="p-4 pt-0 text-xs text-muted-foreground">Active in system</CardContent>
        </Card>

        <Card className="shadow-xs">
          <CardHeader className="p-4 pb-2">
            <CardDescription className="text-xs">Platform Roles</CardDescription>
            <CardTitle className="text-2xl font-bold flex items-center justify-between text-blue-600 dark:text-blue-400">
              <span>{stats.platformCount}</span>
              <Globe className="h-5 w-5 text-blue-500/60" />
            </CardTitle>
          </CardHeader>
          <CardContent className="p-4 pt-0 text-xs text-muted-foreground">National jurisdiction</CardContent>
        </Card>

        <Card className="shadow-xs">
          <CardHeader className="p-4 pb-2">
            <CardDescription className="text-xs">Regional Roles</CardDescription>
            <CardTitle className="text-2xl font-bold flex items-center justify-between text-indigo-600 dark:text-indigo-400">
              <span>{stats.regionalCount}</span>
              <MapPin className="h-5 w-5 text-indigo-500/60" />
            </CardTitle>
          </CardHeader>
          <CardContent className="p-4 pt-0 text-xs text-muted-foreground">Region-scoped access</CardContent>
        </Card>

        <Card className="shadow-xs">
          <CardHeader className="p-4 pb-2">
            <CardDescription className="text-xs">System Built-in</CardDescription>
            <CardTitle className="text-2xl font-bold flex items-center justify-between text-amber-600 dark:text-amber-400">
              <span>{stats.systemCount}</span>
              <ShieldCheck className="h-5 w-5 text-amber-500/60" />
            </CardTitle>
          </CardHeader>
          <CardContent className="p-4 pt-0 text-xs text-muted-foreground">Protected roles</CardContent>
        </Card>
      </div>

      {/* Main Table Card */}
      <Card className="shadow-xs">
        <CardHeader className="pb-3">
          <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
            {/* Filter Tabs */}
            <Tabs value={filterTab} onValueChange={setFilterTab} className="w-full md:w-auto">
              <TabsList className="bg-muted/70">
                <TabsTrigger value="all" className="text-xs px-3">
                  All Roles ({totalCount})
                </TabsTrigger>
                <TabsTrigger value="Platform" className="text-xs px-3">
                  Platform
                </TabsTrigger>
                <TabsTrigger value="Regional" className="text-xs px-3">
                  Regional
                </TabsTrigger>
                <TabsTrigger value="system" className="text-xs px-3">
                  System
                </TabsTrigger>
                <TabsTrigger value="custom" className="text-xs px-3">
                  Custom
                </TabsTrigger>
              </TabsList>
            </Tabs>

            {/* Search Box */}
            <div className="relative w-full md:w-72">
              <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
              <Input
                placeholder="Search by role name or key..."
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                className="pl-9 h-9 text-sm"
              />
            </div>
          </div>
        </CardHeader>

        <CardContent>
          {isLoading ? (
            <div className="text-center py-16 text-muted-foreground">Loading roles directory...</div>
          ) : displayedRoles.length === 0 ? (
            <div className="text-center py-16 text-muted-foreground border rounded-lg border-dashed">
              <Shield className="h-10 w-10 mx-auto text-muted-foreground/40 mb-2" />
              <p className="font-semibold text-foreground">No roles found</p>
              <p className="text-xs">Try adjusting your filters or search keywords.</p>
            </div>
          ) : (
            <div className="rounded-lg border overflow-hidden">
              <Table>
                <TableHeader className="bg-muted/50">
                  <TableRow>
                    <TableHead className="w-[260px]">Role Identity</TableHead>
                    <TableHead className="w-[140px]">Scope</TableHead>
                    <TableHead className="w-[110px]">Type</TableHead>
                    <TableHead className="w-[180px]">Assigned Permissions</TableHead>
                    <TableHead>Description</TableHead>
                    <TableHead className="text-right w-[160px]">Actions</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {displayedRoles.map((role) => {
                    const permissionCount = role.permissions?.length ?? 0;
                    const isSystem = role.isSystemRole;
                    const regionName = role.regionId ? regionMap.get(role.regionId) : null;

                    return (
                      <TableRow key={role.id} className="hover:bg-muted/40 transition-colors">
                        {/* Role Name & Key */}
                        <TableCell>
                          <div className="space-y-1">
                            <span className="font-semibold text-foreground flex items-center gap-1.5">
                              {role.name}
                            </span>
                            <div className="flex items-center gap-1">
                              <code className="text-xs bg-muted px-1.5 py-0.5 rounded font-mono text-muted-foreground">
                                {role.key}
                              </code>
                              <button
                                type="button"
                                onClick={() => copyToClipboard(role.key)}
                                className="text-muted-foreground hover:text-foreground p-0.5 rounded transition-colors"
                                title="Copy key">
                                {copiedKey === role.key ? (
                                  <Check className="h-3 w-3 text-emerald-500" />
                                ) : (
                                  <Copy className="h-3 w-3" />
                                )}
                              </button>
                            </div>
                          </div>
                        </TableCell>

                        {/* Scope */}
                        <TableCell>
                          <div className="space-y-1">
                            <Badge
                              variant={role.scope === "Platform" ? "default" : "secondary"}
                              className="gap-1 text-xs">
                              {role.scope === "Platform" ? (
                                <Globe className="h-3 w-3" />
                              ) : (
                                <MapPin className="h-3 w-3" />
                              )}
                              {role.scope || "Platform"}
                            </Badge>
                            {regionName && (
                              <p className="text-[11px] text-muted-foreground truncate max-w-[130px]" title={regionName}>
                                {regionName}
                              </p>
                            )}
                          </div>
                        </TableCell>

                        {/* Type */}
                        <TableCell>
                          {isSystem ? (
                            <Badge
                              variant="outline"
                              className="border-amber-500/30 text-amber-600 dark:text-amber-400 bg-amber-500/10 text-xs">
                              System
                            </Badge>
                          ) : (
                            <Badge variant="outline" className="text-muted-foreground text-xs">
                              Custom
                            </Badge>
                          )}
                        </TableCell>

                        {/* Permissions Badge & Quick Manage */}
                        <TableCell>
                          <Button
                            variant="ghost"
                            size="sm"
                            onClick={() => openManagePermissions(role)}
                            className="h-8 px-2 text-xs gap-1.5 text-primary hover:bg-primary/10">
                            <ShieldCheck className="h-3.5 w-3.5" />
                            <span>{permissionCount} Permissions</span>
                          </Button>
                        </TableCell>

                        {/* Description */}
                        <TableCell className="text-muted-foreground text-xs max-w-xs truncate">
                          {role.description || "-"}
                        </TableCell>

                        {/* Actions */}
                        <TableCell className="text-right">
                          <div className="flex justify-end items-center gap-1">
                            <Button
                              variant="ghost"
                              size="sm"
                              onClick={() => openManagePermissions(role)}
                              className="h-8 px-2 text-xs font-medium gap-1"
                              title="Manage role permissions">
                              <Key className="h-3.5 w-3.5 text-primary" />
                              <span className="hidden sm:inline">Permissions</span>
                            </Button>

                            <Button
                              variant="ghost"
                              size="sm"
                              onClick={() => openEditDialog(role)}
                              className="h-8 w-8 p-0"
                              title="Edit role">
                              <Edit className="h-3.5 w-3.5" />
                            </Button>

                            <Button
                              variant="ghost"
                              size="sm"
                              onClick={() => openDeleteDialog(role)}
                              disabled={isSystem}
                              className="h-8 w-8 p-0 text-red-600 hover:text-red-700 hover:bg-red-500/10 disabled:opacity-30"
                              title={isSystem ? "System roles cannot be deleted" : "Delete role"}>
                              <Trash2 className="h-3.5 w-3.5" />
                            </Button>
                          </div>
                        </TableCell>
                      </TableRow>
                    );
                  })}
                </TableBody>
              </Table>
            </div>
          )}

          {/* Pagination */}
          {!isLoading && !search && totalCount > 0 && (
            <div className="pt-4">
              <Pagination
                currentPage={page}
                totalPages={totalPages}
                pageSize={pageSize}
                totalCount={totalCount}
                onPageChange={setPage}
                onPageSizeChange={setPageSize}
              />
            </div>
          )}
        </CardContent>
      </Card>

      {/* Permissions Management Dialog */}
      <ManageRolePermissionsDialog
        role={selectedRole}
        open={permissionsDialogOpen}
        onOpenChange={setPermissionsDialogOpen}
        onSuccess={() => void refetch()}
      />

      {/* Create Dialog */}
      <ResponsiveDialog
        open={createDialogOpen}
        onOpenChange={setCreateDialogOpen}
        title="Create New Role"
        description="Configure a new Platform or Regional custom role.">
        <form onSubmit={handleCreate} className="space-y-4">
          <div>
            <Label htmlFor="create-name">Role Name</Label>
            <Input
              id="create-name"
              value={formValues.name}
              onChange={(e) => setFormValues({ ...formValues, name: e.target.value })}
              placeholder="e.g., Regional Network Inspector"
              required
            />
          </div>
          <div>
            <Label htmlFor="create-key">Role Key (Unique Identifier)</Label>
            <Input
              id="create-key"
              value={formValues.key}
              onChange={(e) => setFormValues({ ...formValues, key: e.target.value })}
              placeholder="e.g., regional_network_inspector"
              required
            />
            <p className="text-[11px] text-muted-foreground mt-1">Lowercase alphanumeric identifier used in RBAC logic.</p>
          </div>
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div>
              <Label htmlFor="create-scope">Jurisdiction Scope</Label>
              <Select
                value={formValues.scope}
                onValueChange={(value) => setFormValues({ ...formValues, scope: value, regionId: undefined })}>
                <SelectTrigger id="create-scope">
                  <SelectValue placeholder="Select scope" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="Platform">Platform (National)</SelectItem>
                  <SelectItem value="Regional">Regional Scope</SelectItem>
                </SelectContent>
              </Select>
            </div>
            {formValues.scope === "Regional" && (
              <div>
                <Label htmlFor="create-region">Region Jurisdiction</Label>
                <Select
                  value={formValues.regionId || "none"}
                  onValueChange={(val) => setFormValues({ ...formValues, regionId: val === "none" ? undefined : val })}>
                  <SelectTrigger id="create-region">
                    <SelectValue placeholder="Dynamic / User Region" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="none">Dynamic (User Assigned Regions)</SelectItem>
                    {regions.map((reg) => (
                      <SelectItem key={reg.id} value={reg.id}>
                        {reg.name} ({reg.code})
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
            )}
          </div>
          <div>
            <Label htmlFor="create-description">Description</Label>
            <textarea
              id="create-description"
              className="w-full rounded-md border border-input p-2.5 text-sm bg-background"
              value={formValues.description}
              onChange={(e) => setFormValues({ ...formValues, description: e.target.value })}
              rows={3}
              placeholder="Outline the responsibilities and permissions this role will encompass..."
            />
          </div>
          <div className="flex justify-end gap-2 pt-2">
            <Button type="button" variant="outline" onClick={() => setCreateDialogOpen(false)}>
              Cancel
            </Button>
            <Button type="submit" disabled={isCreating}>
              {isCreating ? "Creating..." : "Create Role"}
            </Button>
          </div>
        </form>
      </ResponsiveDialog>

      {/* Edit Dialog */}
      <ResponsiveDialog
        open={editDialogOpen}
        onOpenChange={setEditDialogOpen}
        title="Edit Role"
        description="Update role identity and configuration.">
        <form onSubmit={handleEdit} className="space-y-4">
          {selectedRole?.isSystemRole && (
            <div className="flex items-start gap-2 p-3 bg-amber-500/10 border border-amber-500/20 rounded-md text-xs text-amber-800 dark:text-amber-300">
              <ShieldAlert className="h-4 w-4 shrink-0 text-amber-600 dark:text-amber-400 mt-0.5" />
              <span>
                <strong>System Role Protection:</strong> Role key and jurisdiction scope are locked for system-defined roles. You can edit display names and descriptions.
              </span>
            </div>
          )}
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
              disabled={selectedRole?.isSystemRole}
              required
            />
          </div>
          {!selectedRole?.isSystemRole && (
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <div>
                <Label htmlFor="edit-scope">Jurisdiction Scope</Label>
                <Select
                  value={formValues.scope}
                  onValueChange={(value) => setFormValues({ ...formValues, scope: value, regionId: undefined })}>
                  <SelectTrigger id="edit-scope">
                    <SelectValue placeholder="Select scope" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="Platform">Platform (National)</SelectItem>
                    <SelectItem value="Regional">Regional Scope</SelectItem>
                  </SelectContent>
                </Select>
              </div>
              {formValues.scope === "Regional" && (
                <div>
                  <Label htmlFor="edit-region">Region</Label>
                  <Select
                    value={formValues.regionId || "none"}
                    onValueChange={(val) => setFormValues({ ...formValues, regionId: val === "none" ? undefined : val })}>
                    <SelectTrigger id="edit-region">
                      <SelectValue placeholder="Dynamic / User Region" />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="none">Dynamic (User Assigned Regions)</SelectItem>
                      {regions.map((reg) => (
                        <SelectItem key={reg.id} value={reg.id}>
                          {reg.name} ({reg.code})
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>
              )}
            </div>
          )}
          <div>
            <Label htmlFor="edit-description">Description</Label>
            <textarea
              id="edit-description"
              className="w-full rounded-md border border-input p-2.5 text-sm bg-background"
              value={formValues.description}
              onChange={(e) => setFormValues({ ...formValues, description: e.target.value })}
              rows={3}
            />
          </div>
          <div className="flex justify-end gap-2 pt-2">
            <Button type="button" variant="outline" onClick={() => setEditDialogOpen(false)}>
              Cancel
            </Button>
            <Button type="submit" disabled={isEditing}>
              {isEditing ? "Updating..." : "Update Role"}
            </Button>
          </div>
        </form>
      </ResponsiveDialog>

      {/* Delete Confirmation Dialog */}
      <AlertDialog open={deleteDialogOpen} onOpenChange={setDeleteDialogOpen}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Delete Role</AlertDialogTitle>
            <AlertDialogDescription>
              Are you sure you want to delete the role &quot;{selectedRole?.name}&quot;? Users assigned to this role will lose their assigned permissions.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancel</AlertDialogCancel>
            <AlertDialogAction
              onClick={handleDelete}
              disabled={isDeleting}
              className="bg-red-600 hover:bg-red-700 text-white">
              {isDeleting ? "Deleting..." : "Delete Role"}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}
