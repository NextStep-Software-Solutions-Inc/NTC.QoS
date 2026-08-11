import { zodResolver } from "@hookform/resolvers/zod";
import { useQueryClient } from "@tanstack/react-query";
import { useForm } from "react-hook-form";
import { toast } from "sonner";
import { z } from "zod";

import { type File } from "@/components/features/file/types/File";
import SubmitButton from "@/components/SubmitButton";
import { Button } from "@/components/ui/button";
import { DialogClose, DialogFooter } from "@/components/ui/dialog";
import { Form, FormControl, FormField, FormItem, FormLabel, FormMessage } from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import { createMutationHookFromTemplate } from "@/hooks/createMutationHookFromTemplate";
import { updateFileRequest } from "../handlers/updateFileRequest";

const useUpdateFile = createMutationHookFromTemplate(updateFileRequest);

const updateFileSchema = z.object({
  name: z.string().trim().min(3, { message: "Name must be at least 3 characters long" }),
});

interface UpdateFileFormProps {
  onSuccess?: () => void;
  onClose: () => void;
  file: File;
}

export default function UpdateFileForm({ onClose, onSuccess, file }: UpdateFileFormProps) {
  const { mutateAsync, isPending } = useUpdateFile();
  const queryClient = useQueryClient();

  const form = useForm<z.infer<typeof updateFileSchema>>({
    resolver: zodResolver(updateFileSchema),
    progressive: true,
    defaultValues: { name: file.name },
  });

  const onSuccessHandler = () => {
    onSuccess?.();
    onClose?.();
  };

  const handleCancel = () => {
    void queryClient.cancelQueries({ queryKey: ["update-file", file.id] });
    onClose?.();
  };

  const handleSubmit = form.handleSubmit(async (values) => {
    toast.promise(mutateAsync({ pathParams: { id: file.id }, payload: values }), {
      loading: "Updating file...",
      success: (res) => {
        onSuccessHandler();
        return (res as { message: string }).message;
      },
      error: "Failed to update file",
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
                <FormItem className="grid grid-cols-4 items-center gap-4">
                  <FormLabel htmlFor="name" className="text-right">
                    Name
                  </FormLabel>
                  <div className="col-span-3">
                    <FormControl className="col-span-3">
                      <Input id="name" {...field} disabled={isPending} />
                    </FormControl>
                    <FormMessage />
                  </div>
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
            Save Changes
          </SubmitButton>
        </DialogFooter>
      </form>
    </Form>
  );
}
