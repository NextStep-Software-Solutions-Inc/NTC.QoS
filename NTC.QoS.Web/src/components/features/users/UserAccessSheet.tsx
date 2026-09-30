"use client";

import React, { useState } from "react";
import { useQueryClient } from "@tanstack/react-query";
import {
  AlertTriangle,
  CheckCircle2,
  Globe,
  Loader2,
  Mail,
  MapPin,
  Plus,
  Shield,
  Trash2,
  UserCheck,
  XCircle,
} from "lucide-react";
import { toast } from "sonner";

import { useGetRegions } from "@/components/features/region/hooks/useGetRegions";
import type { Region } from "@/components/features/region/types/Region";
import { getAllRolesRequest } from "@/components/features/role/handlers/getAllRolesRequest";
import type { Role } from "@/components/features/role/types/Role";
import { createUserRegionRequest } from "@/components/features/user-region/handlers/createUserRegionRequest";
import { getUserRegionsRequest } from "@/components/features/user-region/handlers/getUserRegionsRequest";
import { removeUserRegionRequest } from "@/components/features/user-region/handlers/removeUserRegionRequest";
import { updateUserRegionStatusRequest } from "@/components/features/user-region/handlers/updateUserRegionStatusRequest";
import type { UserRegion } from "@/components/features/user-region/types/UserRegion";
import { updateUserRequest } from "@/components/features/users/handlers/updateUserRequest";
import type { User } from "@/components/features/users/types/User";
import { Avatar, AvatarFallback } from "@/components/ui/avatar";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import {
  Sheet,
  SheetContent,
  SheetDescription,
  SheetHeader,
  SheetTitle,
} from "@/components/ui/sheet";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { createMutationHookFromTemplate } from "@/hooks/createMutationHookFromTemplate";
import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";
import { extractErrorMessage } from "@/lib/extract-error-message";
import { usePermissions } from "@/providers/PermissionsContext";

const useUpdateUser = createMutationHookFromTemplate(updateUserRequest);
const useGetAllRoles = createQueryHookFromTemplate(getAllRolesRequest);
const useGetUserRegions = createQueryHookFromTemplate(getUserRegionsRequest);
const useCreateUserRegion = createMutationHookFromTemplate(createUserRegionRequest);
const useRemoveUserRegion = createMutationHookFromTemplate(removeUserRegionRequest);
const useUpdateUserRegionStatus = createMutationHookFromTemplate(updateUserRegionStatusRequest);

interface UserAccessSheetProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  user: User | null;
  onSuccess: () => void;
  fixedRegionId?: string;
  allowedRoleScope?: "Regional" | "Platform" | "All";
}

export function UserAccessSheet({
  open,
  onOpenChange,
  user,
  onSuccess,
  fixedRegionId,
  allowedRoleScope = "All",
}: UserAccessSheetProps) {
  const queryClient = useQueryClient();
  const { currentUser } = usePermissions();
  const isSelf = Boolean(currentUser && user && currentUser.id === user.id);

  const [activeTab, setActiveTab] = useState<string>("role");
  const [roleForm, setRoleForm] = useState({ roleId: "" });
  const [newRegionForm, setNewRegionForm] = useState({ regionId: fixedRegionId ?? "", roleId: "", status: "Active" });

  const { data: rolesData } = useGetAllRoles({ pathParams: {}, queryParams: {} });
  const { data: regionsData } = useGetRegions({
    pathParams: {},
    queryParams: { pageNumber: 1, pageSize: 100 },
  });

  const { data: userRegionsData, refetch: refetchUserRegions, isLoading: isLoadingUserRegions } = useGetUserRegions({
    pathParams: {},
    queryParams: { authUserIds: user?.authUserId ? [user.authUserId] : [], pageSize: 50 },
    enabled: !!user?.authUserId && open,
  });

  const updateUserMutation = useUpdateUser();
  const createUserRegionMutation = useCreateUserRegion();
  const removeUserRegionMutation = useRemoveUserRegion();
  const updateUserRegionStatusMutation = useUpdateUserRegionStatus();

  const roles = rolesData?.data ?? [];
  const regions: Region[] = (regionsData?.data ?? []) as Region[];
  const userRegions = userRegionsData?.data ?? [];

  const filteredRoles = React.useMemo(() => {
    if (allowedRoleScope === "Regional") {
      return roles.filter(
        (r: Role) =>
          r.scope === "Regional" &&
          r.key !== "super_admin" &&
          !r.name.toLowerCase().includes("super admin")
      );
    }
    if (allowedRoleScope === "Platform") {
      return roles.filter(
        (r: Role) =>
          r.scope === "Platform" ||
          r.key === "super_admin" ||
          r.name.toLowerCase().includes("super admin")
      );
    }
    return roles;
  }, [roles, allowedRoleScope]);

  // Initialize roleForm when user changes
  React.useEffect(() => {
    if (user) {
      setRoleForm({
        roleId: user.role?.id ?? "",
      });
      setNewRegionForm({ regionId: fixedRegionId ?? "", roleId: user.role?.id ?? "", status: "Active" });
    }
  }, [user, fixedRegionId]);

  const initials = user
    ? `${user.firstName?.[0] || ""}${user.lastName?.[0] || ""}`.toUpperCase() || "U"
    : "U";

  const isPlatformRole = user?.role?.scope === "Platform";

  const handleUpdateRole = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!user || isSelf) return;

    if (!roleForm.roleId) {
      toast.error("Please select a primary role.");
      return;
    }

    try {
      await updateUserMutation.mutateAsync({
        pathParams: { id: user.id },
        payload: { roleId: roleForm.roleId },
      });
      toast.success("User primary role updated successfully!");
      onSuccess();
    } catch (err) {
      toast.error(extractErrorMessage(err) || "Failed to update user.");
    }
  };

  const handleAssignRegion = async () => {
    if (!user || !newRegionForm.regionId) {
      toast.error("Please select a region to assign.");
      return;
    }

    try {
      await createUserRegionMutation.mutateAsync({
        pathParams: {},
        payload: {
          userId: user.id,
          regionId: newRegionForm.regionId,
          roleId: newRegionForm.roleId || user.role?.id,
          status: newRegionForm.status,
        },
      });
      toast.success("Region assigned successfully!");
      setNewRegionForm({ regionId: "", roleId: user?.role?.id ?? "", status: "Active" });
      await queryClient.invalidateQueries({ queryKey: ["getUserRegionsRequest"] });
      await refetchUserRegions();
      onSuccess();
    } catch (err) {
      toast.error(extractErrorMessage(err) || "Failed to assign region.");
    }
  };

  const handleUpdateRegionStatus = async (userRegionId: string, activate: boolean) => {
    if (isSelf) {
      toast.error("You cannot approve or change your own region status.");
      return;
    }

    try {
      await updateUserRegionStatusMutation.mutateAsync({
        pathParams: { id: userRegionId },
        queryParams: {},
        payload: { status: activate ? "Active" : "Inactive" },
      });
      toast.success(activate ? "Region access activated!" : "Region access deactivated.");
      await queryClient.invalidateQueries({ queryKey: ["getUserRegionsRequest"] });
      await refetchUserRegions();
      onSuccess();
    } catch (err) {
      toast.error(extractErrorMessage(err) || "Failed to update region status.");
    }
  };

  const handleRemoveRegion = async (userRegionId: string) => {
    try {
      await removeUserRegionMutation.mutateAsync({ pathParams: { id: userRegionId } });
      toast.success("Region assignment removed.");
      await queryClient.invalidateQueries({ queryKey: ["getUserRegionsRequest"] });
      await refetchUserRegions();
      onSuccess();
    } catch (err) {
      toast.error(extractErrorMessage(err) || "Failed to remove region.");
    }
  };

  return (
    <Sheet open={open} onOpenChange={onOpenChange}>
      <SheetContent side="right" className="w-full sm:max-w-xl p-0 flex flex-col h-full">
        {/* Header Profile Section */}
        <SheetHeader className="p-6 pb-4 border-b bg-muted/30">
          <div className="flex items-start gap-4">
            <Avatar className="h-12 w-12 border-2 border-primary/20">
              <AvatarFallback className="bg-primary/10 text-primary font-bold text-base">
                {initials}
              </AvatarFallback>
            </Avatar>
            <div className="space-y-1 flex-1 min-w-0">
              <div className="flex items-center gap-2 flex-wrap">
                <SheetTitle className="text-xl font-bold truncate">
                  {user?.firstName ?? ""} {user?.lastName ?? ""}
                </SheetTitle>
                {isSelf && (
                  <Badge variant="outline" className="text-xs border-primary/40 text-primary font-semibold">
                    You
                  </Badge>
                )}
                {user?.role && (
                  <Badge variant={user.role.scope === "Platform" ? "default" : "secondary"} className="gap-1 text-xs">
                    {user.role.scope === "Platform" ? <Globe className="h-3 w-3" /> : <MapPin className="h-3 w-3" />}
                    {user.role.name}
                  </Badge>
                )}
              </div>
              <SheetDescription className="text-xs text-muted-foreground flex items-center gap-1.5 truncate">
                <Mail className="h-3.5 w-3.5 shrink-0" />
                <span>{user?.email || "No email linked"}</span>
              </SheetDescription>
              <p className="text-[11px] font-mono text-muted-foreground/70 truncate">ID: {user?.authUserId}</p>
            </div>
          </div>
        </SheetHeader>

        {/* Tabs Navigation */}
        <Tabs value={activeTab} onValueChange={setActiveTab} className="flex-1 flex flex-col overflow-hidden">
          <div className="px-6 pt-3 border-b bg-background">
            <TabsList className="w-full grid grid-cols-2 h-9 bg-muted/60">
              <TabsTrigger value="role" className="text-xs gap-1.5">
                <Shield className="h-3.5 w-3.5" />
                <span>Primary Role</span>
              </TabsTrigger>
              <TabsTrigger value="regions" className="text-xs gap-1.5">
                <MapPin className="h-3.5 w-3.5" />
                <span>Regional Access ({userRegions.length})</span>
              </TabsTrigger>
            </TabsList>
          </div>

          {/* Tab 1: Primary Role Assignment */}
          <TabsContent value="role" className="flex-1 overflow-y-auto p-6 space-y-6 m-0">
            {isSelf && (
              <div className="p-3.5 bg-amber-500/10 border border-amber-500/20 rounded-lg text-xs text-amber-800 dark:text-amber-300 flex items-start gap-2.5">
                <AlertTriangle className="h-4 w-4 shrink-0 mt-0.5 text-amber-600 dark:text-amber-400" />
                <div>
                  <strong className="font-semibold block">Self-Escalation Safeguard Active:</strong>
                  You cannot alter your own role assignment. Privilege changes must be granted by another authorized administrator.
                </div>
              </div>
            )}

            <form onSubmit={handleUpdateRole} className="space-y-4">
              <div className="space-y-2">
                <Label htmlFor="user-sheet-role" className="text-xs font-semibold">
                  Primary Role
                </Label>
                <Select
                  value={roleForm.roleId}
                  onValueChange={(val) => setRoleForm({ roleId: val })}
                  disabled={isSelf}>
                  <SelectTrigger id="user-sheet-role" className="h-10">
                    <SelectValue placeholder="Select a role" />
                  </SelectTrigger>
                  <SelectContent>
                    {filteredRoles.map((r: Role) => (
                      <SelectItem key={r.id} value={r.id}>
                        <div className="flex items-center gap-2">
                          <span>{r.name}</span>
                          <Badge variant="outline" className="text-[10px] py-0 h-4">
                            {r.scope || "Platform"}
                          </Badge>
                        </div>
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
                <p className="text-[11px] text-muted-foreground">
                  Determines default functional permissions and global access boundaries.
                </p>
              </div>

              <div className="pt-4 flex justify-end">
                <Button
                  type="submit"
                  disabled={isSelf || updateUserMutation.isPending}
                  className="gap-2 min-w-[140px]">
                  {updateUserMutation.isPending ? (
                    <>
                      <Loader2 className="h-4 w-4 animate-spin" />
                      Saving...
                    </>
                  ) : (
                    "Save Role Assignment"
                  )}
                </Button>
              </div>
            </form>
          </TabsContent>

          {/* Tab 2: Regional Access & Jurisdictions */}
          <TabsContent value="regions" className="flex-1 overflow-y-auto p-6 space-y-5 m-0">
            {isPlatformRole ? (
              <div className="p-4 rounded-lg bg-blue-500/10 border border-blue-500/20 text-xs text-blue-900 dark:text-blue-200 space-y-1.5">
                <div className="flex items-center gap-2 font-semibold text-blue-800 dark:text-blue-300">
                  <Globe className="h-4 w-4 shrink-0" />
                  <span>Implicit Nationwide Jurisdiction</span>
                </div>
                <p className="text-blue-700/90 dark:text-blue-300/80">
                  This user holds a <strong>Platform-scoped role ({user?.role.name})</strong> and automatically possesses full global access across all regional divisions without requiring individual region permits.
                </p>
              </div>
            ) : null}

            {isSelf && (
              <div className="p-3 bg-amber-500/10 border border-amber-500/20 rounded-lg text-xs text-amber-800 dark:text-amber-300 flex items-start gap-2">
                <AlertTriangle className="h-4 w-4 shrink-0 mt-0.5 text-amber-600 dark:text-amber-400" />
                <span>
                  <strong>Self-Approval Restriction:</strong> You cannot activate or modify the approval status of your own region access. Another administrator must review your regional requests.
                </span>
              </div>
            )}

            {/* Quick Assign Region */}
            <div className="p-4 rounded-lg border bg-card space-y-3">
              <h4 className="text-xs font-semibold uppercase tracking-wider text-muted-foreground flex items-center gap-1.5">
                <Plus className="h-3.5 w-3.5" />
                Assign New Region
              </h4>
              <div className="grid grid-cols-1 sm:grid-cols-3 gap-2.5">
                <Select
                  value={newRegionForm.regionId || "none"}
                  onValueChange={(val) => setNewRegionForm((prev) => ({ ...prev, regionId: val === "none" ? "" : val }))}>
                  <SelectTrigger className="h-9 text-xs">
                    <SelectValue placeholder="Select region..." />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="none">Choose region</SelectItem>
                    {regions.map((reg: Region) => (
                      <SelectItem key={reg.id} value={reg.id}>
                        {reg.name} ({reg.code})
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>

                <Select
                  value={newRegionForm.roleId || user?.role?.id || ""}
                  onValueChange={(val) => setNewRegionForm((prev) => ({ ...prev, roleId: val }))}>
                  <SelectTrigger className="h-9 text-xs">
                    <SelectValue placeholder="Regional role" />
                  </SelectTrigger>
                  <SelectContent>
                    {roles.map((r: Role) => (
                      <SelectItem key={r.id} value={r.id}>
                        {r.name}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>

                <Select
                  value={newRegionForm.status}
                  onValueChange={(val) => setNewRegionForm((prev) => ({ ...prev, status: val }))}>
                  <SelectTrigger className="h-9 text-xs">
                    <SelectValue placeholder="Initial status" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="Active">Active (Immediate)</SelectItem>
                    <SelectItem value="Pending">Pending Review</SelectItem>
                  </SelectContent>
                </Select>
              </div>

              <div className="flex justify-end pt-1">
                <Button
                  type="button"
                  size="sm"
                  onClick={handleAssignRegion}
                  disabled={!newRegionForm.regionId || createUserRegionMutation.isPending}
                  className="h-8 text-xs gap-1.5">
                  <UserCheck className="h-3.5 w-3.5" />
                  Assign Region
                </Button>
              </div>
            </div>

            {/* Assigned Regions List */}
            <div className="space-y-2.5">
              <div className="flex items-center justify-between text-xs text-muted-foreground">
                <span className="font-semibold uppercase tracking-wider">Assigned Regional Jurisdictions</span>
                <span>{userRegions.length} assigned</span>
              </div>

              {isLoadingUserRegions ? (
                <div className="text-center py-8 text-muted-foreground text-xs">Loading regional access...</div>
              ) : userRegions.length === 0 ? (
                <div className="text-center py-10 rounded-lg border border-dashed text-muted-foreground text-xs space-y-1">
                  <MapPin className="h-8 w-8 mx-auto text-muted-foreground/40" />
                  <p className="font-medium text-foreground">No specific regions assigned</p>
                  <p>Assign a region above to grant regional operational jurisdiction.</p>
                </div>
              ) : (
                <div className="space-y-2">
                  {userRegions.map((ur: UserRegion) => {
                    const statusLower = ur.status?.toLowerCase() ?? "";
                    const isActive = statusLower === "active";
                    const isPending = statusLower === "pending";

                    return (
                      <div
                        key={ur.id}
                        className="flex items-center justify-between p-3 rounded-lg border bg-card hover:bg-muted/30 transition-colors gap-3">
                        <div className="space-y-0.5 flex-1 min-w-0">
                          <div className="flex items-center gap-2 flex-wrap">
                            <span className="font-semibold text-sm truncate">{ur.region.name}</span>
                            <Badge
                              variant={isActive ? "default" : isPending ? "outline" : "secondary"}
                              className={`text-[10px] px-1.5 py-0 h-4 shrink-0 ${
                                isPending ? "border-amber-500/40 text-amber-600 dark:text-amber-400 bg-amber-500/5" : ""
                              }`}>
                              {ur.status}
                            </Badge>
                            {ur.role && (
                              <Badge variant="outline" className="text-[10px] px-1.5 py-0 h-4 shrink-0 font-normal">
                                {ur.role.name}
                              </Badge>
                            )}
                          </div>
                          <p className="text-xs text-muted-foreground font-mono">{ur.region.code}</p>
                        </div>

                        {/* Region Controls */}
                        <div className="flex items-center gap-1 shrink-0">
                          {isPending && (
                            <Button
                              type="button"
                              variant="outline"
                              size="sm"
                              onClick={() => handleUpdateRegionStatus(ur.id, true)}
                              disabled={isSelf || updateUserRegionStatusMutation.isPending}
                              className="h-7 px-2 text-xs text-emerald-600 hover:text-emerald-700 hover:bg-emerald-500/10 gap-1"
                              title={isSelf ? "Self-approval forbidden" : "Approve region request"}>
                              <CheckCircle2 className="h-3.5 w-3.5" />
                              <span>Approve</span>
                            </Button>
                          )}

                          {isActive && (
                            <Button
                              type="button"
                              variant="ghost"
                              size="sm"
                              onClick={() => handleUpdateRegionStatus(ur.id, false)}
                              disabled={isSelf || updateUserRegionStatusMutation.isPending}
                              className="h-7 px-2 text-xs text-amber-600 hover:text-amber-700 hover:bg-amber-500/10 gap-1"
                              title={isSelf ? "Self-modification forbidden" : "Deactivate region access"}>
                              <XCircle className="h-3.5 w-3.5" />
                              <span>Deactivate</span>
                            </Button>
                          )}

                          <Button
                            type="button"
                            variant="ghost"
                            size="sm"
                            onClick={() => handleRemoveRegion(ur.id)}
                            disabled={removeUserRegionMutation.isPending}
                            className="h-7 w-7 p-0 text-red-600 hover:text-red-700 hover:bg-red-500/10"
                            title="Remove region access">
                            <Trash2 className="h-3.5 w-3.5" />
                          </Button>
                        </div>
                      </div>
                    );
                  })}
                </div>
              )}
            </div>
          </TabsContent>
        </Tabs>
      </SheetContent>
    </Sheet>
  );
}
