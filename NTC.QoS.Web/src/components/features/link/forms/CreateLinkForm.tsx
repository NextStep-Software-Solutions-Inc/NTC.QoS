"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { useQueryClient } from "@tanstack/react-query";
import { useForm } from "react-hook-form";
import { toast } from "sonner";
import { z } from "zod";

import SubmitButton from "@/components/SubmitButton";
import { Button } from "@/components/ui/button";
import { Form, FormControl, FormField, FormItem, FormLabel, FormMessage } from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { createMutationHookFromTemplate } from "@/hooks/createMutationHookFromTemplate";
import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";
import { getBandsRequest } from "../../band/handlers/getBandsRequest";
import { getServiceProvidersRequest } from "../../service-provider/handlers/getServiceProviders";
import { createLinkRequest } from "../handlers/createLinkRequest";
import type { Link } from "../types/Link";

interface CreateLinkFormProps {
  onCreated?: (link: Link) => void;
  onCancel?: () => void;
}

const useGetServiceProviders = createQueryHookFromTemplate(getServiceProvidersRequest);
const useGetBands = createQueryHookFromTemplate(getBandsRequest);
const useCreateLink = createMutationHookFromTemplate(createLinkRequest);

const formSchema = z.object({
  serviceProvider: z.string().min(1, "Service provider is required"),
  type: z.enum(["Downlink", "Uplink"], { required_error: "Type is required" }),
  band: z.string().min(1, "Band is required"),
  centerFrequency: z.coerce.number().positive(),
  lowerFrequency: z.coerce.number().positive(),
  upperFrequency: z.coerce.number().positive(),
  rxThreshold: z.coerce.number(),
});

export default function CreateLinkForm({ onCreated, onCancel }: CreateLinkFormProps) {
  const { mutateAsync, isPending } = useCreateLink();
  const queryClient = useQueryClient();

  const { data: serviceProvidersData } = useGetServiceProviders({
    pathParams: {},
    queryParams: { pageSize: 100, pageNumber: 1, serviceTypes: ["wireless"] },
  });
  const serviceProviders = serviceProvidersData?.data ?? [];

  const { data: bandsData } = useGetBands({ pathParams: {}, queryParams: { pageSize: 100, pageNumber: 1 } });
  const bands = bandsData?.data ?? [];

  const form = useForm<z.infer<typeof formSchema>>({
    resolver: zodResolver(formSchema),
    defaultValues: {
      serviceProvider: "",
      type: "Downlink",
      band: "",
      centerFrequency: 0,
      lowerFrequency: 0,
      upperFrequency: 0,
      rxThreshold: 0,
    },
  });

  const onSubmit = async (values: z.infer<typeof formSchema>) => {
    toast.promise(
      mutateAsync({
        pathParams: {},
        payload: {
          type: values.type,
          centerFrequency: values.centerFrequency,
          lowerFrequency: values.lowerFrequency,
          upperFrequency: values.upperFrequency,
          rxThreshold: values.rxThreshold,
          bandId: values.band,
          serviceProviderId: values.serviceProvider,
        },
      }),
      {
        loading: "Creating link...",
        success: (response) => {
          void queryClient.invalidateQueries({ predicate: (query) => query.queryKey[0] === "get-links" });
          form.reset();
          onCreated?.(response.data);
          return response.message || "Link created successfully.";
        },
        error: (error: unknown) => {
          console.error(error);
          return "Failed to create link.";
        },
      },
    );
  };

  return (
    <Form {...form}>
      <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-6 max-w-md">
        {/* Service Provider */}
        <FormField
          control={form.control}
          name="serviceProvider"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Service Provider</FormLabel>
              <Select onValueChange={field.onChange} value={field.value}>
                <FormControl>
                  <SelectTrigger>
                    <SelectValue placeholder="Select a service provider" />
                  </SelectTrigger>
                </FormControl>
                <SelectContent>
                  {serviceProviders.map((sp) => (
                    <SelectItem key={sp.id} value={sp.id}>
                      {sp.provider.name}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
              <FormMessage />
            </FormItem>
          )}
        />

        {/* Type */}
        <FormField
          control={form.control}
          name="type"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Type</FormLabel>
              <Select onValueChange={field.onChange} value={field.value}>
                <FormControl>
                  <SelectTrigger>
                    <SelectValue placeholder="Select type" />
                  </SelectTrigger>
                </FormControl>
                <SelectContent>
                  <SelectItem value="Downlink">Downlink</SelectItem>
                  <SelectItem value="Uplink">Uplink</SelectItem>
                </SelectContent>
              </Select>
              <FormMessage />
            </FormItem>
          )}
        />

        {/* Band */}
        <FormField
          control={form.control}
          name="band"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Band</FormLabel>
              <Select onValueChange={field.onChange} value={field.value}>
                <FormControl>
                  <SelectTrigger>
                    <SelectValue placeholder="Select a band" />
                  </SelectTrigger>
                </FormControl>
                <SelectContent>
                  {bands.map((band) => (
                    <SelectItem key={band.id} value={band.id}>
                      {band.name}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
              <FormMessage />
            </FormItem>
          )}
        />

        {/* Center Frequency */}
        <FormField
          control={form.control}
          name="centerFrequency"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Center Frequency (MHz)</FormLabel>
              <FormControl>
                <Input type="number" {...field} />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />

        {/* Lower Frequency */}
        <FormField
          control={form.control}
          name="lowerFrequency"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Lower Frequency (MHz)</FormLabel>
              <FormControl>
                <Input type="number" {...field} />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />

        {/* Upper Frequency */}
        <FormField
          control={form.control}
          name="upperFrequency"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Upper Frequency (MHz)</FormLabel>
              <FormControl>
                <Input type="number" {...field} />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />

        {/* RX Threshold */}
        <FormField
          control={form.control}
          name="rxThreshold"
          render={({ field }) => (
            <FormItem>
              <FormLabel>RX Threshold (dBm)</FormLabel>
              <FormControl>
                <Input type="number" {...field} />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />

        {/* Buttons */}
        <div className="flex justify-between gap-4 pt-4">
          <Button
            variant="outline"
            type="button"
            onClick={() => {
              form.reset();
              onCancel?.();
            }}>
            Cancel
          </Button>
          <SubmitButton loading={isPending}>Create</SubmitButton>
        </div>
      </form>
    </Form>
  );
}
