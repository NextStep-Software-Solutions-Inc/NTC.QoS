import React from "react";

interface AdminMapLayout {
  children: React.ReactNode;
}

export default function AdminMapLayout({ children }: AdminMapLayout) {
  return <>{children}</>;
}
