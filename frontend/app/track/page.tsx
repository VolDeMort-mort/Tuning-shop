"use client";

import { useState, type FormEvent } from "react";
import { Button, Card, ErrorBanner, Field, StatusBadge } from "@/components/ui";
import { api, query } from "@/lib/api";
import { categoryNames, formatDate, formatDateTime } from "@/lib/format";
import type { Tracking } from "@/lib/types";

/** UC-10 «Відстежити стан замовлення і деталей»: без входу, лише перегляд (FR-21). */
export default function TrackPage() {
  const [form, setForm] = useState({ number: "", phone: "" });
  const [result, setResult] = useState<Tracking | null>(null);
  const [error, setError] = useState<unknown>(null);
  const [busy, setBusy] = useState(false);

  async function submit(e: FormEvent) {
    e.preventDefault();
    setBusy(true);
    setError(null);
    setResult(null);
    try {
      setResult(await api.get<Tracking>("/api/tracking" + query(form)));
    } catch (err) {
      setError(err);
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="mx-auto max-w-2xl">
      <Card title="Відстежити замовлення">
        <form onSubmit={submit} className="grid gap-3 sm:grid-cols-[1fr_1fr_auto] sm:items-end">
          <Field label="Номер замовлення" placeholder="TS-2026-0001" value={form.number}
            onChange={(e) => setForm({ ...form, number: e.target.value })} />
          <Field label="Телефон власника" placeholder="+380XXXXXXXXX" value={form.phone}
            onChange={(e) => setForm({ ...form, phone: e.target.value })} />
          <Button type="submit" disabled={busy}>Показати</Button>
        </form>
      </Card>

      <ErrorBanner error={error} />

      {result && (
        <Card title={`Замовлення ${result.number}`}>
          <p className="mb-1 text-sm">
            {result.carTitle}, {result.plateNumber}
          </p>
          <p className="mb-4 flex items-center gap-2 text-sm">
            Статус: <StatusBadge status={result.status} name={result.statusName} />
            <span className="text-zinc-600">· {result.progress}</span>
          </p>
          <ul className="flex flex-col gap-3">
            {result.works.map((w, i) => (
              <li key={i} className="rounded-md border border-zinc-200 p-3 text-sm">
                <div className="flex flex-wrap items-center justify-between gap-2">
                  <span>
                    <span className="text-zinc-500">{categoryNames[w.category]}:</span> {w.description}
                  </span>
                  <StatusBadge status={w.status} name={w.statusName} />
                </div>
                {w.parts.length > 0 && (
                  <ul className="mt-2 list-inside list-disc text-zinc-700">
                    {w.parts.map((p, j) => (
                      <li key={j}>
                        {p.name} × {p.quantity} — {p.statusName}
                        {p.status === "Received"
                          ? ` (${formatDateTime(p.receivedAt)})`
                          : p.expectedAt && `, очікується ${formatDate(p.expectedAt)}`}
                      </li>
                    ))}
                  </ul>
                )}
              </li>
            ))}
          </ul>
        </Card>
      )}
    </div>
  );
}
