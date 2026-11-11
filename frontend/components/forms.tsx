"use client";

import { useState, type FormEvent } from "react";
import { Button, ErrorBanner, Field } from "@/components/ui";
import { ApiError } from "@/lib/api";

/** Стан форми: значення, помилки полів із відповіді сервера та загальна відмова. */
export function useForm<T extends Record<string, string>>(initial: T) {
  const [values, setValues] = useState<T>(initial);
  const [error, setError] = useState<unknown>(null);
  const [busy, setBusy] = useState(false);
  const fields = error instanceof ApiError ? error.fields : {};

  const bind = (name: keyof T & string) => ({
    value: values[name],
    error: fields[name],
    onChange: (e: { target: { value: string } }) => setValues((v) => ({ ...v, [name]: e.target.value })),
  });

  async function run(action: () => Promise<void>) {
    setBusy(true);
    setError(null);
    try {
      await action();
    } catch (err) {
      setError(err);
    } finally {
      setBusy(false);
    }
  }

  return { values, setValues, error, busy, bind, run, fields };
}

/** Банер показуємо лише для відмов без позначених полів — інакше помилки видно біля полів. */
export function FormError({ error }: { error: unknown }) {
  if (error instanceof ApiError && Object.keys(error.fields).length > 0 && error.body.code === "validation_failed")
    return <p className="mb-3 text-sm text-rose-700">{error.message}</p>;
  return <ErrorBanner error={error} />;
}

export type ClientValues = { fullName: string; phone: string; email: string };

export function ClientForm({
  initial,
  submitLabel,
  onSubmit,
}: {
  initial: ClientValues;
  submitLabel: string;
  onSubmit: (values: ClientValues) => Promise<void>;
}) {
  const form = useForm(initial);
  const submit = (e: FormEvent) => {
    e.preventDefault();
    form.run(() => onSubmit(form.values));
  };
  return (
    <form onSubmit={submit}>
      <FormError error={form.error} />
      <div className="grid gap-3 sm:grid-cols-3">
        <Field label="ПІБ *" {...form.bind("fullName")} />
        <Field label="Телефон *" placeholder="+380XXXXXXXXX" {...form.bind("phone")} />
        <Field label="E-mail" type="email" {...form.bind("email")} />
      </div>
      <Button type="submit" className="mt-3" disabled={form.busy}>
        {submitLabel}
      </Button>
    </form>
  );
}

export type CarValues = { make: string; model: string; year: string; vin: string; plateNumber: string };

export function CarForm({
  initial,
  submitLabel,
  onSubmit,
}: {
  initial: CarValues;
  submitLabel: string;
  onSubmit: (values: CarValues) => Promise<void>;
}) {
  const form = useForm(initial);
  const submit = (e: FormEvent) => {
    e.preventDefault();
    form.run(() => onSubmit(form.values));
  };
  return (
    <form onSubmit={submit}>
      <FormError error={form.error} />
      <div className="grid gap-3 sm:grid-cols-5">
        <Field label="Марка *" {...form.bind("make")} />
        <Field label="Модель *" {...form.bind("model")} />
        <Field label="Рік *" type="number" {...form.bind("year")} />
        <Field label="VIN *" maxLength={17} {...form.bind("vin")} />
        <Field label="Держномер *" {...form.bind("plateNumber")} />
      </div>
      <Button type="submit" className="mt-3" disabled={form.busy}>
        {submitLabel}
      </Button>
    </form>
  );
}

export const carPayload = (v: CarValues) => ({ ...v, year: v.year === "" ? null : Number(v.year) });
