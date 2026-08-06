"use client";

import { useEffect, useState } from "react";
import { Edit, MapPin, Plus, Trash2, Upload, X } from "lucide-react";
import { toast } from "sonner";

import { createRegionRequest } from "@/components/features/region/handlers/createRegionRequest";
import { deleteRegionRequest } from "@/components/features/region/handlers/deleteRegionRequest";
import { editRegionRequest } from "@/components/features/region/handlers/editRegionRequest";
import { getRegionsRequest } from "@/components/features/region/handlers/getRegionsRequest";
import type { Region } from "@/components/features/region/types/Region";
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
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Pagination } from "@/components/ui/pagination";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Textarea } from "@/components/ui/textarea";
import { createMutationHookFromTemplate } from "@/hooks/createMutationHookFromTemplate";
import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";
import { useUrlPagination } from "@/hooks/useUrlPagination";

const useGetRegions = createQueryHookFromTemplate(getRegionsRequest);
const useDeleteRegion = createMutationHookFromTemplate(deleteRegionRequest);
const useCreateRegion = createMutationHookFromTemplate(createRegionRequest);
const useEditRegion = createMutationHookFromTemplate(editRegionRequest);

export default function RegionsManagementPage() {
  const [createDialogOpen, setCreateDialogOpen] = useState(false);
  const [editDialogOpen, setEditDialogOpen] = useState(false);
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
  const [selectedRegion, setSelectedRegion] = useState<Region | null>(null);
  const [searchQuery, setSearchQuery] = useState("");
  const [formValues, setFormValues] = useState({ name: "", code: "", description: "", area: "" });
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
    data: regionsData,
    isLoading,
    refetch,
  } = useGetRegions({ pathParams: {}, queryParams: { pageNumber: page, pageSize: pageSize, search: search || undefined } });

  const { mutateAsync: deleteRegion, isPending: isDeleting } = useDeleteRegion();
  const { mutateAsync: createRegion, isPending: isCreating } = useCreateRegion();
  const { mutateAsync: editRegion, isPending: isEditing } = useEditRegion();

  const regions = regionsData?.data ?? [];
  const totalCount = regionsData?.totalRows || 0;
  const totalPages = Math.ceil(totalCount / pageSize);

  const handleCreate = async (e: React.FormEvent) => {
    e.preventDefault();
    toast.promise(
      createRegion({
        pathParams: {},
        payload: {
          name: formValues.name,
          code: formValues.code || undefined,
          description: formValues.description || undefined,
          area: formValues.area ? parseFloat(formValues.area) : undefined,
          boundariesFile: boundariesFile || undefined,
        },
      }).then(() => refetch()),
      {
        loading: "Creating region...",
        success: () => {
          setCreateDialogOpen(false);
          setFormValues({ name: "", code: "", description: "", area: "" });
          setBoundariesFile(null);
          return "Region created successfully";
        },
        error: "Failed to create region",
      },
    );
  };

  const handleEdit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedRegion) return;
    toast.promise(
      editRegion({
        pathParams: { id: selectedRegion.id },
        payload: {
          name: formValues.name,
          code: formValues.code || undefined,
          description: formValues.description || undefined,
          area: formValues.area ? parseFloat(formValues.area) : undefined,
          boundariesFile: boundariesFile || undefined,
        },
      }).then(() => refetch()),
      {
        loading: "Updating region...",
        success: () => {
          setEditDialogOpen(false);
          setSelectedRegion(null);
          setBoundariesFile(null);
          return "Region updated successfully";
        },
        error: "Failed to update region",
      },
    );
  };

  const handleDelete = async () => {
    if (!selectedRegion) return;
    toast.promise(
      deleteRegion({ pathParams: { id: selectedRegion.id } }).then(() => refetch()),
      {
        loading: "Deleting region...",
        success: () => {
          setDeleteDialogOpen(false);
          setSelectedRegion(null);
          return "Region deleted successfully";
        },
        error: "Failed to delete region",
      },
    );
  };

  const openEditDialog = (region: Region) => {
    setSelectedRegion(region);
    setFormValues({
      name: region.name,
      code: region.code || "",
      description: region.description || "",
      area: region.area?.toString() || "",
    });
    setBoundariesFile(null);
    setEditDialogOpen(true);
  };

  const openDeleteDialog = (region: Region) => {
    setSelectedRegion(region);
    setDeleteDialogOpen(true);
  };

  return (
    <div className="space-y-6">
      <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-3xl font-bold">Regions Management</h1>
          <p className="text-muted-foreground mt-1">Manage regional divisions</p>
        </div>
        <Button onClick={() => setCreateDialogOpen(true)} className="gap-2">
          <Plus className="h-4 w-4" />
          Create Region
        </Button>
      </div>

      <Card>
        <CardHeader>
          <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4">
            <div>
              <CardTitle className="flex items-center gap-2">
                <MapPin className="h-5 w-5" />
                Regions ({totalCount})
              </CardTitle>
              <CardDescription>View and manage all regional divisions</CardDescription>
            </div>
            <div className="w-full sm:w-auto">
              <Input
                placeholder="Search regions..."
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                className="w-full sm:w-[300px]"
              />
            </div>
          </div>
        </CardHeader>
        <CardContent>
          {isLoading ? (
            <div className="text-center py-12">
              <p className="text-muted-foreground">Loading regions...</p>
            </div>
          ) : regions.length === 0 ? (
            <div className="text-center py-12">
              <MapPin className="mx-auto h-12 w-12 text-muted-foreground/50" />
              <p className="mt-4 text-muted-foreground">
                {search ? "No regions found matching your search." : "No regions created yet."}
              </p>
            </div>
          ) : (
            <>
              <div className="overflow-x-auto">
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead>Name</TableHead>
                      <TableHead>Code</TableHead>
                      <TableHead>Description</TableHead>
                      <TableHead className="text-right">Actions</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {regions.map((region) => (
                      <TableRow key={region.id}>
                        <TableCell className="font-medium">{region.name}</TableCell>
                        <TableCell>{region.code || "—"}</TableCell>
                        <TableCell className="max-w-md truncate">{region.description || "—"}</TableCell>
                        <TableCell className="text-right">
                          <div className="flex items-center justify-end gap-2">
                            <Button variant="ghost" size="icon" onClick={() => openEditDialog(region)}>
                              <Edit className="h-4 w-4" />
                            </Button>
                            <Button variant="ghost" size="icon" onClick={() => openDeleteDialog(region)}>
                              <Trash2 className="h-4 w-4 text-destructive" />
                            </Button>
                          </div>
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </div>

              {!search && totalCount > 0 && (
                <Pagination
                  currentPage={page}
                  totalPages={totalPages}
                  pageSize={pageSize}
                  totalCount={totalCount}
                  onPageChange={setPage}
                  onPageSizeChange={setPageSize}
                />
              )}
            </>
          )}
        </CardContent>
      </Card>

      {/* Create Dialog */}
      <ResponsiveDialog
        open={createDialogOpen}
        onOpenChange={setCreateDialogOpen}
        title="Create Region"
        description="Add a new regional division to the system">
        <form onSubmit={handleCreate} className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="create-name">Name *</Label>
            <Input
              id="create-name"
              value={formValues.name}
              onChange={(e) => setFormValues({ ...formValues, name: e.target.value })}
              placeholder="Enter region name"
              required
            />
          </div>
          <div className="space-y-2">
            <Label htmlFor="create-code">Code</Label>
            <Input
              id="create-code"
              value={formValues.code}
              onChange={(e) => setFormValues({ ...formValues, code: e.target.value })}
              placeholder="Enter region code"
            />
          </div>
          <div className="space-y-2">
            <Label htmlFor="create-description">Description</Label>
            <Textarea
              id="create-description"
              value={formValues.description}
              onChange={(e) => setFormValues({ ...formValues, description: e.target.value })}
              placeholder="Enter region description"
              rows={3}
            />
          </div>
          <div className="space-y-2">
            <Label htmlFor="create-area">Area (km²)</Label>
            <Input
              id="create-area"
              type="number"
              step="0.01"
              value={formValues.area}
              onChange={(e) => setFormValues({ ...formValues, area: e.target.value })}
              placeholder="Enter area in square kilometers"
            />
          </div>
          <div className="space-y-2">
            <Label htmlFor="create-boundariesFile">Boundaries File (GeoJSON)</Label>
            <div className="flex items-center gap-2">
              <Input
                id="create-boundariesFile"
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
            <Button type="button" variant="outline" onClick={() => setCreateDialogOpen(false)}>
              Cancel
            </Button>
            <Button type="submit" disabled={isCreating}>
              {isCreating ? "Creating..." : "Create"}
            </Button>
          </div>
        </form>
      </ResponsiveDialog>

      {/* Edit Dialog */}
      <ResponsiveDialog
        open={editDialogOpen}
        onOpenChange={setEditDialogOpen}
        title="Edit Region"
        description="Update region information">
        <form onSubmit={handleEdit} className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="edit-name">Name *</Label>
            <Input
              id="edit-name"
              value={formValues.name}
              onChange={(e) => setFormValues({ ...formValues, name: e.target.value })}
              placeholder="Enter region name"
              required
            />
          </div>
          <div className="space-y-2">
            <Label htmlFor="edit-code">Code</Label>
            <Input
              id="edit-code"
              value={formValues.code}
              onChange={(e) => setFormValues({ ...formValues, code: e.target.value })}
              placeholder="Enter region code"
            />
          </div>
          <div className="space-y-2">
            <Label htmlFor="edit-description">Description</Label>
            <Textarea
              id="edit-description"
              value={formValues.description}
              onChange={(e) => setFormValues({ ...formValues, description: e.target.value })}
              placeholder="Enter region description"
              rows={3}
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
              placeholder="Enter area in square kilometers"
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
            <Button type="button" variant="outline" onClick={() => setEditDialogOpen(false)}>
              Cancel
            </Button>
            <Button type="submit" disabled={isEditing}>
              {isEditing ? "Updating..." : "Update"}
            </Button>
          </div>
        </form>
      </ResponsiveDialog>

      {/* Delete Dialog */}
      <AlertDialog open={deleteDialogOpen} onOpenChange={setDeleteDialogOpen}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Delete Region</AlertDialogTitle>
            <AlertDialogDescription>
              Are you sure you want to delete <strong>{selectedRegion?.name}</strong>? This action cannot be undone.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancel</AlertDialogCancel>
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
