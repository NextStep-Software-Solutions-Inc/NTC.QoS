"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";

import { type ProvinceColumn } from "@/components/features/province/types/ProvinceColumn";
import { Button } from "@/components/ui/button";

interface ActionsProps {
  data: ProvinceColumn;
  readonly?: boolean;
}

export default function Actions({ data }: ActionsProps) {
  const pathname = usePathname();
  const distProvince = data.id;
  const distPath = `${pathname}/cell/${distProvince}`;
  return (
    <Button variant="link">
      <Link href={distPath}>See All</Link>
    </Button>
  );
}
