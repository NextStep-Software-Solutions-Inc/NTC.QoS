"use client";

import Link from "next/link";
import { ChevronRight, Facebook, Mail, MapPin, Phone, Twitter, Youtube } from "lucide-react";

const showUnavailableFooterLinks = process.env.NEXT_PUBLIC_ENABLE_UNAVAILABLE_FOOTER_LINKS === "true";
const showFooterContactDetails = process.env.NEXT_PUBLIC_ENABLE_FOOTER_CONTACT_DETAILS === "true";
const showFooterSocialLinks = process.env.NEXT_PUBLIC_ENABLE_FOOTER_SOCIAL_LINKS === "true";

const footerLinks = {
  quickLinks: [
    { label: "Home", href: "/" },
    ...(showUnavailableFooterLinks ? [{ label: "Coverage Map", href: "/public/map" }] : []),
    { label: "About Us", href: "/about-us" },
    ...(showUnavailableFooterLinks ? [{ label: "News & Updates", href: "/news-and-updates" }] : []),
  ],
  resources: [
    { label: "Help & Support", href: "/help-and-support" },
    { label: "Compare Providers", href: "/compare-providers" },
    { label: "Feedback", href: "/feedback-and-suggestions" },
    { label: "Broadband Info", href: "/broadband" },
  ],
  legal: [
    { label: "Privacy Policy", href: "#" },
    { label: "Terms of Service", href: "#" },
    { label: "Data Protection", href: "#" },
    { label: "Accessibility", href: "#" },
  ],
};

const socialLinks = [
  { icon: Facebook, href: "https://facebook.com/ntcphilippines", label: "Facebook" },
  { icon: Twitter, href: "https://twitter.com/ntcphilippines", label: "Twitter" },
  { icon: Youtube, href: "https://youtube.com/ntcphilippines", label: "YouTube" },
];

export default function FooterV2() {
  const currentYear = new Date().getFullYear();

  return (
    <footer className="bg-gradient-to-br from-gray-900 via-red-900 to-gray-900 text-white relative overflow-hidden">
      {/* Background pattern */}
      <div className="absolute inset-0 opacity-5">
        <div
          className="absolute inset-0"
          style={{
            backgroundImage: `url("data:image/svg+xml,%3Csvg width='60' height='60' viewBox='0 0 60 60' xmlns='http://www.w3.org/2000/svg'%3E%3Cg fill='none' fill-rule='evenodd'%3E%3Cg fill='%23ffffff' fill-opacity='1'%3E%3Cpath d='M36 34v-4h-2v4h-4v2h4v4h2v-4h4v-2h-4zm0-30V0h-2v4h-4v2h4v4h2V6h4V4h-4zM6 34v-4H4v4H0v2h4v4h2v-4h4v-2H6zM6 4V0H4v4H0v2h4v4h2V6h4V4H6z'/%3E%3C/g%3E%3C/g%3E%3C/svg%3E")`,
          }}
        />
      </div>

      {/* Gradient overlay */}
      <div className="absolute top-0 left-0 right-0 h-px bg-gradient-to-r from-transparent via-red-500 to-transparent" />

      <div className="relative z-10 max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-12 lg:py-16">
        {/* Main footer content */}
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-5 gap-8 lg:gap-12 mb-12">
          {/* Brand section */}
          <div className="lg:col-span-2 space-y-6">
            <div>
              <h3 className="text-2xl font-bold mb-2 bg-gradient-to-r from-red-400 to-yellow-400 bg-clip-text text-transparent">
                NTC QoS Platform
              </h3>
              <p className="text-gray-300 text-sm leading-relaxed">
                Official Philippine National Telecommunications Commission broadband mapping platform. Providing
                transparent access to internet coverage data and service quality metrics.
              </p>
            </div>

            {/* Contact info */}
            <div className="space-y-3">
              {showFooterContactDetails && (
                <>
                  <a
                    href="mailto:alex.maureal@ustp.edu.ph"
                    className="flex items-center gap-3 text-gray-300 hover:text-red-400 transition-colors group">
                    <div className="bg-red-500/10 p-2 rounded-lg group-hover:bg-red-500/20 transition-colors">
                      <Mail className="w-4 h-4" />
                    </div>
                    <span className="text-sm">alex.maureal@ustp.edu.ph</span>
                  </a>

                  <a
                    href="tel:+632-example"
                    className="flex items-center gap-3 text-gray-300 hover:text-yellow-400 transition-colors group">
                    <div className="bg-yellow-500/10 p-2 rounded-lg group-hover:bg-yellow-500/20 transition-colors">
                      <Phone className="w-4 h-4" />
                    </div>
                    <span className="text-sm">+63 917 7146 878</span>
                  </a>
                </>
              )}

              <div className="flex items-start gap-3 text-gray-300">
                <div className="bg-red-500/10 p-2 rounded-lg">
                  <MapPin className="w-4 h-4" />
                </div>
                <span className="text-sm">
                  National Telecommunications Commission
                  <br />
                  Philippines
                </span>
              </div>
            </div>

            {showFooterSocialLinks && (
              <div className="flex gap-3">
                {socialLinks.map((social) => {
                  const Icon = social.icon;
                  return (
                    <a
                      key={social.label}
                      href={social.href}
                      target="_blank"
                      rel="noopener noreferrer"
                      aria-label={social.label}
                      className="bg-white/5 hover:bg-red-500/20 p-3 rounded-lg transition-all duration-300 hover:scale-110">
                      <Icon className="w-5 h-5" />
                    </a>
                  );
                })}
              </div>
            )}
          </div>

          {/* Quick Links */}
          <div>
            <h4 className="text-lg font-semibold mb-4 text-white">Quick Links</h4>
            <ul className="space-y-3">
              {footerLinks.quickLinks.map((link) => (
                <li key={link.href}>
                  <Link
                    href={link.href}
                    className="flex items-center gap-2 text-gray-300 hover:text-red-400 transition-colors group text-sm">
                    <ChevronRight className="w-4 h-4 group-hover:translate-x-1 transition-transform" />
                    {link.label}
                  </Link>
                </li>
              ))}
            </ul>
          </div>

          {showUnavailableFooterLinks && (
            <>
              <div>
                <h4 className="text-lg font-semibold mb-4 text-white">Resources</h4>
                <ul className="space-y-3">
                  {footerLinks.resources.map((link) => (
                    <li key={link.href}>
                      <Link
                        href={link.href}
                        className="flex items-center gap-2 text-gray-300 hover:text-yellow-400 transition-colors group text-sm">
                        <ChevronRight className="w-4 h-4 group-hover:translate-x-1 transition-transform" />
                        {link.label}
                      </Link>
                    </li>
                  ))}
                </ul>
              </div>

              <div>
                <h4 className="text-lg font-semibold mb-4 text-white">Legal</h4>
                <ul className="space-y-3">
                  {footerLinks.legal.map((link, idx) => (
                    <li key={idx}>
                      <Link
                        href={link.href}
                        className="flex items-center gap-2 text-gray-300 hover:text-yellow-400 transition-colors group text-sm">
                        <ChevronRight className="w-4 h-4 group-hover:translate-x-1 transition-transform" />
                        {link.label}
                      </Link>
                    </li>
                  ))}
                </ul>
              </div>
            </>
          )}
        </div>

        {/* Bottom bar */}
        <div className="pt-8 border-t border-white/10">
          <div className="flex flex-col md:flex-row justify-between items-center gap-4">
            <p className="text-gray-400 text-sm text-center md:text-left">
              © {currentYear} National Telecommunications Commission Philippines. All rights reserved.
            </p>

            <div className="flex items-center gap-6 text-sm">
              <span className="text-gray-400">Made with</span>
              <span className="text-red-400 animate-pulse">❤</span>
              <span className="text-gray-400">for Filipinos</span>
            </div>
          </div>
        </div>
      </div>

      {/* Decorative elements */}
      <div className="absolute bottom-0 right-0 w-64 h-64 bg-red-500/5 rounded-full blur-3xl" />
      <div className="absolute top-0 left-0 w-64 h-64 bg-yellow-500/5 rounded-full blur-3xl" />
    </footer>
  );
}
