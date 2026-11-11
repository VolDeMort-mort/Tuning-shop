"use client";

import { Field, SelectField } from "@/components/ui";
import { categoryNames } from "@/lib/format";
import type { Employee, WorkCategory, WorkInput } from "@/lib/types";

export const emptyWork: WorkInput = { category: "", description: "", estimatedCost: "", masterId: "" };

export const workPayload = (w: WorkInput) => ({
  category: w.category === "" ? null : w.category,
  description: w.description,
  estimatedCost: w.estimatedCost === "" ? null : Number(w.estimatedCost),
  masterId: w.masterId === "" ? null : Number(w.masterId),
});

/** Поля роботи: вид, опис, орієнтовна вартість і майстер (FR-07, FR-08). */
export function WorkFields({
  value,
  onChange,
  masters,
  errors,
  showMaster = true,
}: {
  value: WorkInput;
  onChange: (value: WorkInput) => void;
  masters: Employee[];
  errors: Record<string, string | undefined>;
  showMaster?: boolean;
}) {
  return (
    <div className={`grid gap-3 ${showMaster ? "sm:grid-cols-[10rem_1fr_9rem_12rem]" : "sm:grid-cols-[10rem_1fr_9rem]"}`}>
      <SelectField label="Вид роботи *" value={value.category} error={errors.category}
        onChange={(e) => onChange({ ...value, category: e.target.value as WorkCategory | "" })}>
        <option value="">—</option>
        {Object.entries(categoryNames).map(([key, name]) => (
          <option key={key} value={key}>{name}</option>
        ))}
      </SelectField>
      <Field label="Опис *" value={value.description} error={errors.description}
        onChange={(e) => onChange({ ...value, description: e.target.value })} />
      <Field label="Вартість, грн *" type="number" min="0" step="0.01" value={value.estimatedCost} error={errors.estimatedCost}
        onChange={(e) => onChange({ ...value, estimatedCost: e.target.value })} />
      {showMaster && (
        <SelectField label="Майстер" value={value.masterId} error={errors.masterId}
          onChange={(e) => onChange({ ...value, masterId: e.target.value })}>
          <option value="">не призначено</option>
          {masters.map((m) => (
            <option key={m.id} value={m.id}>{m.fullName}</option>
          ))}
        </SelectField>
      )}
    </div>
  );
}
