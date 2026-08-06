"use client";

import React, { useMemo, useState } from "react";
import { zodResolver } from "@hookform/resolvers/zod";
import * as maptilersdk from "@maptiler/sdk";
import { useQueryClient } from "@tanstack/react-query";
import { Loader2, LocateFixed } from "lucide-react";
import { useForm } from "react-hook-form";
import { toast } from "sonner";
import { z } from "zod";

import { type Barangay } from "@/components/features/barangay/types/Barangay";
import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox"; // For isVerified
import { Form, FormControl, FormField, FormItem, FormLabel, FormMessage } from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import {
  Select,
  SelectContent,
  SelectGroup,
  SelectItem,
  SelectLabel,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { createMutationHookFromTemplate } from "@/hooks/createMutationHookFromTemplate";
import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";
import { getServiceProvidersRequest } from "../../service-provider/handlers/getServiceProviders";
import { type ServiceProvider } from "../../service-provider/types/serviceProvider";
import { createCellRequest } from "../handlers/createCellRequest";
import { type Cell } from "../types/Cell";

const useServiceProviders = createQueryHookFromTemplate(getServiceProvidersRequest);
const useCreateCell = createMutationHookFromTemplate(createCellRequest);

interface AddCellSiteBasicInfoFormProps {
  setCellName?: (name: string) => void;
  tabsContent?: string;
  setTabsContent?: (content: string) => void;
  barangays?: Barangay;
  onClose?: () => void;
  onCreated?: (cell: Cell) => void;
  enableMultiStepFlow?: boolean;
}

const addCellSiteFormSchema = z.object({
  siteName: z.string().min(1, "Site name is required").max(200, "Name must not exceed 200 characters"),
  siteId: z.string().optional(),
  group: z.string(), // Auto-filled from service provider name
  description: z.string().optional(),
  address: z.string().optional(),
  towerType: z.string().optional(),
  elevation: z.string().optional(),
  latitude: z
    .string()
    .min(1, "Latitude is required")
    .refine((val) => Number(val) >= -90 && Number(val) <= 90, "Latitude must be between -90 and 90"),
  longitude: z
    .string()
    .min(1, "Longitude is required")
    .refine((val) => Number(val) >= -180 && Number(val) <= 180, "Longitude must be between -180 and 180"),
  serviceProviderId: z.string().min(1, "Service provider is required"),
  cid: z.string().optional(),
  lac: z.string().optional(),
  tac: z.string().optional(),
  status: z.enum(["Pending", "Licensed", "Unknown"]),
  isVerified: z.boolean(),
});

const AddCellSiteBasicInfoForm = ({
  setCellName,
  tabsContent,
  setTabsContent,
  onClose,
  onCreated,
  enableMultiStepFlow = false,
}: AddCellSiteBasicInfoFormProps) => {
  const queryClient = useQueryClient();
  const { mutateAsync, isPending } = useCreateCell();
  const [isLocating, setIsLocating] = useState(false);

  const form = useForm<z.infer<typeof addCellSiteFormSchema>>({
    resolver: zodResolver(addCellSiteFormSchema),
    defaultValues: {
      siteName: "",
      siteId: "",
      group: "",
      description: "",
      address: "",
      towerType: "",
      elevation: "",
      latitude: "",
      longitude: "",
      serviceProviderId: "",
      cid: "",
      lac: "",
      tac: "",
      status: "Unknown",
      isVerified: false,
    },
    mode: "onChange",
  });

  const { data: serviceProviders } = useServiceProviders({
    pathParams: {},
    queryParams: { pageSize: 100, pageNumber: 1 },
  });

  const wirelessServiceProviders = useMemo(
    () => serviceProviders?.data?.filter((provider) => provider.service.type === "Wireless"),
    [serviceProviders?.data],
  );

  // Auto-fill group when service provider changes
  const handleServiceProviderChange = (providerId: string) => {
    const selectedProvider = wirelessServiceProviders?.find((sp) => sp.id === providerId);
    if (selectedProvider) {
      form.setValue("group", selectedProvider.provider.name);
      form.setValue("serviceProviderId", providerId);
    }
  };

  const handleGetCurrentLocation = async () => {
    maptilersdk.config.apiKey = "oNJz21LbFqXa9sZWro72";

    if (!navigator.geolocation) {
      toast.error("Geolocation is not supported by your browser.");
      return;
    }

    setIsLocating(true);

    try {
      const position = await new Promise<GeolocationPosition>((resolve, reject) => {
        navigator.geolocation.getCurrentPosition(resolve, reject);
      });

      const lat = position.coords.latitude.toFixed(6);
      const lng = position.coords.longitude.toFixed(6);

      const response = await maptilersdk.geocoding.reverse([Number(lng), Number(lat)]);
      const mostSpecific = response.features[0];

      if (mostSpecific?.place_name) {
        form.setValue("address", mostSpecific.place_name);
      }
      form.setValue("latitude", lat);
      form.setValue("longitude", lng);
      toast.success("Current location retrieved!");
    } catch {
      toast.error("Unable to retrieve your location or location details.");
    } finally {
      setIsLocating(false);
    }
  };

  const onSubmit = async (values: z.infer<typeof addCellSiteFormSchema>) => {
    try {
      const response = await mutateAsync({
        pathParams: {},
        payload: {
          name: values.siteName,
          group: values.group,
          description: values.description,
          address: values.address,
          serviceProviderId: values.serviceProviderId,
          cid: values.cid,
          lac: values.lac,
          tac: values.tac,
          siteId: values.siteId,
          towerType: values.towerType,
          elevation: values.elevation ? Number(values.elevation) : undefined,
          latitude: Number(values.latitude),
          longitude: Number(values.longitude),
          status: values.status,
        },
      });

      void queryClient.invalidateQueries({ predicate: (query) => query.queryKey[0] === "get-cells", refetchType: "active" });

      toast.success("Cell site created successfully!");
      setCellName?.(values.siteName);
      onCreated?.(response.data);

      if (enableMultiStepFlow) {
        setTabsContent?.("cell-sector");
      } else {
        onClose?.();
      }
    } catch (error) {
      console.error("Error adding cell site:", error);
      toast.error("Failed to create cell site.");
    }
  };

  return (
    <Form {...form}>
      <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-6">
        {/* Site Name & Site ID */}
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <FormField
            control={form.control}
            name="siteName"
            render={({ field }) => (
              <FormItem>
                <FormLabel>
                  Site Name <span className="text-red-500">*</span>
                </FormLabel>
                <FormControl>
                  <Input placeholder="Enter cell site name" {...field} />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />
          <FormField
            control={form.control}
            name="siteId"
            render={({ field }) => (
              <FormItem>
                <FormLabel>Site ID</FormLabel>
                <FormControl>
                  <Input placeholder="Optional site ID" {...field} />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />
        </div>

        {/* Latitude & Longitude */}
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <FormField
            control={form.control}
            name="latitude"
            render={({ field }) => (
              <FormItem>
                <FormLabel>
                  Latitude <span className="text-red-500">*</span>
                </FormLabel>
                <FormControl>
                  <Input placeholder="Ex. 8.567200" {...field} />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />
          <div className="flex items-end gap-2">
            <FormField
              control={form.control}
              name="longitude"
              render={({ field }) => (
                <FormItem className="flex-grow">
                  <FormLabel>
                    Longitude <span className="text-red-500">*</span>
                  </FormLabel>
                  <FormControl>
                    <Input placeholder="Ex. 124.123400" {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
            <Button
              type="button"
              size="icon"
              variant="outline"
              onClick={handleGetCurrentLocation}
              title="Use current location"
              disabled={isLocating}>
              {isLocating ? <Loader2 className="h-4 w-4 animate-spin" /> : <LocateFixed className="h-4 w-4" />}
            </Button>
          </div>
        </div>

        {/* Address */}
        <FormField
          control={form.control}
          name="address"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Address</FormLabel>
              <FormControl>
                <Input placeholder="Ex. Province, Municipality, Barangay" {...field} />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />

        {/* Tower Type & Elevation */}
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <FormField
            control={form.control}
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
          <FormField
            control={form.control}
            name="elevation"
            render={({ field }) => (
              <FormItem>
                <FormLabel>Elevation</FormLabel>
                <div className="flex items-center space-x-2">
                  <FormControl>
                    <Input placeholder="Ex. 25" {...field} />
                  </FormControl>
                  <span className="text-sm text-gray-500">Meters</span>
                </div>
              </FormItem>
            )}
          />
        </div>

        {/* Service Provider & Group (auto-filled) */}
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <FormField
            control={form.control}
            name="serviceProviderId"
            render={({ field }) => (
              <FormItem>
                <FormLabel>
                  Service Provider <span className="text-red-500">*</span>
                </FormLabel>
                <FormControl>
                  <Select
                    onValueChange={handleServiceProviderChange}
                    value={field.value}
                    defaultValue={field.value}>
                    <SelectTrigger className="w-full">
                      <SelectValue placeholder="Select a wireless provider" />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectGroup>
                        <SelectLabel>Wireless Providers</SelectLabel>
                        {wirelessServiceProviders?.map((sp: ServiceProvider) => (
                          <SelectItem key={sp.id} value={sp.id}>
                            {sp.provider.name}
                          </SelectItem>
                        ))}
                      </SelectGroup>
                    </SelectContent>
                  </Select>
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />
          <FormField
            control={form.control}
            name="group"
            render={({ field }) => (
              <FormItem>
                <FormLabel>Group</FormLabel>
                <FormControl>
                  <Input placeholder="Auto-filled from provider" {...field} readOnly className="bg-gray-50" />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />
        </div>

        {/* Description */}
        <FormField
          control={form.control}
          name="description"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Description</FormLabel>
              <FormControl>
                <Input placeholder="Optional description" {...field} />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />

        {/* CID, LAC, and TAC */}
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          <FormField
            control={form.control}
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
            control={form.control}
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
            control={form.control}
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

        {/* Status & Is Verified */}
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <FormField
            control={form.control}
            name="status"
            render={({ field }) => (
              <FormItem>
                <FormLabel>
                  Status <span className="text-red-500">*</span>
                </FormLabel>
                <FormControl>
                  <Select onValueChange={field.onChange} value={field.value}>
                    <SelectTrigger className="w-full">
                      <SelectValue placeholder="Select Status" />
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
          <FormField
            control={form.control}
            name="isVerified"
            render={({ field }) => (
              <FormItem className="flex flex-row items-end space-x-3 space-y-0 pb-2">
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
        </div>

        {/* Buttons */}
        <div className="flex justify-end gap-2 pt-4">
          {tabsContent === "basic-information" && (
            <Button type="button" variant="outline" onClick={() => onClose?.()}>
              Cancel
            </Button>
          )}
          {tabsContent === "basic-information" && (
            <Button type="submit" className="bg-blue-500 text-white hover:bg-blue-600">
              {isPending ? "Creating..." : enableMultiStepFlow ? "Next: Cell Sector" : "Create"}
            </Button>
          )}
        </div>
      </form>
    </Form>
  );
};

export default AddCellSiteBasicInfoForm;
