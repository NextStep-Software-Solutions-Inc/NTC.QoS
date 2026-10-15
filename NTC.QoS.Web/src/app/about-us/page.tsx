"use client";

import React from "react";
import dynamic from "next/dynamic";

const ComingSoon = dynamic(() => import("@/components/ComingSoon"), {
    ssr: false,
});

export default function AboutUs() {
    return <ComingSoon />;
}
