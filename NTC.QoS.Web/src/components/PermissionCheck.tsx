import React from "react";

import { type MetaActions } from "@/types/global";

interface PermissionCheckProps {
  action: MetaActions[number];
  actions: MetaActions;
  children: React.ReactNode;
}

const PermissionCheck: React.FC<PermissionCheckProps> = ({ action, actions, children }) => {
  if (!actions.includes(action)) {
    return null;
  }

  return <>{children}</>;
};

export default PermissionCheck;
