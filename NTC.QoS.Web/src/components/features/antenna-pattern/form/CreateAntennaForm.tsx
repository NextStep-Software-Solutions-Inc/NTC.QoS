"use client";

import { useState } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";

import SubmitButton from "@/components/SubmitButton";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { createMutationHookFromTemplate } from "@/hooks/createMutationHookFromTemplate";
import { createAntennaPatternRequest } from "../handlers/createAntennaPatternRequest";
import { type AntennaPattern } from "../types/AntennaPattern";

interface CreateAntennaFormProps {
  onCreated?: (antenna: AntennaPattern) => void;
  onClose?: () => void;
}

const useCreateAntenna = createMutationHookFromTemplate(createAntennaPatternRequest);

export default function CreateAntennaForm({ onCreated, onClose }: CreateAntennaFormProps) {
  const [name, setName] = useState("");
  const [url, setUrl] = useState("");

  const { mutateAsync, isPending } = useCreateAntenna();
  const queryClient = useQueryClient();

  const handleSubmit = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();

    toast.promise(mutateAsync({ pathParams: {}, payload: { name, url } }), {
      loading: "Creating antenna pattern...",
      success: (response) => {
        void queryClient.invalidateQueries({ predicate: (query) => query.queryKey[0] === "get-antennas" });
        onCreated?.(response.data);
        onClose?.();
        return response.message;
      },
      error: (error: unknown) => {
        console.error(error);
        return "Antenna pattern already exists or request failed.";
      },
    });
  };

  return (
    <form onSubmit={handleSubmit} className="flex flex-col gap-4 max-w-xl">
      <div>
        <Label htmlFor="name" className="text-sm font-medium">
          Antenna Pattern Name
        </Label>
        <Input
          id="name"
          className="mt-1 w-full rounded border border-gray-300 p-2 text-sm font-medium"
          value={name}
          onChange={(e) => setName(e.target.value)}
        />
      </div>

      <div>
        <Label htmlFor="url" className="text-sm font-medium">
          Antenna Pattern URL
        </Label>
        <Input
          id="url"
          type="url"
          className="mt-1 w-full rounded border border-gray-300 p-2 text-sm font-medium"
          value={url}
          onChange={(e) => setUrl(e.target.value)}
        />
      </div>

      <div className="flex justify-between gap-2 mt-4">
        <Button variant="outline" type="button" onClick={onClose}>
          Cancel
        </Button>
        <SubmitButton loading={isPending} className="bg-blue-900 text-white hover:bg-blue-800" disabled={isPending}>
          {isPending ? "Creating..." : "Create"}
        </SubmitButton>
      </div>
    </form>
  );
}

// FIgma design
// "use client";

// import { useState } from "react";
// import { useQueryClient } from "@tanstack/react-query";
// import { toast } from "sonner";

// import SubmitButton from "@/components/SubmitButton";
// import { Button } from "@/components/ui/button";
// import { Input } from "@/components/ui/input";
// import { Label } from "@/components/ui/label";
// import { createMutationHookFromTemplate } from "@/hooks/createMutationHookFromTemplate";
// // import { createAntennaRequest } from "../handlers/createAntennaRequest";
// // import { type AntennaPattern } from "./features/antenna-pattern/types/AntennaPattern";
// import { createAntennaPatternRequest } from "../handlers/createAntennaPatternRequest";
// import { type AntennaPattern } from "../types/AntennaPattern";

// interface CreateAntennaFormProps {
//   onCreated?: (antenna: AntennaPattern) => void;
//   onClose?: () => void;
// }

// const useCreateAntenna = createMutationHookFromTemplate(createAntennaPatternRequest);

// export default function CreateAntennaForm({ onCreated, onClose }: CreateAntennaFormProps) {
//   const [antennaType, setAntennaType] = useState("");
//   const [azimuth, setAzimuth] = useState("");
//   const [beamTilt, setBeamTilt] = useState("");
//   const [height, setHeight] = useState("");

//   const { mutateAsync, isPending } = useCreateAntenna();
//   const queryClient = useQueryClient();

//   const handleSubmit = async (e: React.FormEvent<HTMLFormElement>) => {
//     e.preventDefault();

//     toast.promise(
//       mutateAsync({
//         pathParams: {},
//         payload: { antennaType, azimuth: Number(azimuth), beamTilt: Number(beamTilt), height: Number(height) },
//       }),
//       {
//         loading: "Creating antenna...",
//         success: (response) => {
//           void queryClient.invalidateQueries({ predicate: (query) => query.queryKey[0] === "get-antennas" });
//           onCreated?.(response.data);
//           onClose?.();
//           return response.message;
//         },
//         error: (error: unknown) => {
//           console.error(error);
//           return "Antenna already exists or request failed.";
//         },
//       },
//     );
//   };

//   return (
//     <form onSubmit={handleSubmit} className="flex flex-col gap-4 max-w-xl">
//       <div>
//         <Label htmlFor="antennaType" className="text-sm font-medium">
//           Antenna Type
//         </Label>
//         <Input
//           id="antennaType"
//           className="mt-1 w-full rounded border border-gray-300 p-2 text-sm font-medium"
//           value={antennaType}
//           onChange={(e) => setAntennaType(e.target.value)}
//         />
//       </div>

//       <div>
//         <Label htmlFor="azimuth" className="text-sm font-medium">
//           Azimuth (°)
//         </Label>
//         <Input
//           id="azimuth"
//           type="number"
//           className="mt-1 w-full rounded border border-gray-300 p-2 text-sm font-medium"
//           value={azimuth}
//           onChange={(e) => setAzimuth(e.target.value)}
//         />
//       </div>

//       <div>
//         <Label htmlFor="beamTilt" className="text-sm font-medium">
//           Beam Tilt (°)
//         </Label>
//         <Input
//           id="beamTilt"
//           type="number"
//           className="mt-1 w-full rounded border border-gray-300 p-2 text-sm font-medium"
//           value={beamTilt}
//           onChange={(e) => setBeamTilt(e.target.value)}
//         />
//       </div>

//       <div>
//         <Label htmlFor="height" className="text-sm font-medium">
//           Height (m)
//         </Label>
//         <Input
//           id="height"
//           type="number"
//           className="mt-1 w-full rounded border border-gray-300 p-2 text-sm font-medium"
//           value={height}
//           onChange={(e) => setHeight(e.target.value)}
//         />
//       </div>

//       <div className="flex justify-between gap-2 mt-4">
//         <Button variant="outline" type="button" onClick={onClose}>
//           Cancel
//         </Button>
//         <SubmitButton loading={isPending} className="bg-blue-900 text-white hover:bg-blue-800" disabled={isPending}>
//           {isPending ? "Creating..." : "Create"}
//         </SubmitButton>
//       </div>
//     </form>
//   );
// }
