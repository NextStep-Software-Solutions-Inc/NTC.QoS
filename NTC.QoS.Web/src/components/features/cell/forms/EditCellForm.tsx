import React from "react";
import { zodResolver } from "@hookform/resolvers/zod";
import { useQueryClient } from "@tanstack/react-query";
import * as maptilersdk from "@maptiler/sdk";
import {
  Antenna,
  Building,
  Check,
  FileText,
  Grid,
  Hash,
  Map,
  MapPin,
  Mountain,
  Navigation,
  Radio,
  Save,
  Wrench,
} from "lucide-react";
import { useForm } from "react-hook-form";
import { toast } from "sonner";
import { z } from "zod";

import SubmitButton from "@/components/SubmitButton";
import { Button } from "@/components/ui/button";
import { Form, FormControl, FormField, FormItem, FormLabel, FormMessage } from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { createMutationHookFromTemplate } from "@/hooks/createMutationHookFromTemplate";
import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";
import { editCellRequest } from "../handlers/editCellRequest";
import { getServiceProvidersRequest } from "../../service-provider/handlers/getServiceProviders";
import { type ServiceProvider } from "../../service-provider/types/serviceProvider";
import { type Cell } from "../types/Cell";
import { FormSectionHeader } from "./FormSectionHeader";

const cellFormSchema = z.object({
  name: z.string().min(1, "Name is required"),
  description: z.string().optional(),
  group: z.string().min(1, "Group is required"),
  serviceProviderId: z.string().min(1, "Service provider is required"),
  address: z.string().optional(),
  cid: z.string().optional(),
  lac: z.string().optional(),
  siteId: z.string().optional(),
  towerType: z.string().optional(),
  elevation: z.coerce.number(),
  latitude: z.coerce.number().refine((val) => val >= -90 && val <= 90, "Latitude must be between -90 and 90"),
  longitude: z.coerce.number().refine((val) => val >= -180 && val <= 180, "Longitude must be between -180 and 180"),
  status: z.enum(["Pending", "Licensed", "Unknown"]),
});

interface EditCellFormProps {
  cell?: Cell;
  onClose?: () => void;
}

const STATUS_OPTIONS = ["Pending", "Licensed", "Unknown"] as const;

function normalizeStatus(value?: string): (typeof STATUS_OPTIONS)[number] {
  if (!value) return "Unknown";
  return STATUS_OPTIONS.includes(value as (typeof STATUS_OPTIONS)[number])
    ? (value as (typeof STATUS_OPTIONS)[number])
    : "Unknown";
}

const useEditCell = createMutationHookFromTemplate(editCellRequest);
const useServiceProviders = createQueryHookFromTemplate(getServiceProvidersRequest);

export default function EditCellForm({ onClose, cell }: EditCellFormProps) {
  const { mutateAsync, isPending } = useEditCell();
  const queryClient = useQueryClient();
  const { data: serviceProviders } = useServiceProviders({
    pathParams: {},
    queryParams: { pageSize: 100, pageNumber: 1 },
  });

  const wirelessServiceProviders = React.useMemo(
    () => serviceProviders?.data?.filter((provider) => provider.service.type === "Wireless") ?? [],
    [serviceProviders?.data],
  );

  const form = useForm<z.infer<typeof cellFormSchema>>({
    resolver: zodResolver(cellFormSchema),
    defaultValues: {
      name: cell?.name ?? "",
      description: cell?.description ?? "",
      group: cell?.group ?? cell?.serviceProvider?.provider?.name ?? "",
      serviceProviderId: cell?.serviceProviderId ?? "",
      address: cell?.address ?? "",
      cid: cell?.cid ?? "",
      lac: cell?.lac ?? "",
      siteId: cell?.siteId ?? "",
      towerType: cell?.towerType ?? "",
      elevation: cell?.elevation ?? 0,
      latitude: cell?.latitude ?? 0,
      longitude: cell?.longitude ?? 0,
      status: normalizeStatus(cell?.status),
    },
    mode: "onChange",
  });

  const handleServiceProviderChange = (providerId: string) => {
    const selectedProvider = wirelessServiceProviders.find((sp) => sp.id === providerId);
    if (selectedProvider) {
      form.setValue("group", selectedProvider.provider.name, { shouldValidate: true });
      form.setValue("serviceProviderId", providerId, { shouldValidate: true });
    }
  };

  const reverseGeocodeAddress = React.useCallback(
    async (lat: number, lng: number) => {
      const apiKey = process.env.NEXT_PUBLIC_MAPTILER_API_KEY;
      if (!apiKey) return;

      if (!Number.isFinite(lat) || !Number.isFinite(lng) || lat === 0 || lng === 0) return;

      try {
        maptilersdk.config.apiKey = apiKey;
        const response = await maptilersdk.geocoding.reverse([lng, lat]);
        const placeName = response.features?.[0]?.place_name;
        if (placeName) {
          form.setValue("address", placeName, { shouldDirty: true, shouldValidate: true });
        }
      } catch {
        // Ignore reverse geocode failures to keep form interaction uninterrupted.
      }
    },
    [form],
  );

  const handleCoordinateBlur = React.useCallback(() => {
    const lat = Number(form.getValues("latitude"));
    const lng = Number(form.getValues("longitude"));
    void reverseGeocodeAddress(lat, lng);
  }, [form, reverseGeocodeAddress]);

  const onSubmit = async (values: z.infer<typeof cellFormSchema>) => {
    toast.promise(mutateAsync({ pathParams: { id: cell?.id ?? "" }, payload: values }), {
      loading: "Updating cell...",
      success: (response) => {
        void queryClient.invalidateQueries({ predicate: (query) => query.queryKey[0] === "get-cells", refetchType: "active" });
        onClose?.();
        return response.message ?? "Cell updated successfully";
      },
      error: (error: unknown) => {
        console.error(error);
        return "Failed to update cell.";
      },
    });
  };

  return (
    <Form {...form}>
      <form onSubmit={form.handleSubmit(onSubmit)} className="flex flex-col">
        <div className="overflow-y-auto max-h-[60vh] pr-2">
          <div className="space-y-6 pb-4 px-1">
            {/* Basic Information Section */}
            <div className="p-4 rounded-xl bg-gradient-to-br from-amber-50 to-orange-50 dark:from-amber-950/20 dark:to-orange-950/20 border border-amber-100 dark:border-amber-900">
              <FormSectionHeader
                icon={Radio}
                title="Basic Information"
                subtitle="Update the cell site identification"
                color="bg-gradient-to-br from-amber-500 to-orange-600"
              />
              <div className="grid grid-cols-2 gap-4">
                <FormField
                  control={form.control}
                  name="name"
                  render={({ field }) => (
                    <FormItem className="col-span-2">
                      <FormLabel className="flex items-center gap-1.5 text-amber-700 dark:text-amber-300">
                        <Antenna className="h-3.5 w-3.5" />
                        Site Name <span className="text-red-500">*</span>
                      </FormLabel>
                      <FormControl>
                        <Input
                          placeholder="Enter cell site name"
                          className="border-amber-200 focus:border-amber-500 dark:border-amber-800"
                          {...field}
                        />
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
                      <FormLabel className="flex items-center gap-1.5 text-amber-700 dark:text-amber-300">
                        <Grid className="h-3.5 w-3.5" />
                        Group
                      </FormLabel>
                      <FormControl>
                        <Input
                          placeholder="e.g. Region 1"
                          className="border-amber-200 focus:border-amber-500 dark:border-amber-800"
                          {...field}
                        />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />
                <FormField
                  control={form.control}
                  name="status"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel className="flex items-center gap-1.5 text-amber-700 dark:text-amber-300">
                        <Check className="h-3.5 w-3.5" />
                        Status <span className="text-red-500">*</span>
                      </FormLabel>
                      <FormControl>
                        <Select onValueChange={field.onChange} value={field.value}>
                          <SelectTrigger className="border-amber-200 focus:border-amber-500 dark:border-amber-800">
                            <SelectValue placeholder="Select status" />
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
                  name="description"
                  render={({ field }) => (
                    <FormItem className="col-span-2">
                      <FormLabel className="flex items-center gap-1.5 text-amber-700 dark:text-amber-300">
                        <FileText className="h-3.5 w-3.5" />
                        Description
                      </FormLabel>
                      <FormControl>
                        <Textarea
                          placeholder="Brief description of the cell site"
                          rows={2}
                          className="resize-none border-amber-200 focus:border-amber-500 dark:border-amber-800"
                          {...field}
                        />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />
              </div>
            </div>

            {/* Location Section */}
            <div className="p-4 rounded-xl bg-gradient-to-br from-cyan-50 to-sky-50 dark:from-cyan-950/20 dark:to-sky-950/20 border border-cyan-100 dark:border-cyan-900">
              <FormSectionHeader
                icon={MapPin}
                title="Location"
                subtitle="Modify the geographic position"
                color="bg-gradient-to-br from-cyan-500 to-sky-600"
              />
              <div className="grid grid-cols-2 gap-4">
                <FormField
                  control={form.control}
                  name="latitude"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel className="flex items-center gap-1.5 text-cyan-700 dark:text-cyan-300">
                        <Navigation className="h-3.5 w-3.5" />
                        Latitude
                      </FormLabel>
                      <FormControl>
                        <Input
                          type="number"
                          step="any"
                          placeholder="e.g. 14.5995"
                          className="border-cyan-200 focus:border-cyan-500 dark:border-cyan-800 font-mono"
                          {...field}
                          onBlur={() => {
                            field.onBlur();
                            handleCoordinateBlur();
                          }}
                        />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />
                <FormField
                  control={form.control}
                  name="longitude"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel className="flex items-center gap-1.5 text-cyan-700 dark:text-cyan-300">
                        <Map className="h-3.5 w-3.5" />
                        Longitude
                      </FormLabel>
                      <FormControl>
                        <Input
                          type="number"
                          step="any"
                          placeholder="e.g. 120.9842"
                          className="border-cyan-200 focus:border-cyan-500 dark:border-cyan-800 font-mono"
                          {...field}
                          onBlur={() => {
                            field.onBlur();
                            handleCoordinateBlur();
                          }}
                        />
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
                      <FormLabel className="flex items-center gap-1.5 text-cyan-700 dark:text-cyan-300">
                        <Mountain className="h-3.5 w-3.5" />
                        Elevation (m)
                      </FormLabel>
                      <FormControl>
                        <Input
                          type="number"
                          placeholder="e.g. 100"
                          className="border-cyan-200 focus:border-cyan-500 dark:border-cyan-800"
                          {...field}
                        />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />
                <FormField
                  control={form.control}
                  name="address"
                  render={({ field }) => (
                    <FormItem className="col-span-2">
                      <FormLabel className="flex items-center gap-1.5 text-cyan-700 dark:text-cyan-300">
                        <Building className="h-3.5 w-3.5" />
                        Address
                      </FormLabel>
                      <FormControl>
                        <Input
                          placeholder="Full street address"
                          className="border-cyan-200 focus:border-cyan-500 dark:border-cyan-800"
                          {...field}
                        />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />
              </div>
            </div>

            {/* Technical Details Section */}
            <div className="p-4 rounded-xl bg-gradient-to-br from-violet-50 to-fuchsia-50 dark:from-violet-950/20 dark:to-fuchsia-950/20 border border-violet-100 dark:border-violet-900">
              <FormSectionHeader
                icon={Wrench}
                title="Technical Details"
                subtitle="Update infrastructure specifications"
                color="bg-gradient-to-br from-violet-500 to-fuchsia-600"
              />
              <div className="grid grid-cols-2 gap-4">
                <FormField
                  control={form.control}
                  name="serviceProviderId"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel className="flex items-center gap-1.5 text-violet-700 dark:text-violet-300">
                        <Radio className="h-3.5 w-3.5" />
                        Service Provider <span className="text-red-500">*</span>
                      </FormLabel>
                      <FormControl>
                        {cell && cell.serviceProvider?.provider?.name ? (
                          <div className="flex items-center h-10 px-3 py-2 border border-violet-200 rounded-md bg-gray-50 dark:bg-gray-950 dark:border-violet-800 opacity-60">
                            <span className="text-sm text-gray-700 dark:text-gray-300">{cell.serviceProvider.provider.name}</span>
                          </div>
                        ) : (
                          <Select onValueChange={handleServiceProviderChange} value={field.value} disabled={!!cell}>
                            <SelectTrigger className={`border-violet-200 focus:border-violet-500 dark:border-violet-800 ${!!cell ? "opacity-60 cursor-not-allowed" : ""}`}>
                              <SelectValue placeholder="Select a wireless provider" />
                            </SelectTrigger>
                            <SelectContent>
                              {wirelessServiceProviders.map((sp: ServiceProvider) => (
                                <SelectItem key={sp.id} value={sp.id}>
                                  {sp.provider.name}
                                </SelectItem>
                              ))}
                            </SelectContent>
                          </Select>
                        )}
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
                      <FormLabel className="flex items-center gap-1.5 text-violet-700 dark:text-violet-300">
                        <Hash className="h-3.5 w-3.5" />
                        Site ID
                      </FormLabel>
                      <FormControl>
                        <Input
                          placeholder="Unique site identifier"
                          className="border-violet-200 focus:border-violet-500 dark:border-violet-800"
                          {...field}
                        />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />
                <FormField
                  control={form.control}
                  name="cid"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel className="flex items-center gap-1.5 text-violet-700 dark:text-violet-300">
                        <Hash className="h-3.5 w-3.5" />
                        CID
                      </FormLabel>
                      <FormControl>
                        <Input
                          placeholder="Cell ID"
                          className="border-violet-200 focus:border-violet-500 dark:border-violet-800"
                          {...field}
                        />
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
                      <FormLabel className="flex items-center gap-1.5 text-violet-700 dark:text-violet-300">
                        <Grid className="h-3.5 w-3.5" />
                        LAC
                      </FormLabel>
                      <FormControl>
                        <Input
                          placeholder="Location Area Code"
                          className="border-violet-200 focus:border-violet-500 dark:border-violet-800"
                          {...field}
                        />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />
                <FormField
                  control={form.control}
                  name="towerType"
                  render={({ field }) => (
                    <FormItem className="col-span-2">
                      <FormLabel className="flex items-center gap-1.5 text-violet-700 dark:text-violet-300">
                        <Building className="h-3.5 w-3.5" />
                        Tower Type
                      </FormLabel>
                      <FormControl>
                        <Select onValueChange={field.onChange} value={field.value || undefined}>
                          <SelectTrigger className="border-violet-200 focus:border-violet-500 dark:border-violet-800">
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
            </div>
          </div>
        </div>

        {/* Buttons - Fixed at bottom with gradient */}
        <div className="flex justify-end gap-3 pt-4 mt-4 border-t bg-gradient-to-r from-gray-50 to-slate-50 dark:from-gray-900 dark:to-slate-900 -mx-4 px-4 pb-1 rounded-b-lg">
          <Button variant="outline" type="button" onClick={onClose} className="gap-2">
            Cancel
          </Button>
          <SubmitButton
            loading={isPending}
            className="gap-2 bg-gradient-to-r from-amber-500 to-orange-600 hover:from-amber-600 hover:to-orange-700">
            <Save className="h-4 w-4" />
            {isPending ? "Updating..." : "Save Changes"}
          </SubmitButton>
        </div>
      </form>
    </Form>
  );
}
