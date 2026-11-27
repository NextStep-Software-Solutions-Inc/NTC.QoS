import { type Metadata } from "next";

import AreaStudySignalLevelConfigurationPage from "@/components/features/area-study/pages/AreaStudySignalLevelConfigurationPage";

export const metadata: Metadata = {
  title: "Area Study Signal Configuration",
  description: "Configure signal level and entries for an area study type",
};

export default function SignalLevelConfigurationRoutePage() {
  return <AreaStudySignalLevelConfigurationPage />;
}
