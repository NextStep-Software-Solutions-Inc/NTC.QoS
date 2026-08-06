export type StudyNetwork = {
  id: string;
  name: string;
  description?: string;
  studyId: string;
  networkId: string;
  priority: number;
  status: "active" | "inactive" | "pending";
  createdAt: string;
  updatedAt: string;
};

export type StudyNetworkDetails = StudyNetwork;
