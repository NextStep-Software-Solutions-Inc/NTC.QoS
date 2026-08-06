"use client";

import { useState } from "react";
import { Building2, Pencil, Plus, Search, Trash2 } from "lucide-react";
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
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";

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

  const {
    data: municipalitiesData,
    isLoading,
    refetch,
  } = useGetMunicipalities({ pathParams: {}, queryParams: { pageNumber: 1, pageSize: 1000 } });

  const { data: provincesData } = useGetProvinces({ pathParams: {}, queryParams: { pageNumber: 1, pageSize: 1000 } });

  const { mutateAsync: createMunicipality, isPending: isCreating } = useCreateMunicipality();
  const { mutateAsync: editMunicipality, isPending: isEditing } = useEditMunicipality();
  const { mutateAsync: deleteMunicipality, isPending: isDeleting } = useDeleteMunicipality();

  const municipalities = municipalitiesData?.data ?? [];
  const provinces = provincesData?.data ?? [];

  const filteredMunicipalities = municipalities.filter((municipality) => {
    if (!searchQuery) return true;
    const query = searchQuery.toLowerCase();
    return (
      municipality.name?.toLowerCase().includes(query) ||
      municipality.code?.toLowerCase().includes(query) ||
      municipality.province?.name?.toLowerCase().includes(query)
    );
  });

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
        },
      });
      toast.success("Municipality created successfully");
      setCreateDialogOpen(false);
      setFormValues({ name: "", code: "", description: "", area: "", provinceId: "", isCity: false });
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
        },
      });
      toast.success("Municipality updated successfully");
      setEditDialogOpen(false);
      setSelectedMunicipality(null);
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
    setEditDialogOpen(true);
  };

  const openDeleteDialog = (municipality: Municipality) => {
    setSelectedMunicipality(municipality);
    setDeleteDialogOpen(true);
  };

  return (
    <div className="container mx-auto px-4 py-8">
      <Card>
        <CardHeader>
          <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
            <div>
              <CardTitle className="flex items-center gap-2">
                <Building2 className="h-6 w-6" />
                Municipalities Management
              </CardTitle>
              <CardDescription>Manage municipalities and cities in the system</CardDescription>
            </div>
            <Button onClick={() => setCreateDialogOpen(true)}>
              <Plus className="h-4 w-4 mr-2" />
              Add Municipality
            </Button>
          </div>
        </CardHeader>
        <CardContent>
          <div className="mb-4">
            <div className="relative">
              <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-gray-400" />
              <Input
                placeholder="Search municipalities..."
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                className="pl-10"
              />
            </div>
          </div>

          {isLoading ? (
            <div className="text-center py-8">Loading...</div>
          ) : filteredMunicipalities.length === 0 ? (
            <div className="text-center py-8 text-gray-500">
              {searchQuery
                ? "No municipalities found matching your search."
                : "No municipalities yet. Create one to get started."}
            </div>
          ) : (
            <div className="overflow-x-auto">
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Name</TableHead>
                    <TableHead>Code</TableHead>
                    <TableHead>Type</TableHead>
                    <TableHead>Province</TableHead>
                    <TableHead>Area (km²)</TableHead>
                    <TableHead className="text-right">Actions</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {filteredMunicipalities.map((municipality) => (
                    <TableRow key={municipality.id}>
                      <TableCell className="font-medium">{municipality.name}</TableCell>
                      <TableCell>{municipality.code && <Badge variant="outline">{municipality.code}</Badge>}</TableCell>
                      <TableCell>
                        <Badge variant={municipality.isCity ? "default" : "secondary"}>
                          {municipality.isCity ? "City" : "Municipality"}
                        </Badge>
                      </TableCell>
                      <TableCell>{municipality.province?.name || "N/A"}</TableCell>
                      <TableCell>{municipality.area?.toFixed(2) || "N/A"}</TableCell>
                      <TableCell className="text-right">
                        <div className="flex justify-end gap-2">
                          <Button variant="ghost" size="sm" onClick={() => openEditDialog(municipality)}>
                            <Pencil className="h-4 w-4" />
                          </Button>
                          <Button variant="ghost" size="sm" onClick={() => openDeleteDialog(municipality)}>
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
        </CardContent>
      </Card>

      {/* Create Dialog */}
      <ResponsiveDialog
        open={createDialogOpen}
        onOpenChange={setCreateDialogOpen}
        title="Create Municipality"
        description="Add a new municipality to the system">
        <div className="space-y-4">
          <div>
            <Label htmlFor="create-name">Name *</Label>
            <Input
              id="create-name"
              value={formValues.name}
              onChange={(e) => setFormValues({ ...formValues, name: e.target.value })}
              placeholder="Enter municipality name"
            />
          </div>
          <div>
            <Label htmlFor="create-province">Province *</Label>
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
              id="create-isCity"
              checked={formValues.isCity}
              onCheckedChange={(checked) => setFormValues({ ...formValues, isCity: checked as boolean })}
            />
            <Label htmlFor="create-isCity">Is City</Label>
          </div>
          <div>
            <Label htmlFor="create-code">Code</Label>
            <Input
              id="create-code"
              value={formValues.code}
              onChange={(e) => setFormValues({ ...formValues, code: e.target.value })}
              placeholder="Enter municipality code"
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
            <Label htmlFor="edit-province">Province</Label>
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
            <Label htmlFor="edit-isCity">Is City</Label>
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
              This will permanently delete &quot;{selectedMunicipality?.name}&quot;. This action cannot be undone.
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
