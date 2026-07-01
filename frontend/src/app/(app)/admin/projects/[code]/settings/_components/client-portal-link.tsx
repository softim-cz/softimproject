"use client";

import { useState } from "react";
import { Check, Copy, ExternalLink } from "lucide-react";
import { toast } from "sonner";
import { useTranslations } from "next-intl";

export function ClientPortalLink({ token }: { token: string }) {
  const t = useTranslations("ProjectSettings");
  const [copied, setCopied] = useState(false);
  const url =
    typeof window !== "undefined"
      ? `${window.location.origin}/portal/${token}`
      : `/portal/${token}`;

  const handleCopy = async () => {
    try {
      await navigator.clipboard.writeText(url);
      setCopied(true);
      setTimeout(() => setCopied(false), 1500);
    } catch {
      toast.error(t("copyFailed"));
    }
  };

  return (
    <div className="flex items-center gap-2 rounded-lg border border-border bg-muted/30 px-3 py-2">
      <input
        type="text"
        value={url}
        readOnly
        className="flex-1 bg-transparent text-xs text-foreground font-mono focus:outline-none"
        onFocus={(e) => e.currentTarget.select()}
      />
      <button
        onClick={handleCopy}
        className="inline-flex items-center gap-1 text-xs text-muted-foreground hover:text-foreground transition-colors"
        title={t("copyLink")}
      >
        {copied ? (
          <Check className="h-3.5 w-3.5 text-green-600" />
        ) : (
          <Copy className="h-3.5 w-3.5" />
        )}
        {copied ? t("copied") : t("copy")}
      </button>
      <a
        href={url}
        target="_blank"
        rel="noopener noreferrer"
        className="inline-flex items-center gap-1 text-xs text-muted-foreground hover:text-foreground transition-colors"
        title={t("openPortal")}
      >
        <ExternalLink className="h-3.5 w-3.5" />
        {t("openLink")}
      </a>
    </div>
  );
}
