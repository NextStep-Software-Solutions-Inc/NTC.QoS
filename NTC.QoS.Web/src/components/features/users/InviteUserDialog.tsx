"use client";

import { useEffect, useMemo, useState } from "react";
import { Globe, Lock, MapPin, Plus, Shield, Trash2 } from "lucide-react";
import { toast } from "sonner";

import { ResponsiveDialog } from "@/components/ResponsiveDialog";
import { useGetRegions } from "@/components/features/region/hooks/useGetRegions";
import { getAllRolesRequest } from "@/components/features/role/handlers/getAllRolesRequest";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { createMutationHookFromTemplate } from "@/hooks/createMutationHookFromTemplate";
import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";
import { extractErrorMessage } from "@/lib/extract-error-message";
import { inviteUserRequest, type InviteUserRegionItem } from "./handlers/inviteUserRequest";

const useGetAllRoles = createQueryHookFromTemplate(getAllRolesRequest);
const useInviteUser = createMutationHookFromTemplate(inviteUserRequest);

interface InviteUserDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onSuccess?: () => void;
  fixedRegionId?: string;
  allowedRoleScope?: "Regional" | "Platform" | "All";
}

interface RegionRow {
  regionId: string;
  roleId: string;
}

export function InviteUserDialog({
  open,
  onOpenChange,
  onSuccess,
  fixedRegionId,
  allowedRoleScope = "All",
}: InviteUserDialogProps) {
  const [email, setEmail] = useState("");
  const [primaryRoleId, setPrimaryRoleId] = useState("");
  const [assignedRegions, setAssignedRegions] = useState<RegionRow[]>([]);

  const { data: rolesData } = useGetAllRoles({ pathParams: {}, queryParams: {} });
  const { data: regionsData } = useGetRegions({ pathParams: {}, queryParams: { pageSize: 100 } });
  const inviteUserMutation = useInviteUser();

  const roles = rolesData?.data ?? [];
  const regions = regionsData?.data ?? [];

  const filteredRoles = useMemo(() => {
    if (allowedRoleScope === "Regional") {
      return roles.filter(
        (r) =>
          r.scope === "Regional" &&
          r.key !== "super_admin" &&
          !r.name.toLowerCase().includes("super admin")
      );
    }
    if (allowedRoleScope === "Platform") {
      return roles.filter(
        (r) =>
          r.scope === "Platform" ||
          r.key === "super_admin" ||
          r.name.toLowerCase().includes("super admin")
      );
    }
    return roles;
  }, [roles, allowedRoleScope]);

  const regionalRoles = useMemo(() => {
    return roles.filter(
      (r) =>
        r.scope === "Regional" ||
        (r.scope !== "Platform" && r.key !== "super_admin" && !r.name.toLowerCase().includes("super admin"))
    );
  }, [roles]);

  const selectedRole = roles.find((r) => r.id === primaryRoleId);
  const isSuperAdmin = selectedRole?.key === "super_admin" || selectedRole?.name.toLowerCase().includes("super admin");

  const fixedRegion = useMemo(() => {
    if (!fixedRegionId) return null;
    return regions.find((r) => r.id === fixedRegionId);
  }, [regions, fixedRegionId]);

  useEffect(() => {
    if (open && fixedRegionId) {
      setAssignedRegions([{ regionId: fixedRegionId, roleId: primaryRoleId }]);
    }
  }, [open, fixedRegionId, primaryRoleId]);

  // Unselected regions available to add
  const availableRegions = useMemo(() => {
    const selectedIds = new Set(assignedRegions.map((r) => r.regionId));
    return regions.filter((reg) => !selectedIds.has(reg.id));
  }, [regions, assignedRegions]);

  const handleAddRegion = (regionId?: string) => {
    const targetRegionId = regionId || (availableRegions[0]?.id ?? "");
    if (!targetRegionId) return;
    const defaultRoleId = selectedRole?.scope === "Regional" ? primaryRoleId : (regionalRoles[0]?.id ?? primaryRoleId);
    setAssignedRegions((prev) => [
      ...prev,
      {
        regionId: targetRegionId,
        roleId: defaultRoleId,
      },
    ]);
  };

  const handleAddAllRegions = () => {
    setAssignedRegions(
      regions.map((reg) => ({
        regionId: reg.id ?? "",
        roleId: primaryRoleId,
      }))
    );
  };

  const handleRemoveRegion = (index: number) => {
    setAssignedRegions((prev) => prev.filter((_, i) => i !== index));
  };

  const handleRegionChange = (index: number, regionId: string) => {
    setAssignedRegions((prev) => {
      const copy = [...prev];
      const existing = copy[index];
      if (existing) {
        copy[index] = { regionId, roleId: existing.roleId };
      }
      return copy;
    });
  };

  const handleRoleChangeForRegion = (index: number, roleId: string) => {
    setAssignedRegions((prev) => {
      const copy = [...prev];
      const existing = copy[index];
      if (existing) {
        copy[index] = { regionId: existing.regionId, roleId };
      }
      return copy;
    });
  };

  const resetForm = () => {
    setEmail("");
    setPrimaryRoleId("");
    if (fixedRegionId) {
      setAssignedRegions([{ regionId: fixedRegionId, roleId: "" }]);
    } else {
      setAssignedRegions([]);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!email || !primaryRoleId) {
      toast.error("Please enter email and select a role.");
      return;
    }

    const redirectUrl =
      typeof window !== "undefined"
        ? `${window.location.origin}/select-region`
        : undefined;

    const regionsPayload: InviteUserRegionItem[] = isSuperAdmin
      ? []
      : fixedRegionId
        ? [{ regionId: fixedRegionId, roleId: primaryRoleId, status: "Active" }]
        : assignedRegions
            .filter((r) => r.regionId)
            .map((r) => ({
              regionId: r.regionId,
              roleId: r.roleId || primaryRoleId,
              status: "Active",
            }));

    toast.promise(
      inviteUserMutation.mutateAsync({
        pathParams: {},
        payload: {
          email,
          roleId: primaryRoleId,
          redirectUrl,
          regions: regionsPayload.length > 0 ? regionsPayload : undefined,
        },
      }),
      {
        loading: "Sending invitation via Clerk...",
        success: () => {
          onOpenChange(false);
          resetForm();
          onSuccess?.();
          return "Invitation sent successfully! User will be provisioned upon acceptance.";
        },
        error: (error) => extractErrorMessage(error),
      }
    );
  };

  const handleCancel = () => {
    onOpenChange(false);
    resetForm();
  };

  return (
    <ResponsiveDialog
      open={open}
      onOpenChange={onOpenChange}
      title={fixedRegionId ? "Invite Regional Personnel" : "Invite User"}
      description={
        fixedRegionId
          ? `Send an invitation to onboard personnel to ${fixedRegion?.name ?? "this region"}.`
          : "Send a Clerk invitation with assigned operational regions and custom roles per region."
      }>
      <form onSubmit={handleSubmit} className="space-y-4 max-h-[80vh] overflow-y-auto pr-1">
        <div>
          <Label htmlFor="email" className="font-medium">
            Email Address <span className="text-destructive">*</span>
          </Label>
          <Input
            id="email"
            type="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            placeholder="colleague@domain.gov.ph"
            required
            className="mt-1"
          />
        </div>

        <div>
          <Label htmlFor="role" className="font-medium">
            {fixedRegionId ? "Role in Region" : "Primary Role"} <span className="text-destructive">*</span>
          </Label>
          <Select
            value={primaryRoleId}
            onValueChange={(val) => {
              setPrimaryRoleId(val);
              // Update empty roles on region rows
              setAssignedRegions((prev) =>
                prev.map((row) => ({
                  ...row,
                  roleId: row.roleId ? row.roleId : val,
                }))
              );
            }}>
            <SelectTrigger className="mt-1">
              <SelectValue placeholder={fixedRegionId ? "Select regional role" : "Select primary role"} />
            </SelectTrigger>
            <SelectContent>
              {filteredRoles.map((role) => (
                <SelectItem key={role.id} value={role.id}>
                  <div className="flex items-center gap-2">
                    <Shield className="h-3.5 w-3.5 text-muted-foreground" />
                    <span>{role.name}</span>
                    {role.scope && (
                      <span className="text-xs text-muted-foreground">({role.scope})</span>
                    )}
                  </div>
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
          <p className="text-xs text-muted-foreground mt-1">
            {fixedRegionId
              ? "Designates the operational role and permissions for this region."
              : "This will be the user's primary system role."}
          </p>
        </div>

        {/* Operational Regions Assignment */}
        {fixedRegionId ? (
          <div className="rounded-lg border border-indigo-500/20 bg-indigo-500/5 p-3 flex items-start gap-3">
            <MapPin className="h-5 w-5 text-indigo-600 dark:text-indigo-400 shrink-0 mt-0.5" />
            <div className="min-w-0 flex-1">
              <div className="flex items-center justify-between">
                <p className="text-sm font-medium text-foreground">
                  Assigned Jurisdiction: {fixedRegion?.name ?? "Active Region"}
                </p>
                <Badge variant="outline" className="text-[10px] gap-1 text-muted-foreground">
                  <Lock className="h-3 w-3" /> Scoped
                </Badge>
              </div>
              <p className="text-xs text-muted-foreground mt-0.5">
                Personnel invited through this workspace will be automatically provisioned with access to this region.
              </p>
            </div>
          </div>
        ) : isSuperAdmin ? (
          <div className="rounded-lg border border-primary/20 bg-primary/5 p-3 flex items-start gap-3">
            <Globe className="h-5 w-5 text-primary shrink-0 mt-0.5" />
            <div>
              <p className="text-sm font-medium text-primary">Platform-wide Access</p>
              <p className="text-xs text-muted-foreground mt-0.5">
                Super Administrators automatically receive active operational access across all regions upon signing up.
              </p>
            </div>
          </div>
        ) : (
          <div className="space-y-3 pt-2 border-t">
            <div className="flex items-center justify-between">
              <div>
                <Label className="font-medium">Target Operational Regions</Label>
                <p className="text-xs text-muted-foreground">
                  Specify regions the user can operate in and optionally designate regional roles.
                </p>
              </div>
              <div className="flex items-center gap-1.5">
                {availableRegions.length > 0 && (
                  <Button
                    type="button"
                    variant="outline"
                    size="sm"
                    className="h-7 text-xs"
                    onClick={() => handleAddRegion()}>
                    <Plus className="h-3.5 w-3.5 mr-1" /> Add Region
                  </Button>
                )}
                {regions.length > 0 && assignedRegions.length < regions.length && (
                  <Button
                    type="button"
                    variant="ghost"
                    size="sm"
                    className="h-7 text-xs"
                    onClick={handleAddAllRegions}>
                    All Regions
                  </Button>
                )}
              </div>
            </div>

            {assignedRegions.length === 0 ? (
              <div className="border border-dashed rounded-lg p-4 text-center text-xs text-muted-foreground">
                No operational regions assigned yet. Click <strong>Add Region</strong> above to grant regional jurisdiction.
              </div>
            ) : (
              <div className="space-y-2 max-h-56 overflow-y-auto pr-1">
                {assignedRegions.map((assignment, index) => (
                  <div
                    key={index}
                    className="flex items-center gap-2 p-2.5 rounded-md border bg-muted/30">
                    <div className="flex-1">
                      <Select
                        value={assignment.regionId}
                        onValueChange={(val) => handleRegionChange(index, val)}>
                        <SelectTrigger className="h-8 text-xs">
                          <SelectValue placeholder="Select region" />
                        </SelectTrigger>
                        <SelectContent>
                          {regions.map((reg) => {
                            const isAlreadySelected = assignedRegions.some(
                              (r, i) => i !== index && r.regionId === reg.id
                            );
                            return (
                              <SelectItem
                                key={reg.id}
                                value={reg.id}
                                disabled={isAlreadySelected}
                                className="text-xs">
                                {reg.name} ({reg.code})
                              </SelectItem>
                            );
                          })}
                        </SelectContent>
                      </Select>
                    </div>

                    <div className="flex-1">
                      <Select
                        value={assignment.roleId || (selectedRole?.scope === "Regional" ? primaryRoleId : (regionalRoles[0]?.id ?? primaryRoleId))}
                        onValueChange={(val) => handleRoleChangeForRegion(index, val)}>
                        <SelectTrigger className="h-8 text-xs">
                          <SelectValue placeholder="Role in region" />
                        </SelectTrigger>
                        <SelectContent>
                          {(regionalRoles.length > 0 ? regionalRoles : roles).map((role) => (
                            <SelectItem key={role.id} value={role.id} className="text-xs">
                              {role.name}
                            </SelectItem>
                          ))}
                        </SelectContent>
                      </Select>
                    </div>

                    <Button
                      type="button"
                      variant="ghost"
                      size="icon"
                      className="h-8 w-8 text-destructive hover:text-destructive shrink-0"
                      onClick={() => handleRemoveRegion(index)}>
                      <Trash2 className="h-4 w-4" />
                    </Button>
                  </div>
                ))}
              </div>
            )}
          </div>
        )}

        <div className="flex justify-end gap-2 pt-4 border-t">
          <Button type="button" variant="outline" onClick={handleCancel}>
            Cancel
          </Button>
          <Button type="submit" disabled={inviteUserMutation.isPending}>
            {inviteUserMutation.isPending ? "Sending Invite..." : "Send Clerk Invitation"}
          </Button>
        </div>
      </form>
    </ResponsiveDialog>
  );
}
