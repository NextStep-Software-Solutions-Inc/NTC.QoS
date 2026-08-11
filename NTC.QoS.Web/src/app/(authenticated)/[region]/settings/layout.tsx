import React from "react";

import SettingsNav from "@/components/Tabs/SettingsNav";

interface SettingsLayoutProps {
  children: React.ReactNode;
}

export default function SettingsLayout({ children }: SettingsLayoutProps) {
  return (
    <div className="flex-1 flex flex-col min-h-0 px-4 space-y-4">
      <SettingsNav />
      {children}
    </div>
  );
}
