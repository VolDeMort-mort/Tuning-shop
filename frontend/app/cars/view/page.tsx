"use client";

import Link from "next/link";
import { useSearchParams } from "next/navigation";
import { Suspense, useCallback, useState } from "react";
import { useAuth } from "@/components/auth";
import { CarForm, carPayload } from "@/components/forms";
import { OrdersTable } from "@/components/OrdersTable";
import { Button, Card, ErrorBanner, Loading, PageTitle, SuccessBanner } from "@/components/ui";
import { useLoad } from "@/components/useLoad";
import { api } from "@/lib/api";
import type { CarDetails } from "@/lib/types";

/** UC-11 «Переглянути автомобіль», FR-05 — редагування; звідси починається UC-03. */
function CarCard() {
  const id = useSearchParams().get("id");
  const { user } = useAuth();
  const isManager = user?.role === "Manager";
  const [editing, setEditing] = useState(false);
  const [notice, setNotice] = useState<string | null>(null);
  const load = useCallback(() => api.get<CarDetails>(`/api/cars/${id}`), [id]);
  const { data, error, reload } = useLoad(id ? load : null);

  if (error) return <ErrorBanner error={error} />;
  if (!data) return <Loading />;
  const { car, owner } = data;

  return (
    <>
      <PageTitle
        actions={
          isManager && (
            <>
              {!editing && <Button variant="secondary" onClick={() => setEditing(true)}>Редагувати</Button>}
              {data.activeOrderId ? (
                <Link href={`/orders/view/?id=${data.activeOrderId}`} className="rounded-md border border-zinc-300 bg-white px-3 py-1.5 text-sm">
                  Активне замовлення {data.activeOrderNumber}
                </Link>
              ) : (
                <Link href={`/orders/new/?carId=${car.id}`} className="rounded-md bg-zinc-900 px-3 py-1.5 text-sm text-white">
                  Оформити замовлення
                </Link>
              )}
            </>
          )
        }
      >
        {car.make} {car.model} ({car.year})
      </PageTitle>
      <SuccessBanner message={notice} />

      <Card title="Дані автомобіля">
        {editing ? (
          <CarForm
            initial={{ make: car.make, model: car.model, year: String(car.year), vin: car.vin, plateNumber: car.plateNumber }}
            submitLabel="Зберегти"
            onSubmit={async (values) => {
              await api.put(`/api/cars/${car.id}`, carPayload(values));
              setEditing(false);
              setNotice("Дані автомобіля збережено.");
              await reload();
            }}
          />
        ) : (
          <dl className="grid gap-2 text-sm sm:grid-cols-4">
            <div><dt className="text-zinc-500">Держномер</dt><dd>{car.plateNumber}</dd></div>
            <div><dt className="text-zinc-500">VIN</dt><dd>{car.vin}</dd></div>
            <div>
              <dt className="text-zinc-500">Власник</dt>
              <dd><Link href={`/clients/view/?id=${owner.id}`} className="underline">{owner.fullName}</Link></dd>
            </div>
            <div><dt className="text-zinc-500">Телефон</dt><dd>{owner.phone}</dd></div>
          </dl>
        )}
      </Card>

      <Card title="Замовлення">
        <OrdersTable orders={data.orders} />
      </Card>
    </>
  );
}

export default function CarPage() {
  return (
    <Suspense fallback={<Loading />}>
      <CarCard />
    </Suspense>
  );
}
