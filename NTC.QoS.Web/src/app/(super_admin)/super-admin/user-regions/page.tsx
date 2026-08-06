"use client";

import { useEffect, useState } from "react";
import { MapPin, Plus, Trash2 } from "lucide-react";
import { toast } from "sonner";

import { getRegionsRequest } from "@/components/features/region/handlers/getRegionsRequest";
import { createUserRegionRequest } from "@/components/features/user-region/handlers/createUserRegionRequest";
import { getUserRegionsRequest } from "@/components/features/user-region/handlers/getUserRegionsRequest";
import { removeUserRegionRequest } from "@/components/features/user-region/handlers/removeUserRegionRequest";
import type { UserRegion } from "@/components/features/user-region/types/UserRegion";
import { getAllUsersRequest } from "@/components/features/users/handlers/getAllUsersRequest";
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
import { createMutationHookFromTemplate } from "@/hooks/createMutationHookFromTemplate";
import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";
import { useUrlPagination } from "@/hooks/useUrlPagination";

const useGetUserRegions = createQueryHookFromTemplate(getUserRegionsRequest);
const useRemoveUserRegion = createMutationHookFromTemplate(removeUserRegionRequest);
const useCreateUserRegion = createMutationHookFromTemplate(createUserRegionRequest);
const useGetUsers = createQueryHookFromTemplate(getAllUsersRequest);
const useGetRegions = createQueryHookFromTemplate(getRegionsRequest);

export default function UserRegionsManagementPage() {
  const [createDialogOpen, setCreateDialogOpen] = useState(false);
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
  const [selectedUserRegion, setSelectedUserRegion] = useState<UserRegion | null>(null);
  const [searchQuery, setSearchQuery] = useState("");
  const [formData, setFormData] = useState({ userId: "", regionId: "", status: "active" });

  const { page, pageSize, search, setPage, setPageSize, setSearch } = useUrlPagination();

  // Debounced search
  useEffect(() => {
    const timer = setTimeout(() => {
      setSearch(searchQuery);
    }, 300);
    return () => clearTimeout(timer);
  }, [searchQuery, setSearch]);

  const {
    data: userRegionsData,
    isLoading,
    refetch,
  } = useGetUserRegions({ pathParams: {}, queryParams: { pageNumber: page, pageSize: pageSize } });

  const { data: usersData } = useGetUsers({ pathParams: {}, queryParams: { pageNumber: 1, pageSize: 1000 } });

  const { data: regionsData } = useGetRegions({ pathParams: {}, queryParams: { pageNumber: 1, pageSize: 1000 } });

  const { mutateAsync: removeUserRegion, isPending: isDeleting } = useRemoveUserRegion();
  const { mutateAsync: createUserRegion, isPending: isCreating } = useCreateUserRegion();

  const userRegions = userRegionsData?.data ?? [];
  const users = usersData?.data ?? [];
  const regions = regionsData?.data ?? [];
  const totalCount = userRegionsData?.totalRows || 0;

  const handleCreate = async (e: React.FormEvent) => {
    e.preventDefault();
    toast.promise(createUserRegion({ pathParams: {}, payload: formData }), {
      loading: "Assigning region...",
      success: () => {
        setCreateDialogOpen(false);
        setFormData({ userId: "", regionId: "", status: "active" });
        void refetch();
        return "Region assigned successfully";
      },
      error: "Failed to assign region",
    });
  };

  const handleDelete = async () => {
    if (!selectedUserRegion) return;
    toast.promise(removeUserRegion({ pathParams: { id: selectedUserRegion.id } }), {
      loading: "Removing region...",
      success: () => {
        setDeleteDialogOpen(false);
        setSelectedUserRegion(null);
        void refetch();
        return "Region removed successfully";
      },
      error: "Failed to remove region",
    });
  };

  return (
    <div className="space-y-6">
      <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-3xl font-bold">User Regions Management</h1>
          <p className="text-muted-foreground mt-1">Manage user region assignments</p>
        </div>
        <Button onClick={() => setCreateDialogOpen(true)} className="gap-2">
          <Plus className="h-4 w-4" />
          Assign Region
        </Button>
      </div>

      <Card>
        <CardHeader>
          <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4">
            <div>
              <CardTitle className="flex items-center gap-2">
                <MapPin className="h-5 w-5" />
                User Regions
              </CardTitle>
              <CardDescription>
                Total: {totalCount.toLocaleString()} {totalCount === 1 ? "assignment" : "assignments"}
              </CardDescription>
            </div>
            <div className="w-full sm:w-64">
              <Input placeholder="Search..." value={searchQuery} onChange={(e) => setSearchQuery(e.target.value)} />
            </div>
          </div>
        </CardHeader>
        <CardContent>
          {isLoading ? (
            <div className="text-center py-8">Loading user regions...</div>
          ) : (
            <div className="overflow-x-auto">
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>User</TableHead>
                    <TableHead>Region</TableHead>
                    <TableHead>Status</TableHead>
                    <TableHead className="text-right">Actions</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {userRegions.length === 0 ? (
                    <TableRow>
                      <TableCell colSpan={4} className="text-center py-8 text-muted-foreground">
                        No user regions found
                      </TableCell>
                    </TableRow>
                  ) : (
                    userRegions.map((userRegion) => (
                      <TableRow key={userRegion.id}>
                        <TableCell>
                          <div>
                            <p className="font-medium">
                              {userRegion.user.firstName} {userRegion.user.lastName}
                            </p>
                            <p className="text-xs text-muted-foreground">{userRegion.user.role.name}</p>
                          </div>
                        </TableCell>
                        <TableCell>
                          <Badge variant="secondary">{userRegion.region.name}</Badge>
                        </TableCell>
                        <TableCell>
                          <Badge variant={userRegion.status === "active" ? "default" : "outline"}>
                            {userRegion.status}
                          </Badge>
                        </TableCell>
                        <TableCell className="text-right">
                          <Button
                            variant="ghost"
                            size="sm"
                            onClick={() => {
                              setSelectedUserRegion(userRegion);
                              setDeleteDialogOpen(true);
                            }}>
                            <Trash2 className="h-4 w-4" />
                          </Button>
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
              totalPages={Math.ceil(totalCount / pageSize)}
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
        title="Assign Region to User"
        description="Grant a user access to a specific region">
        <form onSubmit={handleCreate} className="space-y-4">
          <div>
            <Label htmlFor="userId">User</Label>
            <Select value={formData.userId} onValueChange={(value) => setFormData({ ...formData, userId: value })}>
              <SelectTrigger>
                <SelectValue placeholder="Select a user" />
              </SelectTrigger>
              <SelectContent>
                {users.map((user) => (
                  <SelectItem key={user.id} value={user.id}>
                    {user.firstName} {user.lastName} ({user.role.name})
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
          <div>
            <Label htmlFor="regionId">Region</Label>
            <Select value={formData.regionId} onValueChange={(value) => setFormData({ ...formData, regionId: value })}>
              <SelectTrigger>
                <SelectValue placeholder="Select a region" />
              </SelectTrigger>
              <SelectContent>
                {regions.map((region) => (
                  <SelectItem key={region.id} value={region.id}>
                    {region.name}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
          <div className="flex justify-end gap-2">
            <Button type="button" variant="outline" onClick={() => setCreateDialogOpen(false)}>
              Cancel
            </Button>
            <Button type="submit" disabled={isCreating}>
              {isCreating ? "Assigning..." : "Assign"}
            </Button>
          </div>
        </form>
      </ResponsiveDialog>

      {/* Delete Dialog */}
      <AlertDialog open={deleteDialogOpen} onOpenChange={setDeleteDialogOpen}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Remove Region</AlertDialogTitle>
            <AlertDialogDescription>
              Are you sure you want to remove this region assignment from the user? This action cannot be undone.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancel</AlertDialogCancel>
            <AlertDialogAction onClick={handleDelete} disabled={isDeleting}>
              {isDeleting ? "Removing..." : "Remove"}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}
