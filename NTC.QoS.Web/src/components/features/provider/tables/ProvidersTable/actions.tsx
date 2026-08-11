"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";

import { type Provider } from "@/components/features/provider/types/Provider";
import { Button } from "@/components/ui/button";

interface ActionsProps {
  data: Provider;
  readonly?: boolean;
}

export default function Actions({ data }: ActionsProps) {
  const pathname = usePathname();
  const providerId = data.id.toLowerCase().replace(/\s+/g, "-");

  const newPath = `${pathname}/${providerId}`;

  return (
    <Button variant="link">
      <Link href={newPath}>View</Link>
    </Button>
  );
}
