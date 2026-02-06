"use client";

import { useAuth } from "@/lib/auth/use-auth";
import { useEffect } from "react";

export default function LogoutPage() {
  const { logout } = useAuth();

  useEffect(() => {
    logout();
  }, [logout]);

  return (
    <div className="text-center text-white">
      <p className="text-lg">Signing out...</p>
    </div>
  );
}
