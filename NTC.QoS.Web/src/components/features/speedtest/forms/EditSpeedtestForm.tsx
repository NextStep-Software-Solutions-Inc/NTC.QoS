"use client";

import { useCallback, useEffect, useState } from "react";
import { zodResolver } from "@hookform/resolvers/zod";
import { motion } from "framer-motion";
import {
  Activity,
  AlertCircle,
  Download,
  Edit3,
  Gauge,
  Loader2,
  MapPin,
  Navigation,
  Save,
  Signal,
  Smartphone,
  Trash2,
  Upload,
  WifiOff,
  Zap,
} from "lucide-react";
import { useForm } from "react-hook-form";
import { z } from "zod";

import { getServiceProvidersRequest } from "@/components/features/service-provider/handlers/getServiceProvidersRequest";
import { type ServiceProvider } from "@/components/features/service-provider/types/serviceProvider";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
  AlertDialogTrigger,
} from "@/components/ui/alert-dialog";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Form, FormControl, FormDescription, FormField, FormItem, FormLabel, FormMessage } from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Switch } from "@/components/ui/switch";
import { createMutationHookFromTemplate } from "@/hooks/createMutationHookFromTemplate";
import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";
import { cn } from "@/lib/utils";
import { deleteSpeedtestRequest } from "../handlers/deleteSpeedtestRequest";
import { editSpeedtestRequest } from "../handlers/editSpeedtestRequest";
import { SIGNAL_QUALITY_CONFIG, type SignalQuality } from "../types";
import type { SpeedTest } from "../types/SpeedTest";

// Create hooks
const useGetServiceProviders = createQueryHookFromTemplate(getServiceProvidersRequest);
const useUpdateSpeedtest = createMutationHookFromTemplate(editSpeedtestRequest);
const useDeleteSpeedtest = createMutationHookFromTemplate(deleteSpeedtestRequest);

// Validation schema
const editSpeedtestFormSchema = z.object({
  serviceProviderId: z.string().optional(),
  latitude: z.coerce
    .number()
    .min(-90, "Latitude must be between -90 and 90")
    .max(90, "Latitude must be between -90 and 90"),
  longitude: z.coerce
    .number()
    .min(-180, "Longitude must be between -180 and 180")
    .max(180, "Longitude must be between -180 and 180"),
  downloadSpeed: z.coerce.number().min(0, "Download speed must be positive").optional(),
  uploadSpeed: z.coerce.number().min(0, "Upload speed must be positive").optional(),
  ping: z.coerce.number().min(0, "Ping must be positive").optional(),
  jitter: z.coerce.number().min(0, "Jitter must be positive").optional(),
  isNoBroadbandCoverage: z.boolean().optional(),
});

type EditSpeedtestFormValues = z.infer<typeof editSpeedtestFormSchema>;

interface EditSpeedtestFormProps {
  speedtest: SpeedTest;
  onCancel?: () => void;
  onSuccess?: () => void;
  onDelete?: () => void;
}

export default function EditSpeedtestForm({ speedtest, onCancel, onSuccess, onDelete }: EditSpeedtestFormProps) {
  const [isGettingLocation, setIsGettingLocation] = useState(false);
  const [locationError, setLocationError] = useState<string | null>(null);
  const [hasChanges, setHasChanges] = useState(false);

  // Fetch wireless service providers only
  const { data: providersData, isLoading: isLoadingProviders } = useGetServiceProviders({
    pathParams: {},
    queryParams: { pageSize: 100, pageNumber: 1, serviceTypes: ["Wireless"] },
  });
  const updateSpeedtestMutation = useUpdateSpeedtest();
  const deleteSpeedtestMutation = useDeleteSpeedtest();

  const form = useForm<EditSpeedtestFormValues>({
    resolver: zodResolver(editSpeedtestFormSchema),
    defaultValues: {
      serviceProviderId: speedtest.serviceProvider?.id || "",
      latitude: speedtest.latitude,
      longitude: speedtest.longitude,
      downloadSpeed: speedtest.downloadSpeed,
      uploadSpeed: speedtest.uploadSpeed,
      ping: speedtest.ping,
      jitter: speedtest.jitter,
      isNoBroadbandCoverage: speedtest.isNoBroadbandCoverage ?? false,
    },
  });

  // Track form changes
  useEffect(() => {
    const subscription = form.watch((value) => {
      const changed =
        value.serviceProviderId !== speedtest.serviceProvider?.id ||
        value.latitude !== speedtest.latitude ||
        value.longitude !== speedtest.longitude ||
        value.downloadSpeed !== speedtest.downloadSpeed ||
        value.uploadSpeed !== speedtest.uploadSpeed ||
        value.ping !== speedtest.ping ||
        value.jitter !== speedtest.jitter ||
        value.isNoBroadbandCoverage !== (speedtest.isNoBroadbandCoverage ?? false);
      setHasChanges(changed);
    });
    return () => subscription.unsubscribe();
  }, [form, speedtest]);

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

  const onSubmit = async (data: EditSpeedtestFormValues) => {
    try {
      await updateSpeedtestMutation.mutateAsync({
        pathParams: { id: speedtest.id },
        payload: {
          latitude: data.latitude,
          longitude: data.longitude,
          // NBC: always send 0 for speeds when NBC is enabled
          downloadSpeed: data.isNoBroadbandCoverage ? 0 : data.downloadSpeed,
          uploadSpeed: data.isNoBroadbandCoverage ? 0 : data.uploadSpeed,
          ping: data.ping,
          jitter: data.jitter,
          serviceProviderId: data.serviceProviderId,
          isNoBroadbandCoverage: data.isNoBroadbandCoverage,
        },
      });
      onSuccess?.();
    } catch (error) {
      console.error("Failed to update speedtest:", error);
    }
  };

  const handleDelete = async () => {
    try {
      await deleteSpeedtestMutation.mutateAsync({ 
        pathParams: {},
        queryParams: { ids: [speedtest.id] }
      });
      onDelete?.();
    } catch (error) {
      console.error("Failed to delete speedtest:", error);
    }
  };

  const providerName = speedtest.serviceProvider?.provider?.name || "Unknown";
  const providerColor = speedtest.serviceProvider?.provider?.color || "#6b7280";
  const signalConfig = SIGNAL_QUALITY_CONFIG[speedtest.signalQuality as SignalQuality] || SIGNAL_QUALITY_CONFIG.Unknown;

  const watchedIsNBC = form.watch("isNoBroadbandCoverage");

  return (
    <motion.div
      initial={{ opacity: 0, y: 20 }}
      animate={{ opacity: 1, y: 0 }}
      exit={{ opacity: 0, y: -20 }}
      className="space-y-6">
      {/* Header with gradient */}
      <div className="relative overflow-hidden rounded-xl bg-gradient-to-br from-blue-500 via-purple-500 to-pink-500 p-6 text-white shadow-lg">
        <div className="absolute inset-0 bg-grid-white/10 [mask-image:linear-gradient(0deg,transparent,white)]" />
        <div className="relative">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-3">
              <div className="flex h-12 w-12 items-center justify-center rounded-xl bg-white/20 backdrop-blur-sm">
                <Edit3 className="h-6 w-6" />
              </div>
              <div>
                <h3 className="text-lg font-bold">Edit Speedtest</h3>
                <p className="text-sm text-white/80">Update performance data</p>
              </div>
            </div>
            <div className="flex items-center gap-2">
              <Badge className="bg-white/20 border-0 backdrop-blur-sm">{providerName}</Badge>
              <Badge
                className="border-0 backdrop-blur-sm"
                style={{ backgroundColor: `${signalConfig.color}40`, color: "white" }}>
                {signalConfig.label}
              </Badge>
              {watchedIsNBC && (
                <Badge className="border-0 backdrop-blur-sm bg-gray-500/40 text-white gap-1">
                  <WifiOff className="h-3 w-3" />
                  NBC
                </Badge>
              )}
            </div>
          </div>
        </div>
      </div>

      {/* Current values display */}
      <div className="grid grid-cols-4 gap-3">
        <div className="bg-gradient-to-br from-green-50 to-emerald-50/50 dark:from-green-950/30 dark:to-emerald-950/20 rounded-lg p-3 text-center">
          <Download className="h-5 w-5 mx-auto text-green-600 mb-1" />
          <p className="text-lg font-bold text-green-700">{speedtest.downloadSpeed?.toFixed(1) || 0}</p>
          <p className="text-xs text-green-600">Mbps Down</p>
        </div>
        <div className="bg-gradient-to-br from-blue-50 to-cyan-50/50 dark:from-blue-950/30 dark:to-cyan-950/20 rounded-lg p-3 text-center">
          <Upload className="h-5 w-5 mx-auto text-blue-600 mb-1" />
          <p className="text-lg font-bold text-blue-700">{speedtest.uploadSpeed?.toFixed(1) || 0}</p>
          <p className="text-xs text-blue-600">Mbps Up</p>
        </div>
        <div className="bg-gradient-to-br from-orange-50 to-amber-50/50 dark:from-orange-950/30 dark:to-amber-950/20 rounded-lg p-3 text-center">
          <Gauge className="h-5 w-5 mx-auto text-orange-600 mb-1" />
          <p className="text-lg font-bold text-orange-700">{speedtest.ping || 0}</p>
          <p className="text-xs text-orange-600">ms Ping</p>
        </div>
        <div className="bg-gradient-to-br from-purple-50 to-pink-50/50 dark:from-purple-950/30 dark:to-pink-950/20 rounded-lg p-3 text-center">
          <Zap className="h-5 w-5 mx-auto text-purple-600 mb-1" />
          <p className="text-lg font-bold text-purple-700">{speedtest.jitter || 0}</p>
          <p className="text-xs text-purple-600">ms Jitter</p>
        </div>
      </div>

      <Form {...form}>
        <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
          {/* Provider selection */}
          <FormField
            control={form.control}
            name="serviceProviderId"
            render={({ field }) => (
              <FormItem>
                <FormLabel className="flex items-center gap-2">
                  <Activity className="h-4 w-4 text-blue-500" />
                  Service Provider
                </FormLabel>
                <Select 
                  onValueChange={(value) => {
                    field.onChange(value);
                    // Explicitly mark field as dirty to trigger change detection
                    form.setValue("serviceProviderId", value, { 
                      shouldDirty: true, 
                      shouldValidate: true,
                      shouldTouch: true 
                    });
                  }} 
                  value={field.value} 
                  disabled={isLoadingProviders}
                >
                  <FormControl>
                    <SelectTrigger>
                      <SelectValue placeholder={isLoadingProviders ? "Loading providers..." : "Select a provider"} />
                    </SelectTrigger>
                  </FormControl>
                  <SelectContent>
                    {isLoadingProviders ? (
                      <SelectItem value="loading" disabled>
                        Loading providers...
                      </SelectItem>
                    ) : providersData?.data && providersData.data.length > 0 ? (
                      providersData.data.map((serviceProvider: ServiceProvider) => (
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
                      ))
                    ) : (
                      <SelectItem value="no-providers" disabled>
                        No wireless providers available
                      </SelectItem>
                    )}
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
                  <FormLabel>Latitude</FormLabel>
                  <FormControl>
                    <Input type="number" step="any" {...field} />
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
                  <FormLabel>Longitude</FormLabel>
                  <FormControl>
                    <Input type="number" step="any" {...field} />
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
                          shouldDirty: true,
                          shouldValidate: true,
                          shouldTouch: true,
                        });
                        form.setValue("uploadSpeed", 0, {
                          shouldDirty: true,
                          shouldValidate: true,
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
              <span>NBC is enabled — download and upload speeds are locked to <strong>0 Mbps</strong>.</span>
            </div>
          )}

          {/* Speed metrics */}
          <div className="grid grid-cols-2 gap-4">
            <FormField
              control={form.control}
              name="downloadSpeed"
              render={({ field }) => (
                <FormItem>
                  <FormLabel className="flex items-center gap-2">
                    <Download className="h-4 w-4 text-green-500" />
                    Download (Mbps)
                  </FormLabel>
                  <FormControl>
                    <Input type="number" step="0.01" disabled={watchedIsNBC} {...field} />
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
                    Upload (Mbps)
                  </FormLabel>
                  <FormControl>
                    <Input type="number" step="0.01" disabled={watchedIsNBC} {...field} />
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
                    <Input type="number" step="1" {...field} />
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
                    <Input type="number" step="0.1" {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
          </div>

          {/* Form actions */}
          <div className="flex items-center justify-between pt-4 border-t">
            <AlertDialog>
              <AlertDialogTrigger asChild>
                <Button type="button" variant="ghost" className="text-red-600 hover:text-red-700 hover:bg-red-50 gap-2">
                  <Trash2 className="h-4 w-4" />
                  Delete
                </Button>
              </AlertDialogTrigger>
              <AlertDialogContent>
                <AlertDialogHeader>
                  <AlertDialogTitle>Delete Speedtest</AlertDialogTitle>
                  <AlertDialogDescription>
                    Are you sure you want to delete this speedtest? This action cannot be undone.
                  </AlertDialogDescription>
                </AlertDialogHeader>
                <AlertDialogFooter>
                  <AlertDialogCancel>Cancel</AlertDialogCancel>
                  <AlertDialogAction onClick={handleDelete} className="bg-red-600 hover:bg-red-700">
                    {deleteSpeedtestMutation.isPending ? <Loader2 className="h-4 w-4 animate-spin" /> : "Delete"}
                  </AlertDialogAction>
                </AlertDialogFooter>
              </AlertDialogContent>
            </AlertDialog>

            <div className="flex items-center gap-3">
              <Button type="button" variant="outline" onClick={onCancel}>
                Cancel
              </Button>
              <Button
                type="submit"
                disabled={updateSpeedtestMutation.isPending || !hasChanges}
                className="gap-2 bg-gradient-to-r from-blue-500 to-purple-600 text-white">
                {updateSpeedtestMutation.isPending ? (
                  <Loader2 className="h-4 w-4 animate-spin" />
                ) : (
                  <Save className="h-4 w-4" />
                )}
                {updateSpeedtestMutation.isPending ? "Saving..." : "Save Changes"}
              </Button>
            </div>
          </div>
        </form>
      </Form>
    </motion.div>
  );
}
