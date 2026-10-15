import { zodResolver } from "@hookform/resolvers/zod";
import { useQueryClient } from "@tanstack/react-query";
import { useForm } from "react-hook-form";
import { toast } from "sonner";
import { z } from "zod";

import SubmitButton from "@/components/SubmitButton";
import { Button } from "@/components/ui/button";
import { DialogClose, DialogFooter } from "@/components/ui/dialog";
import { Form, FormControl, FormField, FormItem, FormLabel, FormMessage } from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import { createMutationHookFromTemplate } from "@/hooks/createMutationHookFromTemplate";
import { createProvinceRequest } from "../handlers/createProvinceRequest";

const useCreateProvince = createMutationHookFromTemplate(createProvinceRequest);

const createProvinceSchema = z.object({
  name: z.string().trim().min(3, { message: "Name must be at least 3 characters long" }),
  regionId: z.string().trim().min(1, { message: "Region ID is required" }),
});

interface CreateProvinceFormProps {
  onSuccess?: () => void;
  onClose: () => void;
  payload: {
    id: string;
    name: string;
    regionId?: string; // Optional, if you want to pre-fill the region ID
  };
}

export default function CreateProvinceForm({ onClose, onSuccess, payload }: CreateProvinceFormProps) {
  const { mutateAsync, isPending } = useCreateProvince();
  const queryClient = useQueryClient();

  const form = useForm<z.infer<typeof createProvinceSchema>>({
    resolver: zodResolver(createProvinceSchema),
    progressive: true,
    defaultValues: { name: payload.name },
  });

  const onSuccessHandler = () => {
    onSuccess?.();
    onClose?.();
  };

  const handleCancel = () => {
    void queryClient.cancelQueries({ queryKey: ["create-province", payload.id] });
    onClose?.();
  };

  const handleSubmit = form.handleSubmit(async (values) => {
    const payload = { name: values.name, regionId: values.regionId };

    toast.promise(mutateAsync({ pathParams: {}, payload }), {
      loading: "Creating province...",
      success: (res) => {
        onSuccessHandler();
        return (res as { message: string }).message;
      },
      error: "Failed to create province",
    });
  });

  return (
    <Form {...form}>
      <form onSubmit={handleSubmit}>
        <div className="grid gap-4 py-4">
          <FormField
            control={form.control}
            name="name"
            render={({ field }) => {
              return (
                <FormItem className="">
                  <FormLabel htmlFor="name" className="text-right">
                    Name
                  </FormLabel>
                  <FormControl className="col-span-3">
                    <Input id="name" {...field} disabled={isPending} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              );
            }}
          />
          <FormField
            control={form.control}
            name="regionId"
            render={({ field }) => {
              return (
                <FormItem className="">
                  <FormLabel htmlFor="regionId" className="text-right">
                    Region
                  </FormLabel>
                  <FormControl className="col-span-3">
                    <Input id="regionId" {...field} disabled={isPending} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              );
            }}
          />
        </div>
        <DialogFooter>
          <DialogClose asChild>
            <Button variant="ghost" onClick={handleCancel}>
              Cancel
            </Button>
          </DialogClose>
          <SubmitButton loading={isPending} disabled={!form.formState.isValid} type="submit">
            Create
          </SubmitButton>
        </DialogFooter>
      </form>
    </Form>
  );
}
