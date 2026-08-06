import { type Metadata } from "next";

import NAPMapPage from "@/components/features/NAP/pages/NAPMapPage";

interface NAPMapPageProps {
  params: Promise<{ region: string }>;
}

export async function generateMetadata({ params }: NAPMapPageProps): Promise<Metadata> {
  const { region } = await params;
  return {
    title: `NAP Map View | Region ${region}`,
    description: "Geographic view and management of Network Access Points.",
  };
}

export default async function NAPsMapRoute({ params }: NAPMapPageProps) {
  const { region } = await params;

  return <NAPMapPage regionId={region} />;
}
