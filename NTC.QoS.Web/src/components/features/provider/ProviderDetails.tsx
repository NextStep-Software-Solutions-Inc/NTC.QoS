"use client";

import { useRef, useState } from "react";
import { useParams, useRouter } from "next/navigation";
import { useQueryClient } from "@tanstack/react-query";
import { ArrowLeft, Building, Building2, CircleCheck, Router, Trash2, Wifi } from "lucide-react";
import { toast } from "sonner";

import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { Separator } from "@/components/ui/separator";
import WithPlaceholder from "@/components/WithPlaceholder";
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
import { createMutationHookFromTemplate } from "@/hooks/createMutationHookFromTemplate";
import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";
import { cn } from "@/lib/utils";
import AddServiceProviderDialog from "../service-provider/dialogs/AddServiceProviderDialog";
import { deleteServiceProviderRequest } from "../service-provider/handlers/deleteServiceProviderRequest";
import { getServiceProvidersRequest } from "../service-provider/handlers/getServiceProviders";
import { type ServiceProviderDetails } from "../service-provider/types/serviceProvider";
import { getServicesRequest } from "../service/handlers/getServicesRequest";
import { DropdownEditProvider } from "./dropdown-menu/DropDownEditProvider";
import { EditProviderForm, type EditProviderFormHandles } from "./EditProviderForm";
import { deleteProvidersRequest } from "./handlers/deleteProviderRequest";
import { getProviderDetailsRequest } from "./handlers/getProviderDetailsRequest";
import { ProviderDetailsSkeleton } from "./skeleton/ProviderDetailsSkeleton";

const useGetProviderDetails = createQueryHookFromTemplate(getProviderDetailsRequest);
const useGetServices = createQueryHookFromTemplate(getServicesRequest);
const useGetServiceProviders = createQueryHookFromTemplate(getServiceProvidersRequest);
const useDeleteProviders = createMutationHookFromTemplate(deleteProvidersRequest);
const useDeleteServiceProvider = createMutationHookFromTemplate(deleteServiceProviderRequest);

export default function ProviderDetails() {
  const [isServiceProviderDialogOpen, setIsServiceProviderDialogOpen] = useState(false);
  const [serviceProviderToDelete, setServiceProviderToDelete] = useState<ServiceProviderDetails | null>(null);

  const formRef = useRef<EditProviderFormHandles>(null);
  const [editProvider, setEditProvider] = useState(false);
  const router = useRouter();
  const params = useParams();
  const providerId = params.providerId as string;
  const { data: provider, isLoading: isLoadingProvider } = useGetProviderDetails({
    pathParams: { id: providerId },
    queryParams: {},
  });

  const { data: services, isLoading: isLoadingServices } = useGetServices({
    pathParams: {},
    queryParams: { pageSize: 100, pageNumber: 1, providerIds: [providerId] },
  });

  const { data: serviceProviders, isLoading: isLoadingServiceProviders } = useGetServiceProviders({
    pathParams: {},
    queryParams: { pageSize: 100, pageNumber: 1, providerIds: [providerId] },
  });

  const { mutateAsync: deleteServiceProvider, isPending: isDeletingServiceProvider } =
    useDeleteServiceProvider();
  const { mutateAsync, isPending } = useDeleteProviders();
  const queryClient = useQueryClient();

  const handleBackTrack = () => {
    router.back();
  };

  const handleSave = () => {
    if (formRef.current) {
      formRef.current.submitForm();
    }
  };

  const handleDeleteServiceProvider = async () => {
    if (!serviceProviderToDelete) return;
    const serviceName = serviceProviderToDelete.service?.name ?? "Service";

    const syncProviderServiceQueries = async () => {
      await Promise.all([
        queryClient.invalidateQueries({ predicate: (q) => q.queryKey[0] === "get-service-providers" }),
        queryClient.invalidateQueries({ predicate: (q) => q.queryKey[0] === "get-services" }),
        queryClient.invalidateQueries({ predicate: (q) => q.queryKey[0] === "get-providers" }),
      ]);

      // Refetch active views immediately so stale cards disappear without a hard refresh.
      await Promise.all([
        queryClient.refetchQueries({ predicate: (q) => q.queryKey[0] === "get-service-providers", type: "active" }),
        queryClient.refetchQueries({ predicate: (q) => q.queryKey[0] === "get-services", type: "active" }),
      ]);
    };

    try {
      await deleteServiceProvider({ pathParams: { id: serviceProviderToDelete.id } });
      await syncProviderServiceQueries();
      toast.success(`"${serviceName}" removed from this provider.`);
    } catch (error) {
      const status =
        typeof error === "object" && error !== null && "status" in error
          ? Number((error as { status?: unknown }).status)
          : undefined;

      // If the backend reports missing record, treat it as already deleted and sync the UI.
      if (status === 404) {
        await syncProviderServiceQueries();
        toast.success(`"${serviceName}" was already removed.`);
      } else {
        await syncProviderServiceQueries();
        toast.error("Failed to remove service.");
      }
    } finally {
      setServiceProviderToDelete(null);
    }
  };

  const onDeleteProvider = () => {
    toast.promise(
      mutateAsync(
        { pathParams: { id: providerId } },
        {
          onSuccess: (res) => {
            void queryClient.invalidateQueries({ predicate: (query) => query.queryKey[0] === "get-providers" });
            return res.message;
          },
        },
      ),
      {
        loading: "Deleting providers...",
        success: (res) => {
          handleBackTrack();
          return res.message;
        },
        error: "Failed to delete providers.",
      },
    );
  };

  const isLoading = isLoadingServices || isLoadingProvider || isLoadingServiceProviders;
  return (
    <>
    <WithPlaceholder isLoading={isLoading} placeholder={<ProviderDetailsSkeleton />}>
      <div className="p-6 w-full mx-auto space-y-4">
        <div className="w-full flex justify-between items-center space-x-4">
          <div className="flex items-center space-x-2">
            <Button variant="ghost" size="icon" onClick={handleBackTrack}>
              <ArrowLeft className="w-5 h-5" />
            </Button>
            <section className="flex items-center gap-x-1">
              <div
                style={{
                  backgroundColor: provider?.data.color, // adds ~20% opacity
                }}
                className=" text-white font-semibold border rounded-full w-10 h-10 flex items-center justify-center">
                {provider?.data.name.charAt(0).toUpperCase()}
              </div>
              <h1 className="font-semibold uppercase">{provider?.data.name}</h1>
            </section>
          </div>
        </div>

        <Card>
          <CardContent className="w-full p-6 space-y-6">
            <div className="flex items-center justify-between">
              <h1 className="font-semibold text-[18px]">Basic Information</h1>
              {!editProvider && (
                <DropdownEditProvider
                  isPending={isPending}
                  providerId={providerId}
                  deleteProvider={onDeleteProvider}
                  editProvider={() => setEditProvider(true)}
                />
              )}
              {editProvider && (
                <div className="flex items-center space-x-2">
                  <Button onClick={() => setEditProvider(false)} variant="ghost" type="button">
                    Cancel
                  </Button>
                  <Button onClick={handleSave} type="button">
                    Save
                  </Button>
                </div>
              )}
            </div>
            {!editProvider && (
              <div className="w-full grid sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4">
                <div className="flex items-center space-x-4">
                  <div className="p-[.5rem] rounded-full border bg-slate-200">
                    <Building />
                  </div>
                  <section>
                    <div className="text-sm text-muted-foreground">Provider Name</div>
                    <div className="font-medium">{provider?.data.name}</div>
                  </section>
                </div>
                <div className="flex items-center space-x-4">
                  <div className="p-[.5rem] rounded-full border bg-slate-200">
                    <CircleCheck />
                  </div>
                  <section>
                    <div className="text-sm text-muted-foreground">License</div>
                    <p className="text-sm ">
                      {provider?.data.licenseNumber === "" ? "Unknown" : provider?.data.licenseNumber}
                    </p>
                  </section>
                </div>

                <div className="flex items-center space-x-4">
                  <div className="p-[.5rem] rounded-full border bg-slate-200">
                    <section style={{ backgroundColor: provider?.data.color }} className={`w-6 h-6 rounded-full`} />
                  </div>
                  <section>
                    <div className="text-sm text-muted-foreground">Color</div>
                    <div className="font-medium">#0070BA</div>
                  </section>
                </div>

                <div className="flex items-center space-x-4">
                  <div className="p-[.5rem] rounded-full border bg-slate-200">
                    <Building2 />
                  </div>
                  <section>
                    <div className="text-sm text-muted-foreground">Services</div>
                    <div className="font-medium">
                      {serviceProviders?.data?.length === 1
                        ? `${serviceProviders?.data?.length} Available Service`
                        : `${serviceProviders?.data?.length ?? 0} Available Services`}
                    </div>
                  </section>
                </div>
              </div>
            )}
            {editProvider && provider?.data && (
              <EditProviderForm ref={formRef} handleSuccess={() => setEditProvider(false)} provider={provider.data} />
            )}

            <Separator />
            <div>
              <section className={cn(editProvider ? "w-full flex items-center justify-between" : "")}>
                <div className="w-full flex items-center justify-between">
                  <h1 className="font-semibold mb-2 text-[18px]">Service Details</h1>
                  <Button variant="outline" onClick={() => setIsServiceProviderDialogOpen(true)}>
                    + Add Service
                  </Button>
                </div>

                {provider?.data && (
                  <AddServiceProviderDialog
                    open={isServiceProviderDialogOpen}
                    onOpenChange={() => setIsServiceProviderDialogOpen(!isServiceProviderDialogOpen)}
                    provider={provider.data}
                    onCreated={() => {
                      void queryClient.invalidateQueries({ predicate: (q) => q.queryKey[0] === "get-service-providers" });
                    }}
                    onSuccessDialogClose={() => setIsServiceProviderDialogOpen(false)}
                  />
                )}
              </section>
              <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-4 gap-4">
                  {serviceProviders?.data?.length === 0 && (
                    <div className="col-span-4 text-center text-muted-foreground">
                      {"No services assigned to this provider yet."}
                    </div>
                  )}
                  {serviceProviders?.data?.map((sp) => (
                    <Card key={sp.id} className="flex flex-col gap-2 border p-4">
                      <div className="text-xl font-bold truncate">{sp.service?.name}</div>
                      <div className="flex items-center justify-between gap-2">
                        <div className="flex items-center gap-x-1 border rounded-full py-1 px-3 text-sm text-muted-foreground min-w-0 overflow-hidden">
                          <span className="truncate">{sp.service?.type}</span>
                          <span className="flex-shrink-0">
                            {sp.service?.type === "Wired" ? <Router className="w-4 h-4" /> : <Wifi className="w-4 h-4" />}
                          </span>
                        </div>
                        <Button
                          variant="ghost"
                          size="icon"
                          className="h-8 w-8 flex-shrink-0 text-destructive hover:text-destructive hover:bg-destructive/10"
                          onClick={() => setServiceProviderToDelete(sp)}
                          title="Remove service">
                          <Trash2 className="h-4 w-4" />
                        </Button>
                      </div>
                    </Card>
                  ))}
              </div>
            </div>
          </CardContent>
        </Card>
      </div>
    </WithPlaceholder>

      {/* Delete Service Provider Confirmation */}
      <AlertDialog
        open={!!serviceProviderToDelete}
        onOpenChange={(open) => { if (!open) setServiceProviderToDelete(null); }}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Remove Service</AlertDialogTitle>
            <AlertDialogDescription>
              Are you sure you want to remove{" "}
              <span className="font-semibold">{serviceProviderToDelete?.service?.name}</span> from this provider? This
              action cannot be undone.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancel</AlertDialogCancel>
            <AlertDialogAction
              className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
              onClick={handleDeleteServiceProvider}
              disabled={isDeletingServiceProvider}>
              {isDeletingServiceProvider ? "Removing..." : "Remove"}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </>
  );
}
