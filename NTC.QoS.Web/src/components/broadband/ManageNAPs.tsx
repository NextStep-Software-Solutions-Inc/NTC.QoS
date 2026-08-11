"use client";

import { getNAPsRequest } from "@/components/features/NAP/handlers/getNAPsRequest";
import NAPTable from "@/components/features/NAP/tables/NAPTable";
import { type NAPDetails } from "@/components/features/NAP/types/NAP";
import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";

const useGetNAPs = createQueryHookFromTemplate(getNAPsRequest);

interface ManageNAPsProps {
  regionId: string;
}

export default function ManageNAPs({ regionId }: ManageNAPsProps) {
  const {
    data: napsResponse,
    isLoading,
    error,
  } = useGetNAPs({ pathParams: {}, queryParams: { pageSize: 50, pageNumber: 1, regionIds: [regionId] } });

  if (isLoading) {
    return (
      <div className="container mx-auto py-6">
        <div className="mb-6">
          <h1 className="text-3xl font-bold">Network Access Points</h1>
          <p className="text-muted-foreground">Loading NAP data...</p>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="container mx-auto py-6">
        <div className="mb-6">
          <h1 className="text-3xl font-bold">Network Access Points</h1>
          <p className="text-red-600">Error loading NAP data. Please try again.</p>
        </div>
      </div>
    );
  }

  // Transform NAP to NAPDetails
  const napDetails: NAPDetails[] = (napsResponse?.data || []).map((nap) => ({
    id: nap.id,
    lat: nap.lat,
    lng: nap.lng,
    ISP: nap.ISP || "Unknown",
    ISPColor: nap.ISPColor || "#6b7280",
    dist: nap.dist || 0,
  }));

  return (
    <div className="container mx-auto py-6">
      <div className="mb-6">
        <h1 className="text-3xl font-bold">Network Access Points</h1>
        <p className="text-muted-foreground">Manage and monitor Network Access Points across the region.</p>
      </div>

      <NAPTable data={napDetails} totalCount={napsResponse?.totalRows || 0} regionId={regionId} readonly={false} />
    </div>
  );
}
