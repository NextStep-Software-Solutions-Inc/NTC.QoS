import React from "react";
import { type Metadata } from "next";

import ManageBarangayCoverage from "@/components/ManageBarangayCoverage/index";

export const metadata: Metadata = {
  title: "Barangay Coverage | NTC QoS",
  description: "Manage barangay broadband coverage data.",
};

const BarangayCoveragePage = () => {
  return <ManageBarangayCoverage />;
};

export default BarangayCoveragePage;
