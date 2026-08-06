import CellsitesManagementPage from "@/components/features/cell/pages/CellsitesManagementPage";

interface CellsitesPageProps {
  params: Promise<{ region: string }>;
}

export default async function CellsitesPage({ params }: CellsitesPageProps) {
  const { region } = await params;

  return <CellsitesManagementPage regionId={region} />;
}
