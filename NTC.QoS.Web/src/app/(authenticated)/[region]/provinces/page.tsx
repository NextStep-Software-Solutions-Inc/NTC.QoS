import React from "react";
import { type Metadata } from "next";

import ProvinceManagement from "@/components/ProvinceManagement";

export const metadata: Metadata = {
  title: "Provinces | NTC QoS",
  description: "Manage provinces in the Philippine National Broadband Map.",
};

const ProvincesPage = () => {
  return <ProvinceManagement />;
};

export default ProvincesPage;
