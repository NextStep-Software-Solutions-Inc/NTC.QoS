import { type Metadata } from "next";

import CellsiteMapPage from "@/components/features/cell/pages/CellsiteMapPage";

interface CellsiteMapPageProps {
  params: Promise<{ region: string }>;
}

export async function generateMetadata({ params }: CellsiteMapPageProps): Promise<Metadata> {
  const { region } = await params;
  return {
    title: `Cellsite Map View | Region ${region}`,
    description: "Geographic view and management of Cell Sites.",
  };
}

export default async function CellsitesMapRoute({ params }: CellsiteMapPageProps) {
  const { region } = await params;

  return <CellsiteMapPage regionId={region} />;
}
