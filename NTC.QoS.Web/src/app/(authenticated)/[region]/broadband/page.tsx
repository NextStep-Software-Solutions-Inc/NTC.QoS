import { BroadbandDashboard } from "@/components/features/broadband-dashboard/components/BroadbandDashboard";

interface PageProps {
  params: Promise<{ region: string }>;
}

export default async function BroadbandPage({ params }: PageProps) {
  const { region } = await params;
  return <BroadbandDashboard region={region} />;
}
