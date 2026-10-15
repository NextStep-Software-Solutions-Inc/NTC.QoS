"use client";

import { useEffect, useMemo, useState } from "react";
import { useParams } from "next/navigation";
import {
  AlertCircle,
  HardHat,
  Mail,
  MapPin,
  Plus,
  RefreshCw,
  Search,
  Shield,
  UserCheck,
  UserCog,
  Users as UsersIcon,
} from "lucide-react";

import { useGetRegions } from "@/components/features/region/hooks/useGetRegions";
import type { Region } from "@/components/features/region/types/Region";
import { getAllUsersRequest } from "@/components/features/users/handlers/getAllUsersRequest";
import { getInvitationsRequest } from "@/components/features/users/handlers/getInvitationsRequest";
import { InviteUserDialog } from "@/components/features/users/InviteUserDialog";
import { PendingInvitationsTable } from "@/components/features/users/PendingInvitationsTable";
import type { User } from "@/components/features/users/types/User";
import { UserAccessSheet } from "@/components/features/users/UserAccessSheet";
import Unauthorized from "@/components/Unauthorized";
import { Avatar, AvatarFallback } from "@/components/ui/avatar";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Pagination } from "@/components/ui/pagination";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";
import { useUrlPagination } from "@/hooks/useUrlPagination";
import { extractErrorMessage } from "@/lib/extract-error-message";
import { usePermissions } from "@/providers/PermissionsContext";

const useGetAllUsers = createQueryHookFromTemplate(getAllUsersRequest);
const useGetInvitations = createQueryHookFromTemplate(getInvitationsRequest);

export default function RegionalPersonnelPage() {
  const { region } = useParams<{ region: string }>();
  const { currentUser, hasPermission, isLoading: permissionsLoading } = usePermissions();

  const [accessSheetOpen, setAccessSheetOpen] = useState(false);
  const [inviteDialogOpen, setInviteDialogOpen] = useState(false);
  const [selectedUser, setSelectedUser] = useState<User | null>(null);
  const [searchQuery, setSearchQuery] = useState("");
  const [activeMainTab, setActiveMainTab] = useState<string>("personnel");

  const { page, pageSize, search, setPage, setPageSize, setSearch } = useUrlPagination();

  const { data: invitationsData, refetch: refetchInvitations } = useGetInvitations({
    pathParams: {},
    queryParams: {},
  });

  // Load regions to locate the current active region
  const { data: regionsData } = useGetRegions({
    pathParams: {},
    queryParams: { pageNumber: 1, pageSize: 100 },
  });

  const regions: Region[] = useMemo(() => (regionsData?.data ?? []) as Region[], [regionsData?.data]);

  const currentRegion = useMemo(() => {
    return regions.find((r) => r.id === region || r.code?.toLowerCase() === region?.toLowerCase()) ?? null;
  }, [regions, region]);

  // Debounced search
  useEffect(() => {
    const timer = setTimeout(() => {
      setSearch(searchQuery);
    }, 300);
    return () => clearTimeout(timer);
  }, [searchQuery, setSearch]);

  const {
    data: usersData,
    isLoading: usersLoading,
    isError: usersIsError,
    error: usersError,
    refetch: refetchUsers,
  } = useGetAllUsers({
    pathParams: {},
    queryParams: {
      pageNumber: page,
      pageSize: pageSize,
      search: search || undefined,
      regionIds: currentRegion ? [currentRegion.id] : undefined,
    },
    enabled: !!currentRegion?.id,
  });

  const isSuperAdminUser = (u: User) => {
    const roleKey = u.role?.key?.toLowerCase();
    const roleName = u.role?.name?.toLowerCase();
    return (
      roleKey === "super_admin" ||
      roleKey === "super-admin" ||
      roleName === "superadmin" ||
      roleName === "super admin"
    );
  };

  const users = useMemo(() => {
    const raw = usersData?.data ?? [];
    return raw.filter((u) => !isSuperAdminUser(u));
  }, [usersData?.data]);

  const totalCount = useMemo(() => {
    const raw = usersData?.data ?? [];
    const superAdminCount = raw.filter(isSuperAdminUser).length;
    const rawTotal = usersData?.totalRows ?? raw.length;
    return Math.max(0, rawTotal - superAdminCount);
  }, [usersData?.totalRows, usersData?.data]);

  const totalPages = Math.ceil(totalCount / pageSize);

  const stats = useMemo(() => {
    const fieldEngineers = users.filter((u) =>
      u.role?.name?.toLowerCase().includes("field") || u.role?.key?.toLowerCase().includes("field")
    ).length;
    const regionalAdmins = users.filter((u) =>
      u.role?.name?.toLowerCase().includes("admin") || u.role?.key?.toLowerCase().includes("admin")
    ).length;
    const staffMembers = users.length - fieldEngineers - regionalAdmins;
    return {
      fieldEngineers,
      regionalAdmins,
      staffMembers: staffMembers > 0 ? staffMembers : 0,
    };
  }, [users]);

  const pendingInvitationsCount = useMemo(() => {
    const list = invitationsData?.data ?? [];
    return list.filter((i) => {
      if (i.status.toLowerCase() !== "pending") return false;
      if (currentRegion?.id) {
        const matchesThisRegion = i.regions.some((r) => r.regionId === currentRegion.id);
        const isPlatformOrGlobal = i.roleScope === "Platform" || i.regions.length === 0;
        return matchesThisRegion || isPlatformOrGlobal;
      }
      return true;
    }).length;
  }, [invitationsData?.data, currentRegion?.id]);

  const handleOpenAccessSheet = (user: User) => {
    setSelectedUser(user);
    setAccessSheetOpen(true);
  };

  const getInitials = (firstName?: string, lastName?: string) => {
    const first = firstName?.[0] || "";
    const last = lastName?.[0] || "";
    return (first + last).toUpperCase() || "U";
  };

  if (!permissionsLoading && !hasPermission("users.view")) {
    return <Unauthorized />;
  }

  const canInvite = hasPermission("users.invite");
  const canUpdate = hasPermission("users.update");

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl sm:text-3xl font-bold flex items-center gap-2.5">
            <UsersIcon className="h-7 w-7 text-primary" />
            <span>Regional Personnel Directory</span>
          </h1>
          <p className="text-muted-foreground mt-1 text-sm">
            Manage authenticated staff, field engineers, and regional administrative privileges for{" "}
            <strong>{currentRegion ? `${currentRegion.name} (${currentRegion.code})` : "this region"}</strong>.
          </p>
        </div>
        {canInvite && (
          <Button onClick={() => setInviteDialogOpen(true)} className="gap-2 shadow-sm shrink-0">
            <Plus className="h-4 w-4" />
            <span>Invite Personnel</span>
          </Button>
        )}
      </div>

      {/* Top Level Tabs: Active Personnel vs Sent Invitations */}
      <Tabs value={activeMainTab} onValueChange={setActiveMainTab} className="space-y-6">
        <TabsList className="bg-muted/70 p-1 border">
          <TabsTrigger value="personnel" className="gap-2 text-xs sm:text-sm">
            <UsersIcon className="h-4 w-4" />
            <span>Active Personnel</span>
            <Badge variant="secondary" className="text-[10px] px-1.5 py-0 font-normal">
              {totalCount}
            </Badge>
          </TabsTrigger>
          <TabsTrigger value="invitations" className="gap-2 text-xs sm:text-sm">
            <Mail className="h-4 w-4" />
            <span>Sent Invitations</span>
            {pendingInvitationsCount > 0 && (
              <Badge
                variant="outline"
                className="text-[10px] px-1.5 py-0 bg-amber-50 text-amber-700 border-amber-300 dark:bg-amber-950 dark:text-amber-300 dark:border-amber-800 font-semibold"
              >
                {pendingInvitationsCount}
              </Badge>
            )}
          </TabsTrigger>
        </TabsList>

        <TabsContent value="personnel" className="space-y-6 mt-0">
          {/* KPI Stats Cards */}
      <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
        <Card className="shadow-xs">
          <CardHeader className="p-4 pb-2">
            <CardDescription className="text-xs">Assigned Personnel</CardDescription>
            <CardTitle className="text-2xl font-bold flex items-center justify-between">
              <span>{totalCount}</span>
              <UserCheck className="h-5 w-5 text-muted-foreground/60" />
            </CardTitle>
          </CardHeader>
          <CardContent className="p-4 pt-0 text-xs text-muted-foreground">
            Active in {currentRegion?.code ?? "Region"}
          </CardContent>
        </Card>

        <Card className="shadow-xs">
          <CardHeader className="p-4 pb-2">
            <CardDescription className="text-xs">Field Engineers</CardDescription>
            <CardTitle className="text-2xl font-bold flex items-center justify-between text-blue-600 dark:text-blue-400">
              <span>{stats.fieldEngineers}</span>
              <HardHat className="h-5 w-5 text-blue-500/60" />
            </CardTitle>
          </CardHeader>
          <CardContent className="p-4 pt-0 text-xs text-muted-foreground">
            Drive tests & cell inspection
          </CardContent>
        </Card>

        <Card className="shadow-xs">
          <CardHeader className="p-4 pb-2">
            <CardDescription className="text-xs">Administrative & Staff</CardDescription>
            <CardTitle className="text-2xl font-bold flex items-center justify-between text-indigo-600 dark:text-indigo-400">
              <span>{stats.regionalAdmins + stats.staffMembers}</span>
              <Shield className="h-5 w-5 text-indigo-500/60" />
            </CardTitle>
          </CardHeader>
          <CardContent className="p-4 pt-0 text-xs text-muted-foreground">
            Office & operations personnel
          </CardContent>
        </Card>
      </div>

      {/* Main Table Card */}
      <Card className="shadow-xs">
        <CardHeader className="pb-3">
          <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
            <div className="flex items-center gap-2">
              <Badge variant="outline" className="gap-1.5 px-2.5 py-1 text-xs">
                <MapPin className="h-3.5 w-3.5 text-primary" />
                <span>{currentRegion ? currentRegion.name : "Regional Scope"}</span>
              </Badge>
              <span className="text-xs text-muted-foreground">({totalCount} Total)</span>
            </div>

            <div className="relative w-full md:w-80">
              <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
              <Input
                placeholder="Search by name, email, or auth ID..."
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                className="pl-9 h-9 text-sm"
              />
            </div>
          </div>
        </CardHeader>

        <CardContent>
          {usersLoading ? (
            <div className="text-center py-16 text-muted-foreground flex flex-col items-center gap-2">
              <RefreshCw className="h-6 w-6 animate-spin text-primary" />
              <span className="text-sm">Loading regional personnel directory...</span>
            </div>
          ) : usersIsError ? (
            <div className="text-center py-16 text-muted-foreground border rounded-lg border-dashed px-4">
              <AlertCircle className="h-10 w-10 mx-auto text-destructive mb-2" />
              <p className="font-semibold text-foreground">Failed to load regional personnel</p>
              <p className="text-xs text-muted-foreground max-w-md mx-auto mt-1 mb-4">
                {extractErrorMessage(
                  usersError,
                  "A network error occurred while connecting to the server. Please check your connection."
                )}
              </p>
              <Button variant="outline" size="sm" onClick={() => refetchUsers()} className="gap-1.5">
                <RefreshCw className="h-3.5 w-3.5" />
                Retry
              </Button>
            </div>
          ) : users.length === 0 ? (
            <div className="text-center py-16 text-muted-foreground border rounded-lg border-dashed">
              <UsersIcon className="h-10 w-10 mx-auto text-muted-foreground/40 mb-2" />
              <p className="font-semibold text-foreground">No personnel found</p>
              <p className="text-xs mt-1">
                {searchQuery
                  ? "Adjust search parameters or clear your search query."
                  : "Invite officers or field engineers to onboard them to this region."}
              </p>
              {canInvite && !searchQuery && (
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => setInviteDialogOpen(true)}
                  className="mt-4 gap-1.5">
                  <Plus className="h-3.5 w-3.5" />
                  Invite Personnel
                </Button>
              )}
            </div>
          ) : (
            <div className="border rounded-lg overflow-hidden">
              <Table>
                <TableHeader className="bg-muted/50">
                  <TableRow>
                    <TableHead className="w-[320px]">Personnel Identity</TableHead>
                    <TableHead className="w-[200px]">Regional Role</TableHead>
                    <TableHead className="w-[220px]">Jurisdiction Access</TableHead>
                    <TableHead className="text-right w-[140px]">Actions</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {users.map((user: User) => {
                    const isCurrentUser = currentUser && user.id === currentUser.id;

                    return (
                      <TableRow
                        key={user.id}
                        onClick={() => canUpdate && handleOpenAccessSheet(user)}
                        className={`transition-colors ${canUpdate ? "cursor-pointer hover:bg-muted/40" : ""}`}>
                        {/* User Identity */}
                        <TableCell>
                          <div className="flex items-center gap-3">
                            <Avatar className="h-9 w-9 border border-border shrink-0">
                              <AvatarFallback className="bg-primary/10 text-primary text-xs font-semibold">
                                {getInitials(user.firstName, user.lastName)}
                              </AvatarFallback>
                            </Avatar>
                            <div className="space-y-0.5 min-w-0">
                              <div className="flex items-center gap-1.5 flex-wrap">
                                <span className="font-semibold text-sm text-foreground truncate">
                                  {user.firstName ?? ""} {user.lastName ?? ""}
                                </span>
                                {isCurrentUser && (
                                  <Badge
                                    variant="outline"
                                    className="text-[10px] px-1.5 py-0 h-4 border-primary/40 text-primary font-bold">
                                    You
                                  </Badge>
                                )}
                              </div>
                              <p className="text-xs text-muted-foreground truncate">
                                {user.email || user.authUserId}
                              </p>
                            </div>
                          </div>
                        </TableCell>

                        {/* Role */}
                        <TableCell>
                          <div className="space-y-1">
                            <Badge variant="secondary" className="gap-1 text-xs font-medium">
                              <MapPin className="h-3 w-3 text-indigo-500" />
                              <span>{user.role?.name || "No Role"}</span>
                            </Badge>
                            <p className="text-[11px] text-muted-foreground">Regional Jurisdiction</p>
                          </div>
                        </TableCell>

                        {/* Jurisdiction Access */}
                        <TableCell>
                          <Badge
                            variant="outline"
                            className="gap-1 border-indigo-500/30 text-indigo-600 dark:text-indigo-400 bg-indigo-500/5 text-xs">
                            <MapPin className="h-3 w-3" />
                            <span>{currentRegion?.name ?? "Regional Scope"}</span>
                          </Badge>
                        </TableCell>

                        {/* Actions */}
                        <TableCell className="text-right" onClick={(e) => e.stopPropagation()}>
                          {canUpdate ? (
                            <Button
                              variant="outline"
                              size="sm"
                              onClick={() => handleOpenAccessSheet(user)}
                              className="h-8 px-2.5 text-xs gap-1.5 font-medium">
                              <UserCog className="h-3.5 w-3.5 text-primary" />
                              <span>Manage</span>
                            </Button>
                          ) : (
                            <span className="text-xs text-muted-foreground">View Only</span>
                          )}
                        </TableCell>
                      </TableRow>
                    );
                  })}
                </TableBody>
              </Table>
            </div>
          )}

          {/* Pagination */}
          {!usersLoading && !search && totalCount > 0 && (
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
    </TabsContent>

    <TabsContent value="invitations" className="space-y-6 mt-0">
      <PendingInvitationsTable
        regionId={currentRegion?.id}
        onInviteClick={() => setInviteDialogOpen(true)}
      />
    </TabsContent>
  </Tabs>

  {/* Scoped User Access Sheet */}
  <UserAccessSheet
    user={selectedUser}
    open={accessSheetOpen}
    onOpenChange={setAccessSheetOpen}
    fixedRegionId={currentRegion?.id}
    allowedRoleScope="Regional"
    onSuccess={() => void refetchUsers()}
  />

  {/* Scoped Invite Personnel Dialog */}
  <InviteUserDialog
    open={inviteDialogOpen}
    onOpenChange={setInviteDialogOpen}
    fixedRegionId={currentRegion?.id}
    allowedRoleScope="Regional"
    onSuccess={() => {
      void refetchUsers();
      void refetchInvitations();
      setActiveMainTab("invitations");
    }}
  />
</div>
  );
}
