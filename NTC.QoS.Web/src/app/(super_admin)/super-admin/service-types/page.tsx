"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { Briefcase, Pencil, Plus, Search, Trash2 } from "lucide-react";
import { toast } from "sonner";

import { useCreateService } from "@/components/features/service/hooks/useCreateService";
import { useDeleteService } from "@/components/features/service/hooks/useDeleteService";
import { useEditService } from "@/components/features/service/hooks/useEditService";
import { useGetServices } from "@/components/features/service/hooks/useGetServices";
import type { Service } from "@/components/features/service/types/Service";
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
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";

export default function ServiceTypesManagementPage() {
  const router = useRouter();
  const [createDialogOpen, setCreateDialogOpen] = useState(false);
  const [editDialogOpen, setEditDialogOpen] = useState(false);
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
  const [selectedService, setSelectedService] = useState<Service | null>(null);
  const [searchQuery, setSearchQuery] = useState("");
  const [formValues, setFormValues] = useState({ name: "", type: "" });

  const {
    data: servicesData,
    isLoading,
    refetch,
  } = useGetServices({ pathParams: {}, queryParams: { pageNumber: 1, pageSize: 1000 } });

  const { mutateAsync: createService, isPending: isCreating } = useCreateService();
  const { mutateAsync: editService, isPending: isEditing } = useEditService();
  const { mutateAsync: deleteService, isPending: isDeleting } = useDeleteService();

  const services = servicesData?.data ?? [];

  const filteredServices = services.filter((service) => {
    if (!searchQuery) return true;
    const query = searchQuery.toLowerCase();
    return service.name?.toLowerCase().includes(query) || service.type?.toLowerCase().includes(query);
  });

  const handleCreate = async () => {
    if (!formValues.name || !formValues.type) {
      toast.error("Please fill in all required fields");
      return;
    }

    try {
      await createService({ pathParams: {}, payload: { name: formValues.name, type: formValues.type } });
      toast.success("Service type created successfully");
      setCreateDialogOpen(false);
      setFormValues({ name: "", type: "" });
      await refetch();
      router.refresh();
    } catch (error) {
      toast.error("Failed to create service type");
    }
  };

  const handleEdit = async () => {
    if (!selectedService || !formValues.name || !formValues.type) {
      toast.error("Please fill in all required fields");
      return;
    }

    try {
      await editService({
        pathParams: { id: selectedService.id },
        payload: { name: formValues.name, type: formValues.type },
      });
      toast.success("Service type updated successfully");
      setEditDialogOpen(false);
      setSelectedService(null);
      await refetch();
      router.refresh();
    } catch (error) {
      toast.error("Failed to update service type");
    }
  };

  const handleDelete = async () => {
    if (!selectedService) return;

    try {
      await deleteService({ pathParams: { id: selectedService.id } });
      toast.success("Service type deleted successfully");
      setDeleteDialogOpen(false);
      setSelectedService(null);
      await refetch();
      router.refresh();
    } catch (error) {
      toast.error("Failed to delete service type");
    }
  };

  const openEditDialog = (service: Service) => {
    setSelectedService(service);
    setFormValues({ name: service.name || "", type: service.type || "" });
    setEditDialogOpen(true);
  };

  const openDeleteDialog = (service: Service) => {
    setSelectedService(service);
    setDeleteDialogOpen(true);
  };

  return (
    <div className="container mx-auto px-4 py-8">
      <Card>
        <CardHeader>
          <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
            <div>
              <CardTitle className="flex items-center gap-2">
                <Briefcase className="h-6 w-6" />
                Service Types Management
              </CardTitle>
              <CardDescription>Manage service types and classifications</CardDescription>
            </div>
            <Button onClick={() => setCreateDialogOpen(true)}>
              <Plus className="h-4 w-4 mr-2" />
              Add Service Type
            </Button>
          </div>
        </CardHeader>
        <CardContent>
          <div className="mb-4">
            <div className="relative">
              <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-gray-400" />
              <Input
                placeholder="Search service types..."
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                className="pl-10"
              />
            </div>
          </div>

          {isLoading ? (
            <div className="text-center py-8">Loading...</div>
          ) : filteredServices.length === 0 ? (
            <div className="text-center py-8 text-gray-500">
              {searchQuery
                ? "No service types found matching your search."
                : "No service types yet. Create one to get started."}
            </div>
          ) : (
            <div className="overflow-x-auto">
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Name</TableHead>
                    <TableHead>Type</TableHead>
                    <TableHead className="text-right">Actions</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {filteredServices.map((service) => (
                    <TableRow key={service.id}>
                      <TableCell className="font-medium">{service.name}</TableCell>
                      <TableCell>
                        <Badge variant="outline">{service.type}</Badge>
                      </TableCell>
                      <TableCell className="text-right">
                        <div className="flex justify-end gap-2">
                          <Button variant="ghost" size="sm" onClick={() => openEditDialog(service)}>
                            <Pencil className="h-4 w-4" />
                          </Button>
                          <Button variant="ghost" size="sm" onClick={() => openDeleteDialog(service)}>
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
        title="Create Service Type"
        description="Add a new service type to the system">
        <div className="space-y-4">
          <div>
            <Label htmlFor="create-name">Name *</Label>
            <Input
              id="create-name"
              value={formValues.name}
              onChange={(e) => setFormValues({ ...formValues, name: e.target.value })}
              placeholder="Enter service name"
            />
          </div>
          <div>
            <Label htmlFor="create-type">Type *</Label>
            <Input
              id="create-type"
              value={formValues.type}
              onChange={(e) => setFormValues({ ...formValues, type: e.target.value })}
              placeholder="Enter service type"
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
        title="Edit Service Type"
        description="Update service type information">
        <div className="space-y-4">
          <div>
            <Label htmlFor="edit-name">Name *</Label>
            <Input
              id="edit-name"
              value={formValues.name}
              onChange={(e) => setFormValues({ ...formValues, name: e.target.value })}
              placeholder="Enter service name"
            />
          </div>
          <div>
            <Label htmlFor="edit-type">Type *</Label>
            <Input
              id="edit-type"
              value={formValues.type}
              onChange={(e) => setFormValues({ ...formValues, type: e.target.value })}
              placeholder="Enter service type"
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
              This will permanently delete &quot;{selectedService?.name}&quot;. This action cannot be undone.
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
