import type { Config } from "tailwindcss";
import typography from "@tailwindcss/typography";

const config: Config = {
  content: [
    "./src/pages/**/*.{js,ts,jsx,tsx,mdx}",
    "./src/components/**/*.{js,ts,jsx,tsx,mdx}",
    "./src/app/**/*.{js,ts,jsx,tsx,mdx}",
  ],
  theme: {
    extend: {
      colors: {
        // Softim Brand Colors
        "primary-navy": {
          DEFAULT: "#1a2744",
          light: "#2a3d66",
          dark: "#111a2e",
          50: "#f0f3f8",
          100: "#d9e0ec",
          200: "#b3c1d9",
          300: "#8da2c6",
          400: "#5577a3",
          500: "#2a3d66",
          600: "#1a2744",
          700: "#152036",
          800: "#111a2e",
          900: "#0b1120",
        },
        "accent-orange": {
          DEFAULT: "#e85d26",
          light: "#f07a4a",
          dark: "#c94d1e",
          50: "#fef3ee",
          100: "#fde3d4",
          200: "#fac4a8",
          300: "#f59d72",
          400: "#f07a4a",
          500: "#e85d26",
          600: "#d4491a",
          700: "#c94d1e",
          800: "#8e3416",
          900: "#6b2812",
        },
        // Semantic colors (CSS variable based for theme switching)
        border: "var(--border)",
        input: "var(--input)",
        ring: "var(--ring)",
        background: "var(--background)",
        foreground: "var(--foreground)",
        primary: {
          DEFAULT: "var(--primary)",
          foreground: "var(--primary-foreground)",
        },
        secondary: {
          DEFAULT: "var(--secondary)",
          foreground: "var(--secondary-foreground)",
        },
        destructive: {
          DEFAULT: "var(--destructive)",
          foreground: "var(--destructive-foreground)",
        },
        success: {
          DEFAULT: "var(--success)",
          foreground: "var(--success-foreground)",
        },
        warning: {
          DEFAULT: "var(--warning)",
          foreground: "var(--warning-foreground)",
        },
        muted: {
          DEFAULT: "var(--muted)",
          foreground: "var(--muted-foreground)",
        },
        accent: {
          DEFAULT: "var(--accent)",
          foreground: "var(--accent-foreground)",
        },
        popover: {
          DEFAULT: "var(--popover)",
          foreground: "var(--popover-foreground)",
        },
        card: {
          DEFAULT: "var(--card)",
          foreground: "var(--card-foreground)",
        },
      },
      borderRadius: {
        lg: "var(--radius-lg)",
        md: "var(--radius-md)",
        sm: "var(--radius-sm)",
      },
    },
  },
  plugins: [typography],
};

export default config;
