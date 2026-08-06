"use client";

import React, { useEffect, useState } from "react";
import Link from "next/link";

import Logo from "../Logo/Logo";
import styles from "./Navbar.module.css";

interface NavItem {
  href: string;
  label: string;
}

const navItems: NavItem[] = [
  // { href: "/components", label: "Component" },
  { href: "/map", label: "Map" },
  { href: "/#faqs", label: "FAQs" },
  { href: "/#contact", label: "Contact Us" },
];

interface NavbarProps {
  className?: string;
}

const Navbar: React.FC<NavbarProps> = ({ className = "" }) => {
  const [isMobileMenuOpen, setIsMobileMenuOpen] = useState<boolean>(false);
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

  const toggleMobileMenu = (): void => {
    setIsMobileMenuOpen(!isMobileMenuOpen);
  };
  return (
    <nav
      className={`${!(lastScrollY > 260) ? "" : styles.scrolled} ${styles.navbar} ${!isVisible ? styles.hidden : ""} ${className}`.trim()}>
      <div className={styles.navContainer}>
        {/* Logo */}
        <Link href="/" className={styles.logo}>
          <Logo width={55} height={52.2} />
        </Link>

        {/* Desktop Navigation */}
        <div className={styles.desktopNav}>
          {navItems.map((item) => (
            <Link key={item.href} href={item.href} className={styles.navLink}>
              {item.label}
            </Link>
          ))}
        </div>

        {/* Auth Buttons */}
        <div className={styles.authButtons}>
          <Link href="/signup" className={styles.signupButton}>
            Sign Up
          </Link>
          <Link href="/login" className={styles.loginButton}>
            Login
          </Link>
        </div>

        {/* Mobile Menu Button */}
        <button
          className={styles.mobileMenuButton}
          onClick={toggleMobileMenu}
          aria-label="Toggle mobile menu"
          aria-expanded={isMobileMenuOpen}>
          <span className={`${styles.hamburger} ${isMobileMenuOpen ? styles.open : ""}`} />
        </button>

        {/* Mobile Navigation */}
        <div className={`${styles.mobileNav} ${isMobileMenuOpen ? styles.open : ""}`} aria-hidden={!isMobileMenuOpen}>
          {navItems.map((item) => (
            <Link
              key={item.href}
              href={item.href}
              className={styles.mobileNavLink}
              onClick={() => setIsMobileMenuOpen(false)}>
              {item.label}
            </Link>
          ))}
          <div className={styles.mobileAuthButtons}>
            <Link href="/signup" className={styles.mobileSignupButton} onClick={() => setIsMobileMenuOpen(false)}>
              Sign Up
            </Link>
            <Link href="/login" className={styles.mobileLoginButton} onClick={() => setIsMobileMenuOpen(false)}>
              Login
            </Link>
          </div>
        </div>
      </div>
    </nav>
  );
};

export default Navbar;
