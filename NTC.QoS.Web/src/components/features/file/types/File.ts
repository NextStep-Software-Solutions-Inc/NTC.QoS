export type File = {
  id: string;
  createdAt: string;
  createdBy: string;
  updatedAt: string;
  updatedBy: string;
  isDeleted: boolean;
  deletedAt: string;
  deletedBy: string;
  name: string;
  path: string;
  uploadedBy: string;
  type?: string;
  status?: string;
  errorMessage?: string | null;
  errorLogPath?: string | null;
};

export type FileDetails = File;
