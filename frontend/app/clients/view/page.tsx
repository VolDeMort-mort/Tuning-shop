"use client";

import Link from "next/link";
import { useSearchParams } from "next/navigation";
import { Suspense, useCallback, useState } from "react";
import { useAuth } from "@/components/auth";
import { CarForm, carPayload, ClientForm } from "@/components/forms";
import { OrdersTable } from "@/components/OrdersTable";
import { Button, Card, Empty, ErrorBanner, Loading, PageTitle, SuccessBanner, Table, Td } from "@/components/ui";
import { useLoad } from "@/components/useLoad";
import { api } from "@/lib/api";
import type { Car, ClientDetails } from "@/lib/types";

/** Картка клієнта: дані (FR-03), автомобілі (UC-02, FR-04) і замовлення. */
function ClientCard() {
  const id = useSearchParams().get("id");
  const { user } = useAuth();
  const isManager = user?.role === "Manager";
  const [editing, setEditing] = useState(false);
  const [notice, setNotice] = useState<string | null>(null);
  const load = useCallback(() => api.get<ClientDetails>(`/api/clients/${id}`), [id]);
  const { data: client, error, reload } = useLoad(id ? load : null);

  if (error) return <ErrorBanner error={error} />;
  if (!client) return <Loading />;

  return (
    <>
      <PageTitle actions={isManager && !editing && <Button variant="secondary" onClick={() => setEditing(true)}>Редагувати</Button>}>
        {client.fullName}
      </PageTitle>
      <SuccessBanner message={notice} />

      <Card title="Контактні дані">
        {editing ? (
          <ClientForm
            initial={{ fullName: client.fullName, phone: client.phone, email: client.email ?? "" }}
            submitLabel="Зберегти"
            onSubmit={async (values) => {
              await api.put(`/api/clients/${client.id}`, values);
              setEditing(false);
              setNotice("Дані клієнта збережено.");
              await reload();
            }}
          />
        ) : (
          <dl className="grid gap-2 text-sm sm:grid-cols-3">
            <div><dt className="text-zinc-500">Телефон</dt><dd>{client.phone}</dd></div>
            <div><dt className="text-zinc-500">E-mail</dt><dd>{client.email ?? "—"}</dd></div>
          </dl>
        )}
      </Card>

      <Card title="Автомобілі">
        {client.cars.length === 0 ? (
          <Empty>Автомобілів ще немає.</Empty>
        ) : (
          <Table head={["Автомобіль", "Держномер", "VIN"]}>
            {client.cars.map((c) => (
              <tr key={c.id}>
                <Td>
                  <Link href={`/cars/view/?id=${c.id}`} className="underline">{c.make} {c.model} ({c.year})</Link>
                </Td>
                <Td>{c.plateNumber}</Td>
                <Td>{c.vin}</Td>
              </tr>
            ))}
          </Table>
        )}
        {isManager && (
          <div className="mt-4 border-t border-zinc-100 pt-4">
            <h3 className="mb-2 text-sm font-semibold">Додати автомобіль</h3>
            <CarForm
              initial={{ make: "", model: "", year: "", vin: "", plateNumber: "" }}
              submitLabel="Додати"
              onSubmit={async (values) => {
                const car = await api.post<Car>(`/api/clients/${client.id}/cars`, carPayload(values));
                setNotice(`Автомобіль ${car.plateNumber} додано.`);
                await reload();
              }}
            />
          </div>
        )}
      </Card>

      <Card title="Замовлення">
        <OrdersTable orders={client.orders} />
      </Card>
    </>
  );
}

export default function ClientPage() {
  return (
    <Suspense fallback={<Loading />}>
      <ClientCard />
    </Suspense>
  );
}
