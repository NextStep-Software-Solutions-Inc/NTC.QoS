"use client";

import { useState } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";

import SubmitButton from "@/components/SubmitButton";
import { Button } from "@/components/ui/button";
import { DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { createMutationHookFromTemplate } from "@/hooks/createMutationHookFromTemplate";
import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";
import { getBandsRequest } from "../../band/handlers/getBandsRequest";
import { type Band } from "../../band/types/Band";
import { getServiceProvidersRequest } from "../../service-provider/handlers/getServiceProvidersRequest";
import { createLinkRequest } from "../handlers/createLinkRequest";
import { type Link } from "../types/Link";

interface CenterFrequencyBandwidthFormProps {
  onCreated: (link: Link) => void;
  onClose: () => void;
}

const useServiceProviders = createQueryHookFromTemplate(getServiceProvidersRequest);
const useGetBands = createQueryHookFromTemplate(getBandsRequest);
const useCreateLink = createMutationHookFromTemplate(createLinkRequest);

export default function CenterFrequencyBandwidthForm({ onCreated, onClose }: CenterFrequencyBandwidthFormProps) {
  const pageSize = 100;

  const { data: serviceProviders, isLoading: isLoadingServiceProviders } = useServiceProviders({
    pathParams: {},
    queryParams: { pageSize: 1000, pageNumber: 1, serviceTypes: ["Wireless"] },
  });

  const {
    data: bandsData,
    isLoading: bandsLoading,
    isError: bandsError,
  } = useGetBands({ pathParams: {}, queryParams: { pageSize } });

  const [formData, setFormData] = useState({
    type: "",
    serviceProviderId: "",
    bandId: "",
    centerFrequency: "",
    bandwidth: "",
    rxThreshold: "",
  });
  const queryClient = useQueryClient();

  const { mutateAsync: createLink, isPending } = useCreateLink();

  const handleChange = (field: string, value: string) => {
    setFormData((prev) => ({ ...prev, [field]: value }));
  };
  const handleSubmit = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();

    // Calculate lower and upper frequency from center and bandwidth
    const center = Number(formData.centerFrequency);
    const bw = Number(formData.bandwidth);
    const lowerFrequency = center - bw / 2;
    const upperFrequency = center + bw / 2;

    toast.promise(
      createLink({
        pathParams: {},
        payload: {
          type: formData.type,
          serviceProviderId: formData.serviceProviderId,
          bandId: formData.bandId,
          centerFrequency: center,
          rxThreshold: Number(formData.rxThreshold),
          lowerFrequency, // computed value
          upperFrequency, // computed value
        },
      }),
      {
        loading: "Creating Link...",
        success: (response) => {
          void queryClient.invalidateQueries({ predicate: (query) => query.queryKey[0] === "get-cells" });
          const res = response as { data: Link; message?: string };
          onCreated?.(res.data);
          onClose?.();
          return res.message ?? "Cell created successfully";
        },
        error: (error: unknown) => {
          console.error(error);
          return "Failed to create link";
        },
      },
    );
  };

  return (
    <>
      <DialogHeader>
        <DialogTitle>Center Frequency + Bandwidth</DialogTitle>
        <p className="text-sm text-gray-500">Enter center frequency & bandwidth to compute allocationss</p>
      </DialogHeader>
      <form onSubmit={handleSubmit} className="space-y-4 py-4">
        <div className="space-y-2">
          <Label htmlFor="type">Type</Label>
          <Select value={formData.type} onValueChange={(value) => handleChange("type", value)}>
            <SelectTrigger>
              <SelectValue placeholder="Select Type" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="Uplink">Uplink</SelectItem>
              <SelectItem value="Downlink">Downlink</SelectItem>
            </SelectContent>
          </Select>
        </div>
        <div className="space-y-2">
          <Label htmlFor="serviceProvider">Select Service Provider</Label>
          <Select
            value={formData.serviceProviderId}
            onValueChange={(value) => handleChange("serviceProviderId", value)}>
            <SelectTrigger>
              <SelectValue placeholder={isLoadingServiceProviders ? "Loading..." : "Select Service Provider"} />
            </SelectTrigger>
            <SelectContent>
              {serviceProviders?.data?.length
                ? serviceProviders.data.map((sp) => (
                    <SelectItem key={sp.id} value={sp.id}>
                      {sp.provider.name}
                    </SelectItem>
                  ))
                : !isLoadingServiceProviders && (
                    <SelectItem value="" disabled>
                      No wireless providers found
                    </SelectItem>
                  )}
            </SelectContent>
          </Select>
        </div>

        <div className="space-y-2">
          <Label htmlFor="band">Select Band</Label>
          <Select value={formData.bandId} onValueChange={(value) => handleChange("bandId", value)}>
            <SelectTrigger>
              <SelectValue placeholder={bandsLoading ? "Loading..." : "Select Band"} />
            </SelectTrigger>
            <SelectContent>
              {bandsError && (
                <SelectItem value="" disabled>
                  Failed to load bands
                </SelectItem>
              )}
              {bandsData?.data?.length
                ? bandsData.data.map((band: Band) => (
                    <SelectItem key={band.id} value={band.id}>
                      {band.name}
                    </SelectItem>
                  ))
                : !bandsLoading && (
                    <SelectItem value="" disabled>
                      No bands found
                    </SelectItem>
                  )}
            </SelectContent>
          </Select>
        </div>

        <div className="grid grid-cols-2 gap-4">
          <div className="space-y-2">
            <Label htmlFor="centerFrequency">Center Frequency (MHz)</Label>
            <Input
              id="centerFrequency"
              type="number"
              placeholder="0"
              value={formData.centerFrequency}
              onChange={(e) => handleChange("centerFrequency", e.target.value)}
            />
          </div>

          <div className="space-y-2">
            <Label htmlFor="bandwidth">Bandwidth (MHz)</Label>
            <Input
              id="bandwidth"
              type="number"
              placeholder="0"
              value={formData.bandwidth}
              onChange={(e) => handleChange("bandwidth", e.target.value)}
            />
          </div>
          <div className="space-y-2">
            <Label htmlFor="rxThreshold">Rx Threshold (dBm)</Label>
            <Input
              id="rxThreshold"
              type="number"
              placeholder="0"
              value={formData.rxThreshold}
              onChange={(e) => handleChange("rxThreshold", e.target.value)}
            />
          </div>
        </div>
        <div className="flex gap-3 pt-4">
          <Button type="button" variant="outline" onClick={onClose} className="flex-1 bg-transparent">
            Cancel
          </Button>
          <SubmitButton loading={isPending}>{isPending ? "Creating..." : "Create"}</SubmitButton>
        </div>
      </form>
    </>
  );
}
