import React from "react";

import { Button } from "../ui/button";
import { Checkbox } from "../ui/checkbox";
import DropdownMenuEditUser from "./DropdownMenuEditUser";

const TableUserManagement = () => {
  const users = [
    { email: "ken99@yahoo.com", username: "ken99", role: "Admin" },
    { email: "abe45@gmail.com", username: "abeabe", role: "Admin" },
    { email: "monserrat44@gmail.com", username: "monseratt", role: "Admin" },
    { email: "silas22@gmail.com", username: "sila2la2", role: "Admin" },
    { email: "carmella@hotmail.com", username: "carmella", role: "User" },
  ];

  return (
    <>
      <div className="overflow-x-auto mt-5 rounded-xl shadow-md border">
        <table className="min-w-full divide-y divide-gray-200 text-sm text-left cursor-default">
          <thead className="bg-gray-50">
            <tr className="text-[15px]">
              <th className="px-4 py-3">
                <div className="flex items-center space-x-2">
                  <Checkbox />
                  <span className="font-medium text-gray-700">Email</span>
                </div>
              </th>
              <th className="px-4 py-3 font-medium text-gray-700">Username</th>
              <th className="px-4 py-3 font-medium text-gray-700">Role</th>
              <th className="px-4 py-3 text-right font-medium text-gray-700" />
            </tr>
          </thead>
          <tbody className="divide-y divide-gray-200">
            {users.map((user, index) => (
              <tr key={index} className="hover:bg-gray-50 transition-colors">
                <td className="px-4 py-3">
                  <div className="flex items-center space-x-2">
                    <Checkbox />
                    <span>{user.email}</span>
                  </div>
                </td>
                <td className="px-4 py-3">{user.username}</td>
                <td className="px-4 py-3">
                  <span
                    className={`inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium ${
                      user.role === "Admin" ? "bg-green-100 text-green-800" : "bg-blue-100 text-blue-800"
                    }`}>
                    {user.role}
                  </span>
                </td>
                <td className="px-4 py-3 text-right">
                  <DropdownMenuEditUser />
                </td>
                <td className="px-4 py-3 text-right" />
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      <section className="flex justify-between items-center mt-5">
        <span>0 of {users.length + "row(s)"} selected</span>
        <div className="flex items-center space-x-2">
          <Button variant="outline">Previous</Button>
          <Button variant="outline">Next</Button>
        </div>
      </section>
    </>
  );
};

export default TableUserManagement;
