"use client";

/* eslint-disable react-hooks/set-state-in-effect */
import { useState, useEffect } from "react";
import { SlidersHorizontal } from "lucide-react";
import { toast } from "sonner";
import { useTranslations } from "next-intl";
import { useProjectCustomFieldValues, useSaveProjectCustomFieldValues } from "@/queries/projects";
import { Skeleton } from "@/components/shared/loading-skeleton";
import type { ProjectCustomFieldValue } from "@/types";

export function CustomFieldsSection({ projectId }: { projectId: string }) {
  const t = useTranslations("ProjectSettings");
  const { data: fields, isLoading } = useProjectCustomFieldValues(projectId);
  const saveMutation = useSaveProjectCustomFieldValues();
  const [values, setValues] = useState<Record<string, string>>({});
  const [dirty, setDirty] = useState(false);

  useEffect(() => {
    if (fields) {
      const initial: Record<string, string> = {};
      fields.forEach((f: ProjectCustomFieldValue) => {
        if (f.value) initial[f.customFieldDefinitionId] = f.value;
      });
      setValues(initial);
      setDirty(false);
    }
  }, [fields]);

  if (isLoading) {
    return (
      <section className="rounded-lg border border-border bg-card p-6 space-y-4">
        <Skeleton className="h-6 w-32" />
        <Skeleton className="h-32 w-full" />
      </section>
    );
  }

  if (!fields || fields.length === 0) return null;

  const handleChange = (defId: string, value: string) => {
    setValues((prev) => ({ ...prev, [defId]: value }));
    setDirty(true);
  };

  const handleSave = async () => {
    try {
      await saveMutation.mutateAsync({
        projectId,
        values: fields.map((f: ProjectCustomFieldValue) => ({
          customFieldDefinitionId: f.customFieldDefinitionId,
          value: values[f.customFieldDefinitionId] || undefined,
        })),
      });
      toast.success(t("customFieldsSaved"));
      setDirty(false);
    } catch {
      toast.error(t("customFieldsSaveFailed"));
    }
  };

  const renderInput = (field: ProjectCustomFieldValue) => {
    const val = values[field.customFieldDefinitionId] || "";
    const inputClass =
      "w-full rounded-lg border border-input bg-background px-3 py-2 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring";

    switch (field.fieldType) {
      case "Number":
        return (
          <input
            type="number"
            value={val}
            onChange={(e) => handleChange(field.customFieldDefinitionId, e.target.value)}
            className={inputClass}
          />
        );
      case "Date":
        return (
          <input
            type="date"
            value={val}
            onChange={(e) => handleChange(field.customFieldDefinitionId, e.target.value)}
            className={inputClass}
          />
        );
      case "Select": {
        let opts: string[] = [];
        try {
          opts = JSON.parse(field.options || "[]");
        } catch {
          /* ignore */
        }
        return (
          <select
            value={val}
            onChange={(e) => handleChange(field.customFieldDefinitionId, e.target.value)}
            className={inputClass}
          >
            <option value="">{t("selectPlaceholder")}</option>
            {opts.map((o) => (
              <option key={o} value={o}>
                {o}
              </option>
            ))}
          </select>
        );
      }
      default:
        return (
          <input
            type="text"
            value={val}
            onChange={(e) => handleChange(field.customFieldDefinitionId, e.target.value)}
            className={inputClass}
          />
        );
    }
  };

  return (
    <section className="rounded-lg border border-border bg-card p-6 space-y-4">
      <div className="flex items-center gap-2 mb-2">
        <SlidersHorizontal className="h-5 w-5 text-muted-foreground" />
        <h2 className="text-lg font-semibold text-card-foreground">{t("customFields")}</h2>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        {fields.map((field: ProjectCustomFieldValue) => (
          <div key={field.customFieldDefinitionId}>
            <label className="block text-sm font-medium text-card-foreground">
              {field.fieldName}
              {field.isRequired && <span className="text-destructive ml-1">*</span>}
            </label>
            {field.description && (
              <p className="text-xs text-muted-foreground mb-1">{field.description}</p>
            )}
            {renderInput(field)}
          </div>
        ))}
      </div>

      <div className="flex justify-end pt-2">
        <button
          onClick={handleSave}
          disabled={!dirty || saveMutation.isPending}
          className="px-4 py-2 rounded-lg bg-primary text-primary-foreground text-sm font-medium hover:opacity-90 transition-opacity disabled:opacity-50"
        >
          {saveMutation.isPending ? t("saving") : t("saveCustomFields")}
        </button>
      </div>
    </section>
  );
}
