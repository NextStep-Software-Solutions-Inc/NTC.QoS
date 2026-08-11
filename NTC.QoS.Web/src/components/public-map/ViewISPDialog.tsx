import React from "react";

import Each from "../Each";
import { ResponsiveDialog } from "../ResponsiveDialog";
import { Button } from "../ui/button";

interface ViewISPDialogProps {
  providers: { name: string; id: string; color: string; uploadSpeed: number; downloadSpeed: number; rssi: number }[];
}

const ViewISPDialog = ({ providers }: ViewISPDialogProps) => {
  return (
    <ResponsiveDialog
      title="Available Internet Service Providers"
      description="The following ISPs are available in your selected area."
      trigger={
        <Button
          size={"sm"}
          className="mt-2 w-full bg-black text-[12px] text-white hover:bg-gray-800"
          disabled={providers.length === 0}>
          View Available ISP
        </Button>
      }>
      <div className="w-full grid gap-y-3 mx-auto drop-shadow-md">
        <Each of={[...providers]} keyExtractor={(item) => item.id}>
          {(provider) => (
            <section className="rounded-sm border ">
              <h1
                className="text-white text-center text-[14px] px-2 py-2 rounded-t-sm"
                style={{ backgroundColor: provider.color }}>
                {provider.name}
              </h1>
              <ul className="p-2 list-item text-[14px]">
                <li>
                  Download speed: <span className="text-primary">{provider.downloadSpeed} Mbps</span>
                </li>
                <li>
                  Upload speed: <span className="text-primary">{provider.uploadSpeed} Mbps</span>
                </li>
                <li>
                  RSSI: <span className="text-primary">{provider.rssi} dBm</span>
                </li>
              </ul>
            </section>
          )}
        </Each>
      </div>
    </ResponsiveDialog>
  );
};

export default ViewISPDialog;
