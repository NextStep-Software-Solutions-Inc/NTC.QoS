"use client";

import React, { useState } from "react";

import { Card, CardContent } from "../ui/card";
import { Checkbox } from "../ui/checkbox";
import { Input } from "../ui/input";
import { Label } from "../ui/label";
import { Separator } from "../ui/separator";

const AccountInformation = () => {
  const [email, setEmail] = useState("regionx@ntcmail.com");
  const [password, setPassword] = useState("***************");
  const [keepSignedIn, setKeepSignedIn] = useState(true);
  const [enable2FA, setEnable2FA] = useState(false);

  return (
    <Card className="w-[700px]">
      <CardContent>
        <h2 className="text-sm font-semibold my-4">ACCOUNT INFORMATION</h2>

        {/* Email */}
        <div className="mb-4 mt-[2rem] w-[300px]">
          <Label htmlFor="email" className="text-sm mb-1 block">
            Email
          </Label>
          <Input id="email" type="email" value={email} onChange={(e) => setEmail(e.target.value)} />
        </div>

        {/* Password */}
        <div className="mb-4 w-[300px]">
          <section className="flex items-center gap-x-5">
            <Label htmlFor="password" className="text-sm mb-1 block">
              Password
            </Label>
            <Separator orientation="vertical" className="bg-gray-300 w-[1px] h-[20px] my-1" />
            <Label htmlFor="password" className="mb-1 block text-blue-600 p-0 h-auto text-sm">
              Change
            </Label>
          </section>
          <div className="">
            <Input
              id="password"
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              className="w-full"
            />
          </div>
        </div>

        <Separator className="my-5 w-full h-[1px] bg-gray-300" />
        {/* Checkboxes */}
        <div className="space-y-5 mt-6">
          <div className="flex items-center space-x-2">
            <Checkbox id="keepSignedIn" checked={keepSignedIn} onCheckedChange={() => setKeepSignedIn(!keepSignedIn)} />
            <Label htmlFor="keepSignedIn">Keep me signed in on this Device</Label>
          </div>

          <div className="flex items-center space-x-2 ">
            <Checkbox id="enable2FA" checked={enable2FA} onCheckedChange={() => setEnable2FA(!enable2FA)} />
            <Label htmlFor="enable2FA">Enable two-factor authentication (2FA)</Label>
          </div>
        </div>
      </CardContent>
    </Card>
  );
};

export default AccountInformation;
