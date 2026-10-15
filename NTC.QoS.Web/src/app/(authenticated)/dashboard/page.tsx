import React from "react";

import RedirectToCorrectRegion from "@/components/RedirectToCorrectRegion";

const redirectPath = "/dashboard";
export default function DashboardPage() {
  return <RedirectToCorrectRegion redirectPath={redirectPath} />;
}
