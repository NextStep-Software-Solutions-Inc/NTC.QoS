/**
 * Cell Site Details Component
 *
 * Redesigned sidebar component that displays comprehensive cell site information
 * with modern glassmorphic styling and dynamic data from API (no hardcoded values).
 */

"use client";

import { memo, useMemo } from "react";
import Image from "next/image";
import { useSearchParams } from "next/navigation";
import {
  Activity,
  AlertCircle,
  Antenna,
  Building2,
  CheckCircle,
  ChevronLeft,
  Clock,
  Hash,
  Loader2,
  MapPin,
  Mountain,
  Radio,
  Signal,
  XCircle,
} from "lucide-react";

import { getCellDetailsRequest } from "@/components/features/cell/handlers/getCellDetailsRequest";
import { getCellTechnologiesRequest } from "@/components/features/cell/handlers/getCellTechnologiesRequest";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { ScrollArea } from "@/components/ui/scroll-area";
import { Separator } from "@/components/ui/separator";
import { Skeleton } from "@/components/ui/skeleton";
import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";
import { cn } from "@/lib/utils";

const useGetCellDetails = createQueryHookFromTemplate(getCellDetailsRequest);
const useGetCellTechnologies = createQueryHookFromTemplate(getCellTechnologiesRequest);

interface CellSiteDetailsProps {
  /** Optional className for styling */
  className?: string;
  /** Callback when close button is clicked */
  onClose?: () => void;
}

/**
 * Status badge with appropriate color
 */
const StatusBadge = memo(function StatusBadge({ status }: { status?: string }) {
  const statusLower = status?.toLowerCase() ?? "unknown";

  const config = useMemo(() => {
    if (statusLower === "active" || statusLower === "operational") {
      return { color: "bg-green-100 text-green-700 border-green-200", icon: CheckCircle };
    }
    if (statusLower === "inactive" || statusLower === "offline") {
      return { color: "bg-red-100 text-red-700 border-red-200", icon: XCircle };
    }
    if (statusLower === "pending" || statusLower === "maintenance") {
      return { color: "bg-yellow-100 text-yellow-700 border-yellow-200", icon: Clock };
    }
    return { color: "bg-gray-100 text-gray-700 border-gray-200", icon: AlertCircle };
  }, [statusLower]);

  const Icon = config.icon;

  return (
    <Badge variant="outline" className={cn("gap-1", config.color)}>
      <Icon className="w-3 h-3" />
      {status || "Unknown"}
    </Badge>
  );
});

/**
 * Info row component for consistent styling
 */
const InfoRow = memo(function InfoRow({
  icon: Icon,
  label,
  value,
  isLoading,
}: {
  icon: typeof Radio;
  label: string;
  value?: string | number | null;
  isLoading?: boolean;
}) {
  return (
    <div className="flex items-start gap-3 py-2">
      <div className="shrink-0 w-8 h-8 rounded-lg bg-blue-50 flex items-center justify-center">
        <Icon className="w-4 h-4 text-blue-500" />
      </div>
      <div className="flex-1 min-w-0">
        <p className="text-xs font-medium text-gray-500">{label}</p>
        {isLoading ? (
          <Skeleton className="h-4 w-32 mt-1" />
        ) : (
          <p className="text-sm text-gray-800 break-words">{value ?? "N/A"}</p>
        )}
      </div>
    </div>
  );
});

export const CellSiteDetails = memo(function CellSiteDetails({ className, onClose }: CellSiteDetailsProps) {
  const searchParams = useSearchParams();
  const cellId = searchParams.get("cellId");

  const {
    data: cellResponse,
    isLoading,
    isError,
  } = useGetCellDetails({ pathParams: { id: cellId! }, queryParams: {}, enabled: !!cellId });

  const {
    data: technologiesResponse,
    isLoading: technologiesLoading,
  } = useGetCellTechnologies({ pathParams: { id: cellId! }, queryParams: {}, enabled: !!cellId });

  const cell = cellResponse?.data;
  const technologies = technologiesResponse?.data ?? [];

  // Handle close by removing cellId from URL
  const handleClose = () => {
    const params = new URLSearchParams(window.location.search);
    params.delete("cellId");
    window.history.replaceState({}, "", `${window.location.pathname}?${params.toString()}`);
    onClose?.();
  };

  // Don't render if no cellId
  if (!cellId) return null;

  // Error state
  if (isError) {
    return (
      <div
        className={cn(
          "fixed right-4 top-4 bottom-4 z-50",
          "w-[360px] rounded-xl overflow-hidden",
          "bg-white/95 backdrop-blur-md shadow-2xl",
          "border border-gray-200/50",
          className,
        )}>
        <div className="flex flex-col items-center justify-center h-full p-8 text-center">
          <AlertCircle className="w-12 h-12 text-red-400 mb-4" />
          <h3 className="font-semibold text-gray-800 mb-2">Failed to load cell site</h3>
          <p className="text-sm text-gray-500 mb-4">Unable to fetch details for this cell site.</p>
          <Button variant="outline" onClick={handleClose}>
            Close
          </Button>
        </div>
      </div>
    );
  }

  return (
    <div
      className={cn(
        "fixed right-4 top-20 bottom-4 z-50",
        "w-[360px] rounded-xl overflow-hidden",
        "bg-white/95 backdrop-blur-md shadow-2xl",
        "border border-gray-200/50",
        "flex flex-col",
        className,
      )}>
      {/* Header */}
      <div className="shrink-0 bg-gradient-to-r from-blue-500 to-cyan-500 p-4">
        <div className="flex items-center gap-3">
          <Button variant="ghost" size="sm" onClick={handleClose} className="h-8 w-8 p-0 hover:bg-white/20 text-white">
            <ChevronLeft className="w-5 h-5" />
          </Button>
          <div className="flex-1 min-w-0">
            {isLoading ? (
              <>
                <Skeleton className="h-5 w-32 bg-white/30 mb-1" />
                <Skeleton className="h-3 w-24 bg-white/20" />
              </>
            ) : (
              <>
                <h2 className="font-semibold text-white truncate">{cell?.name || `Cell-${cell?.id}`}</h2>
                <p className="text-xs text-white/80">Cell Site Details</p>
              </>
            )}
          </div>
          {!isLoading && <StatusBadge status={cell?.status} />}
        </div>
      </div>

      {/* Image */}
      <div className="shrink-0 h-[140px] w-full bg-gray-100 relative overflow-hidden">
        {isLoading ? (
          <Skeleton className="h-full w-full" />
        ) : cell?.image ? (
          <Image src={cell.image} alt={cell.name || "Cell Tower"} fill className="object-cover" />
        ) : (
          <div className="flex items-center justify-center h-full bg-gradient-to-br from-gray-100 to-gray-200">
            <Radio className="w-16 h-16 text-gray-300" />
          </div>
        )}
      </div>

      {/* Content */}
      <ScrollArea className="flex-1">
        <div className="p-4 space-y-6">
          {/* Basic Information */}
          <section>
            <h3 className="text-xs font-semibold text-gray-500 uppercase tracking-wider mb-3">Basic Information</h3>
            <div className="space-y-1 divide-y divide-gray-100">
              <InfoRow
                icon={Building2}
                label="Service Provider"
                value={cell?.serviceProvider?.provider?.name}
                isLoading={isLoading}
              />
              <InfoRow icon={MapPin} label="Address" value={cell?.address} isLoading={isLoading} />
              <InfoRow icon={Radio} label="Site Name" value={cell?.name} isLoading={isLoading} />
            </div>
          </section>

          <Separator />

          {/* Location Details */}
          <section>
            <h3 className="text-xs font-semibold text-gray-500 uppercase tracking-wider mb-3">Location Details</h3>
            <div className="space-y-1 divide-y divide-gray-100">
              <InfoRow
                icon={MapPin}
                label="Coordinates"
                value={
                  cell?.latitude && cell?.longitude ? `${cell.latitude.toFixed(6)}, ${cell.longitude.toFixed(6)}` : null
                }
                isLoading={isLoading}
              />
              <InfoRow
                icon={Mountain}
                label="Elevation"
                value={cell?.elevation ? `${cell.elevation} m` : null}
                isLoading={isLoading}
              />
            </div>
          </section>

          <Separator />

          {/* Technical Details */}
          <section>
            <h3 className="text-xs font-semibold text-gray-500 uppercase tracking-wider mb-3">Technical Details</h3>
            <div className="space-y-1 divide-y divide-gray-100">
              <InfoRow icon={Hash} label="Site ID" value={cell?.siteId} isLoading={isLoading} />
              <InfoRow icon={Signal} label="Cell ID (CID)" value={cell?.cid} isLoading={isLoading} />
              <InfoRow icon={Activity} label="Location Area Code (LAC)" value={cell?.lac} isLoading={isLoading} />
              <InfoRow icon={Antenna} label="Tower Type" value={cell?.towerType} isLoading={isLoading} />
            </div>
          </section>

          <Separator />

          {/* Additional Info */}
          <section>
            <h3 className="text-xs font-semibold text-gray-500 uppercase tracking-wider mb-3">
              Additional Information
            </h3>
            <div className="space-y-1 divide-y divide-gray-100">
              <InfoRow icon={Hash} label="Group" value={cell?.group} isLoading={isLoading} />
              {cell?.description && (
                <InfoRow icon={AlertCircle} label="Description" value={cell.description} isLoading={isLoading} />
              )}
            </div>

            {/* Verification Status */}
            {!isLoading && (
              <div className="mt-3 p-3 rounded-lg bg-gray-50">
                <div className="flex items-center gap-2">
                  {cell?.isVerified ? (
                    <>
                      <CheckCircle className="w-4 h-4 text-green-500" />
                      <span className="text-sm text-green-700 font-medium">Verified Cell Site</span>
                    </>
                  ) : (
                    <>
                      <AlertCircle className="w-4 h-4 text-yellow-500" />
                      <span className="text-sm text-yellow-700 font-medium">Unverified Cell Site</span>
                    </>
                  )}
                </div>
              </div>
            )}
          </section>

          <Separator />

          {/* Technologies & Bands */}
          <section>
            <h3 className="text-xs font-semibold text-gray-500 uppercase tracking-wider mb-3">Technologies & Bands</h3>
            {technologiesLoading ? (
              <div className="space-y-2">
                {[1, 2].map((i) => (
                  <Skeleton key={i} className="h-20 rounded-lg" />
                ))}
              </div>
            ) : technologies.length === 0 ? (
              <div className="flex flex-col items-center justify-center py-6 text-center">
                <Signal className="w-8 h-8 text-gray-300 mb-2" />
                <p className="text-xs text-gray-500">No technologies assigned.</p>
              </div>
            ) : (
              <div className="space-y-2">
                {technologies.map((tech) => (
                  <div
                    key={tech.technologyId}
                    className="rounded-lg bg-gradient-to-br from-blue-50 to-indigo-50 border border-blue-100 p-3">
                    <div className="flex items-center gap-2 mb-1">
                      <Signal className="w-4 h-4 text-blue-500 shrink-0" />
                      <span className="text-sm font-semibold text-blue-900">{tech.technologyStandard}</span>
                    </div>
                    {tech.technologyDescription && (
                      <p className="text-xs text-gray-500 mb-2 pl-6">{tech.technologyDescription}</p>
                    )}
                    {tech.bands && tech.bands.length > 0 && (
                      <div className="flex flex-wrap gap-1 pl-6">
                        {tech.bands.map((band) => (
                          <span
                            key={band.bandId}
                            className="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium bg-blue-100 text-blue-800 border border-blue-200">
                            {band.bandName}
                          </span>
                        ))}
                      </div>
                    )}
                  </div>
                ))}
              </div>
            )}
          </section>
        </div>
      </ScrollArea>

      {/* Footer */}
      <div className="shrink-0 border-t border-gray-200/50 p-3 bg-gray-50/50">
        <div className="flex gap-2">
          <Button variant="outline" size="sm" className="flex-1" onClick={handleClose}>
            Close
          </Button>
          {cell && (
            <Button
              size="sm"
              className="flex-1"
              onClick={() => {
                // Navigate to the cell on the map
                const params = new URLSearchParams(window.location.search);
                params.set("fly", `${cell.longitude},${cell.latitude},16`);
                window.history.replaceState({}, "", `${window.location.pathname}?${params.toString()}`);
              }}>
              <MapPin className="w-4 h-4 mr-1" />
              Fly to
            </Button>
          )}
        </div>
      </div>

      {/* Loading overlay */}
      {isLoading && (
        <div className="absolute inset-0 bg-white/50 backdrop-blur-sm flex items-center justify-center">
          <Loader2 className="w-8 h-8 text-blue-500 animate-spin" />
        </div>
      )}
    </div>
  );
});

export default CellSiteDetails;
