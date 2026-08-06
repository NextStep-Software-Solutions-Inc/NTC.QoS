import { columns, type Report } from "./columns";
import { DataTable } from "./data-table";

async function getData(): Promise<Report[]> {
  return [];
}

export default async function DemoPage() {
  const data = await getData();

  return (
    <div className="mx-auto w-full max-w-screen-md">
      <DataTable columns={columns} data={data} />
    </div>
  );
}
