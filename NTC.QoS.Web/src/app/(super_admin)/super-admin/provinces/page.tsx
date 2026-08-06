"use client";

import { useEffect, useState } from "react";
import { Building, Pencil, Plus, Search, Trash2, Upload, X } from "lucide-react";
import { toast } from "sonner";

import { useCreateProvince } from "@/components/features/province/hooks/useCreateProvince";
import { useDeleteProvince } from "@/components/features/province/hooks/useDeleteProvince";
import { useEditProvince } from "@/components/features/province/hooks/useEditProvince";
import { useGetProvinces } from "@/components/features/province/hooks/useGetProvinces";
import type { Province } from "@/components/features/province/types/Province";
import { useGetRegions } from "@/components/features/region/hooks/useGetRegions";
import { ResponsiveDialog } from "@/components/ResponsiveDialog";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Pagination } from "@/components/ui/pagination";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { useUrlPagination } from "@/hooks/useUrlPagination";

export default function ProvincesManagementPage() {
  const [createDialogOpen, setCreateDialogOpen] = useState(false);
  const [editDialogOpen, setEditDialogOpen] = useState(false);
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
  const [selectedProvince, setSelectedProvince] = useState<Province | null>(null);
  const [searchQuery, setSearchQuery] = useState("");
  const [formValues, setFormValues] = useState({ name: "", code: "", description: "", area: "", regionId: "" });
  const [boundariesFile, setBoundariesFile] = useState<File | null>(null);

  const { page, pageSize, search, setPage, setPageSize, setSearch } = useUrlPagination();

  // Debounced search
  useEffect(() => {
    const timer = setTimeout(() => {
      setSearch(searchQuery);
    }, 300);
    return () => clearTimeout(timer);
  }, [searchQuery, setSearch]);

  const {
    data: provincesData,
    isLoading,
    refetch,
  } = useGetProvinces({ pathParams: {}, queryParams: { pageNumber: page, pageSize: pageSize, search: search || undefined } });

  const { data: regionsData } = useGetRegions({ pathParams: {}, queryParams: { pageNumber: 1, pageSize: 1000 } });

  const { mutateAsync: createProvince, isPending: isCreating } = useCreateProvince();
  const { mutateAsync: editProvince, isPending: isEditing } = useEditProvince();
  const { mutateAsync: deleteProvince, isPending: isDeleting } = useDeleteProvince();

  const provinces = provincesData?.data ?? [];
  const regions = regionsData?.data ?? [];
  const totalCount = provincesData?.totalRows || 0;

  const handleCreate = async (e: React.FormEvent) => {
    e.preventDefault();
    toast.promise(
      createProvince({
        pathParams: {},
        payload: {
          ...formValues,
          area: formValues.area ? parseFloat(formValues.area) : undefined,
          boundariesFile: boundariesFile || undefined,
        },
      }),
      {
        loading: "Creating province...",
        success: () => {
          setCreateDialogOpen(false);
          setFormValues({ name: "", code: "", description: "", area: "", regionId: "" });
          setBoundariesFile(null);
          void refetch();
          return "Province created successfully";
        },
        error: (err) => `Failed to create province: ${err.message || "Unknown error"}`,
      },
    );
  };

  const handleEdit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedProvince) return;
    toast.promise(
      editProvince({
        pathParams: { id: selectedProvince.id },
        payload: {
          ...formValues,
          area: formValues.area ? parseFloat(formValues.area) : undefined,
          boundariesFile: boundariesFile || undefined,
        },
      }),
      {
        loading: "Updating province...",
        success: () => {
          setEditDialogOpen(false);
          setSelectedProvince(null);
          setBoundariesFile(null);
          void refetch();
          return "Province updated successfully";
        },
        error: (err) => `Failed to update province: ${err.message || "Unknown error"}`,
      },
    );
  };

  const handleDelete = async () => {
    if (!selectedProvince) return;
    toast.promise(deleteProvince({ pathParams: { id: selectedProvince.id } }), {
      loading: "Deleting province...",
      success: () => {
        setDeleteDialogOpen(false);
        setSelectedProvince(null);
        void refetch();
        return "Province deleted successfully";
      },
      error: (err) => `Failed to delete province: ${err.message || "Unknown error"}`,
    });
  };

  return (
    <div className="container mx-auto p-6 space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold flex items-center gap-2">
            <Building className="h-8 w-8" />
            Provinces Management
          </h1>
          <p className="text-muted-foreground mt-1">Manage provinces and their regional assignments</p>
        </div>
        <Button onClick={() => setCreateDialogOpen(true)} className="gap-2">
          <Plus className="h-4 w-4" />
          Add Province
        </Button>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Provinces</CardTitle>
          <CardDescription>All provinces in the system</CardDescription>
        </CardHeader>
        <CardContent>
          <div className="mb-4">
            <div className="relative">
              <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-muted-foreground" />
              <Input
                placeholder="Search provinces..."
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                className="pl-10"
              />
            </div>
          </div>

          {isLoading ? (
            <div className="text-center py-8 text-muted-foreground">Loading...</div>
          ) : provinces.length === 0 ? (
            <div className="text-center py-8 text-muted-foreground">
              {search ? "No provinces found matching your search." : "No provinces added yet."}
            </div>
          ) : (
            <div className="border rounded-lg overflow-hidden">
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Name</TableHead>
                    <TableHead>Code</TableHead>
                    <TableHead>Region</TableHead>
                    <TableHead>Area (km²)</TableHead>
                    <TableHead>Municipalities</TableHead>
                    <TableHead className="text-right">Actions</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {provinces.map((province) => (
                    <TableRow key={province.id}>
                      <TableCell className="font-medium">{province.name}</TableCell>
                      <TableCell>
                        <Badge variant="outline">{province.code || "—"}</Badge>
                      </TableCell>
                      <TableCell>{province.region?.name || "—"}</TableCell>
                      <TableCell>{province.area?.toLocaleString() || "—"}</TableCell>
                      <TableCell>{province.municipalityCount || 0}</TableCell>
                      <TableCell className="text-right space-x-2">
                        <Button
                          variant="ghost"
                          size="sm"
                          onClick={() => {
                            setSelectedProvince(province);
                            setFormValues({
                              name: province.name,
                              code: province.code || "",
                              description: province.description || "",
                              area: province.area?.toString() || "",
                              regionId: province.region?.id || "",
                            });
                            setBoundariesFile(null);
                            setEditDialogOpen(true);
                          }}>
                          <Pencil className="h-4 w-4" />
                        </Button>
                        <Button
                          variant="ghost"
                          size="sm"
                          onClick={() => {
                            setSelectedProvince(province);
                            setDeleteDialogOpen(true);
                          }}
                          className="text-destructive hover:text-destructive">
                          <Trash2 className="h-4 w-4" />
                        </Button>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </div>
          )}

          {!isLoading && !search && totalCount > 0 && (
            <Pagination
              currentPage={page}
              totalPages={Math.ceil(totalCount / pageSize)}
              pageSize={pageSize}
              totalCount={totalCount}
              onPageChange={setPage}
              onPageSizeChange={setPageSize}
            />
          )}
        </CardContent>
      </Card>

      {/* Create Dialog */}
      <ResponsiveDialog
        open={createDialogOpen}
        onOpenChange={setCreateDialogOpen}
        title="Add Province"
        description="Create a new province">
        <form onSubmit={handleCreate} className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="name">Name *</Label>
            <Input
              id="name"
              value={formValues.name}
              onChange={(e) => setFormValues({ ...formValues, name: e.target.value })}
              required
            />
          </div>
          <div className="space-y-2">
            <Label htmlFor="region">Region *</Label>
            <Select
              value={formValues.regionId}
              onValueChange={(value) => setFormValues({ ...formValues, regionId: value })}
              required>
              <SelectTrigger id="region">
                <SelectValue placeholder="Select region" />
              </SelectTrigger>
              <SelectContent>
                {regions.map((region) => (
                  <SelectItem key={region.id} value={region.id}>
                    {region.name}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
          <div className="space-y-2">
            <Label htmlFor="code">Code</Label>
            <Input
              id="code"
              value={formValues.code}
              onChange={(e) => setFormValues({ ...formValues, code: e.target.value })}
            />
          </div>
          <div className="space-y-2">
            <Label htmlFor="area">Area (km²)</Label>
            <Input
              id="area"
              type="number"
              step="0.01"
              value={formValues.area}
              onChange={(e) => setFormValues({ ...formValues, area: e.target.value })}
            />
          </div>
          <div className="space-y-2">
            <Label htmlFor="description">Description</Label>
            <Input
              id="description"
              value={formValues.description}
              onChange={(e) => setFormValues({ ...formValues, description: e.target.value })}
            />
          </div>
          <div className="space-y-2">
            <Label htmlFor="boundariesFile">Boundaries File (GeoJSON)</Label>
            <div className="flex items-center gap-2">
              <Input
                id="boundariesFile"
                type="file"
                accept=".json,.geojson"
                onChange={(e) => setBoundariesFile(e.target.files?.[0] || null)}
              />
              {boundariesFile && (
                <Button type="button" variant="ghost" size="sm" onClick={() => setBoundariesFile(null)}>
                  <X className="h-4 w-4" />
                </Button>
              )}
            </div>
            {boundariesFile && <p className="text-sm text-muted-foreground">Selected: {boundariesFile.name}</p>}
          </div>
          <div className="flex justify-end gap-2 pt-4">
            <Button type="button" variant="outline" onClick={() => setCreateDialogOpen(false)} disabled={isCreating}>
              Cancel
            </Button>
            <Button type="submit" disabled={isCreating}>
              {isCreating ? "Creating..." : "Create Province"}
            </Button>
          </div>
        </form>
      </ResponsiveDialog>

      {/* Edit Dialog */}
      <ResponsiveDialog
        open={editDialogOpen}
        onOpenChange={setEditDialogOpen}
        title="Edit Province"
        description="Update province information">
        <form onSubmit={handleEdit} className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="edit-name">Name *</Label>
            <Input
              id="edit-name"
              value={formValues.name}
              onChange={(e) => setFormValues({ ...formValues, name: e.target.value })}
              required
            />
          </div>
          <div className="space-y-2">
            <Label htmlFor="edit-region">Region *</Label>
            <Select
              value={formValues.regionId}
              onValueChange={(value) => setFormValues({ ...formValues, regionId: value })}
              required>
              <SelectTrigger id="edit-region">
                <SelectValue placeholder="Select region" />
              </SelectTrigger>
              <SelectContent>
                {regions.map((region) => (
                  <SelectItem key={region.id} value={region.id}>
                    {region.name}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
          <div className="space-y-2">
            <Label htmlFor="edit-code">Code</Label>
            <Input
              id="edit-code"
              value={formValues.code}
              onChange={(e) => setFormValues({ ...formValues, code: e.target.value })}
            />
          </div>
          <div className="space-y-2">
            <Label htmlFor="edit-area">Area (km²)</Label>
            <Input
              id="edit-area"
              type="number"
              step="0.01"
              value={formValues.area}
              onChange={(e) => setFormValues({ ...formValues, area: e.target.value })}
            />
          </div>
          <div className="space-y-2">
            <Label htmlFor="edit-description">Description</Label>
            <Input
              id="edit-description"
              value={formValues.description}
              onChange={(e) => setFormValues({ ...formValues, description: e.target.value })}
            />
          </div>
          <div className="space-y-2">
            <Label htmlFor="edit-boundariesFile">Boundaries File (GeoJSON)</Label>
            <div className="flex items-center gap-2">
              <Input
                id="edit-boundariesFile"
                type="file"
                accept=".json,.geojson"
                onChange={(e) => setBoundariesFile(e.target.files?.[0] || null)}
              />
              {boundariesFile && (
                <Button type="button" variant="ghost" size="sm" onClick={() => setBoundariesFile(null)}>
                  <X className="h-4 w-4" />
                </Button>
              )}
            </div>
            {boundariesFile && <p className="text-sm text-muted-foreground">Selected: {boundariesFile.name}</p>}
          </div>
          <div className="flex justify-end gap-2 pt-4">
            <Button type="button" variant="outline" onClick={() => setEditDialogOpen(false)} disabled={isEditing}>
              Cancel
            </Button>
            <Button type="submit" disabled={isEditing}>
              {isEditing ? "Updating..." : "Update Province"}
            </Button>
          </div>
        </form>
      </ResponsiveDialog>

      {/* Delete Dialog */}
      <AlertDialog open={deleteDialogOpen} onOpenChange={setDeleteDialogOpen}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Delete Province?</AlertDialogTitle>
            <AlertDialogDescription>
              Are you sure you want to delete "{selectedProvince?.name}"? This action cannot be undone.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel disabled={isDeleting}>Cancel</AlertDialogCancel>
            <AlertDialogAction
              onClick={handleDelete}
              disabled={isDeleting}
              className="bg-destructive text-destructive-foreground hover:bg-destructive/90">
              {isDeleting ? "Deleting..." : "Delete"}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}
