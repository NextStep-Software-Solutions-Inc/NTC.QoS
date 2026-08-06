"use client";

import { useCallback, useEffect, useState } from "react";
import { zodResolver } from "@hookform/resolvers/zod";
import { AnimatePresence, motion } from "framer-motion";
import {
  Activity,
  AlertCircle,
  Building2,
  CheckCircle2,
  Download,
  Gauge,
  Loader2,
  MapPin,
  Navigation,
  Radio,
  Signal,
  Smartphone,
  Sparkles,
  Upload,
  WifiOff,
  X,
  Zap,
} from "lucide-react";
import { useForm } from "react-hook-form";
import { z } from "zod";

import { getServiceProvidersRequest } from "@/components/features/service-provider/handlers/getServiceProvidersRequest";
import { type ServiceProvider } from "@/components/features/service-provider/types/serviceProvider";
import { getTechnologiesRequest } from "@/components/features/technology/handlers/getTechnologiesRequest";
import { type Technology } from "@/components/features/technology/types/Technology";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Form, FormControl, FormDescription, FormField, FormItem, FormLabel, FormMessage } from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Switch } from "@/components/ui/switch";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { createMutationHookFromTemplate } from "@/hooks/createMutationHookFromTemplate";
import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";
import { cn } from "@/lib/utils";
import { createSpeedtestRequest } from "../handlers/createSpeedtestRequest";
import { SIGNAL_QUALITY_CONFIG, type SignalQuality } from "../types";
import { getSignalQualityFromRSSI, getSignalQualityLabel } from "../utils/signalQuality";

// Create hooks
const useGetServiceProviders = createQueryHookFromTemplate(getServiceProvidersRequest);
const useGetTechnologies = createQueryHookFromTemplate(getTechnologiesRequest);
const useCreateSpeedtest = createMutationHookFromTemplate(createSpeedtestRequest);

// Validation schema
const createSpeedtestFormSchema = z.object({
  serviceProviderId: z.string().min(1, "Please select a provider"),
  generationId: z.string().min(1, "Please select a generation"),
  technologyId: z.string().min(1, "Please select a technology"),
  dateTime: z.date({ required_error: "Date and time is required" }),
  latitude: z.coerce
    .number()
    .min(-90, "Latitude must be between -90 and 90")
    .max(90, "Latitude must be between -90 and 90"),
  longitude: z.coerce
    .number()
    .min(-180, "Longitude must be between -180 and 180")
    .max(180, "Longitude must be between -180 and 180"),
  downloadSpeed: z.coerce.number().min(0, "Download speed must be positive"),
  uploadSpeed: z.coerce.number().min(0, "Upload speed must be positive"),
  ping: z.coerce.number().min(0, "Ping must be positive").optional(),
  jitter: z.coerce.number().min(0, "Jitter must be positive").optional(),
  rssi: z.coerce.number().optional(),
  signalQuality: z.enum(["Unknown", "VeryWeak", "Weak", "Average", "Good", "Excellent"]).optional(),
  phoneModel: z.string().optional(),
  address: z.string().optional(),
  mcc: z.string().optional(),
  mnc: z.string().optional(),
  tac: z.string().optional(),
  cellIdentifier: z.string().optional(),
  isFixedBroadband: z.boolean().optional(),
  isNoBroadbandCoverage: z.boolean().optional(),
});

type CreateSpeedtestFormValues = z.infer<typeof createSpeedtestFormSchema>;

interface CreateSpeedtestFormProps {
  onCancel?: () => void;
  onSuccess?: () => void;
  defaultLocation?: { lat: number; lng: number };
}

const signalQualities: SignalQuality[] = ["Unknown", "VeryWeak", "Weak", "Average", "Good", "Excellent"];

export default function CreateSpeedtestForm({ onCancel, onSuccess, defaultLocation }: CreateSpeedtestFormProps) {
  const [isGettingLocation, setIsGettingLocation] = useState(false);
  const [locationError, setLocationError] = useState<string | null>(null);
  const [activeTab, setActiveTab] = useState("location");

  // Fetch wireless service providers only
  const { data: providersData, isLoading: isLoadingProviders } = useGetServiceProviders({
    pathParams: {},
    queryParams: { pageSize: 100, pageNumber: 1, serviceTypes: ["Wireless"] },
  });

  // Fetch technologies
  const { data: technologiesData, isLoading: isLoadingTechnologies } = useGetTechnologies({
    pathParams: {},
    queryParams: { pageSize: 100, pageNumber: 1 },
  });

  const createSpeedtestMutation = useCreateSpeedtest();

  const form = useForm<CreateSpeedtestFormValues>({
    resolver: zodResolver(createSpeedtestFormSchema),
    defaultValues: {
      serviceProviderId: "",
      generationId: "",
      technologyId: "",
      dateTime: new Date(),
      latitude: defaultLocation?.lat ?? 0,
      longitude: defaultLocation?.lng ?? 0,
      downloadSpeed: undefined,
      uploadSpeed: undefined,
      ping: undefined,
      jitter: undefined,
      rssi: undefined,
      signalQuality: "Unknown",
      phoneModel: "",
      address: "",
      mcc: "",
      mnc: "",
      tac: "",
      cellIdentifier: "",
      isFixedBroadband: false,
      isNoBroadbandCoverage: false,
    },
  });

  // Watch RSSI value to auto-calculate signal quality
  const watchedRssi = form.watch("rssi");

  // Auto-calculate signal quality when RSSI changes
  useEffect(() => {
    if (watchedRssi !== undefined && watchedRssi !== null && !isNaN(watchedRssi)) {
      const calculatedQuality = getSignalQualityFromRSSI(watchedRssi);
      form.setValue("signalQuality", calculatedQuality);
    }
  }, [watchedRssi, form]);

  // Auto-populate generationId when technology is selected
  const watchedTechnologyId = form.watch("technologyId");
  useEffect(() => {
    if (watchedTechnologyId && technologiesData?.data) {
      const selectedTechnology = technologiesData.data.find(
        (tech: Technology) => tech.id === watchedTechnologyId
      );
      if (selectedTechnology?.generation?.id) {
        form.setValue("generationId", selectedTechnology.generation.id);
      }
    }
  }, [watchedTechnologyId, technologiesData, form]);

  // Update coordinates when defaultLocation changes
  useEffect(() => {
    if (defaultLocation) {
      form.setValue("latitude", defaultLocation.lat);
      form.setValue("longitude", defaultLocation.lng);
    }
  }, [defaultLocation, form]);

  const getCurrentLocation = useCallback(() => {
    if (!navigator.geolocation) {
      setLocationError("Geolocation is not supported by your browser");
      return;
    }

    setIsGettingLocation(true);
    setLocationError(null);

    navigator.geolocation.getCurrentPosition(
      (position) => {
        form.setValue("latitude", position.coords.latitude);
        form.setValue("longitude", position.coords.longitude);
        setIsGettingLocation(false);
      },
      (error) => {
        setLocationError(
          error.code === 1
            ? "Location access denied. Please enable location permissions."
            : "Failed to get your location. Please try again.",
        );
        setIsGettingLocation(false);
      },
      { enableHighAccuracy: true, timeout: 10000, maximumAge: 0 },
    );
  }, [form]);

  const onSubmit = async (data: CreateSpeedtestFormValues) => {
    try {
      await createSpeedtestMutation.mutateAsync({
        pathParams: {},
        payload: {
          // Required fields
          serviceProviderId: data.serviceProviderId,
          generationId: data.generationId,
          technologyId: data.technologyId,
          dateTime: data.dateTime.toISOString(),
          latitude: data.latitude,
          longitude: data.longitude,
          // NBC: always send 0 for speeds when NBC is enabled
          downloadSpeed: data.isNoBroadbandCoverage ? 0 : data.downloadSpeed!,
          uploadSpeed: data.isNoBroadbandCoverage ? 0 : data.uploadSpeed!,
          // Optional fields - only include if they have values
          ...(data.address && { address: data.address }),
          ...(data.ping !== undefined && { ping: data.ping }),
          ...(data.jitter !== undefined && { jitter: data.jitter }),
          ...(data.rssi !== undefined && { rssi: data.rssi }),
          ...(data.signalQuality && { signalQuality: data.signalQuality }),
          ...(data.phoneModel && { phoneModel: data.phoneModel }),
          ...(data.mcc && { mcc: data.mcc }),
          ...(data.mnc && { mnc: data.mnc }),
          ...(data.tac && { tac: data.tac }),
          ...(data.cellIdentifier && { cellIdentifier: data.cellIdentifier }),
          ...(data.isFixedBroadband !== undefined && { isFixedBroadband: data.isFixedBroadband }),
          isNoBroadbandCoverage: data.isNoBroadbandCoverage ?? false,
        },
      });
      onSuccess?.();
    } catch (error) {
      console.error("Failed to create speedtest:", error);
    }
  };

  const watchedLat = form.watch("latitude");
  const watchedLng = form.watch("longitude");
  const watchedIsNBC = form.watch("isNoBroadbandCoverage");
  const hasValidCoordinates = watchedLat !== 0 && watchedLng !== 0 && !isNaN(watchedLat) && !isNaN(watchedLng);

  return (
    <motion.div
      initial={{ opacity: 0, y: 20 }}
      animate={{ opacity: 1, y: 0 }}
      exit={{ opacity: 0, y: -20 }}
      className="space-y-6">
      {/* Header with gradient */}
      <div className="relative overflow-hidden rounded-xl bg-gradient-to-br from-cyan-500 via-blue-500 to-purple-500 p-6 text-white shadow-lg">
        <div className="absolute inset-0 bg-grid-white/10 [mask-image:linear-gradient(0deg,transparent,white)]" />
        <div className="relative">
          <div className="flex items-center gap-3">
            <div className="flex h-12 w-12 items-center justify-center rounded-xl bg-white/20 backdrop-blur-sm">
              <Activity className="h-6 w-6" />
            </div>
            <div>
              <h3 className="text-lg font-bold">Add New Speedtest</h3>
              <p className="text-sm text-white/80">Record network performance data</p>
            </div>
          </div>
        </div>
      </div>

      <Form {...form}>
        <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-6">
          <Tabs value={activeTab} onValueChange={setActiveTab} className="w-full">
            <TabsList className="grid w-full grid-cols-3 bg-gray-100 dark:bg-gray-800">
              <TabsTrigger value="location" className="gap-2">
                <MapPin className="h-4 w-4" />
                Location
              </TabsTrigger>
              <TabsTrigger value="speed" className="gap-2">
                <Download className="h-4 w-4" />
                Speed
              </TabsTrigger>
              <TabsTrigger value="network" className="gap-2">
                <Radio className="h-4 w-4" />
                Network
              </TabsTrigger>
            </TabsList>

            {/* Location Tab */}
            <TabsContent value="location" className="space-y-4 mt-4">
              {/* Date Time Field */}
              <FormField
                control={form.control}
                name="dateTime"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>Date & Time <span className="text-red-500">*</span></FormLabel>
                    <FormControl>
                      <Input
                        type="datetime-local"
                        value={field.value instanceof Date ? field.value.toISOString().slice(0, 16) : ""}
                        onChange={(e) => field.onChange(new Date(e.target.value))}
                      />
                    </FormControl>
                    <FormDescription>When the speedtest was conducted</FormDescription>
                    <FormMessage />
                  </FormItem>
                )}
              />

              {/* Provider selection */}
              <FormField
                control={form.control}
                name="serviceProviderId"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel className="flex items-center gap-2">
                      <Building2 className="h-4 w-4 text-blue-500" />
                      Service Provider <span className="text-red-500">*</span>
                    </FormLabel>
                    <Select onValueChange={field.onChange} value={field.value}>
                      <FormControl>
                        <SelectTrigger>
                          <SelectValue placeholder="Select a provider" />
                        </SelectTrigger>
                      </FormControl>
                      <SelectContent>
                        {providersData?.data?.map((serviceProvider: ServiceProvider) => (
                          <SelectItem key={serviceProvider.id} value={serviceProvider.id}>
                            <div className="flex items-center gap-2">
                              {serviceProvider.provider?.color && (
                                <span
                                  className="h-3 w-3 rounded-full"
                                  style={{ backgroundColor: serviceProvider.provider.color }}
                                />
                              )}
                              {serviceProvider.provider?.name || "Unknown Provider"}
                            </div>
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                    <FormMessage />
                  </FormItem>
                )}
              />

              {/* Coordinates */}
              <div className="grid grid-cols-2 gap-4">
                <FormField
                  control={form.control}
                  name="latitude"
                  render={({ field }) => (
                    <FormItem>
                    <FormLabel>Latitude <span className="text-red-500">*</span></FormLabel>
                      <FormControl>
                        <Input type="number" step="any" placeholder="e.g., 14.5995" {...field} />
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
                    <FormLabel>Longitude <span className="text-red-500">*</span></FormLabel>
                      <FormControl>
                        <Input type="number" step="any" placeholder="e.g., 120.9842" {...field} />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />
              </div>

              {/* Get current location button */}
              <Button
                type="button"
                variant="outline"
                onClick={getCurrentLocation}
                disabled={isGettingLocation}
                className="w-full gap-2">
                {isGettingLocation ? <Loader2 className="h-4 w-4 animate-spin" /> : <Navigation className="h-4 w-4" />}
                {isGettingLocation ? "Getting location..." : "Use Current Location"}
              </Button>

              {locationError && (
                <div className="flex items-center gap-2 text-sm text-red-500">
                  <AlertCircle className="h-4 w-4" />
                  {locationError}
                </div>
              )}

              {/* Address */}
              <FormField
                control={form.control}
                name="address"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>Address (Optional)</FormLabel>
                    <FormControl>
                      <Input placeholder="Enter address" {...field} />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
            </TabsContent>

            {/* Speed Tab */}
            <TabsContent value="speed" className="space-y-4 mt-4">
              {/* NBC Toggle */}
              <FormField
                control={form.control}
                name="isNoBroadbandCoverage"
                render={({ field }) => (
                  <FormItem className="flex items-center justify-between rounded-lg border border-gray-200 dark:border-gray-700 p-4 bg-gray-50 dark:bg-gray-800/50">
                    <div className="space-y-0.5">
                      <FormLabel className="flex items-center gap-2 text-sm font-medium">
                        <WifiOff className="h-4 w-4 text-gray-500" />
                        No Broadband Coverage (NBC)
                      </FormLabel>
                      <FormDescription className="text-xs text-gray-500">
                        Mark this site as validated but with no detectable broadband signal. Speeds will be recorded as 0.
                      </FormDescription>
                    </div>
                    <FormControl>
                      <Switch
                        checked={field.value ?? false}
                        onCheckedChange={(checked) => {
                          field.onChange(checked);
                          if (checked) {
                            form.setValue("downloadSpeed", 0, {
                              shouldValidate: true,
                              shouldDirty: true,
                              shouldTouch: true,
                            });
                            form.setValue("uploadSpeed", 0, {
                              shouldValidate: true,
                              shouldDirty: true,
                              shouldTouch: true,
                            });
                          }
                        }}
                      />
                    </FormControl>
                  </FormItem>
                )}
              />

              {watchedIsNBC && (
                <div className="flex items-center gap-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-gray-100 dark:bg-gray-800 px-4 py-3 text-sm text-gray-600 dark:text-gray-400">
                  <WifiOff className="h-4 w-4 flex-shrink-0" />
                  <span>NBC is enabled — download and upload speeds are locked to <strong>0 Mbps</strong>. The site was visited but no signal was detected.</span>
                </div>
              )}

              <div className="grid grid-cols-2 gap-4">
                <FormField
                  control={form.control}
                  name="downloadSpeed"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel className="flex items-center gap-2">
                        <Download className="h-4 w-4 text-green-500" />
                        Download Speed (Mbps) <span className="text-red-500">*</span>
                      </FormLabel>
                      <FormControl>
                        <Input
                          type="number"
                          step="0.01"
                          placeholder="e.g., 50.5"
                          disabled={watchedIsNBC}
                          {...field}
                        />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />
                <FormField
                  control={form.control}
                  name="uploadSpeed"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel className="flex items-center gap-2">
                        <Upload className="h-4 w-4 text-blue-500" />
                        Upload Speed (Mbps) <span className="text-red-500">*</span>
                      </FormLabel>
                      <FormControl>
                        <Input
                          type="number"
                          step="0.01"
                          placeholder="e.g., 25.0"
                          disabled={watchedIsNBC}
                          {...field}
                        />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />
              </div>

              <div className="grid grid-cols-2 gap-4">
                <FormField
                  control={form.control}
                  name="ping"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel className="flex items-center gap-2">
                        <Gauge className="h-4 w-4 text-orange-500" />
                        Ping (ms)
                      </FormLabel>
                      <FormControl>
                        <Input type="number" step="1" placeholder="e.g., 20" {...field} />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />
                <FormField
                  control={form.control}
                  name="jitter"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel className="flex items-center gap-2">
                        <Zap className="h-4 w-4 text-purple-500" />
                        Jitter (ms)
                      </FormLabel>
                      <FormControl>
                        <Input type="number" step="0.1" placeholder="e.g., 5.0" {...field} />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />
              </div>

              <FormField
                control={form.control}
                name="signalQuality"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel className="flex items-center gap-2">
                      <Signal className="h-4 w-4 text-cyan-500" />
                      Signal Quality
                    </FormLabel>
                    <Select onValueChange={field.onChange} value={field.value}>
                      <FormControl>
                        <SelectTrigger>
                          <SelectValue placeholder="Select signal quality" />
                        </SelectTrigger>
                      </FormControl>
                      <SelectContent>
                        {signalQualities.map((quality) => (
                          <SelectItem key={quality} value={quality}>
                            <div className="flex items-center gap-2">
                              <span
                                className="h-3 w-3 rounded-full"
                                style={{ backgroundColor: SIGNAL_QUALITY_CONFIG[quality].color }}
                              />
                              {SIGNAL_QUALITY_CONFIG[quality].label}
                            </div>
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                    <FormMessage />
                  </FormItem>
                )}
              />
            </TabsContent>

            {/* Network Tab */}
            <TabsContent value="network" className="space-y-4 mt-4">
              <FormField
                control={form.control}
                name="technologyId"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>Technology <span className="text-red-500">*</span></FormLabel>
                    <Select onValueChange={field.onChange} value={field.value}>
                      <FormControl>
                        <SelectTrigger>
                          <SelectValue placeholder="Select technology" />
                        </SelectTrigger>
                      </FormControl>
                      <SelectContent>
                        {technologiesData?.data?.map((technology: Technology) => (
                          <SelectItem key={technology.id} value={technology.id}>
                            {technology.standard} ({technology.generation?.name || "Unknown"})
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                    <FormDescription>
                      Select the network technology (e.g., LTE (4G), NR (5G))
                    </FormDescription>
                    <FormMessage />
                  </FormItem>
                )}
              />

              <FormField
                control={form.control}
                name="cellIdentifier"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>Cell Identifier (Optional)</FormLabel>
                    <FormControl>
                      <Input placeholder="Enter cell identifier" {...field} />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />

              <div className="grid grid-cols-3 gap-4">
                <FormField
                  control={form.control}
                  name="mcc"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>MCC</FormLabel>
                      <FormControl>
                        <Input placeholder="e.g., 515" {...field} />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />
                <FormField
                  control={form.control}
                  name="mnc"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>MNC</FormLabel>
                      <FormControl>
                        <Input placeholder="e.g., 01" {...field} />
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
                        <Input placeholder="e.g., 12345" {...field} />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />
              </div>

              <FormField
                control={form.control}
                name="phoneModel"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel className="flex items-center gap-2">
                      <Smartphone className="h-4 w-4 text-gray-500" />
                      Phone Model (Optional)
                    </FormLabel>
                    <FormControl>
                      <Input placeholder="e.g., iPhone 14 Pro" {...field} />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />

              <FormField
                control={form.control}
                name="rssi"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>RSSI (dBm)</FormLabel>
                    <FormControl>
                      <Input type="number" placeholder="e.g., -70" {...field} />
                    </FormControl>
                    <FormDescription>
                      Signal strength in dBm (auto-calculates signal quality)
                    </FormDescription>
                    <FormMessage />
                  </FormItem>
                )}
              />

              {/* Display auto-calculated signal quality */}
              {watchedRssi && (
                <div className="rounded-lg bg-blue-50 dark:bg-blue-950/30 p-3 border border-blue-200 dark:border-blue-800">
                  <div className="flex items-center gap-2 text-sm">
                    <Signal className="h-4 w-4 text-blue-600 dark:text-blue-400" />
                    <span className="font-medium text-blue-900 dark:text-blue-100">Auto-calculated Quality:</span>
                    <span className="text-blue-700 dark:text-blue-300">
                      {getSignalQualityLabel(getSignalQualityFromRSSI(watchedRssi))}
                    </span>
                  </div>
                </div>
              )}
            </TabsContent>
          </Tabs>

          {/* Form actions */}
          <div className="flex items-center justify-end gap-3 pt-4 border-t">
            <Button type="button" variant="outline" onClick={onCancel}>
              Cancel
            </Button>
            <Button
              type="submit"
              disabled={createSpeedtestMutation.isPending}
              className="gap-2 bg-gradient-to-r from-cyan-500 to-blue-600 text-white">
              {createSpeedtestMutation.isPending ? (
                <Loader2 className="h-4 w-4 animate-spin" />
              ) : (
                <CheckCircle2 className="h-4 w-4" />
              )}
              {createSpeedtestMutation.isPending ? "Creating..." : "Create Speedtest"}
            </Button>
          </div>
        </form>
      </Form>
    </motion.div>
  );
}
