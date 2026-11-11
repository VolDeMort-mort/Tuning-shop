"use client";

import { useCallback, useState } from "react";
import { Button, Card, Empty, ErrorBanner, Loading, PageTitle, StatusBadge, SuccessBanner } from "@/components/ui";
import { useLoad } from "@/components/useLoad";
import { api } from "@/lib/api";
import { categoryNames, formatDate, formatDateTime } from "@/lib/format";
import type { MasterWork } from "@/lib/types";

const nextAction = { Planned: "Розпочати", InProgress: "Завершити" } as const;

/** UC-05 «Виконати призначену роботу»: перелік робіт майстра (FR-10) і зміна статусу лише вперед (FR-11). */
export default function MasterPage() {
  const load = useCallback(() => api.get<MasterWork[]>("/api/master/works"), []);
  const { data: works, error, reload } = useLoad(load);
  const [actionError, setActionError] = useState<unknown>(null);
  const [notice, setNotice] = useState<string | null>(null);

  async function advance(work: MasterWork) {
    setActionError(null);
    setNotice(null);
    try {
      const updated = await api.post<MasterWork>(`/api/master/works/${work.workId}/advance`);
      setNotice(`«${updated.description}»: ${updated.statusName.toLowerCase()} · замовлення ${updated.orderNumber}, ${updated.orderProgress}.`);
    } catch (err) {
      setActionError(err);
    }
    await reload();
  }

  return (
    <>
      <PageTitle>Мої роботи</PageTitle>
      <SuccessBanner message={notice} />
      <ErrorBanner error={actionError ?? error} />
      {!works ? (
        !error && <Loading />
      ) : works.length === 0 ? (
        <Empty>Призначених робіт у замовленнях «В роботі» немає.</Empty>
      ) : (
        works.map((w) => (
          <Card
            key={w.workId}
            title={`${w.orderNumber} · ${w.carTitle}, ${w.plateNumber}`}
            actions={w.status !== "Done" && <Button onClick={() => advance(w)}>{nextAction[w.status]}</Button>}
          >
            <div className="flex flex-wrap items-center justify-between gap-2 text-sm">
              <span>
                <span className="text-zinc-500">{categoryNames[w.category]}:</span> <span className="font-medium">{w.description}</span>
              </span>
              <span className="flex items-center gap-2">
                <StatusBadge status={w.status} name={w.statusName} />
                <span className="text-xs text-zinc-500">{formatDateTime(w.statusChangedAt)}</span>
              </span>
            </div>
            <p className="mt-1 text-sm text-zinc-600">Прогрес замовлення: {w.orderProgress}</p>
            <div className="mt-2 text-sm">
              <span className="font-medium text-zinc-700">Деталі: </span>
              {w.parts.length === 0
                ? "не потрібні"
                : w.parts.map((p) => (
                    <span key={p.id} className="mr-3 inline-flex items-center gap-1">
                      {p.name} × {p.quantity} <StatusBadge status={p.status} name={p.statusName} />
                      {p.status === "Awaiting" && p.expectedAt && <span className="text-xs text-zinc-500">до {formatDate(p.expectedAt)}</span>}
                    </span>
                  ))}
            </div>
          </Card>
        ))
      )}
    </>
  );
}
