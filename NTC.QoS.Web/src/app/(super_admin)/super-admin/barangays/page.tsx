"use client";

import { useEffect, useState } from "react";
import { Home, Pencil, Plus, Search, Trash2 } from "lucide-react";
import { toast } from "sonner";

import { useCreateBarangay } from "@/components/features/barangay/hooks/useCreateBarangay";
import { useDeleteBarangay } from "@/components/features/barangay/hooks/useDeleteBarangay";
import { useEditBarangay } from "@/components/features/barangay/hooks/useEditBarangay";
import { useGetBarangays } from "@/components/features/barangay/hooks/useGetBarangays";
import type { Barangay } from "@/components/features/barangay/types/Barangay";
import { useGetMunicipalities } from "@/components/features/municipality/hooks/useGetMunicipalities";
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

export default function BarangaysManagementPage() {
  const [createDialogOpen, setCreateDialogOpen] = useState(false);
  const [editDialogOpen, setEditDialogOpen] = useState(false);
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
  const [selectedBarangay, setSelectedBarangay] = useState<Barangay | null>(null);
  const [searchQuery, setSearchQuery] = useState("");
  const [formValues, setFormValues] = useState({ name: "", code: "", description: "", area: "", municipalityId: "" });
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
    data: barangaysData,
    isLoading,
    refetch,
  } = useGetBarangays({ pathParams: {}, queryParams: { pageNumber: page, pageSize: pageSize, search: search || undefined } });

  const { data: municipalitiesData } = useGetMunicipalities({
    pathParams: {},
    queryParams: { pageNumber: 1, pageSize: 1000 },
  });

  const { mutateAsync: createBarangay, isPending: isCreating } = useCreateBarangay();
  const { mutateAsync: editBarangay, isPending: isEditing } = useEditBarangay();
  const { mutateAsync: deleteBarangay, isPending: isDeleting } = useDeleteBarangay();

  const barangays = barangaysData?.data ?? [];
  const municipalities = municipalitiesData?.data ?? [];
  const totalCount = barangaysData?.totalRows || 0;

  const handleCreate = async () => {
    if (!formValues.name || !formValues.municipalityId) {
      toast.error("Please fill in all required fields");
      return;
    }

    try {
      await createBarangay({
        pathParams: {},
        payload: {
          name: formValues.name,
          code: formValues.code || undefined,
          description: formValues.description || undefined,
          area: formValues.area ? parseFloat(formValues.area) : undefined,
          municipalityId: formValues.municipalityId,
          boundariesFile: boundariesFile || undefined,
        },
      });
      toast.success("Barangay created successfully");
      setCreateDialogOpen(false);
      setFormValues({ name: "", code: "", description: "", area: "", municipalityId: "" });
      setBoundariesFile(null);
      refetch();
    } catch (error) {
      toast.error("Failed to create barangay");
    }
  };

  const handleEdit = async () => {
    if (!selectedBarangay || !formValues.name) {
      toast.error("Please fill in all required fields");
      return;
    }

    try {
      await editBarangay({
        pathParams: { id: selectedBarangay.id },
        payload: {
          name: formValues.name,
          code: formValues.code || undefined,
          description: formValues.description || undefined,
          area: formValues.area ? parseFloat(formValues.area) : undefined,
          municipalityId: formValues.municipalityId || undefined,
          boundariesFile: boundariesFile || undefined,
        },
      });
      toast.success("Barangay updated successfully");
      setEditDialogOpen(false);
      setSelectedBarangay(null);
      refetch();
    } catch (error) {
      toast.error("Failed to update barangay");
    }
  };

  const handleDelete = async () => {
    if (!selectedBarangay) return;

    try {
      await deleteBarangay({ pathParams: { id: selectedBarangay.id } });
      toast.success("Barangay deleted successfully");
      setDeleteDialogOpen(false);
      setSelectedBarangay(null);
      refetch();
    } catch (error) {
      toast.error("Failed to delete barangay");
    }
  };

  const openEditDialog = (barangay: Barangay) => {
    setSelectedBarangay(barangay);
    setFormValues({
      name: barangay.name || "",
      code: barangay.code || "",
      description: barangay.description || "",
      area: barangay.area?.toString() || "",
      municipalityId: barangay.municipality?.id || "",
    });
    setBoundariesFile(null);
    setEditDialogOpen(true);
  };

  const openDeleteDialog = (barangay: Barangay) => {
    setSelectedBarangay(barangay);
    setDeleteDialogOpen(true);
  };

  return (
    <div className="container mx-auto px-4 py-8">
      <Card>
        <CardHeader>
          <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
            <div>
              <CardTitle className="flex items-center gap-2">
                <Home className="h-6 w-6" />
                Barangays Management
              </CardTitle>
              <CardDescription>
                Total: {totalCount.toLocaleString()} {totalCount === 1 ? "barangay" : "barangays"}
              </CardDescription>
            </div>
            <Button
              onClick={() => {
                setFormValues({ name: "", code: "", description: "", area: "", municipalityId: "" });
                setBoundariesFile(null);
                setCreateDialogOpen(true);
              }}>
              <Plus className="h-4 w-4 mr-2" />
              Add Barangay
            </Button>
          </div>
        </CardHeader>
        <CardContent>
          <div className="mb-4">
            <div className="relative">
              <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-gray-400" />
              <Input
                placeholder="Search barangays..."
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                className="pl-10"
              />
            </div>
          </div>

          {isLoading ? (
            <div className="text-center py-8">Loading...</div>
          ) : barangays.length === 0 ? (
            <div className="text-center py-8 text-gray-500">No barangays yet. Create one to get started.</div>
          ) : (
            <div className="overflow-x-auto">
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Name</TableHead>
                    <TableHead>Code</TableHead>
                    <TableHead>Municipality</TableHead>
                    <TableHead>Area (km²)</TableHead>
                    <TableHead className="text-right">Actions</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {barangays.map((barangay) => (
                    <TableRow key={barangay.id}>
                      <TableCell className="font-medium">{barangay.name}</TableCell>
                      <TableCell>{barangay.code && <Badge variant="outline">{barangay.code}</Badge>}</TableCell>
                      <TableCell>{barangay.municipality?.name || "N/A"}</TableCell>
                      <TableCell>{barangay.area?.toFixed(2) || "N/A"}</TableCell>
                      <TableCell className="text-right">
                        <div className="flex justify-end gap-2">
                          <Button variant="ghost" size="sm" onClick={() => openEditDialog(barangay)}>
                            <Pencil className="h-4 w-4" />
                          </Button>
                          <Button variant="ghost" size="sm" onClick={() => openDeleteDialog(barangay)}>
                            <Trash2 className="h-4 w-4 text-red-500" />
                          </Button>
                        </div>
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
        title="Create Barangay"
        description="Add a new barangay to the system">
        <div className="space-y-4">
          <div>
            <Label htmlFor="create-name">Name *</Label>
            <Input
              id="create-name"
              value={formValues.name}
              onChange={(e) => setFormValues({ ...formValues, name: e.target.value })}
              placeholder="Enter barangay name"
            />
          </div>
          <div>
            <Label htmlFor="create-municipality">Municipality *</Label>
            <Select
              value={formValues.municipalityId}
              onValueChange={(value) => setFormValues({ ...formValues, municipalityId: value })}>
              <SelectTrigger>
                <SelectValue placeholder="Select municipality" />
              </SelectTrigger>
              <SelectContent>
                {municipalities.map((municipality) => (
                  <SelectItem key={municipality.id} value={municipality.id}>
                    {municipality.name}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
          <div>
            <Label htmlFor="create-code">Code</Label>
            <Input
              id="create-code"
              value={formValues.code}
              onChange={(e) => setFormValues({ ...formValues, code: e.target.value })}
              placeholder="Enter barangay code"
            />
          </div>
          <div>
            <Label htmlFor="create-area">Area (km²)</Label>
            <Input
              id="create-area"
              type="number"
              step="0.01"
              value={formValues.area}
              onChange={(e) => setFormValues({ ...formValues, area: e.target.value })}
              placeholder="Enter area"
            />
          </div>
          <div>
            <Label htmlFor="create-description">Description</Label>
            <Input
              id="create-description"
              value={formValues.description}
              onChange={(e) => setFormValues({ ...formValues, description: e.target.value })}
              placeholder="Enter description"
            />
          </div>
          <div>
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
                  Clear
                </Button>
              )}
            </div>
            <p className="text-xs text-muted-foreground mt-1">Upload a GeoJSON file defining the barangay boundaries</p>
          </div>
          <div className="flex justify-end gap-2">
            <Button variant="outline" onClick={() => setCreateDialogOpen(false)}>
              Cancel
            </Button>
            <Button onClick={handleCreate} disabled={isCreating}>
              {isCreating ? "Creating..." : "Create"}
            </Button>
          </div>
        </div>
      </ResponsiveDialog>

      {/* Edit Dialog */}
      <ResponsiveDialog
        open={editDialogOpen}
        onOpenChange={setEditDialogOpen}
        title="Edit Barangay"
        description="Update barangay information">
        <div className="space-y-4">
          <div>
            <Label htmlFor="edit-name">Name *</Label>
            <Input
              id="edit-name"
              value={formValues.name}
              onChange={(e) => setFormValues({ ...formValues, name: e.target.value })}
              placeholder="Enter barangay name"
            />
          </div>
          <div>
            <Label htmlFor="edit-municipality">Municipality</Label>
            <Select
              value={formValues.municipalityId}
              onValueChange={(value) => setFormValues({ ...formValues, municipalityId: value })}>
              <SelectTrigger>
                <SelectValue placeholder="Select municipality" />
              </SelectTrigger>
              <SelectContent>
                {municipalities.map((municipality) => (
                  <SelectItem key={municipality.id} value={municipality.id}>
                    {municipality.name}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
          <div>
            <Label htmlFor="edit-code">Code</Label>
            <Input
              id="edit-code"
              value={formValues.code}
              onChange={(e) => setFormValues({ ...formValues, code: e.target.value })}
              placeholder="Enter barangay code"
            />
          </div>
          <div>
            <Label htmlFor="edit-area">Area (km²)</Label>
            <Input
              id="edit-area"
              type="number"
              step="0.01"
              value={formValues.area}
              onChange={(e) => setFormValues({ ...formValues, area: e.target.value })}
              placeholder="Enter area"
            />
          </div>
          <div>
            <Label htmlFor="edit-description">Description</Label>
            <Input
              id="edit-description"
              value={formValues.description}
              onChange={(e) => setFormValues({ ...formValues, description: e.target.value })}
              placeholder="Enter description"
            />
          </div>
          <div>
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
                  Clear
                </Button>
              )}
            </div>
            <p className="text-xs text-muted-foreground mt-1">
              Upload a new GeoJSON file to update boundaries (optional)
            </p>
          </div>
          <div className="flex justify-end gap-2">
            <Button variant="outline" onClick={() => setEditDialogOpen(false)}>
              Cancel
            </Button>
            <Button onClick={handleEdit} disabled={isEditing}>
              {isEditing ? "Updating..." : "Update"}
            </Button>
          </div>
        </div>
      </ResponsiveDialog>

      {/* Delete Confirmation Dialog */}
      <AlertDialog open={deleteDialogOpen} onOpenChange={setDeleteDialogOpen}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Are you sure?</AlertDialogTitle>
            <AlertDialogDescription>
              This will permanently delete &quot;{selectedBarangay?.name}&quot;. This action cannot be undone.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancel</AlertDialogCancel>
            <AlertDialogAction onClick={handleDelete} disabled={isDeleting}>
              {isDeleting ? "Deleting..." : "Delete"}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}
