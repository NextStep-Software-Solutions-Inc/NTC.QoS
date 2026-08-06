import { type AreaStudyDetails } from "../../area-study/types/AreaStudy";

export type Study = {
  id: string;
  name: string;
  description?: string;
  areaStudyId: string;
  areaStudy: AreaStudyDetails;
  studyRadius: number;
};

export type StudyDetails = Study;
