"use client";

import { useId, type ButtonHTMLAttributes, type InputHTMLAttributes, type ReactNode, type SelectHTMLAttributes } from "react";
import { ApiError } from "@/lib/api";
import { statusTone } from "@/lib/format";
import type { OrderStatus, PartStatus, WorkStatus } from "@/lib/types";

export function PageTitle({ children, actions }: { children: ReactNode; actions?: ReactNode }) {
  return (
    <div className="mb-5 flex flex-wrap items-center justify-between gap-3">
      <h1 className="text-2xl font-semibold">{children}</h1>
      {actions && <div className="flex flex-wrap gap-2">{actions}</div>}
    </div>
  );
}

export function Card({ title, children, actions }: { title?: string; children: ReactNode; actions?: ReactNode }) {
  return (
    <section className="mb-5 rounded-lg border border-zinc-200 bg-white p-4 shadow-sm">
      {(title || actions) && (
        <div className="mb-3 flex flex-wrap items-center justify-between gap-2">
          {title && <h2 className="text-lg font-semibold">{title}</h2>}
          {actions && <div className="flex flex-wrap gap-2">{actions}</div>}
        </div>
      )}
      {children}
    </section>
  );
}

type ButtonProps = ButtonHTMLAttributes<HTMLButtonElement> & { variant?: "primary" | "secondary" | "danger" };

export function Button({ variant = "primary", className = "", ...props }: ButtonProps) {
  const tone = {
    primary: "bg-zinc-900 text-white hover:bg-zinc-700",
    secondary: "border border-zinc-300 bg-white text-zinc-900 hover:bg-zinc-100",
    danger: "bg-rose-600 text-white hover:bg-rose-500",
  }[variant];
  return (
    <button
      type="button"
      className={`rounded-md px-3 py-1.5 text-sm font-medium transition-colors disabled:cursor-not-allowed disabled:opacity-50 ${tone} ${className}`}
      {...props}
    />
  );
}

/** Поле введення з повідомленням про помилку біля нього (NFR-03); повідомлення пов'язане з полем через aria-describedby. */
export function Field({
  label,
  error,
  ...props
}: InputHTMLAttributes<HTMLInputElement> & { label: string; error?: string }) {
  const id = useId();
  return (
    <div className="flex flex-col gap-1 text-sm">
      <label htmlFor={id} className="font-medium text-zinc-700">{label}</label>
      <input
        id={id}
        className={`rounded-md border px-2.5 py-1.5 ${error ? "border-rose-500 bg-rose-50" : "border-zinc-300"}`}
        aria-invalid={!!error}
        aria-describedby={error ? id + "-error" : undefined}
        {...props}
      />
      {error && <span id={id + "-error"} className="text-xs text-rose-600">{error}</span>}
    </div>
  );
}

export function SelectField({
  label,
  error,
  children,
  ...props
}: SelectHTMLAttributes<HTMLSelectElement> & { label: string; error?: string; children: ReactNode }) {
  const id = useId();
  return (
    <div className="flex flex-col gap-1 text-sm">
      <label htmlFor={id} className="font-medium text-zinc-700">{label}</label>
      <select
        id={id}
        className={`rounded-md border px-2.5 py-1.5 ${error ? "border-rose-500 bg-rose-50" : "border-zinc-300"}`}
        aria-invalid={!!error}
        aria-describedby={error ? id + "-error" : undefined}
        {...props}
      >
        {children}
      </select>
      {error && <span id={id + "-error"} className="text-xs text-rose-600">{error}</span>}
    </div>
  );
}

export function StatusBadge({ status, name }: { status: OrderStatus | WorkStatus | PartStatus; name: string }) {
  return <span className={`inline-block rounded-full px-2 py-0.5 text-xs font-medium ${statusTone(status)}`}>{name}</span>;
}

/** Повідомлення про відмову: текст сервера та перелік деталей (наприклад, роботи без майстра). */
export function ErrorBanner({ error }: { error: unknown }) {
  if (!error) return null;
  const message = error instanceof ApiError ? error.message : "Неочікувана помилка.";
  const details = error instanceof ApiError ? (error.body.details ?? []) : [];
  return (
    <div role="alert" className="mb-4 rounded-md border border-rose-300 bg-rose-50 px-3 py-2 text-sm text-rose-800">
      <p className="font-medium">{message}</p>
      {details.length > 0 && (
        <ul className="mt-1 list-inside list-disc">
          {details.map((d) => (
            <li key={d}>{d}</li>
          ))}
        </ul>
      )}
    </div>
  );
}

export function SuccessBanner({ message }: { message: string | null }) {
  if (!message) return null;
  return (
    <div role="status" className="mb-4 rounded-md border border-emerald-300 bg-emerald-50 px-3 py-2 text-sm text-emerald-800">
      {message}
    </div>
  );
}

export function Loading() {
  return <p className="text-sm text-zinc-500">Завантаження…</p>;
}

export function Empty({ children }: { children: ReactNode }) {
  return <p className="text-sm text-zinc-500">{children}</p>;
}

export function Table({ head, children }: { head: string[]; children: ReactNode }) {
  return (
    <div className="overflow-x-auto">
      <table className="w-full text-left text-sm">
        <thead className="border-b border-zinc-200 text-zinc-500">
          <tr>
            {head.map((h) => (
              <th key={h} className="px-2 py-2 font-medium">
                {h}
              </th>
            ))}
          </tr>
        </thead>
        <tbody className="divide-y divide-zinc-100">{children}</tbody>
      </table>
    </div>
  );
}

export function Td({ children, className = "" }: { children: ReactNode; className?: string }) {
  return <td className={`px-2 py-2 align-top ${className}`}>{children}</td>;
}
