import Link from "next/link";
import { SignedIn, SignedOut, SignInButton, SignUpButton, UserButton } from "@clerk/nextjs";

import { Button } from "./ui/button";

export default function PublicAuthButtons() {
  return (
    <>
      <SignedOut>
        <SignInButton mode="modal">
          <Button size="sm">Sign In</Button>
        </SignInButton>
        <SignUpButton mode="modal">
          <Button variant="outline" size="sm">
            Sign Up
          </Button>
        </SignUpButton>
      </SignedOut>
      <SignedIn>
        <div className="flex items-center gap-5">
          <Link href="/select-region" className="text-sm font-semibold text-primary hover:underline">
            Dashboard
          </Link>
          <UserButton />
        </div>
      </SignedIn>
    </>
  );
}
