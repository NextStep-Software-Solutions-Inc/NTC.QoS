import React, { useEffect } from "react";
import { zodResolver } from "@hookform/resolvers/zod";
import { useQueryClient } from "@tanstack/react-query";
import { useForm } from "react-hook-form";
import { toast } from "sonner";
import { z } from "zod";

import SubmitButton from "@/components/SubmitButton";
import { Button } from "@/components/ui/button";
import { Form, FormControl, FormField, FormItem, FormLabel, FormMessage } from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import { createMutationHookFromTemplate } from "@/hooks/createMutationHookFromTemplate";
import { editLinkRequest } from "../handlers/editLinkRequest";
import { type Link } from "../types/Link";

const editLinkFormSchema = z.object({
  type: z.string().min(1, "Type is required"),
  centerFrequency: z.coerce.number().min(1, "Center frequency is required"),
  lowerFrequency: z.coerce.number().min(1, "Lower frequency is required"),
  upperFrequency: z.coerce.number().min(1, "Upper frequency is required"),
  rxThreshold: z.coerce.number().min(1, "RX threshold is required"),
});

interface EditLinkFormProps {
  link?: Link;
  onClose?: () => void;
}

const useEditLink = createMutationHookFromTemplate(editLinkRequest);

export default function EditLinkForm({ onClose, link }: EditLinkFormProps) {
  const { mutateAsync, isPending } = useEditLink();
  const queryClient = useQueryClient();

  const form = useForm<z.infer<typeof editLinkFormSchema>>({
    resolver: zodResolver(editLinkFormSchema),
    defaultValues: {
      type: link?.type ?? "",
      centerFrequency: link?.centerFrequency ?? 0,
      lowerFrequency: link?.lowerFrequency ?? 0,
      upperFrequency: link?.upperFrequency ?? 0,
      rxThreshold: link?.rxThreshold ?? 0,
    },
    mode: "onChange",
  });

  // Keep form values in sync with link prop
  useEffect(() => {
    if (link) {
      form.reset({
        type: link.type ?? "",
        centerFrequency: link.centerFrequency ?? 0,
        lowerFrequency: link.lowerFrequency ?? 0,
        upperFrequency: link.upperFrequency ?? 0,
        rxThreshold: link.rxThreshold ?? 0,
      });
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [link]);

  // Frequency handling logic
  // (Retain the logic if you want to auto-calculate center from bounds, or vice versa)
  // For now, keep it simple and let user edit all fields

  const onSubmit = async (values: z.infer<typeof editLinkFormSchema>) => {
    toast.promise(
      mutateAsync({
        pathParams: { id: link?.id ?? "" },
        payload: {
          type: values.type,
          centerFrequency: values.centerFrequency,
          lowerFrequency: values.lowerFrequency,
          upperFrequency: values.upperFrequency,
          rxThreshold: values.rxThreshold,
        },
      }),
      {
        loading: "Updating link...",
        success: (response) => {
          void queryClient.invalidateQueries({ predicate: (query) => query.queryKey[0] === "get-links" });
          onClose?.();
          return response.message ?? "Link updated successfully.";
        },
        error: (error: unknown) => {
          console.error(error);
          return "Failed to update link.";
        },
      },
    );
  };

  return (
    <Form {...form}>
      <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-6 max-w-md">
        {/* Link Type */}
        <FormField
          control={form.control}
          name="type"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Type</FormLabel>
              <FormControl>
                <select
                  {...field}
                  className="mt-1 block w-full border border-gray-300 rounded-md shadow-sm focus:ring focus:ring-opacity-50"
                  required>
                  <option value="">Select type</option>
                  <option value="Downlink">Downlink</option>
                  <option value="Uplink">Uplink</option>
                </select>
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />
        {/* Frequency Inputs */}
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
        <div className="flex justify-between gap-3 pt-2">
          <Button variant="outline" type="button" onClick={onClose}>
            Cancel
          </Button>
          <SubmitButton loading={isPending}>{isPending ? "Updating..." : "Save"}</SubmitButton>
        </div>
      </form>
    </Form>
  );
}
