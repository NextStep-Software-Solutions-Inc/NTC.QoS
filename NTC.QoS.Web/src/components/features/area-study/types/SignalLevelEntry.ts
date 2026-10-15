export type SignalLevelEntry = {
  id: string;
  signalLevelId: string;
  color: string;
  thresholdFrom?: number | null;
  thresholdTo?: number | null;
  description: string;
  sortOrder: number;
};

export type CreateSignalLevelEntryPayload = {
  color: string;
  thresholdFrom?: number;
  thresholdTo?: number;
  description: string;
  sortOrder: number;
};

export type UpdateSignalLevelEntryPayload = Partial<CreateSignalLevelEntryPayload>;
