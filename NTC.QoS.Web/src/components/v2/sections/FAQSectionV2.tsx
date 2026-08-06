"use client";

import { useEffect, useRef, useState } from "react";
import { ChevronDown } from "lucide-react";

const faqs = [
  {
    question: "What is the NTC QoS platform?",
    answer:
      "The NTC QoS platform is the official Philippine National Telecommunications Commission broadband mapping and quality of service monitoring system. It provides transparent access to internet coverage data, speed test results, and infrastructure information across the Philippines.",
  },
  {
    question: "How often is the data updated?",
    answer:
      "Our platform receives daily updates from various sources including ISP reports, user-submitted speed tests, and official NTC monitoring systems. Coverage maps and infrastructure data are updated in real-time as new information becomes available.",
  },
  {
    question: "Is this service free to use?",
    answer:
      "Yes, the public-facing features of the NTC QoS platform are completely free to access. No registration is required to view coverage maps, speed test results, and basic analytics. Advanced administrative features are available to authorized NTC personnel.",
  },
  {
    question: "How accurate is the coverage data?",
    answer:
      "Our coverage data is compiled from official ISP submissions, verified cell site locations, and crowdsourced speed test results. We employ validation algorithms and cross-reference multiple data sources to ensure high accuracy. However, actual coverage may vary based on terrain, building structures, and network conditions.",
  },
  {
    question: "Can I contribute my speed test results?",
    answer:
      "Yes! Users can contribute to our database by running speed tests through our platform. Your anonymized results help create a more comprehensive picture of actual internet performance across the Philippines.",
  },
  {
    question: "Who manages this platform?",
    answer:
      "This platform is developed and maintained by the National Telecommunications Commission (NTC) Philippines in collaboration with authorized technology partners. All data is managed in accordance with NTC regulations and data privacy standards.",
  },
];

export default function FAQSectionV2() {
  const [openIndex, setOpenIndex] = useState<number | null>(0);
  const [visibleIndexes, setVisibleIndexes] = useState<number[]>([]);
  const sectionRef = useRef<HTMLElement>(null);

  useEffect(() => {
    const observer = new IntersectionObserver(
      (entries) => {
        entries.forEach((entry) => {
          if (entry.isIntersecting) {
            faqs.forEach((_, index) => {
              setTimeout(() => {
                setVisibleIndexes((prev) => [...prev, index]);
              }, index * 100);
            });
          }
        });
      },
      { threshold: 0.1 },
    );

    if (sectionRef.current) {
      observer.observe(sectionRef.current);
    }

    return () => observer.disconnect();
  }, []);

  return (
    <section ref={sectionRef} className="py-20 lg:py-32 bg-white relative overflow-hidden">
      {/* Background decoration */}
      <div className="absolute top-1/2 left-0 w-96 h-96 bg-red-100 rounded-full blur-3xl opacity-30 -z-10 transform -translate-y-1/2" />

      <div className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8">
        {/* Section header */}
        <div className="text-center mb-16">
          <h2 className="text-3xl sm:text-4xl lg:text-5xl font-bold text-gray-900 mb-6">
            Frequently Asked{" "}
            <span className="bg-gradient-to-r from-red-600 to-yellow-600 bg-clip-text text-transparent">Questions</span>
          </h2>
          <p className="text-lg text-gray-600">Everything you need to know about the NTC QoS platform</p>
        </div>

        {/* FAQ Accordion */}
        <div className="space-y-4">
          {faqs.map((faq, index) => {
            const isOpen = openIndex === index;
            const isVisible = visibleIndexes.includes(index);

            return (
              <div
                key={index}
                className={`transform transition-all duration-500 ${
                  isVisible ? "translate-x-0 opacity-100" : "-translate-x-10 opacity-0"
                }`}>
                <button
                  onClick={() => setOpenIndex(isOpen ? null : index)}
                  className="w-full text-left bg-white border-2 border-gray-100 rounded-xl p-6 hover:border-red-200 hover:shadow-md transition-all duration-300"
                  aria-expanded={isOpen}
                  aria-controls={`faq-answer-${index}`}>
                  <div className="flex items-center justify-between gap-4">
                    <h3 className="text-lg font-semibold text-gray-900 pr-4">{faq.question}</h3>
                    <ChevronDown
                      className={`w-5 h-5 text-gray-500 flex-shrink-0 transition-transform duration-300 ${
                        isOpen ? "transform rotate-180" : ""
                      }`}
                    />
                  </div>

                  <div
                    id={`faq-answer-${index}`}
                    className={`grid transition-all duration-300 ease-in-out ${
                      isOpen ? "grid-rows-[1fr] mt-4" : "grid-rows-[0fr]"
                    }`}>
                    <div className="overflow-hidden">
                      <p className="text-gray-600 leading-relaxed">{faq.answer}</p>
                    </div>
                  </div>
                </button>
              </div>
            );
          })}
        </div>

        {/* Contact CTA */}
        <div className="mt-16 text-center p-8 bg-gradient-to-br from-red-50 to-yellow-50 rounded-2xl border border-red-100">
          <h3 className="text-xl font-semibold text-gray-900 mb-2">Still have questions?</h3>
          <p className="text-gray-600 mb-6">Contact our support team for assistance</p>
          <button className="inline-flex items-center px-6 py-3 bg-gradient-to-r from-red-600 to-red-700 text-white rounded-lg font-semibold hover:shadow-lg transform hover:-translate-y-0.5 transition-all duration-200">
            Contact Support
          </button>
        </div>
      </div>
    </section>
  );
}
