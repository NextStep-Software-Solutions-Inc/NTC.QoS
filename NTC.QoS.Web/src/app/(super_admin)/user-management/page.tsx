"use client";

import { useState } from "react";
import { Edit, Users } from "lucide-react";

import { EditUserDialog } from "@/components/features/users/EditUserDialog";
import { getAllUsersRequest } from "@/components/features/users/handlers/getAllUsersRequest";
import { InviteUserDialog } from "@/components/features/users/InviteUserDialog";
import { RegionManagementDialog } from "@/components/features/users/RegionManagementDialog";
import type { User } from "@/components/features/users/types/User";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";

const useGetAllUsers = createQueryHookFromTemplate(getAllUsersRequest);

export default function UserManagementPage() {
  const [editDialogOpen, setEditDialogOpen] = useState(false);
  const [regionDialogOpen, setRegionDialogOpen] = useState(false);
  const [inviteDialogOpen, setInviteDialogOpen] = useState(false);
  const [selectedUser, setSelectedUser] = useState<User | null>(null);
  const [selectedUserForRegions, setSelectedUserForRegions] = useState<User | null>(null);

  const {
    data: usersData,
    isLoading: usersLoading,
    refetch: refetchUsers,
  } = useGetAllUsers({ pathParams: {}, queryParams: { pageNumber: 1, pageSize: 1000 } });

  const handleSelectUser = (user: User) => {
    setSelectedUser(user);
    setEditDialogOpen(true);
  };

  const handleManageRegion = (user: User) => {
    // get clerk users details using its id

    setSelectedUserForRegions(user);
    setRegionDialogOpen(true);
  };

  const users = usersData?.data ?? [];

  return (
    <div className="container mx-auto p-6 space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold">User Management</h1>
          <p className="text-muted-foreground">Manage users, roles, and organizations</p>
        </div>
        <Button onClick={() => setInviteDialogOpen(true)} className="gap-2">
          <Edit className="h-4 w-4" />
          Invite User
        </Button>
      </div>

      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Users className="h-5 w-5" />
            Users ({users.length})
          </CardTitle>
          <CardDescription>Manage user roles and organizations</CardDescription>
        </CardHeader>
        <CardContent>
          {usersLoading ? (
            <div className="text-center py-8">Loading users...</div>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>User ID</TableHead>
                  <TableHead>Role</TableHead>
                  <TableHead>Organization</TableHead>
                  <TableHead>Regions</TableHead>
                  <TableHead>Actions</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {users.map((user: User) => (
                  <TableRow key={user.id}>
                    <TableCell className="font-mono text-sm">
                      <div>
                        <p className="text-primary">
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
                      <div className="flex flex-wrap gap-1">
                        {/* We'll add region display here */}
                        <Button variant="ghost" size="sm" onClick={() => handleManageRegion(user)} className="text-xs">
                          Manage Regions
                        </Button>
                      </div>
                    </TableCell>
                    <TableCell>
                      <Button variant="ghost" size="sm" onClick={() => handleSelectUser(user)} className="gap-2">
                        <Edit className="h-4 w-4" />
                        Edit
                      </Button>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>

      {/* Edit User Dialog */}
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

      {/* Region Management Dialog */}
      <RegionManagementDialog
        open={regionDialogOpen}
        onOpenChange={setRegionDialogOpen}
        user={selectedUserForRegions}
        onSuccess={async () => {
          setRegionDialogOpen(false);
          setSelectedUserForRegions(null);
        }}
      />

      {/* Invite User Dialog */}
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
