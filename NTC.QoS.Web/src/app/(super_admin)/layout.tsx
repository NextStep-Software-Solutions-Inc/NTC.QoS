import { SuperAdminSidebar } from "@/components/super-admin/SuperAdminSidebar";
import { SuperAdminGuard } from "@/components/SuperAdminGuard";

export default function SuperAdminLayout({ children }: { children: React.ReactNode }) {
  return (
    <SuperAdminGuard>
      <div className="flex h-screen overflow-hidden bg-background">
        <SuperAdminSidebar />
        <main className="flex-1 overflow-y-auto lg:ml-64">
          <div className="container mx-auto p-4 md:p-6 lg:p-8">{children}</div>
        </main>
      </div>
    </SuperAdminGuard>
  );
}
