import { useState } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { AlertCircle, RotateCcw } from "lucide-react";
import { toast } from "sonner";

import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
import { createMutationHookFromTemplate } from "@/hooks/createMutationHookFromTemplate";
import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";
import { extractErrorMessage } from "@/lib/extract-error-message";
import { type Provider } from "../../provider/types/Provider";
import { addServiceProviderRequest } from "../../service-provider/handlers/addServiceProvider";
import { restoreServiceProviderRequest } from "../../service-provider/handlers/restoreServiceProviderRequest";
import { getServicesRequest } from "../handlers/getServicesRequest";

type ServiceConflict = {
  serviceId: string;
  deletedId: string;
  serviceName: string;
};

/** Extracts the deleted service-provider ID from the backend conflict error. */
function extractDeletedServiceProviderId(error: unknown): string | null {
  if (error && typeof error === "object" && "response" in error) {
    const response = (error as { response?: { message?: string; errors?: Record<string, string[]> } }).response;
    const sources = [
      ...(response?.errors?.DeletedServiceProviderExists ?? []),
      response?.message ?? "",
    ];
    for (const text of sources) {
      const match = text.match(/\(Id:\s*([0-9a-f-]{36})\)/i);
      if (match?.[1]) return match[1];
    }
  }
  return null;
}

interface ServiceFormProps {
  onCreated?: () => void;
  onClose?: () => void;
  provider: Provider;
  onSuccessDialogClose?: () => void;
}

const useGetService = createQueryHookFromTemplate(getServicesRequest);
const useCreateServiceProvider = createMutationHookFromTemplate(addServiceProviderRequest);
const useRestoreServiceProvider = createMutationHookFromTemplate(restoreServiceProviderRequest);

const AddServiceForm = ({ onClose, onCreated, provider, onSuccessDialogClose }: ServiceFormProps) => {
  const [selected, setSelected] = useState<string[]>([]);
  const [conflicts, setConflicts] = useState<ServiceConflict[]>([]);
  const [restoringIds, setRestoringIds] = useState<Set<string>>(new Set());

  const { data: services } = useGetService({ pathParams: {}, queryParams: { pageSize: 100, pageNumber: 1 } });

  const { mutateAsync } = useCreateServiceProvider();
  const { mutateAsync: restoreServiceProvider } = useRestoreServiceProvider();
  const queryClient = useQueryClient();

  const handleCheckboxChange = (serviceId: string) => {
    setConflicts((prev) => prev.filter((c) => c.serviceId !== serviceId));
    setSelected((prev) =>
      prev.includes(serviceId) ? prev.filter((item) => item !== serviceId) : [...prev, serviceId],
    );
  };

  const handleSubmitService = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    if (!selected.length) {
      toast.error("Please select at least one service.");
      return;
    }

    setConflicts([]);

    const results = await Promise.allSettled(
      selected.map((serviceId) =>
        mutateAsync({ pathParams: {}, payload: { providerId: provider.id, serviceId, mnc: "", mcc: "" } }).then(
          (res) => ({ serviceId, res }),
        ),
      ),
    );

    const newConflicts: ServiceConflict[] = [];
    let successCount = 0;

    for (const result of results) {
      if (result.status === "fulfilled") {
        successCount++;
      } else {
        const deletedId = extractDeletedServiceProviderId(result.reason);
        if (deletedId) {
          // Recover serviceId from the settled index
          const idx = results.indexOf(result);
          const sid = selected[idx] ?? "";
          const serviceName = services?.data.find((s) => s.id === sid)?.name ?? sid;
          newConflicts.push({ serviceId: sid, deletedId, serviceName });
        } else {
          toast.error(extractErrorMessage(result.reason));
        }
      }
    }

    if (newConflicts.length) {
      setConflicts(newConflicts);
    }

    if (successCount > 0) {
      void queryClient.invalidateQueries({ predicate: (query) => query.queryKey[0] === "get-service-providers" });
      if (!newConflicts.length) {
        toast.success("Service(s) added successfully!");
        onCreated?.();
        onSuccessDialogClose?.();
      } else {
        toast.success(`${successCount} service(s) added. ${newConflicts.length} require restore.`);
      }
    }
  };

  const handleRestore = async (conflict: ServiceConflict) => {
    setRestoringIds((prev) => new Set(prev).add(conflict.deletedId));
    try {
      await restoreServiceProvider({ pathParams: { id: conflict.deletedId } });
      setConflicts((prev) => prev.filter((c) => c.deletedId !== conflict.deletedId));
      void queryClient.invalidateQueries({ predicate: (query) => query.queryKey[0] === "get-service-providers" });
      toast.success(`"${conflict.serviceName}" restored successfully.`);
      if (conflicts.length === 1) {
        onCreated?.();
        onSuccessDialogClose?.();
      }
    } catch (error) {
      toast.error(extractErrorMessage(error));
    } finally {
      setRestoringIds((prev) => {
        const next = new Set(prev);
        next.delete(conflict.deletedId);
        return next;
      });
    }
  };

  const handleRestoreAll = async () => {
    await Promise.allSettled(conflicts.map(handleRestore));
  };

  return (
    <form onSubmit={handleSubmitService}>
      <div className=" w-full mx-auto">
        <div className="flex flex-wrap gap-2">
          {services?.data.map((service) => (
            <div key={service.id} className="flex items-center justify-between gap-x-3 rounded-xl p-2">
              <div className="flex items-center gap-2">
                <Checkbox
                  id={service.id}
                  checked={selected.includes(service.id)}
                  onCheckedChange={() => handleCheckboxChange(service.id)}
                />
                <label htmlFor={service.id} className="text-sm">
                  {service.name}
                </label>
              </div>
              <Button variant="outline" size="sm" className="rounded-full px-2 py-0.5 text-xs">
                {service.type}
              </Button>
            </div>
          ))}
        </div>

        {/* Restore conflicts banner */}
        {conflicts.length > 0 && (
          <div className="mt-4 rounded-md border border-destructive/50 bg-destructive/10 p-4 text-destructive">
            <div className="flex items-start gap-3">
              <AlertCircle className="mt-0.5 h-4 w-4 shrink-0" />
              <div className="flex-1">
                <p className="text-sm font-semibold">
                  {conflicts.length === 1
                    ? "A deleted service already exists for this provider"
                    : `${conflicts.length} deleted services already exist for this provider`}
                </p>
                <p className="mt-1 text-sm">
                  The following service(s) were previously deleted. Restore them to re-enable.
                </p>
                <ul className="mt-2 space-y-1">
                  {conflicts.map((conflict) => (
                    <li key={conflict.deletedId} className="flex items-center justify-between gap-2">
                      <span className="text-sm font-medium">{conflict.serviceName}</span>
                      <Button
                        type="button"
                        size="sm"
                        variant="destructive"
                        className="gap-1"
                        disabled={restoringIds.has(conflict.deletedId)}
                        onClick={() => handleRestore(conflict)}>
                        <RotateCcw className="h-3 w-3" />
                        {restoringIds.has(conflict.deletedId) ? "Restoring..." : "Restore"}
                      </Button>
                    </li>
                  ))}
                </ul>
                {conflicts.length > 1 && (
                  <Button
                    type="button"
                    size="sm"
                    variant="destructive"
                    className="mt-3 gap-1"
                    disabled={restoringIds.size > 0}
                    onClick={handleRestoreAll}>
                    <RotateCcw className="h-3 w-3" />
                    {restoringIds.size > 0 ? "Restoring..." : "Restore All"}
                  </Button>
                )}
              </div>
            </div>
          </div>
        )}

        <div className="mt-4 flex items-start gap-2 rounded-md border border-amber-200 bg-amber-50 p-3 text-sm text-amber-800">
          <AlertCircle className="mt-0.5 h-4 w-4 shrink-0 text-amber-600" />
          <span>
            If the service you need is not listed above, please request it from the{" "}
            <span className="font-semibold">Super Admin</span>. New service types can only be created by a Super Admin.
          </span>
        </div>

        <div className="flex justify-between mt-4">
          <Button variant="ghost" className="" onClick={onClose}>
            Close
          </Button>
          <Button type="submit" className="">
            Update
          </Button>
        </div>
      </div>
    </form>
  );
};

export default AddServiceForm;
