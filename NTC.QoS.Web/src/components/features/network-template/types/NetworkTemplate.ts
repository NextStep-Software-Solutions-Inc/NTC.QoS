export type NetworkTemplate = {
  id: string;
  name: string;
  description?: string;
  templateType: "standard" | "custom" | "premium";
  networkType: "2G" | "3G" | "4G" | "5G";
  configuration: Record<string, unknown>;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
};

export type NetworkTemplateDetails = NetworkTemplate;
