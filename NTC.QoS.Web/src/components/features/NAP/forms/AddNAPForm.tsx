"use client";

import { useCallback, useEffect, useState } from "react";
import { zodResolver } from "@hookform/resolvers/zod";
import { AnimatePresence, motion } from "framer-motion";
import { AlertCircle, Building2, CheckCircle2, Globe, Loader2, MapPin, Navigation, Sparkles, X } from "lucide-react";
import { useForm } from "react-hook-form";
import { z } from "zod";

import { getServiceProvidersRequest } from "@/components/features/service-provider/handlers/getServiceProvidersRequest";
import { type ServiceProvider } from "@/components/features/service-provider/types/serviceProvider";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Form, FormControl, FormField, FormItem, FormLabel, FormMessage } from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { createMutationHookFromTemplate } from "@/hooks/createMutationHookFromTemplate";
import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";
import { cn } from "@/lib/utils";
import { createNAPRequest } from "../handlers/createNAPRequest";

// Create hooks
const useGetServiceProviders = createQueryHookFromTemplate(getServiceProvidersRequest);
const useCreateNAP = createMutationHookFromTemplate(createNAPRequest);

// Validation schema
const addNAPFormSchema = z.object({
  providerId: z.string().min(1, "Please select a provider"),
  latitude: z
    .string()
    .refine((v) => v.trim() !== "" && !isNaN(parseFloat(v)), "Latitude must be a number")
    .refine((v) => { const n = parseFloat(v); return n >= -90 && n <= 90; }, "Latitude must be between -90 and 90"),
  longitude: z
    .string()
    .refine((v) => v.trim() !== "" && !isNaN(parseFloat(v)), "Longitude must be a number")
    .refine((v) => { const n = parseFloat(v); return n >= -180 && n <= 180; }, "Longitude must be between -180 and 180"),
});

type AddNAPFormValues = z.infer<typeof addNAPFormSchema>;

interface AddNAPFormProps {
  onCancel?: () => void;
  onSuccess?: () => void;
  initialCoordinates?: { lat: number; lng: number } | null;
  defaultLocation?: { lat: number; lng: number };
}

// Provider colors mapping
const providerColors: Record<string, string> = {
  DITO: "#00A651",
  Globe: "#0066FF",
  Smart: "#FF6600",
  PLDT: "#0033A0",
  Converge: "#8BC34A",
  Sky: "#E91E63",
};

const getProviderColor = (name: string): string => {
  return providerColors[name] || "#6366f1";
};

export default function AddNAPForm({ onCancel, onSuccess, initialCoordinates, defaultLocation }: AddNAPFormProps) {
  const [isGettingLocation, setIsGettingLocation] = useState(false);
  const [locationError, setLocationError] = useState<string | null>(null);
  const [selectedProviderName, setSelectedProviderName] = useState<string>("");

  // Use either initialCoordinates or defaultLocation
  const coords = initialCoordinates || defaultLocation;

  const { data: serviceProvidersData, isLoading: isLoadingProviders } = useGetServiceProviders({
    pathParams: {},
    queryParams: { pageSize: 100, pageNumber: 1, serviceTypes: ["wired"] },
  });
  const createNAPMutation = useCreateNAP();

  const form = useForm<AddNAPFormValues>({
    resolver: zodResolver(addNAPFormSchema),
    defaultValues: { providerId: "", latitude: String(coords?.lat ?? 0), longitude: String(coords?.lng ?? 0) },
  });

  // Update coordinates when initialCoordinates or defaultLocation changes
  useEffect(() => {
    if (coords) {
      form.setValue("latitude", String(coords.lat));
      form.setValue("longitude", String(coords.lng));
    }
  }, [coords, form]);

  const getCurrentLocation = useCallback(() => {
    if (!navigator.geolocation) {
      setLocationError("Geolocation is not supported by your browser");
      return;
    }

    setIsGettingLocation(true);
    setLocationError(null);

    navigator.geolocation.getCurrentPosition(
      (position) => {
        form.setValue("latitude", String(position.coords.latitude));
        form.setValue("longitude", String(position.coords.longitude));
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

  const onSubmit = async (data: AddNAPFormValues) => {
    try {
      await createNAPMutation.mutateAsync({
        pathParams: {},
        payload: {
          serviceProviderId: data.providerId,
          address: "",
          latitude: parseFloat(data.latitude),
          longitude: parseFloat(data.longitude),
          isFileUploaded: false,
        },
      });
      onSuccess?.();
    } catch (error) {
      console.error("Failed to create NAP:", error);
    }
  };

  const watchedProviderId = form.watch("providerId");
  const watchedLat = form.watch("latitude");
  const watchedLng = form.watch("longitude");

  // Update selected provider name when providerId changes
  useEffect(() => {
    if (watchedProviderId && serviceProvidersData?.data) {
      const serviceProvider = serviceProvidersData.data.find((sp: ServiceProvider) => sp.id === watchedProviderId);
      setSelectedProviderName(serviceProvider?.provider?.name || "");
    }
  }, [watchedProviderId, serviceProvidersData]);

  const latNum = parseFloat(watchedLat);
  const lngNum = parseFloat(watchedLng);
  const hasValidCoordinates = !isNaN(latNum) && !isNaN(lngNum) && latNum !== 0 && lngNum !== 0;

  return (
    <motion.div
      initial={{ opacity: 0, y: 20 }}
      animate={{ opacity: 1, y: 0 }}
      exit={{ opacity: 0, y: -20 }}
      className="space-y-6">
      {/* Header with gradient */}
      <div className="relative overflow-hidden rounded-xl bg-gradient-to-br from-indigo-500 via-purple-500 to-pink-500 p-6 text-white shadow-lg">
        <div className="absolute inset-0 bg-grid-white/10 [mask-image:linear-gradient(0deg,transparent,white)]" />
        <div className="relative">
          <div className="flex items-center gap-3">
            <div className="flex h-12 w-12 items-center justify-center rounded-xl bg-white/20 backdrop-blur-sm">
              <MapPin className="h-6 w-6" />
            </div>
            <div>
              <h3 className="text-xl font-bold">Add Network Access Point</h3>
              <p className="text-sm text-white/80">Location: {coords?.lat.toFixed(6)}, {coords?.lng.toFixed(6)}</p>
            </div>
          </div>
        </div>
      </div>

      <Form {...form}>
        <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-6">
          {/* Provider Selection */}
          <FormField
            control={form.control}
            name="providerId"
            render={({ field }) => (
              <FormItem>
                <FormLabel className="flex items-center gap-2 text-sm font-medium">
                  <Building2 className="h-4 w-4 text-muted-foreground" />
                  Provider
                </FormLabel>
                <Select onValueChange={field.onChange} defaultValue={field.value}>
                  <FormControl>
                    <SelectTrigger className="h-12 border-2 transition-all focus:border-indigo-500 focus:ring-2 focus:ring-indigo-500/20">
                      <SelectValue placeholder="Select a provider" />
                    </SelectTrigger>
                  </FormControl>
                  <SelectContent>
                    {isLoadingProviders ? (
                      <div className="flex items-center justify-center py-4">
                        <Loader2 className="h-4 w-4 animate-spin text-muted-foreground" />
                      </div>
                    ) : serviceProvidersData?.data && serviceProvidersData.data.length > 0 ? (
                      serviceProvidersData.data.map((serviceProvider: ServiceProvider) => (
                        <SelectItem key={serviceProvider.id} value={serviceProvider.id}>
                          <div className="flex items-center gap-3">
                            <div
                              className="h-3 w-3 rounded-full shadow-sm"
                              style={{ backgroundColor: getProviderColor(serviceProvider.provider?.name || "") }}
                            />
                            <span className="font-medium">{serviceProvider.provider?.name || "Unknown"}</span>
                          </div>
                        </SelectItem>
                      ))
                    ) : (
                      <div className="flex items-center justify-center py-4 text-sm text-muted-foreground">
                        No wired providers available
                      </div>
                    )}
                  </SelectContent>
                </Select>
                {selectedProviderName && (
                  <motion.div initial={{ opacity: 0, scale: 0.9 }} animate={{ opacity: 1, scale: 1 }}>
                    <Badge
                      className="mt-2 gap-1.5"
                      style={{
                        backgroundColor: `${getProviderColor(selectedProviderName)}20`,
                        color: getProviderColor(selectedProviderName),
                        borderColor: getProviderColor(selectedProviderName),
                      }}>
                      <div
                        className="h-2 w-2 rounded-full"
                        style={{ backgroundColor: getProviderColor(selectedProviderName) }}
                      />
                      {selectedProviderName} selected
                    </Badge>
                  </motion.div>
                )}
                <FormMessage />
              </FormItem>
            )}
          />

          {/* Coordinates Section */}
          <div className="space-y-4">
            <div className="flex items-center justify-between">
              <h4 className="flex items-center gap-2 text-sm font-medium">
                <Globe className="h-4 w-4 text-muted-foreground" />
                Coordinates
              </h4>
              <Button
                type="button"
                variant="outline"
                size="sm"
                onClick={getCurrentLocation}
                disabled={isGettingLocation}
                className="gap-2 text-xs">
                {isGettingLocation ? (
                  <>
                    <Loader2 className="h-3 w-3 animate-spin" />
                    Getting location...
                  </>
                ) : (
                  <>
                    <Navigation className="h-3 w-3" />
                    Use my location
                  </>
                )}
              </Button>
            </div>

            <AnimatePresence>
              {locationError && (
                <motion.div
                  initial={{ opacity: 0, height: 0 }}
                  animate={{ opacity: 1, height: "auto" }}
                  exit={{ opacity: 0, height: 0 }}
                  className="flex items-center gap-2 rounded-lg border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-700">
                  <AlertCircle className="h-4 w-4 flex-shrink-0" />
                  <span>{locationError}</span>
                  <Button
                    type="button"
                    variant="ghost"
                    size="icon"
                    className="ml-auto h-6 w-6"
                    onClick={() => setLocationError(null)}>
                    <X className="h-3 w-3" />
                  </Button>
                </motion.div>
              )}
            </AnimatePresence>

            <div className="grid grid-cols-2 gap-4">
              <FormField
                control={form.control}
                name="latitude"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel className="flex items-center gap-1.5 text-xs font-medium text-muted-foreground">
                      Latitude
                      <span className="rounded bg-muted px-1 py-0.5 font-mono text-[10px] text-muted-foreground">N</span>
                    </FormLabel>
                    <FormControl>
                      <Input
                        type="text"
                        inputMode="decimal"
                        placeholder="e.g., 14.5995"
                        className={cn(
                          "h-11 border-2 font-mono text-sm transition-all focus:border-indigo-500 focus:ring-2 focus:ring-indigo-500/20",
                          field.value !== "" && "border-green-300 bg-green-50/50",
                        )}
                        {...field}
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
                    <FormLabel className="flex items-center gap-1.5 text-xs font-medium text-muted-foreground">
                      Longitude
                      <span className="rounded bg-muted px-1 py-0.5 font-mono text-[10px] text-muted-foreground">E</span>
                    </FormLabel>
                    <FormControl>
                      <Input
                        type="text"
                        inputMode="decimal"
                        placeholder="e.g., 120.9842"
                        className={cn(
                          "h-11 border-2 font-mono text-sm transition-all focus:border-indigo-500 focus:ring-2 focus:ring-indigo-500/20",
                          field.value !== "" && "border-green-300 bg-green-50/50",
                        )}
                        {...field}
                      />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
            </div>

            {/* Coordinate Preview */}
            <AnimatePresence>
              {hasValidCoordinates && (
                <motion.div
                  initial={{ opacity: 0, y: -10 }}
                  animate={{ opacity: 1, y: 0 }}
                  exit={{ opacity: 0, y: -10 }}
                  className="flex items-center gap-2 rounded-lg border border-green-200 bg-green-50 px-3 py-2">
                  <CheckCircle2 className="h-4 w-4 text-green-600" />
                  <span className="text-sm text-green-700">
                    Location set:{" "}
                    <span className="font-mono font-medium">
                      {latNum.toFixed(6)}, {lngNum.toFixed(6)}
                    </span>
                  </span>
                </motion.div>
              )}
            </AnimatePresence>
          </div>

          {/* Form Actions */}
          <div className="flex items-center gap-3 pt-4">
            {onCancel && (
              <Button
                type="button"
                variant="outline"
                onClick={onCancel}
                className="flex-1"
                disabled={createNAPMutation.isPending}>
                Cancel
              </Button>
            )}
            <Button
              type="submit"
              className="flex-1 gap-2 bg-gradient-to-r from-indigo-500 to-purple-600 text-white shadow-lg shadow-indigo-500/25 hover:from-indigo-600 hover:to-purple-700"
              disabled={createNAPMutation.isPending}>
              {createNAPMutation.isPending ? (
                <>
                  <Loader2 className="h-4 w-4 animate-spin" />
                  Creating...
                </>
              ) : (
                <>
                  <MapPin className="h-4 w-4" />
                  Add NAP
                </>
              )}
            </Button>
          </div>

          {/* Error Display */}
          <AnimatePresence>
            {createNAPMutation.isError && (
              <motion.div
                initial={{ opacity: 0, height: 0 }}
                animate={{ opacity: 1, height: "auto" }}
                exit={{ opacity: 0, height: 0 }}
                className="rounded-lg border border-red-200 bg-red-50 p-4 text-sm text-red-700">
                <div className="flex items-center gap-2">
                  <AlertCircle className="h-4 w-4" />
                  <span className="font-medium">Failed to create NAP</span>
                </div>
                <p className="mt-1 text-red-600">
                  {(createNAPMutation.error as Error)?.message || "An unexpected error occurred. Please try again."}
                </p>
              </motion.div>
            )}
          </AnimatePresence>
        </form>
      </Form>
    </motion.div>
  );
}
