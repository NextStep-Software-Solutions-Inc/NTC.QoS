import { type Metadata } from "next";

import NAPsManagementPage from "@/components/features/NAP/pages/NAPsManagementPage";

interface NAPsPageProps {
  params: Promise<{ region: string }>;
}

export async function generateMetadata({ params }: NAPsPageProps): Promise<Metadata> {
  const { region } = await params;
  return {
    title: `NAP Management | Region ${region}`,
    description: "Manage Network Access Points in the broadband infrastructure.",
  };
}

export default async function NAPsPage({ params }: NAPsPageProps) {
  const { region } = await params;

  return <NAPsManagementPage regionId={region} />;
}
