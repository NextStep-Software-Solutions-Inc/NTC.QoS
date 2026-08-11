"use client";

import { Fragment, useMemo } from "react";
import Link from "next/link";
import { usePathname } from "next/navigation";

import {
  Breadcrumb as BC,
  BreadcrumbItem,
  BreadcrumbLink,
  BreadcrumbList,
  BreadcrumbPage,
  BreadcrumbSeparator,
} from "@/components/ui/breadcrumb";
import { useLocationMap } from "@/providers/LocationMapContext";

export default function Breadcrumb() {
  const pathname = usePathname();
  const { resolveUuid } = useLocationMap();
  const segments = useMemo(() => {
    return pathname.split("/").filter(Boolean);
  }, [pathname]);

  return (
    <BC>
      <BreadcrumbList className="">
        {segments.map((segment, index) => {
          const isLast = index === segments.length - 1;
          const label = resolveUuid(segment) ?? decodeURIComponent(segment.replace(/[-_+=]/g, " ").toLowerCase());
          return (
            <Fragment key={segment}>
              <BreadcrumbItem>
                {isLast ? (
                  <BreadcrumbPage className="capitalize">{label}</BreadcrumbPage>
                ) : (
                  <>
                    <BreadcrumbLink asChild>
                      <Link href={`/${segments.slice(0, index + 1).join("/")}`} className="capitalize">
                        {label}
                      </Link>
                    </BreadcrumbLink>
                  </>
                )}
              </BreadcrumbItem>
              {!isLast && <BreadcrumbSeparator />}
            </Fragment>
          );
        })}
      </BreadcrumbList>
    </BC>
  );
}
