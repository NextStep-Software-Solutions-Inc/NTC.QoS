"use client";

import Link from "next/link";
import { SignedIn, SignedOut, SignInButton, SignUpButton, UserButton, useUser } from "@clerk/nextjs";

import { cn } from "@/lib/utils";
import { Button } from "./ui/button";
import { getAuthUserRequest } from "@/components/features/users/handlers/getAuthUserRequest";
import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";

const useAuthUser = createQueryHookFromTemplate(getAuthUserRequest);

export default function AuthButtons() {
  const { isLoaded, user } = useUser();
  const { data: authUser } = useAuthUser({
    pathParams: { id: user?.id ?? "" },
    queryParams: {},
  });

  const isSuperAdmin = authUser?.data?.role?.key === "super_admin";

  return (
    <>
      <SignedOut>
        <SignUpButton mode="modal">
          <Button
            className={cn(
              "rounded-sm px-4 py-2 text-base no-underline transition-all duration-200 ease-in-out",
              "border border-solid border-white bg-transparent text-white hover:bg-[#ffffff25]",
            )}
            size="lg">
            Sign Up
          </Button>
        </SignUpButton>

        <SignInButton mode="modal">
          <Button
            className={cn(
              "rounded-sm px-4 py-2 text-base no-underline transition-all duration-200 ease-in-out",
              "bg-white text-[#003366] hover:bg-[#f0f0f0]",
            )}
            size="lg">
            Login
          </Button>
        </SignInButton>
      </SignedOut>
      <SignedIn>
        <div className="flex items-center justify-center gap-4 flex-wrap my-2 flex-col md:flex-row">
          <div className="text-[#333333] truncate max-w-[200px] text-center">
            <Link href="/select-region" className="">
              Dashboard
            </Link>
          </div>
        </div>
        <div className="w-[40px] h-[40px] flex items-center justify-center">
          {!isLoaded ? (
            <div className="animate-pulse rounded-full bg-gray-300 w-[40px] h-[40px]" />
          ) : (
            <UserButton>
              <UserButton.MenuItems>
                {isSuperAdmin && (
                  <UserButton.Link
                    label="Super Admin Console"
                    labelIcon={<span className="text-xs">⚙️</span>}
                    href="/super-admin"
                  />
                )}
              </UserButton.MenuItems>
            </UserButton>
          )}
        </div>
      </SignedIn>
    </>
  );
}
