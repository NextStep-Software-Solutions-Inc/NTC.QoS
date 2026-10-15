import { type Metadata } from "next";

import "./page.css";

import LandingHeader from "@/components/LandingHeader";
import FooterV2 from "@/components/v2/FooterV2";
import AdminDashboardSection from "@/components/v2/sections/AdminDashboardSection";
import CTASectionV2 from "@/components/v2/sections/CTASectionV2";
import FAQSectionV2 from "@/components/v2/sections/FAQSectionV2";
import FeaturesSectionV2 from "@/components/v2/sections/FeaturesSectionV2";
import HeroSectionV2 from "@/components/v2/sections/HeroSectionV2";
import StatsSectionV2 from "@/components/v2/sections/StatsSectionV2";

const rawSiteUrl = process.env.NEXT_PUBLIC_SITE_URL ?? process.env.VERCEL_URL ?? "http://localhost:3000";
let siteUrl = rawSiteUrl;
try {
  // ensure URL has protocol; if missing, assume https
  if (!/^https?:\/\//i.test(siteUrl)) {
    siteUrl = `https://${siteUrl}`;
  }
  // validate, will throw on invalid input like "https//dev-qos.vercel.app"
  // and prevent build-time crash
  new URL(siteUrl);
} catch {
  // fallback to safe default during build
  // keep message short so server logs show the issue
  console.warn("Invalid NEXT_PUBLIC_SITE_URL; falling back to http://localhost:3000 — value:", rawSiteUrl);
  siteUrl = "http://localhost:3000";
}

export const metadata: Metadata = {
  title: "NTC QoS - Philippine National Broadband Map | Coverage & Speed Testing",
  description:
    "Official Philippine National Telecommunications Commission (NTC) broadband mapping platform. Check internet coverage, speed test results, service quality metrics, and infrastructure data across the Philippines. Find reliable ISP providers in your area.",
  keywords: [
    "NTC",
    "QoS",
    "Philippines",
    "Broadband",
    "Map",
    "Internet Coverage",
    "Speed Test",
    "ISP",
    "Telecommunications",
    "Network Quality",
    "Philippine Internet",
    "Connectivity Map",
    "Broadband Infrastructure",
    "NTC Philippines",
    "Internet Providers",
    "Network Coverage",
    "Telecommunications Commission",
    "Digital Infrastructure",
  ],
  authors: [{ name: "National Telecommunications Commission Philippines" }],
  creator: "National Telecommunications Commission Philippines",
  publisher: "NTC Philippines",
  formatDetection: { email: false, address: false, telephone: false },
  metadataBase: new URL(siteUrl),
  alternates: { canonical: siteUrl },
  robots: {
    index: true,
    follow: true,
    googleBot: { index: true, follow: true, "max-video-preview": -1, "max-image-preview": "large", "max-snippet": -1 },
  },
  openGraph: {
    title: "NTC QoS - Philippine National Broadband Map",
    description:
      "Official Philippine National Telecommunications Commission broadband mapping platform. Check internet coverage, speed test results, and service quality metrics across the Philippines.",
    url: siteUrl,
    siteName: "NTC QoS",
    type: "website",
    locale: "en-US",
    images: [
      {
        url: `/og-image.png`,
        width: 1200,
        height: 630,
        alt: "NTC QoS Philippine National Broadband Map - Coverage and Speed Testing Platform",
        type: "image/png",
      },
    ],
  },
  twitter: {
    card: "summary_large_image",
    title: "NTC QoS - Philippine National Broadband Map",
    description:
      "Official Philippine National Telecommunications Commission broadband mapping platform. Check internet coverage and speed test results across the Philippines.",
    images: [{ url: `/og-image.png`, alt: "NTC QoS Philippine National Broadband Map" }],
    creator: "@NTCPhilippines",
    site: "@NTCPhilippines",
  },
  // verification: {
  //   google: process.env.NEXT_PUBLIC_GOOGLE_VERIFICATION ?? "",
  //   yandex: process.env.NEXT_PUBLIC_YANDEX_VERIFICATION ?? "",
  //   other: {
  //     'msvalidate.01': process.env.NEXT_PUBLIC_BING_VERIFICATION ?? "",
  //   },
  // },
  category: "Technology",
  classification: "Government Service",
  other: {
    "apple-mobile-web-app-capable": "yes",
    "apple-mobile-web-app-status-bar-style": "black-translucent",
    "theme-color": "#1e3a8a",
  },
};

export default async function Home() {
  return (
    <div className="overflow-x-hidden">
      <LandingHeader />
      <main className="text-sm sm:text-base">
        <HeroSectionV2 />
        <FeaturesSectionV2 />
        <StatsSectionV2 />
        <AdminDashboardSection />
        <CTASectionV2 />
        <FAQSectionV2 />
        <FooterV2 />
      </main>
    </div>
  );
}
