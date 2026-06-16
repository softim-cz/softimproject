"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { toast } from "sonner";
import { KeyRound, Copy, Check, Trash2, Plus } from "lucide-react";
import { useApiKeys, useGenerateApiKey, useRevokeApiKey } from "@/queries/api-keys";
import { Skeleton } from "@/components/shared/loading-skeleton";
import type { ApiKey } from "@/types";

export default function ApiKeysPage() {
  const t = useTranslations("ApiKeys");
  const { data: keys, isLoading } = useApiKeys();
  const generate = useGenerateApiKey();
  const revoke = useRevokeApiKey();

  const [name, setName] = useState("");
  const [expiry, setExpiry] = useState<string>("never");
  const [plaintext, setPlaintext] = useState<string | null>(null);
  const [copied, setCopied] = useState(false);

  const handleGenerate = async () => {
    if (!name.trim()) return;
    try {
      const expiresInDays = expiry === "never" ? undefined : parseInt(expiry, 10);
      const result = await generate.mutateAsync({ name: name.trim(), expiresInDays });
      setPlaintext(result.plaintextKey);
      setName("");
      setExpiry("never");
      toast.success(t("created"));
    } catch {
      toast.error(t("createFailed"));
    }
  };

  const handleCopy = async () => {
    if (!plaintext) return;
    try {
      await navigator.clipboard.writeText(plaintext);
      setCopied(true);
      setTimeout(() => setCopied(false), 1500);
    } catch {
      toast.error(t("copyFailed"));
    }
  };

  const handleRevoke = async (key: ApiKey) => {
    if (!window.confirm(t("revokeConfirm", { name: key.name }))) return;
    try {
      await revoke.mutateAsync(key.id);
      toast.success(t("revoked"));
    } catch {
      toast.error(t("revokeFailed"));
    }
  };

  const status = (k: ApiKey) => {
    if (k.revokedAt) return { label: t("statusRevoked"), cls: "text-muted-foreground" };
    if (k.expiresAt && new Date(k.expiresAt) < new Date())
      return { label: t("statusExpired"), cls: "text-destructive" };
    return { label: t("statusActive"), cls: "text-green-600" };
  };

  const inputClass =
    "rounded-lg border border-input bg-background px-3 py-2 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring";

  return (
    <div className="max-w-3xl space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-foreground flex items-center gap-2">
          <KeyRound className="h-6 w-6 text-muted-foreground" />
          {t("title")}
        </h1>
        <p className="text-sm text-muted-foreground mt-1">{t("subtitle")}</p>
      </div>

      {/* Newly generated key — shown once */}
      {plaintext && (
        <div className="rounded-lg border border-green-300 bg-green-50 p-4 space-y-2">
          <p className="text-sm font-medium text-green-900">{t("createdOnceWarning")}</p>
          <div className="flex items-center gap-2">
            <code className="flex-1 rounded bg-card border border-green-200 dark:border-green-800 px-3 py-2 text-sm font-mono break-all">
              {plaintext}
            </code>
            <button
              onClick={handleCopy}
              className="inline-flex items-center gap-1 px-3 py-2 rounded-lg bg-green-600 text-white text-sm font-medium hover:opacity-90"
            >
              {copied ? <Check className="h-4 w-4" /> : <Copy className="h-4 w-4" />}
              {copied ? t("copied") : t("copy")}
            </button>
          </div>
          <button
            onClick={() => setPlaintext(null)}
            className="text-xs text-green-800 underline hover:no-underline"
          >
            {t("dismiss")}
          </button>
        </div>
      )}

      {/* Generate form */}
      <div className="rounded-lg border border-border bg-card p-4 space-y-3">
        <h2 className="text-sm font-semibold text-card-foreground">{t("newKey")}</h2>
        <div className="flex flex-wrap items-end gap-3">
          <div className="flex-1 min-w-[200px]">
            <label className="block text-xs font-medium text-muted-foreground mb-1">
              {t("nameLabel")}
            </label>
            <input
              value={name}
              onChange={(e) => setName(e.target.value)}
              placeholder={t("namePlaceholder")}
              className={`${inputClass} w-full`}
            />
          </div>
          <div>
            <label className="block text-xs font-medium text-muted-foreground mb-1">
              {t("expiryLabel")}
            </label>
            <select
              value={expiry}
              onChange={(e) => setExpiry(e.target.value)}
              className={inputClass}
            >
              <option value="never">{t("expiryNever")}</option>
              <option value="30">{t("expiryDays", { days: 30 })}</option>
              <option value="90">{t("expiryDays", { days: 90 })}</option>
              <option value="365">{t("expiryDays", { days: 365 })}</option>
            </select>
          </div>
          <button
            onClick={handleGenerate}
            disabled={!name.trim() || generate.isPending}
            className="inline-flex items-center gap-1.5 px-4 py-2 rounded-lg bg-primary text-primary-foreground text-sm font-medium hover:opacity-90 transition-opacity disabled:opacity-50"
          >
            <Plus className="h-4 w-4" />
            {generate.isPending ? t("creating") : t("generate")}
          </button>
        </div>
        <p className="text-xs text-muted-foreground">{t("usageHint")}</p>
      </div>

      {/* Keys list */}
      {isLoading ? (
        <Skeleton className="h-24 w-full" />
      ) : keys && keys.length > 0 ? (
        <div className="rounded-lg border border-border overflow-hidden">
          <table className="w-full">
            <thead>
              <tr className="bg-muted/50 text-left text-xs font-medium text-muted-foreground uppercase">
                <th className="px-4 py-2">{t("colName")}</th>
                <th className="px-4 py-2">{t("colPrefix")}</th>
                <th className="px-4 py-2">{t("colStatus")}</th>
                <th className="px-4 py-2">{t("colLastUsed")}</th>
                <th className="px-4 py-2">{t("colExpires")}</th>
                <th className="px-4 py-2"></th>
              </tr>
            </thead>
            <tbody className="divide-y divide-border">
              {keys.map((k) => {
                const s = status(k);
                return (
                  <tr key={k.id} className="text-sm">
                    <td className="px-4 py-2 text-foreground">{k.name}</td>
                    <td className="px-4 py-2 font-mono text-muted-foreground">{k.prefix}</td>
                    <td className={`px-4 py-2 font-medium ${s.cls}`}>{s.label}</td>
                    <td className="px-4 py-2 text-muted-foreground">
                      {k.lastUsedAt ? new Date(k.lastUsedAt).toLocaleString() : "—"}
                    </td>
                    <td className="px-4 py-2 text-muted-foreground">
                      {k.expiresAt ? new Date(k.expiresAt).toLocaleDateString() : t("expiryNever")}
                    </td>
                    <td className="px-4 py-2 text-right">
                      {!k.revokedAt && (
                        <button
                          onClick={() => handleRevoke(k)}
                          disabled={revoke.isPending}
                          className="p-1 text-muted-foreground hover:text-destructive rounded disabled:opacity-50"
                          title={t("revoke")}
                          aria-label={t("revoke")}
                        >
                          <Trash2 className="h-4 w-4" />
                        </button>
                      )}
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      ) : (
        <p className="text-sm text-muted-foreground">{t("empty")}</p>
      )}
    </div>
  );
}
