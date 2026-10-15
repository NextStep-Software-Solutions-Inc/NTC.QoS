"use client";

import React, { useState } from "react";
import { zodResolver } from "@hookform/resolvers/zod";
import { useQueryClient } from "@tanstack/react-query";
import { format } from "date-fns";
import { Loader2, LocateFixed } from "lucide-react";
import { useForm } from "react-hook-form";
import { toast } from "sonner";
import { z } from "zod";

import SubmitButton from "@/components/SubmitButton";
import { Button } from "@/components/ui/button";
import { Calendar } from "@/components/ui/calendar";
import { Checkbox } from "@/components/ui/checkbox";
import { Form, FormControl, FormField, FormItem, FormLabel, FormMessage } from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover";
import {
  Select,
  SelectContent,
  SelectGroup,
  SelectItem,
  SelectLabel,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { createMutationHookFromTemplate } from "@/hooks/createMutationHookFromTemplate";
import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";
import { getGenerationsRequest } from "../../generation/handlers/getGenerationsRequest";
import { getServiceProvidersRequest } from "../../service-provider/handlers/getServiceProviders";
import { type ServiceProvider } from "../../service-provider/types/serviceProvider";
import { getTechnologiesRequest } from "../../technology/handlers/getTechnologiesRequest";
import { createSpeedtestRequest } from "../handlers/createSpeedtestRequest";

// Schema
const addSpeedtestFormSchema = z.object({
  serviceProviderId: z.string().min(1, "Service provider is required"),
  generationId: z.string().min(1, "Generation is required"),
  technologyId: z.string().min(1, "Technology is required"),
  download: z.string().min(1, "Download speed is required"),
  upload: z.string().min(1, "Upload speed is required"),
  signalQuality: z.string().min(1, "Signal Quality is required"),
  ping: z.string().min(1, "Ping is required"),
  rssi: z.string(),
  latitude: z.string(),
  longitude: z.string(),
  dateTime: z.string().min(1, "Date and time are required"),
  isFixedBroadband: z.boolean(),
});

const useCreateSpeedtest = createMutationHookFromTemplate(createSpeedtestRequest);
const useServiceProviders = createQueryHookFromTemplate(getServiceProvidersRequest);
const useTechnologies = createQueryHookFromTemplate(getTechnologiesRequest);
const useGenerations = createQueryHookFromTemplate(getGenerationsRequest);

interface AddSpeedtestFormProps {
  onCloseDialog?: (value: boolean) => void;
}

const AddSpeedtestForm = ({ onCloseDialog }: AddSpeedtestFormProps) => {
  const { mutateAsync, isPending } = useCreateSpeedtest();
  const queryClient = useQueryClient();
  const [isLocating, setIsLocating] = useState(false);

  const { data: serviceProviders } = useServiceProviders({
    pathParams: {},
    queryParams: { pageSize: 100, pageNumber: 1 },
  });

  const { data: technologies } = useTechnologies({ pathParams: {}, queryParams: { pageSize: 100, pageNumber: 1 } });
  const { data: generations } = useGenerations({ pathParams: {}, queryParams: { pageSize: 100, pageNumber: 1 } });

  const form = useForm<z.infer<typeof addSpeedtestFormSchema>>({
    resolver: zodResolver(addSpeedtestFormSchema),
    defaultValues: {
      serviceProviderId: "",
      generationId: "",
      technologyId: "",
      download: "",
      upload: "",
      signalQuality: "",
      ping: "",
      rssi: "",
      latitude: "",
      longitude: "",
      dateTime: new Date().toISOString(),
      isFixedBroadband: false,
    },
    mode: "onChange",
  });

  // ✅ Single locator for both latitude & longitude
  const locateCoordinates = () => {
    if (!navigator.geolocation) {
      toast.error("Geolocation is not supported by your browser.");
      return;
    }
    setIsLocating(true);
    navigator.geolocation.getCurrentPosition(
      (position) => {
        form.setValue("latitude", position.coords.latitude.toString());
        form.setValue("longitude", position.coords.longitude.toString());
        toast.success("Coordinates detected!");
        setIsLocating(false);
      },
      (error) => {
        toast.error(`Unable to retrieve location: ${error.message}`);
        setIsLocating(false);
      },
    );
  };

  const onSubmit = async (values: z.infer<typeof addSpeedtestFormSchema>) => {
    toast.promise(
      mutateAsync({
        pathParams: {},
        payload: {
          serviceProviderId: values.serviceProviderId,
          generationId: values.generationId,
          technologyId: values.technologyId,
          dateTime: values.dateTime,
          ping: Number(values.ping),
          jitter: 0,
          rssi: Number(values.rssi) || 0,
          uploadSpeed: Number(values.upload),
          downloadSpeed: Number(values.download),
          latitude: Number(values.latitude),
          longitude: Number(values.longitude),
          phoneModel: "Apple iPhone Pro Max",
          mcc: "515",
          mnc: "01",
          tac: "1234",
          signalQuality: values.signalQuality as "Unknown" | "VeryWeak" | "Weak" | "Average" | "Good" | "Excellent",
          cellIdentifier: "123456789-dig-test",
          isFixedBroadband: values.isFixedBroadband,
        },
      }),
      {
        success: () => {
          void queryClient.invalidateQueries({ predicate: (query) => query.queryKey[0] === "get-speed-tests" });
          onCloseDialog?.(false);
          return "Speed test added successfully!";
        },
        error: "Error adding speed test.",
      },
    );
  };

  return (
    <Form {...form}>
      <form onSubmit={form.handleSubmit(onSubmit)} className="w-full">
        {/* Service Provider */}
        <FormField
          control={form.control}
          name="serviceProviderId"
          render={({ field }) => (
            <FormItem>
              <Label htmlFor="provider" className="mb-1 text-[15px] font-semibold">
                Service Provider
              </Label>
              <Select value={field.value} onValueChange={field.onChange}>
                <SelectTrigger>
                  <SelectValue placeholder="Select a provider" />
                </SelectTrigger>
                <SelectContent className="w-[300px]">
                  <SelectGroup>
                    <SelectLabel>Providers</SelectLabel>
                    {serviceProviders?.data.map((sp: ServiceProvider) => (
                      <SelectItem key={sp.id} value={sp.id}>
                        {sp.provider.name}
                      </SelectItem>
                    ))}
                  </SelectGroup>
                </SelectContent>
              </Select>
            </FormItem>
          )}
        />

        {/* Download & Upload */}
        <div className="grid grid-cols-2 gap-x-3 my-2">
          {["download", "upload"].map((name) => (
            <FormField
              key={name}
              control={form.control}
              name={name as "download" | "upload"}
              render={({ field }) => (
                <FormItem>
                  <FormLabel className="capitalize">{name}</FormLabel>
                  <FormControl>
                    <div className="flex items-center">
                      <Input type="text" placeholder={`Enter ${name} speed`} {...field} />
                      <div className="px-2 text-[13px]">Mbps</div>
                    </div>
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
          ))}
        </div>

        {/* Ping & Signal Strength */}
        <div className="grid grid-cols-2 gap-x-3 mt-3">
          <FormField
            control={form.control}
            name="ping"
            render={({ field }) => (
              <FormItem>
                <FormLabel>Ping</FormLabel>
                <FormControl>
                  <Input type="text" placeholder="Enter ping" {...field} />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />
          <FormField
            control={form.control}
            name="signalQuality"
            render={({ field }) => (
              <FormItem>
                <FormLabel>Signal Strength</FormLabel>
                <FormControl>
                  <Input
                    type="text"
                    placeholder="Enter signal strength"
                    {...field}
                    onChange={(e) => field.onChange(e.target.value)}
                  />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />
        </div>

        {/* Date & Time */}
        <div className="mt-3">
          <FormField
            control={form.control}
            name="dateTime"
            render={({ field }) => (
              <FormItem className="flex flex-col">
                <FormLabel>Date and Time</FormLabel>
                <Popover>
                  <PopoverTrigger asChild>
                    <Button variant="outline" className="w-full justify-start text-left font-normal">
                      {field.value && !isNaN(new Date(field.value).getTime())
                        ? format(new Date(field.value), "PPP p")
                        : "Pick a date"}
                    </Button>
                  </PopoverTrigger>
                  <PopoverContent className="w-auto p-0">
                    <Calendar
                      mode="single"
                      selected={field.value ? new Date(field.value) : undefined}
                      onSelect={(date) => field.onChange(date?.toISOString() ?? "")}
                      initialFocus
                    />
                  </PopoverContent>
                </Popover>
                <FormMessage />
              </FormItem>
            )}
          />
        </div>

        {/* Coordinates with single button */}
        <div className="mt-4">
          <Label className="text-[15px] font-semibold mb-1 block">Coordinates</Label>
          <div className="flex items-center gap-x-3">
            <FormField
              control={form.control}
              name="latitude"
              render={({ field }) => (
                <FormItem className="flex-1">
                  <FormControl>
                    <Input type="text" placeholder="Enter latitude" {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
            <FormField
              control={form.control}
              name="longitude"
              render={({ field }) => (
                <FormItem className="flex-1">
                  <FormControl>
                    <Input type="text" placeholder="Enter longitude" {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
            <Button type="button" size="icon" variant="outline" onClick={locateCoordinates} disabled={isLocating}>
              {isLocating ? <Loader2 className="w-4 h-4 animate-spin" /> : <LocateFixed className="w-4 h-4" />}
            </Button>
          </div>
        </div>

        {/* Network Technologies */}
        <div className="mt-4">
          <Label className="text-[15px] font-semibold mb-1 block">Network Technologies</Label>
          <div className="flex items-center gap-x-4">
            <FormField
              control={form.control}
              name="technologyId"
              render={({ field }) => (
                <Select value={field.value} onValueChange={field.onChange}>
                  <SelectTrigger>
                    <SelectValue placeholder="Select a technology" />
                  </SelectTrigger>
                  <SelectContent>
                    {technologies?.data.map((tech) => (
                      <SelectItem key={tech.id} value={tech.id}>
                        {tech.standard}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              )}
            />
          </div>
        </div>

        {/* Network Generation */}
        <div className="mt-4">
          <Label className="text-[15px] font-semibold mb-1 block">Network Generation</Label>
          <div className="flex items-center gap-x-4">
            <FormField
              control={form.control}
              name="generationId"
              render={({ field }) => (
                <Select value={field.value} onValueChange={field.onChange}>
                  <SelectTrigger>
                    <SelectValue placeholder="Select a generation" />
                  </SelectTrigger>
                  <SelectContent>
                    {generations?.data.map((gen) => (
                      <SelectItem key={gen.id} value={gen.id}>
                        {gen.name}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              )}
            />
          </div>
        </div>

        {/* isFixedBroadband Checkbox */}
        <div className="mt-4">
          <FormField
            control={form.control}
            name="isFixedBroadband"
            render={({ field }) => (
              <FormItem className="flex flex-row items-start space-x-3 space-y-0 rounded-md border p-4">
                <FormControl>
                  <Checkbox checked={field.value} onCheckedChange={field.onChange} />
                </FormControl>
                <div className="space-y-1 leading-none">
                  <FormLabel>Is this a fixed broadband speed test?</FormLabel>
                </div>
              </FormItem>
            )}
          />
        </div>

        {/* Buttons */}
        <div className="flex justify-end gap-2 mt-4">
          <Button type="button" variant="ghost" onClick={() => onCloseDialog?.(false)}>
            Cancel
          </Button>
          <SubmitButton type="submit" loading={isPending}>
            {isPending ? "Adding Speedtest..." : "Add Speedtest"}
          </SubmitButton>
        </div>
      </form>
    </Form>
  );
};

export default AddSpeedtestForm;
