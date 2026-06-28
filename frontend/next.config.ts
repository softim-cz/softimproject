import type { NextConfig } from "next";
import createNextIntlPlugin from "next-intl/plugin";
import pkg from "./package.json";

const withNextIntl = createNextIntlPlugin("./src/i18n/request.ts");

const nextConfig: NextConfig = {
  output: "standalone",
  // App version (footer) comes from package.json so it's always correct without extra CI
  // wiring. The git commit SHA is injected by the deploy workflow via NEXT_PUBLIC_GIT_SHA.
  env: {
    NEXT_PUBLIC_APP_VERSION: pkg.version,
  },
};

export default withNextIntl(nextConfig);
