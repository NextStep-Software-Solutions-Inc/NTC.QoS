"use client";

import { useEffect, useState } from "react";
import { Building2, Pencil, Plus, Search, Trash2, Upload } from "lucide-react";
import { toast } from "sonner";

import { useCreateMunicipality } from "@/components/features/municipality/hooks/useCreateMunicipality";
import { useDeleteMunicipality } from "@/components/features/municipality/hooks/useDeleteMunicipality";
import { useEditMunicipality } from "@/components/features/municipality/hooks/useEditMunicipality";
import { useGetMunicipalities } from "@/components/features/municipality/hooks/useGetMunicipalities";
import type { Municipality } from "@/components/features/municipality/types/Municipality";
import { useGetProvinces } from "@/components/features/province/hooks/useGetProvinces";
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
import { Checkbox } from "@/components/ui/checkbox";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Pagination } from "@/components/ui/pagination";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { useUrlPagination } from "@/hooks/useUrlPagination";

export default function MunicipalitiesManagementPage() {
  const [createDialogOpen, setCreateDialogOpen] = useState(false);
  const [editDialogOpen, setEditDialogOpen] = useState(false);
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
  const [selectedMunicipality, setSelectedMunicipality] = useState<Municipality | null>(null);
  const [searchQuery, setSearchQuery] = useState("");
  const [formValues, setFormValues] = useState({
    name: "",
    code: "",
    description: "",
    area: "",
    provinceId: "",
    isCity: false,
  });
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
    data: municipalitiesData,
    isLoading,
    refetch,
  } = useGetMunicipalities({ pathParams: {}, queryParams: { pageNumber: page, pageSize: pageSize, search: search || undefined } });

  const { data: provincesData } = useGetProvinces({ pathParams: {}, queryParams: { pageNumber: 1, pageSize: 1000 } });

  const { mutateAsync: createMunicipality, isPending: isCreating } = useCreateMunicipality();
  const { mutateAsync: editMunicipality, isPending: isEditing } = useEditMunicipality();
  const { mutateAsync: deleteMunicipality, isPending: isDeleting } = useDeleteMunicipality();

  const municipalities = municipalitiesData?.data ?? [];
  const provinces = provincesData?.data ?? [];
  const totalCount = municipalitiesData?.totalRows || 0;

  const handleCreate = async () => {
    if (!formValues.name || !formValues.provinceId) {
      toast.error("Please fill in all required fields");
      return;
    }

    try {
      await createMunicipality({
        pathParams: {},
        payload: {
          name: formValues.name,
          code: formValues.code || undefined,
          description: formValues.description || undefined,
          area: formValues.area ? parseFloat(formValues.area) : undefined,
          provinceId: formValues.provinceId,
          isCity: formValues.isCity,
          boundariesFile: boundariesFile || undefined,
        },
      });
      toast.success("Municipality created successfully");
      setCreateDialogOpen(false);
      setFormValues({ name: "", code: "", description: "", area: "", provinceId: "", isCity: false });
      setBoundariesFile(null);
      refetch();
    } catch (error) {
      toast.error("Failed to create municipality");
    }
  };

  const handleEdit = async () => {
    if (!selectedMunicipality || !formValues.name) {
      toast.error("Please fill in all required fields");
      return;
    }

    try {
      await editMunicipality({
        pathParams: { id: selectedMunicipality.id },
        payload: {
          name: formValues.name,
          code: formValues.code || undefined,
          description: formValues.description || undefined,
          area: formValues.area ? parseFloat(formValues.area) : undefined,
          provinceId: formValues.provinceId || undefined,
          isCity: formValues.isCity,
          boundariesFile: boundariesFile || undefined,
        },
      });
      toast.success("Municipality updated successfully");
      setEditDialogOpen(false);
      setSelectedMunicipality(null);
      setFormValues({ name: "", code: "", description: "", area: "", provinceId: "", isCity: false });
      setBoundariesFile(null);
      refetch();
    } catch (error) {
      toast.error("Failed to update municipality");
    }
  };

  const handleDelete = async () => {
    if (!selectedMunicipality) return;

    try {
      await deleteMunicipality({ pathParams: { id: selectedMunicipality.id } });
      toast.success("Municipality deleted successfully");
      setDeleteDialogOpen(false);
      setSelectedMunicipality(null);
      refetch();
    } catch (error) {
      toast.error("Failed to delete municipality");
    }
  };

  const openEditDialog = (municipality: Municipality) => {
    setSelectedMunicipality(municipality);
    setFormValues({
      name: municipality.name || "",
      code: municipality.code || "",
      description: municipality.description || "",
      area: municipality.area?.toString() || "",
      provinceId: municipality.province?.id || "",
      isCity: municipality.isCity || false,
    });
    setBoundariesFile(null);
    setEditDialogOpen(true);
  };

  const openDeleteDialog = (municipality: Municipality) => {
    setSelectedMunicipality(municipality);
    setDeleteDialogOpen(false);
  };

  return (
    <div className="container mx-auto p-6 space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold flex items-center gap-2">
            <Building2 className="h-8 w-8" />
            Municipalities Management
          </h1>
          <p className="text-muted-foreground mt-1">Manage municipalities and cities</p>
        </div>
        <Button
          onClick={() => {
            setFormValues({ name: "", code: "", description: "", area: "", provinceId: "", isCity: false });
            setBoundariesFile(null);
            setCreateDialogOpen(true);
          }}>
          <Plus className="mr-2 h-4 w-4" />
          Add Municipality
        </Button>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Municipalities</CardTitle>
          <CardDescription>
            Total: {totalCount.toLocaleString()} {totalCount === 1 ? "municipality" : "municipalities"}
          </CardDescription>
          <div className="relative">
            <Search className="absolute left-3 top-3 h-4 w-4 text-muted-foreground" />
            <Input
              placeholder="Search municipalities..."
              className="pl-10"
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
            />
          </div>
        </CardHeader>
        <CardContent>
          {isLoading ? (
            <div className="text-center py-8">Loading municipalities...</div>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Name</TableHead>
                  <TableHead>Type</TableHead>
                  <TableHead>Code</TableHead>
                  <TableHead>Province</TableHead>
                  <TableHead>Area (km²)</TableHead>
                  <TableHead className="text-right">Actions</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {municipalities.length === 0 ? (
                  <TableRow>
                    <TableCell colSpan={6} className="text-center py-8 text-muted-foreground">
                      No municipalities found
                    </TableCell>
                  </TableRow>
                ) : (
                  municipalities.map((municipality) => (
                    <TableRow key={municipality.id}>
                      <TableCell className="font-medium">{municipality.name}</TableCell>
                      <TableCell>
                        <Badge variant={municipality.isCity ? "default" : "secondary"}>
                          {municipality.isCity ? "City" : "Municipality"}
                        </Badge>
                      </TableCell>
                      <TableCell>
                        <code className="text-xs">{municipality.code || "—"}</code>
                      </TableCell>
                      <TableCell>{municipality.province?.name || "—"}</TableCell>
                      <TableCell>{municipality.area?.toFixed(2) || "—"}</TableCell>
                      <TableCell className="text-right space-x-2">
                        <Button variant="ghost" size="sm" onClick={() => openEditDialog(municipality)}>
                          <Pencil className="h-4 w-4" />
                        </Button>
                        <Button
                          variant="ghost"
                          size="sm"
                          onClick={() => {
                            setSelectedMunicipality(municipality);
                            setDeleteDialogOpen(true);
                          }}>
                          <Trash2 className="h-4 w-4 text-destructive" />
                        </Button>
                      </TableCell>
                    </TableRow>
                  ))
                )}
              </TableBody>
            </Table>
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
        title="Create Municipality"
        description="Add a new municipality or city">
        <div className="space-y-4">
          <div>
            <Label htmlFor="name">Name *</Label>
            <Input
              id="name"
              value={formValues.name}
              onChange={(e) => setFormValues({ ...formValues, name: e.target.value })}
              placeholder="Enter municipality name"
            />
          </div>
          <div>
            <Label htmlFor="province">Province *</Label>
            <Select
              value={formValues.provinceId}
              onValueChange={(value) => setFormValues({ ...formValues, provinceId: value })}>
              <SelectTrigger>
                <SelectValue placeholder="Select province" />
              </SelectTrigger>
              <SelectContent>
                {provinces.map((province) => (
                  <SelectItem key={province.id} value={province.id}>
                    {province.name}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
          <div className="flex items-center space-x-2">
            <Checkbox
              id="isCity"
              checked={formValues.isCity}
              onCheckedChange={(checked) => setFormValues({ ...formValues, isCity: checked as boolean })}
            />
            <Label htmlFor="isCity">This is a city (not a municipality)</Label>
          </div>
          <div>
            <Label htmlFor="code">Code</Label>
            <Input
              id="code"
              value={formValues.code}
              onChange={(e) => setFormValues({ ...formValues, code: e.target.value })}
              placeholder="Enter municipality code"
            />
          </div>
          <div>
            <Label htmlFor="area">Area (km²)</Label>
            <Input
              id="area"
              type="number"
              step="0.01"
              value={formValues.area}
              onChange={(e) => setFormValues({ ...formValues, area: e.target.value })}
              placeholder="Enter area in square kilometers"
            />
          </div>
          <div>
            <Label htmlFor="description">Description</Label>
            <Input
              id="description"
              value={formValues.description}
              onChange={(e) => setFormValues({ ...formValues, description: e.target.value })}
              placeholder="Enter description"
            />
          </div>
          <div>
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
                  Clear
                </Button>
              )}
            </div>
            <p className="text-xs text-muted-foreground mt-1">
              Upload a GeoJSON file defining the municipality boundaries
            </p>
          </div>
          <div className="flex justify-end gap-2 pt-4">
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
        title="Edit Municipality"
        description="Update municipality information">
        <div className="space-y-4">
          <div>
            <Label htmlFor="edit-name">Name *</Label>
            <Input
              id="edit-name"
              value={formValues.name}
              onChange={(e) => setFormValues({ ...formValues, name: e.target.value })}
              placeholder="Enter municipality name"
            />
          </div>
          <div>
            <Label htmlFor="edit-province">Province *</Label>
            <Select
              value={formValues.provinceId}
              onValueChange={(value) => setFormValues({ ...formValues, provinceId: value })}>
              <SelectTrigger>
                <SelectValue placeholder="Select province" />
              </SelectTrigger>
              <SelectContent>
                {provinces.map((province) => (
                  <SelectItem key={province.id} value={province.id}>
                    {province.name}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
          <div className="flex items-center space-x-2">
            <Checkbox
              id="edit-isCity"
              checked={formValues.isCity}
              onCheckedChange={(checked) => setFormValues({ ...formValues, isCity: checked as boolean })}
            />
            <Label htmlFor="edit-isCity">This is a city (not a municipality)</Label>
          </div>
          <div>
            <Label htmlFor="edit-code">Code</Label>
            <Input
              id="edit-code"
              value={formValues.code}
              onChange={(e) => setFormValues({ ...formValues, code: e.target.value })}
              placeholder="Enter municipality code"
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
              placeholder="Enter area in square kilometers"
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
          <div className="flex justify-end gap-2 pt-4">
            <Button variant="outline" onClick={() => setEditDialogOpen(false)}>
              Cancel
            </Button>
            <Button onClick={handleEdit} disabled={isEditing}>
              {isEditing ? "Updating..." : "Update"}
            </Button>
          </div>
        </div>
      </ResponsiveDialog>

      {/* Delete Dialog */}
      <AlertDialog open={deleteDialogOpen} onOpenChange={setDeleteDialogOpen}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Are you sure?</AlertDialogTitle>
            <AlertDialogDescription>
              This will permanently delete the municipality "{selectedMunicipality?.name}". This action cannot be
              undone.
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
