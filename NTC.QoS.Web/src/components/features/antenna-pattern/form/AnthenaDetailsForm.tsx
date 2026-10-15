"use client";

import React from "react";
import { zodResolver } from "@hookform/resolvers/zod";
import { useQueryClient } from "@tanstack/react-query";
import { useForm } from "react-hook-form";
import { toast } from "sonner";
import { z } from "zod";

import { Button } from "@/components/ui/button";
import { Form, FormControl, FormField, FormItem, FormLabel, FormMessage } from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { createMutationHookFromTemplate } from "@/hooks/createMutationHookFromTemplate";
import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";
import { getBandsRequest } from "../../band/handlers/getBandsRequest";
import { createCellSectorRequest } from "../../cell-sector/handlers/createCellSectorRequest";
import { createCellularRequest } from "../../cellular/handlers/getCellularsRequest";
import { getTechnologiesRequest } from "../../technology/handlers/getTechnologiesRequest";

interface AntennaDetailsFormProps {
  tabsContent?: string;
  setTabsContent?: (content: string) => void;
  isCloseDialog?: (value: boolean) => void;
  cellId?: string;
  serviceProviderId?: string;
  defaultAddress?: string;
}

const sectorAndCellularFormSchema = z.object({
  antennaHeight: z.coerce.number().positive("Antenna height is required"),
  azimuth: z.coerce.number().min(0, "Azimuth must be at least 0").max(360, "Azimuth must not exceed 360"),
  beamTilt: z.coerce.number(),
  antennaType: z.string().min(1, "Antenna type is required"),
  technologyId: z.string().min(1, "Technology is required"),
  bandId: z.string().min(1, "Band is required"),
  address: z.string().optional(),
});

const useCreateCellSector = createMutationHookFromTemplate(createCellSectorRequest);
const useCreateCellular = createMutationHookFromTemplate(createCellularRequest);
const useGetTechnologies = createQueryHookFromTemplate(getTechnologiesRequest);
const useGetBands = createQueryHookFromTemplate(getBandsRequest);

const AntennaDetailsForm = ({
  tabsContent,
  setTabsContent,
  cellId,
  serviceProviderId,
  defaultAddress,
  isCloseDialog,
}: AntennaDetailsFormProps) => {
  const queryClient = useQueryClient();
  const { mutateAsync: createCellSector, isPending: creatingCellSector } = useCreateCellSector();
  const { mutateAsync: createCellular, isPending: creatingCellular } = useCreateCellular();
  const isSubmitting = creatingCellSector || creatingCellular;

  const { data: technologiesResponse, isLoading: isTechnologiesLoading, isError: isTechnologiesError } = useGetTechnologies({
    pathParams: {},
    queryParams: { pageSize: 100, pageNumber: 1 },
  });

  const { data: bandsResponse, isLoading: isBandsLoading, isError: isBandsError } = useGetBands({
    pathParams: {},
    queryParams: { pageSize: 200, pageNumber: 1 },
  });

  const technologies = technologiesResponse?.data ?? [];
  const bands = bandsResponse?.data ?? [];

  const form = useForm<z.infer<typeof sectorAndCellularFormSchema>>({
    resolver: zodResolver(sectorAndCellularFormSchema),
    defaultValues: {
      antennaHeight: 0,
      azimuth: 0,
      beamTilt: 0,
      antennaType: "panel",
      technologyId: "",
      bandId: "",
      address: defaultAddress ?? "",
    },
    mode: "onChange",
  });

  const onSubmit = async (values: z.infer<typeof sectorAndCellularFormSchema>) => {
    if (!cellId || !serviceProviderId) {
      toast.error("Cell information is missing. Please re-create the cell site first.");
      return;
    }

    try {
      const sectorResponse = await createCellSector({
        pathParams: {},
        payload: {
          cellId,
          antennaType: values.antennaType,
          antennaHeight: values.antennaHeight,
          azimuth: values.azimuth,
          beamTilt: values.beamTilt,
        },
      });

      const createdCellSectorId = sectorResponse.data.id;

      await createCellular({
        pathParams: {},
        payload: {
          cellSectorId: createdCellSectorId,
          serviceProviderId,
          technologyId: values.technologyId,
          bandId: values.bandId,
          address: values.address,
        },
      });

      void queryClient
        .invalidateQueries({ predicate: (query) => query.queryKey[0] === "get-cell-sectors", refetchType: "active" })
        .catch((error) => {
          console.error("Failed to invalidate get-cell-sectors query:", error);
        });
      void queryClient
        .invalidateQueries({ predicate: (query) => query.queryKey[0] === "get-cellulars", refetchType: "active" })
        .catch((error) => {
          console.error("Failed to invalidate get-cellulars query:", error);
        });

      toast.success("Cell sector and cellular details added successfully!");
      isCloseDialog?.(false);
    } catch (error) {
      console.error("Error creating cell sector/cellular:", error);
      toast.error("Failed to add cell sector and cellular details.");
    }
  };

  return (
    <div className="space-y-4">
      <section className="w-full border rounded-lg p-4">
        <h3 className="text-sm font-semibold mb-4">Cell Sector Details</h3>
        <Form {...form}>
          <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <FormField
                control={form.control}
                name="antennaHeight"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>Antenna Height</FormLabel>
                    <FormControl>
                      <Input type="number" placeholder="Enter height in meters" {...field} />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
              <FormField
                control={form.control}
                name="azimuth"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>Azimuth</FormLabel>
                    <FormControl>
                      <Input type="number" placeholder="Enter azimuth (0-360)" {...field} />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
              <FormField
                control={form.control}
                name="beamTilt"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>Beam Tilt</FormLabel>
                    <FormControl>
                      <Input type="number" placeholder="Enter beam tilt" {...field} />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
              <FormField
                control={form.control}
                name="antennaType"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>Antenna Type</FormLabel>
                    <FormControl>
                      <Select onValueChange={field.onChange} value={field.value}>
                        <SelectTrigger>
                          <SelectValue placeholder="Select antenna type" />
                        </SelectTrigger>
                        <SelectContent>
                          <SelectItem value="omni">Omni-Directional Antenna</SelectItem>
                          <SelectItem value="panel">Panel Antenna</SelectItem>
                          <SelectItem value="yagi">Yagi Antenna</SelectItem>
                          <SelectItem value="parabolic">Parabolic Dish Antenna</SelectItem>
                          <SelectItem value="panel-mimo">Panel Antenna with MIMO Support</SelectItem>
                          <SelectItem value="small-cell">Small Cell / DAS Antennas</SelectItem>
                          <SelectItem value="smart">Smart Antennas (Massive MIMO)</SelectItem>
                        </SelectContent>
                      </Select>
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
            </div>

            <h3 className="text-sm font-semibold pt-2">Cellular Details (Technology + Band)</h3>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <FormField
                control={form.control}
                name="technologyId"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>Technology</FormLabel>
                    <FormControl>
                      <Select onValueChange={field.onChange} value={field.value}>
                        <SelectTrigger>
                          <SelectValue placeholder="Select technology" />
                        </SelectTrigger>
                        <SelectContent>
                          {isTechnologiesLoading && (
                            <SelectItem value="loading-technologies" disabled>
                              Loading technologies...
                            </SelectItem>
                          )}
                          {isTechnologiesError && (
                            <SelectItem value="failed-technologies" disabled>
                              Failed to load technologies
                            </SelectItem>
                          )}
                          {technologies.map((technology) => (
                            <SelectItem key={technology.id} value={technology.id}>
                              {technology.standard}
                            </SelectItem>
                          ))}
                        </SelectContent>
                      </Select>
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
              <FormField
                control={form.control}
                name="bandId"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>Band</FormLabel>
                    <FormControl>
                      <Select onValueChange={field.onChange} value={field.value}>
                        <SelectTrigger>
                          <SelectValue placeholder="Select band" />
                        </SelectTrigger>
                        <SelectContent>
                          {isBandsLoading && (
                            <SelectItem value="loading-bands" disabled>
                              Loading bands...
                            </SelectItem>
                          )}
                          {isBandsError && (
                            <SelectItem value="failed-bands" disabled>
                              Failed to load bands
                            </SelectItem>
                          )}
                          {bands.map((band) => (
                            <SelectItem key={band.id} value={band.id}>
                              {band.name}
                            </SelectItem>
                          ))}
                        </SelectContent>
                      </Select>
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
              <FormField
                control={form.control}
                name="address"
                render={({ field }) => (
                  <FormItem className="md:col-span-2">
                    <FormLabel>Address</FormLabel>
                    <FormControl>
                      <Input placeholder="Optional address for this cellular entity" {...field} />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
            </div>

            <div className="flex flex-col md:flex-row justify-between items-center gap-4 pt-4">
              <Button
                variant="outline"
                type="button"
                className="w-full md:w-auto"
                onClick={() => setTabsContent?.("basic-information")}>
                Prev
              </Button>
              <div className="flex gap-2 w-full md:w-auto justify-end">
                {tabsContent === "cell-sector" && (
                  <Button
                    variant="ghost"
                    type="button"
                    className="text-gray-600 hover:text-red-500"
                    onClick={() => setTabsContent?.("basic-information")}>
                    Cancel
                  </Button>
                )}
                <Button variant="outline" onClick={() => isCloseDialog?.(false)} type="button" className="w-full md:w-auto">
                  Close
                </Button>
                <Button type="submit" disabled={isSubmitting} className="bg-blue-500 text-white hover:bg-blue-600">
                  {isSubmitting ? "Saving..." : "Save Cell Sector & Cellular"}
                </Button>
              </div>
            </div>
          </form>
        </Form>
      </section>
    </div>
  );
};

export default AntennaDetailsForm;
