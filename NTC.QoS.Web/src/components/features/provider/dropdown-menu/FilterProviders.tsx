"use client";

import * as React from "react";
import { useState } from "react";
import { useSearchParams } from "next/navigation";
import { Filter } from "lucide-react";

import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { type ServiceProvider } from "../../service-provider/types/serviceProvider";

// create interface to add providers name for multi select function in this component
interface FilterProvidersProps {
  serviceProviders: ServiceProvider[];
}

export default function FilterProviders({ serviceProviders }: FilterProvidersProps) {
  const [isOpen, setIsOpen] = useState(false);
  const searchParams = useSearchParams();
  const selectedProviderIds = searchParams.getAll("filterServiceProviderId");

  const handleSelectProvider = (id: string) => {
    // append the selected provider to the URL or handle it as needed with query parameter named `filterServiceProviderId`
    const params = new URLSearchParams(window.location.search);
    const currentIds = params.getAll("filterServiceProviderId");

    if (!currentIds.includes(id)) {
      currentIds.push(id);
    } else {
      currentIds.splice(currentIds.indexOf(id), 1);
    }
    params.delete("filterServiceProviderId");
    currentIds.forEach((value) => {
      params.append(`filterServiceProviderId`, value);
    });

    window.history.replaceState({}, "", `${window.location.pathname}?${params.toString()}`);
  };

  const renderCheckboxItem = (isSelected: boolean, label: string) => (
    <>
      <div className="w-4 h-4 border border-gray-400 rounded-sm flex items-center justify-center">
        <Checkbox className="w-4 h-4 text-green-500" checked={isSelected} />
      </div>
      <span className="line-clamp-1">{label}</span>
    </>
  );

  return (
    <div>
      <DropdownMenu defaultOpen={isOpen} onOpenChange={setIsOpen}>
        <DropdownMenuTrigger asChild>
          <Button variant="outline" onClick={() => setIsOpen(!isOpen)}>
            <Filter />
            Filters
          </Button>
        </DropdownMenuTrigger>
        <DropdownMenuContent className="w-64" align="end" onCloseAutoFocus={(e) => e.preventDefault()}>
          {serviceProviders.length === 0 ? (
            <DropdownMenuItem disabled className="text-gray-500">
              No Providers
            </DropdownMenuItem>
          ) : (
            serviceProviders.map(({ id, provider }) => {
              const isChecked = selectedProviderIds.includes(id);

              return (
                <DropdownMenuItem
                  key={id}
                  className="flex items-center gap-3 cursor-pointer px-3 py-2"
                  onSelect={(e) => {
                    e.preventDefault();
                    handleSelectProvider(id);
                  }}>
                  {renderCheckboxItem(isChecked, provider.name)}
                </DropdownMenuItem>
              );
            })
          )}
        </DropdownMenuContent>
      </DropdownMenu>
    </div>
  );
}
