"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { useParams, usePathname, useRouter } from "next/navigation";
import { FileText, Import, SatelliteDish, Settings, Home, Map, MoreHorizontal, MapPin, Loader2 } from "lucide-react";
import { cn } from "@/lib/utils";
import Each from "../Each";
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger, DropdownMenuSeparator, DropdownMenuLabel } from "@/components/ui/dropdown-menu";
import { Button } from "@/components/ui/button";
import { useAuth, UserButton, useUser } from "@clerk/nextjs";
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
  const { isLoaded } = useUser();
  const [switchingRegionId, setSwitchingRegionId] = useState<string | null>(null);
  
  const { data: authUser } = useAuthUser({
    pathParams: { id: userId ?? "" },
    queryParams: {},
  });

  const isSuperAdmin = authUser?.data?.role?.key === "super_admin";

  const { data: userRegions } = useUserRegions({
    pathParams: {},
    queryParams: { pageSize: 100, pageNumber: 1, authUserIds: userId ? [userId] : [], statuses: ["Active"] },
  });

  const accessibleRegions = userRegions?.data ?? [];
  const currentRegion = accessibleRegions.find((ur) => ur.region.id === region);

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

      {/* User Avatar */}
      <div className="flex items-center gap-2 shrink-0">
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
