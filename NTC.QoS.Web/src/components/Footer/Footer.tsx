import React from "react";
import Link from "next/link";

import { cn } from "@/lib/utils";
import Logo from "../Logo/Logo";

interface FooterSection {
  title: string;
  links: { href: string; label: string }[];
}

interface SocialLink {
  href: string;
  icon: string;
  label: string;
}

const showUnavailableFooterLinks = process.env.NEXT_PUBLIC_ENABLE_UNAVAILABLE_FOOTER_LINKS === "true";
const showFooterContactDetails = process.env.NEXT_PUBLIC_ENABLE_FOOTER_CONTACT_DETAILS === "true";

const quickLinks: FooterSection = {
  title: "QUICK LINKS",
  links: [
    ...(showUnavailableFooterLinks
      ? [
          { href: "/public/map", label: "Explore Broadband Map" },
          { href: "/compare-providers", label: "Compare Providers" },
        ]
      : []),
    { href: "#faqs", label: "FAQs" },
    ...(showUnavailableFooterLinks ? [{ href: "/help-and-support", label: "Help & Support" }] : []),
  ],
};

const aboutLinks: FooterSection = {
  title: "ABOUT US",
  links: [
    { href: "/about-us", label: "About Us" },
    { href: "#mission", label: "Our Mission" },
    ...(showUnavailableFooterLinks ? [{ href: "/news-and-updates", label: "News & Updates" }] : []),
  ],
};

const connectLinks: FooterSection = {
  title: "STAY CONNECTED",
  links: [
    { href: "#contact", label: "Contact Us" },
    ...(showUnavailableFooterLinks ? [{ href: "/feedback-and-suggestions", label: "Feedback & Suggestions" }] : []),
    { href: "#", label: "Follow Us" },
  ],
};

const socialLinks: SocialLink[] = [
  { href: "#", icon: "fab fa-facebook-f", label: "Facebook" },
  { href: "#", icon: "fab fa-instagram", label: "Instagram" },
  { href: "#", icon: "fab fa-slack", label: "Slack" },
  { href: "#", icon: "fab fa-twitter", label: "Twitter" },
];

const Footer = () => {
  const currentYear = new Date().getFullYear();

  const renderLinkColumn = (section: FooterSection) => (
    <div className="flex flex-col gap-3">
      <h3 className="text-white text-sm font-semibold mb-4">{section.title}</h3>
      {section.links.map((link, index) => (
        <Link
          key={index}
          href={link.href}
          scroll={true}
          className="text-[#b0b7c3] text-sm hover:text-white transition-colors">
          {link.label}
        </Link>
      ))}
    </div>
  );

  return (
    <footer className="w-full text-white py-16 px-6 md:px-16 relative bg-gradient-to-b from-[#0C43D1] to-[#DE0E23]">
      <div className={cn("relative container mx-auto max-w-[85rem] px-4")}>
        {/* Logo & Title */}
        <div className="flex items-center gap-4 mb-12">
          <Logo width={90} height={50} />
          <h2 className="text-2xl font-medium">Quality of Service</h2>
        </div>

        {/* Footer Links */}
        <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-4 gap-10 mb-16">
          {renderLinkColumn(quickLinks)}
          {renderLinkColumn(aboutLinks)}
          {renderLinkColumn(connectLinks)}

          {showFooterContactDetails && (
            <div className="flex flex-col gap-3">
              <h3 className="text-white text-sm font-semibold mb-4">EMAIL US</h3>
              <a href="mailto:alex.maureal@ustp.edu.ph" className="text-[#b0b7c3] text-sm hover:text-white">
                alex.maureal@ustp.edu.ph
              </a>
            </div>
          )}
        </div>

        {/* Footer Bottom */}
        <div className="flex flex-col md:flex-row justify-between items-center pt-8 border-t border-white/20 gap-6">
          <p className="text-sm">© {currentYear} All rights reserved.</p>
          <div className="flex gap-6">
            {socialLinks.map((social, index) => (
              <Link
                key={index}
                href={social.href}
                aria-label={social.label}
                className="text-white text-xl hover:opacity-80 transition-opacity">
                <i className={social.icon}></i>
              </Link>
            ))}
          </div>
        </div>
      </div>
    </footer>
  );
};

export default Footer;
