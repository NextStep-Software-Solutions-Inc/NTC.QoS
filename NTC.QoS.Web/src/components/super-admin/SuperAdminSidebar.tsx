"use client";

import { useState } from "react";
import Link from "next/link";
import { usePathname } from "next/navigation";
import {
  Briefcase,
  Building,
  Building2,
  FileText,
  Home,
  Key,
  Layers,
  Map,
  MapPin,
  Menu,
  Shield,
  Sparkles,
  UserCog,
  Users,
  X,
} from "lucide-react";

import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";
import { UserButton, useUser } from "@clerk/nextjs";

const navigation = [
  { name: "Users", href: "/super-admin/users", icon: Users, description: "Manage user accounts" },
  { name: "Roles", href: "/super-admin/roles", icon: Shield, description: "Manage user roles" },
  { name: "Permissions", href: "/super-admin/permissions", icon: Key, description: "Manage permissions" },
  {
    name: "Role Permissions",
    href: "/super-admin/role-permissions",
    icon: UserCog,
    description: "Assign permissions to roles",
  },
  {
    name: "User Regions",
    href: "/super-admin/user-regions",
    icon: MapPin,
    description: "Manage user region assignments",
  },
  { name: "Regions", href: "/super-admin/regions", icon: Map, description: "Manage regions" },
  { name: "Provinces", href: "/super-admin/provinces", icon: Building, description: "Manage provinces" },
  {
    name: "Municipalities",
    href: "/super-admin/municipalities",
    icon: Building2,
    description: "Manage municipalities",
  },
  { name: "Barangays", href: "/super-admin/barangays", icon: Home, description: "Manage barangays" },
  { name: "Service Types", href: "/super-admin/service-types", icon: Briefcase, description: "Manage service types" },
  { name: "Organizations", href: "/super-admin/organizations", icon: Layers, description: "Manage organizations" },
  {
    name: "Report Templates",
    href: "/super-admin/report-templates",
    icon: FileText,
    description: "Manage AI report templates",
  },
  {
    name: "AI Reports",
    href: "/super-admin/ai-reports",
    icon: Sparkles,
    description: "Manage AI-generated reports",
  },
];

export function SuperAdminSidebar() {
  const pathname = usePathname();
  const [isOpen, setIsOpen] = useState(false);
  const { isLoaded } = useUser();

  return (
    <>
      {/* Mobile header with menu button and user avatar */}
      <div className="lg:hidden fixed top-0 left-0 right-0 z-50 h-16 bg-card border-b border-border flex items-center justify-between px-4">
        <Button variant="outline" size="icon" onClick={() => setIsOpen(!isOpen)} className="bg-background">
          {isOpen ? <X className="h-5 w-5" /> : <Menu className="h-5 w-5" />}
        </Button>
        <h2 className="text-lg font-semibold text-foreground">Super Admin</h2>
        <div className="w-8 h-8 flex items-center justify-center">
          {!isLoaded ? (
            <div className="animate-pulse rounded-full bg-gray-300 w-8 h-8" />
          ) : (
            <UserButton appearance={{ elements: { avatarBox: "w-8 h-8" } }} />
          )}
        </div>
      </div>

      {/* Spacer for mobile header */}
      <div className="lg:hidden h-16" />

      {/* Overlay for mobile */}
      {isOpen && <div className="lg:hidden fixed inset-0 bg-black/50 z-40" onClick={() => setIsOpen(false)} />}

      {/* Sidebar */}
      <aside
        className={cn(
          "fixed top-0 left-0 z-40 h-screen transition-transform duration-300 ease-in-out",
          "w-64 bg-card border-r border-border",
          "lg:translate-x-0",
          isOpen ? "translate-x-0" : "-translate-x-full",
        )}>
        <div className="flex flex-col h-full">
          {/* Header */}
          <div className="p-6 border-b border-border">
            <h2 className="text-xl font-bold text-foreground">Super Admin</h2>
            <p className="text-sm text-muted-foreground mt-1">System Management</p>
          </div>

          {/* Navigation */}
          <nav className="flex-1 overflow-y-auto p-4">
            <ul className="space-y-1">
              {navigation.map((item) => {
                const isActive = pathname === item.href;
                const Icon = item.icon;

                return (
                  <li key={item.name}>
                    <Link
                      href={item.href}
                      onClick={() => setIsOpen(false)}
                      className={cn(
                        "flex items-center gap-3 px-3 py-2.5 rounded-lg transition-colors",
                        "hover:bg-accent hover:text-accent-foreground",
                        isActive ? "bg-primary text-primary-foreground hover:bg-primary/90" : "text-foreground",
                      )}>
                      <Icon className="h-5 w-5 flex-shrink-0" />
                      <div className="flex-1 min-w-0">
                        <p className="text-sm font-medium truncate">{item.name}</p>
                        <p className="text-xs text-muted-foreground truncate mt-0.5">{item.description}</p>
                      </div>
                    </Link>
                  </li>
                );
              })}
            </ul>
          </nav>

          {/* Footer */}
          <div className="p-4 border-t border-border space-y-3">
            {/* User Avatar for Desktop */}
            <div className="hidden lg:flex items-center gap-3">
              <div className="w-10 h-10 flex items-center justify-center">
                {!isLoaded ? (
                  <div className="animate-pulse rounded-full bg-gray-300 w-10 h-10" />
                ) : (
                  <UserButton appearance={{ elements: { avatarBox: "w-10 h-10" } }} />
                )}
              </div>
              <div className="flex-1 min-w-0">
                <p className="text-sm font-medium text-foreground truncate">Account</p>
                <p className="text-xs text-muted-foreground truncate">Manage profile</p>
              </div>
            </div>
            <Link
              href="/select-region"
              className="flex items-center gap-2 text-sm text-muted-foreground hover:text-foreground transition-colors">
              <span>← Back to Regional Dashboard</span>
            </Link>
          </div>
        </div>
      </aside>
    </>
  );
}
