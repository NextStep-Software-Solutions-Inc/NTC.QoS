"use client";

import { useState, type ReactNode } from "react";
import { useSearchParams } from "next/navigation";
import { ArrowUpDown, RadioTower, Search } from "lucide-react";

import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";

interface AdminSearchToolbarProps {
  broadbandName?: ReactNode;
  dataLength?: ReactNode;
  placeholder?: string;
  searchValue?: string;
  setSearchValue?: (value: string) => void;
  leftIcon?: ReactNode;
  status?: ReactNode;
  statusLabel?: ReactNode;
  totalData?: ReactNode;
  filterFunction?: ReactNode;
}

const AdminSearchToolbar = ({
  broadbandName,
  dataLength,
  placeholder,
  searchValue,
  setSearchValue,
  leftIcon,
  statusLabel,
  status,
  filterFunction,
}: AdminSearchToolbarProps) => {
  const searchParams = useSearchParams();
  const [isAscending, setIsAscending] = useState(() => {
    const sortParam = searchParams.get("sort");
    if (!sortParam) {
      const params = new URLSearchParams(window.location.search);
      params.set("sort", "asc");
      window.history.replaceState({}, "", `?${params.toString()}`);
      return true;
    }
    return sortParam === "asc";
  });

  const handleSortClick = () => {
    const newDirection = !isAscending;
    setIsAscending(newDirection);
    const params = new URLSearchParams(window.location.search);
    params.set("sort", newDirection ? "asc" : "desc");
    window.history.replaceState({}, "", `?${params.toString()}`);
  };

  return (
    <div className="w-full flex flex-col gap-4 sm:gap-6 mb-4 mt-2">
      {/* Top Row: Search, Filter, Sort */}
      <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4">
        <div className="flex flex-1 items-center gap-2">
          <div className="relative w-full">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
            <Input
              type="text"
              value={searchValue}
              onChange={(e) => setSearchValue?.(e.target.value)}
              placeholder={placeholder ?? "Search Site ID or Coordinates"}
              className="pl-10 focus:outline-none focus:ring-0 focus-visible:ring-0"
            />
          </div>
          {filterFunction}
          <Button variant="outline" className="whitespace-nowrap" onClick={handleSortClick}>
            <ArrowUpDown
              className={`mr-2 h-4 w-4 transition-all ${
                isAscending ? "text-primary rotate-0" : "text-destructive rotate-180"
              }`}
            />
            Sort
          </Button>
        </div>
      </div>

      {/* Bottom Row: Info Section */}
      <div className="pl-5 flex flex-col sm:flex-row items-start sm:items-center w-full">
        <div className="w-1/2">
          <h3 className="text-base font-semibold">{broadbandName ?? "Ex. Cell Site Available"}</h3>
          <p className="text-sm text-muted-foreground">{dataLength}</p>
        </div>

        <div className="flex gap-2 items-center">
          <div className="bg-gray-100 p-2 rounded-full">
            {leftIcon ?? <RadioTower className="h-5 w-5 text-muted-foreground" />}
          </div>
          <div>
            <p className="text-sm font-medium">{statusLabel ?? "No name"}</p>
            <p className="text-sm text-muted-foreground">{status}</p>
          </div>
        </div>
      </div>
    </div>
  );
};

export default AdminSearchToolbar;
