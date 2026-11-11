"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useState, type FormEvent } from "react";
import { useAuth } from "@/components/auth";
import { ClientForm } from "@/components/forms";
import { Button, Card, Empty, ErrorBanner, Field, PageTitle, Table, Td } from "@/components/ui";
import { api, query } from "@/lib/api";
import type { Car, Client } from "@/lib/types";

/** UC-08, UC-11 — пошук клієнта й автомобіля (FR-18); UC-01 — реєстрація клієнта (FR-02). */
export default function ClientsPage() {
  const { user } = useAuth();
  const router = useRouter();
  const [text, setText] = useState("");
  const [found, setFound] = useState<{ clients: Client[]; cars: Car[] } | null>(null);
  const [error, setError] = useState<unknown>(null);

  async function search(e: FormEvent) {
    e.preventDefault();
    setError(null);
    try {
      const [clients, cars] = await Promise.all([
        api.get<Client[]>("/api/clients" + query({ query: text })),
        api.get<Car[]>("/api/cars" + query({ query: text })),
      ]);
      // Якщо знайдено рівно один запис, одразу відкриваємо його картку (AC-18).
      if (clients.length + cars.length === 1) {
        router.push(clients.length ? `/clients/view/?id=${clients[0].id}` : `/cars/view/?id=${cars[0].id}`);
        return;
      }
      setFound({ clients, cars });
    } catch (err) {
      setError(err);
    }
  }

  return (
    <>
      <PageTitle>Клієнти й автомобілі</PageTitle>
      <Card title="Пошук">
        <form onSubmit={search} className="flex flex-wrap items-end gap-3">
          <div className="min-w-64 flex-1">
            <Field label="ПІБ (частина), телефон, держномер або VIN" value={text} onChange={(e) => setText(e.target.value)} />
          </div>
          <Button type="submit">Знайти</Button>
        </form>
      </Card>

      <ErrorBanner error={error} />

      {found && (
        <Card title="Результати">
          {found.clients.length + found.cars.length === 0 ? (
            <Empty>Нічого не знайдено.</Empty>
          ) : (
            <>
              {found.clients.length > 0 && (
                <Table head={["Клієнт", "Телефон", "E-mail"]}>
                  {found.clients.map((c) => (
                    <tr key={c.id}>
                      <Td>
                        <Link href={`/clients/view/?id=${c.id}`} className="underline">{c.fullName}</Link>
                      </Td>
                      <Td>{c.phone}</Td>
                      <Td>{c.email ?? "—"}</Td>
                    </tr>
                  ))}
                </Table>
              )}
              {found.cars.length > 0 && (
                <Table head={["Автомобіль", "Держномер", "VIN"]}>
                  {found.cars.map((c) => (
                    <tr key={c.id}>
                      <Td>
                        <Link href={`/cars/view/?id=${c.id}`} className="underline">
                          {c.make} {c.model} ({c.year})
                        </Link>
                      </Td>
                      <Td>{c.plateNumber}</Td>
                      <Td>{c.vin}</Td>
                    </tr>
                  ))}
                </Table>
              )}
            </>
          )}
        </Card>
      )}

      {user?.role === "Manager" && (
        <Card title="Зареєструвати клієнта">
          <ClientForm
            initial={{ fullName: "", phone: "", email: "" }}
            submitLabel="Зареєструвати"
            onSubmit={async (values) => {
              const client = await api.post<Client>("/api/clients", values);
              router.push(`/clients/view/?id=${client.id}`);
            }}
          />
        </Card>
      )}
    </>
  );
}
