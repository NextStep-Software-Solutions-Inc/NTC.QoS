"use client";

import React, { useState } from "react";
import { zodResolver } from "@hookform/resolvers/zod";
import { useQueryClient } from "@tanstack/react-query";
import { ChevronDown, Loader2 } from "lucide-react";
import { useForm } from "react-hook-form";
import { toast } from "sonner";
import { z } from "zod";

import { ResponsiveDialog } from "@/components/ResponsiveDialog";
import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
import { Form, FormControl, FormField, FormItem, FormLabel, FormMessage } from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { createMutationHookFromTemplate } from "@/hooks/createMutationHookFromTemplate";
import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";
import { editCellRequest } from "../handlers/editCellRequest";
import { editCellStatusRequest } from "../handlers/editCellStatusRequest";
import { getCellTechnologiesRequest } from "../handlers/getCellTechnologiesRequest";
import { updateCellTechnologiesRequest } from "../handlers/updateCellTechnologiesRequest";
import { getServiceProvidersRequest } from "../../service-provider/handlers/getServiceProviders";
import { getTechnologiesRequest } from "../../technology/handlers/getTechnologiesRequest";
import { type ServiceProviderDetails } from "../../service-provider/types/serviceProvider";
import { type Technology } from "../../technology/types/Technology";
import { type CellTechnology } from "../types/Cell";
import { type Cell } from "../types/Cell";

const useEditCell = createMutationHookFromTemplate(editCellRequest);
const useEditCellStatus = createMutationHookFromTemplate(editCellStatusRequest);
const useGetCellTechnologies = createQueryHookFromTemplate(getCellTechnologiesRequest);
const useUpdateCellTechnologies = createMutationHookFromTemplate(updateCellTechnologiesRequest);
const useServiceProviders = createQueryHookFromTemplate(getServiceProvidersRequest);
const useGetTechnologies = createQueryHookFromTemplate(getTechnologiesRequest);

const editCellFormSchema = z.object({
  serviceProviderId: z.string().min(1, "Service provider is required"),
  group: z.string().min(1, "Group is required"),
  address: z.string().optional(),
  cid: z.string().optional(),
  lac: z.string().optional(),
  tac: z.string().optional(),
  siteId: z.string().optional(),
  towerType: z.string().optional(),
  elevation: z.string().optional(),
  latitude: z
    .string()
    .optional()
    .refine(
      (val) => !val || val === "" || (Number(val) >= -90 && Number(val) <= 90),
      "Latitude must be between -90 and 90",
    ),
  longitude: z
    .string()
    .optional()
    .refine(
      (val) => !val || val === "" || (Number(val) >= -180 && Number(val) <= 180),
      "Longitude must be between -180 and 180",
    ),
  isVerified: z.boolean().optional(),
});

const statusFormSchema = z.object({
  status: z.enum(["Pending", "Licensed", "Unknown"]),
});

interface EditCellDialogProps {
  cell: Cell;
  open?: boolean;
  onOpenChange?: (open: boolean) => void;
  trigger?: React.ReactNode;
}

export function EditCellDialog({ cell, open, onOpenChange, trigger }: EditCellDialogProps) {
  const queryClient = useQueryClient();
  const [activeTab, setActiveTab] = useState<string>("details");
  const [selectedTechnologyIds, setSelectedTechnologyIds] = useState<string[]>([]);
  const [technologiesInitialized, setTechnologiesInitialized] = useState(false);

  const { mutateAsync: updateCell, isPending: isUpdatingCell } = useEditCell();
  const { mutateAsync: updateStatus, isPending: isUpdatingStatus } = useEditCellStatus();
  const { mutateAsync: updateTechnologies, isPending: isUpdatingTechnologies } = useUpdateCellTechnologies();

  // Fetch service providers (wireless only)
  const { data: serviceProviders } = useServiceProviders({
    pathParams: {},
    queryParams: { pageSize: 100, pageNumber: 1 },
  });
  const wirelessServiceProviders = React.useMemo(
    () => serviceProviders?.data?.filter((sp: ServiceProviderDetails) => sp.service.type === "Wireless") ?? [],
    [serviceProviders?.data],
  );

  // Fetch all available technologies
  const { data: allTechnologiesResponse } = useGetTechnologies({
    pathParams: {},
    queryParams: { pageSize: 100, pageNumber: 1 },
  });
  const allTechnologies = allTechnologiesResponse?.data ?? [];

  // Fetch current cell technologies
  const { data: cellTechnologiesResponse } = useGetCellTechnologies({
    pathParams: { id: cell.id },
    queryParams: {},
  });

  // Initialize selected technologies from current cell technologies;
  // reset when the dialog opens to avoid stale state from a previous cell
  React.useEffect(() => {
    if (!open) {
      setTechnologiesInitialized(false);
    }
  }, [open]);

  React.useEffect(() => {
    if (cellTechnologiesResponse?.data && !technologiesInitialized) {
      setSelectedTechnologyIds(cellTechnologiesResponse.data.map((t: CellTechnology) => t.technologyId));
      setTechnologiesInitialized(true);
    }
  }, [cellTechnologiesResponse, technologiesInitialized]);

  const detailsForm = useForm<z.infer<typeof editCellFormSchema>>({
    resolver: zodResolver(editCellFormSchema),
    defaultValues: {
      serviceProviderId: cell.serviceProviderId ?? "",
      group: cell.group ?? cell.serviceProvider?.provider?.name ?? "",
      address: cell.address ?? "",
      cid: cell.cid ?? "",
      lac: cell.lac ?? "",
      tac: cell.tac ?? "",
      siteId: cell.siteId ?? "",
      towerType: cell.towerType ?? "",
      elevation: cell.elevation?.toString() ?? "",
      latitude: cell.latitude?.toString() ?? "",
      longitude: cell.longitude?.toString() ?? "",
      isVerified: cell.isVerified ?? false,
    },
    mode: "onChange",
  });

  const statusForm = useForm<z.infer<typeof statusFormSchema>>({
    resolver: zodResolver(statusFormSchema),
    defaultValues: {
      status: (cell.status as "Pending" | "Licensed" | "Unknown") ?? "Unknown",
    },
    mode: "onChange",
  });

  const handleServiceProviderChange = (providerId: string) => {
    const selectedProvider = wirelessServiceProviders.find((sp: ServiceProviderDetails) => sp.id === providerId);
    if (selectedProvider) {
      // Auto-fill group from provider name (intentional; user can still override via the Group input)
      detailsForm.setValue("group", selectedProvider.provider.name, { shouldValidate: true });
      detailsForm.setValue("serviceProviderId", providerId, { shouldValidate: true });
    }
  };

  const handleTechnologyToggle = (technologyId: string) => {
    setSelectedTechnologyIds((prev) =>
      prev.includes(technologyId) ? prev.filter((id) => id !== technologyId) : [...prev, technologyId],
    );
  };

  const onSubmitDetails = async (values: z.infer<typeof editCellFormSchema>) => {
    try {
      await updateCell({
        pathParams: { id: cell.id },
        payload: {
          serviceProviderId: values.serviceProviderId,
          group: values.group,
          address: values.address,
          cid: values.cid,
          lac: values.lac,
          tac: values.tac,
          siteId: values.siteId,
          towerType: values.towerType,
          elevation: values.elevation ? Number(values.elevation) : undefined,
          latitude: values.latitude ? Number(values.latitude) : undefined,
          longitude: values.longitude ? Number(values.longitude) : undefined,
          isVerified: values.isVerified,
        },
      });
    } catch (error) {
      console.error("Error updating cell site details:", error);
      toast.error("Failed to update cell site details.");
      return;
    }

    // Update technologies separately; report partial failure without closing dialog
    try {
      await updateTechnologies({
        pathParams: { id: cell.id },
        payload: { technologyIds: selectedTechnologyIds },
      });
    } catch (error) {
      console.error("Error updating cell technologies:", error);
      void queryClient.invalidateQueries({ predicate: (query) => query.queryKey[0] === "get-cells", refetchType: "active" });
      toast.error("Cell site details saved, but failed to update technologies.");
      onOpenChange?.(false);
      return;
    }

    void queryClient.invalidateQueries({ predicate: (query) => query.queryKey[0] === "get-cells", refetchType: "active" });
    void queryClient.invalidateQueries({ predicate: (query) => query.queryKey[0] === "cell-technologies", refetchType: "active" });
    toast.success("Cell site details updated successfully!");
    onOpenChange?.(false);
  };

  const onSubmitStatus = async (values: z.infer<typeof statusFormSchema>) => {
    try {
      await updateStatus({
        pathParams: { id: cell.id },
        payload: values.status,
      });

      void queryClient.invalidateQueries({ predicate: (query) => query.queryKey[0] === "get-cells", refetchType: "active" });
      toast.success("Cell site status updated successfully!");
      onOpenChange?.(false);
    } catch (error) {
      console.error("Error updating cell status:", error);
      toast.error("Failed to update cell status.");
    }
  };

  return (
    <ResponsiveDialog
      open={open}
      onOpenChange={onOpenChange}
      trigger={trigger}
      title={`Edit Cell Site: ${cell.name}`}
      description="Update cell site details or change its status"
      className="sm:max-w-[700px]">
      <div className="w-full overflow-y-auto flex-1">
        <Tabs value={activeTab} onValueChange={setActiveTab} className="w-full">
          <TabsList className="grid w-full grid-cols-2">
            <TabsTrigger value="details">Site Details</TabsTrigger>
            <TabsTrigger value="status">Status</TabsTrigger>
          </TabsList>

          <TabsContent value="details" className="space-y-4 mt-4">
            <Form {...detailsForm}>
              <form onSubmit={detailsForm.handleSubmit(onSubmitDetails)} className="space-y-6">

              {/* Service Provider & Group */}
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <FormField
                  control={detailsForm.control}
                  name="serviceProviderId"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>
                        Service Provider <span className="text-red-500">*</span>
                      </FormLabel>
                      <FormControl>
                        <Select onValueChange={handleServiceProviderChange} value={field.value}>
                          <SelectTrigger className="w-full">
                            <SelectValue placeholder="Select a wireless provider" />
                          </SelectTrigger>
                          <SelectContent>
                            {wirelessServiceProviders.map((sp: ServiceProviderDetails) => (
                              <SelectItem key={sp.id} value={sp.id}>
                                {sp.provider.name}
                              </SelectItem>
                            ))}
                          </SelectContent>
                        </Select>
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />
                <FormField
                  control={detailsForm.control}
                  name="group"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>
                        Group <span className="text-red-500">*</span>
                      </FormLabel>
                      <FormControl>
                        <Input placeholder="e.g. Region 1" {...field} />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />
              </div>

              {/* Address */}
              <FormField
                control={detailsForm.control}
                name="address"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>Address</FormLabel>
                    <FormControl>
                      <Input placeholder="Full street address" {...field} />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />

              {/* Technologies */}
              <div>
                <FormLabel className="text-sm font-medium">Technologies</FormLabel>
                <p className="text-xs text-muted-foreground mb-2">Select the network technologies available at this cell site.</p>
                {allTechnologies.length === 0 ? (
                  <p className="text-sm text-muted-foreground italic">No technologies available.</p>
                ) : (
                  <div className="grid grid-cols-2 md:grid-cols-3 gap-2 p-3 border rounded-md">
                    {allTechnologies.map((tech: Technology) => (
                      <label
                        key={tech.id}
                        className="flex items-center gap-2 cursor-pointer p-1.5 rounded hover:bg-muted/50 transition-colors">
                        <Checkbox
                          checked={selectedTechnologyIds.includes(tech.id)}
                          onCheckedChange={() => handleTechnologyToggle(tech.id)}
                        />
                        <span className="text-sm">{tech.standard}</span>
                      </label>
                    ))}
                  </div>
                )}
              </div>

              {/* CID, LAC, TAC */}
              <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                <FormField
                  control={detailsForm.control}
                  name="cid"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>CID</FormLabel>
                      <FormControl>
                        <Input placeholder="Cell ID" {...field} />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />
                <FormField
                  control={detailsForm.control}
                  name="lac"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>LAC</FormLabel>
                      <FormControl>
                        <Input placeholder="Location Area Code" {...field} />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />
                <FormField
                  control={detailsForm.control}
                  name="tac"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>TAC</FormLabel>
                      <FormControl>
                        <Input placeholder="Tracking Area Code" {...field} />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />
              </div>

              {/* Site ID & Tower Type */}
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <FormField
                  control={detailsForm.control}
                  name="siteId"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>Site ID</FormLabel>
                      <FormControl>
                        <Input placeholder="Site identifier" {...field} />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />
                <FormField
                  control={detailsForm.control}
                  name="towerType"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>Tower Type</FormLabel>
                      <FormControl>
                        <Select onValueChange={field.onChange} value={field.value} defaultValue={field.value}>
                          <SelectTrigger className="w-full">
                            <SelectValue placeholder="Select tower type" />
                          </SelectTrigger>
                          <SelectContent>
                            <SelectItem value="Unknown">Unknown</SelectItem>
                            <SelectItem value="Guyed">Guyed</SelectItem>
                            <SelectItem value="SelfSupporting">Self Supporting</SelectItem>
                            <SelectItem value="Monopole">Monopole</SelectItem>
                            <SelectItem value="Stealth">Stealth</SelectItem>
                            <SelectItem value="Rooftop">Rooftop</SelectItem>
                            <SelectItem value="WallMounted">Wall Mounted</SelectItem>
                          </SelectContent>
                        </Select>
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />
              </div>

              {/* Elevation */}
              <FormField
                control={detailsForm.control}
                name="elevation"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>Elevation (meters)</FormLabel>
                    <FormControl>
                      <Input placeholder="Tower elevation" type="number" {...field} />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />

              {/* Latitude & Longitude */}
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <FormField
                  control={detailsForm.control}
                  name="latitude"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>Latitude</FormLabel>
                      <FormControl>
                        <Input placeholder="Ex. 8.567200" {...field} />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />
                <FormField
                  control={detailsForm.control}
                  name="longitude"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>Longitude</FormLabel>
                      <FormControl>
                        <Input placeholder="Ex. 124.123400" {...field} />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />
              </div>

              {/* Is Verified */}
              <FormField
                control={detailsForm.control}
                name="isVerified"
                render={({ field }) => (
                  <FormItem className="flex flex-row items-start space-x-3 space-y-0 rounded-md border p-4">
                    <FormControl>
                      <Checkbox checked={field.value} onCheckedChange={field.onChange} />
                    </FormControl>
                    <div className="space-y-1 leading-none">
                      <FormLabel className="font-normal">
                        Mark as verified - Check this if all site details are confirmed and the site is active
                      </FormLabel>
                    </div>
                  </FormItem>
                )}
              />

              {/* Buttons */}
              <div className="flex justify-end gap-2 pt-4">
                <Button type="button" variant="outline" onClick={() => onOpenChange?.(false)}>
                  Cancel
                </Button>
                <Button type="submit" disabled={isUpdatingCell || isUpdatingTechnologies}>
                  {isUpdatingCell || isUpdatingTechnologies ? (
                    <>
                      <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                      Updating...
                    </>
                  ) : (
                    "Update Details"
                  )}
                </Button>
              </div>
            </form>
          </Form>
        </TabsContent>

        <TabsContent value="status" className="space-y-4 mt-4">
          <Form {...statusForm}>
            <form onSubmit={statusForm.handleSubmit(onSubmitStatus)} className="space-y-6">
              <div className="bg-blue-50 dark:bg-blue-950/20 p-4 rounded-lg">
                <p className="text-sm text-blue-800 dark:text-blue-200">
                  Update the operational status of this cell site. This action uses a separate endpoint specifically
                  for status management.
                </p>
              </div>

              <FormField
                control={statusForm.control}
                name="status"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>
                      Cell Site Status <span className="text-red-500">*</span>
                    </FormLabel>
                    <FormControl>
                      <Select onValueChange={field.onChange} value={field.value}>
                        <SelectTrigger className="w-full">
                          <SelectValue placeholder="Select status" />
                          <ChevronDown className="h-4 w-4 opacity-50" />
                        </SelectTrigger>
                        <SelectContent>
                          <SelectItem value="Unknown">Unknown</SelectItem>
                          <SelectItem value="Pending">Pending</SelectItem>
                          <SelectItem value="Licensed">Licensed</SelectItem>
                        </SelectContent>
                      </Select>
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />

              <div className="flex justify-end gap-2 pt-4">
                <Button type="button" variant="outline" onClick={() => onOpenChange?.(false)}>
                  Cancel
                </Button>
                <Button type="submit" disabled={isUpdatingStatus}>
                  {isUpdatingStatus ? (
                    <>
                      <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                      Updating...
                    </>
                  ) : (
                    "Update Status"
                  )}
                </Button>
              </div>
            </form>
          </Form>
        </TabsContent>
      </Tabs>
      </div>
    </ResponsiveDialog>
  );
}
