"use client";

import { useAuth } from "@/lib/auth/use-auth";
import { useRouter } from "next/navigation";
import { useEffect } from "react";

export default function LoginPage() {
  const { isAuthenticated, login } = useAuth();
  const router = useRouter();

  useEffect(() => {
    if (isAuthenticated) {
      router.push("/dashboard");
    }
  }, [isAuthenticated, router]);

  return (
    <div className="w-full max-w-md mx-auto">
      <div className="bg-card rounded-2xl shadow-2xl p-8 space-y-8">
        {/* Logo */}
        <div className="text-center">
          <div className="inline-flex items-center justify-center h-16 w-16 rounded-2xl bg-accent-orange mb-4">
            <span className="text-2xl font-bold tracking-tight text-white">PM</span>
          </div>
          <h1 className="text-2xl font-bold text-card-foreground">ProjectMan</h1>
          <p className="text-muted-foreground text-sm mt-1">Internal project management tool</p>
        </div>

        {/* Login button */}
        <button
          onClick={() => login()}
          className="w-full flex items-center justify-center gap-3 px-6 py-3 rounded-lg bg-primary-navy text-white font-medium hover:bg-primary-navy-light transition-colors"
        >
          <svg
            className="h-5 w-5"
            viewBox="0 0 21 21"
            fill="none"
            xmlns="http://www.w3.org/2000/svg"
          >
            <rect x="1" y="1" width="9" height="9" fill="#F25022" />
            <rect x="11" y="1" width="9" height="9" fill="#7FBA00" />
            <rect x="1" y="11" width="9" height="9" fill="#00A4EF" />
            <rect x="11" y="11" width="9" height="9" fill="#FFB900" />
          </svg>
          Sign in with Microsoft
        </button>

        <p className="text-center text-xs text-muted-foreground">
          Use your organization account to sign in.
        </p>
      </div>

      {/* Footer */}
      <p className="text-center text-xs text-white/40 mt-8">
        &copy; {new Date().getFullYear()} Softim.cz s.r.o. All rights reserved.
      </p>
    </div>
  );
}
