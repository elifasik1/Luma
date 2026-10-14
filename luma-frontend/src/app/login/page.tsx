"use client";

import { Suspense } from "react";
import { AuthForm } from "@/components/auth/AuthForm";

export default function LoginPage() {
  return (
    <Suspense fallback={<main className="min-h-[70vh]" />}>
      <AuthForm mode="login" />
    </Suspense>
  );
}
