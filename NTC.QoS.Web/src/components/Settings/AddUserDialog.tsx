"use client";

import React, { useState } from "react";

import { Button } from "../ui/button";
import { CardContent } from "../ui/card";
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle, DialogTrigger } from "../ui/dialog";
import { Input } from "../ui/input";
// import { Label } from 'recharts'
import { Label } from "../ui/label";
import { Select, SelectContent, SelectGroup, SelectItem, SelectTrigger, SelectValue } from "../ui/select";

const AddUserDialog = () => {
  const [email, setEmail] = useState("");
  const [role, setRole] = useState("Admin");

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
  };

  return (
    <Dialog>
      <Button>
        <DialogTrigger>Add User</DialogTrigger>
      </Button>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Add New User</DialogTitle>
          <DialogDescription>
            {`Enter user details, assign a role, and click "Add User" to complete.`}
          </DialogDescription>
        </DialogHeader>
        <CardContent>
          <form onSubmit={handleSubmit} className="h-full space-y-4 cursor-default">
            <div className="space-y-1">
              <Label htmlFor="email">Email</Label>
              <Input
                id="email"
                type="email"
                placeholder="name@organization.gov.ph"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
              />
            </div>
            <div>
              <Label htmlFor="role">Role</Label>
              <Select value={role} onValueChange={setRole}>
                <SelectTrigger className="w-full border">
                  <SelectValue placeholder="Select a role" />
                </SelectTrigger>
                <SelectContent>
                  <SelectGroup>
                    {/* <SelectLabel>Fruits</SelectLabel> */}
                    <SelectItem value="Admin">Admin</SelectItem>
                    <SelectItem value="User">User</SelectItem>
                  </SelectGroup>
                </SelectContent>
              </Select>
            </div>

            <div className="flex justify-between pt-4">
              <DialogTrigger>
                <Button type="button" variant="ghost">
                  Cancel
                </Button>
              </DialogTrigger>
              <Button type="submit" className="bg-blue-700 hover:bg-blue-800 text-white">
                Submit
              </Button>
            </div>
          </form>
        </CardContent>
      </DialogContent>
    </Dialog>
  );
};

export default AddUserDialog;
