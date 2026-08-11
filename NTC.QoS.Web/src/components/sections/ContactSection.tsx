import React from "react";

const ContactSection = () => {
  return (
    <section className="w-full border-t border-gray-900 px-6 md:px-16 py-24" id="contact">
      <div className="flex flex-col gap-5 px-4 md:px-7 py-6 max-w-7xl mx-auto">
        <div className="flex flex-col gap-3">
          <h2 className="text-xl font-normal text-gray-500">Need further assistance?</h2>
          <h1 className="text-5xl font-bold text-gray-900">Contact Us</h1>
        </div>
        <hr className="border-t border-gray-900" />
        <div className="flex flex-col gap-5 pl-4 md:pl-8">
          <p className="text-base text-gray-700">
            Email:
            <a href="mailto:alex.maureal@ustp.edu.ph" className="text-blue-600 hover:underline">
              alex.maureal@ustp.edu.ph
            </a>
          </p>
          <p className="text-base text-gray-700">
            Hotline: <span className="font-semibold">+63 917 7146 878</span>
          </p>
        </div>
      </div>
    </section>
  );
};

export default ContactSection;
