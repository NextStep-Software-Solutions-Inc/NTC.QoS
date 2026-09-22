"use client";

import React from "react";
import { usePermissions } from "@/providers/PermissionsContext";

interface CanProps {
  permission?: string;
  permissions?: string[];
  requireAll?: boolean;
  regionId?: string;
  fallback?: React.ReactNode;
  children: React.ReactNode;
}

export function Can({
  permission,
  permissions,
  requireAll = false,
  regionId,
  fallback = null,
  children,
}: CanProps) {
  const { hasPermission, hasAnyPermission, hasAllPermissions, hasRegionAccess, isLoading } =
    usePermissions();

  if (isLoading) {
    return null;
  }

  // Check region access if specified
  if (regionId && !hasRegionAccess(regionId)) {
    return <>{fallback}</>;
  }

  // Check single permission
  if (permission && !hasPermission(permission)) {
    return <>{fallback}</>;
  }

  // Check multiple permissions
  if (permissions && permissions.length > 0) {
    const granted = requireAll ? hasAllPermissions(permissions) : hasAnyPermission(permissions);
    if (!granted) {
      return <>{fallback}</>;
    }
  }

  return <>{children}</>;
}
