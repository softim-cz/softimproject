import { useState } from "react";
import type { Dispatch, SetStateAction } from "react";

/**
 * Sdílený stav pro tabulky s inline přidáváním/editací řádků (číselníky v administraci).
 *
 * Sjednocuje opakovanou logiku „přidávám / edituji řádek X / formulář / ulož / zruš",
 * která se dříve ručně opisovala v každé záložce číselníků.
 */
export interface InlineCrudConfig<T extends { id: string }, F> {
  /** Načtená data tabulky (z TanStack Query). */
  data: T[] | undefined;
  /**
   * Prázdný formulář pro nový řádek. Funkce dostane aktuální data, aby šlo
   * dopočítat např. výchozí `sortOrder`.
   */
  emptyForm: F | ((items: T[]) => F);
  /** Převod existující položky na hodnoty formuláře pro editaci. */
  toForm: (item: T) => F;
  /** Vytvoření nové položky. */
  create: (form: F) => Promise<unknown>;
  /** Úprava existující položky. */
  update: (item: T, form: F) => Promise<unknown>;
  /** Smazání položky. */
  remove: (id: string) => void;
  /** Validace formuláře; pokud vrátí false, ukládání je zakázané. Výchozí: vždy true. */
  canSave?: (form: F) => boolean;
}

export interface InlineCrud<T extends { id: string }, F> {
  data: T[] | undefined;
  adding: boolean;
  editId: string | null;
  form: F;
  setForm: Dispatch<SetStateAction<F>>;
  startAdd: () => void;
  startEdit: (item: T) => void;
  save: () => Promise<void>;
  cancel: () => void;
  remove: (id: string) => void;
  canSave: boolean;
}

export function useInlineCrudState<T extends { id: string }, F>(
  config: InlineCrudConfig<T, F>
): InlineCrud<T, F> {
  const resolveEmpty = (items: T[]): F =>
    typeof config.emptyForm === "function"
      ? (config.emptyForm as (items: T[]) => F)(items)
      : config.emptyForm;

  const [adding, setAdding] = useState(false);
  const [editId, setEditId] = useState<string | null>(null);
  const [form, setForm] = useState<F>(() => resolveEmpty(config.data ?? []));

  const startAdd = () => {
    setForm(resolveEmpty(config.data ?? []));
    setAdding(true);
    setEditId(null);
  };

  const startEdit = (item: T) => {
    setForm(config.toForm(item));
    setEditId(item.id);
    setAdding(false);
  };

  const cancel = () => {
    setAdding(false);
    setEditId(null);
  };

  const save = async () => {
    if (editId) {
      const existing = config.data?.find((i) => i.id === editId);
      if (!existing) return;
      await config.update(existing, form);
      setEditId(null);
    } else {
      await config.create(form);
      setAdding(false);
    }
  };

  const canSave = config.canSave ? config.canSave(form) : true;

  return {
    data: config.data,
    adding,
    editId,
    form,
    setForm,
    startAdd,
    startEdit,
    save,
    cancel,
    remove: config.remove,
    canSave,
  };
}
