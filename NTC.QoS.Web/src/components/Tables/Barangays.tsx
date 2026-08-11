"use client";

import React, { useEffect, useMemo } from "react";
import { useParams, useSearchParams } from "next/navigation";

import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";
import { useDebounceContext } from "@/hooks/useDebounceContext";
import AddBarangayDialog from "../features/barangay/dialog/AddBarangayDialog";
import DeleteBarangayDialog from "../features/barangay/dialog/DeleteBarangayDialog";
import { getBarangaysRequest } from "../features/barangay/handlers/getBarangaysRequest";
import { getMunicipalityRequest } from "../features/municipality/handlers/getMunicipalityRequest";
import GenericList from "./GenericList";

const useBarangays = createQueryHookFromTemplate(getBarangaysRequest);
const useMunicipality = createQueryHookFromTemplate(getMunicipalityRequest);

export default function Barangays() {
  const searcParams = useSearchParams();
  const barangayIds = searcParams.getAll("barangayId");
  const { search, queryDebounce, setQueryDebounce } = useDebounceContext();
  const [activeItemIds, setActiveItemIds] = React.useState<Set<string>>(new Set([...barangayIds]));
  const [selectedBarangays, setSelectedBarangays] = React.useState<Set<string>>(new Set());
  const { municipal } = useParams<{ municipal: string }>();
  const { data: municipality } = useMunicipality({ pathParams: { id: municipal }, queryParams: {} });

  const { data: Barangays, isLoading } = useBarangays({
    pathParams: {},
    queryParams: { pageSize: 100, pageNumber: 1, municipalityIds: [municipal] },
  });

  const mappedBarangays = useMemo(
    () =>
      (Barangays?.data ?? [])
        .map((barangay) => ({ id: barangay.id, name: barangay.name, href: `${municipal}/${barangay.id}` }))
        .sort((a, b) => a.name.localeCompare(b.name))
        .filter((barangay) => barangay.name.toLowerCase().includes(queryDebounce.toLowerCase())),
    [Barangays, queryDebounce, municipal],
  );

  const handleDeleteBarangays = async () => {
    // Logic to handle deletion of selected barangays
  };

  const handleClickBarangay = (id: string) => {
    setActiveItemIds(new Set([id]));
    const searchParams = new URLSearchParams(window.location.search);
    searchParams.set("barangayId", id);
    window.history.replaceState({}, "", `?${searchParams.toString()}`);
  };

  useEffect(() => {
    const timer = setTimeout(() => {
      setQueryDebounce(search);
    }, 300);
    return () => {
      clearTimeout(timer);
    };
  }, [search, setQueryDebounce]);

  return (
    <GenericList
      items={mappedBarangays}
      activeItemIds={activeItemIds}
      getSelectedItemsIds={setSelectedBarangays}
      activeClassName="border-blue-500 bg-blue-50 text-blue-600"
      selectedClassName="border-red-600 bg-red-50 text-red-600"
      searchPlaceholder="Search Barangays"
      onClick={handleClickBarangay}
      addComponent={<AddBarangayDialog municipality={municipality?.data} />}
      deleteComponent={<DeleteBarangayDialog count={selectedBarangays.size} onDelete={handleDeleteBarangays} />}
      isLoading={isLoading}
    />
  );
}
