"use client";

import { useCallback, useEffect, useMemo, useState, useTransition } from "react";
import { useRouter } from "next/navigation";
import { useAuth } from "@clerk/nextjs";
import { ChevronRight, Globe2, Grid, Info, List, MapPin, Sparkles, X } from "lucide-react";
import { toast } from "sonner";

import { getRegionsRequest } from "@/components/features/region/handlers/getRegionsRequest";
import { createUserRegionRequest } from "@/components/features/user-region/handlers/createUserRegionRequest";
import { getUserRegionsRequest } from "@/components/features/user-region/handlers/getUserRegionsRequest";
import { getAuthUserRequest } from "@/components/features/users/handlers/getAuthUserRequest";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import WithPlaceholder from "@/components/WithPlaceholder";
import { createMutationHookFromTemplate } from "@/hooks/createMutationHookFromTemplate";
import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";
import { extractErrorMessage } from "@/lib/extract-error-message";
import { formatNumber } from "@/lib/format-number";
import { usePermissions } from "@/providers/PermissionsContext";

const SUPER_ADMIN_ROLE_KEY = "super_admin";

const useRegions = createQueryHookFromTemplate(getRegionsRequest);
const useCreateUserRegion = createMutationHookFromTemplate(createUserRegionRequest);
const useUserRegions = createQueryHookFromTemplate(getUserRegionsRequest);
const useAuthUser = createQueryHookFromTemplate(getAuthUserRequest);

export default function RegionList() {
  const router = useRouter();
  const [isPending, startTransition] = useTransition();
  const [selectedRegionId, setSelectedRegionId] = useState<string | null>(null);
  const { userId } = useAuth();
  const { hasRegionAccess, isSuperAdmin, isPlatformUser } = usePermissions();
  const { data: authUser } = useAuthUser({ pathParams: { id: userId! }, queryParams: {} });
  const { data: regions, isLoading } = useRegions({ pathParams: {}, queryParams: { pageNumber: 1, pageSize: 100 } });
  const { data: userRegions, refetch: refetchUserRegions } = useUserRegions({
    pathParams: {},
    queryParams: { pageNumber: 1, pageSize: 100, authUserIds: userId ? [userId] : [], statuses: ["Active"] },
  });
  const { mutateAsync: createUserRegion } = useCreateUserRegion();

  useEffect(() => {
    if (authUser?.data?.id) {
      refetchUserRegions();
    }
  }, [authUser?.data?.id, refetchUserRegions]);

  const userAccessibleRegionIds = useMemo(
    () => new Set(userRegions?.data.map((ur) => ur.region.id) ?? []),
    [userRegions],
  );

  const handleRequestAccess = useCallback(
    (regionId: string) => {
      if (!authUser?.data.id) {
        toast.error("You must be logged in to request access to a region.");
        return;
      }

      toast.promise(
        createUserRegion({ pathParams: {}, payload: { userId: authUser.data.id, regionId, status: "Pending" } }),
        {
          loading: "Submitting your access request...",
          success: (res) =>
            res.message ||
            "Your access request has been submitted successfully. Please wait for approval from the administrator.",
          error: (err) =>
            extractErrorMessage(err) ||
            "An error occurred while submitting your access request. Please try again later.",
        },
      );
    },
    [createUserRegion, authUser],
  );

  const [view, setView] = useState<"list" | "grid">("grid");
  const [bannerDismissed, setBannerDismissed] = useState(false);

  const hasNoRegions =
    !isLoading &&
    !isSuperAdmin &&
    !isPlatformUser &&
    (authUser?.data as any)?.role?.key !== SUPER_ADMIN_ROLE_KEY &&
    userAccessibleRegionIds.size === 0;

  const handleRegionClick = useCallback(
    (regionId: string, e: React.MouseEvent) => {
      e.preventDefault();
      setSelectedRegionId(regionId);
      startTransition(() => {
        router.push(`/${regionId}/broadband`);
      });
    },
    [router],
  );

  // --- Skeleton placeholders ---
  const listSkeleton = (
    <ul className="w-full space-y-4 flex-1 min-h-0 overflow-y-auto">
      {Array.from({ length: 6 }).map((_, i) => (
        <li
          key={i}
          className="animate-in fade-in slide-in-from-bottom-4"
          style={{ animationDelay: `${i * 100}ms`, animationDuration: "500ms" }}>
          <div className="w-full bg-card/50 border border-border/50 rounded-2xl px-6 py-5 shadow-lg backdrop-blur-sm flex flex-col gap-3">
            <div className="flex items-center gap-3 mb-2">
              <Skeleton className="w-10 h-10 rounded-full" />
              <Skeleton className="h-6 w-40" />
              <Skeleton className="ml-auto h-5 w-16 rounded-full" />
            </div>
            <Skeleton className="h-4 w-3/4" />
            <div className="flex items-center justify-between mt-2">
              <Skeleton className="h-4 w-24" />
              <Skeleton className="h-9 w-32 rounded-full" />
            </div>
          </div>
        </li>
      ))}
    </ul>
  );

  const gridSkeleton = (
    <div className="w-full flex-1 min-h-0 grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4 sm:gap-6 overflow-y-auto pr-2 pb-4 auto-rows-min">
      {Array.from({ length: 8 }).map((_, i) => (
        <div
          key={i}
          className="bg-card/50 backdrop-blur-sm rounded-3xl shadow-xl flex flex-col gap-4 p-6 sm:p-8 min-h-[280px] border border-border/50 animate-in fade-in slide-in-from-bottom-4"
          style={{ animationDelay: `${i * 100}ms`, animationDuration: "500ms" }}>
          <div className="flex flex-col items-center gap-3 border-b border-muted/50 pb-4">
            <Skeleton className="w-12 h-12 rounded-full mb-2" />
            <Skeleton className="h-6 w-32" />
            <Skeleton className="h-4 w-16 rounded-full mt-1" />
          </div>
          <div className="flex-1 flex flex-col items-center justify-center">
            <Skeleton className="h-4 w-40 mb-2" />
            <Skeleton className="h-4 w-24" />
          </div>
          <Skeleton className="h-10 w-full rounded-full mt-auto" />
        </div>
      ))}
    </div>
  );

  return (
    <div className="flex-1 min-h-0 flex flex-col">
      {hasNoRegions && !bannerDismissed && (
        <div className="mb-4 rounded-2xl border border-blue-500/20 bg-blue-500/10 p-4 sm:p-5 backdrop-blur-md animate-in fade-in slide-in-from-top-2 duration-500 relative flex items-start gap-3.5">
          <div className="p-2 rounded-xl bg-blue-500/15 text-blue-500 shrink-0 mt-0.5">
            <Info className="w-5 h-5" />
          </div>
          <div className="flex-1 min-w-0 pr-6">
            <h3 className="text-sm sm:text-base font-semibold text-foreground">
              Welcome to the NTC QoS Portal
            </h3>
            <p className="text-xs sm:text-sm text-muted-foreground mt-1 leading-relaxed">
              You do not currently have assigned access to any regional dashboard. To get started, select your designated region below and click <span className="font-semibold text-foreground">Request Access</span>, or contact your regional administrator to assign your regional role.
            </p>
          </div>
          <button
            type="button"
            onClick={() => setBannerDismissed(true)}
            aria-label="Dismiss banner"
            className="absolute top-4 right-4 p-1 rounded-lg text-muted-foreground hover:text-foreground hover:bg-muted/50 transition-colors">
            <X className="w-4 h-4" />
          </button>
        </div>
      )}

      {/* View toggle with animation */}
      <div
        className="flex justify-end mb-6 animate-in fade-in slide-in-from-top-4 duration-700"
        style={{ animationDelay: "200ms" }}>
        <div className="flex gap-2 bg-muted/50 p-1 rounded-full backdrop-blur-sm border border-border/50">
          <Button
            variant={view === "list" ? "default" : "ghost"}
            size="sm"
            onClick={() => setView("list")}
            className="rounded-full transition-all duration-300"
            aria-label="List view">
            <List className="w-4 h-4 mr-2" />
            List
          </Button>
          <Button
            variant={view === "grid" ? "default" : "ghost"}
            size="sm"
            onClick={() => setView("grid")}
            className="rounded-full transition-all duration-300"
            aria-label="Grid view">
            <Grid className="w-4 h-4 mr-2" />
            Grid
          </Button>
        </div>
      </div>

      {/* List or Grid wrapped with WithPlaceholder */}
      <WithPlaceholder isLoading={isLoading} placeholder={view === "list" ? listSkeleton : gridSkeleton}>
        {view === "list" ? (
          <ul className="w-full space-y-4 flex-1 min-h-0 overflow-y-auto pr-2">
            {(regions?.data ?? []).map((region, index) => {
              const isSelected = selectedRegionId === region.id;
              const hasAccess =
                isSuperAdmin ||
                isPlatformUser ||
                hasRegionAccess(region.id) ||
                userAccessibleRegionIds.has(region.id) ||
                (authUser?.data as any)?.role?.key === SUPER_ADMIN_ROLE_KEY;
              return (
                <li
                  key={region.id}
                  className="animate-in fade-in slide-in-from-bottom-4"
                  style={{ animationDelay: `${index * 50}ms`, animationDuration: "500ms" }}>
                  <div
                    onClick={(e) => hasAccess && handleRegionClick(region.id, e)}
                    className={`w-full bg-card/80 backdrop-blur-sm border border-border rounded-2xl px-6 py-6 shadow-lg hover:shadow-2xl transition-all duration-300 group cursor-pointer relative overflow-hidden ${
                      hasAccess ? "hover:bg-accent/70 hover:border-blue-500/50 hover:-translate-y-1" : "opacity-75"
                    } ${isSelected ? "ring-2 ring-blue-500 animate-pulse" : ""}`}>
                    {/* Shine effect on hover - Philippine yellow */}
                    <div className="absolute inset-0 bg-gradient-to-r from-transparent via-yellow-500/10 to-transparent -translate-x-full group-hover:translate-x-full transition-transform duration-1000" />

                    <div className="relative z-10 flex flex-col gap-3">
                      <div className="flex items-center gap-4">
                        <div className="p-2 bg-gradient-to-br from-blue-500/10 to-red-500/10 rounded-full group-hover:from-blue-500/20 group-hover:to-red-500/20 transition-colors duration-300">
                          <MapPin className="w-6 h-6 text-red-600" />
                        </div>
                        <div className="flex-1">
                          <div className="flex items-center gap-3 flex-wrap">
                            <span className="font-bold text-lg group-hover:text-blue-600 transition-colors duration-300">
                              {region.name}
                            </span>
                            <span className="text-xs text-muted-foreground font-mono px-3 py-1 rounded-full bg-muted/60 border border-border/50">
                              {region.code}
                            </span>
                          </div>
                        </div>
                        {hasAccess && (
                          <ChevronRight
                            className={`w-5 h-5 text-muted-foreground group-hover:text-blue-600 group-hover:translate-x-1 transition-all duration-300 ${
                              isSelected ? "animate-bounce" : ""
                            }`}
                          />
                        )}
                      </div>

                      <p className="text-sm text-muted-foreground ml-14">{region.description}</p>

                      <div className="flex items-center justify-between mt-2 ml-14">
                        <div className="flex items-center gap-2 text-xs text-muted-foreground">
                          <Sparkles className="w-3 h-3" />
                          <span>{formatNumber(parseFloat(region.area))} sq.km</span>
                        </div>
                        {!hasAccess ? (
                          <Button
                            variant="outline"
                            size="sm"
                            className="font-semibold px-4 py-2 text-sm flex items-center gap-2 rounded-full hover:bg-blue-600 hover:text-white transition-all duration-300 border-blue-600/30"
                            onClick={(e) => {
                              e.stopPropagation();
                              handleRequestAccess(region.id);
                            }}>
                            Request Access
                            <ChevronRight className="w-4 h-4" />
                          </Button>
                        ) : isSelected && isPending ? (
                          <div className="flex items-center gap-2 text-sm text-blue-600">
                            <div className="w-4 h-4 border-2 border-blue-600 border-t-transparent rounded-full animate-spin" />
                            Loading...
                          </div>
                        ) : (
                          <Button
                            variant="default"
                            size="sm"
                            className="font-semibold px-4 py-2 text-sm flex items-center gap-2 rounded-full transition-all duration-300">
                            Explore Region
                            <ChevronRight className="w-4 h-4" />
                          </Button>
                        )}
                      </div>
                    </div>
                  </div>
                </li>
              );
            })}
          </ul>
        ) : (
          <div className="w-full flex-1 min-h-0 grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4 sm:gap-6 overflow-y-auto px-2 pb-4 auto-rows-min">
            {(regions?.data ?? []).map((region, index) => {
              const isSelected = selectedRegionId === region.id;
              const hasAccess =
                isSuperAdmin ||
                isPlatformUser ||
                hasRegionAccess(region.id) ||
                userAccessibleRegionIds.has(region.id) ||
                (authUser?.data as any)?.role?.key === SUPER_ADMIN_ROLE_KEY;
              return (
                <div
                  key={region.id}
                  onClick={(e) => hasAccess && handleRegionClick(region.id, e)}
                  className={`bg-card/80 backdrop-blur-sm rounded-3xl shadow-xl hover:shadow-2xl transition-all duration-500 flex flex-col gap-4 p-6 sm:p-8 min-h-[280px] cursor-pointer border border-border/50 group relative overflow-hidden animate-in fade-in slide-in-from-bottom-4 ${
                    hasAccess
                      ? "hover:bg-accent/70 hover:border-blue-500/50 hover:-translate-y-2 hover:rotate-1"
                      : "opacity-75"
                  } ${isSelected ? "ring-2 ring-blue-500 scale-105" : ""}`}
                  style={{ animationDelay: `${index * 50}ms`, animationDuration: "500ms" }}>
                  {/* Animated background gradient - Philippine colors */}
                  <div className="absolute inset-0 bg-gradient-to-br from-blue-500/5 via-transparent to-red-500/5 opacity-0 group-hover:opacity-100 transition-opacity duration-500" />

                  {/* Shine effect */}
                  <div className="absolute inset-0 bg-gradient-to-r from-transparent via-yellow-500/10 to-transparent -translate-x-full group-hover:translate-x-full transition-transform duration-1000" />

                  <div className="relative z-10 flex flex-col items-center gap-3 border-b border-muted/50 pb-4">
                    <div
                      className={`p-3 bg-gradient-to-br from-blue-500/10 via-red-500/10 to-yellow-500/10 rounded-2xl group-hover:from-blue-500/20 group-hover:via-red-500/20 group-hover:to-yellow-500/20 transition-all duration-500 group-hover:scale-110 group-hover:rotate-12 ${
                        isSelected ? "animate-bounce" : ""
                      }`}>
                      <Globe2 className="w-8 h-8 text-blue-600" />
                    </div>
                    <span className="font-bold text-lg sm:text-xl group-hover:text-blue-600 transition-colors duration-300 text-center">
                      {region.name}
                    </span>
                    <span className="text-xs text-muted-foreground font-mono px-3 py-1.5 rounded-full bg-muted/60 border border-border/50 mt-1">
                      {region.code}
                    </span>
                  </div>

                  <div className="relative z-10 flex-1 flex flex-col items-center justify-center">
                    <p className="text-sm text-muted-foreground text-center mb-3 line-clamp-2">{region.description}</p>
                    <div className="flex items-center gap-2 text-xs text-muted-foreground">
                      <Sparkles className="w-3 h-3" />
                      <span>{formatNumber(parseFloat(region.area))} sq.km</span>
                    </div>
                  </div>

                  <div className="relative z-10 flex justify-center mt-auto">
                    {!hasAccess ? (
                      <Button
                        variant="outline"
                        size="sm"
                        className="w-full font-semibold px-4 py-2 text-sm flex items-center justify-center gap-2 rounded-full hover:bg-blue-600 hover:text-white transition-all duration-300 border-blue-600/30"
                        onClick={(e) => {
                          e.stopPropagation();
                          handleRequestAccess(region.id);
                        }}>
                        Request Access
                        <ChevronRight className="w-4 h-4" />
                      </Button>
                    ) : isSelected && isPending ? (
                      <div className="flex items-center gap-2 text-sm text-blue-600">
                        <div className="w-4 h-4 border-2 border-blue-600 border-t-transparent rounded-full animate-spin" />
                        Loading...
                      </div>
                    ) : (
                      <Button
                        variant="default"
                        size="sm"
                        className="w-full font-semibold px-4 py-2 text-sm flex items-center justify-center gap-2 rounded-full transition-all duration-300">
                        Explore Region
                        <ChevronRight className="w-4 h-4" />
                      </Button>
                    )}
                  </div>
                </div>
              );
            })}
          </div>
        )}
      </WithPlaceholder>
    </div>
  );
}
