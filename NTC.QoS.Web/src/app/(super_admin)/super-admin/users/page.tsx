"use client";

import { useEffect, useState } from "react";
import { Edit, Plus, Users as UsersIcon } from "lucide-react";

import { EditUserDialog } from "@/components/features/users/EditUserDialog";
import { getAllUsersRequest } from "@/components/features/users/handlers/getAllUsersRequest";
import { InviteUserDialog } from "@/components/features/users/InviteUserDialog";
import { RegionManagementDialog } from "@/components/features/users/RegionManagementDialog";
import type { User } from "@/components/features/users/types/User";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Pagination } from "@/components/ui/pagination";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";
import { useUrlPagination } from "@/hooks/useUrlPagination";

const useGetAllUsers = createQueryHookFromTemplate(getAllUsersRequest);

export default function UsersManagementPage() {
  const [editDialogOpen, setEditDialogOpen] = useState(false);
  const [regionDialogOpen, setRegionDialogOpen] = useState(false);
  const [inviteDialogOpen, setInviteDialogOpen] = useState(false);
  const [selectedUser, setSelectedUser] = useState<User | null>(null);
  const [selectedUserForRegions, setSelectedUserForRegions] = useState<User | null>(null);
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
    refetch: refetchUsers,
  } = useGetAllUsers({ pathParams: {}, queryParams: { pageNumber: page, pageSize: pageSize, search: search || undefined } });

  const handleSelectUser = (user: User) => {
    setSelectedUser(user);
    setEditDialogOpen(true);
  };

  const handleManageRegion = (user: User) => {
    setSelectedUserForRegions(user);
    setRegionDialogOpen(true);
  };

  const users = usersData?.data ?? [];
  const totalCount = usersData?.totalRows || 0;
  const totalPages = Math.ceil(totalCount / pageSize);

  return (
    <div className="space-y-6">
      <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-3xl font-bold">User Management</h1>
          <p className="text-muted-foreground mt-1">Manage users, roles, and organizations</p>
        </div>
        <Button onClick={() => setInviteDialogOpen(true)} className="gap-2">
          <Plus className="h-4 w-4" />
          Invite User
        </Button>
      </div>

      <Card>
        <CardHeader>
          <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4">
            <div>
              <CardTitle className="flex items-center gap-2">
                <UsersIcon className="h-5 w-5" />
                Users ({totalCount})
              </CardTitle>
              <CardDescription>Manage user roles and organizations</CardDescription>
            </div>
            <div className="w-full sm:w-64">
              <Input
                placeholder="Search users..."
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
              />
            </div>
          </div>
        </CardHeader>
        <CardContent>
          {usersLoading ? (
            <div className="text-center py-8">Loading users...</div>
          ) : (
            <div className="overflow-x-auto">
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>User</TableHead>
                    <TableHead>Role</TableHead>
                    <TableHead>Organization</TableHead>
                    <TableHead>Regions</TableHead>
                    <TableHead className="text-right">Actions</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {users.length === 0 ? (
                    <TableRow>
                      <TableCell colSpan={5} className="text-center py-8 text-muted-foreground">
                        No users found
                      </TableCell>
                    </TableRow>
                  ) : (
                    users.map((user: User) => (
                      <TableRow key={user.id}>
                        <TableCell className="font-mono text-sm">
                          <div>
                            <p className="text-primary font-medium">
                              {user.firstName ?? "-"} {user.lastName ?? "-"}
                            </p>
                            <p className="text-xs text-muted-foreground">{user.authUserId}</p>
                          </div>
                        </TableCell>
                        <TableCell>
                          <Badge variant="secondary">{user.role.name}</Badge>
                        </TableCell>
                        <TableCell>
                          <Badge variant="outline">{user.organization.name}</Badge>
                        </TableCell>
                        <TableCell>
                          <Button
                            variant="ghost"
                            size="sm"
                            onClick={() => handleManageRegion(user)}
                            className="text-xs">
                            Manage Regions
                          </Button>
                        </TableCell>
                        <TableCell className="text-right">
                          <div className="flex justify-end gap-2">
                            <Button variant="ghost" size="sm" onClick={() => handleSelectUser(user)} className="gap-2">
                              <Edit className="h-4 w-4" />
                              Edit
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

          {!usersLoading && !search && totalCount > 0 && (
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

      <EditUserDialog
        open={editDialogOpen}
        onOpenChange={setEditDialogOpen}
        user={selectedUser}
        onSuccess={async () => {
          setEditDialogOpen(false);
          setSelectedUser(null);
          await refetchUsers();
        }}
      />

      <RegionManagementDialog
        open={regionDialogOpen}
        onOpenChange={setRegionDialogOpen}
        user={selectedUserForRegions}
        onSuccess={async () => {
          setRegionDialogOpen(false);
          setSelectedUserForRegions(null);
        }}
      />

      <InviteUserDialog
        open={inviteDialogOpen}
        onOpenChange={setInviteDialogOpen}
        onSuccess={async () => {
          await refetchUsers();
        }}
      />
    </div>
  );
}
