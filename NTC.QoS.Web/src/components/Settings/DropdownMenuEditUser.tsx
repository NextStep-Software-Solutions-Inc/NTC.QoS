import React from "react";
import { SquarePen, Trash } from "lucide-react";

import { Button } from "../ui/button";
import { DropdownMenu, DropdownMenuContent, DropdownMenuTrigger } from "../ui/dropdown-menu";

const DropdownMenuEditUser = () => {
  return (
    <DropdownMenu>
      <DropdownMenuTrigger>
        <Button variant="outline">...</Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent className="w-[18px]">
        <div className="grid space-y-2 p-2">
          <Button variant="ghost">
            <SquarePen /> Edit
          </Button>
          <Button variant="ghost" className="text-red-500">
            <Trash /> Delete
          </Button>
        </div>
      </DropdownMenuContent>
    </DropdownMenu>
  );
};

export default DropdownMenuEditUser;
