"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { SignedIn, SignedOut, SignInButton, UserButton, useUser } from "@clerk/nextjs";
import { Menu } from "lucide-react";

import Logo from "@/components/Logo/Logo";
import { Button } from "@/components/ui/button";
import { Sheet, SheetContent, SheetTrigger } from "@/components/ui/sheet";
import { cn } from "@/lib/utils";
import { getAuthUserRequest } from "@/components/features/users/handlers/getAuthUserRequest";
import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";

const useAuthUser = createQueryHookFromTemplate(getAuthUserRequest);

interface NavItem {
  href: string;
  label: string;
}

const navItems: NavItem[] = [
  { href: "/public/map", label: "Map" },
  { href: "/#faqs", label: "FAQs" },
  { href: "/#contact", label: "Contact Us" },
];

export default function LandingHeader() {
  const [isVisible, setIsVisible] = useState(true);
  const [lastScrollY, setLastScrollY] = useState(0);
  const [isScrolled, setIsScrolled] = useState(false);
  const { user } = useUser();

  const { data: authUser } = useAuthUser({
    pathParams: { id: user?.id ?? "" },
    queryParams: {},
  });

  const isSuperAdmin = authUser?.data?.role?.key === "super_admin";

  useEffect(() => {
    let ticking = false;

    const controlNavbar = () => {
      const currentScrollY = window.scrollY;

      // Update background opacity based on scroll
      setIsScrolled(currentScrollY > 20);

      // Hide/show navbar based on scroll direction
      if (currentScrollY > lastScrollY && currentScrollY > 100) {
        // Scrolling down & past 100px - hide navbar
        setIsVisible(false);
      } else {
        // Scrolling up or at top - show navbar
        setIsVisible(true);
      }

      setLastScrollY(currentScrollY);
      ticking = false;
    };

    const handleScroll = () => {
      if (!ticking) {
        window.requestAnimationFrame(controlNavbar);
        ticking = true;
      }
    };

    window.addEventListener("scroll", handleScroll, { passive: true });
    return () => window.removeEventListener("scroll", handleScroll);
  }, [lastScrollY]);

  return (
    <header
      className={cn(
        "fixed top-0 left-0 right-0 z-50 transition-all duration-300 ease-in-out",
        isVisible ? "translate-y-0" : "-translate-y-full",
        isScrolled ? "bg-white/70 backdrop shadow-md " : "bg-transparent backdrop-blur-md",
      )}>
      <nav className="container mx-auto px-4 sm:px-6 lg:px-8">
        <div className="flex items-center justify-between h-16">
          {/* Logo */}
          <Link href="/" className="flex items-center gap-3 group">
            <Logo width={40} height={38} className="transition-transform group-hover:scale-105" />
            <span className="hidden sm:block text-base font-semibold text-gray-900">NTC QoS</span>
          </Link>

          {/* Desktop Navigation */}
          <div className="hidden md:flex items-center gap-6">
            {navItems.map((item) => (
              <Link
                key={item.href}
                href={item.href}
                className="text-sm font-medium text-gray-900 hover:text-blue-600 transition-colors">
                {item.label}
              </Link>
            ))}
          </div>

          {/* Auth Buttons - Desktop */}
          <div className="hidden md:flex items-center gap-4">
            <SignedOut>
              <SignInButton mode="modal">
                <Button variant="default" size="default" className="font-medium">
                  Login
                </Button>
              </SignInButton>
            </SignedOut>
            <SignedIn>
              <UserButton appearance={{ elements: { avatarBox: "w-8 h-8" } }}>
                <UserButton.MenuItems>
                  <UserButton.Link
                    label="Dashboard"
                    labelIcon={<span className="text-xs">📊</span>}
                    href="/select-region"
                  />
                  {isSuperAdmin && (
                    <UserButton.Link
                      label="Super Admin Console"
                      labelIcon={<span className="text-xs">⚙️</span>}
                      href="/super-admin"
                    />
                  )}
                </UserButton.MenuItems>
              </UserButton>
            </SignedIn>
          </div>

          {/* Mobile Menu Trigger */}
          <Sheet>
            <SheetTrigger asChild>
              <button
                className={cn(
                  "md:hidden p-2 rounded-md transition-colors",
                  "hover:bg-accent focus:outline-none focus:ring-2 focus:ring-primary",
                  isScrolled ? "text-foreground" : "text-white hover:text-foreground",
                )}
                aria-label="Open menu">
                <Menu className="h-6 w-6" />
              </button>
            </SheetTrigger>
            <SheetContent side="left" className="w-80 bg-white">
              <div className="flex flex-col gap-6 pt-6">
                {/* Logo in Drawer */}
                <Link href="/" className="flex items-center gap-3">
                  <Logo width={40} height={38} />
                  <span className="text-base font-semibold">NTC QoS</span>
                </Link>
                {/* Navigation Links */}
                <div className="space-y-4">
                  {navItems.map((item) => (
                    <Link
                      key={item.href}
                      href={item.href}
                      className="block py-2 text-sm font-medium text-muted-foreground hover:text-foreground transition-colors">
                      {item.label}
                    </Link>
                  ))}
                </div>
                {/* Auth Buttons */}
                <div className="pt-4 border-t border-border">
                  <SignedOut>
                    <SignInButton mode="modal">
                      <Button variant="default" size="default" className="w-full">
                        Login
                      </Button>
                    </SignInButton>
                  </SignedOut>
                  <SignedIn>
                    <div className="space-y-4">
                      <div className="flex items-center gap-3">
                        <UserButton appearance={{ elements: { avatarBox: "w-12 h-12" } }}>
                          <UserButton.MenuItems>
                            <UserButton.Link
                              label="Dashboard"
                              labelIcon={<span className="text-xs">📊</span>}
                              href="/select-region"
                            />
                            {isSuperAdmin && (
                              <UserButton.Link
                                label="Super Admin Console"
                                labelIcon={<span className="text-xs">⚙️</span>}
                                href="/super-admin"
                              />
                            )}
                          </UserButton.MenuItems>
                        </UserButton>
                        <div>
                          <p className="text-sm font-medium">Account</p>
                          <p className="text-xs text-muted-foreground">Manage your profile</p>
                        </div>
                      </div>
                      <Link href="/select-region" className="block">
                        <Button variant="outline" size="sm" className="w-full">
                          Go to Dashboard
                        </Button>
                      </Link>
                      {isSuperAdmin && (
                        <Link href="/super-admin" className="block">
                          <Button variant="outline" size="sm" className="w-full">
                            Go to Super Admin
                          </Button>
                        </Link>
                      )}
                    </div>
                  </SignedIn>
                </div>
              </div>
            </SheetContent>
          </Sheet>
        </div>
      </nav>
    </header>
  );
}
