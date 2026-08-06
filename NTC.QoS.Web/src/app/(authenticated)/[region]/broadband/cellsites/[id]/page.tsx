import CellsiteDetailPage from "@/components/features/cell/pages/CellsiteDetailPage";

interface CellsiteDetailPageProps {
  params: Promise<{ region: string; id: string }>;
}

export default async function CellsiteDetailRoute({ params }: CellsiteDetailPageProps) {
  const { region, id } = await params;

  return <CellsiteDetailPage regionId={region} cellId={id} />;
}
