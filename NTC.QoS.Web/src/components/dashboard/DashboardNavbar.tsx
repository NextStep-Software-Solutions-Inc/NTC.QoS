"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { useParams, usePathname, useRouter } from "next/navigation";
import {
  FileText,
  Import,
  SatelliteDish,
  Settings,
  Home,
  Map,
  MoreHorizontal,
  MapPin,
  Loader2,
  Shield,
  ShieldCheck,
  HardHat,
  UserCheck,
  Globe,
  User as UserIcon,
} from "lucide-react";
import { cn } from "@/lib/utils";
import Each from "../Each";
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger, DropdownMenuSeparator, DropdownMenuLabel } from "@/components/ui/dropdown-menu";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { useAuth, UserButton, useUser } from "@clerk/nextjs";
import { usePermissions } from "@/providers/PermissionsContext";
import { getUserRegionsRequest } from "@/components/features/user-region/handlers/getUserRegionsRequest";
import { getAuthUserRequest } from "@/components/features/users/handlers/getAuthUserRequest";
import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";

const useUserRegions = createQueryHookFromTemplate(getUserRegionsRequest);
const useAuthUser = createQueryHookFromTemplate(getAuthUserRequest);

const RegionalNavbar = () => {
  const pathname = usePathname();
  const { region } = useParams<{ region: string }>();
  const router = useRouter();
  const { userId } = useAuth();
  const { user, isLoaded } = useUser();
  const { currentUser } = usePermissions();
  const [switchingRegionId, setSwitchingRegionId] = useState<string | null>(null);
  
  const { data: authUser } = useAuthUser({
    pathParams: { id: userId ?? "" },
    queryParams: {},
  });

  const isSuperAdmin = authUser?.data?.role?.key === "super_admin" || !!currentUser?.isSuperAdmin;

  const { data: userRegions } = useUserRegions({
    pathParams: {},
    queryParams: { pageSize: 100, pageNumber: 1, authUserIds: userId ? [userId] : [], statuses: ["Active"] },
  });

  const accessibleRegions = userRegions?.data ?? [];
  const currentRegion = accessibleRegions.find((ur) => ur.region.id === region);

  const activeUser = currentUser || authUser?.data;
  const activeRole = currentRegion?.role ?? activeUser?.role;
  const roleKey = activeRole?.key?.toLowerCase() ?? (isSuperAdmin ? "super_admin" : "");
  const roleName = activeRole?.name || (isSuperAdmin ? "SuperAdmin" : activeUser?.role?.name);

  const displayName =
    user?.fullName ||
    (activeUser?.firstName
      ? `${activeUser.firstName} ${activeUser.lastName ?? ""}`.trim()
      : user?.primaryEmailAddress?.emailAddress?.split("@")[0] || "");

  const getRoleConfig = (key: string, name?: string | null) => {
    const lowerKey = key.toLowerCase();
    const lowerName = (name ?? "").toLowerCase();

    if (isSuperAdmin || lowerKey === "super_admin" || lowerName.includes("super admin")) {
      return {
        icon: <ShieldCheck className="h-3 w-3 text-purple-600 dark:text-purple-400 shrink-0" />,
        className:
          "bg-purple-50 text-purple-700 border-purple-200 dark:bg-purple-950/60 dark:text-purple-300 dark:border-purple-800",
      };
    }
    if (lowerKey.includes("admin") || lowerName.includes("admin")) {
      return {
        icon: <Shield className="h-3 w-3 text-blue-600 dark:text-blue-400 shrink-0" />,
        className:
          "bg-blue-50 text-blue-700 border-blue-200 dark:bg-blue-950/60 dark:text-blue-300 dark:border-blue-800",
      };
    }
    if (lowerKey.includes("engineer") || lowerName.includes("engineer") || lowerName.includes("field")) {
      return {
        icon: <HardHat className="h-3 w-3 text-amber-600 dark:text-amber-400 shrink-0" />,
        className:
          "bg-amber-50 text-amber-700 border-amber-200 dark:bg-amber-950/60 dark:text-amber-300 dark:border-amber-800",
      };
    }
    if (lowerKey.includes("staff") || lowerName.includes("staff")) {
      return {
        icon: <UserCheck className="h-3 w-3 text-emerald-600 dark:text-emerald-400 shrink-0" />,
        className:
          "bg-emerald-50 text-emerald-700 border-emerald-200 dark:bg-emerald-950/60 dark:text-emerald-300 dark:border-emerald-800",
      };
    }
    if (lowerKey.includes("national") || lowerName.includes("national")) {
      return {
        icon: <Globe className="h-3 w-3 text-cyan-600 dark:text-cyan-400 shrink-0" />,
        className:
          "bg-cyan-50 text-cyan-700 border-cyan-200 dark:bg-cyan-950/60 dark:text-cyan-300 dark:border-cyan-800",
      };
    }
    return {
      icon: <UserIcon className="h-3 w-3 text-slate-500 shrink-0" />,
      className:
        "bg-slate-50 text-slate-700 border-slate-200 dark:bg-slate-900 dark:text-slate-300 dark:border-slate-800",
    };
  };

  const roleConfig = getRoleConfig(roleKey, roleName);

  // Reset switching state when region changes (navigation completed)
  useEffect(() => {
    setSwitchingRegionId(null);
  }, [region]);

  const handleRegionSwitch = (regionId: string) => {
    if (regionId === region) return; // Don't switch to the same region

    setSwitchingRegionId(regionId);

    // Navigate to the new region
    router.push(`/${regionId}/broadband`);

    // Reset loading state after a reasonable timeout (in case navigation fails)
    setTimeout(() => {
      setSwitchingRegionId(null);
    }, 5000); // 5 seconds should be enough for navigation
  };

  const links = [
    { href: "/#", label: "Home", icon: Home },
    { href: `/${region}/map`, label: "Map", icon: Map },
    { href: `/${region}/broadband`, label: "Broadband", icon: SatelliteDish },
    { href: `/${region}/imports?type=NAP`, label: "Imports", icon: Import },
    { href: `/${region}/reports`, label: "Reports", icon: FileText },
    { href: `/${region}/settings`, label: "Settings", icon: Settings },
  ];

  const getIsActive = (href: string) => {
    if (href === "/#") {
      return pathname === "/";
    }
    if (href === `/${region}`) {
      return pathname === `/${region}` || pathname === `/${region}/`;
    }

    // For imports, check if pathname starts with the base imports path
    if (href.includes("/imports")) {
      return pathname.includes("/imports");
    }

    // For reports, check if pathname contains /reports
    if (href.includes("/reports")) {
      return pathname.includes("/reports");
    }

    return (
      pathname.startsWith(href) &&
      (href === pathname || pathname.startsWith(href + "/") || pathname.startsWith(href + "?"))
    );
  };

  // Show first 3 items on mobile, all on desktop
  const visibleItemsOnMobile = 3;

  return (
    <header className="flex h-16 shrink-0 items-center justify-between gap-2 px-4 sm:px-6 bg-white border-b border-gray-200">
      {/* Region Switcher */}
      <div className="flex items-center gap-2 sm:gap-4 shrink-0">
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Button
              variant="outline"
              size="sm"
              disabled={!!switchingRegionId}
              className="flex items-center gap-2 px-3 py-2 h-auto border-blue-200 hover:bg-blue-50 hover:border-blue-300 transition-colors disabled:opacity-50">
              {!!switchingRegionId ? (
                <Loader2 className="h-4 w-4 animate-spin text-blue-600" />
              ) : (
                <MapPin className="h-4 w-4 text-blue-600" />
              )}
              <span className="hidden sm:inline font-medium text-sm">
                {!!switchingRegionId ? "Switching..." : currentRegion?.region.name || "Select Region"}
              </span>
              <span className="sm:hidden font-medium text-sm">
                {!!switchingRegionId ? "..." : currentRegion?.region.code || "Region"}
              </span>
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="start" className="w-56">
            <DropdownMenuLabel className="text-xs text-muted-foreground">Switch Region</DropdownMenuLabel>
            <DropdownMenuSeparator />
            {accessibleRegions.map((userRegion) => {
              const isSwitchingToThisRegion = switchingRegionId === userRegion.region.id;
              const isCurrentRegion = userRegion.region.id === region;

              return (
                <DropdownMenuItem
                  key={userRegion.region.id}
                  onClick={() => handleRegionSwitch(userRegion.region.id)}
                  disabled={!!switchingRegionId}
                  className="flex items-center gap-2 cursor-pointer disabled:opacity-50">
                  {isSwitchingToThisRegion ? (
                    <Loader2 className="h-4 w-4 animate-spin text-blue-600" />
                  ) : (
                    <MapPin className={cn("h-4 w-4", isCurrentRegion ? "text-blue-600" : "text-gray-400")} />
                  )}
                  <div className="flex flex-col">
                    <span className={cn("font-medium text-sm", isCurrentRegion && "text-blue-600")}>
                      {isSwitchingToThisRegion ? "Switching..." : userRegion.region.name}
                    </span>
                    <span className="text-xs text-muted-foreground">{userRegion.region.code}</span>
                  </div>
                </DropdownMenuItem>
              );
            })}
            <DropdownMenuSeparator />
            <DropdownMenuItem asChild disabled={!!switchingRegionId}>
              <Link
                href="/select-region"
                className="flex items-center gap-2 cursor-pointer text-blue-600 disabled:opacity-50">
                <MapPin className="h-4 w-4" />
                <span className="font-medium">Change Region</span>
              </Link>
            </DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>
      </div>

      <div className="flex items-center gap-2 flex-1 overflow-x-auto scrollbar-hide">
        <ul className="flex items-center gap-2 sm:gap-4 lg:gap-6 text-xs sm:text-sm text-gray-500 min-w-max">
          {/* Visible items on all screens */}
          <Each of={links.slice(0, visibleItemsOnMobile)} keyExtractor={(link) => link.href}>
            {({ href, label, icon: Icon }) => (
              <li>
                <Link
                  href={href}
                  className={cn(
                    "flex items-center gap-1 sm:gap-2 px-2 sm:px-3 lg:px-4 py-2 rounded-lg transition-colors hover:bg-gray-100 hover:text-gray-900 whitespace-nowrap",
                    {
                      "font-semibold text-blue-600 bg-blue-50": getIsActive(href),
                      "text-gray-600": !getIsActive(href),
                    },
                  )}>
                  {Icon && <Icon className="w-3 h-3 sm:w-4 sm:h-4" />}
                  <span className="hidden sm:inline">{label}</span>
                  <span className="sm:hidden">{label.split(" ")[0]}</span>
                </Link>
              </li>
            )}
          </Each>

          {/* Additional items on desktop */}
          <Each of={links.slice(visibleItemsOnMobile)} keyExtractor={(link) => link.href}>
            {({ href, label, icon: Icon }) => (
              <li className="hidden lg:block">
                <Link
                  href={href}
                  className={cn(
                    "flex items-center gap-2 px-4 py-2 rounded-lg transition-colors hover:bg-gray-100 hover:text-gray-900 whitespace-nowrap",
                    {
                      "font-semibold text-blue-600 bg-blue-50": getIsActive(href),
                      "text-gray-600": !getIsActive(href),
                    },
                  )}>
                  {Icon && <Icon size={16} />}
                  {label}
                </Link>
              </li>
            )}
          </Each>

          {/* More dropdown for mobile/tablet */}
          {links.length > visibleItemsOnMobile && (
            <li className="lg:hidden">
              <DropdownMenu>
                <DropdownMenuTrigger asChild>
                  <Button variant="ghost" size="sm" className="flex items-center gap-1 px-2 sm:px-3 py-2 h-auto">
                    <MoreHorizontal className="h-4 w-4" />
                    <span className="hidden sm:inline text-xs">More</span>
                  </Button>
                </DropdownMenuTrigger>
                <DropdownMenuContent align="end">
                  {links.slice(visibleItemsOnMobile).map((link) => (
                    <DropdownMenuItem key={link.href} asChild>
                      <Link href={link.href} className="flex items-center gap-2 cursor-pointer">
                        {link.icon && <link.icon size={16} />}
                        {link.label}
                      </Link>
                    </DropdownMenuItem>
                  ))}
                </DropdownMenuContent>
              </DropdownMenu>
            </li>
          )}
        </ul>
      </div>

      {/* User Identity & Role Tag */}
      <div className="flex items-center gap-2 sm:gap-3 shrink-0">
        {isLoaded && (displayName || roleName) && (
          <div
            className="flex flex-col items-end text-right min-w-0"
            title={`${displayName}${roleName ? ` • ${roleName}` : ""}`}>
            {displayName && (
              <span className="hidden sm:inline text-xs font-semibold text-gray-800 dark:text-gray-200 truncate max-w-[140px] leading-tight">
                {displayName}
              </span>
            )}
            {roleName && (
              <Badge
                variant="outline"
                className={cn(
                  "text-[10px] px-1.5 py-0 h-4 font-medium flex items-center gap-1 shrink-0 sm:mt-0.5",
                  roleConfig.className
                )}>
                {roleConfig.icon}
                <span className="truncate max-w-[95px] sm:max-w-[130px]">{roleName}</span>
              </Badge>
            )}
          </div>
        )}

        {/* User Avatar */}
        <div className="w-8 h-8 flex items-center justify-center">
          {!isLoaded ? (
            <div className="animate-pulse rounded-full bg-gray-300 w-8 h-8" />
          ) : (
            <UserButton appearance={{ elements: { avatarBox: "w-8 h-8" } }}>
              <UserButton.MenuItems>
                {isSuperAdmin && (
                  <UserButton.Link
                    label="Super Admin Console"
                    labelIcon={<span className="text-xs">⚙️</span>}
                    href="/super-admin"
                  />
                )}
              </UserButton.MenuItems>
            </UserButton>
          )}
        </div>
      </div>
    </header>
  );
};

export default RegionalNavbar;
