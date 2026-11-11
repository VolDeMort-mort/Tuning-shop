"use client";

import { useCallback, useState } from "react";
import { OrdersTable } from "@/components/OrdersTable";
import { Card, ErrorBanner, Field, Loading, PageTitle } from "@/components/ui";
import { useLoad } from "@/components/useLoad";
import { api, query } from "@/lib/api";
import type { OrderSummary } from "@/lib/types";

/** UC-07 «Переглянути завершені замовлення» за період (FR-17). */
export default function CompletedOrdersPage() {
  const [period, setPeriod] = useState({ from: "", to: "" });
  const load = useCallback(() => api.get<OrderSummary[]>("/api/orders/completed" + query(period)), [period]);
  const { data, error } = useLoad(load);

  return (
    <>
      <PageTitle>Завершені замовлення</PageTitle>
      <Card>
        <div className="mb-4 grid gap-3 sm:grid-cols-4">
          <Field label="З" type="date" value={period.from} onChange={(e) => setPeriod({ ...period, from: e.target.value })} />
          <Field label="По" type="date" value={period.to} onChange={(e) => setPeriod({ ...period, to: e.target.value })} />
        </div>
        <ErrorBanner error={error} />
        {data ? <OrdersTable orders={data} completed /> : !error && <Loading />}
      </Card>
    </>
  );
}
