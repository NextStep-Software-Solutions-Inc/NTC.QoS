"use client";

import React, { createContext, useContext, useMemo } from "react";
import { useAuth } from "@clerk/nextjs";

import { useGetCurrentUser } from "@/components/features/users/hooks/useGetCurrentUser";
import type { CurrentUser } from "@/components/features/users/types/User";

interface PermissionsContextType {
  currentUser: CurrentUser | null;
  permissions: string[];
  accessibleRegionIds: string[];
  isSuperAdmin: boolean;
  isPlatformUser: boolean;
  isLoading: boolean;
  hasPermission: (permission: string) => boolean;
  hasAnyPermission: (permissions: string[]) => boolean;
  hasAllPermissions: (permissions: string[]) => boolean;
  hasRegionAccess: (regionId: string) => boolean;
  refetch: () => Promise<unknown>;
}

const PermissionsContext = createContext<PermissionsContextType>({
  currentUser: null,
  permissions: [],
  accessibleRegionIds: [],
  isSuperAdmin: false,
  isPlatformUser: false,
  isLoading: true,
  hasPermission: () => false,
  hasAnyPermission: () => false,
  hasAllPermissions: () => false,
  hasRegionAccess: () => false,
  refetch: async () => {},
});

export function PermissionsProvider({ children }: { children: React.ReactNode }) {
  const { isSignedIn, isLoaded: isAuthLoaded } = useAuth();

  const {
    data: currentUserResponse,
    isLoading: isUserLoading,
    refetch,
  } = useGetCurrentUser({
    pathParams: {},
    queryParams: {},
    enabled: !!isSignedIn && isAuthLoaded,
  });

  const currentUser = currentUserResponse?.data ?? null;

  const permissions = useMemo(() => currentUser?.permissions ?? [], [currentUser?.permissions]);
  const accessibleRegionIds = useMemo(
    () => currentUser?.accessibleRegionIds ?? [],
    [currentUser?.accessibleRegionIds]
  );
  const isSuperAdmin = currentUser?.isSuperAdmin ?? false;
  const isPlatformUser = currentUser?.isPlatformUser ?? false;

  const permissionsSet = useMemo(
    () => new Set(permissions.map((p) => p.toLowerCase())),
    [permissions]
  );

  const regionSet = useMemo(
    () => new Set(accessibleRegionIds.map((r) => r.toLowerCase())),
    [accessibleRegionIds]
  );

  const hasPermission = useMemo(() => {
    return (permission: string): boolean => {
      if (!permission) return false;
      if (isSuperAdmin) return true;
      return permissionsSet.has(permission.toLowerCase());
    };
  }, [isSuperAdmin, permissionsSet]);

  const hasAnyPermission = useMemo(() => {
    return (perms: string[]): boolean => {
      if (!perms || perms.length === 0) return false;
      if (isSuperAdmin) return true;
      return perms.some((p) => permissionsSet.has(p.toLowerCase()));
    };
  }, [isSuperAdmin, permissionsSet]);

  const hasAllPermissions = useMemo(() => {
    return (perms: string[]): boolean => {
      if (!perms || perms.length === 0) return true;
      if (isSuperAdmin) return true;
      return perms.every((p) => permissionsSet.has(p.toLowerCase()));
    };
  }, [isSuperAdmin, permissionsSet]);

  const hasRegionAccess = useMemo(() => {
    return (regionId: string): boolean => {
      if (!regionId) return false;
      if (isPlatformUser || isSuperAdmin) return true;
      return regionSet.has(regionId.toLowerCase());
    };
  }, [isPlatformUser, isSuperAdmin, regionSet]);

  const value = useMemo<PermissionsContextType>(
    () => ({
      currentUser,
      permissions,
      accessibleRegionIds,
      isSuperAdmin,
      isPlatformUser,
      isLoading: !isAuthLoaded || (isSignedIn ? isUserLoading : false),
      hasPermission,
      hasAnyPermission,
      hasAllPermissions,
      hasRegionAccess,
      refetch,
    }),
    [
      currentUser,
      permissions,
      accessibleRegionIds,
      isSuperAdmin,
      isPlatformUser,
      isAuthLoaded,
      isSignedIn,
      isUserLoading,
      hasPermission,
      hasAnyPermission,
      hasAllPermissions,
      hasRegionAccess,
      refetch,
    ]
  );

  return <PermissionsContext.Provider value={value}>{children}</PermissionsContext.Provider>;
}

export function usePermissions() {
  return useContext(PermissionsContext);
}
