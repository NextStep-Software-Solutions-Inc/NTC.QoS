"use client";

import React, { useEffect, useState } from "react";

import { cn } from "@/lib/utils";

interface NavbarProps {
  className?: string;
  children: React.ReactNode;
}

const Navbar: React.FC<NavbarProps> = ({ children }) => {
  const [isVisible, setIsVisible] = useState<boolean>(true);
  const [lastScrollY, setLastScrollY] = useState<number>(0);

  useEffect(() => {
    const controlNavbar = () => {
      const currentScrollY = window.scrollY;

      if (currentScrollY > lastScrollY && currentScrollY > 260) {
        // Hide navbar when scrolling down and past 80px
        setIsVisible(false);
      } else {
        setIsVisible(true);
      }

      setLastScrollY(currentScrollY);
    };

    window.addEventListener("scroll", controlNavbar);

    return () => {
      window.removeEventListener("scroll", controlNavbar);
    };
  }, [lastScrollY]);

  return (
    <>
      <nav
        className={cn("group: fixed left-0 right-0 top-0 transition-all duration-300 ease-in-out", {
          "translate-y-[-100%]": !isVisible,
          "bg-gradient-to-br from-[#0a0f2c65] to-[#0f172ae8] backdrop-blur-[10px]": lastScrollY > 260,
        })}>
        {children}
      </nav>
    </>
  );
};

export default Navbar;
