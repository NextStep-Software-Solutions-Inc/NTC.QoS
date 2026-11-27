import { type SignalLevelEntry } from "./SignalLevelEntry";

export type SignalLevel = {
  id: string;
  areaStudyId: string;
  noOfLevels: number;
  signalLevelEntries?: SignalLevelEntry[];
};

export type CreateSignalLevelPayload = {
  areaStudyId: string;
  noOfLevels: number;
};
