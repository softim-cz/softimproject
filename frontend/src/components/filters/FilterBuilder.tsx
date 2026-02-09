"use client";

import { useState } from "react";
import type { FilterCondition } from "@/stores/filter-store";

interface FilterBuilderProps {
  fields: { value: string; label: string }[];
  onApply: (condition: FilterCondition) => void;
  onClose: () => void;
}

const operators: { value: FilterCondition["operator"]; label: string }[] = [
  { value: "eq", label: "equals" },
  { value: "neq", label: "not equals" },
  { value: "contains", label: "contains" },
  { value: "gt", label: "greater than" },
  { value: "lt", label: "less than" },
];

export function FilterBuilder({ fields, onApply, onClose }: FilterBuilderProps) {
  const [field, setField] = useState(fields[0]?.value ?? "");
  const [operator, setOperator] =
    useState<FilterCondition["operator"]>("eq");
  const [value, setValue] = useState("");

  const handleApply = () => {
    if (!field || !value.trim()) return;
    onApply({ field, operator, value: value.trim() });
    setValue("");
  };

  return (
    <div className="bg-card border border-border rounded-lg shadow-lg p-3 w-80">
      <div className="space-y-2">
        <div>
          <label className="block text-xs font-medium text-muted-foreground mb-1">
            Field
          </label>
          <select
            value={field}
            onChange={(e) => setField(e.target.value)}
            className="w-full rounded border border-input bg-background px-2 py-1.5 text-sm focus:outline-none focus:ring-1 focus:ring-ring"
          >
            {fields.map((f) => (
              <option key={f.value} value={f.value}>
                {f.label}
              </option>
            ))}
          </select>
        </div>

        <div>
          <label className="block text-xs font-medium text-muted-foreground mb-1">
            Operator
          </label>
          <select
            value={operator}
            onChange={(e) =>
              setOperator(e.target.value as FilterCondition["operator"])
            }
            className="w-full rounded border border-input bg-background px-2 py-1.5 text-sm focus:outline-none focus:ring-1 focus:ring-ring"
          >
            {operators.map((op) => (
              <option key={op.value} value={op.value}>
                {op.label}
              </option>
            ))}
          </select>
        </div>

        <div>
          <label className="block text-xs font-medium text-muted-foreground mb-1">
            Value
          </label>
          <input
            type="text"
            value={value}
            onChange={(e) => setValue(e.target.value)}
            placeholder="Enter value..."
            className="w-full rounded border border-input bg-background px-2 py-1.5 text-sm focus:outline-none focus:ring-1 focus:ring-ring"
            onKeyDown={(e) => {
              if (e.key === "Enter") handleApply();
            }}
          />
        </div>

        <div className="flex justify-end gap-2 pt-1">
          <button
            onClick={onClose}
            className="px-3 py-1 text-sm text-muted-foreground hover:text-foreground transition-colors"
          >
            Cancel
          </button>
          <button
            onClick={handleApply}
            disabled={!value.trim()}
            className="px-3 py-1 text-sm bg-primary text-primary-foreground rounded hover:opacity-90 disabled:opacity-50"
          >
            Apply
          </button>
        </div>
      </div>
    </div>
  );
}
