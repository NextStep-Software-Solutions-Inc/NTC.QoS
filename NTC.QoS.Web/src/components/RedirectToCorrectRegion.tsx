"use client";

import { useEffect } from "react";
import { useRouter, useSearchParams } from "next/navigation";

import { getRegionsRequest } from "@/components/features/region/handlers/getRegionsRequest";
import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";

const useRegions = createQueryHookFromTemplate(getRegionsRequest);

interface RedirectToCorrectRegionProps {
  redirectPath?: string;
}
export default function RedirectToCorrectRegion({ redirectPath }: RedirectToCorrectRegionProps) {
  const searchParams = useSearchParams();
  const { data: regions, isLoading } = useRegions({ pathParams: {}, queryParams: { pageSize: 1, pageNumber: 1 } });
  const router = useRouter();
  useEffect(() => {
    if (isLoading) return; // Wait until the data is loaded
    // get the first region from the data if available
    if (regions && regions.data.length > 0) {
      const distPath = `${redirectPath}?${searchParams.toString()}`;
      router.replace(distPath);
    } else {
      console.error("No regions available");
    }
  }, [isLoading, regions, router, redirectPath, searchParams]);
  return null;
}
