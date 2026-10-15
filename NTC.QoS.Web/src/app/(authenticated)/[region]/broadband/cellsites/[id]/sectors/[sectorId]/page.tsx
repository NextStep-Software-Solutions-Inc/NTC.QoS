import SectorDetailPage from "@/components/features/cell-sector/pages/SectorDetailPage";

interface PageProps {
  params: Promise<{
    region: string;
    id: string;
    sectorId: string;
  }>;
}

export default async function SectorDetailRoute({ params }: PageProps) {
  const { region, id: cellId, sectorId } = await params;

  return (
    <SectorDetailPage
      region={region}
      cellId={cellId}
      sectorId={sectorId}
    />
  );
}
