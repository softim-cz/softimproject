"use client";

import Link from "next/link";
import { useTranslations } from "next-intl";

const REPO_URL = "https://github.com/softim-cz/softimproject";
const version = process.env.NEXT_PUBLIC_APP_VERSION ?? "0.0.0";
const commit = process.env.NEXT_PUBLIC_GIT_SHA ?? "dev";
const shortCommit = commit.length > 7 ? commit.slice(0, 7) : commit;

/**
 * App footer (mirrors the Money ERP footer): repo · version → release notes · commit,
 * with the company copyright on the right. Version comes from package.json (baked at build),
 * the commit SHA from the deploy workflow (NEXT_PUBLIC_GIT_SHA).
 */
export function Footer() {
  const t = useTranslations("Footer");
  const year = new Date().getFullYear();

  return (
    <footer className="flex flex-none items-center justify-between gap-3 border-t border-border bg-card px-6 py-2 text-[11px] text-muted-foreground">
      <span className="flex items-center gap-1.5">
        <a
          href={REPO_URL}
          target="_blank"
          rel="noopener noreferrer"
          className="hover:text-foreground transition-colors"
        >
          ProjectMan
        </a>
        <span className="text-border">·</span>
        <Link
          href="/releases"
          title={t("whatsNew")}
          className="hover:text-foreground transition-colors"
        >
          v{version}
        </Link>
        {commit !== "dev" && (
          <>
            <span className="text-border">·</span>
            <a
              href={`${REPO_URL}/commit/${commit}`}
              target="_blank"
              rel="noopener noreferrer"
              className="font-mono hover:text-foreground transition-colors"
            >
              {shortCommit}
            </a>
          </>
        )}
      </span>
      <span>
        © {year}{" "}
        <a
          href="https://softim.cz"
          target="_blank"
          rel="noopener noreferrer"
          className="hover:text-foreground transition-colors"
        >
          SOFTIM.CZ spol. s r. o.
        </a>
      </span>
    </footer>
  );
}
