import React from "react";

import { Accordion, AccordionContent, AccordionItem, AccordionTrigger } from "@/components/ui/accordion";

const faqs = [
  {
    question: "What is Quality of Service (QOS)?",
    answer:
      "QoS is a broadband information tool developed by the National Telecommunications Commission (NTC). It helps you check providers, compare speeds, and see Ultra-Fast Broadband (UFB) availability in your area.",
  },
  {
    question: "How it works?",
    answer:
      "The NTC-QoS Broadband Map allows users to explore internet coverage and signal quality across different areas. Simply search for a location or navigate the map, then view available providers and their performance. Filters and charts help compare signal strength, tower distribution, and coverage, making it easier to identify the top-performing provider in your area.",
  },
  {
    question: "Why should I check broadband options before moving?",
    answer:
      "Internet access differs per location. Checking ahead ensures you will have reliable and fast internet where you plan to live.",
  },
  {
    question: "Is this service free?",
    answer:
      "Yes. The QoS broadband map is a free public service by the NTC to help consumers make better internet choices.",
  },
];

const FAQSection = () => {
  return (
    <section
      id="faqs"
      className="py-16 md:py-20 flex items-center justify-center lg:p-12"
      style={{
        background: `
          linear-gradient(to right, rgba(0,0,0,0.5) 0%, transparent 30%),
          linear-gradient(to right, #0C43D1 0%, #DE0E23 100%)
        `,
      }}>
      <div className="container flex items-center flex-col mx-auto gap-10 lg:gap-0 max-w-7xl w-full flex-wrap justify-between px-7">
        <div className="mb-2 text-center">
          <span className="text-xs uppercase tracking-wider text-white/80 md:text-sm">FAQS</span>
        </div>
        <h2 className="mb-8 text-center text-3xl font-bold text-white md:mb-12 md:text-4xl">
          Frequently Asked Questions
        </h2>

        <Accordion type="multiple" defaultValue={["item-0"]} className="w-full space-y-4 md:space-y-6">
          {faqs.map((faq, index) => (
            <AccordionItem key={index} value={`item-${index}`} className="border-b border-white/10 [&_button]:border-0">
              <AccordionTrigger className="py-4 px-6 md:px-8 text-left text-base text-white hover:no-underline md:text-lg [&>svg]:text-white [&>svg]:transition-transform [&[data-state=open]>svg]:rotate-180">
                {faq.question}
              </AccordionTrigger>
              <AccordionContent className="py-4 md:px-8 hover:no-underline pb-4 text-sm text-white/70 md:text-base text-left px-6">
                {faq.answer}
              </AccordionContent>
            </AccordionItem>
          ))}
        </Accordion>

        {/* <p className="mt-8 text-center text-sm text-white/80 md:mt-12 md:text-base">
          {`Can't find the answer you're looking for? `} Reach out to our{" "}
          <span className="font-medium text-white">customer support team</span>
        </p> */}
      </div>
    </section>
  );
};

export default FAQSection;
