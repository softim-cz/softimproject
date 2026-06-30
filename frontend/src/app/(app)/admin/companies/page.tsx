"use client";

import { useTranslations } from "next-intl";
import { Building2 } from "lucide-react";
import { TableSkeleton } from "@/components/shared/loading-skeleton";
import { EmptyState } from "@/components/shared/empty-state";
import {
  useCompanies,
  useCreateCompany,
  useUpdateCompany,
  useDeleteCompany,
} from "@/queries/lookups";
import type { Company } from "@/types";
import { useInlineCrudState } from "@/hooks/use-inline-crud-state";
import { InlineCrudTable, type CrudColumn } from "@/components/admin/inline-crud-table";

const inputClass = "w-full px-2 py-1 text-sm border rounded";

type CompanyForm = { name: string; description: string };

function CompaniesTable() {
  const t = useTranslations("Lookups");
  const tCommon = useTranslations("Common");
  const { data, isLoading } = useCompanies();
  const createMutation = useCreateCompany();
  const updateMutation = useUpdateCompany();
  const deleteMutation = useDeleteCompany();

  const crud = useInlineCrudState<Company, CompanyForm>({
    data,
    emptyForm: { name: "", description: "" },
    toForm: (item) => ({ name: item.name, description: item.description || "" }),
    create: (form) =>
      createMutation.mutateAsync({ name: form.name, description: form.description || undefined }),
    update: (item, form) =>
      updateMutation.mutateAsync({
        ...item,
        name: form.name,
        description: form.description || undefined,
      }),
    remove: (id) => deleteMutation.mutate(id),
    canSave: (form) => !!form.name,
  });

  const columns: CrudColumn<Company, CompanyForm>[] = [
    {
      header: t("common.name"),
      edit: ({ form, set, mode }) => (
        <input
          value={form.name}
          onChange={(e) => set({ name: e.target.value })}
          className={inputClass}
          placeholder={t("common.name")}
          autoFocus={mode === "add"}
        />
      ),
      display: (item) => <span className="font-medium">{item.name}</span>,
    },
    {
      header: t("common.description"),
      edit: ({ form, set }) => (
        <input
          value={form.description}
          onChange={(e) => set({ description: e.target.value })}
          className={inputClass}
          placeholder={t("common.description")}
        />
      ),
      display: (item) => <span className="text-muted-foreground">{item.description || "—"}</span>,
    },
    {
      header: t("common.isActive"),
      edit: ({ mode, item }) =>
        mode === "add" ? (
          <span className="text-green-600">{tCommon("yes")}</span>
        ) : (
          <span>{item?.isActive ? tCommon("yes") : tCommon("no")}</span>
        ),
      display: (item) =>
        item.isActive ? (
          <span className="text-green-600">{tCommon("yes")}</span>
        ) : (
          <span className="text-muted-foreground">{tCommon("no")}</span>
        ),
    },
  ];

  if (isLoading) return <TableSkeleton rows={4} />;
  if (!data)
    return <EmptyState icon={<Building2 className="h-10 w-10" />} title={t("common.empty")} />;

  return (
    <InlineCrudTable
      crud={crud}
      columns={columns}
      addLabel={t("company.newCompany")}
      actionsLabel={t("common.actions")}
    />
  );
}

export default function AdminCompaniesPage() {
  const t = useTranslations("AdminCompanies");

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-foreground">{t("title")}</h1>
        <p className="text-sm text-muted-foreground mt-1">{t("subtitle")}</p>
      </div>

      <CompaniesTable />
    </div>
  );
}
