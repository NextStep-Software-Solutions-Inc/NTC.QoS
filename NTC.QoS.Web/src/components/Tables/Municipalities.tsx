"use client";

import React, { useEffect, useMemo } from "react";
import { useParams, useSearchParams } from "next/navigation";

import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";
import { useDebounceContext } from "@/hooks/useDebounceContext";
import DeleteMunicipalsDialog from "../features/municipality/dialog/DeleteMunicipalsDialog";
import { getMunicipalitiesRequest } from "../features/municipality/handlers/getMunicipalitiesRequest";
import GenericList from "./GenericList";

const useMunicipalities = createQueryHookFromTemplate(getMunicipalitiesRequest);

export default function Municipalities() {
  const searcParams = useSearchParams();
  const municipalIds = searcParams.getAll("municipalId");
  const { search, queryDebounce, setQueryDebounce } = useDebounceContext();
  const { province } = useParams<{ province: string }>();
  const [activeMunicipalities, setActiveMunicipalities] = React.useState<Set<string>>(new Set([...municipalIds]));
  const [selectedMunicipalities, setSelectedMunicipalities] = React.useState<Set<string>>(new Set());
  const { data: municipalities, isLoading } = useMunicipalities({
    pathParams: {},
    queryParams: { pageSize: 100, pageNumber: 1, provinceId: province },
  });

  const mappedMunicipalities = useMemo(() => {
    return (municipalities?.data ?? [])
      .map((municipal) => ({
        id: municipal.id,
        name: municipal.name,
        provinceName: municipal.province?.name ?? "N/A",
        href: `${province}/${municipal.id}`,
      }))
      .sort((a, b) => a.name.localeCompare(b.name))
      .filter((municipal) => municipal.name.toLowerCase().includes(queryDebounce.toLowerCase()));
  }, [municipalities, queryDebounce, province]);

  const handleDeleteMunicipalities = () => {
    // Logic to handle deletion of selected municipalities
  };

  const handleClickMunicipality = (id: string) => {
    setActiveMunicipalities(new Set([id]));
    // push municipal id to the query params
    const searchParams = new URLSearchParams(window.location.search);
    searchParams.set("municipalId", id);
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
      items={mappedMunicipalities}
      activeItemIds={activeMunicipalities}
      getSelectedItemsIds={setSelectedMunicipalities}
      activeClassName="border-blue-500 bg-blue-50 text-blue-600"
      selectedClassName="border-red-600 bg-red-50 text-red-600"
      searchPlaceholder="Search Municipalities"
      onClick={handleClickMunicipality}
      // addComponent={<AddMunicipalDialog provinceId={province} />}
      deleteComponent={
        <DeleteMunicipalsDialog count={selectedMunicipalities.size} onDelete={handleDeleteMunicipalities} />
      }
      isLoading={isLoading}
    />
  );
}
