"use client";

import { useEffect, useMemo, useState } from "react";
import {
  Activity,
  Globe,
  Key,
  MapPin,
  Search,
  Settings2,
} from "lucide-react";

import { ManageRolePermissionsDialog } from "@/components/features/role-permission/dialogs/ManageRolePermissionsDialog";
import { useGetRoles } from "@/components/features/role/hooks/useGetRoles";
import type { Role } from "@/components/features/role/types/Role";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Pagination } from "@/components/ui/pagination";
import { Progress } from "@/components/ui/progress";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Tabs, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { useUrlPagination } from "@/hooks/useUrlPagination";

export default function RolePermissionsManagementPage() {
  const [manageDialogOpen, setManageDialogOpen] = useState(false);
  const [selectedRole, setSelectedRole] = useState<Role | null>(null);
  const [searchQuery, setSearchQuery] = useState("");
  const [filterTab, setFilterTab] = useState<string>("all");

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
  } = useGetRoles({
    pathParams: {},
    queryParams: {
      pageNumber: page,
      pageSize: pageSize,
      search: search || undefined,
      scope: filterTab === "Platform" || filterTab === "Regional" ? filterTab : undefined,
    },
  });

  const allRoles = useMemo(() => rolesData?.data ?? [], [rolesData?.data]);
  const totalCount = rolesData?.totalRows || 0;
  const totalPages = Math.ceil(totalCount / pageSize);

  const displayedRoles = useMemo(() => {
    if (filterTab === "system") {
      return allRoles.filter((r) => r.isSystemRole);
    }
    if (filterTab === "custom") {
      return allRoles.filter((r) => !r.isSystemRole);
    }
    return allRoles;
  }, [allRoles, filterTab]);

  const stats = useMemo(() => {
    const totalPermissionsAssigned = allRoles.reduce((acc, r) => acc + (r.permissions?.length ?? 0), 0);
    const avgPermissions = allRoles.length > 0 ? Math.round(totalPermissionsAssigned / allRoles.length) : 0;
    const platformCount = allRoles.filter((r) => r.scope === "Platform").length;
    const regionalCount = allRoles.filter((r) => r.scope === "Regional").length;
    return { avgPermissions, platformCount, regionalCount };
  }, [allRoles]);

  const handleManagePermissions = (role: Role) => {
    setSelectedRole(role);
    setManageDialogOpen(true);
  };

  const handleDialogSuccess = () => {
    void refetchRoles();
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-3xl font-bold flex items-center gap-2.5">
            <Key className="h-8 w-8 text-primary" />
            Role Permissions Matrix
          </h1>
          <p className="text-muted-foreground mt-1">
            Audit and configure granular functional capabilities assigned to each platform and regional role.
          </p>
        </div>
      </div>

      {/* KPI Stats */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        <Card className="shadow-xs">
          <CardHeader className="p-4 pb-2">
            <CardDescription className="text-xs">Total Configured Roles</CardDescription>
            <CardTitle className="text-2xl font-bold">{totalCount}</CardTitle>
          </CardHeader>
          <CardContent className="p-4 pt-0 text-xs text-muted-foreground">Roles in registry</CardContent>
        </Card>

        <Card className="shadow-xs">
          <CardHeader className="p-4 pb-2">
            <CardDescription className="text-xs">Platform Roles</CardDescription>
            <CardTitle className="text-2xl font-bold text-blue-600 dark:text-blue-400 flex items-center justify-between">
              <span>{stats.platformCount}</span>
              <Globe className="h-5 w-5 text-blue-500/50" />
            </CardTitle>
          </CardHeader>
          <CardContent className="p-4 pt-0 text-xs text-muted-foreground">Unrestricted scope</CardContent>
        </Card>

        <Card className="shadow-xs">
          <CardHeader className="p-4 pb-2">
            <CardDescription className="text-xs">Regional Roles</CardDescription>
            <CardTitle className="text-2xl font-bold text-indigo-600 dark:text-indigo-400 flex items-center justify-between">
              <span>{stats.regionalCount}</span>
              <MapPin className="h-5 w-5 text-indigo-500/50" />
            </CardTitle>
          </CardHeader>
          <CardContent className="p-4 pt-0 text-xs text-muted-foreground">Jurisdiction restricted</CardContent>
        </Card>

        <Card className="shadow-xs">
          <CardHeader className="p-4 pb-2">
            <CardDescription className="text-xs">Avg Permissions / Role</CardDescription>
            <CardTitle className="text-2xl font-bold text-emerald-600 dark:text-emerald-400 flex items-center justify-between">
              <span>{stats.avgPermissions}</span>
              <Activity className="h-5 w-5 text-emerald-500/50" />
            </CardTitle>
          </CardHeader>
          <CardContent className="p-4 pt-0 text-xs text-muted-foreground">Capabilities per role</CardContent>
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
              </TabsList>
            </Tabs>

            {/* Search Box */}
            <div className="relative w-full md:w-72">
              <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
              <Input
                placeholder="Search roles..."
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                className="pl-9 h-9 text-sm"
              />
            </div>
          </div>
        </CardHeader>

        <CardContent>
          {isLoadingRoles ? (
            <div className="text-center py-16 text-muted-foreground">Loading role permissions matrix...</div>
          ) : displayedRoles.length === 0 ? (
            <div className="text-center py-16 text-muted-foreground border rounded-lg border-dashed">
              <Key className="h-10 w-10 mx-auto text-muted-foreground/40 mb-2" />
              <p className="font-semibold text-foreground">No roles found</p>
              <p className="text-xs">Adjust search query or role scope filter.</p>
            </div>
          ) : (
            <div className="border rounded-lg overflow-hidden">
              <Table>
                <TableHeader className="bg-muted/50">
                  <TableRow>
                    <TableHead className="w-[260px]">Role Identity</TableHead>
                    <TableHead className="w-[140px]">Scope</TableHead>
                    <TableHead className="w-[220px]">Granted Capabilities</TableHead>
                    <TableHead>Description</TableHead>
                    <TableHead className="text-right w-[160px]">Action</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {displayedRoles.map((role) => {
                    const permissionCount = role.permissions?.length ?? 0;
                    const isSystem = role.isSystemRole;

                    return (
                      <TableRow key={role.id} className="hover:bg-muted/40 transition-colors">
                        {/* Role Identity */}
                        <TableCell>
                          <div className="space-y-1">
                            <span className="font-semibold text-foreground flex items-center gap-1.5">
                              {role.name}
                              {isSystem && (
                                <Badge
                                  variant="outline"
                                  className="text-[10px] border-amber-500/30 text-amber-600 dark:text-amber-400 bg-amber-500/10">
                                  System
                                </Badge>
                              )}
                            </span>
                            <code className="text-xs bg-muted px-1.5 py-0.5 rounded font-mono text-muted-foreground">
                              {role.key}
                            </code>
                          </div>
                        </TableCell>

                        {/* Scope */}
                        <TableCell>
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
                        </TableCell>

                        {/* Granted Capabilities */}
                        <TableCell>
                          <div className="space-y-1.5">
                            <div className="flex items-center justify-between text-xs">
                              <span className="font-semibold text-foreground">{permissionCount} Permissions</span>
                              <span className="text-muted-foreground text-[11px]">
                                {Math.min(100, Math.round((permissionCount / 70) * 100))}%
                              </span>
                            </div>
                            <Progress value={Math.min(100, (permissionCount / 70) * 100)} className="h-1.5" />
                          </div>
                        </TableCell>

                        {/* Description */}
                        <TableCell className="text-muted-foreground text-xs max-w-sm truncate">
                          {role.description || "-"}
                        </TableCell>

                        {/* Action */}
                        <TableCell className="text-right">
                          <Button
                            variant="outline"
                            size="sm"
                            onClick={() => handleManagePermissions(role)}
                            className="gap-1.5 h-8 text-xs font-medium">
                            <Settings2 className="h-3.5 w-3.5 text-primary" />
                            <span>Configure</span>
                          </Button>
                        </TableCell>
                      </TableRow>
                    );
                  })}
                </TableBody>
              </Table>
            </div>
          )}

          {/* Pagination */}
          {!isLoadingRoles && !search && totalCount > 0 && (
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

      {/* Permissions Dialog */}
      <ManageRolePermissionsDialog
        role={selectedRole}
        open={manageDialogOpen}
        onOpenChange={setManageDialogOpen}
        onSuccess={handleDialogSuccess}
      />
    </div>
  );
}
