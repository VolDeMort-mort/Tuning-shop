"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useEffect, useState, type FormEvent } from "react";
import { homeFor, useAuth } from "@/components/auth";
import { Button, Card, ErrorBanner, Field } from "@/components/ui";

/** FR-01: вхід працівника за логіном і паролем. */
export default function LoginPage() {
  const { user, login } = useAuth();
  const router = useRouter();
  const [form, setForm] = useState({ login: "", password: "" });
  const [error, setError] = useState<unknown>(null);
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    if (user) router.replace(homeFor(user));
  }, [user, router]);

  async function submit(e: FormEvent) {
    e.preventDefault();
    setBusy(true);
    setError(null);
    try {
      const current = await login(form.login, form.password);
      router.replace(homeFor(current));
    } catch (err) {
      setError(err);
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="mx-auto max-w-sm">
      <Card title="Вхід для працівників">
        <ErrorBanner error={error} />
        <form onSubmit={submit} className="flex flex-col gap-3">
          <Field label="Логін" autoComplete="username" value={form.login}
            onChange={(e) => setForm({ ...form, login: e.target.value })} />
          <Field label="Пароль" type="password" autoComplete="current-password" value={form.password}
            onChange={(e) => setForm({ ...form, password: e.target.value })} />
          <Button type="submit" disabled={busy}>
            Увійти
          </Button>
        </form>
      </Card>
      <p className="text-center text-sm text-zinc-600">
        Клієнт? <Link href="/track/" className="underline">Відстежте стан свого замовлення</Link>
      </p>
    </div>
  );
}
