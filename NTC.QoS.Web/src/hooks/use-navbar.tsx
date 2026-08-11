"use client";

import React, { createContext, useContext, useState, type ReactNode } from "react";

interface NavPropsType {
  isOpenBurger: boolean;
  getOpen: () => void;
}

const NavbarContext = createContext<NavPropsType | undefined>(undefined);

export const NavbarProvider = ({ children }: { children: ReactNode }) => {
  const [isOpenBurger, setIsOpenburger] = useState<boolean>(false);

  const getOpen = () => {
    setIsOpenburger((prev) => !prev);
  };

  const DataValue: NavPropsType = { isOpenBurger, getOpen };

  return <NavbarContext.Provider value={DataValue}>{children}</NavbarContext.Provider>;
};

export const useNavbar = (): NavPropsType => {
  const context = useContext(NavbarContext);
  if (!context) {
    throw new Error("useNavbar must be wrap within a NavbarProdiver");
  }
  return context;
};
