// ReportTemplate types based on API specs (swagger.json)

// Full ReportTemplate DTO with all fields
export type ReportTemplate = {
  id: string;
  title: string | null;
  description: string | null;
  instruction: string | null;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
};

// List item DTO (used in getAll endpoint)
export type ReportTemplateListItem = {
  id: string;
  title: string | null;
  description: string | null;
};

// Create payload
export type CreateReportTemplatePayload = {
  title: string;
  description?: string | null;
  instruction: string;
};

// Update payload
export type UpdateReportTemplatePayload = {
  title?: string | null;
  description?: string | null;
  instruction?: string | null;
  isActive?: boolean | null;
};
