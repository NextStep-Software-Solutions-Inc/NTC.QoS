import { type Metadata } from "next";

import ProviderDetails from "@/components/features/provider/ProviderDetails";

export const metadata: Metadata = {
  title: "Provider Details",
  description: "This page contains details about a service provider",
};
export default function ProviderDetailsPage() {
  return <ProviderDetails />;
}
