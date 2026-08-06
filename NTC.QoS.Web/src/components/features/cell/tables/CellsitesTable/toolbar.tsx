"use client";

import React from "react";
import { type Table } from "@tanstack/react-table";
import { CheckCircle2, ChevronDown, Filter, Search, Settings2, Trash, X, XCircle } from "lucide-react";

import { type Cell } from "@/components/features/cell/types/Cell";
import { Button } from "@/components/ui/button";
import {
  DropdownMenu,
  DropdownMenuCheckboxItem,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { Input } from "@/components/ui/input";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";

interface ToolbarProps {
  table: Table<Cell>;
  // Filter states
  statusFilter: string;
  providerFilter: string;
  provinceFilter: string;
  municipalityFilter: string;
  barangayFilter: string;
  searchTerm: string;
  // Filter setters
  onStatusFilterChange: (value: string) => void;
  onProviderFilterChange: (value: string) => void;
  onProvinceFilterChange: (value: string) => void;
  onMunicipalityFilterChange: (value: string) => void;
  onBarangayFilterChange: (value: string) => void;
  onSearchChange: (value: string) => void;
  // Filter options
  providers: Array<{ id: string; name: string }>;
  provinces: Array<{ id: string; name: string }>;
  municipalities: Array<{ id: string; name: string }>;
  barangays: Array<{ id: string; name: string }>;
  // Batch actions
  onDeleteSelected: (ids: string[]) => void;
  onUpdateStatusSelected: (ids: string[], status: string) => void;
}

function getSelectedRows(table: Table<Cell>) {
  return table.getSelectedRowModel().rows;
}

export default function Toolbar({
  table,
  statusFilter,
  providerFilter,
  provinceFilter,
  municipalityFilter,
  barangayFilter,
  searchTerm,
  onStatusFilterChange,
  onProviderFilterChange,
  onProvinceFilterChange,
  onMunicipalityFilterChange,
  onBarangayFilterChange,
  onSearchChange,
  providers,
  provinces,
  municipalities,
  barangays,
  onDeleteSelected,
  onUpdateStatusSelected,
}: ToolbarProps) {
  const readonly = table.options.meta?.readonly;
  const selectedRows = getSelectedRows(table);
  const selectedIds = selectedRows.map((row) => row.original.id);

  const hasActiveFilters =
    statusFilter !== "all" ||
    providerFilter !== "all" ||
    provinceFilter !== "all" ||
    municipalityFilter !== "all" ||
    barangayFilter !== "all" ||
    searchTerm !== "";

  const clearAllFilters = () => {
    onStatusFilterChange("all");
    onProviderFilterChange("all");
    onProvinceFilterChange("all");
    onMunicipalityFilterChange("all");
    onBarangayFilterChange("all");
    onSearchChange("");
  };

  return (
    <div className="space-y-3">
      {/* Top Row: Search and Actions */}
      <div className="flex items-center gap-2">
        {/* Search */}
        <div className="relative flex-1 max-w-md">
          <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 text-muted-foreground h-4 w-4" />
          <Input
            placeholder="Search cells by provider, coordinates, or site ID..."
            value={searchTerm}
            onChange={(event) => onSearchChange(event.target.value)}
            className="pl-10 h-10"
          />
          {searchTerm && (
            <Button
              variant="ghost"
              size="sm"
              className="absolute right-1 top-1/2 transform -translate-y-1/2 h-7 w-7 p-0"
              onClick={() => onSearchChange("")}>
              <X className="h-4 w-4" />
            </Button>
          )}
        </div>

        {/* Column Visibility */}
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Button variant="outline" size="sm" className="h-10">
              <Settings2 className="h-4 w-4 mr-2" />
              Columns
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end" className="w-48">
            <DropdownMenuLabel>Toggle columns</DropdownMenuLabel>
            <DropdownMenuSeparator />
            {table
              .getAllColumns()
              .filter((column) => column.getCanHide())
              .map((column) => {
                return (
                  <DropdownMenuCheckboxItem
                    key={column.id}
                    className="capitalize"
                    checked={column.getIsVisible()}
                    onCheckedChange={(value) => column.toggleVisibility(!!value)}>
                    {column.id}
                  </DropdownMenuCheckboxItem>
                );
              })}
          </DropdownMenuContent>
        </DropdownMenu>

        {/* Clear Filters */}
        {hasActiveFilters && (
          <Button variant="outline" size="sm" className="h-10" onClick={clearAllFilters}>
            <X className="h-4 w-4 mr-2" />
            Clear
          </Button>
        )}
      </div>

      {/* Bottom Row: Filters or Batch Actions */}
      {selectedRows.length > 0 && !readonly ? (
        // Batch Actions Bar
        <div className="flex items-center gap-2 p-3 bg-blue-50 dark:bg-blue-950/20 rounded-lg border border-blue-200 dark:border-blue-800">
          <div className="flex items-center gap-2">
            <div className="h-7 w-7 rounded-full bg-blue-500 text-white flex items-center justify-center text-sm font-bold">
              {selectedRows.length}
            </div>
            <span className="text-sm font-medium">selected</span>
          </div>
          <div className="flex-1" />

          {/* Update Status */}
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="outline" size="sm" className="h-9">
                <CheckCircle2 className="h-4 w-4 mr-2" />
                Status
                <ChevronDown className="h-4 w-4 ml-2" />
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent>
              <DropdownMenuLabel>Update status</DropdownMenuLabel>
              <DropdownMenuSeparator />
              <DropdownMenuItem
                onClick={() => {
                  onUpdateStatusSelected(selectedIds, "active");
                  table.resetRowSelection();
                }}>
                <CheckCircle2 className="mr-2 h-4 w-4 text-emerald-500" />
                Active
              </DropdownMenuItem>
              <DropdownMenuItem
                onClick={() => {
                  onUpdateStatusSelected(selectedIds, "inactive");
                  table.resetRowSelection();
                }}>
                <XCircle className="mr-2 h-4 w-4 text-slate-500" />
                Inactive
              </DropdownMenuItem>
              <DropdownMenuItem
                onClick={() => {
                  onUpdateStatusSelected(selectedIds, "maintenance");
                  table.resetRowSelection();
                }}>
                <XCircle className="mr-2 h-4 w-4 text-amber-500" />
                Maintenance
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>

          {/* Delete */}
          <Button
            variant="destructive"
            size="sm"
            className="h-9"
            onClick={() => {
              onDeleteSelected(selectedIds);
              table.resetRowSelection();
            }}>
            <Trash className="h-4 w-4 mr-2" />
            Delete
          </Button>
        </div>
      ) : (
        // Filters Bar
        <div className="flex flex-wrap items-center gap-2">
          <div className="flex items-center gap-2 text-sm text-muted-foreground">
            <Filter className="h-4 w-4" />
            <span className="font-medium">Filters:</span>
          </div>

          <Select value={statusFilter} onValueChange={onStatusFilterChange}>
            <SelectTrigger className="w-36 h-9">
              <SelectValue placeholder="Status" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">All Status</SelectItem>
              <SelectItem value="active">Active</SelectItem>
              <SelectItem value="inactive">Inactive</SelectItem>
              <SelectItem value="maintenance">Maintenance</SelectItem>
            </SelectContent>
          </Select>

          <Select value={providerFilter} onValueChange={onProviderFilterChange}>
            <SelectTrigger className="w-48 h-9">
              <SelectValue placeholder="Provider" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">All Providers</SelectItem>
              {providers.map((provider) => (
                <SelectItem key={provider.id} value={provider.id}>
                  {provider.name}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>

          <Select value={provinceFilter} onValueChange={onProvinceFilterChange}>
            <SelectTrigger className="w-48 h-9">
              <SelectValue placeholder="Province" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">All Provinces</SelectItem>
              {provinces.map((province) => (
                <SelectItem key={province.id} value={province.id}>
                  {province.name}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>

          <Select
            value={municipalityFilter}
            onValueChange={onMunicipalityFilterChange}
            disabled={provinceFilter === "all"}>
            <SelectTrigger className="w-48 h-9">
              <SelectValue placeholder="Municipality" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">All Municipalities</SelectItem>
              {municipalities.map((municipality) => (
                <SelectItem key={municipality.id} value={municipality.id}>
                  {municipality.name}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>

          <Select value={barangayFilter} onValueChange={onBarangayFilterChange} disabled={municipalityFilter === "all"}>
            <SelectTrigger className="w-48 h-9">
              <SelectValue placeholder="Barangay" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">All Barangays</SelectItem>
              {barangays.map((barangay) => (
                <SelectItem key={barangay.id} value={barangay.id}>
                  {barangay.name}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
      )}
    </div>
  );
}
