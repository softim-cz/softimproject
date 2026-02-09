import { create } from "zustand";

export interface FilterCondition {
  field: string;
  operator: "eq" | "neq" | "contains" | "gt" | "lt";
  value: string;
}

interface FilterState {
  // Active filters per view key (e.g. "TaskList:projectId", "Kanban:projectId")
  activeFilters: Record<string, FilterCondition[]>;
  setFilters: (viewKey: string, filters: FilterCondition[]) => void;
  addFilter: (viewKey: string, filter: FilterCondition) => void;
  removeFilter: (viewKey: string, index: number) => void;
  clearFilters: (viewKey: string) => void;
}

export const useFilterStore = create<FilterState>((set) => ({
  activeFilters: {},
  setFilters: (viewKey, filters) =>
    set((state) => ({
      activeFilters: { ...state.activeFilters, [viewKey]: filters },
    })),
  addFilter: (viewKey, filter) =>
    set((state) => ({
      activeFilters: {
        ...state.activeFilters,
        [viewKey]: [...(state.activeFilters[viewKey] ?? []), filter],
      },
    })),
  removeFilter: (viewKey, index) =>
    set((state) => ({
      activeFilters: {
        ...state.activeFilters,
        [viewKey]: (state.activeFilters[viewKey] ?? []).filter(
          (_, i) => i !== index
        ),
      },
    })),
  clearFilters: (viewKey) =>
    set((state) => ({
      activeFilters: { ...state.activeFilters, [viewKey]: [] },
    })),
}));
