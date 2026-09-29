"use client";

import { useEffect, useMemo, useState } from "react";
import {
  AlertCircle,
  Globe,
  Mail,
  MapPin,
  Plus,
  RefreshCw,
  Search,
  UserCheck,
  UserCog,
  Users as UsersIcon,
} from "lucide-react";

import { extractErrorMessage } from "@/lib/extract-error-message";
import { getAllUsersRequest } from "@/components/features/users/handlers/getAllUsersRequest";
import { InviteUserDialog } from "@/components/features/users/InviteUserDialog";
import type { User } from "@/components/features/users/types/User";
import { UserAccessSheet } from "@/components/features/users/UserAccessSheet";
import { Avatar, AvatarFallback } from "@/components/ui/avatar";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Pagination } from "@/components/ui/pagination";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { getInvitationsRequest } from "@/components/features/users/handlers/getInvitationsRequest";
import { PendingInvitationsTable } from "@/components/features/users/PendingInvitationsTable";
import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";
import { useUrlPagination } from "@/hooks/useUrlPagination";
import { usePermissions } from "@/providers/PermissionsContext";

const useGetAllUsers = createQueryHookFromTemplate(getAllUsersRequest);
const useGetInvitations = createQueryHookFromTemplate(getInvitationsRequest);

export default function UsersManagementPage() {
  const { currentUser } = usePermissions();
  const [activeMainTab, setActiveMainTab] = useState<string>("personnel");
  const [accessSheetOpen, setAccessSheetOpen] = useState(false);
  const [inviteDialogOpen, setInviteDialogOpen] = useState(false);
  const [selectedUser, setSelectedUser] = useState<User | null>(null);
  const [filterTab, setFilterTab] = useState<string>("all");
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
    data: usersData,
    isLoading: usersLoading,
    isError: usersIsError,
    error: usersError,
    refetch: refetchUsers,
  } = useGetAllUsers({
    pathParams: {},
    queryParams: { pageNumber: page, pageSize: pageSize, search: search || undefined },
  });

  const { data: invitationsData, refetch: refetchInvitations } = useGetInvitations({
    pathParams: {},
    queryParams: {},
  });

  const allUsers = useMemo(() => usersData?.data ?? [], [usersData?.data]);
  const totalCount = usersData?.totalRows || 0;
  const totalPages = Math.ceil(totalCount / pageSize);

  const pendingInvitationsCount = useMemo(() => {
    return (invitationsData?.data ?? []).filter(
      (i) => i.status.toLowerCase() === "pending",
    ).length;
  }, [invitationsData?.data]);

  const displayedUsers = useMemo(() => {
    if (filterTab === "platform") {
      return allUsers.filter((u) => u.role?.scope === "Platform");
    }
    if (filterTab === "regional") {
      return allUsers.filter((u) => u.role?.scope === "Regional");
    }
    return allUsers;
  }, [allUsers, filterTab]);

  const stats = useMemo(() => {
    const platformCount = allUsers.filter((u) => u.role?.scope === "Platform").length;
    const regionalCount = allUsers.filter((u) => u.role?.scope === "Regional").length;
    return { platformCount, regionalCount };
  }, [allUsers]);

  const handleOpenAccessSheet = (user: User) => {
    setSelectedUser(user);
    setAccessSheetOpen(true);
  };

  const getInitials = (firstName?: string, lastName?: string) => {
    const first = firstName?.[0] || "";
    const last = lastName?.[0] || "";
    return (first + last).toUpperCase() || "U";
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-3xl font-bold flex items-center gap-2.5">
            <UsersIcon className="h-8 w-8 text-primary" />
            User Access & Jurisdictions
          </h1>
          <p className="text-muted-foreground mt-1">
            Manage authenticated personnel, role scopes, and multi-region administrative privileges.
          </p>
        </div>
        <Button onClick={() => setInviteDialogOpen(true)} className="gap-2 shadow-sm">
          <Plus className="h-4 w-4" />
          Invite User
        </Button>
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
            <CardDescription className="text-xs">Total Active Personnel</CardDescription>
            <CardTitle className="text-2xl font-bold flex items-center justify-between">
              <span>{totalCount}</span>
              <UserCheck className="h-5 w-5 text-muted-foreground/60" />
            </CardTitle>
          </CardHeader>
          <CardContent className="p-4 pt-0 text-xs text-muted-foreground">Provisioned accounts</CardContent>
        </Card>

        <Card className="shadow-xs">
          <CardHeader className="p-4 pb-2">
            <CardDescription className="text-xs">Platform Officers</CardDescription>
            <CardTitle className="text-2xl font-bold flex items-center justify-between text-blue-600 dark:text-blue-400">
              <span>{stats.platformCount}</span>
              <Globe className="h-5 w-5 text-blue-500/60" />
            </CardTitle>
          </CardHeader>
          <CardContent className="p-4 pt-0 text-xs text-muted-foreground">Nationwide jurisdiction</CardContent>
        </Card>

        <Card className="shadow-xs">
          <CardHeader className="p-4 pb-2">
            <CardDescription className="text-xs">Regional Personnel</CardDescription>
            <CardTitle className="text-2xl font-bold flex items-center justify-between text-indigo-600 dark:text-indigo-400">
              <span>{stats.regionalCount}</span>
              <MapPin className="h-5 w-5 text-indigo-500/60" />
            </CardTitle>
          </CardHeader>
          <CardContent className="p-4 pt-0 text-xs text-muted-foreground">Region-scoped accounts</CardContent>
        </Card>
      </div>

      {/* Main Table Card */}
      <Card className="shadow-xs">
        <CardHeader className="pb-3">
          <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
            <Tabs value={filterTab} onValueChange={setFilterTab} className="w-full md:w-auto">
              <TabsList className="bg-muted/70">
                <TabsTrigger value="all" className="text-xs px-3">
                  All Users ({totalCount})
                </TabsTrigger>
                <TabsTrigger value="platform" className="text-xs px-3">
                  Platform Scope
                </TabsTrigger>
                <TabsTrigger value="regional" className="text-xs px-3">
                  Regional Scope
                </TabsTrigger>
              </TabsList>
            </Tabs>

            <div className="relative w-full md:w-72">
              <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
              <Input
                placeholder="Search users by name, email, or auth ID..."
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
              <span className="text-sm">Loading personnel directory...</span>
            </div>
          ) : usersIsError ? (
            <div className="text-center py-16 text-muted-foreground border rounded-lg border-dashed px-4">
              <AlertCircle className="h-10 w-10 mx-auto text-destructive mb-2" />
              <p className="font-semibold text-foreground">Failed to load user directory</p>
              <p className="text-xs text-muted-foreground max-w-md mx-auto mt-1 mb-4">
                {extractErrorMessage(
                  usersError,
                  "A network error occurred while connecting to the server. Please check your connection and try again."
                )}
              </p>
              <Button variant="outline" size="sm" onClick={() => refetchUsers()} className="gap-1.5">
                <RefreshCw className="h-3.5 w-3.5" />
                Retry
              </Button>
            </div>
          ) : displayedUsers.length === 0 ? (
            <div className="text-center py-16 text-muted-foreground border rounded-lg border-dashed">
              <UsersIcon className="h-10 w-10 mx-auto text-muted-foreground/40 mb-2" />
              <p className="font-semibold text-foreground">No users found</p>
              <p className="text-xs">Adjust search parameters or invite personnel.</p>
            </div>
          ) : (
            <div className="border rounded-lg overflow-hidden">
              <Table>
                <TableHeader className="bg-muted/50">
                  <TableRow>
                    <TableHead className="w-[320px]">Personnel Identity</TableHead>
                    <TableHead className="w-[200px]">Role & Scope</TableHead>
                    <TableHead className="w-[240px]">Jurisdiction Access</TableHead>
                    <TableHead className="text-right w-[140px]">Actions</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {displayedUsers.map((user: User) => {
                    const isCurrentUser = currentUser && user.id === currentUser.id;
                    const isPlatform = user.role?.scope === "Platform";

                    return (
                      <TableRow
                        key={user.id}
                        onClick={() => handleOpenAccessSheet(user)}
                        className="hover:bg-muted/40 transition-colors cursor-pointer">
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
                              {user.email ? (
                                <p className="text-xs text-muted-foreground flex items-center gap-1 truncate">
                                  <Mail className="h-3 w-3 shrink-0" />
                                  <span>{user.email}</span>
                                </p>
                              ) : (
                                <p className="text-[11px] font-mono text-muted-foreground truncate">
                                  {user.authUserId}
                                </p>
                              )}
                            </div>
                          </div>
                        </TableCell>

                        {/* Role & Scope */}
                        <TableCell>
                          <div className="space-y-1">
                            <Badge
                              variant={isPlatform ? "default" : "secondary"}
                              className="gap-1 text-xs font-medium">
                              {isPlatform ? <Globe className="h-3 w-3" /> : <MapPin className="h-3 w-3" />}
                              {user.role?.name || "No Role"}
                            </Badge>
                            <p className="text-[11px] text-muted-foreground">{user.role?.scope || "Platform"} Scope</p>
                          </div>
                        </TableCell>

                        {/* Jurisdiction Access */}
                        <TableCell>
                          {isPlatform ? (
                            <Badge
                              variant="outline"
                              className="gap-1 border-blue-500/30 text-blue-600 dark:text-blue-400 bg-blue-500/5 text-xs">
                              <Globe className="h-3 w-3" />
                              Global / Nationwide
                            </Badge>
                          ) : (
                            <Badge
                              variant="outline"
                              className="gap-1 border-indigo-500/30 text-indigo-600 dark:text-indigo-400 bg-indigo-500/5 text-xs">
                              <MapPin className="h-3 w-3" />
                              Regional Assignments
                            </Badge>
                          )}
                        </TableCell>

                        {/* Actions */}
                        <TableCell className="text-right" onClick={(e) => e.stopPropagation()}>
                          <Button
                            variant="outline"
                            size="sm"
                            onClick={() => handleOpenAccessSheet(user)}
                            className="h-8 px-2.5 text-xs gap-1.5 font-medium">
                            <UserCog className="h-3.5 w-3.5 text-primary" />
                            <span>Manage Access</span>
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
      <PendingInvitationsTable onInviteClick={() => setInviteDialogOpen(true)} />
    </TabsContent>
  </Tabs>

  {/* Unified User Access & Jurisdiction Sheet */}
  <UserAccessSheet
    user={selectedUser}
    open={accessSheetOpen}
    onOpenChange={setAccessSheetOpen}
    onSuccess={() => void refetchUsers()}
  />

  {/* Invite User Dialog */}
  <InviteUserDialog
    open={inviteDialogOpen}
    onOpenChange={setInviteDialogOpen}
    onSuccess={() => {
      void refetchUsers();
      void refetchInvitations();
    }}
  />
</div>
);
}
