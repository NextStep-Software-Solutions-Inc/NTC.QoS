import { Metadata } from "next";

import MapClient from "@/components/MapClient";
import { WebWorkerDebugPanel } from "@/components/WebWorkerDebugPanel";

export const metadata: Metadata = {
  title: "NTC Broadband QoS Public Map | Philippines Internet Coverage & Quality",
  description:
    "Explore the Philippines' most comprehensive broadband Quality of Service (QoS) map. View real-time internet coverage, speed metrics, and service quality data across all regions. Make informed decisions about your internet service provider.",
  keywords: [
    "NTC",
    "Broadband",
    "QoS",
    "Quality of Service",
    "Internet Coverage",
    "Philippines",
    "ISP",
    "Internet Speed",
    "Network Coverage",
    "Telecommunications",
    "Mobile Data",
    "Fiber Internet",
    "DSL",
    "5G Coverage",
    "LTE Coverage",
  ],
  authors: [{ name: "NTC - National Telecommunications Commission" }],
  creator: "NTC QoS Team",
  publisher: "National Telecommunications Commission",
  formatDetection: { email: false, address: false, telephone: false },
  metadataBase: new URL("https://ntc-qos.vercel.app"),
  alternates: { canonical: "/public/map" },
  openGraph: {
    title: "NTC Broadband QoS Public Map | Philippines Internet Coverage",
    description:
      "Explore comprehensive broadband coverage and quality metrics across the Philippines. Real-time data on internet speeds, network reliability, and service providers.",
    url: "https://ntc-qos.vercel.app/public/map",
    siteName: "NTC QoS",
    images: [
      {
        url: "/og-public-map.png",
        width: 1200,
        height: 630,
        alt: "NTC Broadband QoS Public Map - Philippines Internet Coverage Visualization",
        type: "image/png",
      },
      {
        url: "/og-public-map-square.png",
        width: 1200,
        height: 1200,
        alt: "NTC Broadband QoS Public Map - Philippines Internet Coverage Visualization",
        type: "image/png",
      },
    ],
    locale: "en_PH",
    type: "website",
    countryName: "Philippines",
  },
  twitter: {
    card: "summary_large_image",
    title: "NTC Broadband QoS Public Map | Philippines Internet Coverage",
    description:
      "Explore the Philippines' most comprehensive broadband Quality of Service map. Real-time internet coverage, speed metrics, and service quality data.",
    images: ["/og-public-map.png"],
    creator: "@NTC_Ph",
    site: "@NTC_Ph",
  },
  robots: {
    index: true,
    follow: true,
    googleBot: { index: true, follow: true, "max-video-preview": -1, "max-image-preview": "large", "max-snippet": -1 },
  },
  // verification: {
  //   google: "your-google-site-verification-code",
  // },
  category: "Technology",
};

// Disable static generation for this page since it uses Clerk components
export const dynamic = "force-dynamic";

export default function PublicMapPage() {
  return (
    <>
      <MapClient />
      <WebWorkerDebugPanel />
    </>
  );
}
