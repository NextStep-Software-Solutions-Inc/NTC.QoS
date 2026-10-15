import Breadcrumb from "@/components/Tables/Breadcrumb";

export default function RegionLayout({ children }: { children: React.ReactNode }) {
  return (
    <div className="px-4 flex flex-1 flex-col min-h-0 space-y-4">
      <Breadcrumb />
      {children}
    </div>
  );
}
