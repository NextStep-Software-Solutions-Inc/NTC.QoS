export interface ReportTemplate {
  id: string;
  name: string;
  description: string;
  file: string;
  icon: string;
  category: string;
  tags: string[];
  suggestedPrompts?: string[];
}

export interface ReportParameters {
  region?: string;
  dateRange?: {
    start: Date;
    end: Date;
  };
  providers?: string[];
  includeAnalytics?: boolean;
  includeCoverage?: boolean;
  includeSpeedTests?: boolean;
}

// Request type for generating a report
export interface GenerateReportRequest {
  reportTitle: string;
  reportDescription?: string;
  templateId?: string;
  prompt: string;
  files?: File[];
}

// Backend DTO - Full generated report details
export interface GeneratedReportDto {
  id: string;
  title: string;
  promptPreview: string | null;
  prompt: string;
  content: string | null;
  filesAnalyzed: number;
  fileMetadata: string | null;
  generatedAt: string; // ISO date string
  templateId: string | null;
}

// Backend DTO - Summary for list view
export interface GeneratedReportSummaryDto {
  id: string;
  title: string;
  promptPreview: string | null;
  filesAnalyzed: number;
  generatedAt: string; // ISO date string
  contentPreview: string | null;
}

export interface GenerateReportResponse extends GeneratedReportDto { }
