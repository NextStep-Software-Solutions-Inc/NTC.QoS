"use client";

import Link from "next/link";
import {
  ArrowRight,
  Briefcase,
  Building,
  Building2,
  Home,
  Key,
  Layers,
  Map,
  MapPin,
  Shield,
  UserCog,
  Users,
} from "lucide-react";

import { useGetPermissions } from "@/components/features/permission/hooks/useGetPermissions";
import { useGetRegions } from "@/components/features/region/hooks/useGetRegions";
import { useGetRoles } from "@/components/features/role/hooks/useGetRoles";
import { useGetUsers } from "@/components/features/users/hooks/useGetUsers";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";

const managementCards = [
  {
    name: "Users",
    href: "/super-admin/users",
    icon: Users,
    description: "Manage user accounts, roles, and regional jurisdictions",
    color: "text-blue-500",
  },
  {
    name: "Roles",
    href: "/super-admin/roles",
    icon: Shield,
    description: "Define and manage user roles",
    color: "text-purple-500",
  },
  {
    name: "Permissions",
    href: "/super-admin/permissions",
    icon: Key,
    description: "Configure system permissions",
    color: "text-amber-500",
  },
  {
    name: "Role Permissions",
    href: "/super-admin/role-permissions",
    icon: UserCog,
    description: "Assign permissions to roles",
    color: "text-emerald-500",
  },
];

const adminSections = [
  {
    name: "User Regions",
    href: "/super-admin/user-regions",
    icon: MapPin,
    description: "Manage user region assignments",
    color: "text-blue-500",
  },
  {
    name: "Regions",
    href: "/super-admin/regions",
    icon: Map,
    description: "Manage region records",
    color: "text-indigo-500",
  },
  {
    name: "Provinces",
    href: "/super-admin/provinces",
    icon: Building,
    description: "Manage province records",
    color: "text-cyan-500",
  },
  {
    name: "Municipalities",
    href: "/super-admin/municipalities",
    icon: Building2,
    description: "Manage municipality records",
    color: "text-teal-500",
  },
  {
    name: "Barangays",
    href: "/super-admin/barangays",
    icon: Home,
    description: "Manage barangay records",
    color: "text-orange-500",
  },
  {
    name: "Service Types",
    href: "/super-admin/service-types",
    icon: Briefcase,
    description: "Manage service type categories",
    color: "text-pink-500",
  },
];

export default function SuperAdminPage() {
  // Fetch data for Quick Stats
  const { data: usersData } = useGetUsers({ pathParams: {}, queryParams: { pageNumber: 1, pageSize: 10 } });

  const { data: rolesData } = useGetRoles({ pathParams: {}, queryParams: { pageNumber: 1, pageSize: 10 } });

  const { data: permissionsData } = useGetPermissions({ pathParams: {}, queryParams: { pageNumber: 1, pageSize: 10 } });

  const { data: regionsData } = useGetRegions({ pathParams: {}, queryParams: { pageNumber: 1, pageSize: 10 } });

  const totalUsers = usersData?.totalRows || 0;
  const totalRoles = rolesData?.totalRows || 0;
  const totalPermissions = permissionsData?.totalRows || 0;
  const totalRegions = regionsData?.totalRows || 0;

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold tracking-tight">Super Admin Console</h1>
        <p className="text-muted-foreground mt-2">Manage system-wide settings, users, and geographic data</p>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
        {managementCards.map((card) => {
          const Icon = card.icon;
          return (
            <Link key={card.name} href={card.href}>
              <Card className="hover:shadow-lg transition-shadow cursor-pointer h-full">
                <CardHeader>
                  <div className="flex items-center justify-between">
                    <Icon className={`h-8 w-8 ${card.color}`} />
                    <ArrowRight className="h-5 w-5 text-muted-foreground" />
                  </div>
                  <CardTitle className="mt-4">{card.name}</CardTitle>
                  <CardDescription>{card.description}</CardDescription>
                </CardHeader>
              </Card>
            </Link>
          );
        })}
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Quick Stats</CardTitle>
          <CardDescription>Overview of system data</CardDescription>
        </CardHeader>
        <CardContent>
          <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
            <div className="text-center">
              <div className="text-2xl font-bold">{totalUsers.toLocaleString()}</div>
              <div className="text-sm text-muted-foreground">Total Users</div>
            </div>
            <div className="text-center">
              <div className="text-2xl font-bold">{totalRoles.toLocaleString()}</div>
              <div className="text-sm text-muted-foreground">Active Roles</div>
            </div>
            <div className="text-center">
              <div className="text-2xl font-bold">{totalPermissions.toLocaleString()}</div>
              <div className="text-sm text-muted-foreground">Permissions</div>
            </div>
            <div className="text-center">
              <div className="text-2xl font-bold">{totalRegions.toLocaleString()}</div>
              <div className="text-sm text-muted-foreground">Regions</div>
            </div>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
