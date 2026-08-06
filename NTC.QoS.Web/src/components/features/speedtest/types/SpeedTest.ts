import { type Barangay } from "@/components/features/barangay/types/Barangay";
import { type Cell } from "@/components/features/cell/types/Cell";
import { type Generation } from "@/components/features/generation/types/Generation";
import { type ServiceProviderDetails } from "../../service-provider/types/serviceProvider";
import { type Technology } from "../../technology/types/Technology";

export type SpeedTest = {
  id: string;
  dateTime: string;
  ping: number;
  jitter: number;
  rssi: number;
  uploadSpeed: number;
  downloadSpeed: number;
  latitude: number;
  longitude: number;
  phoneModel: string;
  address?: string;
  mcc: string;
  mnc: string;
  tac: string;
  signalQuality: "Unknown" | "VeryWeak" | "Weak" | "Average" | "Good" | "Excellent";
  isNoBroadbandCoverage: boolean;
  isFixedBroadband?: boolean;
  // API response includes nested objects, not separate fields
  serviceProvider: ServiceProviderDetails;
  barangay: Barangay;
  generation: Generation;
  cell: Cell;
  file?: File;
  technology: Technology;
};

export type SpeedTestDetails = SpeedTest;
