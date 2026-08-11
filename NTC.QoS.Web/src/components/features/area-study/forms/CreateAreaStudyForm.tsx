import React from "react";
import { zodResolver } from "@hookform/resolvers/zod";
import { useQueryClient } from "@tanstack/react-query";
import { useForm } from "react-hook-form";
import { toast } from "sonner";
import { z } from "zod";

import SubmitButton from "@/components/SubmitButton";
import { Button } from "@/components/ui/button";
import { Form, FormControl, FormField, FormItem, FormLabel, FormMessage } from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";
import { createMutationHookFromTemplate } from "@/hooks/createMutationHookFromTemplate";
import { createAreaStudyRequest } from "../handlers/createAreaStudyRequest";
import { type AreaStudy } from "../types/AreaStudy";

const areaStudyFormSchema = z.object({
  name: z.string().min(1, "Name is required"),
  description: z.string().optional(),
});

interface CreateAreaStudyFormProps {
  onCreated?: (areaStudy: AreaStudy) => void;
  onCancel?: () => void;
}

const useCreateAreaStudy = createMutationHookFromTemplate(createAreaStudyRequest);

export default function CreateAreaStudyForm({ onCreated, onCancel }: CreateAreaStudyFormProps) {
  const { mutateAsync, isPending } = useCreateAreaStudy();
  const queryClient = useQueryClient();

  const form = useForm<z.infer<typeof areaStudyFormSchema>>({
    resolver: zodResolver(areaStudyFormSchema),
    defaultValues: { name: "", description: "" },
    mode: "onChange",
  });

  const onSubmit = async (values: z.infer<typeof areaStudyFormSchema>) => {
    toast.promise(
      mutateAsync({ pathParams: {}, payload: { name: values.name, description: values.description ?? undefined } }),
      {
        loading: "Creating area study...",
        success: (response) => {
          void queryClient.invalidateQueries({ predicate: (query) => query.queryKey[0] === "get-area-studies" });
          form.reset();
          onCreated?.(response.data);
          onCancel?.();
          return response.message ?? "Area study created successfully.";
        },
        error: "Failed to create area study.",
      },
    );
  };

  const handleCancel = () => {
    form.reset();
    onCancel?.();
  };

  return (
    <Form {...form}>
      <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-6 max-w-md">
        {/* Area Study Name */}
        <FormField
          control={form.control}
          name="name"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Name *</FormLabel>
              <FormControl>
                <Input placeholder="Enter area study name" {...field} />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />
        {/* Description */}
        <FormField
          control={form.control}
          name="description"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Description</FormLabel>
              <FormControl>
                <Textarea placeholder="Enter description" rows={4} className="resize-none" {...field} />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />
        {/* Buttons */}
        <div className="flex justify-between gap-4 pt-2">
          <Button variant="outline" type="button" onClick={handleCancel}>
            Cancel
          </Button>
          <SubmitButton loading={isPending}>Create</SubmitButton>
        </div>
      </form>
    </Form>
  );
}
