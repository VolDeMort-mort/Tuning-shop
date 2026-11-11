"use client";

import Link from "next/link";
import { Empty, StatusBadge, Table, Td } from "@/components/ui";
import { formatDate, formatDateTime } from "@/lib/format";
import type { OrderSummary } from "@/lib/types";

export function OrdersTable({ orders, completed = false }: { orders: OrderSummary[]; completed?: boolean }) {
  if (orders.length === 0) return <Empty>Замовлень немає.</Empty>;
  return (
    <Table head={["Номер", "Створено", "Автомобіль", "Клієнт", "Статус", completed ? "Завершення" : "Прогрес"]}>
      {orders.map((o) => (
        <tr key={o.id} className="hover:bg-zinc-50">
          <Td>
            <Link href={`/orders/view/?id=${o.id}`} className="font-medium underline">
              {o.number}
            </Link>
          </Td>
          <Td>{formatDateTime(o.createdAt)}</Td>
          <Td>
            {o.carTitle}
            <div className="text-xs text-zinc-500">{o.plateNumber}</div>
          </Td>
          <Td>{o.clientName}</Td>
          <Td>
            <StatusBadge status={o.status} name={o.statusName} />
            {o.allWorksDone && o.status === "InProgress" && (
              <div className="mt-1 text-xs font-medium text-emerald-700">усі роботи виконано</div>
            )}
          </Td>
          <Td>
            {completed
              ? o.status === "Issued"
                ? `Видано ${formatDate(o.issuedAt)}`
                : `Причина: ${o.cancelReason}`
              : o.progress}
          </Td>
        </tr>
      ))}
    </Table>
  );
}
