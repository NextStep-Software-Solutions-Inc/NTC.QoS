"use client";

import { useCallback, useEffect, useState } from "react";
import { zodResolver } from "@hookform/resolvers/zod";
import { AnimatePresence, motion } from "framer-motion";
import {
  AlertCircle,
  Building2,
  CheckCircle2,
  Edit3,
  Globe,
  Loader2,
  MapPin,
  Navigation,
  Save,
  Sparkles,
  Trash2,
  X,
} from "lucide-react";
import { useForm } from "react-hook-form";
import { z } from "zod";

import { getServiceProvidersRequest } from "@/components/features/service-provider/handlers/getServiceProvidersRequest";
import { type ServiceProvider } from "@/components/features/service-provider/types/serviceProvider";
import { extractErrorMessage } from "@/lib/extract-error-message";
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
import { Form, FormControl, FormField, FormItem, FormLabel, FormMessage } from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { createMutationHookFromTemplate } from "@/hooks/createMutationHookFromTemplate";
import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";
import { cn } from "@/lib/utils";
import { deleteNAPRequest } from "../handlers/deleteNAPRequest";
import { editNAPRequest } from "../handlers/editNAPRequest";
import type { NAPDetails } from "../types/NAP";

// Create hooks
const useGetServiceProviders = createQueryHookFromTemplate(getServiceProvidersRequest);
const useUpdateNAP = createMutationHookFromTemplate(editNAPRequest);
const useDeleteNAP = createMutationHookFromTemplate(deleteNAPRequest);

// Validation schema
const editNAPFormSchema = z.object({
  providerId: z.string().optional(),
  address: z.string().optional(),
  latitude: z
    .string()
    .refine((v) => v.trim() !== "" && !isNaN(parseFloat(v)), "Latitude must be a number")
    .refine((v) => { const n = parseFloat(v); return n >= -90 && n <= 90; }, "Latitude must be between -90 and 90"),
  longitude: z
    .string()
    .refine((v) => v.trim() !== "" && !isNaN(parseFloat(v)), "Longitude must be a number")
    .refine((v) => { const n = parseFloat(v); return n >= -180 && n <= 180; }, "Longitude must be between -180 and 180"),
});

type EditNAPFormValues = z.infer<typeof editNAPFormSchema>;


interface EditNAPFormProps {
  nap: NAPDetails;
  onCancel?: () => void;
  onSuccess?: () => void;
  onDelete?: () => void;
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

export default function EditNAPForm({ nap, onCancel, onSuccess, onDelete }: EditNAPFormProps) {
  const [isGettingLocation, setIsGettingLocation] = useState(false);
  const [locationError, setLocationError] = useState<string | null>(null);
  const [selectedProviderName, setSelectedProviderName] = useState<string>(
    nap.serviceProvider?.provider?.name || nap.ISP || "",
  );
  const [hasChanges, setHasChanges] = useState(false);

  const { data: serviceProvidersData, isLoading: isLoadingProviders } = useGetServiceProviders({
    pathParams: {},
    queryParams: { pageSize: 100, pageNumber: 1, serviceTypes: ["wired"] },
  });
  const updateNAPMutation = useUpdateNAP();
  const deleteNAPMutation = useDeleteNAP();

  const form = useForm<EditNAPFormValues>({
    resolver: zodResolver(editNAPFormSchema),
    defaultValues: {
      providerId: nap.serviceProvider?.id || nap.serviceProviderId || "",
      address: nap.address || "",
      latitude: String(typeof nap.lat === "number" ? nap.lat : parseFloat(String(nap.lat)) || 0),
      longitude: String(typeof nap.lng === "number" ? nap.lng : parseFloat(String(nap.lng)) || 0),
    },
  });

  // Track form changes
  useEffect(() => {
    const subscription = form.watch((value) => {
      const changed =
        value.providerId !== (nap.serviceProvider?.id || nap.serviceProviderId || "") ||
        value.address !== (nap.address || "") ||
        parseFloat(String(value.latitude ?? "")) !== nap.lat ||
        parseFloat(String(value.longitude ?? "")) !== nap.lng;
      setHasChanges(changed);
    });
    return () => subscription.unsubscribe();
  }, [form, nap]);

  // Update provider name when selection changes
  useEffect(() => {
    const providerId = form.watch("providerId");
    if (providerId && serviceProvidersData?.data) {
      const serviceProvider = serviceProvidersData.data.find((sp: ServiceProvider) => sp.id === providerId);
      setSelectedProviderName(serviceProvider?.provider?.name || "");
    }
  }, [form.watch("providerId"), serviceProvidersData]);

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

  const onSubmit = async (data: EditNAPFormValues) => {
    try {
      const serviceProviderId = (data.providerId || nap.serviceProvider?.id || nap.serviceProviderId || "").trim();

      await updateNAPMutation.mutateAsync({
        pathParams: { id: nap.id },
        payload: {
          ...(serviceProviderId ? { serviceProviderId } : {}),
          address: data.address || "",
          latitude: parseFloat(data.latitude),
          longitude: parseFloat(data.longitude),
        },
      });
      onSuccess?.();
    } catch (error) {
      console.error("Failed to update NAP:", error);
    }
  };

  const handleDelete = async () => {
    try {
      await deleteNAPMutation.mutateAsync({ pathParams: {}, queryParams: { ids: nap.id } });
      onDelete?.();
    } catch (error) {
      console.error("Failed to delete NAP:", error);
    }
  };

  const watchedLat = form.watch("latitude");
  const watchedLng = form.watch("longitude");

  const latNum = parseFloat(watchedLat);
  const lngNum = parseFloat(watchedLng);

  const hasValidCoordinates = !isNaN(latNum) && !isNaN(lngNum) && latNum !== 0 && lngNum !== 0;

  const isPending = updateNAPMutation.isPending || deleteNAPMutation.isPending;

  return (
    <motion.div
      initial={{ opacity: 0, y: 20 }}
      animate={{ opacity: 1, y: 0 }}
      exit={{ opacity: 0, y: -20 }}
      className="space-y-6">
      {/* Header with gradient */}
      <div className="relative overflow-hidden rounded-xl bg-gradient-to-br from-amber-500 via-orange-500 to-red-500 p-6 text-white shadow-lg">
        <div className="absolute inset-0 bg-grid-white/10 [mask-image:linear-gradient(0deg,transparent,white)]" />
        <div className="relative">
          <div className="flex items-center gap-3">
            <div className="flex h-12 w-12 items-center justify-center rounded-xl bg-white/20 backdrop-blur-sm">
              <Edit3 className="h-6 w-6" />
            </div>
            <div className="flex-1">
              <h3 className="text-xl font-bold">Edit NAP</h3>
              <p className="text-sm text-white/80">Modify Network Access Point details</p>
            </div>
            {nap.serviceProvider?.provider && (
              <Badge className="border-white/30 bg-white/20 text-white backdrop-blur-sm" variant="outline">
                {nap.serviceProvider.provider.name}
              </Badge>
            )}
          </div>
          <div className="mt-4 flex items-center gap-4 text-xs text-white/70">
            <div className="flex items-center gap-1">
              <MapPin className="h-3 w-3" />
              <span>ID: {nap.id.slice(0, 8)}...</span>
            </div>
            {hasChanges && (
              <motion.div
                initial={{ opacity: 0, scale: 0.9 }}
                animate={{ opacity: 1, scale: 1 }}
                className="flex items-center gap-1">
                <Sparkles className="h-3 w-3" />
                <span>Unsaved changes</span>
              </motion.div>
            )}
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
                <Select onValueChange={field.onChange} value={field.value ?? ""}>
                  <FormControl>
                    <SelectTrigger className="h-12 border-2 transition-all focus:border-orange-500 focus:ring-2 focus:ring-orange-500/20">
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
                      {selectedProviderName}
                    </Badge>
                  </motion.div>
                )}
                <FormMessage />
              </FormItem>
            )}
          />

          {/* Address Field */}
          <FormField
            control={form.control}
            name="address"
            render={({ field }) => (
              <FormItem>
                <FormLabel className="flex items-center gap-2 text-sm font-medium">
                  <MapPin className="h-4 w-4 text-muted-foreground" />
                  Address
                </FormLabel>
                <FormControl>
                  <Input
                    placeholder="Enter address (optional)"
                    className={cn(
                      "h-11 border-2 transition-all focus:border-orange-500 focus:ring-2 focus:ring-orange-500/20",
                      field.value !== (nap.address || "") && "border-amber-300 bg-amber-50/50",
                    )}
                    {...field}
                  />
                </FormControl>
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
                          "h-11 border-2 font-mono text-sm transition-all focus:border-orange-500 focus:ring-2 focus:ring-orange-500/20",
                            parseFloat(field.value) !== nap.lat && "border-amber-300 bg-amber-50/50",
                        )}
                        onFocus={(e) => {
                          const input = e.currentTarget;
                          requestAnimationFrame(() => {
                            if (document.activeElement === input && input.selectionStart === 0 && input.selectionEnd === input.value.length && input.value.length > 0) {
                              input.setSelectionRange(input.value.length, input.value.length);
                            }
                          });
                        }}
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
                          "h-11 border-2 font-mono text-sm transition-all focus:border-orange-500 focus:ring-2 focus:ring-orange-500/20",
parseFloat(field.value) !== nap.lng && "border-amber-300 bg-amber-50/50",
                        )}
                        onFocus={(e) => {
                          const input = e.currentTarget;
                          requestAnimationFrame(() => {
                            if (document.activeElement === input && input.selectionStart === 0 && input.selectionEnd === input.value.length && input.value.length > 0) {
                              input.setSelectionRange(input.value.length, input.value.length);
                            }
                          });
                        }}
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
                    Location:{" "}
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
            {/* Delete Button */}
            <AlertDialog>
              <AlertDialogTrigger asChild>
                <Button
                  type="button"
                  variant="outline"
                  size="icon"
                  className="border-red-200 text-red-600 hover:bg-red-50 hover:text-red-700"
                  disabled={isPending}>
                  <Trash2 className="h-4 w-4" />
                </Button>
              </AlertDialogTrigger>
              <AlertDialogContent>
                <AlertDialogHeader>
                  <AlertDialogTitle>Delete NAP?</AlertDialogTitle>
                  <AlertDialogDescription>
                    This action cannot be undone. This will permanently delete this Network Access Point from the
                    database.
                  </AlertDialogDescription>
                </AlertDialogHeader>
                <AlertDialogFooter>
                  <AlertDialogCancel>Cancel</AlertDialogCancel>
                  <AlertDialogAction onClick={handleDelete} className="bg-red-600 hover:bg-red-700">
                    {deleteNAPMutation.isPending ? (
                      <>
                        <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                        Deleting...
                      </>
                    ) : (
                      "Delete"
                    )}
                  </AlertDialogAction>
                </AlertDialogFooter>
              </AlertDialogContent>
            </AlertDialog>

            {onCancel && (
              <Button type="button" variant="outline" onClick={onCancel} className="flex-1" disabled={isPending}>
                Cancel
              </Button>
            )}
            <Button
              type="submit"
              className={cn(
                "flex-1 gap-2 shadow-lg transition-all",
                hasChanges
                  ? "bg-gradient-to-r from-amber-500 to-orange-600 text-white shadow-orange-500/25 hover:from-amber-600 hover:to-orange-700"
                  : "bg-muted text-muted-foreground",
              )}
              disabled={isPending || !hasChanges}>
              {updateNAPMutation.isPending ? (
                <>
                  <Loader2 className="h-4 w-4 animate-spin" />
                  Saving...
                </>
              ) : (
                <>
                  <Save className="h-4 w-4" />
                  Save Changes
                </>
              )}
            </Button>
          </div>

          {/* Error Display */}
          <AnimatePresence>
            {(updateNAPMutation.isError || deleteNAPMutation.isError) && (
              <motion.div
                initial={{ opacity: 0, height: 0 }}
                animate={{ opacity: 1, height: "auto" }}
                exit={{ opacity: 0, height: 0 }}
                className="rounded-lg border border-red-200 bg-red-50 p-4 text-sm text-red-700">
                <div className="flex items-center gap-2">
                  <AlertCircle className="h-4 w-4" />
                  <span className="font-medium">
                    {updateNAPMutation.isError ? "Failed to update NAP" : "Failed to delete NAP"}
                  </span>
                </div>
                <p className="mt-1 text-red-600">
                  {extractErrorMessage(updateNAPMutation.isError ? updateNAPMutation.error : deleteNAPMutation.error)}
                </p>
              </motion.div>
            )}
          </AnimatePresence>
        </form>
      </Form>
    </motion.div>
  );
}
