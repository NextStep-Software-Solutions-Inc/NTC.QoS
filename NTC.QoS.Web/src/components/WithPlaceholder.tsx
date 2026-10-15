import React from "react";

interface WithPlaceholderProps {
  isLoading: boolean;
  children: React.ReactNode;
  placeholder: React.ReactNode;
}
export default function WithPlaceholder({ isLoading, placeholder, children }: WithPlaceholderProps) {
  return <>{isLoading ? placeholder : children}</>;
}
