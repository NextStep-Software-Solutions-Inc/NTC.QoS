export type Network = {
  id: string;
  name: string;
  description?: string;
  networkType: "2G" | "3G" | "4G" | "5G";
  providerId: string;
  frequency: number;
  bandwidth: number;
  coverage: string;
  createdAt: string;
  updatedAt: string;
};

export type NetworkDetails = Network;
