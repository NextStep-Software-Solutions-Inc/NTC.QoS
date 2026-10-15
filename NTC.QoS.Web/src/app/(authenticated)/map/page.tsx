import React from "react";

import RedirectToCorrectRegion from "@/components/RedirectToCorrectRegion";

const redirectPath = "/map";
export default function MapPage() {
  return <RedirectToCorrectRegion redirectPath={redirectPath} />;
}
