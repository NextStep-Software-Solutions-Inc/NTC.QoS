"use client";

import { useState } from "react";
import { Plus, UserCog, Users } from "lucide-react";

import { getAllUsersRequest } from "@/components/features/users/handlers/getAllUsersRequest";
import { InviteUserDialog } from "@/components/features/users/InviteUserDialog";
import { UserAccessSheet } from "@/components/features/users/UserAccessSheet";
import type { User } from "@/components/features/users/types/User";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";

const useGetAllUsers = createQueryHookFromTemplate(getAllUsersRequest);

export default function UserManagementPage() {
  const [accessSheetOpen, setAccessSheetOpen] = useState(false);
  const [inviteDialogOpen, setInviteDialogOpen] = useState(false);
  const [selectedUser, setSelectedUser] = useState<User | null>(null);

  const {
    data: usersData,
    isLoading: usersLoading,
    refetch: refetchUsers,
  } = useGetAllUsers({ pathParams: {}, queryParams: { pageNumber: 1, pageSize: 1000 } });

  const handleManageUser = (user: User) => {
    setSelectedUser(user);
    setAccessSheetOpen(true);
  };

  const users = usersData?.data ?? [];

  return (
    <div className="container mx-auto p-6 space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold">User Management</h1>
          <p className="text-muted-foreground">Manage users, roles, and regional access</p>
        </div>
        <Button onClick={() => setInviteDialogOpen(true)} className="gap-2">
          <Plus className="h-4 w-4" />
          Invite User
        </Button>
      </div>

      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Users className="h-5 w-5" />
            Users ({users.length})
          </CardTitle>
          <CardDescription>Manage user roles and regional jurisdictions</CardDescription>
        </CardHeader>
        <CardContent>
          {usersLoading ? (
            <div className="text-center py-8 text-muted-foreground">Loading users...</div>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>User Identity</TableHead>
                  <TableHead>Primary Role</TableHead>
                  <TableHead className="text-right">Actions</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {users.map((user: User) => (
                  <TableRow key={user.id}>
                    <TableCell className="font-mono text-sm">
                      <div>
                        <p className="font-semibold text-foreground">
                          {user.firstName ?? ""} {user.lastName ?? ""}
                        </p>
                        <p className="text-xs text-muted-foreground">{user.email || user.authUserId}</p>
                      </div>
                    </TableCell>
                    <TableCell>
                      <Badge variant="secondary">{user.role?.name || "No Role"}</Badge>
                    </TableCell>
                    <TableCell className="text-right">
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={() => handleManageUser(user)}
                        className="gap-1.5 text-xs font-medium">
                        <UserCog className="h-3.5 w-3.5 text-primary" />
                        Manage Access
                      </Button>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>

      {/* Unified User Access Sheet (Role + Regional Access) */}
      <UserAccessSheet
        open={accessSheetOpen}
        onOpenChange={setAccessSheetOpen}
        user={selectedUser}
        allowedRoleScope="All"
        onSuccess={async () => {
          await refetchUsers();
        }}
      />

      {/* Invite User Dialog */}
      <InviteUserDialog
        open={inviteDialogOpen}
        onOpenChange={setInviteDialogOpen}
        allowedRoleScope="All"
        onSuccess={async () => {
          await refetchUsers();
        }}
      />
    </div>
  );
}
