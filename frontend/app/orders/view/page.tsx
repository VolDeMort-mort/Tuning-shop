"use client";

import Link from "next/link";
import { useSearchParams } from "next/navigation";
import { Suspense, useCallback, useState, type FormEvent } from "react";
import { useAuth } from "@/components/auth";
import { FormError, useForm } from "@/components/forms";
import {
  Button, Card, ErrorBanner, Field, Loading, PageTitle, SelectField, StatusBadge, SuccessBanner, Table, Td,
} from "@/components/ui";
import { useLoad } from "@/components/useLoad";
import { useMasters } from "@/components/useMasters";
import { emptyWork, WorkFields, workPayload } from "@/components/WorkFields";
import { api, ApiError } from "@/lib/api";
import { categoryNames, formatDate, formatDateTime, formatMoney } from "@/lib/format";
import type { Employee, OrderDetails, WorkInput, WorkItem } from "@/lib/types";

type Apply = (action: () => Promise<OrderDetails>, notice: string) => Promise<void>;

const actionLabels = {
  startWork: "Передати в роботу",
  markReady: "Позначити готовим",
  issue: "Видати автомобіль",
} as const;

/** UC-04: дії, дозволені State Machine для поточного статусу; скасування — з обов'язковою причиною. */
function StatusActions({ order, apply }: { order: OrderDetails; apply: Apply }) {
  const [cancelling, setCancelling] = useState(false);
  const [reason, setReason] = useState("");
  const [reasonError, setReasonError] = useState<string>();
  const routes = { startWork: "start-work", markReady: "mark-ready", issue: "issue" } as const;

  if (order.allowedActions.length === 0)
    return <p className="text-sm text-zinc-500">Замовлення у фінальному статусі — лише перегляд.</p>;

  return (
    <div className="flex flex-col gap-3">
      <div className="flex flex-wrap gap-2">
        {order.allowedActions
          .filter((a): a is keyof typeof routes => a !== "cancel")
          .map((a) => (
            <Button key={a} onClick={() => apply(() => api.post(`/api/orders/${order.id}/${routes[a]}`), `Виконано: ${actionLabels[a].toLowerCase()}.`)}>
              {actionLabels[a]}
            </Button>
          ))}
        {order.allowedActions.includes("cancel") && !cancelling && (
          <Button variant="danger" onClick={() => setCancelling(true)}>Скасувати замовлення</Button>
        )}
      </div>
      {cancelling && (
        <form
          className="flex flex-wrap items-end gap-2"
          onSubmit={async (e) => {
            e.preventDefault();
            setReasonError(undefined);
            try {
              await apply(() => api.post(`/api/orders/${order.id}/cancel`, { reason }), "Замовлення скасовано.");
            } catch (err) {
              if (err instanceof ApiError) setReasonError(err.fields.reason ?? err.message);
            }
          }}
        >
          <div className="min-w-72 flex-1">
            <Field label="Причина скасування *" value={reason} error={reasonError} onChange={(e) => setReason(e.target.value)} />
          </div>
          <Button type="submit" variant="danger">Підтвердити скасування</Button>
          <Button variant="secondary" onClick={() => setCancelling(false)}>Відміна</Button>
        </form>
      )}
    </div>
  );
}

/** Рядок роботи: редагування й видалення запланованої роботи (FR-07), майстер (FR-08), деталі (FR-20). */
function WorkRow({ work, order, masters, canManage, apply }: {
  work: WorkItem; order: OrderDetails; masters: Employee[]; canManage: boolean; apply: Apply;
}) {
  const [editing, setEditing] = useState(false);
  const [value, setValue] = useState<WorkInput>({
    category: work.category, description: work.description, estimatedCost: String(work.estimatedCost), masterId: "",
  });
  const [error, setError] = useState<unknown>(null);
  const part = useForm({ name: "", quantity: "1", expectedAt: "" });
  const editable = canManage && order.canEditWorks && work.status === "Planned";
  const fields = error instanceof ApiError ? error.fields : {};

  async function save(e: FormEvent) {
    e.preventDefault();
    setError(null);
    try {
      await apply(() => api.put(`/api/works/${work.id}`, workPayload(value)), "Роботу змінено.");
      setEditing(false);
    } catch (err) {
      setError(err);
    }
  }

  return (
    <li className="rounded-md border border-zinc-200 p-3">
      {editing ? (
        <form onSubmit={save} className="flex flex-col gap-2">
          <FormError error={error} />
          <WorkFields value={value} onChange={setValue} masters={masters} errors={fields} showMaster={false} />
          <div className="flex gap-2">
            <Button type="submit">Зберегти</Button>
            <Button variant="secondary" onClick={() => setEditing(false)}>Відміна</Button>
          </div>
        </form>
      ) : (
        <div className="flex flex-wrap items-start justify-between gap-2 text-sm">
          <div>
            <div>
              <span className="text-zinc-500">{categoryNames[work.category]}:</span> <span className="font-medium">{work.description}</span>
            </div>
            <div className="text-zinc-600">
              {formatMoney(work.estimatedCost)} · майстер: {work.masterName ?? <span className="text-amber-700">не призначено</span>}
            </div>
          </div>
          <div className="flex flex-wrap items-center gap-2">
            <StatusBadge status={work.status} name={work.statusName} />
            <span className="text-xs text-zinc-500">{formatDateTime(work.statusChangedAt)}</span>
            {editable && (
              <>
                <Button variant="secondary" onClick={() => setEditing(true)}>Змінити</Button>
                <Button variant="secondary" onClick={() => apply(() => api.delete(`/api/works/${work.id}`), "Роботу видалено.")}>
                  Видалити
                </Button>
              </>
            )}
          </div>
        </div>
      )}

      {editable && !editing && (
        <div className="mt-2 max-w-xs">
          {fields.masterId && <p className="mb-1 text-xs text-rose-600">{fields.masterId}</p>}
          <SelectField
            label="Призначити майстра"
            value={work.masterId ?? ""}
            onChange={(e) => {
              if (!e.target.value) return;
              setError(null);
              apply(() => api.put(`/api/works/${work.id}/master`, { masterId: Number(e.target.value) }), "Майстра призначено.")
                .catch(setError);
            }}
          >
            <option value="">—</option>
            {masters.map((m) => (
              <option key={m.id} value={m.id}>{m.fullName}</option>
            ))}
          </SelectField>
        </div>
      )}

      <div className="mt-3 border-t border-zinc-100 pt-2 text-sm">
        <div className="mb-1 font-medium text-zinc-700">Деталі</div>
        {work.parts.length === 0 ? (
          <p className="text-zinc-500">Деталей не потрібно або ще не додано.</p>
        ) : (
          <ul className="flex flex-col gap-1">
            {work.parts.map((p) => (
              <li key={p.id} className="flex flex-wrap items-center gap-2">
                <span>{p.name} × {p.quantity}</span>
                <StatusBadge status={p.status} name={p.statusName} />
                <span className="text-xs text-zinc-500">
                  {p.status === "Received" ? `отримано ${formatDateTime(p.receivedAt)}` : p.expectedAt ? `очікується ${formatDate(p.expectedAt)}` : ""}
                </span>
                {canManage && order.canEditWorks && p.status === "Awaiting" && (
                  <Button variant="secondary" onClick={() => apply(() => api.post(`/api/parts/${p.id}/receive`), "Надходження деталі відмічено.")}>
                    Відмітити надходження
                  </Button>
                )}
              </li>
            ))}
          </ul>
        )}
        {canManage && order.canEditWorks && (
          <form
            className="mt-2 grid gap-2 sm:grid-cols-[1fr_6rem_10rem_auto] sm:items-end"
            onSubmit={(e) => {
              e.preventDefault();
              part.run(async () => {
                await apply(
                  () => api.post(`/api/works/${work.id}/parts`, {
                    name: part.values.name,
                    quantity: part.values.quantity === "" ? null : Number(part.values.quantity),
                    expectedAt: part.values.expectedAt || null,
                  }),
                  "Деталь додано.",
                );
                part.setValues({ name: "", quantity: "1", expectedAt: "" });
              });
            }}
          >
            <Field label="Назва деталі" {...part.bind("name")} />
            <Field label="Кількість" type="number" min="1" {...part.bind("quantity")} />
            <Field label="Очікується" type="date" {...part.bind("expectedAt")} />
            <Button type="submit" variant="secondary" disabled={part.busy}>Додати деталь</Button>
          </form>
        )}
      </div>
    </li>
  );
}

function AddWork({ order, masters, apply }: { order: OrderDetails; masters: Employee[]; apply: Apply }) {
  const [value, setValue] = useState<WorkInput>({ ...emptyWork });
  const [error, setError] = useState<unknown>(null);
  const fields = error instanceof ApiError ? error.fields : {};
  return (
    <form
      className="mt-4 border-t border-zinc-100 pt-4"
      onSubmit={async (e) => {
        e.preventDefault();
        setError(null);
        try {
          await apply(() => api.post(`/api/orders/${order.id}/works`, workPayload(value)), "Роботу додано.");
          setValue({ ...emptyWork });
        } catch (err) {
          setError(err);
        }
      }}
    >
      <h3 className="mb-2 text-sm font-semibold">Додати роботу</h3>
      <FormError error={error} />
      <WorkFields value={value} onChange={setValue} masters={masters} errors={fields} />
      <Button type="submit" className="mt-3">Додати</Button>
    </form>
  );
}

/** Картка замовлення (FR-19): клієнт, автомобіль, роботи з майстрами й деталями, прогрес, сума, історія. */
function OrderCard() {
  const params = useSearchParams();
  const id = params.get("id");
  const { user } = useAuth();
  const canManage = user?.role === "Manager";
  const masters = useMasters(canManage);
  const [notice, setNotice] = useState<string | null>(params.get("created") ? "Замовлення збережено." : null);
  const [actionError, setActionError] = useState<unknown>(null);
  const load = useCallback(() => api.get<OrderDetails>(`/api/orders/${id}`), [id]);
  const { data: order, setData, error } = useLoad(id ? load : null);

  // Кожна дія повертає оновлену картку — показуємо збережений стан після підтвердження запису.
  const apply: Apply = async (action, message) => {
    setActionError(null);
    setNotice(null);
    try {
      setData(await action());
      setNotice(message);
    } catch (err) {
      if (err instanceof ApiError && err.body.code === "validation_failed") throw err; // помилки біля полів форми
      setActionError(err);
    }
  };

  if (error) return <ErrorBanner error={error} />;
  if (!order) return <Loading />;

  return (
    <>
      <PageTitle>
        Замовлення {order.number} <StatusBadge status={order.status} name={order.statusName} />
      </PageTitle>
      <SuccessBanner message={notice} />
      <ErrorBanner error={actionError} />

      <div className="grid gap-x-5 md:grid-cols-2">
        <Card title="Клієнт і автомобіль">
          <dl className="grid gap-2 text-sm">
            <div>
              <dt className="text-zinc-500">Клієнт</dt>
              <dd><Link href={`/clients/view/?id=${order.client.id}`} className="underline">{order.client.fullName}</Link>, {order.client.phone}</dd>
            </div>
            <div>
              <dt className="text-zinc-500">Автомобіль</dt>
              <dd>
                <Link href={`/cars/view/?id=${order.car.id}`} className="underline">{order.car.make} {order.car.model} ({order.car.year})</Link>
                , {order.car.plateNumber}, VIN {order.car.vin}
              </dd>
            </div>
            <div><dt className="text-zinc-500">Побажання</dt><dd>{order.clientRequest || "—"}</dd></div>
          </dl>
        </Card>
        <Card title="Стан">
          <dl className="grid grid-cols-2 gap-2 text-sm">
            <div><dt className="text-zinc-500">Створено</dt><dd>{formatDateTime(order.createdAt)}</dd></div>
            <div><dt className="text-zinc-500">Прогрес</dt><dd className="font-medium">{order.progress}</dd></div>
            <div><dt className="text-zinc-500">Орієнтовна вартість</dt><dd className="font-medium">{formatMoney(order.totalEstimate)}</dd></div>
            {order.issuedAt && <div><dt className="text-zinc-500">Видано</dt><dd>{formatDateTime(order.issuedAt)}</dd></div>}
            {order.cancelReason && <div><dt className="text-zinc-500">Причина скасування</dt><dd>{order.cancelReason}</dd></div>}
          </dl>
          {order.allWorksDone && order.status === "InProgress" && (
            <p className="mt-2 text-sm font-medium text-emerald-700">Усі роботи виконано — замовлення можна позначити готовим.</p>
          )}
        </Card>
      </div>

      {canManage && (
        <Card title="Статус замовлення">
          <StatusActions order={order} apply={apply} />
        </Card>
      )}

      <Card title={`Роботи (${order.workCount})`}>
        <ul className="flex flex-col gap-3">
          {order.works.map((w) => (
            <WorkRow key={w.id} work={w} order={order} masters={masters} canManage={canManage} apply={apply} />
          ))}
        </ul>
        {canManage && order.canEditWorks && <AddWork order={order} masters={masters} apply={apply} />}
      </Card>

      <Card title="Історія статусів">
        <Table head={["Дата й час", "Попередній", "Новий", "Користувач", "Коментар"]}>
          {order.history.map((h, i) => (
            <tr key={i}>
              <Td>{formatDateTime(h.changedAt)}</Td>
              <Td>{h.fromName ?? "—"}</Td>
              <Td>{h.toName}</Td>
              <Td>{h.employeeName}</Td>
              <Td>{h.comment ?? ""}</Td>
            </tr>
          ))}
        </Table>
      </Card>
    </>
  );
}

export default function OrderPage() {
  return (
    <Suspense fallback={<Loading />}>
      <OrderCard />
    </Suspense>
  );
}
