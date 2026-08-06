"use client";

import { useState } from "react";
import { zodResolver } from "@hookform/resolvers/zod";
import { CheckCircle2, Pencil, Plus } from "lucide-react";
import { useForm } from "react-hook-form";
import { toast } from "sonner";
import { z } from "zod";

import SubmitButton from "@/components/SubmitButton";
import { Button } from "@/components/ui/button";
import { Form, FormControl, FormField, FormItem, FormLabel, FormMessage } from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Textarea } from "@/components/ui/textarea";
import { createMutationHookFromTemplate } from "@/hooks/createMutationHookFromTemplate";
import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";
import { getBandsRequest } from "../../band/handlers/getBandsRequest";
import { createLinkRequest } from "../../link/handlers/createLinkRequest";
import { deleteLinksRequest } from "../../link/handlers/deleteLinksRequest";
import { getServiceProvidersRequest } from "../../service-provider/handlers/getServiceProviders";
import { createFrequencyAllocationRequest } from "../handlers/createFrequencyAllocationRequest";
import LinkFrequencyConfigDialog, {
  type LinkFrequencyValues,
  FreqBadges,
} from "../dialogs/LinkFrequencyConfigDialog";

const useCreateLink = createMutationHookFromTemplate(createLinkRequest);
const useDeleteLink = createMutationHookFromTemplate(deleteLinksRequest);
const useCreateFrequencyAllocation = createMutationHookFromTemplate(createFrequencyAllocationRequest);
const useGetServiceProviders = createQueryHookFromTemplate(getServiceProvidersRequest);
const useGetBands = createQueryHookFromTemplate(getBandsRequest);

const formSchema = z.object({
  name: z.string().min(1, "Name is required"),
  notes: z.string().optional(),
  serviceProviderId: z.string().min(1, "Service provider is required"),
  bandId: z.string().min(1, "Band is required"),
});

type FormValues = z.infer<typeof formSchema>;

interface CreateFrequencyAllocationFormProps {
  onClose: () => void;
}

// ─── Link config card ─────────────────────────────────────────────────────────
function LinkConfigCard({
  label,
  values,
  onConfigure,
  showError,
}: {
  label: "Uplink" | "Downlink";
  values: LinkFrequencyValues | null;
  onConfigure: () => void;
  showError: boolean;
}) {
  const isSet = values !== null;
  return (
    <div
      className={`rounded-lg border p-4 transition-colors ${
        isSet ? "border-green-200 bg-green-50/60 dark:bg-green-950/20" : showError ? "border-destructive" : "border-border"
      }`}>
      <div className="flex items-center justify-between mb-2">
        <div className="flex items-center gap-2">
          {isSet ? (
            <CheckCircle2 className="h-4 w-4 text-green-600" />
          ) : (
            <div className="h-4 w-4 rounded-full border-2 border-muted-foreground/40" />
          )}
          <span className="text-sm font-semibold">{label}</span>
        </div>
        <Button
          type="button"
          variant={isSet ? "ghost" : "outline"}
          size="sm"
          onClick={onConfigure}
          className="gap-1.5 text-xs">
          {isSet ? (
            <>
              <Pencil className="h-3 w-3" /> Edit
            </>
          ) : (
            <>
              <Plus className="h-3 w-3" /> Configure
            </>
          )}
        </Button>
      </div>
      {isSet ? (
        <FreqBadges values={values} />
      ) : (
        <p className="text-xs text-muted-foreground">Not configured yet</p>
      )}
      {showError && !isSet && (
        <p className="text-xs text-destructive mt-1">{label} is required</p>
      )}
    </div>
  );
}

// ─── Main form ────────────────────────────────────────────────────────────────
export default function CreateFrequencyAllocationForm({ onClose }: CreateFrequencyAllocationFormProps) {
  const { mutateAsync: createLink, isPending: isCreatingLink } = useCreateLink();
  const { mutateAsync: deleteLink } = useDeleteLink();
  const { mutateAsync: createAllocation, isPending: isCreatingAllocation } = useCreateFrequencyAllocation();

  const { data: serviceProvidersData } = useGetServiceProviders({
    pathParams: {},
    queryParams: { pageSize: 100, pageNumber: 1, serviceTypes: ["wireless"] },
  });
  const serviceProviders = serviceProvidersData?.data ?? [];

  const { data: bandsData } = useGetBands({ pathParams: {}, queryParams: { pageSize: 100, pageNumber: 1 } });
  const bands = bandsData?.data ?? [];

  // Configured link state (stored locally — API only called on final submit)
  const [uplink, setUplink] = useState<LinkFrequencyValues | null>(null);
  const [downlink, setDownlink] = useState<LinkFrequencyValues | null>(null);

  // Which sub-dialog is open
  const [configuringLink, setConfiguringLink] = useState<"Uplink" | "Downlink" | null>(null);

  // Submission-attempt flag for showing link errors
  const [submitted, setSubmitted] = useState(false);

  const isPending = isCreatingLink || isCreatingAllocation;

  const form = useForm<FormValues>({
    resolver: zodResolver(formSchema),
    defaultValues: { name: "", notes: "", serviceProviderId: "", bandId: "" },
  });

  const onSubmit = async (values: FormValues) => {
    setSubmitted(true);
    if (!uplink || !downlink) return;

    const selectedSP = serviceProviders.find((sp) => sp.id === values.serviceProviderId);
    if (!selectedSP) { toast.error("Service provider not found."); return; }

    try {
      const [uplinkRes, downlinkRes] = await Promise.all([
        createLink({
          pathParams: {},
          payload: {
            type: "Uplink",
            bandId: values.bandId,
            serviceProviderId: values.serviceProviderId,
            ...uplink,
          },
        }),
        createLink({
          pathParams: {},
          payload: {
            type: "Downlink",
            bandId: values.bandId,
            serviceProviderId: values.serviceProviderId,
            ...downlink,
          },
        }),
      ]);

      try {
        await createAllocation({
          pathParams: {},
          payload: {
            name: values.name,
            notes: values.notes ?? "",
            providerId: selectedSP.provider.id,
            uplinkId: uplinkRes.data.id,
            downlinkId: downlinkRes.data.id,
          },
        });
      } catch (allocErr) {
        // Compensating rollback — delete both links to avoid orphaned records
        console.error("Frequency allocation failed, rolling back links:", allocErr);
        await Promise.allSettled([
          deleteLink({ pathParams: { id: uplinkRes.data.id } }),
          deleteLink({ pathParams: { id: downlinkRes.data.id } }),
        ]);
        toast.error("Failed to create frequency allocation. The paired links have been rolled back.");
        return;
      }

      toast.success("Frequency allocation created successfully.");
      onClose();
    } catch (err) {
      console.error(err);
      toast.error("Failed to create the paired links.");
    }
  };

  return (
    <>
      <Form {...form}>
        <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-5">
          {/* Row 1: Name + Service Provider */}
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
            <FormField
              control={form.control}
              name="name"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>
                    Name <span className="text-destructive">*</span>
                  </FormLabel>
                  <FormControl>
                    <Input placeholder="e.g. 700 MHz Band 28" {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
            <FormField
              control={form.control}
              name="serviceProviderId"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>
                    Service Provider <span className="text-destructive">*</span>
                  </FormLabel>
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
          </div>

          {/* Notes */}
          <FormField
            control={form.control}
            name="notes"
            render={({ field }) => (
              <FormItem>
                <FormLabel>Notes</FormLabel>
                <FormControl>
                  <Textarea placeholder="Optional notes..." rows={2} {...field} />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />

          {/* Band */}
          <FormField
            control={form.control}
            name="bandId"
            render={({ field }) => (
              <FormItem>
                <FormLabel>
                  Band <span className="text-destructive">*</span>
                </FormLabel>
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

          {/* Uplink / Downlink cards */}
          <div className="space-y-3">
            <p className="text-xs font-semibold text-muted-foreground uppercase tracking-wider">
              Link Configuration <span className="text-destructive">*</span>
            </p>
            <LinkConfigCard
              label="Uplink"
              values={uplink}
              onConfigure={() => setConfiguringLink("Uplink")}
              showError={submitted}
            />
            <LinkConfigCard
              label="Downlink"
              values={downlink}
              onConfigure={() => setConfiguringLink("Downlink")}
              showError={submitted}
            />
          </div>

          {/* Footer */}
          <div className="flex justify-between gap-3 pt-2">
            <Button variant="outline" type="button" onClick={onClose} disabled={isPending}>
              Cancel
            </Button>
            <SubmitButton loading={isPending}>
              {isPending ? "Creating..." : "Create Frequency Allocation"}
            </SubmitButton>
          </div>
        </form>
      </Form>

      {/* Sub-dialog for link configuration */}
      {configuringLink && (
        <LinkFrequencyConfigDialog
          open={true}
          onOpenChange={(open) => { if (!open) setConfiguringLink(null); }}
          linkType={configuringLink}
          initial={configuringLink === "Uplink" ? uplink ?? undefined : downlink ?? undefined}
          onConfirm={(vals) => {
            if (configuringLink === "Uplink") setUplink(vals);
            else setDownlink(vals);
            setConfiguringLink(null);
          }}
        />
      )}
    </>
  );
}
