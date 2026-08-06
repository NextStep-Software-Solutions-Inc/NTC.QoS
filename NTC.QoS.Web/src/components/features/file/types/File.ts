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
};

export type FileDetails = File;
