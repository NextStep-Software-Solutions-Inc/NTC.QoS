"use client";

import { useEffect, useState } from "react";
import { Search, Settings2, Shield } from "lucide-react";

import { ManageRolePermissionsDialog } from "@/components/features/role-permission/dialogs/ManageRolePermissionsDialog";
import { useGetRolePermissions } from "@/components/features/role-permission/hooks/useGetRolePermissions";
import { useGetRoles } from "@/components/features/role/hooks/useGetRoles";
import type { Role } from "@/components/features/role/types/Role";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Pagination } from "@/components/ui/pagination";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { useUrlPagination } from "@/hooks/useUrlPagination";

export default function RolePermissionsManagementPage() {
  const [manageDialogOpen, setManageDialogOpen] = useState(false);
  const [selectedRole, setSelectedRole] = useState<Role | null>(null);
  const [searchQuery, setSearchQuery] = useState("");

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
    isLoading: isLoadingRoles,
    refetch: refetchRoles,
  } = useGetRoles({ pathParams: {}, queryParams: { pageNumber: page, pageSize: pageSize, search: search || undefined } });

  // Get all role permissions to count permissions per role
  const { data: allRolePermissionsData, refetch: refetchAllRolePermissions } = useGetRolePermissions({
    pathParams: {},
    queryParams: { pageNumber: 1, pageSize: 10000 },
  });

  const roles = rolesData?.data ?? [];
  const totalCount = rolesData?.totalRows || 0;
  const allRolePermissions = allRolePermissionsData?.data ?? [];

  // Create a map of role ID to permission count
  const rolePermissionCounts = new Map<string, number>();
  allRolePermissions.forEach((rp) => {
    const count = rolePermissionCounts.get(rp.role.id) || 0;
    rolePermissionCounts.set(rp.role.id, count + 1);
  });

  const handleManagePermissions = (role: Role) => {
    setSelectedRole(role);
    setManageDialogOpen(true);
  };

  const handleDialogSuccess = () => {
    void refetchRoles();
    void refetchAllRolePermissions();
  };

  return (
    <div className="container mx-auto p-6 space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold flex items-center gap-2">
            <Shield className="h-8 w-8" />
            Role Permissions Management
          </h1>
          <p className="text-muted-foreground mt-1">Manage permissions for each role</p>
        </div>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Roles</CardTitle>
          <CardDescription>Click "Manage Permissions" to configure permissions for each role</CardDescription>
        </CardHeader>
        <CardContent>
          <div className="mb-4">
            <div className="relative">
              <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-muted-foreground" />
              <Input
                placeholder="Search roles..."
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                className="pl-10"
              />
            </div>
          </div>

          {isLoadingRoles ? (
            <div className="text-center py-8 text-muted-foreground">Loading...</div>
          ) : roles.length === 0 ? (
            <div className="text-center py-8 text-muted-foreground">No roles found.</div>
          ) : (
            <div className="border rounded-lg overflow-hidden">
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Role Name</TableHead>
                    <TableHead>Role Key</TableHead>
                    <TableHead>Description</TableHead>
                    <TableHead className="text-center">Permissions</TableHead>
                    <TableHead className="text-right">Actions</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {roles.map((role) => {
                    const permissionCount = rolePermissionCounts.get(role.id) || 0;
                    return (
                      <TableRow key={role.id}>
                        <TableCell>
                          <div className="flex items-center gap-2">
                            <Badge variant="outline">{role.name}</Badge>
                          </div>
                        </TableCell>
                        <TableCell>
                          <code className="text-xs bg-muted px-2 py-1 rounded">{role.key}</code>
                        </TableCell>
                        <TableCell className="text-sm text-muted-foreground max-w-md">
                          {role.description || "—"}
                        </TableCell>
                        <TableCell className="text-center">
                          <Badge variant="secondary">
                            {permissionCount} {permissionCount === 1 ? "permission" : "permissions"}
                          </Badge>
                        </TableCell>
                        <TableCell className="text-right">
                          <Button
                            variant="outline"
                            size="sm"
                            onClick={() => handleManagePermissions(role)}
                            className="gap-2">
                            <Settings2 className="h-4 w-4" />
                            Manage Permissions
                          </Button>
                        </TableCell>
                      </TableRow>
                    );
                  })}
                </TableBody>
              </Table>
            </div>
          )}

          {!isLoadingRoles && !search && totalCount > 0 && (
            <Pagination
              currentPage={page}
              totalPages={Math.ceil(totalCount / pageSize)}
              pageSize={pageSize}
              totalCount={totalCount}
              onPageChange={setPage}
              onPageSizeChange={setPageSize}
            />
          )}
        </CardContent>
      </Card>

      {/* Manage Permissions Dialog */}
      <ManageRolePermissionsDialog
        role={selectedRole}
        open={manageDialogOpen}
        onOpenChange={setManageDialogOpen}
        onSuccess={handleDialogSuccess}
      />
    </div>
  );
}
