"use client";

import Link from "next/link";
import { useRouter, useSearchParams } from "next/navigation";
import { Suspense, useCallback, useState, type FormEvent } from "react";
import { FormError } from "@/components/forms";
import { Button, Card, Empty, ErrorBanner, Field, Loading, PageTitle } from "@/components/ui";
import { useLoad } from "@/components/useLoad";
import { useMasters } from "@/components/useMasters";
import { emptyWork, WorkFields, workPayload } from "@/components/WorkFields";
import { api, ApiError, query } from "@/lib/api";
import { formatMoney } from "@/lib/format";
import type { Car, CarDetails, OrderDetails, WorkInput } from "@/lib/types";

/** Крок 1 UC-03: пошук автомобіля за держномером або VIN (UC-11). */
function CarSearch() {
  const router = useRouter();
  const [text, setText] = useState("");
  const [cars, setCars] = useState<Car[] | null>(null);
  const [error, setError] = useState<unknown>(null);

  async function search(e: FormEvent) {
    e.preventDefault();
    setError(null);
    try {
      const found = await api.get<Car[]>("/api/cars" + query({ query: text }));
      if (found.length === 1) router.push(`/orders/new/?carId=${found[0].id}`);
      else setCars(found);
    } catch (err) {
      setError(err);
    }
  }

  return (
    <Card title="1. Знайдіть автомобіль">
      <form onSubmit={search} className="flex flex-wrap items-end gap-3">
        <div className="min-w-64 flex-1">
          <Field label="Держномер або VIN" value={text} onChange={(e) => setText(e.target.value)} />
        </div>
        <Button type="submit">Знайти</Button>
      </form>
      <ErrorBanner error={error} />
      {cars && cars.length === 0 && (
        // A1: автомобіль не знайдено — менеджер реєструє його (UC-02, за потреби UC-01).
        <div className="mt-3 text-sm">
          <Empty>Автомобіль не знайдено.</Empty>
          <Link href="/clients/" className="underline">Знайти або зареєструвати клієнта й додати автомобіль →</Link>
        </div>
      )}
    </Card>
  );
}

/** Кроки 3–12 UC-03: побажання, роботи з майстрами, збереження. */
function OrderForm({ carId }: { carId: string }) {
  const router = useRouter();
  const masters = useMasters(true);
  const load = useCallback(() => api.get<CarDetails>(`/api/cars/${carId}`), [carId]);
  const { data: car, error: loadError } = useLoad(load);
  const [clientRequest, setClientRequest] = useState("");
  const [works, setWorks] = useState<WorkInput[]>([{ ...emptyWork }]);
  const [error, setError] = useState<unknown>(null);
  const [busy, setBusy] = useState(false);
  const fields = error instanceof ApiError ? error.fields : {};

  if (loadError) return <ErrorBanner error={loadError} />;
  if (!car) return <Loading />;

  // A2: активне замовлення вже є — нове не оформлюється.
  if (car.activeOrderId)
    return (
      <Card title="Оформлення неможливе">
        <p className="text-sm">
          Для автомобіля {car.car.plateNumber} вже є активне замовлення{" "}
          <Link href={`/orders/view/?id=${car.activeOrderId}`} className="font-medium underline">{car.activeOrderNumber}</Link>.
        </p>
      </Card>
    );

  const total = works.reduce((sum, w) => sum + (Number(w.estimatedCost) || 0), 0);
  const withoutMaster = works.filter((w) => w.masterId === "").length;

  async function submit(e: FormEvent) {
    e.preventDefault();
    setBusy(true);
    setError(null);
    try {
      const order = await api.post<OrderDetails>("/api/orders", {
        carId: Number(carId),
        clientRequest,
        works: works.map(workPayload),
      });
      router.push(`/orders/view/?id=${order.id}&created=1`);
    } catch (err) {
      setError(err);
    } finally {
      setBusy(false);
    }
  }

  return (
    <form onSubmit={submit}>
      <Card title="Автомобіль">
        <p className="text-sm">
          {car.car.make} {car.car.model} ({car.car.year}), {car.car.plateNumber} · власник {car.owner.fullName}, {car.owner.phone}
        </p>
      </Card>

      <Card title="2. Побажання клієнта">
        <textarea className="w-full rounded-md border border-zinc-300 p-2 text-sm" rows={3} value={clientRequest}
          onChange={(e) => setClientRequest(e.target.value)} />
      </Card>

      <Card title="3. Роботи" actions={<Button variant="secondary" onClick={() => setWorks([...works, { ...emptyWork }])}>Додати роботу</Button>}>
        <FormError error={error} />
        {fields.works && <p className="mb-3 text-sm text-rose-700">{fields.works}</p>}
        <div className="flex flex-col gap-4">
          {works.map((w, i) => (
            <div key={i} className="flex items-start gap-2 border-b border-zinc-100 pb-4">
              <div className="flex-1">
                <WorkFields
                  value={w}
                  masters={masters}
                  onChange={(value) => setWorks(works.map((x, j) => (j === i ? value : x)))}
                  errors={{
                    category: fields[`works[${i}].category`],
                    description: fields[`works[${i}].description`],
                    estimatedCost: fields[`works[${i}].estimatedCost`],
                    masterId: fields[`works[${i}].masterId`],
                  }}
                />
              </div>
              {works.length > 1 && (
                <Button variant="secondary" className="mt-6" onClick={() => setWorks(works.filter((_, j) => j !== i))}>✕</Button>
              )}
            </div>
          ))}
        </div>
        {withoutMaster > 0 && (
          // A4: робота без майстра — попередження про FR-09.
          <p className="mt-3 text-sm text-amber-700">
            Робіт без майстра: {withoutMaster}. Замовлення не можна буде передати в роботу, доки майстрів не призначено.
          </p>
        )}
        <p className="mt-3 text-sm">Орієнтовна вартість: <span className="font-semibold">{formatMoney(total)}</span></p>
      </Card>

      <Button type="submit" disabled={busy}>Зберегти замовлення</Button>
    </form>
  );
}

function NewOrder() {
  const carId = useSearchParams().get("carId");
  return (
    <>
      <PageTitle>Оформити замовлення</PageTitle>
      {carId ? <OrderForm carId={carId} /> : <CarSearch />}
    </>
  );
}

export default function NewOrderPage() {
  return (
    <Suspense fallback={<Loading />}>
      <NewOrder />
    </Suspense>
  );
}
