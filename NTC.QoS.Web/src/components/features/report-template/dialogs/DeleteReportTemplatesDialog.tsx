import { useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";

import ConfirmAlertDialog from "@/components/ConfirmAlertDialog";
import { createMutationHookFromTemplate } from "@/hooks/createMutationHookFromTemplate";
import { deleteReportTemplateRequest } from "../handlers/deleteReportTemplateRequest";

interface DeleteReportTemplatesDialogProps {
  open: boolean;
  onOpenChange: (state: boolean) => void;
  ids: string[];
  onSuccess?: () => void;
  onCancel?: () => void;
}

const useDeleteReportTemplate = createMutationHookFromTemplate(deleteReportTemplateRequest);

export default function DeleteReportTemplatesDialog({
  ids,
  open,
  onOpenChange,
  onCancel,
  onSuccess,
}: DeleteReportTemplatesDialogProps) {
  const { mutateAsync, isPending } = useDeleteReportTemplate();
  const queryClient = useQueryClient();

  const onDelete = () => {
    for (const id of ids) {
      toast.promise(
        mutateAsync(
          { pathParams: { id } },
          {
            onSuccess: () => {
              void queryClient.invalidateQueries({
                predicate: (query) => query.queryKey[0] === "get-report-templates",
              });
            },
          },
        ),
        {
          loading: "Deleting report template...",
          success: (res) => {
            onSuccess?.();
            onOpenChange(false);
            return res.message ?? "Report template deleted successfully!";
          },
          error: "Failed to delete report template.",
        },
      );
    }
  };

  const handleCancelDelete = () => {
    void queryClient.cancelQueries({});
    onCancel?.();
  };

  return (
    <ConfirmAlertDialog
      id=""
      loading={isPending}
      onConfirm={onDelete}
      onCancel={handleCancelDelete}
      open={open}
      onOpenChange={onOpenChange}
      title="Are you absolutely sure you want to delete these report templates?"
      description="This action cannot be undone. This will permanently delete the record from the server."
    />
  );
}
