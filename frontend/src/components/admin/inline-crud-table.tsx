"use client";

import type { ReactNode } from "react";
import { Plus, Pencil, Trash2, Check, X } from "lucide-react";
import { cn } from "@/lib/utils";
import type { InlineCrud } from "@/hooks/use-inline-crud-state";

export interface CrudEditContext<T, F> {
  form: F;
  /** Sloučí částečnou změnu do formuláře. */
  set: (patch: Partial<F>) => void;
  mode: "add" | "edit";
  /** Editovaná položka (jen v režimu „edit"). */
  item?: T;
}

export interface CrudColumn<T extends { id: string }, F> {
  header: string;
  /** Doplňkové třídy pro hlavičku sloupce (např. šířka). */
  thClassName?: string;
  align?: "left" | "right";
  /** Vstupní pole pro řádek přidání/editace. */
  edit: (ctx: CrudEditContext<T, F>) => ReactNode;
  /** Hodnota pro zobrazený (needitovaný) řádek. */
  display: (item: T) => ReactNode;
}

/** Trojice vstupů název / český název / anglický název, sdílená napříč číselníky. */
export function NameTrioInputs<F extends { name: string; nameCs: string; nameEn: string }>({
  form,
  set,
  mode,
  placeholders,
  compact = false,
}: {
  form: F;
  set: (patch: Partial<F>) => void;
  mode: "add" | "edit";
  placeholders: { name: string; cs: string; en: string };
  compact?: boolean;
}) {
  const cls = cn("w-full px-2 py-1 border rounded", compact ? "text-xs" : "text-sm");
  return (
    <div className="flex gap-1.5">
      <input
        value={form.name}
        onChange={(e) => set({ name: e.target.value } as Partial<F>)}
        className={cls}
        placeholder={placeholders.name}
        autoFocus={mode === "add"}
      />
      <input
        value={form.nameCs}
        onChange={(e) => set({ nameCs: e.target.value } as Partial<F>)}
        className={cls}
        placeholder={placeholders.cs}
      />
      <input
        value={form.nameEn}
        onChange={(e) => set({ nameEn: e.target.value } as Partial<F>)}
        className={cls}
        placeholder={placeholders.en}
      />
    </div>
  );
}

interface InlineCrudTableProps<T extends { id: string }, F> {
  crud: InlineCrud<T, F>;
  columns: CrudColumn<T, F>[];
  /** Popisek tlačítka „přidat". */
  addLabel: string;
  /** Kompaktní varianta (menší odsazení/ikony) pro vnořené tabulky. */
  compact?: boolean;
  /** Titulek vlevo v hlavičce (kompaktní varianta). */
  title?: string;
  /** Zobrazit prázdný řádek, když nejsou žádná data. */
  emptyLabel?: string;
  /** Popisek hlavičky sloupce s akcemi. */
  actionsLabel?: string;
}

export function InlineCrudTable<T extends { id: string }, F>({
  crud,
  columns,
  addLabel,
  compact = false,
  title,
  emptyLabel,
  actionsLabel,
}: InlineCrudTableProps<T, F>) {
  const {
    data,
    adding,
    editId,
    form,
    setForm,
    startAdd,
    startEdit,
    save,
    cancel,
    remove,
    canSave,
  } = crud;

  const set = (patch: Partial<F>) => setForm((prev) => ({ ...prev, ...patch }));

  const thBase = cn(
    "text-left text-xs font-medium text-muted-foreground uppercase",
    compact ? "px-3 py-2" : "px-4 py-3"
  );
  const editTd = compact ? "px-3 py-1.5" : "px-4 py-2";
  const displayTd = compact ? "px-3 py-2" : "px-4 py-3";
  const actionColWidth = compact ? "w-20" : "w-24";
  const items = data ?? [];
  const colSpan = columns.length + 1;

  return (
    <div>
      {title ? (
        <div className="flex items-center justify-between mb-2">
          <p className="text-xs font-medium text-muted-foreground uppercase">{title}</p>
          <button
            onClick={startAdd}
            className="flex items-center gap-1 px-2 py-1 text-xs font-medium text-accent-orange hover:bg-accent-orange/10 rounded"
          >
            <Plus className="h-3 w-3" /> {addLabel}
          </button>
        </div>
      ) : (
        <div className="flex justify-end mb-3">
          <button
            onClick={startAdd}
            className="flex items-center gap-1.5 px-3 py-1.5 text-sm font-medium bg-accent-orange text-white rounded-lg hover:bg-accent-orange/90"
          >
            <Plus className="h-4 w-4" /> {addLabel}
          </button>
        </div>
      )}

      <div className="rounded-lg border border-border overflow-hidden">
        <table className="w-full">
          <thead>
            <tr className="bg-muted/50">
              {columns.map((col, i) => (
                <th
                  key={i}
                  className={cn(thBase, col.align === "right" && "text-right", col.thClassName)}
                >
                  {col.header}
                </th>
              ))}
              <th className={cn(thBase, "text-right", actionColWidth)}>{actionsLabel}</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-border">
            {adding && (
              <tr className="bg-accent-orange/5">
                {columns.map((col, i) => (
                  <td key={i} className={editTd}>
                    {col.edit({ form, set, mode: "add" })}
                  </td>
                ))}
                <td className={cn(editTd, "text-right")}>
                  <button
                    onClick={save}
                    disabled={!canSave}
                    className="p-1 text-green-600 hover:bg-green-50 rounded disabled:opacity-30"
                  >
                    <Check className={compact ? "h-3.5 w-3.5" : "h-4 w-4"} />
                  </button>
                  <button
                    onClick={cancel}
                    className="p-1 text-muted-foreground hover:bg-muted/50 rounded"
                  >
                    <X className={compact ? "h-3.5 w-3.5" : "h-4 w-4"} />
                  </button>
                </td>
              </tr>
            )}

            {emptyLabel && items.length === 0 && !adding && (
              <tr>
                <td
                  colSpan={colSpan}
                  className="px-3 py-3 text-xs text-center text-muted-foreground"
                >
                  {emptyLabel}
                </td>
              </tr>
            )}

            {items.map((item) =>
              editId === item.id ? (
                <tr key={item.id} className="bg-accent-orange/5">
                  {columns.map((col, i) => (
                    <td key={i} className={editTd}>
                      {col.edit({ form, set, mode: "edit", item })}
                    </td>
                  ))}
                  <td className={cn(editTd, "text-right")}>
                    <button
                      onClick={save}
                      disabled={!canSave}
                      className="p-1 text-green-600 hover:bg-green-50 rounded disabled:opacity-30"
                    >
                      <Check className={compact ? "h-3.5 w-3.5" : "h-4 w-4"} />
                    </button>
                    <button
                      onClick={cancel}
                      className="p-1 text-muted-foreground hover:bg-muted/50 rounded"
                    >
                      <X className={compact ? "h-3.5 w-3.5" : "h-4 w-4"} />
                    </button>
                  </td>
                </tr>
              ) : (
                <tr key={item.id} className="hover:bg-muted/30">
                  {columns.map((col, i) => (
                    <td
                      key={i}
                      className={cn(
                        displayTd,
                        compact ? "text-xs" : "text-sm",
                        col.align === "right" && "text-right"
                      )}
                    >
                      {col.display(item)}
                    </td>
                  ))}
                  <td className={cn(displayTd, "text-right")}>
                    <button
                      onClick={() => startEdit(item)}
                      className={cn(
                        "text-muted-foreground hover:text-foreground hover:bg-muted/50 rounded",
                        compact ? "p-0.5" : "p-1"
                      )}
                    >
                      <Pencil className={compact ? "h-3 w-3" : "h-3.5 w-3.5"} />
                    </button>
                    <button
                      onClick={() => remove(item.id)}
                      className={cn(
                        "text-muted-foreground hover:text-red-600 hover:bg-red-50 rounded",
                        compact ? "p-0.5" : "p-1"
                      )}
                    >
                      <Trash2 className={compact ? "h-3 w-3" : "h-3.5 w-3.5"} />
                    </button>
                  </td>
                </tr>
              )
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}
