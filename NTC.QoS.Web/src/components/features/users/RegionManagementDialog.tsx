"use client";

import { useState } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { X } from "lucide-react";
import { toast } from "sonner";

import { getRegionsRequest } from "@/components/features/region/handlers/getRegionsRequest";
import type { Region } from "@/components/features/region/types/Region";
import { createUserRegionRequest } from "@/components/features/user-region/handlers/createUserRegionRequest";
import { removeUserRegionRequest } from "@/components/features/user-region/handlers/removeUserRegionRequest";
import type { UserRegion } from "@/components/features/user-region/types/UserRegion";
import type { User } from "@/components/features/users/types/User";
import { ResponsiveDialog } from "@/components/ResponsiveDialog";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { createMutationHookFromTemplate } from "@/hooks/createMutationHookFromTemplate";
import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";
import { extractErrorMessage } from "@/lib/extract-error-message";
import { getUserRegionsRequest } from "../user-region/handlers/getUserRegionsRequest";
import { updateUserRegionStatusRequest } from "../user-region/handlers/updateUserRegionStatusRequest";

const useGetRegions = createQueryHookFromTemplate(getRegionsRequest);
const useGetUserRegions = createQueryHookFromTemplate(getUserRegionsRequest);
const useCreateUserRegion = createMutationHookFromTemplate(createUserRegionRequest);
const useRemoveUserRegion = createMutationHookFromTemplate(removeUserRegionRequest);
const useUpdateUserRegionStatus = createMutationHookFromTemplate(updateUserRegionStatusRequest);

interface RegionManagementDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  user: User | null;
  onSuccess: () => void;
}

export function RegionManagementDialog({ open, onOpenChange, user, onSuccess }: RegionManagementDialogProps) {
  const queryClient = useQueryClient();
  const [form, setForm] = useState({ regionId: "", status: "active" });

  const { data: regionsData } = useGetRegions({ pathParams: {}, queryParams: { pageNumber: 1, pageSize: 20 } });

  const { data: userRegionsData, refetch: refetchUserRegions } = useGetUserRegions({
    pathParams: {},
    queryParams: { authUserIds: user?.authUserId ? [user.authUserId] : [], pageSize: 20 },
  });

  const createUserRegionMutation = useCreateUserRegion();
  const removeUserRegionMutation = useRemoveUserRegion();
  const updateUserRegionStatusMutation = useUpdateUserRegionStatus();

  const regions = regionsData?.data ?? [];
  const userRegions = userRegionsData?.data ?? [];

  const handleAssignRegion = async () => {
    if (!user || !form.regionId) {
      toast.error("Please select a region");
      return;
    }

    toast.promise(
      createUserRegionMutation.mutateAsync({
        pathParams: {},
        payload: { userId: user.id, regionId: form.regionId, status: form.status },
      }),
      {
        loading: "Assigning...",
        success: async (res) => {
          setForm({ regionId: "", status: "active" });
          // Invalidate queries to refetch data
          await queryClient.invalidateQueries({ queryKey: ["getUserRegionsRequest"] });
          await refetchUserRegions();
          onSuccess();
          return res.message || "Region assigned successfully!";
        },
        error: (error) => extractErrorMessage(error),
      },
    );
  };

  const handleRemoveRegion = async (userRegionId: string) => {
    toast.promise(removeUserRegionMutation.mutateAsync({ pathParams: { id: userRegionId } }), {
      loading: "Removing...",
      success: async () => {
        // Invalidate queries to refetch data
        await queryClient.invalidateQueries({ queryKey: ["getUserRegionsRequest"] });
        await refetchUserRegions();
        onSuccess();
        return "Region removed successfully!";
      },
      error: (error) => extractErrorMessage(error),
    });
  };

  const handleActivateRegion = async (userRegionId: string, activate: boolean) => {
    const action = activate ? "Activating" : "Deactivating";
    const successMessage = activate ? "Region activated successfully!" : "Region deactivated successfully!";

    toast.promise(
      updateUserRegionStatusMutation.mutateAsync({
        pathParams: { id: userRegionId },
        queryParams: {},
        payload: { status: activate ? "active" : "inactive" },
      }),
      {
        loading: `${action}...`,
        success: async () => {
          // Invalidate queries to refetch data
          await queryClient.invalidateQueries({ queryKey: ["getUserRegionsRequest"] });
          await refetchUserRegions();
          onSuccess();
          return successMessage;
        },
        error: (error) => extractErrorMessage(error),
      },
    );
  };

  const handleCancel = () => {
    setForm({ regionId: "", status: "active" });
    onOpenChange(false);
  };

  return (
    <ResponsiveDialog
      open={open}
      onOpenChange={onOpenChange}
      title="Manage User Regions"
      description="Assign or remove regions for this user"
      className="sm:max-w-[600px]">
      <div className="space-y-4 max-h-[60vh] overflow-y-auto pr-2">
        <div>
          <Label>User</Label>
          <div className="font-mono text-sm bg-muted px-3 py-2 rounded-md break-all">{user?.authUserId}</div>
        </div>

        {/* Current Regions */}
        <div>
          <Label>Current Regions</Label>
          <div className="space-y-2 mt-2 max-h-[200px] overflow-y-auto">
            {userRegions && userRegions.length > 0 ? (
              userRegions.map((userRegion: UserRegion) => (
                <div key={userRegion.id} className="flex items-center justify-between gap-2 p-2 bg-muted rounded-md">
                  <div className="flex items-center gap-2 flex-1 min-w-0">
                    <Badge variant={userRegion.status === "active" ? "default" : "secondary"} className="shrink-0">
                      {userRegion.region.name}
                    </Badge>
                    <span className="text-xs text-muted-foreground truncate">
                      {userRegion.region.code} • {userRegion.status}
                    </span>
                  </div>
                  <div className="flex items-center gap-1 shrink-0">
                    {userRegion.status === "Pending" && (
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={() => handleActivateRegion(userRegion.id, true)}
                        disabled={updateUserRegionStatusMutation.isPending}
                        className="text-green-600 hover:text-green-700 h-8 px-2"
                        title="Activate region">
                        Activate
                      </Button>
                    )}
                    {userRegion.status === "Active" && (
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={() => handleActivateRegion(userRegion.id, false)}
                        disabled={updateUserRegionStatusMutation.isPending}
                        className="text-orange-600 hover:text-orange-700 h-8 px-2"
                        title="Deactivate region">
                        Deactivate
                      </Button>
                    )}
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={() => handleRemoveRegion(userRegion.id)}
                      disabled={removeUserRegionMutation.isPending}
                      className="text-red-600 hover:text-red-700 h-8 w-8 p-0"
                      title="Remove region">
                      <X className="h-4 w-4" />
                    </Button>
                  </div>
                </div>
              ))
            ) : (
              <div className="text-sm text-muted-foreground p-2 bg-muted rounded-md">
                No regions assigned to this user
              </div>
            )}
          </div>
        </div>

        {/* Assign New Region */}
        <div className="border-t pt-4">
          <Label>Assign New Region</Label>
          <div className="space-y-3 mt-2">
            <div className="w-full">
              <Select
                value={form.regionId}
                onValueChange={(value) => setForm((prev) => ({ ...prev, regionId: value }))}>
                <SelectTrigger className="w-full">
                  <SelectValue placeholder="Select a region" />
                </SelectTrigger>
                <SelectContent position="popper" className="max-h-[200px] w-full">
                  {regions.map((region: Region) => (
                    <SelectItem key={region.id} value={region.id}>
                      {region.name} ({region.code})
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div className="w-full">
              <Select value={form.status} onValueChange={(value) => setForm((prev) => ({ ...prev, status: value }))}>
                <SelectTrigger className="w-full">
                  <SelectValue placeholder="Select status" />
                </SelectTrigger>
                <SelectContent position="popper">
                  <SelectItem value="active">Active</SelectItem>
                  <SelectItem value="inactive">Inactive</SelectItem>
                </SelectContent>
              </Select>
            </div>
          </div>
        </div>

        <div className="flex justify-end gap-2 pt-4 border-t">
          <Button variant="outline" onClick={handleCancel}>
            Cancel
          </Button>
          <Button onClick={handleAssignRegion} disabled={createUserRegionMutation.isPending}>
            {createUserRegionMutation.isPending ? "Assigning..." : "Assign Region"}
          </Button>
        </div>
      </div>
    </ResponsiveDialog>
  );
}
