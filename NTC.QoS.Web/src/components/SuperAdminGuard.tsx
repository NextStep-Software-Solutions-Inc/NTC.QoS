"use client";

import React from "react";
import LoadingComponent from "@/components/Loading";
import Unauthorized from "@/components/Unauthorized";
import { usePermissions } from "@/providers/PermissionsContext";

interface SuperAdminGuardProps {
  children: React.ReactNode;
}

export function SuperAdminGuard({ children }: SuperAdminGuardProps) {
  const { isSuperAdmin, isLoading } = usePermissions();

  if (isLoading) {
    return <LoadingComponent />;
  }

  if (!isSuperAdmin) {
    return <Unauthorized />;
  }

  return <>{children}</>;
}
