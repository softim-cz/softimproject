"use client";

import { useState } from "react";
import { X, Filter, Save, ChevronDown } from "lucide-react";
import { useFilterStore, type FilterCondition } from "@/stores/filter-store";
import {
  useSavedFilters,
  useCreateSavedFilter,
  type SavedFilterDto,
} from "@/queries/saved-filters";
import { FilterBuilder } from "./FilterBuilder";
import { toast } from "sonner";

interface FilterBarProps {
  viewKey: string;
  viewType: string;
  projectId?: string;
  filterFields: { value: string; label: string }[];
}

export function FilterBar({ viewKey, viewType, projectId, filterFields }: FilterBarProps) {
  const { activeFilters, setFilters, removeFilter, clearFilters } = useFilterStore();
  const filters = activeFilters[viewKey] ?? [];
  const { data: savedFilters } = useSavedFilters(viewType, projectId);
  const createSavedFilter = useCreateSavedFilter();
  const [showBuilder, setShowBuilder] = useState(false);
  const [showSaved, setShowSaved] = useState(false);
  const [showSaveDialog, setShowSaveDialog] = useState(false);
  const [saveName, setSaveName] = useState("");

  const handleApplyFilter = (condition: FilterCondition) => {
    setFilters(viewKey, [...filters, condition]);
    setShowBuilder(false);
  };

  const handleLoadSavedFilter = (saved: SavedFilterDto) => {
    try {
      const conditions = JSON.parse(saved.filterJson) as FilterCondition[];
      setFilters(viewKey, conditions);
    } catch {
      toast.error("Failed to load filter");
    }
    setShowSaved(false);
  };

  const handleSaveFilter = async () => {
    if (!saveName.trim()) return;
    try {
      await createSavedFilter.mutateAsync({
        name: saveName.trim(),
        projectId,
        viewType,
        filterJson: JSON.stringify(filters),
        isSystem: false,
        sortOrder: 0,
      });
      toast.success("Filter saved");
      setSaveName("");
      setShowSaveDialog(false);
    } catch {
      toast.error("Failed to save filter");
    }
  };

  const operatorLabel = (op: string) => {
    switch (op) {
      case "eq":
        return "=";
      case "neq":
        return "!=";
      case "contains":
        return "contains";
      case "gt":
        return ">";
      case "lt":
        return "<";
      default:
        return op;
    }
  };

  return (
    <div className="space-y-2">
      <div className="flex items-center gap-2 flex-wrap">
        {/* Add filter button */}
        <div className="relative">
          <button
            onClick={() => {
              setShowBuilder((v) => !v);
              setShowSaved(false);
            }}
            className="inline-flex items-center gap-1.5 px-3 py-1.5 text-sm text-muted-foreground hover:text-foreground border border-border rounded-lg hover:bg-muted transition-colors"
          >
            <Filter className="h-3.5 w-3.5" />
            Add filter
          </button>
          {showBuilder && (
            <div className="absolute left-0 top-full mt-1 z-20">
              <FilterBuilder
                fields={filterFields}
                onApply={handleApplyFilter}
                onClose={() => setShowBuilder(false)}
              />
            </div>
          )}
        </div>

        {/* Load saved filter */}
        <div className="relative">
          <button
            onClick={() => {
              setShowSaved((v) => !v);
              setShowBuilder(false);
            }}
            className="inline-flex items-center gap-1.5 px-3 py-1.5 text-sm text-muted-foreground hover:text-foreground border border-border rounded-lg hover:bg-muted transition-colors"
          >
            <ChevronDown className="h-3.5 w-3.5" />
            Saved filters
          </button>
          {showSaved && savedFilters && (
            <div className="absolute left-0 top-full mt-1 z-20 bg-card border border-border rounded-lg shadow-lg py-1 w-56 max-h-64 overflow-y-auto">
              {savedFilters.length === 0 ? (
                <p className="px-3 py-2 text-xs text-muted-foreground">No saved filters</p>
              ) : (
                savedFilters.map((sf) => (
                  <button
                    key={sf.id}
                    onClick={() => handleLoadSavedFilter(sf)}
                    className="w-full text-left px-3 py-1.5 text-sm hover:bg-muted transition-colors flex items-center gap-2"
                  >
                    {sf.isSystem && <span className="text-xs text-muted-foreground">[sys]</span>}
                    {sf.name}
                  </button>
                ))
              )}
            </div>
          )}
        </div>

        {/* Active filter chips */}
        {filters.map((f, i) => (
          <span
            key={i}
            className="inline-flex items-center gap-1 px-2 py-1 text-xs bg-muted rounded-full"
          >
            <span className="font-medium">{f.field}</span>
            <span className="text-muted-foreground">{operatorLabel(f.operator)}</span>
            <span>{f.value}</span>
            <button
              onClick={() => removeFilter(viewKey, i)}
              className="ml-0.5 hover:text-destructive"
            >
              <X className="h-3 w-3" />
            </button>
          </span>
        ))}

        {filters.length > 0 && (
          <>
            <button
              onClick={() => clearFilters(viewKey)}
              className="text-xs text-muted-foreground hover:text-destructive transition-colors"
            >
              Clear all
            </button>
            <div className="relative">
              <button
                onClick={() => setShowSaveDialog((v) => !v)}
                className="inline-flex items-center gap-1 text-xs text-muted-foreground hover:text-foreground transition-colors"
              >
                <Save className="h-3 w-3" />
                Save
              </button>
              {showSaveDialog && (
                <div className="absolute left-0 top-full mt-1 z-20 bg-card border border-border rounded-lg shadow-lg p-3 w-56">
                  <input
                    type="text"
                    value={saveName}
                    onChange={(e) => setSaveName(e.target.value)}
                    placeholder="Filter name"
                    className="w-full rounded border border-input bg-background px-2 py-1 text-sm focus:outline-none focus:ring-1 focus:ring-ring mb-2"
                    onKeyDown={(e) => {
                      if (e.key === "Enter") handleSaveFilter();
                    }}
                  />
                  <button
                    onClick={handleSaveFilter}
                    disabled={!saveName.trim()}
                    className="w-full px-2 py-1 text-sm bg-primary text-primary-foreground rounded hover:opacity-90 disabled:opacity-50"
                  >
                    Save filter
                  </button>
                </div>
              )}
            </div>
          </>
        )}
      </div>
    </div>
  );
}
