"use client";

import React from "react";

import { Input } from "../ui/input";
import AddUserDialog from "./AddUserDialog";

const ToolbarUserManagement = () => {
  const [searchUser, setSearchUser] = React.useState("");
  return (
    <div className="w-full flex justify-between items-center">
      <Input
        placeholder="Search User"
        className="w-[300px]"
        value={searchUser}
        onChange={(e) => setSearchUser(e.target.value)}
      />
      <div>
        <AddUserDialog />
      </div>
    </div>
  );
};

export default ToolbarUserManagement;
