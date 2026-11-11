"use client";

import { useCallback, useState } from "react";
import { useAuth } from "@/components/auth";
import { OrdersTable } from "@/components/OrdersTable";
import { Card, ErrorBanner, Loading, PageTitle, SelectField } from "@/components/ui";
import { useLoad } from "@/components/useLoad";
import { useMasters } from "@/components/useMasters";
import { api, query } from "@/lib/api";
import type { OrderStatus, OrderSummary } from "@/lib/types";

/** UC-06 «Переглянути поточні замовлення» з фільтрами за статусом і майстром (FR-16). */
export default function CurrentOrdersPage() {
  const { user } = useAuth();
  const [status, setStatus] = useState<OrderStatus | "">("");
  const [masterId, setMasterId] = useState("");
  const masters = useMasters(user?.role === "Manager");
  const load = useCallback(
    () => api.get<OrderSummary[]>("/api/orders/current" + query({ status, masterId })),
    [status, masterId],
  );
  const { data, error } = useLoad(load);

  return (
    <>
      <PageTitle>Поточні замовлення</PageTitle>
      <Card>
        <div className="mb-4 grid gap-3 sm:grid-cols-3">
          <SelectField label="Статус" value={status} onChange={(e) => setStatus(e.target.value as OrderStatus | "")}>
            <option value="">Усі поточні</option>
            <option value="New">Нове</option>
            <option value="InProgress">В роботі</option>
            <option value="Ready">Готове до видачі</option>
          </SelectField>
          {masters.length > 0 && (
            <SelectField label="Майстер" value={masterId} onChange={(e) => setMasterId(e.target.value)}>
              <option value="">Усі майстри</option>
              {masters.map((m) => (
                <option key={m.id} value={m.id}>{m.fullName}</option>
              ))}
            </SelectField>
          )}
        </div>
        <ErrorBanner error={error} />
        {data ? <OrdersTable orders={data} /> : !error && <Loading />}
      </Card>
    </>
  );
}
