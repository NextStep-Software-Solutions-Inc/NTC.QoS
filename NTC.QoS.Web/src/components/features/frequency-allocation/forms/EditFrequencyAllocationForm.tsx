"use client";

import { useEffect } from "react";
import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import { toast } from "sonner";
import { z } from "zod";

import SubmitButton from "@/components/SubmitButton";
import { Button } from "@/components/ui/button";
import { Form, FormControl, FormField, FormItem, FormLabel, FormMessage } from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import { Switch } from "@/components/ui/switch";
import { Textarea } from "@/components/ui/textarea";
import { createMutationHookFromTemplate } from "@/hooks/createMutationHookFromTemplate";
import { editFrequencyAllocationRequest } from "../handlers/editFrequencyAllocationRequest";
import { type FrequencyAllocation } from "../types/FrequencyAllocation";

const useEditFrequencyAllocation = createMutationHookFromTemplate(editFrequencyAllocationRequest);

const formSchema = z.object({
  name: z.string().min(1, "Name is required"),
  notes: z.string().optional(),
  isActive: z.boolean(),
});

type FormValues = z.infer<typeof formSchema>;

interface EditFrequencyAllocationFormProps {
  allocation: FrequencyAllocation;
  onClose: () => void;
}

export default function EditFrequencyAllocationForm({ allocation, onClose }: EditFrequencyAllocationFormProps) {
  const { mutateAsync, isPending } = useEditFrequencyAllocation();

  const form = useForm<FormValues>({
    resolver: zodResolver(formSchema),
    defaultValues: {
      name: allocation.name ?? "",
      notes: allocation.notes ?? "",
      isActive: allocation.isActive ?? true,
    },
  });

  useEffect(() => {
    form.reset({
      name: allocation.name ?? "",
      notes: allocation.notes ?? "",
      isActive: allocation.isActive ?? true,
    });
  }, [allocation, form]);

  const onSubmit = async (values: FormValues) => {
    toast.promise(
      mutateAsync({
        pathParams: { id: allocation.id },
        payload: {
          name: values.name,
          notes: values.notes,
          isActive: values.isActive,
        },
      }),
      {
        loading: "Updating frequency allocation...",
        success: () => {
          onClose();
          return "Frequency allocation updated successfully.";
        },
        error: "Failed to update frequency allocation.",
      },
    );
  };

  return (
    <Form {...form}>
      <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-5">
        <FormField
          control={form.control}
          name="name"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Name</FormLabel>
              <FormControl>
                <Input {...field} />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />

        <FormField
          control={form.control}
          name="notes"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Notes</FormLabel>
              <FormControl>
                <Textarea rows={3} {...field} />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />

        <FormField
          control={form.control}
          name="isActive"
          render={({ field }) => (
            <FormItem className="flex items-center gap-3">
              <FormLabel className="mt-0">Active</FormLabel>
              <FormControl>
                <Switch checked={field.value} onCheckedChange={field.onChange} />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />

        <div className="flex justify-between gap-3 pt-2">
          <Button variant="outline" type="button" onClick={onClose} disabled={isPending}>
            Cancel
          </Button>
          <SubmitButton loading={isPending}>{isPending ? "Saving..." : "Save Changes"}</SubmitButton>
        </div>
      </form>
    </Form>
  );
}
